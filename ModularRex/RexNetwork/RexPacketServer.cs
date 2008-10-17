using System.Net;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public class RexPacketServer : LLPacketServer 
    {
        public RexPacketServer(ILLClientStackNetworkHandler networkHandler, ClientStackUserSettings userSettings)
            : base(networkHandler, userSettings)
        {
        }

        protected override IClientAPI CreateNewCircuit(EndPoint remoteEP,
            UseCircuitCodePacket initialcirpack,
            ClientManager clientManager, IScene scene,
            AssetCache assetCache,
            LLPacketServer packServer, AgentCircuitManager authenSessions,
            OpenMetaverse.UUID agentId, OpenMetaverse.UUID sessionId,
            uint circuitCode, EndPoint proxyEP)
        {
            return
                new RexClientView(remoteEP, scene, assetCache, packServer,
                                  authenSessions, agentId, sessionId,
                                  circuitCode, proxyEP, new ClientStackUserSettings());
        }

        public override bool AddNewClient(EndPoint epSender, UseCircuitCodePacket useCircuit, AssetCache assetCache, AgentCircuitManager circuitManager, EndPoint proxyEP)
        {
            IClientAPI newuser;

            if (m_scene.ClientManager.TryGetClient(useCircuit.CircuitCode.Code, out newuser))
            {
                return false;
            }

            RexClientView rexuser;

            rexuser =
                (RexClientView) CreateNewCircuit(epSender, useCircuit, m_scene.ClientManager, m_scene, assetCache, this,
                                                 circuitManager, useCircuit.CircuitCode.ID,
                                                 useCircuit.CircuitCode.SessionID, useCircuit.CircuitCode.Code, proxyEP);

            m_scene.ClientManager.Add(useCircuit.CircuitCode.Code, rexuser);

            rexuser.OnViewerEffect += m_scene.ClientManager.ViewerEffectHandler;
            rexuser.OnLogout += LogoutHandler;
            rexuser.OnConnectionClosed += CloseClient;

            return true;
        }
    }
}
