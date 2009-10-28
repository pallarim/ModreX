using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using WebDAVSharp;
using OpenSim.Region.Framework.Scenes;
using WebDAVSharp.NHibernateStorage;
using Nini.Config;
using log4net;
using System.Reflection;
using ModularRex.NHibernate;
using ModularRex.RexFramework;

namespace ModularRex.WorldInventory
{
    public class WorldInventoryServer
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private HttpServerLogWriter httpserverlog = new HttpServerLogWriter();

        protected HttpListener m_listener = null;
        protected WebDAVListener m_webdav = null;
        protected NHibernateIWebDAVResource m_propertyMngr = null;
        protected NHibernateAssetsFolder m_assetFolderStrg = null;

        private List<Scene> m_scenes = null;
        private IConfigSource m_configs = null;
        private List<IWebDAVResource> m_rootFolders = new List<IWebDAVResource>();

        public WorldInventoryServer()
        {
        }

        public WorldInventoryServer(List<Scene> scenes, IConfigSource configs)
        {
            m_scenes = scenes;
            m_configs = configs;
        }

        public bool Start(System.Net.IPAddress ip, int port)
        {
            try
            {
                m_listener = HttpListener.Create(httpserverlog, ip, port);
                m_webdav = new WebDAVListener(m_listener, @"^/inventory/");
                m_webdav.Authentication = AuthenticationType.None;
                m_listener.Start(10);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WORLDINVENTORY]: Failed to start WebDAV listener on port {0}. Threw excpetion {1}", port, e.ToString());
                return false;
            }

            string webdavPropertyStrgConnectionString = String.Empty;
            IConfig config = m_configs.Configs["realXtend"];
            if (config != null)
            {
                webdavPropertyStrgConnectionString = config.GetString("WebDAVProperyStorageConnectionString");
            }

            if (webdavPropertyStrgConnectionString == null || webdavPropertyStrgConnectionString == String.Empty)
                return false;

            m_assetFolderStrg = new NHibernateAssetsFolder();
            m_propertyMngr = new NHibernateIWebDAVResource();
            m_propertyMngr.Initialise(webdavPropertyStrgConnectionString);
            m_assetFolderStrg.Initialise(webdavPropertyStrgConnectionString);
            AddRootFolders();

            m_webdav.OnPropFind += PropFindHandler;

            return true;
        }

        private void AddRootFolders()
        {
            //check if we already have the required folders
            IWebDAVResource res = m_propertyMngr.GetResource("inventory");
            if (res == null)
            {
                //add only these folders now, add more when needed
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory/3d models", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory/3d animations", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory/materials", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory/textures", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory/sounds", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("inventory/particles", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            }

            AssetFolder folder = m_assetFolderStrg.GetItem("/", "inventory");
            if (folder == null)
            {
                m_assetFolderStrg.Save(new AssetFolder("/", "inventory"));
                m_assetFolderStrg.Save(new AssetFolder("inventory/", "3d models"));
                m_assetFolderStrg.Save(new AssetFolder("inventory/", "3d animations"));
                m_assetFolderStrg.Save(new AssetFolder("inventory/", "materials"));
                m_assetFolderStrg.Save(new AssetFolder("inventory/", "textures"));
                m_assetFolderStrg.Save(new AssetFolder("inventory/", "sounds"));
                m_assetFolderStrg.Save(new AssetFolder("inventory/", "particles"));
            }
        }

        public void Stop()
        {
            m_webdav.OnPropFind -= PropFindHandler;
            m_listener.Stop();
        }

        IList<IWebDAVResource> PropFindHandler(string username, string path, DepthHeader depth)
        {
            string[] pathParts = path.Split('/');
            if (pathParts.Length >= 2 && pathParts[1] == "inventory")
            {
                List<IWebDAVResource> resources = new List<IWebDAVResource>();
                if (depth == DepthHeader.Zero)
                {
                    IWebDAVResource res = m_propertyMngr.GetResource(path);
                    resources.Add(res);
                    return resources;
                }
                else if (depth == DepthHeader.One)
                {
                    IWebDAVResource res = m_propertyMngr.GetResource(path);
                    resources.Add(res);
                    IList<AssetFolder> folders = m_assetFolderStrg.GetSubItems(path);
                    foreach (AssetFolder folder in folders)
                    {
                        folder.ParentPath = folder.ParentPath.EndsWith("/") == true ? folder.ParentPath : folder.ParentPath+"/";
                        IWebDAVResource folderProps = m_propertyMngr.GetResource(folder.ParentPath+folder.Name);
                        if (folderProps != null)
                        {
                            resources.Add(folderProps);
                        }
                        else
                        {
                            //create new props, save them and add them to response
                        }
                    }
                    return resources;
                }
                else if (depth == DepthHeader.Infinity)
                {
                    IWebDAVResource res = m_propertyMngr.GetResource(path);
                    resources.Add(res);
                    IList<AssetFolder> folders = m_assetFolderStrg.GetSubItems(path);
                    //get subitems until found all
                    foreach (AssetFolder folder in folders)
                    {
                        folder.ParentPath = folder.ParentPath.EndsWith("/") == true ? folder.ParentPath : folder.ParentPath + "/";
                        IWebDAVResource folderProps = m_propertyMngr.GetResource(folder.ParentPath + folder.Name);
                        if (folderProps != null)
                        {
                            resources.Add(folderProps);
                        }
                        else
                        {
                            //create new props, save them and add them to response
                        }
                    }
                    return resources;
                }
            }

            return null;
        }
    }
}
