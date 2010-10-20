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

        protected WorldInventoryServer m_server = null;

        private List<Scene> m_scenes = new List<Scene>();
        private int m_port = 6000;
        private bool enabled = false;
        private IConfigSource m_configs = null;

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scenes.Add(scene);
            IConfig config = source.Configs["realXtend"];
            if (config != null)
            {
                enabled = config.GetBoolean("WorldInventoryOn", false);
                m_port = config.GetInt("WorldInventoryPort", 6000);
            }
            m_configs = source;
            scene.EventManager.OnNewClient += OnNewClient;
            scene.AddCommand(this,
                "worldinventory load assets",
                "worldinventory load assets",
                "Loads assets from scene to world inventory. If no scene is selected, load from all scenes.",
                consoleHandleLoadAssets);
        }

        public void PostInitialise()
        {
            if (enabled)
            {
                IPAddress ip = m_scenes[0].RegionInfo.ExternalEndPoint.Address;
                m_server = new WorldInventoryServer(m_scenes, m_configs);
                bool started = m_server.Start(ip, m_port);
            }
        }

        public void Close()
        {
            foreach (Scene scene in m_scenes)
            {
                scene.EventManager.OnNewClient -= OnNewClient;
            }

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

        internal void consoleHandleLoadAssets(string module, string[] args)
        {
            Scene scene = m_scenes[0].ConsoleScene();
            if (scene != null)
            {
                //load assets from selected scene
                m_server.LoadAssetsFromScene(scene);
            }
            else
            {
                //no console scene selected
                //load assets from all scenes
                foreach (Scene s in m_scenes)
                {
                    m_server.LoadAssetsFromScene(s);
                }
            }
        }

        void OnNewClient(IClientAPI client)
        {
            if(client is ModularRex.RexNetwork.RexClientViewBase)
            {
                if (!client.AddGenericPacketHandler("wi_req", HandleWorldInventoryGenericMessage))
                    m_log.Warn("[WORLDINVENTORY]: Could not add generic message handler for user");
            }
        }

        void HandleWorldInventoryGenericMessage(Object sender, string method, List<String> args)
        {
            if (sender is IClientAPI)
            {
                IClientAPI client = (IClientAPI)sender;
                //TODO: parse properties (and invent what they are if necessary)
                List<string> response = new List<string>();
                if (enabled) //send world inventory port (and/or address) ToBeDecided
                {
                    response.Add(m_port.ToString());
                }
                else //send not in use
                {
                    response.Add("-1");
                }
                client.SendGenericMessage("wi_resp", response);
            }
            else
            {
                m_log.Warn("[WORLDINVENTORY]: wi_req sender was not IClientAPI");
            }
        }
    }

    public class HttpServerLogWriter : ILogWriter
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Write(object source, LogPrio priority, string message)
        {
            switch (priority)
            {
                case LogPrio.Trace:
                case LogPrio.Debug:
                    m_log.DebugFormat("[WORLDINVENTORY]: {0}: {1}", source.ToString(), message);
                    break;
                case LogPrio.Error:
                    m_log.ErrorFormat("[WORLDINVENTORY]: {0}: {1}", source.ToString(), message);
                    break;
                case LogPrio.Info:
                    m_log.InfoFormat("[WORLDINVENTORY]: {0}: {1}", source.ToString(), message);
                    break;
                case LogPrio.Warning:
                    m_log.WarnFormat("[WORLDINVENTORY]: {0}: {1}", source.ToString(), message);
                    break;
                case LogPrio.Fatal:
                    m_log.FatalFormat("[WORLDINVENTORY]: {0}: FATAL! - {1}", source.ToString(), message);
                    break;
                default:
                    m_log.DebugFormat("[WORLDINVENTORY]: {0}: {1}", source.ToString(), message);
                    break;
            }

            return;
        }
    }
}
