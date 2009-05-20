using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;

namespace ModularRex.RexParts.Modules
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
                            IClientRexAppearance rex;
                            if (avatar.ClientView.TryGet(out rex))
                            {
                                rex.SendRexAppearance(user, avatarServerURL);
                            }
                        });
            }
        }

        public void SendAllAppearancesToUser(RexClientViewBase target)
        {
            m_log.Info("[REXAPR] Sending all appearances to user " + target.AgentId + ".");
            List<UUID> sent = new List<UUID>();

            IClientAPI client;
            string avatarurl;

            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                    {
                        IClientRexAppearance rex;
                        if (avatar.ClientView.TryGet(out rex))
                        {
                            client = avatar.ControllingClient;
                            if (!sent.Contains(client.AgentId) && target != client)
                            {
                                avatarurl = rex.RexAvatarURLVisible;
                                if (!string.IsNullOrEmpty(avatarurl))
                                {
                                    target.SendRexAppearance(client.AgentId, avatarurl);
                                    sent.Add(client.AgentId);
                                }
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
                            RexClientViewBase rex;
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

        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore clientCore)
        {
            if(clientCore is IClientAPI)
            {
                IClientAPI client = (IClientAPI)clientCore;

                IClientRexAppearance rexClientAppearance;
                if (clientCore.TryGet(out rexClientAppearance))
                {
                    rexClientAppearance.OnRexAppearance += mcv_OnRexAppearance;
                    SendAppearanceToAllUsers(client.AgentId, rexClientAppearance.RexAvatarURLVisible);
                    if (client is RexClientViewBase)
                    {
                        SendAllAppearancesToUser((RexClientViewBase)client);
                    }
                }
            }
        }

        /// <summary>
        /// Fired when a "Neighbours: Update your appearance" packet is sent by the viewer
        /// </summary>
        /// <param name="sender">IClientApi of the sender</param>
        void mcv_OnRexAppearance(IClientAPI sender)
        {
            if (sender is IClientRexAppearance)
                SendAppearanceToAllUsers(sender.AgentId, ((IClientRexAppearance)sender).RexAvatarURLVisible);
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