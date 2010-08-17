using System;
using System.Reflection;
using System.Collections.Generic;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Server.Base;
using OpenSim.Server.Handlers.Base;
using OpenSim.Server.Handlers.Login;
using OpenSim.Services.Interfaces;

namespace ModularRex.RexNetwork.RexLogin
{
    public class RexLoginServiceInConnectorModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static bool m_Enabled = false;
        private static bool m_Registered = false;

        private IConfigSource m_Config;
        private List<Scene> m_Scenes = new List<Scene>();

        #region IRegionModule interface

        public void Initialise(IConfigSource config)
        {
            m_Config = config;

            IConfig moduleConfig = config.Configs["Modules"];
            if (moduleConfig != null)
            {
                m_Enabled = moduleConfig.GetBoolean("REXLoginServiceInConnector", false);
                if (m_Enabled)
                {
                    m_log.Info("[REXLOGIN IN CONNECTOR]: REXLoginerviceInConnector enabled");
                }

            }

        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;

            m_log.Info("[REXLOGIN IN CONNECTOR]: Starting...");
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "REXLoginServiceInConnectorModule"; }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            m_Scenes.Add(scene);

        }

        public void RemoveRegion(Scene scene)
        {
            if (m_Enabled && m_Scenes.Contains(scene))
                m_Scenes.Remove(scene);
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (!m_Registered)
            {
                m_Registered = true;
                new RexLoginServiceInConnector(m_Config, MainServer.Instance, scene);
                Object[] args = new Object[] { m_Config, MainServer.Instance, this, scene };
                ServerUtils.LoadPlugin<IServiceConnector>("OpenSim.Server.Handlers.dll:LLLoginServiceInConnector", args);
            }

        }

        #endregion
    }
}
