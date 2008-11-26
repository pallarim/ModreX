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

    public delegate void RexFaceExpressionDelegate(RexClientView sender, List<string> vParams);

    public delegate void RexAvatarProperties(RexClientView sender, List<string> parameters);

    public class RexClientView : LLClientView, IClientRexFaceExpression, IClientRexAppearance
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_rexAccountID;
        private string m_rexAvatarURL;
        private string m_rexAuthURL;
        private string m_rexSkypeURL;

        public event RexAppearanceDelegate OnRexAppearance;
        public event RexFaceExpressionDelegate OnRexFaceExpression;
        public event RexAvatarProperties OnRexAvatarProperties;

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            OnGenericMessage += RealXtendClientView_OnGenericMessage;
        }

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, string rexAvatarURL, string rexAuthURL, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            OnGenericMessage += RealXtendClientView_OnGenericMessage;

            RexAvatarURL = rexAvatarURL;
            RexAuthURL = rexAuthURL;
        }

        protected override void RegisterInterfaces()
        {
            RegisterInterface<IClientRexAppearance>(this);
            RegisterInterface<IClientRexFaceExpression>(this);
            RegisterInterface<RexClientView>(this);

            base.RegisterInterfaces();
        }

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

        public string RexSkypeURL
        {
            get { return m_rexSkypeURL; }
            set { m_rexSkypeURL = value; }
        }

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

            m_log.Warn("[REXCLIENTVIEW] Unhandled GenericMessage (" + method + ") {");
            foreach (string s in args)
            {
                m_log.Warn("\t" + s);
            }
            m_log.Warn("}");

        }

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
            string rexAsAddress = ((Hashtable)authreply.Value)["as_address"].ToString();
            string rexSkypeURL = ((Hashtable)authreply.Value)["skype_url"].ToString();
            UUID userID = new UUID(((Hashtable) authreply.Value)["uuid"].ToString());

            // Sanity check
            if (userID == AgentId)
            {
                RexAvatarURL = rexAsAddress;
                RexSkypeURL = rexSkypeURL;
            }
        }
    }
}