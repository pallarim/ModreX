using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Scenes;
using ModularRex.RexNetwork;
using OpenSim.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using log4net;
using System.Reflection;

namespace ModularRex.RexParts.Modules
{
    public class RexSkypeModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "RexSkypeModule"; }
        }

        public void PostInitialise()
        {
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
        }

        #endregion

        #region Event Handlers

        private void EventManager_OnNewClient(IClientAPI client)
        {
            if (client is RexClientView)
            {
                RexClientView remoteClient = (RexClientView)client;

                //Subscribe to event to get notified when the users skypeurl changes
                remoteClient.OnReceiveRexSkypeStore += RexSkypeModule_OnReceiveRexSkypeStore;

                //Send all other users skypeaddress to this user
                SendAllOtherSkypeAddressesToClient(remoteClient);
            }
        }

        /// <summary>
        /// Skype url changed. Send the changed skypeurl to other users
        /// </summary>
        /// <param name="remoteClient"></param>
        private void RexSkypeModule_OnReceiveRexSkypeStore(RexClientView remoteClient)
        {
            try
            {
                SendSkypeToAllClients(remoteClient.RexSkypeURL, remoteClient.AgentId);
            }
            catch (Exception ex)
            {
                m_log.Error("[REXSKYPE]: ProcessRexSkypeStore threw an exception: " + ex.ToString());
            }  
        }

        #endregion

        /// <summary>
        /// Send skype address to all users
        /// </summary>
        /// <param name="skypeAddr">New skype address</param>
        /// <param name="agentID">User to whom the skype address belongs to</param>
        public void SendSkypeToAllClients(string skypeAddr, UUID agentID)
        {
            foreach (ScenePresence sp in m_scene.GetScenePresences())
            {
                if (sp.ControllingClient is RexClientView)
                {
                    ((RexClientView)sp.ControllingClient).SendSkypeAddress(agentID, skypeAddr);
                }
            }
        }

        /// <summary>
        /// Send all skype addresses to this client
        /// </summary>
        /// <param name="remoteClient">Client to send to</param>
        public void SendAllOtherSkypeAddressesToClient(RexClientView remoteClient)
        {
            foreach (ScenePresence sp in m_scene.GetScenePresences())
            {
                if (sp.ControllingClient is RexClientView)
                {
                    RexClientView client = ((RexClientView)sp.ControllingClient);
                    if (client.RexSkypeURL != null && client.RexSkypeURL != string.Empty)
                    {
                        remoteClient.SendSkypeAddress(client.AgentId, client.RexSkypeURL);
                    }
                }
            }
        }
    }
}
