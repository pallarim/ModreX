using System.Collections.Generic;
using System.Net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public delegate void RexAppearanceDelegate(RexClientView sender);

    public delegate void RexFaceExpressionDelegate(RexClientView sender, List<string> vParams);

    public class RexClientView : LLClientView 
    {
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

        void RealXtendClientView_OnGenericMessage(object sender, string method, List<string> args)
        {
            //TODO: Convert to Dictionary<Method, GenericMessageHandler>

            if (method == "RexAppearance")
                if (OnRexAppearance != null)
                {
                    OnRexAppearance(this);
                    return;
                }

            if(method == "RexFaceExpression")
            {
                if(OnRexFaceExpression != null)
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
    }
}