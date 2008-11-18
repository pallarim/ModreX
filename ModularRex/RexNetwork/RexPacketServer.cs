using System.Net;
using System.Reflection;
using log4net;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public class RexPacketServer : LLPacketServer 
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public RexPacketServer(ILLClientStackNetworkHandler networkHandler, ClientStackUserSettings userSettings)
            : base(networkHandler, userSettings)
        {
        }

        protected override IClientAPI CreateNewCircuit(EndPoint remoteEP, 
            IScene scene, AssetCache assetCache, LLPacketServer packServer, 
            AuthenticateResponse sessionInfo, OpenMetaverse.UUID agentId, 
            OpenMetaverse.UUID sessionId, uint circuitCode, EndPoint proxyEP)
        {
            return
                new RexClientView(remoteEP, scene, assetCache, packServer,
                                  sessionInfo, agentId, sessionId,
                                  circuitCode, proxyEP, new ClientStackUserSettings());
        }

        public override bool AddNewClient(EndPoint epSender, UseCircuitCodePacket useCircuit, 
            AssetCache assetCache, AuthenticateResponse circuitManager, EndPoint proxyEP)
        {
            IClientAPI newuser;

            if (m_scene.ClientManager.TryGetClient(useCircuit.CircuitCode.Code, out newuser))
            {
                return false;
            }

            RexClientView rexuser;

            m_log.Debug("[REXCLIENT] Creating RexClient for user");

            rexuser = (RexClientView) CreateNewCircuit(epSender, m_scene, assetCache, this, circuitManager,
                                                       useCircuit.CircuitCode.ID, useCircuit.CircuitCode.SessionID,
                                                       useCircuit.CircuitCode.Code, proxyEP);

            m_scene.ClientManager.Add(useCircuit.CircuitCode.Code, rexuser);

            rexuser.OnViewerEffect += m_scene.ClientManager.ViewerEffectHandler;
            rexuser.OnLogout += LogoutHandler;
            rexuser.OnConnectionClosed += CloseClient;

            return true;
        }
    }
}
