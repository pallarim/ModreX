using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public delegate void RexAppearanceDelegate(RexClientView sender);

    public delegate void RexFaceExpressionDelegate(RexClientView sender, List<string> parameters);

    public delegate void RexAvatarProperties(RexClientView sender, List<string> parameters);

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
            OnGenericMessage += RealXtendClientView_OnGenericMessage;
        }

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, string rexAvatarURL, string rexAuthURL, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            // Rex communication now occurs via GenericMessage
            // We have a special handler here below.
            OnGenericMessage += RealXtendClientView_OnGenericMessage;

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
            RegisterInterface<RexClientView>(this);

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
                if (value.Contains("@"))
                {
                    m_rexAuthURL = "http://" + value.Split('@')[1];
                }
                else
                {
                    m_rexAuthURL = value;
                }
                // Request Agent Properties Asynchronously
                ThreadPool.QueueUserWorkItem(RequestProperties);
            }
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

            if (method == "rexscr")
            {
                if (OnReceiveRexClientScriptCmd != null)
                {
                    OnReceiveRexClientScriptCmd(this, AgentId, args);
                }
            }

            if (method == "RexStartup")
            {
                if (OnReceiveRexStartUp != null)
                {
                    OnReceiveRexStartUp(this, AgentId, args[0]);
                }
            }

            m_log.Warn("[REXCLIENTVIEW] Unhandled GenericMessage (" + method + ") {");
            foreach (string s in args)
            {
                m_log.Warn("\t" + s);
            }
            m_log.Warn("}");

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
            ReqVals["avatar_uuid"] = AgentId.ToString();
            ReqVals["AuthenticationAddress"] = RexAuthURL;

            ArrayList SendParams = new ArrayList();
            SendParams.Add(ReqVals);

            XmlRpcRequest req = new XmlRpcRequest("get_user_by_uuid", SendParams);

            m_log.Info("[REXCLIENT] Sending XMLRPC Request to " + RexAuthURL);
            XmlRpcResponse authreply = req.Send(RexAuthURL, 9000);
            if (!((Hashtable)authreply.Value).ContainsKey("error_type"))
            {
                string rexAsAddress = ((Hashtable)authreply.Value)["as_address"].ToString();
                string rexSkypeURL = ((Hashtable)authreply.Value)["skype_url"].ToString();
                UUID userID = new UUID(((Hashtable)authreply.Value)["uuid"].ToString());

                // Sanity check
                if (userID == AgentId)
                {
                    RexAvatarURL = rexAsAddress;
                    RexSkypeURL = rexSkypeURL;
                }
            }
            else
            {
                //Error has occurred
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

        internal void SendRexPostProcess(int vEffectId, bool vbToggle)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexRttCamera(int command, string name, UUID uUID, Vector3 pos, Vector3 lookat, int width, int height)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexViewport(int command, string name, float vX, float vY, float vWidth, float vHeight)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexToggleWindSound(bool vbToggle)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexCameraClientSideEffect(bool enable, UUID uUID, Vector3 pos, Quaternion rot)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexSetAmbientLight(Vector3 lightDir, Vector3 lightC, Vector3 ambientC)
        {
            throw new System.NotImplementedException();
        }

        internal void SendRexPlayFlashAnimation(UUID uUID, float left, float top, float right, float bottom, float timeToDeath)
        {
            throw new System.NotImplementedException();
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