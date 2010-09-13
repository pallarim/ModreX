using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using log4net;
using System.Reflection;
using System.IO;
using OpenSim.Framework.Servers.HttpServer;
using Ionic.Zip;
using System.Drawing;
using OpenSim.Framework;
using ModularRex.RexFramework;
using System.Net;
using OgreSceneImporter.UploadSceneDB;

//using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Xml;


namespace OgreSceneImporter
{
    public delegate byte[] HttpRequestCallback(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse);

    public class StreamHandler : BaseStreamHandler
    {
        private HttpRequestCallback m_callback;

        public override string ContentType { get { return null; } }

        public StreamHandler(string httpMethod, string path, HttpRequestCallback callback) :
            base(httpMethod, path)
        {
            m_callback = callback;
        }

        public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            return m_callback(path, request, httpRequest, httpResponse);
        }
    }

    [Serializable()]
    public class SceneRegion
    {
        public string SceneName;
        public string Region;
        public string SceneUuid;
    }


    /// <summary>
    /// Handle adding caps handlers for uploading scenes, when user, with rights to upload scene files, logs in,
    /// + handle uploads
    /// </summary>
    public class UploadHandler
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private List<Scene> m_scenes = new List<Scene>();
        private OgreSceneImporter.UploadSceneDB.ISceneStorage m_SceneStorage;

        
        private OgreSceneImportModule m_osi;

        private const string EXTRACT_FOLDER_NAME = "SceneUploadZipFiles";

        public IAssetDataSaver AssetDataSaver
        {
            get { return (IAssetDataSaver)m_SceneStorage; }
        }

        public void Configure(Nini.Config.IConfigSource source)
        {
            m_SceneStorage = new OgreSceneImporter.UploadSceneDB.NHibernateSceneStorage(source);
        }

        /// <summary>
        /// Configure must be called before this is called
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="osi"></param>
        public void AddUploadCap(Scene scene, OgreSceneImportModule osi)
        {
            try
            {
                if (m_SceneStorage == null) { throw new Exception("SceneStorage is null"); }
                m_scene = scene;
                m_scenes.Add(scene);
                m_osi = osi;
                scene.EventManager.OnRegisterCaps += this.RegisterCaps;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error starting upload handler: {0}, {1}", e.Message, e.StackTrace);
            }
        }

        public void RegisterCaps(UUID agentID, OpenSim.Framework.Capabilities.Caps caps)
        {
            if (CheckRights(agentID))
            {
                UUID capID = UUID.Random();
                m_log.InfoFormat("[OGRESCENE]: Creating capability: /CAPS/{0}", capID);
                caps.RegisterHandler("UploadScene", new StreamHandler("POST", "/CAPS/" + capID, ProcessUploadSceneMessages));
            }
        }

        private byte[] ProcessUploadSceneMessages(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            
            string method = httpRequest.Headers["USceneMethod"];
            
            m_log.InfoFormat("[OGRESCENE]: Processing UploadScene packet with method {0}", method);

            // Dispatch
            switch (method)
            {
                case "Upload": 
                    return ProcessUploadScene(path, request, httpRequest, httpResponse);
                case "GetUploadSceneList": 
                    return ProcessGetUploadSceneList(path, request, httpRequest, httpResponse);
                case "DeleteServerScene": 
                    return ProcessDeleteScene(path, request, httpRequest, httpResponse);
                case "UnloadServerScene": 
                    return ProcessUnloadServerScene(path, request, httpRequest, httpResponse);
                case "LoadServerScene": 
                    return ProcessLoadServerScene(path, request, httpRequest, httpResponse);
                case "UploadSceneUrl":
                    return ProcessUploadSceneUrl(path, request, httpRequest, httpResponse);
                default: return null;
            }
        }

        private byte[] ProcessUploadSceneUrl(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                string regionName = httpRequest.Headers["RegionName"];
                string offset = httpRequest.Headers["OffSet"];
                string url = httpRequest.Headers["SceneUrl"];

                string[] uriParts = url.Split('/');
                string fileName = uriParts[uriParts.Length - 1];
                string urlBase = url.Remove(url.LastIndexOf(fileName));

                string[] offsetParts = offset.Replace(" ", String.Empty).Split(',');
                if (offsetParts.Length != 3)
                {
                    httpResponse.StatusCode = 400;
                    return new byte[0];
                }
                Vector3 offsetVector = new Vector3(
                    Convert.ToSingle(offsetParts[0]),
                    Convert.ToSingle(offsetParts[1]),
                    Convert.ToSingle(offsetParts[2]));

                if (HandleUploadSceneWithReferences(urlBase, fileName, offsetVector))
                {
                    httpResponse.StatusCode = 201;
                }
                else
                {
                    httpResponse.StatusCode = 500;
                }
                return new byte[0];
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failed to load scene: {0}\n"
                    + "StackTrace: {1}", e.Message, e.StackTrace);
                SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                errorMessage.Add("error", e.Message);
                return ConstructResponceBytesFromDictionary(errorMessage);
            }
        }

        private byte[] ProcessLoadServerScene(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                SerializableDictionary<string, string> responce = new SerializableDictionary<string, string>();
                // actually returns xmldata
                string loadscenexml = GetDataFromRequestBody(httpRequest);
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(loadscenexml);
                XmlNodeList regionNodeList = xDoc.GetElementsByTagName("region");
                XmlNodeList sceneuuidNodeList = xDoc.GetElementsByTagName("sceneuuid");
                string regionName = regionNodeList[0].InnerText;
                string sceneUuidStr = sceneuuidNodeList[0].InnerText;
                // positions and rotations are in xml scene that is in uploadscene table
                UploadScene us = m_SceneStorage.GetScene(sceneUuidStr);

                DotSceneLoader loader = new DotSceneLoader();
                SceneManager ogreSceneManager = new SceneManager();

                Scene scene = null;
                string regionId = String.Empty;
                foreach (Scene s in m_scenes)
                {
                    if (s.RegionInfo.RegionName == regionName)
                    {
                        scene = s;
                        regionId = s.RegionInfo.RegionID.ToString();
                        break;
                    }
                }
                if (scene != null)
                {
                    loader.ImportSceneFromString(us.XmlFile, ogreSceneManager);
                    List<SceneAsset> meshes = new List<SceneAsset>();
                    List<SceneAsset> sassets = m_SceneStorage.GetSceneAssets(sceneUuidStr);
                    Dictionary<string, UUID> materials = new Dictionary<string, UUID>();
                    foreach (SceneAsset sa in sassets)
                    {
                        if (sa.AssetType == 1)
                        {
                            meshes.Add(sa);
                        }
                        if (sa.AssetType == 2)
                        {
                            materials.Add(sa.Name, new UUID(sa.AssetId));
                        }
                    }
                    UploadSceneLoader usl = new UploadSceneLoader(scene, this.m_osi);
                    usl.AddObjectsToScene(ogreSceneManager.RootSceneNode, meshes, sceneUuidStr, materials, AssetDataSaver);
                }
                
                // push back meshes, that should be stored in asset service (the textures and material files should all be ready in asset service)
                m_SceneStorage.SetSceneToRegion(sceneUuidStr, regionId);
                responce["error"] = "None";
                return ConstructResponceBytesFromDictionary(responce);
            }
            catch (Exception exp)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failed to load scene: {0}\n"
                    + "StackTrace: {1}", exp.Message, exp.StackTrace);
                SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                errorMessage.Add("error", exp.Message);
                return ConstructResponceBytesFromDictionary(errorMessage);
            }
        }

        private byte[] ProcessUnloadServerScene(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                SerializableDictionary<string, string> responce = new SerializableDictionary<string, string>();
                string sceneUuidAndRegion = GetDataFromRequestBody(httpRequest);
                m_log.Debug(sceneUuidAndRegion);
                int ind = sceneUuidAndRegion.IndexOf(':');
                string uuid = sceneUuidAndRegion.Substring(0, ind);
                string region = sceneUuidAndRegion.Substring(ind + 1, sceneUuidAndRegion.Length - (ind + 1));
                m_log.Debug(uuid);
                m_log.Debug(region);
                UnloadServerScene(uuid, region);

                responce["error"] = "None";
                return ConstructResponceBytesFromDictionary(responce);

            }
            catch (Exception exp)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failed to unload scene: {0}\n"
                    + "StackTrace: {1}", exp.Message, exp.StackTrace);
                SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                errorMessage.Add("error", exp.Message);
                return ConstructResponceBytesFromDictionary(errorMessage);
            }
        }

        private byte[] ProcessDeleteScene(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                SerializableDictionary<string, string> responce = new SerializableDictionary<string, string>();
                string sceneUuidStr = GetDataFromRequestBody(httpRequest);

                // check if its active scene, if so do unload first for each region this scene is in then delete assets
                List<string> regions = m_SceneStorage.GetScenesRegionIds(sceneUuidStr);
                foreach (string region in regions)
                {
                    this.UnloadServerScene(sceneUuidStr, region);
                }
                // now del assets and remove asset references and finally scene
                List<SceneAsset> sassets = m_SceneStorage.GetSceneAssets(sceneUuidStr);
                foreach (SceneAsset asset in sassets)
                {
                    m_scenes[0].AssetService.Delete(asset.AssetId);
                }

                bool ret = m_SceneStorage.DeleteScene(sceneUuidStr);
                if (ret == true)
                {
                    responce["error"] = "None";
                }
                else
                {
                    responce["error"] = "Something went wrong";
                }
                return ConstructResponceBytesFromDictionary(responce);

            }
            catch (Exception exp)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failed to unload scene: {0}\n"
                    + "StackTrace: {1}", exp.Message, exp.StackTrace);
                SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                errorMessage.Add("error", exp.Message);
                return ConstructResponceBytesFromDictionary(errorMessage);
            }
        }

        private bool UnloadServerScene(string sceneuuid, string region)
        {
            // get scene meshes, remove them from scene
            List<SceneAsset> assets = this.m_SceneStorage.GetSceneAssets(sceneuuid);
            List<SceneAsset> meshes = new List<SceneAsset>();
            foreach (SceneAsset sa in assets)
            {
                if (sa.AssetType == 1)
                    meshes.Add(sa);
            }
            //get region where uploadscene is located
            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionName == region)
                {
                    try
                    {
                        lock (scene.Entities)
                        {
                            foreach (SceneAsset mesh_ in meshes)
                            {
                                EntityBase eb_ = scene.Entities[new UUID(mesh_.EntityId)];
                                if (eb_ == null)
                                {
                                    m_log.Debug(mesh_.EntityId.ToString() + " not found");
                                }
                            }

                            foreach (SceneAsset mesh in meshes)
                            {

                                EntityBase eb = scene.Entities[new UUID(mesh.EntityId)];

                                if (eb != null)
                                {
                                    SceneObjectGroup sog = scene.SceneGraph.GetGroupByPrim(eb.LocalId);
                                    m_log.Debug("--------------------------------------------");
                                    m_log.Debug("Removing " + sog.UUID.ToString());
                                    m_log.Debug("Removing " + mesh.EntityId);
                                    m_log.Debug("--------------------------------------------");

                                    //SceneObjectGroup sog = (SceneObjectGroup)eb;


                                    //scene.RemoveGroupTarget(sog);
                                    scene.DeleteSceneObject(sog, false);

                                }

                            }

                            string regionid = scene.RegionInfo.RegionID.ToString();
                            // remove scene from upload scene regionscene db table, leave to other tables since this is just unload
                            this.m_SceneStorage.RemoveSceneFromRegion(sceneuuid, regionid);
                            
                        }
                    }
                    catch (Exception exp)
                    {
                        m_log.Debug("----------------------------------");
                        m_log.ErrorFormat("[OGRESCENE]: Error unloading scene: {0}, {1}", exp.Message, exp.StackTrace);
                        m_log.Debug("----------------------------------");
                        //m_log.Debug(exp.StackTrace.ToString());
                        throw;
                    }
                }
            }
            

            return false;
        }

        private string GetDataFromRequestBody(OSHttpRequest httpRequest) 
        {
            byte[] data = httpRequest.GetBody();
            byte[] content = ReadContent(data);
            string retData;
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            retData = enc.GetString(content);
            int end = retData.IndexOf('\r');
            if (end != -1)
            {
                retData = retData.Remove(end);
            }
            return retData;
        }

        private byte[] ProcessGetUploadSceneList(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            try
            {
                SerializableDictionary<string, SceneRegion> sceneids = new SerializableDictionary<string, SceneRegion>();

                List<RegionScene> rscenes = m_SceneStorage.GetRegionSceneList();
                List<UploadScene> uscenes = m_SceneStorage.GetScenes();

                List<string> keys = new List<string>();
                int i = 0;
                foreach (RegionScene rs in rscenes)
                {
                    // Get region name 
                    string regionName = "";
                    foreach (Scene scene in m_scenes)
                    {
                        if (scene.RegionInfo.RegionID.ToString() == rs.RegionId)
                        {
                            regionName = scene.RegionInfo.RegionName;
                            break;
                        }
                    }
                    // Get scene name
                    string sceneName = "";
                    foreach (UploadScene us in uscenes)
                    {
                        if (us.SceneId == rs.SceneId)
                        {
                            sceneName = us.Name;
                            break;
                        }
                    }

                    SceneRegion sr = new SceneRegion();
                    sr.Region = regionName;
                    sr.SceneName = sceneName;
                    sr.SceneUuid = rs.SceneId;
                    keys.Add(rs.SceneId);
                    sceneids[i.ToString()] = sr;
                    i++;
                }
                List<UploadScene> allScenes = m_SceneStorage.GetScenes();
                // add unloaded scenes
                foreach (UploadScene us in allScenes)
                {
                    if (!keys.Contains(us.SceneId))
                    {
                        SceneRegion sr = new SceneRegion();
                        sr.Region = "";
                        sr.SceneName = us.Name;
                        sr.SceneUuid = us.SceneId;
                        sceneids[i.ToString()] = sr;
                        i++;
                    }
                }

                return ConstructResponceBytesFromDictionary(sceneids);

            }
            catch (Exception exp)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failed to create list of uploaded scenes: {0}\n"
                    +"StackTrace: {1}", exp.Message, exp.StackTrace);
                SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                errorMessage.Add("error", exp.Message);
                return ConstructResponceBytesFromDictionary(errorMessage);
            }
        }

        private bool HandleUploadSceneWithReferences(string basePath, string sceneFileName, Vector3 offset)
        {
            Uri sceneFileUri = new Uri(basePath + sceneFileName);
            WebClient client = new WebClient();
            byte[] sceneFile = client.DownloadData(sceneFileUri);
            
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            string scenexml = enc.GetString(sceneFile);

            m_osi.ImportUploadedOgreScene(basePath, scenexml, m_scene, offset);

            return true;
        }

        private byte[] ProcessUploadScene(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] data = httpRequest.GetBody();

            string regionName = httpRequest.Headers["RegionName"];
            string publishName = httpRequest.Headers["PublishName"];

            bool regionFound = false;

            if (regionName != null)
            {
                foreach (Scene sc in m_scenes)
                {
                    if (sc.RegionInfo.RegionName == regionName)
                    {
                        m_scene = sc;
                        regionFound = true;
                        break;
                    }
                }
            }

            if (regionFound)
            {            
                string outputFile = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "SceneUploadZipFile.zip";

                m_log.Info("[OGRESCENE]: Writing zip file");

                try
                {
                    // need to parse these out from beginning
                    //--0446c18b14c143c3bd0e787cb6ea4c09
                    //Content-Disposition: form-data; name="uploadscene"; filename="C:/CODE/NaaliGit2/naali/bin/test3.scene.zip"
                    //Content-Type: application/zip
                    //Content-Length: 10144449

                    // need to parse this out from end
                    //--0446c18b14c143c3bd0e787cb6ea4c09--

                    byte[] content = ReadContent(data);

                    BinaryWriter bw = null;
                    try
                    {
                        bw = new BinaryWriter(File.Open(outputFile, FileMode.Create));
                        bw.Write(content);
                        bw.Flush();
                        bw.Close();

                    }
                    catch (Exception)
                    {
                        if (bw != null) { bw.Close(); }
                        throw;
                    }
                    RemoveFolder(EXTRACT_FOLDER_NAME);

                    Ionic.Zip.ZipFile f = null;
                    try
                    {
                        f = Ionic.Zip.ZipFile.Read("SceneUploadZipFile.zip");
                        f.ExtractAll(EXTRACT_FOLDER_NAME);
                        f.Dispose();

                    }
                    catch (Exception)
                    {
                        if (f != null)
                            f.Dispose();
                        throw;
                    }
                    string error = "";
                    LoadScene(EXTRACT_FOLDER_NAME, publishName, out error);

                    if (error == "")
                    {
                        //string respstring = "<Root>";
                        //respstring += "Scene upload done";
                        //respstring += "</Root>";
                        //System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                        //RemoveFolder(EXTRACT_FOLDER_NAME);
                        //return encoding.GetBytes(respstring);
                        SerializableDictionary<string, string> resp = new SerializableDictionary<string, string>();
                        resp.Add("error", "None");
                        return ConstructResponceBytesFromDictionary(resp);


                    }
                    else {
                        SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                        errorMessage.Add("error", error);
                        return ConstructResponceBytesFromDictionary(errorMessage);                        
                    }
                }
                catch (Exception exp)
                {
                    RemoveFolder(EXTRACT_FOLDER_NAME);
                    
                    m_log.ErrorFormat("[OGRESCENE]: Failing to parse upload scene package: {0}\n"
                        +"StackTrace: {1}", exp.Message, exp.StackTrace);
                    SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                    errorMessage.Add("error", exp.Message);
                    return ConstructResponceBytesFromDictionary(errorMessage);

                }
            }
            else {
                SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
                errorMessage.Add("error", "No such region found");
                return ConstructResponceBytesFromDictionary(errorMessage);
            }
        }

        private void RemoveFolder(string folder)
        {
            //string path = Path.Combine(Directory.GetCurrentDirectory(), folder);
            //if (System.IO.Directory.Exists(path))
            //    System.IO.Directory.Delete(path, true);

            string path = Path.Combine(Directory.GetCurrentDirectory(), folder);
            if (System.IO.Directory.Exists(path))
            {
                //    System.IO.Directory.Delete(path, true);

                string[] files = Directory.GetFiles(folder);
                string[] dirs = Directory.GetDirectories(folder);

                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                foreach (string dir in dirs)
                {
                    RemoveFolder(dir);
                }

                Directory.Delete(folder, false);
            }
        }

        private bool CheckRights(UUID agentID)
        {
            // currently only owner is able to upload scenes
            if (agentID == m_scene.RegionInfo.EstateSettings.EstateOwner)
                return true;
            UUID[] managers = m_scene.RegionInfo.EstateSettings.EstateManagers;
            foreach (UUID id in managers) 
            {
                if (id == agentID)
                    return true;
            }

            return false;
        }

        private byte[] ReadContent(byte[] data)
        {
            MemoryStream mstream = new MemoryStream(data);
            System.IO.BinaryReader reader = new BinaryReader(mstream);
            //reader.BaseStream.Position
            byte[] startBytes;
            if (mstream.Length > 500)
            {
                startBytes = reader.ReadBytes(500);
            }
            else
            {
                startBytes = reader.ReadBytes((int)mstream.Length);
            }
            string startString = System.Text.Encoding.ASCII.GetString(startBytes).ToString();

            // find 2 linefeeds in a row, marks the end of headers
            int startIndex = 0;
            bool crlf = false;
            int LF_index = startString.IndexOf("\n", 0);
            int CRLF_index = startString.IndexOf("\r\n", 0);
            if (CRLF_index != -1)
            {
                crlf = true;
            }

            if (crlf)
            {
                startIndex = startString.IndexOf("\r\n\r\n", 38);
            }
            else
            {
                startIndex = startString.IndexOf("\n\n", 36);
            }
            
            int contentStartIndex;
            if(crlf)
                contentStartIndex= startIndex + 4;
            else
                contentStartIndex = startIndex + 2;
            int contentEndIndex = (int)mstream.Length - 38; // 38 = length of "--uuid--"
            int byteCount = contentEndIndex - contentStartIndex;
            byte[] fileBuffer = new byte[byteCount];

            Buffer.BlockCopy(data, contentStartIndex, fileBuffer, 0, byteCount);

            return fileBuffer;
        }

        private void LoadScene(string extractFolderName, string publishName, out string error)
        {
            error = "";
            m_log.Info("[OGRESCENE]: LoadScene");
            string packagePath = Path.Combine(extractFolderName, "UploadPackage");
            DirectoryInfo di = new DirectoryInfo(packagePath);
            // find .scene file, should be only one file
            FileInfo fi = di.GetFiles("*.scene")[0];
            string path = fi.FullName;
            
            string loadName = path.Substring(0, path.Length - 6);
            m_log.InfoFormat("[OGRESCENE]: Loading scene file: {0}", fi.FullName);
            CreateSceneMaterialFileIfNeeded(fi.Name, di);
            FileStream fs;
            StreamReader sr = null;
            try
            {
                UUID sceneId = UUID.Random();
                m_osi.ImportUploadedOgreScene(loadName, m_scene, sceneId);
                // get scene file
                fs = File.OpenRead(path);
                sr = new StreamReader(fs);
                string xml = sr.ReadToEnd();
                UploadScene us;
                if (publishName == null)
                {
                    us = new UploadScene(sceneId, fi.Name, xml);
                }
                else
                {
                    us = new UploadScene(sceneId, publishName, xml);
                }
                this.m_SceneStorage.SaveScene(us);
                m_SceneStorage.SetSceneToRegion(sceneId.ToString(), m_scene.RegionInfo.RegionID.ToString());
                sr.Close();
            }
            catch (Exception exp)
            {
                if (sr != null)
                {
                    sr.Close();
                }
                m_log.ErrorFormat("[OGRESCENE]: Error importing uploaded ogre scene: {0}, {1}", exp.Message, exp.StackTrace);
                error = exp.Message;                
            }
        }

        private void CreateSceneMaterialFileIfNeeded(string scene, DirectoryInfo di)
        {
            string materialname = scene.Substring(0, scene.Length - 6) + ".material";
            bool exist = false;
            foreach (FileInfo file in di.GetFiles())
            {
                if (file.Name == materialname) { exist = true; break; }
            }
            if (!exist)
            {
                FileStream fs = null;
                // create scene material file from other material files
                try
                {
                    string dirPath = di.FullName;
                    m_log.InfoFormat("[OGRESCENE]: Creating material file: {0}", Path.Combine(dirPath, materialname));
                    fs = File.Create(Path.Combine(dirPath, materialname));
                    StreamWriter mwriter = new StreamWriter(fs);
                    foreach (FileInfo mfile in di.GetFiles("*.material"))
                    {
                        if (mfile.Name != materialname)
                        {
                            m_log.InfoFormat("[OGRESCENE]: Adding material file: {0}", mfile.Name);
                            FileStream mfs = mfile.Open(FileMode.Open);
                            StreamReader sr = new StreamReader(mfs);
                            string line;
                            try
                            {
                                while ((line = sr.ReadLine()) != null)
                                {
                                    if (line.StartsWith("material"))
                                    {
                                        string[] spl = line.Split(':');
                                        if (spl.Length > 1)
                                        {
                                            // Try reading the inherited material from OgreMaterials folder and add it to stream
                                            // Try reading the inherited material from upload folder and add it to stream
                                            string baseMaterialContent = ReadBaseMaterial(spl[1].Trim(), dirPath);
                                            line = spl[0].Trim();
                                            mwriter.WriteLine(line);
                                            if ((line = sr.ReadLine()).StartsWith("{"))
                                            {
                                                mwriter.WriteLine(line);
                                            }
                                            else
                                            {
                                                throw new Exception("malformed .material file");
                                            }
                                            mwriter.WriteLine(baseMaterialContent);
                                        }
                                        else
                                        {
                                            mwriter.WriteLine(line);
                                        }
                                    }
                                    else
                                    {
                                        mwriter.WriteLine(line);
                                    }
                                }
                                mwriter.Flush();
                                mfs.Close();
                            }
                            catch (Exception exp)
                            {
                                m_log.WarnFormat("[OGRESCENE]: Warning, Error while creating material file {0}" 
                                     + "StackTrace {1}", exp.Message, exp.StackTrace);
                                if (mfs != null)
                                {
                                    mfs.Close();
                                }
                            }                                                        
                        }
                    }
                    mwriter.Flush();
                    mwriter.Close();

                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[OGRESCENE]: Failing to create material file: {0}" +
                                      "StackTrace: {1}", e.Message, e.StackTrace);
                    if(fs!=null)
                        fs.Close();
                }
            }
        }

        private string ReadBaseMaterial(string name, string dirPath)
        {
            string content = "";
            StreamReader sr = null;
            try
            {
                string path = Path.Combine(dirPath, name + ".material");
                if (File.Exists(path))
                {
                    sr = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read));
                    content = sr.ReadToEnd();
                    content = ParseInnerContent(content, name);
                    sr.Close();

                }
            }
            catch (Exception exp)
            {
                m_log.WarnFormat("Failing to read basematerial {0} \n" +
                    "StackTrace: {1} \n" +
                    "returning empty string", exp.Message, exp.StackTrace);
                if (sr != null)
                {
                    sr.Close();
                }
            }
            return content;
        }

        private string ParseInnerContent(string content, string name)
        {
            int materialIndex = content.IndexOf("material " + name);
            int start = content.IndexOf("{", materialIndex);
            int stop = content.LastIndexOf("}");
            int len = stop - start;
            return content.Substring(start + 1, len - 2);
        }

        
        private byte[] ConstructJSonResponce(Object obj)
        {
            //Newtonsoft.Json.JsonConverter jc = new Newtonsoft.Json.JsonConverter();
            ////jc.WriteJson(
            //string ans = JsonConvert.SerializeObject(eList, Formatting.Indented);

            return null;
        }

        //private byte[] ConstructResponceBytesFromStringDictionary(SerializableDictionary<string, string> dictionary)
        //{
        //    //System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<UploadScene>));
        //    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializableDictionary<string, string>));
        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
        //    StringWriter sw = new StringWriter(sb);
        //    serializer.Serialize(sw, dictionary);

        //    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
        //    return encoding.GetBytes(sb.ToString());
        //}

        //private byte[] ConstructResponceBytesFromStringSceneRegionDictionary(SerializableDictionary<string, SceneRegion> sceneids)
        private byte[] ConstructResponceBytesFromDictionary<T, Y>(SerializableDictionary<T, Y> dictionary)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializableDictionary<T, Y>));
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            StringWriter sw = new StringWriter(sb);
            serializer.Serialize(sw, dictionary);
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(sb.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assets"></param>
        /// <param name="sceneDataID"></param>
        /// <param name="type">1 = mesh, 0 = other</param>
        public void SaveDataFromAssets(Dictionary<string, UUID> assets, UUID sceneDataID, int type)
        {
            foreach (KeyValuePair<string, UUID> kvp in assets)
            {
                AssetDataSaver.SaveAssetData(sceneDataID, kvp.Value, kvp.Key, type);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="sceneDataID"></param>
        /// <param name="type">1 = mesh, 0 = other</param>
        public void SaveAssetData(AssetBase asset, UUID sceneDataID, int type, SceneObjectGroup sceneObject)
        {
            AssetDataSaver.SaveAssetData(sceneDataID, asset.FullID, asset.Name, type, 0, sceneObject.UUID);
        }

    }

    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {

        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }


        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));


            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();


            if (wasEmpty)
                return;


            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");


                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();


                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);

                reader.ReadEndElement();

                this.Add(key, value);


                reader.ReadEndElement();
                reader.MoveToContent();

            }
            reader.ReadEndElement();
        }


        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");


                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();

            }

        }

        #endregion
    }
}
