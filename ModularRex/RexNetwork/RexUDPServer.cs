using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;
using System.Net;
using Nini.Config;
using OpenSim.Framework;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Threading;
using System;
using OpenSim.Region.Framework.Scenes;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// Extends the standard OpenSim UDP Server Class
    /// With the only difference being that the packet
    /// server spawns RexClientView instances instead
    /// of LLClientView's.
    /// </summary>
    public class RexUDPServer : LLUDPServer 
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_clientToSpawn;

        public RexUDPServer(IPAddress _listenIP, ref uint port, int proxyPortOffset, bool allow_alternate_port, IConfigSource configSource,
            AgentCircuitManager authenticateClass) : base (_listenIP, ref port, proxyPortOffset, allow_alternate_port, configSource, authenticateClass)
        {
            Init(configSource);
        }

        protected void Init(IConfigSource configSource)
        {
            m_clientToSpawn = "default";
            IConfig rexConfig = configSource.Configs["realXtend"];
            if (rexConfig != null)
            {
                if (rexConfig.Contains("ClientView"))
                {
                    m_clientToSpawn = rexConfig.Get("ClientView", "default");
                }
            }

            //CreatePacketServer(userSettings, clientToSpawn);
        }

        protected override void AddClient(uint circuitCode, UUID agentID, UUID sessionID, IPEndPoint remoteEndPoint, AuthenticateResponse sessionInfo)
        {
            // Create the LLUDPClient
            LLUDPClient udpClient = new LLUDPClient(this, m_throttleRates, m_throttle, circuitCode, agentID, remoteEndPoint);
            IClientAPI existingClient;

            if (!m_scene.TryGetClient(agentID, out existingClient))
            {
                // Create the LLClientView
                LLClientView client = CreateNewClientView(remoteEndPoint, m_scene, this, udpClient, sessionInfo, agentID, sessionID, circuitCode);
                client.OnLogout += LogoutHandler;

                // Start the IClientAPI
                client.Start();
            }
            else
            {
                m_log.WarnFormat("[LLUDPSERVER]: Ignoring a repeated UseCircuitCode from {0} at {1} for circuit {2}",
                    udpClient.AgentID, remoteEndPoint, circuitCode);
            }
        }

        protected LLClientView CreateNewClientView(EndPoint remoteEP, Scene scene, LLUDPServer udpServer, LLUDPClient udpClient,
            AuthenticateResponse sessionInfo, OpenMetaverse.UUID agentId, OpenMetaverse.UUID sessionId, uint circuitCode)
        {
            switch (m_clientToSpawn.ToLower())
            {
                case "ng":
                case "naali":
                    return new NaaliClientView(remoteEP, scene, udpServer, udpClient,
                                  sessionInfo, agentId, sessionId, circuitCode);
                case "0.4":
                case "0.40":
                case "0.41":
                case "legacy":
                    return new RexClientViewLegacy(remoteEP, scene, udpServer, udpClient,
                                  sessionInfo, agentId, sessionId, circuitCode);
                case "default":
                case "compatible":
                default:
                    return new RexClientViewCompatible(remoteEP, scene, udpServer, udpClient,
                                  sessionInfo, agentId, sessionId, circuitCode);
            }
        }
    }
}
