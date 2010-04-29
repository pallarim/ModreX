using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using System.Collections;
using OpenMetaverse;
using log4net;
using System.Reflection;
using Nwc.XmlRpc;
using System.Net;

namespace ModularRex.RexNetwork
{
    delegate void AppearanceAddedDelegate(UUID agentID);

    public class AvatarUrlReciver : ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_scenes = new List<Scene>();
        private Dictionary<UUID, string> m_avatarUrls = new Dictionary<UUID, string>();
        private event AppearanceAddedDelegate OnNewAvatarUrl;

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
        }

        #endregion

        #region IRegionModuleBase Members

        public string Name
        {
            get { return "RexAvatarUrlReciver"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(Nini.Config.IConfigSource source)
        {
            MainServer.Instance.AddXmlRPCHandler("realXtend_avatar_url", new OpenSim.Framework.Servers.HttpServer.XmlRpcMethod(XmlRpcHandler));
            this.OnNewAvatarUrl += new AppearanceAddedDelegate(AvatarUrlReciver_OnNewAvatarUrl);
        }

        public void Close()
        {
            this.OnNewAvatarUrl -= new AppearanceAddedDelegate(AvatarUrlReciver_OnNewAvatarUrl);
        }

        public void AddRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += new EventManager.OnClientConnectCoreDelegate(HandleOnClientConnect);
        }

        public void RemoveRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            m_scenes.Remove(scene);

            scene.EventManager.OnClientConnect -= new EventManager.OnClientConnectCoreDelegate(HandleOnClientConnect);
        }

        public void RegionLoaded(OpenSim.Region.Framework.Scenes.Scene scene)
        {
        }

        #endregion

        private void TriggerOnNewAvatarUrl(UUID agentID)
        {
            try
            {
                if (OnNewAvatarUrl != null)
                {
                    OnNewAvatarUrl(agentID);
                }
            }
            catch (Exception e)
            {
                m_log.Error("[REXAVATARURL]: Error triggering OnNewAvatarUrl event: " + e.Message);
            }
        }

        private XmlRpcResponse XmlRpcHandler(XmlRpcRequest request, IPEndPoint client)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable requestData = (Hashtable)request.Params[0];
            Hashtable resp = new Hashtable();
            resp["SUCCESS"] = bool.FalseString;
            resp["ERROR"] = String.Empty;
            response.Value = resp;

            if (requestData.ContainsKey("AgentID") &&
                requestData.ContainsKey("AvatarURL"))
            {
                UUID agentID;
                string avatarUrl;
                if (!UUID.TryParse(requestData["AgentID"].ToString(), out agentID))
                {
                    resp["ERROR"] = "Could not parse Agent ID";
                    return response;
                }

                if (requestData["AvatarURL"] is string)
                {
                    avatarUrl = requestData["AvatarURL"].ToString();
                }
                else
                {
                    resp["ERROR"] = "Could not parse avatar url";
                    return response;
                }

                if (String.IsNullOrEmpty(avatarUrl))
                {
                    resp["ERROR"] = "Avatar url was null or empty";
                    return response;
                }

                m_avatarUrls[agentID] = avatarUrl;
                TriggerOnNewAvatarUrl(agentID);
                resp["SUCCESS"] = bool.TrueString;
            }

            return response;
        }

        void HandleOnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            if (m_avatarUrls.ContainsKey(client.AgentId))
            {
                IClientRexAppearance avatar;
                if (client.TryGet<IClientRexAppearance>(out avatar))
                {
                    avatar.RexAvatarURL = m_avatarUrls[client.AgentId];
                    m_log.InfoFormat("[REXAVATARURL]: Set avatar url {0} to user {1}", avatar.RexAvatarURL, client.AgentId);
                }
            }
        }

        void AvatarUrlReciver_OnNewAvatarUrl(UUID agentID)
        {
            foreach (Scene scene in m_scenes)
            {
                scene.ForEachClient(delegate(IClientAPI client)
                {
                    if (client.AgentId == agentID)
                    {
                        if (client is IClientRexAppearance)
                        {
                            ((IClientRexAppearance)client).RexAvatarURL = m_avatarUrls[agentID];
                            m_log.InfoFormat("[REXAVATARURL]: Set avatar url {0} to user {1}", m_avatarUrls[agentID], agentID);
                        }
                    }
                });
            }
        }
    }
}
