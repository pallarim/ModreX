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
using System.Net;
using HttpListener = HttpServer.HttpListener;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;

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
        private bool m_giveFolderContentOnGet = false;
        private bool m_autoconvertJpgToJ2k = false;

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
                m_giveFolderContentOnGet = config.GetBoolean("WorldInventoryGetFolderContent", false);
                m_autoconvertJpgToJ2k = config.GetBoolean("WorldInventoryAutoConvertJpegToJ2K", false);
            }

            if (webdavPropertyStrgConnectionString == null || webdavPropertyStrgConnectionString == String.Empty)
                return false;

            m_assetFolderStrg = new NHibernateAssetsFolder();
            m_propertyMngr = new NHibernateIWebDAVResource();
            m_propertyMngr.Initialise(webdavPropertyStrgConnectionString);
            m_assetFolderStrg.Initialise(webdavPropertyStrgConnectionString);
            AddRootFolders();

            m_webdav.OnPropFind += PropFindHandler;
            m_webdav.OnGet += GetHandler;
            m_webdav.OnPut += PutHandler;

            return true;
        }

        public void Stop()
        {
            m_webdav.OnPropFind -= PropFindHandler;
            m_webdav.OnGet -= GetHandler;
            m_webdav.OnPut -= PutHandler;
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

        #region WebDAV method handlers

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

        HttpStatusCode GetHandler(IHttpResponse response, string path, string username)
        {
            try
            {
                string[] pathParts = path.Split('/');
                string parentPath = String.Empty;
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    parentPath += pathParts[i];
                    parentPath += "/";
                }
                AssetFolder resource = m_assetFolderStrg.GetItem(parentPath, pathParts[pathParts.Length - 1]);
                if (resource is AssetFolderItem)
                {
                    AssetFolderItem item = resource as AssetFolderItem;
                    AssetBase asset = m_scenes[0].AssetService.Get(item.AssetID.ToString());
                    response.ContentType = MimeTypeConverter.GetContentType((int)asset.Type);
                    response.AddHeader("Content-Disposition", "attachment; filename=" + item.Name);
                    response.ContentLength = asset.Data.Length;
                    response.Body.Write(asset.Data, 0, asset.Data.Length);
                    return HttpStatusCode.OK;
                }
                else
                {
                    //check if we give out asset list with get
                    if (m_giveFolderContentOnGet)
                    {
                        string body = String.Empty;
                        // HTML
                        body += "<html>" +
                                "<head>" +
                                "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />" +
                                "<title>" + pathParts[pathParts.Length - 1] + "</title>" +
                                "</head>" +
                                "<body>" +
                                "<p>";

                        IList<AssetFolder> items = m_assetFolderStrg.GetSubItems(path);
                        foreach (AssetFolder item in items)
                        {
                            string itemName = item.Name;
                            if (!(item is AssetFolderItem))
                                itemName += "/";
                            body += "<a href=\"" + path + itemName + "\">" + itemName;
                            body += "</a><br>";
                        }

                        body += "</p></body></html>";

                        UTF8Encoding ecoding = new UTF8Encoding();
                        response.ContentType = "text/html";
                        byte[] bytes = Encoding.UTF8.GetBytes(body);
                        response.ContentLength = bytes.Length;
                        response.Body.Write(bytes, 0, bytes.Length);
                        return HttpStatusCode.OK;
                    }
                    else
                    {
                        return HttpStatusCode.Forbidden;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WORLDINVENTORY]: Failed to get resource for request to {0}. Exception {1} occurred.", path, e.ToString());
                return HttpStatusCode.InternalServerError;
            }
        }

        HttpStatusCode PutHandler(IHttpRequest request, string path, string username)
        {
            try
            {
                byte[] assetData = request.GetBody();

                string[] pathParts = path.Split('/');
                string parentPath = String.Empty;
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    parentPath += pathParts[i];
                    parentPath += "/";
                }

                string contentType = request.Headers["Content-type"];
                int assetType = -1; //unknown
                if (contentType != null && contentType != String.Empty)
                {
                    m_log.DebugFormat("[WORLDINVENTORY]: Found content-type {0} for put request to {1}", contentType, path);
                    assetType = MimeTypeConverter.GetAssetTypeFromMimeType(contentType);
                }
                else
                {
                    //missing content type
                    m_log.WarnFormat("[WORLDINVENTORY]: Could not find content-type from request {0} headers. Trying to parse from file extension", path);
                    string[] fileParts = pathParts[pathParts.Length - 1].Split('.');
                    if (fileParts.Length > 1)
                    {
                        string fileExtension = fileParts[fileParts.Length - 1];
                        assetType = MimeTypeConverter.GetAssetTypeFromFileExtension(fileExtension);
                    }
                    contentType = MimeTypeConverter.GetContentType(assetType);
                }

                AssetBase asset = new AssetBase(UUID.Random(), pathParts[pathParts.Length - 1], (sbyte)assetType);
                asset.Local = false;

                if (m_autoconvertJpgToJ2k && assetType == (int)AssetType.ImageJPEG)
                {
                    System.IO.MemoryStream ms = new System.IO.MemoryStream(assetData);
                    System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(ms);
                    assetData = OpenMetaverse.Imaging.OpenJPEG.EncodeFromImage(bitmap, false);
                    asset.Type = (int)AssetType.Texture;
                    asset.Name = ReplaceFileExtension(asset.Name, "jp2");
                }

                asset.Data = assetData;
                m_scenes[0].AssetService.Store(asset);

                m_assetFolderStrg.Save(new AssetFolderItem(parentPath, asset.Name, asset.FullID));
                return HttpStatusCode.Created;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WORLDINVENTORY]: Failed to put resource to {0}. Exception {1} occurred.", path, e.ToString());
                return HttpStatusCode.InternalServerError;
            }
        }

        #endregion

        #region Helpers

        private string ReplaceFileExtension(string filename, string newExtension)
        {
            string[] fileParts = filename.Split('.');
            if (fileParts.Length > 1)
            {
                fileParts[fileParts.Length - 1] = newExtension;
                string newFileName = String.Empty;
                foreach (string s in fileParts)
                {
                    newFileName += s;
                    newFileName += ".";
                }
                return newFileName;
            }
            else if (fileParts.Length == 1)
            {
                return filename + "." + newExtension;
            }
            else
            {
                throw new FormatException("Could not change file extension");
            }
        }

        #endregion
    }
}
