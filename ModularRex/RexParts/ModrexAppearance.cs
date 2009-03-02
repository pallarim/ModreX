using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

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
                            RexClientView rex;
                            if (avatar.ClientView.TryGet(out rex))
                            {
                                rex.SendRexAppearance(user, avatarServerURL);
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
                            RexClientView rex;
                            if (avatar.ClientView.TryGet(out rex))
                            {
                                if (!sent.Contains(rex.AgentId) &&
                                    rex != target &&
                                    !string.IsNullOrEmpty(
                                         rex.RexAvatarURLVisible))
                                {
                                    target.SendRexAppearance(
                                        avatar.ControllingClient.AgentId,
                                        ((RexClientView) avatar.ControllingClient)
                                            .RexAvatarURLVisible);
                                    sent.Add(avatar.ControllingClient.AgentId);
                                }
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
                            RexClientView rex;
                            if (avatar.ClientView.TryGet(out rex))
                            {
                                if (!sent.Contains(avatar.ControllingClient.AgentId))
                                {
                                    sent.Add(rex.AgentId);
                                    SendAllAppearancesToUser(rex);
                                }
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

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;
        }

        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            RexClientView rex;
            if (client.TryGet(out rex))
            {
                rex.OnRexAppearance += mcv_OnRexAppearance;

                // Send initial appearance to others
                SendAppearanceToAllUsers(rex.AgentId, rex.RexAvatarURLVisible);
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
            SendAppearanceToAllUsers(sender.AgentId, sender.RexAvatarURLVisible);
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