using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Framework;
using System.Drawing;
using ModularRex.RexFramework;
using OgreSceneImporter.UploadSceneDB;

namespace OgreSceneImporter
{
    public class OgreSceneImportModule : IRegionModule
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_scenes = new List<Scene>();
        private Scene m_scene;
        private float m_objectScale = 1.0f;
        private Vector3 m_offset = Vector3.Zero;
        private bool m_swapAxes = false;
        private bool m_useCollisionMesh = true;
        private float m_sceneRotation = 0.0f;
        private NHibernateSceneStorage m_SceneStorage;

        private Dictionary<UUID, RegisterCaps> m_scene_caps = new Dictionary<UUID, RegisterCaps>();

        #region IRegionModule Members

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scenes.Add(scene);
            scene.AddCommand(this, "ogrescene", "ogrescene <action> <filename>", "Type \"ogrescene help\" to view longer help", OgreSceneCommand);

            if (SceneStorageEnabled(source))
            {
                m_SceneStorage = new NHibernateSceneStorage(source);
            }
        }

        public void PostInitialise()
        {
            foreach (Scene s in m_scenes)
            {
                m_scene_caps[s.RegionInfo.RegionID] = new RegisterCaps(s, this);
            }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "OgreSceneImportModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion

        private bool SceneStorageEnabled(Nini.Config.IConfigSource config)
        {
            bool enabled = true;
            Nini.Config.IConfig serverConfig = config.Configs["UploadSceneConfig"];
            if (serverConfig == null)
            {
                m_log.Info("[OGRESCENEUPLOADSTORE]: No configuration found, module is disabled");
                enabled = false;
            }
            else if (!serverConfig.Contains("ConnectionString"))
            {
                m_log.Info("[OGRESCENEUPLOADSTORE]: No configuration found, module is disabled");
                enabled = false;
            }
            return enabled;
        }

        private void OgreSceneCommand(string module, string[] cmdparams)
        {
            if (OpenSim.Framework.Console.MainConsole.Instance.ConsoleScene != null && OpenSim.Framework.Console.MainConsole.Instance.ConsoleScene is Scene)
            {
                m_scene = (Scene)OpenSim.Framework.Console.MainConsole.Instance.ConsoleScene;
                //current scene to m_scene for time when console comman is processed. This is because current scene can change in between of processing
            }
            else
            {
                if (m_scenes.Count == 1)
                    m_scene = m_scenes[0];
                else
                {
                    m_log.ErrorFormat("[OGRESCENE]: More than one scene in region. Set current scene with command \"change region <region name>\"");
                    return;
                }
            }

            try
            {
                bool showHelp = false;
                if (cmdparams.Length > 1)
                {
                    string command = cmdparams[1].ToLower(); //[0] == ogrescene
                    switch (command)
                    {
                        case "help":
                            showHelp = true;
                            break;
                        case "import":
                            ImportOgreScene(cmdparams[2], UUID.Zero);
                            break;
                        case "collisionmesh":
                            if (cmdparams.Length == 2)
                                m_log.Info("[OGRESCENE]: Current use collision meshes setting is " + m_useCollisionMesh.ToString());                                
                            else
                                SetCollisionMesh(cmdparams[2]);
                            break;
                        case "swapaxes":
                            if (cmdparams.Length == 2)
                                m_log.Info("[OGRESCENE]: Current swap Y/Z axes setting is " + m_swapAxes.ToString());
                            else 
                                SetSwapAxes(cmdparams[2]);
                            break;
                        case "scale":
                            if (cmdparams.Length == 2)
                                m_log.Info("[OGRESCENE]: Current import scale is " + m_objectScale.ToString());
                            else 
                                SetScale(cmdparams[2]);
                            break;
                        case "rotation":
                            if (cmdparams.Length == 2)
                                m_log.Info("[OGRESCEEN]: Current import rotation (around Z axis) is " + m_sceneRotation.ToString() + " degrees");
                            else
                                SetRotation(cmdparams[2]);
                            break;
                        case "offset":
                            if (cmdparams.Length == 2)
                                m_log.Info("[OGRESCENE]: Current import offset is " + m_offset.ToString());
                            else
                            {
                                try
                                {
                                    string[] vectParts = cmdparams[2].Split(',');
                                    Vector3 newOffset = new Vector3(
                                        Convert.ToSingle(vectParts[0]),
                                        Convert.ToSingle(vectParts[1]),
                                        Convert.ToSingle(vectParts[2]));
                                    m_offset = newOffset;
                                }
                                catch (Exception)
                                {
                                    m_log.ErrorFormat("[OGRESCENE]: Could not parse new offset vector {0}", cmdparams[2]);
                                }
                            }
                            break;
                        case "load":
                            if (cmdparams.Length == 2)
                            {
                                m_log.Info("[OGRESCENE]: Missing argument. Use command with url location to scene file");
                            }
                            else if (cmdparams.Length == 3)
                            {
                                string url = cmdparams[2];
                                string[] uriParts = url.Split('/');
                                string fileName = uriParts[uriParts.Length - 1];
                                string urlBase = url.Remove(url.LastIndexOf(fileName));
                                Uri sceneFileUri = new Uri(cmdparams[2]);
                                System.Net.WebClient client = new System.Net.WebClient();
                                byte[] sceneFile = client.DownloadData(sceneFileUri);

                                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                                string scenexml = enc.GetString(sceneFile);

                                ImportUploadedOgreScene(urlBase, scenexml, m_scene, m_offset);
                            }
                            break;
                        default:
                            showHelp = true;
                            break;
                    }
                }
                else showHelp = true;
                
                if (showHelp)
                    m_log.Info("[OGRESCENE]: Available commands: import offset rotation scale swapaxes collisionmesh load");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[OGRESCENE]: Failed to execute ogrescene command. Exception {0} was thrown.", e);
            }

            m_scene = null;
        }

        public float ToRadians(double degrees)
        {
            return (float)(Math.PI * degrees / 180);
        }

        private void SetSwapAxes(string p)
        {
            try
            {
                bool newSwapAxes = Convert.ToBoolean(p);
                m_swapAxes = newSwapAxes;
            }
            catch (Exception)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error parsing swapaxes from value {0}", p);
            }
        }
                
        private void SetCollisionMesh(string p)
        {
            try
            {
                bool newUseCollisionMesh = Convert.ToBoolean(p);
                m_useCollisionMesh = newUseCollisionMesh;
            }
            catch (Exception)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error parsing collisionmesh from value {0}", p);
            }
        }

        private void SetRotation(string p)
        {
            try
            {
                float newRotation = Convert.ToSingle(p);
                m_sceneRotation = newRotation;
            }
            catch (Exception)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error parsing rotation from value {0}", p);
            }
        }         
                
        private void SetScale(string p)
        {
            try
            {
                float newScale = Convert.ToSingle(p);
                m_objectScale = newScale;
            }
            catch (Exception)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error parsing scale from value {0}", p);
            }
        }

        private void ImportOgreScene(string fileName, UUID saveSceneDataID)
        {
            DotSceneLoader loader = new DotSceneLoader();
            SceneManager ogreSceneManager = new SceneManager();
            
            if (System.IO.File.Exists(fileName + ".scene"))
                loader.ParseDotScene(fileName + ".scene", "General", ogreSceneManager);
            else
            {
                m_log.ErrorFormat("[OGRESCENE]: Could not find scene file {0}.scene", fileName);
                return;
            }
            
            //Load&parse materials & textures
            //check that file exists
            if (!System.IO.File.Exists(fileName + ".material"))
            {
                m_log.ErrorFormat("[OGRESCENE]: Could not find material file {0}.material", fileName);
                return;
            }
            System.IO.StreamReader sreader = System.IO.File.OpenText(fileName+".material");
            string data = sreader.ReadToEnd();
            sreader.Close();
            OgreMaterialParser parser = new OgreMaterialParser(m_scene);
            string filepath = PathFromFileName(fileName);
            Dictionary<string, UUID> materials = null;
            Dictionary<string, UUID> textures = null;
            if (!parser.ParseAndSaveMaterial(data, out materials, out textures))
            {
                m_log.Error("[OGRESCENE]: Material parsing failed. Ending operation");
                return;
            }

            if (!LoadAndSaveTextures(textures, filepath))
            {
                m_log.ErrorFormat("[OGRESCENE]: Aborting ogre scene importing, because there were some errors in loading textures");
                return;
            }

            if (saveSceneDataID != UUID.Zero) // if true save data for later unloading, loading, removing of loaded scene assets
            {
                SaveDataFromAssets(materials, saveSceneDataID, 2);
                SaveDataFromAssets(textures, saveSceneDataID, 3);
            }

            m_log.InfoFormat("[OGRESCENE]: Found and loaded {0} materials and {1} textures", materials.Count, textures.Count);

            //Load&parse meshes and add them to scene
            m_log.Info("[OGRESCENE]: Loading OGRE stuff to scene");

            AddObjectsToScene(ogreSceneManager.RootSceneNode, materials, filepath, saveSceneDataID);
        }

        /// <summary>
        /// Saves assets name/UUID dictionary to db.
        /// </summary>
        /// <param name="assets">The assets name/UUID dictionary.</param>
        /// <param name="sceneDataID">The scene data ID.</param>
        /// <param name="type">Asset type: 1 = mesh, 0 = other</param>
        private void SaveDataFromAssets(Dictionary<string, UUID> assets, UUID sceneDataID, int type)
        {
            NHibernateSceneStorage storage;
            if (TryGetSceneStorage(out storage))
            {
                foreach (KeyValuePair<string, UUID> kvp in assets)
                {
                    storage.SaveAssetData(sceneDataID, kvp.Value, kvp.Key, type);
                }
            }
        }

        private bool LoadAndSaveTextures(Dictionary<string, UUID> textures, string additionalPath)
        {
            foreach (KeyValuePair<string, UUID> texture in textures)
            {
                try
                {
                    //check that file exists
                    bool usePath = false;
                    string p = System.IO.Path.Combine(additionalPath, texture.Key);
                    if (!System.IO.File.Exists(texture.Key) && System.IO.File.Exists(p)) { usePath = true; }

                    if (!System.IO.File.Exists(texture.Key)&&usePath==false)
                    {
                        m_log.ErrorFormat("[OGRESCENE]: Could not load file {0}, because it doesn't exist in working directory", texture.Key);
                        continue;
                    }

                    //Load file
                    byte[] data;
                    if (!usePath)
                    {
                        data = System.IO.File.ReadAllBytes(texture.Key);
                    }
                    else
                    {
                        data = System.IO.File.ReadAllBytes(p);
                    }

                    //resize asset if needed
                    System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
                    Bitmap image = new Bitmap(stream);
                    if (Math.Log(image.Width, 2) != 2 ||
                        Math.Log(image.Height, 2) != 2)
                    {
                        //image width or height is not power of 2.
                        //image needs to be resized
                        Size newSize = new Size();
                        double wdthLog = Math.Round(Math.Log(image.Width, 2), 0);
                        double hghtLog = Math.Round(Math.Log(image.Height, 2), 0);
                        newSize.Width = (int)Math.Pow(2, wdthLog);
                        newSize.Height = (int)Math.Pow(2, hghtLog);

                        image = ResizeImage(image, newSize);
                    }

                    //encode image to j2k
                    data = OpenMetaverse.Imaging.OpenJPEG.EncodeFromImage(image, true);

                    //Create asset
                    AssetBase asset = new AssetBase(texture.Value, texture.Key, (sbyte)AssetType.Texture);
                    asset.Data = data;

                    m_scene.AssetService.Store(asset);
                    
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("[OGRESCENE]: Could not load texture {0}, because {1}", texture.Key, e);
                    continue;
                }
            }
            return true;
        }

        public Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            int destWidth = size.Width;
            int destHeight = size.Height;

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return b;
        }

        private void AddObjectsToScene(SceneNode node, Dictionary<string, UUID> materials, string additionalSearchPath, UUID sceneDataID)
        {
            // Make sure node global transform is refreshed
            node.RefreshDerivedTransform();

            if (node.Entities.Count >= 0) //add this to scene and do stuff
            {
                foreach (Entity ent in node.Entities)
                {
                    //first check that file exists
                    bool usePath = false;
                    string p = System.IO.Path.Combine(additionalSearchPath, ent.MeshName);
                    if (!System.IO.File.Exists(ent.MeshName) && System.IO.File.Exists(p)) { usePath = true; }

                    if (!System.IO.File.Exists(ent.MeshName) && !System.IO.File.Exists(p))
                    {
                        m_log.ErrorFormat("[OGRESCENE]: Could not find mesh file {0}. Skipping", ent.MeshName);
                        continue;
                    }

                    //Load mesh object
                    byte[] data;
                    if (!usePath)
                        data = System.IO.File.ReadAllBytes(ent.MeshName);
                    else
                        data = System.IO.File.ReadAllBytes(p);

                    //Add mesh to asset db
                    AssetBase asset = new AssetBase(UUID.Random(), ent.MeshName, 43);
                    asset.Description = ent.Name;
                    asset.Data = data;
                    m_scene.AssetService.Store(asset);

                    //Read material names
                    List<string> materialNames;
                    string meshLoaderError;
                    RexDotMeshLoader.DotMeshLoader.ReadDotMeshMaterialNames(data, out materialNames, out meshLoaderError);
                    if (meshLoaderError != "")
                    {
                        //probably error in the mesh. this can't be fixed.
                        //setting this to physics engine could have devastating effect.
                        //must skip this object
                        m_log.ErrorFormat("[OGRESCENE]: Error occurred while parsing material names from mesh {0}. Error message {1}", ent.MeshName, meshLoaderError);
                    }

                    SceneObjectGroup sceneObject = AddObjectToScene(node, ent);
                    if (sceneObject != null)
                    {
                        if (sceneDataID != UUID.Zero)
                        {
                            NHibernateSceneStorage storage;
                            if (TryGetSceneStorage(out storage))
                            {
                                storage.SaveAssetData(sceneDataID, asset.FullID, asset.Name, 1, 0, sceneObject.UUID); // Store mesh data from uploaded scene
                            }
                        }
                        bool useCollision = (meshLoaderError == "") && (m_useCollisionMesh == true);
                        //Add refs to materials, mesh etc.
                        AddRexObjectProperties(sceneObject.RootPart.UUID, asset.FullID, ent, materials, materialNames, useCollision);
                    }
                }
            }

            if (node.Children.Count >= 0)
            {
                foreach (SceneNode child in node.Children)
                {
                    AddObjectsToScene(child, materials, additionalSearchPath, sceneDataID);
                }
            }
        }

        public void AddObjectsToScene(Scene scene, SceneNode node, List<SceneAsset> meshes, string uploadsceneid, Dictionary<string, UUID> materials)
        {
            Scene oldScene = m_scene;
            m_scene = scene;

            if (node.Entities.Count >= 0)
            {
                foreach (Entity ent in node.Entities)
                {
                    // here we should read mesh assetservice, first we need to get saved identifier from db, identified by upload scene and name
                    string meshName = ent.MeshName;

                    SceneAsset sa = Util.GetWithNameFromList(ent.MeshName, meshes);
                    if (sa != null)
                    {
                        byte[] data = m_scene.AssetService.GetData(sa.AssetId.ToString()); // mesh data

                        List<string> materialNames;
                        string meshLoaderError;
                        RexDotMeshLoader.DotMeshLoader.ReadDotMeshMaterialNames(data, out materialNames, out meshLoaderError);
                        if (meshLoaderError != "")
                        {
                            //probably error in the mesh. this can't be fixed.
                            //setting this to physics engine could have devastating effect.
                            //must skip this object
                            m_log.ErrorFormat("[OGRESCENE]: Error occurred while parsing material names from mesh {0}. Error message {1}", ent.MeshName, meshLoaderError);
                        }

                        SceneObjectGroup sceneObject = AddObjectToScene(node, ent);
                        if (sceneObject != null)
                        {
                            NHibernateSceneStorage storage;
                            if (TryGetSceneStorage(out storage))
                            {
                                storage.UpdateAssetEntityId(new UUID(sa.SceneId), new UUID(sa.AssetId), sceneObject.UUID);
                            }

                            bool useCollision = (meshLoaderError == "") && (m_useCollisionMesh == true);
                            //Add refs to materials, mesh etc.
                            AddRexObjectProperties(sceneObject.RootPart.UUID, new UUID(sa.AssetId), ent, materials, materialNames, useCollision);
                        }
                    }
                }
            }
            if (node.Children.Count >= 0)
            {
                foreach (SceneNode child in node.Children)
                {
                    AddObjectsToScene(scene, child, meshes, uploadsceneid, materials);
                }
            }

            m_scene = oldScene;
        }

        private Vector3 GetObjectScale(Vector3 vector3)
        {
            Vector3 newScale = new Vector3();
            newScale.X = vector3.X * m_objectScale;
            newScale.Y = vector3.Y * m_objectScale;
            newScale.Z = vector3.Z * m_objectScale;
            if (m_swapAxes == true)
            {
                Vector3 temp = new Vector3(newScale);
                newScale.X = temp.X;
                newScale.Y = temp.Z;
                newScale.Z = temp.Y;
            }
            return newScale;
        }

        private Quaternion GetSceneObjectRotation(Quaternion quaternion, Quaternion sceneRotQuat)
        {
            Quaternion rot = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
            if (m_swapAxes == true)
            {
                Vector3 temp = new Vector3(rot.X, rot.Y, rot.Z);
                rot.X = -temp.X;
                rot.Y = temp.Z;
                rot.Z = temp.Y;
            }
            else
            {
                // Do the rotation adjust as in original importer
                rot *= new Quaternion(1, 0, 0);
                rot *= new Quaternion(0, 1, 0);
            }
            rot = sceneRotQuat * rot;
            return rot;
        }

        private Vector3 GetSceneObjectPosition(Vector3 nodePos, Quaternion sceneRotQuat)
        {
            Vector3 objPos = new Vector3(nodePos.X, nodePos.Y, nodePos.Z);
            if (m_swapAxes == true)
            {
                Vector3 temp = new Vector3(objPos);
                objPos.X = -temp.X;
                objPos.Y = temp.Z;
                objPos.Z = temp.Y;
            }
            objPos = objPos * sceneRotQuat; // Apply scene rotation
            objPos = (objPos * m_objectScale) + m_offset; // Apply scale and add offset

            return objPos;
        }

        private SceneObjectGroup AddObjectToScene(SceneNode node, Entity ent)
        {
            // Quaternion for whole scene rotation
            Quaternion sceneRotQuat = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), ToRadians(m_sceneRotation));

            Vector3 objPos = GetSceneObjectPosition(node.DerivedPosition, sceneRotQuat);
            if (objPos.X >= 0 && objPos.Y >= 0 && objPos.Z >= 0 &&
                objPos.X <= 256 && objPos.Y <= 256)
            {
                if (objPos.Z < 20)
                    m_log.WarnFormat("[OGRESCENE]: Inserting object {1} to height {0}. This object might be under water", objPos.Z, ent.MeshName);

                //Add object to scene
                Quaternion rot = GetSceneObjectRotation(node.DerivedOrientation, sceneRotQuat);

                SceneObjectGroup sceneObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID,
                    m_scene.RegionInfo.MasterAvatarAssignedUUID, objPos, rot, PrimitiveBaseShape.CreateBox());

                sceneObject.RootPart.Scale = GetObjectScale(node.DerivedScale);
                sceneObject.Name = ent.Name;
                return sceneObject;
            }
            else
            {
                m_log.ErrorFormat("[OGRESCENE]: Node postion was not inside the scene. Skipping object {0} with position {1}", ent.MeshName, objPos.ToString());
                return null;
            }
        }

        private void AddObjectsToScene(SceneNode node, string baseUrl)
        {
            // Quaternion for whole scene rotation
            Quaternion sceneRotQuat = Quaternion.CreateFromAxisAngle(new Vector3(0,0,1), ToRadians(m_sceneRotation));

            // Make sure node global transform is refreshed
            node.RefreshDerivedTransform();

            if (node.Entities.Count >= 0) //add this to scene and do stuff
            {
                foreach (Entity ent in node.Entities)
                {
                    System.Net.WebClient client = new System.Net.WebClient();
                    byte[] data = client.DownloadData(baseUrl + ent.MeshName);
                    List<string> materialNames;
                    string meshLoaderError;
                    UUID collisionId = UUID.Zero;
                    RexDotMeshLoader.DotMeshLoader.ReadDotMeshMaterialNames(data, out materialNames, out meshLoaderError);
                    if (meshLoaderError != "")
                    {
                        m_log.ErrorFormat("[OGRESCENE]: Error occurred while parsing material names from mesh {0}. Error message {1}", ent.MeshName, meshLoaderError);
                    }
                    else
                    {
                        //Add mesh to asset db
                        AssetBase asset = new AssetBase(UUID.Random(), ent.MeshName, 43);
                        asset.Description = ent.Name;
                        asset.Data = data;
                        m_scene.AssetService.Store(asset);
                        collisionId = asset.FullID;
                    }

                    SceneObjectGroup sceneObject = AddObjectToScene(node, ent);
                    if (sceneObject != null)
                    {
                        //Add refs to materials, mesh etc.
                        AddRexObjectProperties(sceneObject.RootPart.UUID, baseUrl, ent, materialNames, collisionId);
                    }
                }
            }

            if (node.Children.Count >= 0)
            {
                foreach (SceneNode child in node.Children)
                {
                    AddObjectsToScene(child, baseUrl);
                }
            }
        }

        private RexObjectProperties CreateRexObjectProperties(UUID objectId, Entity ent)
        {
            IModrexObjectsProvider rexObjects = m_scene.RequestModuleInterface<IModrexObjectsProvider>();
            RexObjectProperties robject = rexObjects.GetObject(objectId);
            robject.RexDrawDistance = ent.RenderingDistance;
            robject.RexCastShadows = ent.CastShadows;
            robject.RexDrawType = 1;
            return robject;
        }

        private void AddRexObjectProperties(UUID objectId, UUID meshId, Entity ent, Dictionary<string, UUID> materials, List<string> materialNames, bool useCollision)
        {
            RexObjectProperties robject = CreateRexObjectProperties(objectId, ent);
            robject.RexMeshUUID = meshId;

            if (useCollision)
            {
                try
                {
                    robject.RexCollisionMeshUUID = meshId;
                }
                catch (Exception)
                {
                }
            }

            for (int i = 0; i < materialNames.Count; i++)
            {
                UUID materilUUID;
                if (materials.TryGetValue(materialNames[i], out materilUUID))
                {
                    robject.RexMaterials.AddMaterial((uint)i, materilUUID);
                }
                else
                {
                    m_log.ErrorFormat("[OGRESCENE]: Could not find material UUID for material {0}. Skipping material", materialNames[i]);
                    continue;
                }
            }
        }

        private void AddRexObjectProperties(UUID objectId, string baseUrl, Entity ent, List<string> materialNames, UUID collisionId)
        {
            RexObjectProperties robject = CreateRexObjectProperties(objectId, ent);
            robject.RexMeshURI = baseUrl + ent.MeshName;

            for (int i = 0; i < materialNames.Count; i++)
            {
                robject.RexMaterials.AddMaterial((uint)i, UUID.Zero, baseUrl + materialNames[i]+".material");
            }

            if (collisionId != UUID.Zero)
            {
                try
                {
                    robject.RexCollisionMeshUUID = collisionId;
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Imports the uploaded ogre scene.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="scene">The scene where to put uploaded scene.</param>
        public void ImportUploadedOgreScene(string fileName, Scene scene, UUID uploadSceneID)
        {
            // Temporary fix for scene being null, should make scene a method variable, passed forward in callstack where needed
            Scene temp = m_scene;
            m_scene = scene;
            ImportOgreScene(fileName, uploadSceneID);
            m_scene = temp;
        }

        /// <summary>
        /// Imports the uploaded ogre scene.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="sceneFile">The scene filename.</param>
        /// <param name="scene">The scene where to put uploaded scene.</param>
        public void ImportUploadedOgreScene(string baseUrl, string sceneFile, Scene scene, Vector3 offSet)
        {
            Scene temp = m_scene;
            m_scene = scene;
            Vector3 tempOffset = m_offset;
            m_offset = offSet;

            try
            {
                DotSceneLoader loader = new DotSceneLoader();
                SceneManager ogreSceneManager = new SceneManager();

                System.Xml.XmlDocument XMLDoc = new System.Xml.XmlDocument();
                XMLDoc.LoadXml(sceneFile);

                loader.ParseDotScene(XMLDoc, "General", ogreSceneManager, null, "");

                AddObjectsToScene(ogreSceneManager.RootSceneNode, baseUrl);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[OGRESCENE]: Error importing uploaded scene file. Exception: {0} {1}", e.Message, e.StackTrace, e);
            }

            m_offset = tempOffset;
            m_scene = temp;
        }

        public bool TryGetSceneStorage(out NHibernateSceneStorage storage)
        {
            storage = m_SceneStorage;
            if (storage != null)
                return true;
            else
                return false;
        }

        private string PathFromFileName(string fileName)
        {
            string[] split = fileName.Split(System.IO.Path.DirectorySeparatorChar);
            if (split.Length == 1)
            {
                split = fileName.Split(System.IO.Path.AltDirectorySeparatorChar);
                if (split.Length == 1)
                {
                    return "";
                }
            }
            List<string> list = new List<string>(split);
            list.RemoveAt(list.Count - 1);
            split = list.ToArray();
            return String.Join(System.IO.Path.DirectorySeparatorChar.ToString(), split);
        }

        public List<Scene> GetScenes()
        {
            return this.m_scenes;
        }

    }
}