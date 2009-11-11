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
using ModularRex.RexParts.Helpers;
using OpenSim.Framework;

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

        public void Stop()
        {
            m_webdav.OnPropFind -= PropFindHandler;
            m_listener.Stop();
        }

        private void AddRootFolders()
        {
            //check if we already have the required folders
            IWebDAVResource res = m_propertyMngr.GetResource("inventory");
            if (res == null)
            {
                //add only these folders now, add more when needed
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/3d_models/", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/3d_animations/", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/ogre_scripts/", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/textures/", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/sounds/", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            }

            AssetFolder folder = m_assetFolderStrg.GetItem("/", "inventory");
            if (folder == null)
            {
                m_assetFolderStrg.Save(new AssetFolder("/", "inventory"));
                m_assetFolderStrg.Save(new AssetFolder("/inventory/", "3d_models"));
                m_assetFolderStrg.Save(new AssetFolder("/inventory/", "3d_animations"));
                m_assetFolderStrg.Save(new AssetFolder("/inventory/", "ogre_scripts"));
                m_assetFolderStrg.Save(new AssetFolder("/inventory/", "textures"));
                m_assetFolderStrg.Save(new AssetFolder("/inventory/", "sounds"));
            }
        }

        public void LoadAssetsFromScene(Scene scene)
        {
            List<AssetBase> textures = AssetsHelper.GetAssetList(scene, 0);
            foreach (AssetBase texture in textures)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/textures/", texture.Name, texture.FullID));
            }

            List<AssetBase> sounds = AssetsHelper.GetAssetList(scene, 1);
            foreach (AssetBase sound in sounds)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/sounds/", sound.Name, sound.FullID));
            }

            List<AssetBase> meshes = AssetsHelper.GetAssetList(scene, 6);
            foreach (AssetBase mesh in meshes)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/3d_models/", mesh.Name, mesh.FullID));
            }

            List<AssetBase> ogreScripts = AssetsHelper.GetAssetList(scene, 41);
            foreach (AssetBase oScript in ogreScripts)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/ogre_scripts/", oScript.Name, oScript.FullID));
            }

            List<AssetBase> animations = AssetsHelper.GetAssetList(scene, 19);
            foreach (AssetBase animation in animations)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/3d_animations/", animation.Name, animation.FullID));
            }
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
                        IWebDAVResource folderProps = m_propertyMngr.GetResource(folder.ParentPath + folder.Name);
                        if (folderProps != null)
                        {
                            resources.Add(folderProps);
                        }
                        else
                        {
                            //create new props, save them and add them to response
                            if (folder is AssetFolderItem)
                            {
                                AssetFolderItem item = (AssetFolderItem)folder;
                                AssetBase asset = m_scenes[0].AssetService.Get(item.AssetID.ToString());
                                string contentType = MimeTypeConverter.GetContentType((int)asset.Type);
                                WebDAVFile file = new WebDAVFile(folder.ParentPath + folder.Name,
                                    contentType, asset.Data.Length,
                                    asset.Metadata.CreationDate, DateTime.Now, DateTime.Now, false, false);

                                //add asset id to custom properties
                                file.AddProperty(new WebDAVProperty("AssetID", "http://www.realxtend.org/", item.AssetID.ToString()));
                                m_propertyMngr.SaveResource(file);
                                resources.Add(file);
                            }
                            else
                            {
                                WebDAVFolder resource = new WebDAVFolder(folder.ParentPath + folder.Name, DateTime.Now, DateTime.Now, DateTime.Now, false);
                                m_propertyMngr.SaveResource(resource);
                                resources.Add(resource);
                            }
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
