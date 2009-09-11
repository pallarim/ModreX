using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;
using System.Net;
using Nini.Config;
using OpenSim.Framework;
using log4net;
using System.Reflection;

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

        public RexUDPServer() { }

        public RexUDPServer(IPAddress _listenIP, ref uint port, int proxyPortOffset, bool allow_alternate_port, IConfigSource configSource,
            AgentCircuitManager authenticateClass)
        {
            Init(_listenIP, ref port, proxyPortOffset, allow_alternate_port, configSource, authenticateClass);
        }

        protected override void CreatePacketServer(ClientStackUserSettings userSettings)
        {
            new RexPacketServer(this, userSettings);
        }

        protected void CreatePacketServer(ClientStackUserSettings userSettings, string clientToSpawn)
        {
            new RexPacketServer(this, userSettings, clientToSpawn);
        }

        protected void Init(IPAddress _listenIP, ref uint port, int proxyPortOffsetParm, bool allow_alternate_port, IConfigSource configSource,
            AgentCircuitManager circuitManager)
        {
            ClientStackUserSettings userSettings = new ClientStackUserSettings();

            IConfig config = configSource.Configs["ClientStack.LindenUDP"];

            if (config != null)
            {
                if (config.Contains("client_throttle_max_bps"))
                {
                    int maxBPS = config.GetInt("client_throttle_max_bps", 1500000);
                    userSettings.TotalThrottleSettings = new ThrottleSettings(0, maxBPS,
                    maxBPS > 28000 ? maxBPS : 28000);
                }

                if (config.Contains("client_throttle_multiplier"))
                    userSettings.ClientThrottleMultipler = config.GetFloat("client_throttle_multiplier");
                if (config.Contains("client_socket_rcvbuf_size"))
                    m_clientSocketReceiveBuffer = config.GetInt("client_socket_rcvbuf_size");
            }

            m_log.DebugFormat("[CLIENT]: client_throttle_multiplier = {0}", userSettings.ClientThrottleMultipler);
            m_log.DebugFormat("[CLIENT]: client_socket_rcvbuf_size  = {0}", (m_clientSocketReceiveBuffer != 0 ?
                                                                             m_clientSocketReceiveBuffer.ToString() : "OS default"));
            string clientToSpawn = "default";
            IConfig rexConfig = configSource.Configs["realXtend"];
            if (rexConfig != null)
            {
                if (rexConfig.Contains("ClientView"))
                {
                    clientToSpawn = rexConfig.Get("ClientView", "default");
                }
            }

            proxyPortOffset = proxyPortOffsetParm;
            listenPort = (uint)(port + proxyPortOffsetParm);
            listenIP = _listenIP;
            Allow_Alternate_Port = allow_alternate_port;
            m_circuitManager = circuitManager;
            CreatePacketServer(userSettings, clientToSpawn);

            // Return new port
            // This because in Grid mode it is not really important what port the region listens to as long as it is correctly registered.
            // So the option allow_alternate_ports="true" was added to default.xml
            port = (uint)(listenPort - proxyPortOffsetParm);
        }
    }
}
