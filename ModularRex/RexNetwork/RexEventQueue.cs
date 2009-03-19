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
using Caps = OpenSim.Framework.Communications.Capabilities.Caps;

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

        private void ClientClosed(UUID clientID)
        {
            m_agent_type.Remove(clientID);
        }

        private void OnNewClient(IClientAPI client)
        {
            if (client is RexClientView)
            {
                m_agent_type.Add(client.AgentId, typeof(RexClientView));
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
                if (m_agent_type[AgentId] == typeof(RexClientView))
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

        string ModifyCAPS(string caps)
        {
            return caps;
            //Example CAPS construction
            //string capsPath = "http://" + reg.ExternalHostName + ":" + reg.HttpPort
            //      + "/CAPS/" + a.CapsPath + "0000/";
            //http://192.168.0.101:9000/CAPS/da43e83f-4e45-47de-bc7c-546150df0000/

            int start = caps.IndexOf(':', 6);
            int end = caps.IndexOf('/', start);

            string port = caps.Substring(start + 1, (end - start - 1));
            //m_log.InfoFormat("[REXEVENTQUEUE]: parse port {0}", port);
            int iPort = Convert.ToInt32(port);

            string newCaps = caps.Substring(0, start+1) + (iPort - 2000).ToString() + caps.Substring(end);
            m_log.InfoFormat("[REXEVENTQUEUE]: new caps {0}", newCaps);
            return newCaps;
        }

        #region IEventQueue Members

        //Can't be inherited
        public override void CrossRegion(ulong handle, OpenMetaverse.Vector3 pos, OpenMetaverse.Vector3 lookAt, System.Net.IPEndPoint newRegionExternalEndPoint, string capsURL, OpenMetaverse.UUID avatarID, OpenMetaverse.UUID sessionID)
        {
            IPEndPoint endpoint;
            if (IsRexClient(avatarID))
            {
                endpoint = new IPEndPoint(newRegionExternalEndPoint.Address, newRegionExternalEndPoint.Port - 2000);
                capsURL = ModifyCAPS(capsURL);
            }
            else
                endpoint = newRegionExternalEndPoint;

            OSD item = EventQueueHelper.CrossRegion(handle, pos, lookAt, endpoint,
                                                        capsURL, avatarID, sessionID);

            Enqueue(item, avatarID);
        }

        //Can't be inherited
        public override void EnableSimulator(ulong handle, System.Net.IPEndPoint endPoint, OpenMetaverse.UUID avatarID)
        {
            IPEndPoint newEndpoint;
            if (IsRexClient(avatarID))
            {
                newEndpoint = new IPEndPoint(endPoint.Address, endPoint.Port - 2000);
            }
            else
                newEndpoint = endPoint;

            OSD item = EventQueueHelper.EnableSimulator(handle, newEndpoint);
            Enqueue(item, avatarID);
        }

        //Can't be inherited
        public override void EstablishAgentCommunication(OpenMetaverse.UUID avatarID, System.Net.IPEndPoint endPoint, string capsPath)
        {
            IPEndPoint newEndpoint;
            if (IsRexClient(avatarID))
            {
                newEndpoint = new IPEndPoint(endPoint.Address, endPoint.Port - 2000);
                //endPoint.Port = endPoint.Port - 2000;
                capsPath = ModifyCAPS(capsPath);
            }
            else
                newEndpoint = endPoint;

            OSD item = EventQueueHelper.EstablishAgentCommunication(avatarID, newEndpoint.ToString(), capsPath);
            Enqueue(item, avatarID);
        }

        //Can't be inherited
        public override void TeleportFinishEvent(ulong regionHandle, byte simAccess, System.Net.IPEndPoint regionExternalEndPoint, uint locationID, uint flags, string capsURL, OpenMetaverse.UUID agentID)
        {
            IPEndPoint newEndpoint;
            if (IsRexClient(agentID))
            {
                newEndpoint = new IPEndPoint(regionExternalEndPoint.Address, regionExternalEndPoint.Port - 2000);
                //regionExternalEndPoint.Port = regionExternalEndPoint.Port - 2000;
                capsURL = ModifyCAPS(capsURL);
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
