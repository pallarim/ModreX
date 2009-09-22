using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using HttpServer;
using log4net;
using System.Reflection;
using System.Net;
using WebDAVSharp;

namespace ModularRex.WorldInventory
{
    public class WorldInventoryModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region IRegionModule Members

        protected WorldInventoryServer m_server = null;

        private List<Scene> m_scenes = new List<Scene>();
        private int m_port = 6000;
        private bool enabled = false;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scenes.Add(scene);
            IConfig config = source.Configs["realXtend"];
            if (config != null)
            {
                enabled = config.GetBoolean("WorldInventoryOn", false);
                m_port = config.GetInt("WorldInventoryPort", 6000);
            }
        }

        public void PostInitialise()
        {
            if (enabled)
            {
                IPAddress ip = m_scenes[0].RegionInfo.ExternalEndPoint.Address;
                m_server = new WorldInventoryServer(m_scenes);
                m_server.Start(ip, m_port);
            }
        }

        public void Close()
        {
            if (m_server != null)
                m_server.Stop();
        }

        public string Name
        {
            get { return "WorldInventory"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion
    }

    public class HttpServerLogWriter : ILogWriter
    {
        //private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Write(object source, LogPrio priority, string message)
        {
            /*
            switch (priority)
            {
                case HttpServer.LogPrio.Debug:
                    m_log.DebugFormat("[{0}]: {1}", source.ToString(), message);
                    break;
                case HttpServer.LogPrio.Error:
                    m_log.ErrorFormat("[{0}]: {1}", source.ToString(), message);
                    break;
                case HttpServer.LogPrio.Info:
                    m_log.InfoFormat("[{0}]: {1}", source.ToString(), message);
                    break;
                case HttpServer.LogPrio.Warning:
                    m_log.WarnFormat("[{0}]: {1}", source.ToString(), message);
                    break;
                case HttpServer.LogPrio.Fatal:
                    m_log.ErrorFormat("[{0}]: FATAL! - {1}", source.ToString(), message);
                    break;
                default:
                    break;
            }
            */

            return;
        }
    }
}
