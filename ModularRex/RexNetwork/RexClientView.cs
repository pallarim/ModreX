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

namespace ModularRex.RexNetwork
{
    public delegate void RexAppearanceDelegate(RexClientView sender);

    public delegate void RexFaceExpressionDelegate(RexClientView sender, List<string> parameters);

    public delegate void RexAvatarProperties(RexClientView sender, List<string> parameters);

    public delegate void RexRecieveObjectPropertiesDelegate(RexClientView sender, UUID id, RexObjectProperties props);

    public delegate void ReceiveRexStartUp(RexClientView remoteClient, UUID agentID, string status);

    public delegate void ReceiveRexClientScriptCmd(RexClientView remoteClient, UUID agentID, List<string> parameters);

    /// <summary>
    /// Inherits from LLClientView the majority of functionality
    /// Overrides and extends for Rex-specific functionality.
    /// 
    /// In the case whereby functionality uses the same packets but differs
    /// between Rex and LL, you can use a override on those specific functions
    /// to overload the request.
    /// </summary>
    public class RexClientView : LLClientView, IClientRexFaceExpression, IClientRexAppearance
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_rexAccountID;
        private string m_rexAvatarURL;
        private string m_rexAuthURL;
        private string m_rexSkypeURL;
        public string AvatarStorageOverride;

        public float RexCharacterSpeedMod = 1.0f;
        public float RexMovementSpeedMod = 1.0f;
        public float RexVertMovementSpeedMod = 1.0f; 
        public bool RexWalkDisabled = false;
        public bool RexFlyDisabled = false;
        public bool RexSitDisabled = false;

        public event RexAppearanceDelegate OnRexAppearance;
        public event RexFaceExpressionDelegate OnRexFaceExpression;
        public event RexAvatarProperties OnRexAvatarProperties;
        public event RexRecieveObjectPropertiesDelegate OnRexObjectProperties;
        public event ReceiveRexStartUp OnReceiveRexStartUp;
        public event ReceiveRexClientScriptCmd OnReceiveRexClientScriptCmd;

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            // Rex communication now occurs via GenericMessage
            // We have a special handler here below.


            AddGenericPacketHandler("RexAppearance", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexFaceExpression", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexAvatarProp", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexPrimData", RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexData", RealXtendClientView_OnGenericMessage);

            OnBinaryGenericMessage += RexClientView_BinaryGenericMessage;
            OnGenericMessage += RealXtendClientView_OnGenericMessage;
        }

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, string rexAvatarURL, string rexAuthURL, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            // Rex communication now occurs via GenericMessage
            // We need to register GenericMessage handlers

            AddGenericPacketHandler("RexAppearance",        RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexFaceExpression",    RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexAvatarProp",        RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexPrimData",          RealXtendClientView_OnGenericMessage);
            AddGenericPacketHandler("RexData",              RealXtendClientView_OnGenericMessage);

            OnBinaryGenericMessage += RexClientView_BinaryGenericMessage;

            RexAvatarURL = rexAvatarURL;
            RexAuthURL = rexAuthURL;
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

            // Register our own class 'as-is' so it can be
            // used via IClientCore.Get<RexClientView>()...
            RegisterInterface(this);

            base.RegisterInterfaces();
        }

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
        /// Skype username of the avatar
        /// eg: Skypeuser
        /// </summary>
        public string RexSkypeURL
        {
            get { return m_rexSkypeURL; }
            set { m_rexSkypeURL = value; }
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
                
                // Request Agent Properties Asynchronously
                ThreadPool.QueueUserWorkItem(RequestProperties);
            }
        }

        void RexClientView_BinaryGenericMessage(Object sender, string method, byte[][] args)
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
                if(!first)
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
                if(!first)
                {
                    first = true;
                    continue;
                }

                arg.CopyTo(rpdArray,idx);
                idx += arg.Length;
            }

            if (OnRexObjectProperties != null)
                OnRexObjectProperties(this, id, new RexObjectProperties(rpdArray));
        }

        /// <summary>
        /// Special - used to convert GenericMessage packets
        /// to their appropriate Rex equivilents.
        /// 
        /// Eg: GenericMessage(RexAppearance) ->
        ///     OnRexAppearance(...)
        /// </summary>
        void RealXtendClientView_OnGenericMessage(object sender, string method, List<string> args)
        {
            //TODO: Convert to Dictionary<Method, GenericMessageHandler>

            if (method == "RexAppearance")
                if (OnRexAppearance != null)
                {
                    OnRexAppearance(this);
                    return;
                }

            if (method == "RexFaceExpression")
            {
                if (OnRexFaceExpression != null)
                {
                    OnRexFaceExpression(this, args);
                    return;
                }
            }

            if (method == "RexAvatarProp")
            {
                if(OnRexAvatarProperties != null)
                {
                    OnRexAvatarProperties(this, args);
                    return;
                }
            }

            if(method == "RexData")
            {
                
            }

            if (method == "rexscr")
            {
                if (OnReceiveRexClientScriptCmd != null)
                {
                    OnReceiveRexClientScriptCmd(this, AgentId, args);
                    return;
                }
            }

            if (method == "RexStartup")
            {
                if (OnReceiveRexStartUp != null)
                {
                    OnReceiveRexStartUp(this, AgentId, args[0]);
                    return;
                }
            }

            m_log.Warn("[REXCLIENTVIEW] Unhandled GenericMessage (" + method + ") {");
            foreach (string s in args)
            {
                m_log.Warn("\t" + s);
            }
            m_log.Warn("}");

        }

        public void SendRexObjectProperties(UUID id, RexObjectProperties x)
        {
            GenericMessagePacket gmp = new GenericMessagePacket();
            gmp.MethodData.Method = Utils.StringToBytes("RexPrimData");

            byte[] temprexprimdata = x.GetRexPrimDataToBytes();
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

        public void SendRexAppearance(UUID agentID, string avatarURL)
        {
            List<string> pack = new List<string>();
            pack.Add(avatarURL);
            pack.Add(agentID.ToString());

            SendGenericMessage("RexAppearance", pack);
        }

        /// <summary>
        /// Requests properties about this agent from their 
        /// authentication server. This should be run in
        /// an async thread.
        /// 
        /// Note that this particular function may set the
        /// avatars appearance which may in turn call 
        /// additional modules and functions elsewhere.
        /// </summary>
        /// <param name="o"></param>
        private void RequestProperties(object o)
        {
            m_log.Info("[REXCLIENT] Resolving avatar...");
            Hashtable ReqVals = new Hashtable();
            ReqVals["avatar_account"] = RexAccount;
            ReqVals["AuthenticationAddress"] = RexAuthURL;

            ArrayList SendParams = new ArrayList();
            SendParams.Add(ReqVals);

            XmlRpcRequest req = new XmlRpcRequest("get_user_by_account", SendParams);

            m_log.Info("[REXCLIENT] Sending XMLRPC Request to http://" + RexAuthURL);

            XmlRpcResponse authreply = req.Send("http://" + RexAuthURL, 9000);

            //m_log.Info(authreply.ToString());
            if (!((Hashtable)authreply.Value).ContainsKey("error_type"))
            {
            string rexAsAddress = ((Hashtable)authreply.Value)["as_address"].ToString();
            //string rexSkypeURL = ((Hashtable)authreply.Value)["skype_url"].ToString(); 
            UUID userID = new UUID(((Hashtable) authreply.Value)["uuid"].ToString());

                // Sanity check
                if (userID == AgentId)
                {
                    RexAvatarURL = rexAsAddress;
                    //RexSkypeURL = rexSkypeURL;
                }
            }
            else
            {
                m_log.Warn("[REXCLIENT]: User not found");
            }
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
            List<string> pack = new List<string>();

            pack.Add(command.ToString());
            pack.Add(name);
            pack.Add(assetId.ToString());
            pack.Add(pos.ToString());
            pack.Add(lookat.ToString());
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

            pack.Add(direction.ToString());
            pack.Add(colour.ToString());
            pack.Add(ambientColour.ToString());

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

        internal void SendRexPreloadAvatarAssets(List<string> vAssetsList)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexForceFOV(float fov, bool enable)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexForceCamera(int forceMode, float minZoom, float maxZoom)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexSky(int type, string images, float curvature, float tiling)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexPreloadAssets(Dictionary<UUID, uint> tempassetlist)
        {
            throw new System.NotImplementedException();
        }

        internal void SendMediaURL(UUID assetId, string mediaURL, byte vRefreshRate)
        {
            throw new System.NotImplementedException();
        }

        internal void RexIKSendLimbTarget(UUID vAgentID, int vLimbId, Vector3 vDest, float vTimeToTarget, float vStayTime, float vConstraintAngle, string vStartAnim, string vTargetAnim, string vEndAnim)
        {
            throw new System.NotImplementedException();
        }

        public void SendRexAvatarAnimation(UUID agentID, string vAnimName, float vRate, float vFadeIn, float vFadeOut, int nRepeats, bool vbStopAnim) //rex
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexAvatarMorph(UUID uUID, string vMorphName, float vWeight, float vTime)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexMeshAnimation(UUID uUID, string vAnimName, float vRate, bool vbLooped, bool vbStopAnim)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexClientSideEffect(string assetId, float vTimeUntilLaunch, float vTimeUntilDeath, Vector3 pos, Quaternion rot, float vSpeed)
        {
            throw new System.NotImplementedException();
        }
    }
}
