using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using log4net;
using System.Reflection;

namespace ModularRex.RexNetwork.RexLogin
{
    public class LoginConfigurationManager : ISharedRegionModule, IRexUDPPort
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IConfigSource m_config = null;
        private int m_nextPort = 7000;
        private bool m_loginMethodOverloaded = false;
        private Dictionary<ulong, int> m_region_ports = new Dictionary<ulong, int>();
        private List<Scene> m_scenes = new List<Scene>();

        public int NextPort
        {
            get {
                int toReturn = m_nextPort;
                m_nextPort++;
                return toReturn;
            }
        }

        public bool LoginMethodOverloaded
        {
            get { return m_loginMethodOverloaded; }
            set { m_loginMethodOverloaded = value; }
        }

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
        }

        #endregion

        #region IRegionModuleBase Members

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);
            scene.RegisterModuleInterface<IRexUDPPort>(this);
            if (m_config.Configs["realXtend"] != null && m_config.Configs["realXtend"].GetBoolean("enabled", false))
            {
                scene.RegisterModuleInterface<LoginConfigurationManager>(this);
            }
        }

        public void Close()
        {
        }

        public void Initialise(IConfigSource source)
        {
            m_config = source;
            if (m_config.Configs["realXtend"] != null && m_config.Configs["realXtend"].GetBoolean("enabled", false))
            {
                m_nextPort = m_config.Configs["realXtend"].GetInt("FirstPort", 7000);
            }
        }

        public string Name
        {
            get { return "RexLoginConfigManager"; }
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion


        #region IRexUDPPort Members

        public int GetPort(ulong regionHandle)
        {
            if (m_region_ports.ContainsKey(regionHandle))
            {
                return m_region_ports[regionHandle];
            }
            else
            {
                m_log.Warn("[IRexUDPPort]: Port not found for region handle " + regionHandle);
                return 0;
            }
        }

        public int GetPort(System.Net.IPEndPoint endPoint)
        {
            foreach (Scene s in m_scenes)
            {
                if (s.RegionInfo.ExternalEndPoint.Address.Equals(endPoint.Address) && s.RegionInfo.ExternalEndPoint.Port == endPoint.Port)
                {
                    return GetPort(s.RegionInfo.RegionHandle);
                }
            }
            m_log.WarnFormat("[IRexUDPPort]: Port not found for IP end point {0}", endPoint);
            return 0;
        }

        public bool RegisterRegionPort(ulong regionHandle, int port)
        {
            try
            {
                m_region_ports.Add(regionHandle, port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
