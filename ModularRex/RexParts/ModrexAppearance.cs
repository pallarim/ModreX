using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts
{
    public class ModrexAppearance : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<Scene> m_scenes = new List<Scene>();

        public void SendAppearanceToAllUsers(UUID user, string avatarServerURL)
        {
            m_log.Info("[REXAPR] Sending user " + user + " appearance to all users. [" + avatarServerURL + "]");
            // Ignore empty avatars
            if (String.IsNullOrEmpty(avatarServerURL))
            {
                m_log.Info("[REXAPR] Skipping blank URL on user...");
                return;
            }

            // Send to every agent in every scene
            // We may want to target this more cleanly
            // in future.
            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                        {
                            if (avatar.ControllingClient is RexClientView)
                            {
                                ((RexClientView) avatar.ControllingClient).SendRexAppearance(
                                    user, avatarServerURL);
                            }
                        });
            }
        }

        public void SendAllAppearancesToUser(RexClientView target)
        {
            m_log.Info("[REXAPR] Sending all appearances to user " + target.AgentId + ".");
            List<UUID> sent = new List<UUID>();

            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                        {
                            if (avatar.ControllingClient is RexClientView &&
                                !sent.Contains(avatar.ControllingClient.AgentId) &&
                                avatar.ControllingClient != target &&
                                !string.IsNullOrEmpty(
                                     ((RexClientView) avatar.ControllingClient)
                                         .RexAvatarURL))
                            {
                                target.SendRexAppearance(avatar.ControllingClient.AgentId,
                                                         ((RexClientView) avatar.ControllingClient)
                                                             .RexAvatarURL);
                                sent.Add(avatar.ControllingClient.AgentId);
                            }
                        });
            }            
        }

        public void SendAllAppearancesToAllUsers()
        {
            m_log.Info("[REXAPR] Sending all appearances to all users.");
            List<UUID> sent = new List<UUID>();

            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                        {
                            if (avatar.ControllingClient is RexClientView &&
                                !sent.Contains(avatar.ControllingClient.AgentId))
                            {
                                sent.Add(avatar.ControllingClient.AgentId);
                                SendAllAppearancesToUser((RexClientView) avatar.ControllingClient);
                            }
                        });
            }
        }


        public void Initialise(Scene scene, IConfigSource source)
        {
            try
            {
                if(!source.Configs["realXtend"].GetBoolean("enabled",false))
                {
                    return;
                }

                m_log.Info("RexAppearance Module Being Used");
            }
            catch (Exception)
            {
                m_log.Info("Rex Config Error, Disabled");
                return;
            }

            m_log.Info("[REXAPPEAR] Added Scene");
            m_scenes.Add(scene);

            scene.EventManager.OnNewClient += EventManager_OnNewClient;
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            // Check if the client was insubstantiated as a RexClientView.
            if(client is RexClientView)
            {
                RexClientView mcv = (RexClientView) client;

                mcv.OnRexAppearance += mcv_OnRexAppearance;

                // Send initial appearance to others
                SendAppearanceToAllUsers(mcv.AgentId, mcv.RexAvatarURL);
                // Send others appearance to us
                SendAllAppearancesToUser((RexClientView) client);
            }
        }

        /// <summary>
        /// Fired when a "Neighbours: Update your appearance" packet is sent by the viewer
        /// </summary>
        /// <param name="sender">IClientApi of the sender</param>
        void mcv_OnRexAppearance(RexClientView sender)
        {
            SendAppearanceToAllUsers(sender.AgentId, sender.RexAvatarURL);
        }

        public void PostInitialise()
        {
            m_log.Info("[REXAPPEAR] PostInit called");
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RexAppearance"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}