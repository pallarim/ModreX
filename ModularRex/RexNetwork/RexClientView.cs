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
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public delegate void RexAppearanceDelegate(RexClientView sender);

    public delegate void RexFaceExpressionDelegate(RexClientView sender, List<string> vParams);

    public class RexClientView : LLClientView, IClientRexFaceExpression, IClientRexAppearance
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_rexAvatarURL;
        private string m_rexAuthURL;
        private string m_rexSkypeURL;

        public event RexAppearanceDelegate OnRexAppearance;
        public event RexFaceExpressionDelegate OnRexFaceExpression;

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AgentCircuitManager authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP)
        {
            OnGenericMessage += RealXtendClientView_OnGenericMessage;
        }

        public RexClientView(EndPoint remoteEP, IScene scene, AssetCache assetCache,
                             LLPacketServer packServer, AgentCircuitManager authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, string rexAvatarURL, string rexAuthURL)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP)
        {
            OnGenericMessage += RealXtendClientView_OnGenericMessage;

            RexAvatarURL = rexAvatarURL;
            RexAuthURL = rexAuthURL;
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
                }
            }
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