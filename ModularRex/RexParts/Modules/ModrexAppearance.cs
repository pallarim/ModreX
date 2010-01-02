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

        public void SendAppearanceToAllUsers(UUID user, string avatarServerURL, bool overrideUsed)
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
                                rex.SendRexAppearance(user, avatarServerURL, overrideUsed);
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
                                    target.SendRexAppearance(client.AgentId, avatarurl, !string.IsNullOrEmpty(rex.RexAvatarURLOverride));
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
            scene.RegisterModuleInterface<ModrexAppearance>(this);

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
                    rexClientAppearance.OnRexSetAppearance += mcv_OnRexSetAppearance;
                    SendAppearanceToAllUsers(client.AgentId, rexClientAppearance.RexAvatarURLVisible, !string.IsNullOrEmpty(rexClientAppearance.RexAvatarURLOverride));
                    if (client is RexClientViewBase)
                    {
                        SendAllAppearancesToUser((RexClientViewBase)client);
                    }
                }

                IRexClientCore rexclientcore;
                if (clientCore.TryGet(out rexclientcore))
                    rexclientcore.OnRexStartUp += mcv_OnRexClientStartUp;
            }
        }

        /// <summary>
        /// Fired when a "Neighbours: Update your appearance" packet is sent by the viewer
        /// </summary>
        /// <param name="sender">IClientApi of the sender</param>
        void mcv_OnRexAppearance(IClientAPI sender)
        {
            if (sender is IClientRexAppearance)
                SendAppearanceToAllUsers(sender.AgentId, ((IClientRexAppearance)sender).RexAvatarURLVisible, !string.IsNullOrEmpty(((IClientRexAppearance)sender).RexAvatarURLOverride));
        }

        /// <summary>
        /// Fired when a viewer sends a packet telling new appearance
        /// </summary>
        /// <param name="sender">IClientApi of the sender</param>
        /// <param name="agentID">Agent ID of the sender</param>
        /// <param name="args">Generic message arguments</param>
        void mcv_OnRexSetAppearance(IClientAPI sender, UUID agentID, List<string> args)
        {
            try
            {        
                if (args.Count >= 2)
                {
                    if (sender is IClientRexAppearance)
                    {
                        // Check that agent id matches
                        UUID id;
                        UUID.TryParse(args[0], out id);
                        if (id != agentID)
                        {
                            m_log.Info("[REXAPPEAR] RexSetAppearance with non-matching agent id");
                            return;
                        }       
                        
                        IClientRexAppearance rexClientAppearance = (IClientRexAppearance)sender;
                        // This should trigger OnRexAppearance: replication of new avatar URL to everyone
                        m_log.Info("[REXAPPEAR] Setting new avatar address " + args[1]);
                        rexClientAppearance.RexAvatarURL = args[1];
                    }                                                           
                }            
            }
            catch (Exception ex)
            {
                m_log.Info("[REXAPPEAR] Exception in RexSetAppearance" + ex.ToString());
                return;
            }
        }


        /// <summary>
        /// Fired when a client is started (rendering world after loading menu)
        /// </summary>
        void mcv_OnRexClientStartUp(IRexClientCore remoteClient, UUID agentID, string status)
        {
            if (status.ToLower() == "started" && remoteClient is IClientRexAppearance)
                SendAppearanceToAllUsers(remoteClient.AgentId, ((IClientRexAppearance)remoteClient).RexAvatarURLVisible, !string.IsNullOrEmpty(((IClientRexAppearance)remoteClient).RexAvatarURLOverride));
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