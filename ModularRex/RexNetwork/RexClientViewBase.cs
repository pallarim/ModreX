using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using ModularRex.RexFramework;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;
using ModularRex.RexNetwork.RexLogin;
using System.Timers;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// Inherits from LLClientView the majority of functionality
    /// Overrides and extends for Rex-specific functionality.
    /// 
    /// In the case whereby functionality uses the same packets but differs
    /// between Rex and LL, you can use a override on those specific functions
    /// to overload the request.
    /// 
    /// This class acts as a base class for legacy client and new rex-ng client (Bob)
    /// </summary>                                                                 
    public class RexClientViewBase : LLClientView, IClientRexFaceExpression, IClientRexAppearance, IClientMediaURL, IRexClientCore
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Dictionary<string, RexGenericMessageDelegate> m_genericMessageHandlers = new Dictionary<string, RexGenericMessageDelegate>();

        private string m_rexAccountID;
        private string m_rexAvatarURL;
        private string m_rexAvatarURLOverride;
        private string m_rexAuthURL;

        private float m_RexCharacterSpeedMod = 1.0f;
        private float m_RexVertMovementSpeedMod = 1.0f;

        public event RexAppearanceDelegate OnRexAppearance;
        public event RexGenericMessageDelegate OnRexFaceExpression;
        public event RexGenericMessageDelegate OnRexAvatarProperties;
        public event RexObjectPropertiesDelegate OnRexObjectProperties;
        public event RexStartUpDelegate OnRexStartUp;
        public event RexClientScriptCmdDelegate OnRexClientScriptCmd;
        public event ReceiveRexMediaURL OnReceiveRexMediaURL;
        public event RexGenericMessageDelegate OnPrimFreeData;

        public RexClientViewBase(EndPoint remoteEP, IScene scene, IAssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            // Rex communication now occurs via GenericMessage
            // We have a special handler here below.
            AddGenericPacketHandlers();

            OnBinaryGenericMessage += RexClientView_BinaryGenericMessage;
            OnGenericMessage += RealXtendClientView_OnGenericMessage;
        }

        public RexClientViewBase(EndPoint remoteEP, IScene scene, IAssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, string rexAvatarURL, string rexAuthURL, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            // Rex communication now occurs via GenericMessage
            // We need to register GenericMessage handlers

            AddGenericPacketHandlers();

            OnBinaryGenericMessage += RexClientView_BinaryGenericMessage;

            RexAvatarURL = rexAvatarURL;
            RexAuthURL = rexAuthURL;
        }

        private void AddGenericPacketHandlers()
        {
            AddGenericPacketHandler("RexAppearance", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexFaceExpression", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexAvatarProp", RealXtendClientView_OnGenericMessage);

            //This added here only to disable warning about unhandled generic message
            //RexPrimData is actually handled  in RexClientView_BinaryGenericMessage
            AddGenericPacketHandler("RexPrimData", RealXtendClientView_OnGenericMessage); 
            AddGenericPacketHandler("RexData", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexMediaUrl", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("rexstartup", RealXtendClientView_OnGenericMessage);

            m_genericMessageHandlers.Add("rexfaceexpression", OnRexFaceExpression);
            m_genericMessageHandlers.Add("rexavatarprop", OnRexAvatarProperties);
            m_genericMessageHandlers.Add("rexmediaurl", TriggerOnReceivedRexMediaURL);
            m_genericMessageHandlers.Add("rexdata", TriggerOnPrimFreeData);
        }

        

        /// <summary>
        /// Registers interfaces for IClientCore,
        /// every time you make a new Rex-specific
        /// Interface. Make sure to register it here.
        /// </summary>
        protected override void RegisterInterfaces()
        {
            RegisterInterface<IClientRexAppearance>(this);
            RegisterInterface<IClientRexFaceExpression>(this);
            RegisterInterface<IClientMediaURL>(this);
            RegisterInterface<IRexClientCore>(this);

            // Register our own class 'as-is' so it can be
            // used via IClientCore.Get<RexClientView>()...
            RegisterInterface(this);

            base.RegisterInterfaces();
        }

        #region Properties

        /// <summary>
        /// The avatar URL for this avatar
        /// Eg: http://avatar.com:10000/uuid/
        /// </summary>
        public string RexAvatarURL
        {
            get { return m_rexAvatarURL; }
            set
            {
                m_rexAvatarURL = value;
                if (OnRexAppearance != null)
                {
                    OnRexAppearance(this);
                    return;
                }
            }
        }

        /// <summary>
        /// The avatar URL override for this avatar
        /// Eg: http://avatar.com:10000/uuid/
        /// </summary>
        public string RexAvatarURLOverride
        {
            get { return m_rexAvatarURLOverride; }
            set
            {
                m_rexAvatarURLOverride = value;
                if (OnRexAppearance != null)
                {
                    OnRexAppearance(this);
                    return;
                }
            }
        }

        /// <summary>
        /// The URL to avatar appearance which this view currently uses.
        /// If override is used, return it. Otherwise return normal avatar url.
        /// Eg: http://avatar.com:10000/uuid/
        /// </summary>
        public string RexAvatarURLVisible
        {
            get
            {
                if (!string.IsNullOrEmpty(RexAvatarURLOverride))
                    return RexAvatarURLOverride;
                else
                    return RexAvatarURL;         
            }
        }

        /// <summary>
        /// The full Rex Username of this account
        /// Eg: user@hostname.com:10001
        /// 
        /// Note: This is not filled immedietely on
        /// creation. This property is filled in
        /// via Login and may not be availible
        /// immedietely upon connect.
        /// 
        /// The above glitch is scheduled to be
        /// fixed by a new RexCommsManager which
        /// will allow this to be set at spawn in
        /// login.
        /// </summary>
        public string RexAccount
        {
            get { return m_rexAccountID; }
            set
            {
                // Todo: More solid data checking here.
                m_rexAccountID = value;
                RexAuthURL = m_rexAccountID.Split('@')[1];
            }
        }

        /// <summary>
        /// The URL of the Avatar's Authentication Server
        /// Eg: http://authentication.com:10001/
        /// </summary>
        public string RexAuthURL
        {
            get { return m_rexAuthURL; }
            set
            {
                m_rexAuthURL = value;
            }
        }
        
        public float RexCharacterSpeedMod
        {
            get { return m_RexCharacterSpeedMod; }
            set { m_RexCharacterSpeedMod = value; }
        }  
                  
        public float RexVertMovementSpeedMod
        {
            get { return m_RexVertMovementSpeedMod; }
            set { m_RexVertMovementSpeedMod = value; }
        }

        #endregion
        

        protected void RexClientView_BinaryGenericMessage(Object sender, string method, byte[][] args)
        {
            if(method == "RexPrimData".ToLower())
            {
                HandleRexPrimData(args);
                return;
            }
        }

        private void HandleRexPrimData(byte[][] args)
        {
            int rpdLen = 0;
            int idx = 0;
            bool first = false;
            UUID id = UUID.Zero;

            foreach (byte[] arg in args)
            {
                if (!first)
                {
                    id = new UUID(Util.FieldToString(arg));
                    first = true;
                    continue;
                }

                rpdLen += arg.Length;
            }

            first = false;
            byte[] rpdArray = new byte[rpdLen];

            foreach (byte[] arg in args)
            {
                if (!first)
                {
                    first = true;
                    continue;
                }

                arg.CopyTo(rpdArray, idx);
                idx += arg.Length;
            }

            //by default message doesn't contain URIs. However, with NG-client situation can be quite different
            TriggerOnRexObjectProperties(this, id, new RexObjectProperties(rpdArray, false));
        }

        protected void TriggerOnRexObjectProperties(RexClientViewBase client, UUID id, RexObjectProperties robject)
        {
            if (OnRexObjectProperties != null)
            {
                OnRexObjectProperties(client, id, robject);
            }
        }

        /// <summary>
        /// Special - used to convert GenericMessage packets
        /// to their appropriate Rex equivilents.
        /// 
        /// Eg: GenericMessage(RexAppearance) ->
        ///     OnRexAppearance(...)
        /// </summary>
        private void RealXtendClientView_OnGenericMessage(object sender, string method, List<string> args)
        {
            RexGenericMessageDelegate handler;
            if (m_genericMessageHandlers.ContainsKey(method.ToLower()))
            {
                handler = m_genericMessageHandlers[method.ToLower()];
                if (handler != null)
                {
                    handler(this, args);
                }
                return;
            }

            if (method.ToLower() == "rexappearance")
            {
                if (OnRexAppearance != null)
                {
                    OnRexAppearance(this);
                    return;
                }
            }

            if (method == "rexscr")
            {
                if (OnRexClientScriptCmd != null)
                {
                    OnRexClientScriptCmd(this, AgentId, args);
                    return;
                }
            }

            if (method == "rexstartup")
            {
                if (OnRexStartUp != null)
                {
                    OnRexStartUp(this, AgentId, args[1]);
                    return;
                }
            }
            if (method == "rexprimdata")
                return;

            m_log.Warn("[REXCLIENT] Unhandled GenericMessage (" + method + ") {");
            foreach (string s in args)
            {
                m_log.Warn("\t" + s);
            }
            m_log.Warn("}");
        }

        private void TriggerOnPrimFreeData(IClientAPI sender, List<string> args)
        {
            try
            {
                //foreach (string s in args)
                //{
                //    m_log.Debug("[REXCLIENT] PrimFreeData: " + s);
                //}

                if (OnPrimFreeData != null)
                {
                    OnPrimFreeData(this, args);
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[REXCLIENT] Error parseing incoming prim free data. Exception: ", e);
            }
        }

        private void TriggerOnReceivedRexMediaURL(IClientAPI sender, List<string> args)
        {
            try
            {
                foreach (string s in args)
                {
                    m_log.Debug("[REXCLIENT] MediaURL: " + s);
                }

                UUID assetID = new UUID(args[0]);
                string mediaUrl = args[1];
                byte refreshRate = Convert.ToByte(args[2]);

                if (OnReceiveRexMediaURL != null)
                {
                    OnReceiveRexMediaURL(this, AgentId, assetID, mediaUrl, refreshRate);
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[REXCLIENT] Error parseing incoming media url. Exception: ", e);
            }
        }

        public virtual void SendRexObjectProperties(UUID id, RexObjectProperties x)
        {
            GenericMessagePacket gmp = new GenericMessagePacket();
            gmp.MethodData.Method = Utils.StringToBytes("RexPrimData");

            byte[] temprexprimdata = x.GetRexPrimDataToBytes(false);
            int numlines = 0;
            int i = 0;

            if (temprexprimdata != null)
            {
                while (i <= temprexprimdata.Length)
                {
                    numlines++;
                    i += 200;
                }
            }

            gmp.ParamList = new GenericMessagePacket.ParamListBlock[1 + numlines];
            gmp.ParamList[0] = new GenericMessagePacket.ParamListBlock();
            gmp.ParamList[0].Parameter = Utils.StringToBytes(id.ToString());

            for (i = 0; i < numlines; i++)
            {
                gmp.ParamList[i + 1] = new GenericMessagePacket.ParamListBlock();

                if ((temprexprimdata.Length - i * 200) < 200)
                {
                    gmp.ParamList[i + 1].Parameter = new byte[temprexprimdata.Length - i * 200];
                    Buffer.BlockCopy(temprexprimdata, i * 200, gmp.ParamList[i + 1].Parameter, 0, temprexprimdata.Length - i * 200);
                }
                else
                {
                    gmp.ParamList[i + 1].Parameter = new byte[200];
                    Buffer.BlockCopy(temprexprimdata, i * 200, gmp.ParamList[i + 1].Parameter, 0, 200);
                }
            }

            // m_log.Warn("[REXDEBUG]: SendRexPrimData " + vPrimId.ToString());
            OutPacket(gmp, ThrottleOutPacketType.Task);
        }

        /// <summary>
        /// Sends a Rex Script Command to the viewer
        /// attached to this ClientView.
        /// 
        /// If you are coding something, try use
        /// SendRex*** instead, as many of them
        /// will trigger this instead with type
        /// and parameter checking.
        /// </summary>
        public void SendRexScriptCommand(string unit, string command, string parameters)
        {
            List<string> pack = new List<string>();

            pack.Add(unit);
            pack.Add(command);

            if (!string.IsNullOrEmpty(parameters))
                pack.Add(parameters);

            SendGenericMessage("RexScr", pack);
        }

        public void SendRexInventoryMessage(string message)
        {
            SendRexScriptCommand("hud", "ShowInventoryMessage(\"" + message + "\")", "");
        }

        public void SendRexScrollMessage(string message, double time)
        {
            SendRexScriptCommand("hud", "ShowScrollMessage(\"" + message + "\", \"" + time + "\")", "");
        }

        public void SendRexTutorialMessage(string message, double time)
        {
            SendRexScriptCommand("hud", "ShowScrollMessage(\"" + message + "\", \"" + time + "\")", "");
        }

        public void SendRexFadeInAndOut(string message, double between, double time)
        {
            SendRexScriptCommand("hud",
                                 "ShowInventoryMessage(\"" + message + "\","
                                 + " \"" + between + "\", \"" + time + "\")", "");
        }


        public void SendRexFaceExpression(List<string> expressionData)
        {
            expressionData.Insert(0, AgentId.ToString());
            SendGenericMessage("RexFaceExpression", expressionData);
        }

        public void SendRexAppearance(UUID agentID, string avatarURL, bool overrideUsed)
        {
            List<string> pack = new List<string>();
            pack.Add(avatarURL);
            pack.Add(agentID.ToString());
            pack.Add(overrideUsed.ToString());

            SendGenericMessage("RexAppearance", pack);
        }

        /// <summary>
        /// Sends Fog parameters to client. Only works underwater.
        /// </summary>
        /// <param name="start">meters from camera where the fog starts</param>
        /// <param name="end">meters from camera where the fog ends</param>
        /// <param name="red">redness in fog</param>
        /// <param name="green">greeness in fog</param>
        /// <param name="blue">blueness in fog</param>
        public void SendRexFog(float start, float end, float red, float green, float blue)
        {
            List<string> pack = new List<string>();

            pack.Add(start.ToString());
            pack.Add(end.ToString());
            pack.Add(red.ToString());
            pack.Add(green.ToString());
            pack.Add(blue.ToString());

            SendGenericMessage("RexFog", pack);
        }

        /// <summary>
        /// Sends water height to client. Usually used when changing water height on the fly with scripting.
        /// </summary>
        /// <param name="height">Water height in meters</param>
        public void SendRexWaterHeight(float height)
        {
            List<string> pack = new List<string>();

            pack.Add(height.ToString());

            SendGenericMessage("RexWaterHeight", pack);
        }

        /// <summary>
        /// Sends post postprosessing effect toggle to client.
        /// </summary>
        /// <see cref="http://rexdeveloper.org/wiki/index.php?title=Content_Scripting_Python_Post-Process"/>
        /// <param name="effectId">Id of the effect. See documentation for the effect ids</param>
        /// <param name="toggle">True to set effect on. False to set effect off.</param>
        public void SendRexPostProcess(int effectId, bool toggle)
        {
            List<string> pack = new List<string>();

            pack.Add(effectId.ToString());
            pack.Add(toggle.ToString());

            SendGenericMessage("RexPostP", pack);
        }

        /// <summary>
        /// Creates client side rtt camera
        /// </summary>
        /// <param name="command">0 to remove existing rtt camera (by name), 1 to add new rtt camera</param>
        /// <param name="name">Unique identifier for the camera</param>
        /// <param name="assetId">UUID of the texture that gets rendered to</param>
        /// <param name="pos">Position of the camera in the world</param>
        /// <param name="lookat">Point in the world the camera will look at</param>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        public void SendRexRttCamera(int command, string name, UUID assetId, Vector3 pos, Vector3 lookat, int width, int height)
        {
            string sPos = pos.X.ToString() + " " + pos.Y.ToString() + " " + pos.Z.ToString();
            sPos = sPos.Replace(",", ".");
            string sLookAt = lookat.X.ToString() + " " + lookat.Y.ToString() + " " + lookat.Z.ToString();
            sLookAt = sLookAt.Replace(",", ".");

            List<string> pack = new List<string>();

            pack.Add(command.ToString());
            pack.Add(name);
            pack.Add(assetId.ToString());
            pack.Add(sPos);
            pack.Add(sLookAt);
            pack.Add(width.ToString());
            pack.Add(height.ToString());

            SendGenericMessage("RexRttCam", pack);
        }

        /// <summary>
        /// Sends a viewport to client
        /// </summary>
        /// <param name="command">0 to remove existing viewport (by name), 1 to add new viewport.</param>
        /// <param name="name">Unique identifier for the viewport</param>
        /// <param name="posX">screen relative position of the left edge of the viewport</param>
        /// <param name="posY">screen relative position of the top edge of the viewport</param>
        /// <param name="width">screen relative width of the viewport</param>
        /// <param name="height">screen relative height of the viewport</param>
        public void SendRexViewport(int command, string name, float posX, float posY, float width, float height)
        {
            List<string> pack = new List<string>();

            pack.Add(command.ToString());
            pack.Add(name);
            pack.Add(posX.ToString());
            pack.Add(posY.ToString());
            pack.Add(width.ToString());
            pack.Add(height.ToString());

            SendGenericMessage("RexSetViewport", pack);
        }

        /// <summary>
        /// Toggles the wind sound on client
        /// </summary>
        /// <param name="toggle"></param>
        public void SendRexToggleWindSound(bool toggle)
        {
            List<string> pack = new List<string>();

            pack.Add(toggle.ToString());

            SendGenericMessage("RexToggleWindSound", pack);
        }

        /// <summary>
        /// Sends Rex clientside camera effects, particle script attached to camera etc.
        /// </summary>
        /// <param name="enable">True to enable the effect, False to disable</param>
        /// <param name="assetId">Id of the effect</param>
        /// <param name="pos">Offset position from the camera</param>
        /// <param name="rot">Offset rotation from the camera</param>
        public void SendRexCameraClientSideEffect(bool enable, UUID assetId, Vector3 pos, Quaternion rot)
        {
            List<string> pack = new List<string>();

            pack.Add(assetId.ToString());
            pack.Add(pos.ToString());
            pack.Add(rot.ToString());
            pack.Add(enable.ToString());

            SendGenericMessage("RexSCSEffect", pack);
        }

        /// <summary>
        /// Overrides default lighting conditions and ambient light in the world.
        /// 
        /// Note that this override is a hard one. The user will be unable to change the lighting
        /// conditions in any way after they are overridden. 
        /// </summary>
        /// <param name="direction">Direction of the global light (sun)</param>
        /// <param name="colour">Colour of the global light</param>
        /// <param name="ambientColour">Colour of the ambient light (the light that is always present)</param>
        public void SendRexSetAmbientLight(Vector3 direction, Vector3 colour, Vector3 ambientColour)
        {
            List<string> pack = new List<string>();

            string slightDirection = direction.X.ToString() + " " + direction.Y.ToString() + " " + direction.Z.ToString();
            slightDirection = slightDirection.Replace(",", ".");
            string slightColour = colour.X.ToString() + " " + colour.Y.ToString() + " " + colour.Z.ToString();
            slightColour = slightColour.Replace(",", ".");
            string sambientColour = ambientColour.X.ToString() + " " + ambientColour.Y.ToString() + " " + ambientColour.Z.ToString();
            sambientColour = sambientColour.Replace(",", ".");

            pack.Add(slightDirection);
            pack.Add(slightColour);
            pack.Add(sambientColour);

            SendGenericMessage("RexAmbientL", pack);
        }

        /// <summary>
        /// Lauch flash animation to play on client
        /// </summary>
        /// <param name="assetId">Id of the flash animation (swf) to play</param>
        /// <param name="left">left border of the rectangle</param>
        /// <param name="top">top border of the rectangle</param>
        /// <param name="right">right border of the rectangle</param>
        /// <param name="bottom">bottom border of the rectangle</param>
        /// <param name="timeToDeath">time in seconds from start of animation playback until the flash control is destroyed</param>
        public void SendRexPlayFlashAnimation(UUID assetId, float left, float top, float right, float bottom, float timeToDeath)
        {
            List<string> pack = new List<string>();

            pack.Add(assetId.ToString());
            pack.Add(left.ToString());
            pack.Add(top.ToString());
            pack.Add(right.ToString());
            pack.Add(bottom.ToString());
            pack.Add(timeToDeath.ToString());

            SendGenericMessage("RexFlashAnim", pack);
        }

        /// <summary>
        /// Sends preload avatar assets
        /// </summary>
        /// <param name="assetList">List of avatar assets</param>
        public void SendRexPreloadAvatarAssets(List<string> assetList)
        {
            try
            {
                List<string> pack = new List<string>();

                foreach (string avatarUrl in assetList)
                {
                    pack.Add(avatarUrl);
                }

                SendGenericMessage("RexPreloadAppearance", pack);
            }
            catch (Exception exep)
            {
                m_log.Error("[REXCLIENT]: SendRexPreloadAvatarAssets fail:" + exep.ToString());
            }
        }

        /// <summary>
        /// Force Field Of View
        /// </summary>
        /// <param name="fov">Field of View in degrees. This parameter is irrelevant when disabling FOV</param>
        /// <param name="enable">True to enable, False to disable</param>
        public void SendRexForceFOV(float fov, bool enable)
        {
            List<string> pack = new List<string>();

            pack.Add(fov.ToString());
            pack.Add(enable.ToString());

            SendGenericMessage("RexForceFOV", pack);
        }

        /// <summary>
        /// Sends Forced Camera mode to client
        /// </summary>
        /// <param name="forceMode">1 = 1st person mode, 3 = 3rd person mode, 0 = no limits</param>
        /// <param name="minZoom">Minimum zoom (0.0-1.0)</param>
        /// <param name="maxZoom">Maximum zoom (0.0-1.0)</param>
        public void SendRexForceCamera(int forceMode, float minZoom, float maxZoom)
        {
            List<string> pack = new List<string>();

            pack.Add(forceMode.ToString());
            pack.Add(minZoom.ToString());
            pack.Add(maxZoom.ToString());

            SendGenericMessage("RexForceCamera", pack);
        }

        /// <summary>
        /// Sends the sky to user
        /// </summary>
        /// <param name="type">Type of the sky: 0 = none, 1 = skybox, 2 = skydome</param>
        /// <param name="images">List of image uuids - separated by space - to use for the sky.
        ///  Skyboxes need 6 images, skydomes take one image.
        ///  You can add suffix to the uuids to specify which side the texture should go to: 
        ///  _fr front, _lf left, _rt right, _bk back, _up up, _dn down </param>
        /// <param name="curvature">Curvature of the skydome. Values around 10.0 are good for
        ///  open spaces and landscapes. Not used with skyboxes</param>
        /// <param name="tiling">Skydome tiling. Not used with skyboxes.</param>
        public void SendRexSky(int type, string images, float curvature, float tiling)
        {
            List<string> pack = new List<string>();

            pack.Add(type.ToString());
            pack.Add(images);
            pack.Add(curvature.ToString());
            pack.Add(tiling.ToString());

            SendGenericMessage("RexSky", pack);
        }

        /// <summary>
        /// Sends list of preloaded assets to user
        /// </summary>
        /// <param name="assetList"></param>
        public void SendRexPreloadAssets(Dictionary<UUID, uint> assetList)
        {
            try
            {
                List<string> pack = new List<string>();

                string assetline = String.Empty;
                foreach (UUID materialUUID in assetList.Keys)
                {
                    assetline = assetList[materialUUID] + " " + materialUUID.ToString();
                    pack.Add(assetline);
                }

                SendGenericMessage("RexPreloadAssets", pack);
            }
            catch (Exception exep)
            {
                m_log.Error("[REXCLIENT]: SendRexPreloadAssets fail:" + exep.ToString());
            }
        }

        /// <summary>
        /// Sends MediaURL to client
        /// </summary>
        /// <param name="assetId">UUID of the asset which to replace with MediaURL content</param>
        /// <param name="mediaURL">URL pointing to web-page or vnc server</param>
        /// <param name="refreshRate">How many times per second to refresh the texture</param>
        public void SendMediaURL(UUID assetId, string mediaURL, byte refreshRate)
        {
            if (mediaURL == null)
            {
                m_log.Warn("[REXCLIENT]: Did not send media url to user, because it was null");
                return;
            }
            List<string> pack = new List<string>();

            pack.Add(assetId.ToString());
            pack.Add(mediaURL);
            pack.Add(refreshRate.ToString());

            SendGenericMessage("RexMediaUrl", pack);
        }

        public void RexIKSendLimbTarget(UUID agentID, int limbId, Vector3 destination, float timeToTarget,
            float stayTime, float constraintAngle, string startAnim, string targetAnim, string endAnim)
        {
            List<string> pack = new List<string>();

            pack.Add("0");
            pack.Add(agentID.ToString());
            pack.Add(limbId.ToString());
            string sDest = destination.X.ToString() + " " + destination.Y.ToString() + " " + destination.Z.ToString();
            sDest = sDest.Replace(",", ".");
            pack.Add(sDest);
            pack.Add(timeToTarget.ToString());
            pack.Add(stayTime.ToString());
            pack.Add(constraintAngle.ToString());
            pack.Add(startAnim);
            pack.Add(targetAnim);
            pack.Add(endAnim);

            SendGenericMessage("RexIK", pack);
        }

        public void SendRexAvatarAnimation(UUID agentID, string animName, float rate, float fadeIn, 
            float fadeOut, int repeats, bool stopAnim)
        {
            List<string> pack = new List<string>();

            pack.Add(agentID.ToString());
            pack.Add(animName);
            pack.Add(rate.ToString());
            pack.Add(fadeIn.ToString().Replace(",","."));
            pack.Add(fadeOut.ToString().Replace(",", "."));
            pack.Add(repeats.ToString());
            pack.Add(stopAnim.ToString());

            SendGenericMessage("RexAnim", pack);
        }

        public void SendRexAvatarMorph(UUID agentID, string morphName, float weight, float time)
        {
            List<string> pack = new List<string>();

            pack.Add(agentID.ToString());
            pack.Add(morphName);
            pack.Add(weight.ToString());
            pack.Add(time.ToString());

            SendGenericMessage("RexMorph", pack);
        }

        /// <summary>
        /// Send Mesh Animation command to client
        /// </summary>
        /// <param name="primId">id of the primitive</param>
        /// <param name="animationName">Name of the animation to launch</param>
        /// <param name="rate">Speed of the animation, where 1.0 is default speed</param>
        /// <param name="loop">True to make the animation looped, false to play it only once</param>
        /// <param name="stopAnimation">True to stop the animation, false to launch the animation</param>
        public void SendRexMeshAnimation(UUID primId, string animationName, float rate, bool loop, bool stopAnimation)
        {
            List<string> pack = new List<string>();

            pack.Add(primId.ToString());
            pack.Add(animationName);
            pack.Add(rate.ToString());
            pack.Add(loop.ToString());
            pack.Add(stopAnimation.ToString());

            SendGenericMessage("RexPrimAnim", pack);
        }

        /// <summary>
        /// Send Client side effect to client
        /// </summary>
        /// <param name="assetId">Id of the asset</param>
        /// <param name="timeUntilLaunch">Time in seconds until the effect is launched on the client.
        ///  Set to zero to launch immediatelly</param>
        /// <param name="timeUntilDeath">The duration of the effect. The particle system gets completely 
        ///  destroyed after this duration. The duration is counted after the effect is launched. </param>
        /// <param name="pos">Position of the effect in the world</param>
        /// <param name="rot">Rotation of the particle system. Affects it's movement if speed > 0</param>
        /// <param name="speed">Speed at which the particle system moves. The system moves at the direction
        ///  specified by rot. Set to zero to make it stationary.</param>
        public void SendRexClientSideEffect(string assetId, float timeUntilLaunch, float timeUntilDeath, Vector3 pos, Quaternion rot, float speed)
        {
            List<string> pack = new List<string>();

            pack.Add(assetId.ToString());
            pack.Add(timeUntilLaunch.ToString().Replace(",", "."));
            pack.Add(timeUntilDeath.ToString().Replace(",", "."));

            string sPos = pos.X.ToString() + " " + pos.Y.ToString() + " " + pos.Z.ToString();
            sPos = sPos.Replace(",", ".");
            pack.Add(sPos);

            string sRot = rot.X.ToString() + " " + rot.Y.ToString() + " " + rot.Z.ToString() + " " + rot.W.ToString();
            sRot = sRot.Replace(",", ".");
            pack.Add(sRot);

            pack.Add(speed.ToString().Replace(",", "."));

            SendGenericMessage("RexCSEffect", pack);
        }

        public override void InformClientOfNeighbour(ulong neighbourHandle, IPEndPoint neighbourExternalEndPoint)
        {
            IRexUDPPort module = m_scene.RequestModuleInterface<IRexUDPPort>();
            if (module != null)
            {
                int udpport = module.GetPort(neighbourHandle);

                m_log.DebugFormat("[REXCLIENT]: Informing Client About Neighbour {0}", neighbourExternalEndPoint);
                base.InformClientOfNeighbour(neighbourHandle, new IPEndPoint(neighbourExternalEndPoint.Address, udpport));
            }
            else
            {
                base.InformClientOfNeighbour(neighbourHandle, neighbourExternalEndPoint);
            }
        }

        public override void CrossRegion(ulong newRegionHandle, Vector3 pos, Vector3 lookAt, IPEndPoint externalIPEndPoint,
                                string capsURL)
        {
            IRexUDPPort module = m_scene.RequestModuleInterface<IRexUDPPort>();
            if (module != null)
            {
                int udpport = module.GetPort(newRegionHandle);

                m_log.DebugFormat("[REXCLIENT]: Crossing client to region {0}", externalIPEndPoint);
                base.CrossRegion(newRegionHandle, pos, lookAt, new IPEndPoint(externalIPEndPoint.Address, udpport), capsURL);
            }
            else
            {
                base.CrossRegion(newRegionHandle, pos, lookAt, externalIPEndPoint, capsURL);
            }
        }

        public override void SendRegionTeleport(ulong regionHandle, byte simAccess, IPEndPoint newRegionEndPoint, uint locationID,
                                       uint flags, string capsURL)
        {
            IRexUDPPort module = m_scene.RequestModuleInterface<IRexUDPPort>();
            if (module != null)
            {
                int udpport = module.GetPort(regionHandle);

                m_log.DebugFormat("[REXCLIENT]: Sending region teleport to client {0}", newRegionEndPoint);
                base.SendRegionTeleport(regionHandle, simAccess, new IPEndPoint(newRegionEndPoint.Address, udpport), locationID, flags, capsURL);
            }
            else
            {
                base.SendRegionTeleport(regionHandle, simAccess, newRegionEndPoint, locationID, flags, capsURL);
            }
        }

        #region Avatar terse update

        protected override void InitNewClient()
        {
            m_avatarTerseUpdateTimer = new System.Timers.Timer(m_avatarTerseUpdateRate);
            m_avatarTerseUpdateTimer.Elapsed += new ElapsedEventHandler(ProcessAvatarTerseUpdates);
            m_avatarTerseUpdateTimer.AutoReset = false;

            base.InitNewClient();
        }

        private System.Timers.Timer m_avatarTerseUpdateTimer;
        private List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock> m_avatarTerseUpdates = new List<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>();

        /// <summary>
        /// Send a terse positional/rotation/velocity update about an avatar
        /// to the client.  This avatar can be that of the client itself.
        /// </summary>
        public override void SendAvatarTerseUpdate(ulong regionHandle,
                ushort timeDilation, uint localID, Vector3 position,
                Vector3 velocity, Quaternion rotation, UUID agentid)
        {
            if (rotation.X == rotation.Y &&
                rotation.Y == rotation.Z &&
                rotation.Z == rotation.W && rotation.W == 0)
                rotation = Quaternion.Identity;

            position.Z = (float)(position.Z - 0.15);

            ImprovedTerseObjectUpdatePacket.ObjectDataBlock terseBlock =
                RexCreateAvatarImprovedBlock(localID, position, velocity, rotation);
                //CreateAvatarImprovedBlock(localID, position, velocity, rotation);

            lock (m_avatarTerseUpdates)
            {
                m_avatarTerseUpdates.Add(terseBlock);

                // If packet is full or own movement packet, send it.
                if (m_avatarTerseUpdates.Count >= m_avatarTerseUpdatesPerPacket)
                {
                    ProcessAvatarTerseUpdates(this, null);
                }
                else if (m_avatarTerseUpdates.Count == 1)
                {
                    m_avatarTerseUpdateTimer.Start();
                }
            }
        }

        private void ProcessAvatarTerseUpdates(object sender, ElapsedEventArgs e)
        {
            lock (m_avatarTerseUpdates)
            {
                ImprovedTerseObjectUpdatePacket terse = (ImprovedTerseObjectUpdatePacket)PacketPool.Instance.GetPacket(PacketType.ImprovedTerseObjectUpdate);

                terse.RegionData = new ImprovedTerseObjectUpdatePacket.RegionDataBlock();

                terse.RegionData.RegionHandle = Scene.RegionInfo.RegionHandle;
                terse.RegionData.TimeDilation =
                        (ushort)(Scene.TimeDilation * ushort.MaxValue);

                int max = m_avatarTerseUpdatesPerPacket;
                if (max > m_avatarTerseUpdates.Count)
                    max = m_avatarTerseUpdates.Count;

                int count = 0;
                int size = 0;

                byte[] zerobuffer = new byte[1024];
                byte[] blockbuffer = new byte[1024];

                for (count = 0; count < max; count++)
                {
                    int length = 0;
                    m_avatarTerseUpdates[count].ToBytes(blockbuffer, ref length);
                    length = Helpers.ZeroEncode(blockbuffer, length, zerobuffer);
                    if (size + length > m_packetMTU)
                        break;
                    size += length;
                }

                terse.ObjectData = new ImprovedTerseObjectUpdatePacket.ObjectDataBlock[count];

                for (int i = 0; i < count; i++)
                {
                    terse.ObjectData[i] = m_avatarTerseUpdates[0];
                    m_avatarTerseUpdates.RemoveAt(0);
                }

                terse.Header.Reliable = false;
                terse.Header.Zerocoded = true;
                OutPacket(terse, ThrottleOutPacketType.Task);

                if (m_avatarTerseUpdates.Count == 0)
                    m_avatarTerseUpdateTimer.Stop();
            }
        }

        /// <summary>
        /// Creates compressed avatar terse update block. This is about half smaller than the orginal SL
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="pos"></param>
        /// <param name="velocity"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        protected ImprovedTerseObjectUpdatePacket.ObjectDataBlock RexCreateAvatarImprovedBlock(uint localID, Vector3 pos,
                                                                                            Vector3 velocity,
                                                                                            Quaternion rotation)
        {
            byte[] bytes = new byte[30];
            int i = 0;
            ImprovedTerseObjectUpdatePacket.ObjectDataBlock dat = PacketPool.GetDataBlock<ImprovedTerseObjectUpdatePacket.ObjectDataBlock>();

            dat.TextureEntry = new byte[0];

            uint ID = localID;
            bytes[i++] = (byte)(ID % 256);
            bytes[i++] = (byte)((ID >> 8) % 256);
            bytes[i++] = (byte)((ID >> 16) % 256);
            bytes[i++] = (byte)((ID >> 24) % 256);

            // pos
            byte[] pb = pos.GetBytes();
            Array.Copy(pb, 0, bytes, i, pb.Length);
            i += 12;

            // velocity
            velocity = velocity / 128.0f;
            velocity.X += 1;
            velocity.Y += 1;
            velocity.Z += 1;

            ushort InternVelocityX = (ushort)(32768 * velocity.X);
            ushort InternVelocityY = (ushort)(32768 * velocity.Y);
            ushort InternVelocityZ = (ushort)(32768 * velocity.Z);
            bytes[i++] = (byte)(InternVelocityX % 256);
            bytes[i++] = (byte)((InternVelocityX >> 8) % 256);
            bytes[i++] = (byte)(InternVelocityY % 256);
            bytes[i++] = (byte)((InternVelocityY >> 8) % 256);
            bytes[i++] = (byte)(InternVelocityZ % 256);
            bytes[i++] = (byte)((InternVelocityZ >> 8) % 256);

            //rotation
            ushort rw = (ushort)(32768 * (rotation.W + 1));
            ushort rx = (ushort)(32768 * (rotation.X + 1));
            ushort ry = (ushort)(32768 * (rotation.Y + 1));
            ushort rz = (ushort)(32768 * (rotation.Z + 1));

            //rot
            bytes[i++] = (byte)(rx % 256);
            bytes[i++] = (byte)((rx >> 8) % 256);
            bytes[i++] = (byte)(ry % 256);
            bytes[i++] = (byte)((ry >> 8) % 256);
            bytes[i++] = (byte)(rz % 256);
            bytes[i++] = (byte)((rz >> 8) % 256);
            bytes[i++] = (byte)(rw % 256);
            bytes[i++] = (byte)((rw >> 8) % 256);

            dat.Data = bytes;
            return (dat);
        }
        #endregion
    }
}
