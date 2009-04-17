
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using OpenMetaverse;
using System.Collections.Generic;
using log4net;
using System.Reflection;

namespace ModularRex.RexParts
{
    public interface ISitMod
    {
        void SetSitDisabled(UUID agentId, bool disabled);
        bool GetSitDisabled(UUID agentId);
    }


    public class SitModule : IRegionModule, ISitMod
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private Dictionary<UUID, bool> sitDisabled = new Dictionary<UUID, bool>();

        private bool default_sit_disabled = false;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<ISitMod>(this);
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "SitModule"; }
        }

        public void PostInitialise()
        {
            m_scene.EventManager.OnNewClient += HandleOnNewClient;
        }

        private void HandleOnNewClient(IClientAPI client)
        {
            //remove old sit methods so sit permission can be checked before allowing sitting
            ScenePresence sp = m_scene.GetScenePresence(client.AgentId);
            if (sp != null)
            {
                sp.ControllingClient.OnAgentRequestSit -= sp.HandleAgentRequestSit;
                sp.ControllingClient.OnAgentSit -= sp.HandleAgentSit;
            }
            else
            {
                m_log.WarnFormat("[SITMOD]: Could not disable sit from agent {0}", client.AgentId);
            }

            client.OnAgentRequestSit += client_OnAgentRequestSit;
            client.OnAgentSit += client_OnAgentSit;
        }

        private void client_OnAgentSit(IClientAPI remoteClient, UUID agentID)
        {
            if (!GetSitDisabled(remoteClient.AgentId))
            {
                ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
                if (sp != null)
                {
                    sp.HandleAgentSit(remoteClient, agentID);
                }
            }
        }

        private void client_OnAgentRequestSit(IClientAPI remoteClient, UUID agentID, UUID targetID, Vector3 offset)
        {
            if (!GetSitDisabled(remoteClient.AgentId))
            {
                ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
                if (sp != null)
                {
                    sp.HandleAgentRequestSit(remoteClient, agentID, targetID, offset);
                }
            }
        }

        #endregion

        #region ISitMod Members

        public void SetSitDisabled(UUID agentId, bool disabled)
        {
            sitDisabled[agentId] = disabled;
        }

        public bool GetSitDisabled(UUID agentId)
        {
            if (sitDisabled.ContainsKey(agentId))
            {
                return sitDisabled[agentId];
            }
            else
            {
                return default_sit_disabled;
            }
        }

        #endregion
    }
}
