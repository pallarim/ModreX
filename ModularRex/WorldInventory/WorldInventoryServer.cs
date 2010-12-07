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
            m_webdav.OnNewCol += MkcolHandler;
            m_webdav.OnMove += MoveHandler;
            m_webdav.OnDelete += DeleteHandler;

            return true;
        }

        public void Stop()
        {
            m_webdav.OnPropFind -= PropFindHandler;
            m_webdav.OnGet -= GetHandler;
            m_webdav.OnPut -= PutHandler;
            m_webdav.OnNewCol -= MkcolHandler;
            m_webdav.OnMove -= MoveHandler;
            m_webdav.OnDelete -= DeleteHandler;
            m_listener.Stop();
        }

        private void AddRootFolders()
        {
            //check if we already have the required folders
            IWebDAVResource res = m_propertyMngr.GetResource("inventory");
            if (res == null)
            {
                //add only these folders now, add more when needed
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/", DateTime.Now, DateTime.Now, DateTime.Now, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/3d_models/", DateTime.Now, DateTime.Now, DateTime.Now, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/3d_animations/", DateTime.Now, DateTime.Now, DateTime.Now, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/ogre_scripts/", DateTime.Now, DateTime.Now, DateTime.Now, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/textures/", DateTime.Now, DateTime.Now, DateTime.Now, false));
                m_propertyMngr.SaveResource(new WebDAVFolder("/inventory/sounds/", DateTime.Now, DateTime.Now, DateTime.Now, false));
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
            Dictionary<UUID, AssetBase> textures = AssetsHelper.GetAssetList(scene, 0);
            foreach (AssetBase texture in textures.Values)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/textures/", texture.Name, texture.FullID));
            }

            Dictionary<UUID, AssetBase> sounds = AssetsHelper.GetAssetList(scene, 1);
            foreach (AssetBase sound in sounds.Values)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/sounds/", sound.Name, sound.FullID));
            }

            Dictionary<UUID, AssetBase> meshes = AssetsHelper.GetAssetList(scene, 6);
            foreach (AssetBase mesh in meshes.Values)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/3d_models/", mesh.Name, mesh.FullID));
            }

            Dictionary<UUID, AssetBase> ogreScripts = AssetsHelper.GetAssetList(scene, 41);
            foreach (AssetBase oScript in ogreScripts.Values)
            {
                m_assetFolderStrg.Save(new AssetFolderItem("/inventory/ogre_scripts/", oScript.Name, oScript.FullID));
            }

            Dictionary<UUID, AssetBase> animations = AssetsHelper.GetAssetList(scene, 19);
            foreach (AssetBase animation in animations.Values)
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
                        if (!folder.ParentPath.EndsWith("/")) folder.ParentPath += "/";
                        string resourcePath = folder.ParentPath + folder.Name;
                        if (!(folder is AssetFolderItem))
                            resourcePath += "/";
                        IWebDAVResource folderProps = m_propertyMngr.GetResource(resourcePath);
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
                                WebDAVFile file = new WebDAVFile(resourcePath, contentType, asset.Data.Length,
                                    asset.Metadata.CreationDate, DateTime.Now, DateTime.Now, false, false);

                                //add asset id to custom properties
                                file.AddProperty(new WebDAVProperty("AssetID", "http://www.realxtend.org/", item.AssetID.ToString()));
                                m_propertyMngr.SaveResource(file);
                                resources.Add(file);
                            }
                            else
                            {
                                WebDAVFolder resource = new WebDAVFolder(resourcePath, DateTime.Now, DateTime.Now, DateTime.Now, false);
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
                        if (!folder.ParentPath.EndsWith("/")) folder.ParentPath += "/";
                        string resourcePath = folder.ParentPath + folder.Name;
                        if (!(folder is AssetFolderItem))
                            resourcePath += "/";
                        IWebDAVResource folderProps = m_propertyMngr.GetResource(resourcePath);
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
                                WebDAVFile file = new WebDAVFile(resourcePath, contentType, asset.Data.Length,
                                    asset.Metadata.CreationDate, DateTime.Now, DateTime.Now, false, false);

                                //add asset id to custom properties
                                file.AddProperty(new WebDAVProperty("AssetID", "http://www.realxtend.org/", item.AssetID.ToString()));
                                m_propertyMngr.SaveResource(file);
                                resources.Add(file);
                            }
                            else
                            {
                                WebDAVFolder resource = new WebDAVFolder(resourcePath, DateTime.Now, DateTime.Now, DateTime.Now, false);
                                m_propertyMngr.SaveResource(resource);
                                resources.Add(resource);
                            }
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

                AssetBase asset = new AssetBase(UUID.Random(), pathParts[pathParts.Length - 1], (sbyte)assetType, m_scenes[0].RegionInfo.EstateSettings.EstateOwner.ToString());
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

                AssetFolder oldAsset = m_assetFolderStrg.GetItem(parentPath, asset.Name);
                if (oldAsset != null)
                {
                    m_log.InfoFormat("[WORLDINVENTORY]: Replacing old asset {0} with new", oldAsset.Name);
                    if (!m_assetFolderStrg.RemoveItem(oldAsset))
                    {
                        return HttpStatusCode.Conflict;
                    }
                }
                m_assetFolderStrg.Save(new AssetFolderItem(parentPath, asset.Name, asset.FullID));

                IWebDAVResource oldProp = m_propertyMngr.GetResource(path);
                if (oldProp == null)
                {
                    WebDAVFile prop = new WebDAVFile(path, contentType, asset.Data.Length, asset.Metadata.CreationDate, asset.Metadata.CreationDate, DateTime.Now, false, false);
                    prop.AddProperty(new WebDAVProperty("AssetID", "http://www.realxtend.org/", asset.FullID.ToString()));
                    m_propertyMngr.SaveResource(prop);
                }
                else
                {
                    WebDAVProperty assetIdProp = null;
                    foreach (WebDAVProperty prop in oldProp.CustomProperties)
                    {
                        if (prop.Name == "AssetID" && prop.Namespace == "http://www.realxtend.org/")
                            assetIdProp = prop;
                    }
                    if (assetIdProp != null)
                        oldProp.CustomProperties.Remove(assetIdProp);
                    oldProp.AddProperty(new WebDAVProperty("AssetID", "http://www.realxtend.org/", asset.FullID.ToString()));
                    m_propertyMngr.SaveResource(oldProp);
                }

                return HttpStatusCode.Created;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WORLDINVENTORY]: Failed to put resource to {0}. Exception {1} occurred.", path, e.ToString());
                return HttpStatusCode.InternalServerError;
            }
        }

        HttpStatusCode MkcolHandler(string path, string username)
        {
            if (path.EndsWith("/"))
                path = path.TrimEnd('/');
            string[] pathParts = path.Split('/');
            string parentPath = String.Empty;
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                parentPath += pathParts[i];
                parentPath += "/";
            }

            AssetFolder parent = GetAssetFolder(parentPath);
            if (parent == null)
            {
                return HttpStatusCode.Conflict;
            }
            if (parent is AssetFolderItem)
            {
                return HttpStatusCode.Conflict;
            }

            AssetFolder folder = GetAssetFolder(path);
            if (folder != null)
            {
                return HttpStatusCode.MethodNotAllowed;
            }
            else
            {
                folder = new AssetFolder(parentPath, pathParts[pathParts.Length - 1]);
                m_assetFolderStrg.Save(folder);
                if (!path.EndsWith("/"))
                    path += "/";
                m_propertyMngr.SaveResource(new WebDAVFolder(path, DateTime.Now, DateTime.Now, DateTime.Now, false));
                m_log.DebugFormat("[WORLDINVENTORY]: Created folder {0} to {1}", folder.Name, folder.ParentPath);
                return HttpStatusCode.Created;
            }
        }

        HttpStatusCode MoveHandler(string username, Uri source, string destination, DepthHeader depth, bool overwrite,
            string[] ifHeaders, out Dictionary<string, HttpStatusCode> multiStatusValues)
        {
            multiStatusValues = null;
            try
            {
                string[] srcParts = source.ToString().Split('/');
                string[] dstParts = destination.Split('/');

                //check the source
                string srcParent = "/";
                string[] srcFolders = new string[srcParts.Length - 4];
                Array.Copy(srcParts, 3, srcFolders, 0, srcFolders.Length);
                for (int i = 0; i < srcFolders.Length; i++)
                {
                    srcParent += srcFolders[i];
                    srcParent += "/";
                }

                string srcResourceName = (source.ToString().EndsWith("/")) ? srcParts[srcParts.Length - 2] : srcParts[srcParts.Length - 1];
                AssetFolder sourceResource = m_assetFolderStrg.GetItem(srcParent, srcResourceName);

                if (sourceResource == null)
                    return HttpStatusCode.NotFound; //source resource not found

                //then check destination
                string dstParent = "/";
                string[] dstFolders = new string[dstParts.Length - 4];
                Array.Copy(dstParts, 3, dstFolders, 0, dstFolders.Length);
                for (int i = 0; i < dstFolders.Length; i++)
                {
                    dstParent += dstFolders[i];
                    dstParent += "/";
                }

                //check if destination parent exists
                AssetFolder destinationParent = GetAssetFolder(dstParent);
                if (destinationParent == null)
                    return HttpStatusCode.Conflict; //it didn't, conflict

                //if no problems, move
                string dstResourceName = (source.ToString().EndsWith("/")) ? dstParts[dstParts.Length - 2] : dstParts[dstParts.Length - 1];
                AssetFolder destinationResource = m_assetFolderStrg.GetItem(dstParent, dstResourceName);
                if (destinationResource == null)
                {
                    //no destination. simplest case.
                    //just move existing resource to here
                    MoveFolderAndAllSubFolders(sourceResource, destinationResource, dstParent, dstResourceName, ref multiStatusValues);

                    if (multiStatusValues != null)
                        return (HttpStatusCode)207;
                    else
                        return HttpStatusCode.Created;
                }
                else
                {
                    if (overwrite)
                    {
                        //check if source and destination are both folder so we move only the content
                        //and remove the old folder
                        bool sourceIsFolder = !(sourceResource is AssetFolderItem);
                        bool destinationIsFolder = !(destinationResource is AssetFolderItem);
                        if (sourceIsFolder && destinationIsFolder)
                        {
                            //remove, add etc.
                            MoveFolderAndAllSubFolders(sourceResource, destinationResource, dstParent, dstResourceName, ref multiStatusValues);

                            if (multiStatusValues != null)
                                return (HttpStatusCode)207;
                            else
                                return HttpStatusCode.NoContent; //all ok
                        }
                        else
                        {
                            MoveFolderAndAllSubFolders(sourceResource, destinationResource, dstParent, dstResourceName, ref multiStatusValues);

                            if (multiStatusValues != null)
                                return (HttpStatusCode)207;
                            else
                                return HttpStatusCode.Created;
                        }
                    }
                    else
                    {
                        return HttpStatusCode.PreconditionFailed;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WORLDINVENTORY]: Could not move {0} to {1}, because exception {2} was thrown.", source.ToString(), destination, e.ToString());
                return HttpStatusCode.InternalServerError;
            }
        }

        HttpStatusCode DeleteHandler(Uri uri, string username, out Dictionary<string, HttpStatusCode> multiStatus)
        {
            multiStatus = null;

            if (DeleteResource(uri.AbsolutePath, ref multiStatus))
            {
                return HttpStatusCode.NoContent;
            }
            else if (multiStatus.Count == 1 && multiStatus.ContainsKey(uri.AbsolutePath))
            {
                HttpStatusCode ret = multiStatus[uri.AbsolutePath];
                multiStatus = null;
                return ret;
            }
            else
            {
                return (HttpStatusCode)207;
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

        private AssetFolder GetAssetFolder(string path)
        {
            if (path.EndsWith("/"))
                path = path.TrimEnd('/');
            string[] pathParts = path.Split('/');
            string parentPath = String.Empty;
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                parentPath += pathParts[i];
                parentPath += "/";
            }
            return m_assetFolderStrg.GetItem(parentPath, pathParts[pathParts.Length - 1]);
        }

        private void MoveFolderAndAllSubFolders(AssetFolder source, AssetFolder destination, string destinationParent, string destinationName, ref Dictionary<string, HttpStatusCode> multiStatus)
        {
            string srcPath = source.ParentPath + source.Name;
            if (!(source is AssetFolderItem)) srcPath += "/";

            //check "permissions"
            if (IsProtectedPath(srcPath))
            {
                //this is one of the default paths. this can't be deleted because it might result some problems
                if (multiStatus == null)
                    multiStatus = new Dictionary<string, HttpStatusCode>();
                multiStatus.Add(srcPath, HttpStatusCode.Forbidden);
                return;
            }

            //TODO: Lock check

            //first move resource
            if (destination == null)
            {
                if (!m_assetFolderStrg.RemoveItem(source)) { SetMultiStatusPrecoditionFailedError(destinationParent + destinationName, ref multiStatus); return; }
                source.Name = destinationName;
                source.ParentPath = destinationParent;
                m_assetFolderStrg.Save(source);

                IWebDAVResource resProp = m_propertyMngr.GetResource(srcPath);
                if (!m_propertyMngr.Remove(resProp)) { SetMultiStatusPrecoditionFailedError(destinationParent + destinationName, ref multiStatus); return; }
                string newPath = destinationParent + destinationName;
                if (!(source is AssetFolderItem)) newPath += "/";
                resProp.Path = newPath;
                if (!m_propertyMngr.SaveResource(resProp)) { SetMultiStatusPrecoditionFailedError(destinationParent + destinationName, ref multiStatus); return; }
            }
            else
            {
                //just delete the source
                if (!m_assetFolderStrg.RemoveItem(source)) { SetMultiStatusPrecoditionFailedError(destinationParent + destinationName, ref multiStatus); return; }
                if (!m_propertyMngr.Remove(srcPath)) { SetMultiStatusPrecoditionFailedError(destinationParent + destinationName, ref multiStatus); return; }
            }

            //then move sub resources
            if (!(source is AssetFolderItem))
            {
                IList<AssetFolder> subitems = m_assetFolderStrg.GetSubItems(srcPath);
                foreach (AssetFolder item in subitems)
                {
                    AssetFolder subDst = m_assetFolderStrg.GetItem(destinationParent + destinationName + "/", item.Name);
                    MoveFolderAndAllSubFolders(item, subDst, destinationParent + destinationName + "/", item.Name, ref multiStatus);
                }
            }
        }

        private void SetMultiStatusPrecoditionFailedError(string path, ref Dictionary<string, HttpStatusCode> multiStatus)
        {
            if (multiStatus == null)
                multiStatus = new Dictionary<string, HttpStatusCode>();
            multiStatus.Add(path, HttpStatusCode.PreconditionFailed);
        }

        private bool IsProtectedPath(string path)
        {
            switch (path)
            {
                case "/inventory/":
                case "/inventory/3d_models/":
                case "/inventory/3d_animations/":
                case "/inventory/ogre_scripts/":
                case "/inventory/textures/":
                case "/inventory/sounds/":
                    return true;
                default:
                    return false;
            }
        }

        private bool DeleteResource(string path, ref Dictionary<string, HttpStatusCode> multiStatus)
        {
            AssetFolder item = GetAssetFolder(path);
            if (item is AssetFolderItem)
            {
                //delete item and webdavproperties
                if (!m_assetFolderStrg.RemoveItem(item) || !m_propertyMngr.Remove(path))
                {
                    SetMultiStatusPrecoditionFailedError(path, ref multiStatus);
                    return false;
                }
                else
                    return true;
            }
            else
            {
                bool okToDeleteThis = true;

                //first delete subitems
                if (!path.EndsWith("/")) path += "/";
                IList<AssetFolder> subItems = m_assetFolderStrg.GetSubItems(path);
                foreach (AssetFolder subitem in subItems)
                {
                    string subItemPath = path + subitem.Name;
                    if (!(subitem is AssetFolderItem))
                        subItemPath += "/";
                    if (!DeleteResource(subItemPath, ref multiStatus))
                        okToDeleteThis = false;
                }

                //if no problems, delete also this
                if (okToDeleteThis)
                {
                    if (IsProtectedPath(path)) //this is not ok to be deleted
                    {
                        if (multiStatus == null)
                            multiStatus = new Dictionary<string, HttpStatusCode>();
                        multiStatus.Add(path, HttpStatusCode.Forbidden);
                        return false;
                    }

                    if (!m_assetFolderStrg.RemoveItem(item) || !m_propertyMngr.Remove(path))
                    {
                        SetMultiStatusPrecoditionFailedError(path, ref multiStatus);
                        return false;
                    }
                    else
                        return true;
                }
                else
                {
                    SetMultiStatusPrecoditionFailedError(path, ref multiStatus);
                    return false;
                }
            }
        }

        #endregion
    }
}
