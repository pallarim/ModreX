using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Server.Handlers.Asset;
using log4net;
using System.Reflection;

namespace ModularRex.RexNetwork
{
    public class HttpAssetProvider : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IAssetService m_AssetService;
        private List<Scene> m_scenes = new List<Scene>();
        private bool enabled = false;

        public string Name
        {
            get { return "HttpAssetProvider"; }
        }

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scenes.Add(scene);
            if (source.Configs["realXtend"] != null)
            {
                enabled = !(source.Configs["realXtend"].GetBoolean("ServeHttpAssets", false));
            }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        public void PostInitialise()
        {
            if (enabled)
            {
                m_AssetService = m_scenes[0].AssetService;
                if (m_AssetService != null)
                    MainServer.Instance.AddStreamHandler(new AssetServerGetHandler(m_AssetService));
                else
                    m_log.Error("[HttpAssetProvider]: Could not initiate HttpAssetProvider since IAssetService is null!");
            }
        }

        #endregion
    }
}
