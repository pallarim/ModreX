using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.CoreModules.Framework.EventQueue;
//using BlockingLLSDQueue = OpenSim.Framework.BlockingQueue<OpenMetaverse.StructuredData.OSD>;
using Caps = OpenSim.Framework.Capabilities.Caps;

namespace ModularRex.RexNetwork
{
    
    /// <summary>
    /// This module is intended to replace EventQueueGetModule. This module enables multiregion and grid support.
    /// 
    /// To disable that module set "EventQueue = false" to OpenSim.ini under [Startup]
    /// </summary>
    public class RexEventQueue : EventQueueGetModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<UUID, Type> m_agent_type = new Dictionary<UUID, Type>();

        private RexLogin.IRexUDPPort rexUdpPortModule;

        #region IRegionModule Members

        public override void Close()
        {
            //TODO: deregister module interface
            m_scene.EventManager.OnClientClosed -= ClientClosed;
            m_scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
            m_scene.EventManager.OnNewClient -= OnNewClient;
        }

        public override void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;

            bool enable_now = ReadAndPopulateConfig(source);

            if (enable_now)
            {
                scene.RegisterModuleInterface<IEventQueue>(this);

                scene.EventManager.OnClientClosed += ClientClosed;
                scene.EventManager.OnRegisterCaps += OnRegisterCaps;

                scene.EventManager.OnNewClient += OnNewClient;
            }
            else
            {
                ;
            }
        }

        private void ClientClosed(UUID clientID, Scene scene)
        {
            m_agent_type.Remove(clientID);
        }

        private void OnNewClient(IClientAPI client)
        {
            if (client is RexClientViewBase)
            {
                m_agent_type.Add(client.AgentId, typeof(RexClientViewBase));
            }
            else
            {
                m_agent_type.Add(client.AgentId, typeof(IClientAPI));
            }
        }

        private bool ReadAndPopulateConfig(Nini.Config.IConfigSource source)
        {
            bool rex_conf = CheckRexConfig(source);
            if (CheckStartupConfig(source))
            {
                //Default event queue enabled or line missing
                if (rex_conf)
                {
                    m_log.Warn("[REXEVENTQUEUE]: Both default and Rex Event Queue enabled. Using default.");
                }
                return false;
            }
            else
            {
                //Default event queue disabled
                if (rex_conf)
                {
                    m_log.Info("[REXEVENTQUEUE]: Using Rex Event Queue");
                    return rex_conf;
                }
            }

            return false;
        }

        private bool CheckStartupConfig(Nini.Config.IConfigSource source)
        {
            if (source.Configs["Startup"] != null)
            {
                return source.Configs["Startup"].GetBoolean("EventQueue", true);
            }
            else
            {
                return true;
            }
        }

        private bool CheckRexConfig(Nini.Config.IConfigSource source)
        {
            if (source.Configs["realXtend"] != null)
            {
                if (source.Configs["realXtend"].GetBoolean("RexEventQueue", true))
                {
                    return true;
                }
            }
            return false;
        }

        public override string Name
        {
            get { return "RexEventQueue"; }
        }

        #endregion

        /// <summary>
        /// Checks if the user is RexClientView or not
        /// </summary>
        /// <param name="AgentId">UUID of the user to check</param>
        /// <returns>Returns false if not or if not found</returns>
        bool IsRexClient(UUID AgentId)
        {
            if (m_agent_type.ContainsKey(AgentId))
            {
                if (m_agent_type[AgentId] == typeof(RexClientViewBase))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        private IPEndPoint modifyIPEndPoint(IPEndPoint endPoint, ulong regionHandle)
        {
            if (rexUdpPortModule == null)
            {
                rexUdpPortModule = m_scene.RequestModuleInterface<RexLogin.IRexUDPPort>();
            }
            int port = rexUdpPortModule.GetPort(regionHandle);
            return new IPEndPoint(endPoint.Address, port);
        }

        #region IEventQueue Members

        //Can't be inherited
        public override void CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt, IPEndPoint newRegionExternalEndPoint, string capsURL, UUID avatarID, UUID sessionID)
        {
            IPEndPoint endpoint;
            if (IsRexClient(avatarID))
            {
                endpoint = modifyIPEndPoint(newRegionExternalEndPoint, handle);
            }
            else
                endpoint = newRegionExternalEndPoint;

            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, endpoint,
                                                        capsURL, avatarID, sessionID);

            Enqueue(item, avatarID);
        }

        public override void EnableSimulator(ulong handle, System.Net.IPEndPoint endPoint, OpenMetaverse.UUID avatarID)
        {
            IPEndPoint newEndpoint;
            if (IsRexClient(avatarID))
            {
                newEndpoint = modifyIPEndPoint(endPoint, handle);
            }
            else
                newEndpoint = endPoint;

            OSD item = EventQueueHelper.EnableSimulator(handle, newEndpoint);
            Enqueue(item, avatarID);
        }

        public override void EstablishAgentCommunication(UUID avatarID, IPEndPoint endPoint, string capsPath)
        {
            IPEndPoint newEndpoint;
            if (IsRexClient(avatarID))
            {
                int port = rexUdpPortModule.GetPort(endPoint);
                newEndpoint = new IPEndPoint(endPoint.Address, port);
            }
            else
                newEndpoint = endPoint;

            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, newEndpoint.ToString(), capsPath);
            Enqueue(item, avatarID);
        }

        public override void TeleportFinishEvent(ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint, uint locationID, uint flags, string capsURL, OpenMetaverse.UUID agentID)
        {
            IPEndPoint newEndpoint;
            if (IsRexClient(agentID))
            {
                newEndpoint = modifyIPEndPoint(regionExternalEndPoint, regionHandle);
            }
            else
                newEndpoint = regionExternalEndPoint;

            OSD item = EventQueueHelper.TeleportFinishEvent(regionHandle, simAccess, newEndpoint,
                                                            locationID, flags, capsURL, agentID);
            Enqueue(item, agentID);
        }

        #endregion
    }
}
