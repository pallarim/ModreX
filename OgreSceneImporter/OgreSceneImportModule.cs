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

        private UploadHandler m_uploadHandler = new UploadHandler();

        #region IRegionModule Members

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scenes.Add(scene);
            scene.AddCommand(this, "ogrescene", "ogrescene <action> <filename>", "Only command supported currently is import", OgreSceneCommand);
        }

        public void PostInitialise()
        {
            m_uploadHandler.AddUploadCap(m_scene, this);
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
                            ImportOgreScene(cmdparams[2]);
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
                                catch (Exception e)
                                {
                                    m_log.ErrorFormat("[OGRESCENE]: Could not parse new offset vector {0}", cmdparams[2]);
                                }
                            }
                            break;
                        default:
                            showHelp = true;
                            break;
                    }
                }
                else showHelp = true;
                
                if (showHelp)
                    m_log.Info("[OGRESCENE]: Available commands: import offset rotation scale swapaxes collisionmesh");
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

        private void ImportOgreScene(string fileName)
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
            m_log.InfoFormat("[OGRESCENE]: Found and loaded {0} materials and {1} textures", materials.Count, textures.Count);

            //Load&parse meshes and add them to scene
            m_log.Info("[OGRESCENE]: Loading OGRE stuff to scene");

            AddObjectsToScene(ogreSceneManager.RootSceneNode, materials, filepath);
            //AddObjectsToScene(ogreSceneManager.RootSceneNode, materials);
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

        private void AddObjectsToScene(SceneNode node, Dictionary<string, UUID> materials, string additionalSearchPath)
        {

			// Quaternion for whole scene rotation
            Quaternion sceneRotQuat = Quaternion.CreateFromAxisAngle(new Vector3(0,0,1), ToRadians(m_sceneRotation));

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

                    //check that postition of the object is inside scene
                    Vector3 objPos = new Vector3(node.DerivedPosition.X, node.DerivedPosition.Y, node.DerivedPosition.Z);
                    if (m_swapAxes == true)
                    {
                        Vector3 temp = new Vector3(objPos);
                        objPos.X = -temp.X;
                        objPos.Y = temp.Z;
                        objPos.Z = temp.Y;
                    } 
                    objPos = objPos * sceneRotQuat; // Apply scene rotation
                    objPos = (objPos * m_objectScale) + m_offset; // Apply scale and add offset
                    if (objPos.X >= 0 && objPos.Y >= 0 && objPos.Z >= 0 &&
                        objPos.X <= 256 && objPos.Y <= 256 && objPos.Z <= 256)
                    {
                        if (objPos.Z < 20)
                            m_log.WarnFormat("[OGRESCENE]: Inserting object {1} to height {0}. This object might be under water", objPos.Z, ent.MeshName);

                        //Add object to scene
                        Quaternion rot = new Quaternion(node.DerivedOrientation.X, node.DerivedOrientation.Y, node.DerivedOrientation.Z, node.DerivedOrientation.W);
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
                        
                        SceneObjectGroup sceneObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID,
                            m_scene.RegionInfo.MasterAvatarAssignedUUID, objPos, rot, PrimitiveBaseShape.CreateBox());
                        Vector3 newScale = new Vector3();
                        newScale.X = node.DerivedScale.X * m_objectScale;
                        newScale.Y = node.DerivedScale.Y * m_objectScale;
                        newScale.Z = node.DerivedScale.Z * m_objectScale;
                        if (m_swapAxes == true)
                        {
                            Vector3 temp = new Vector3(newScale);
                            newScale.X = temp.X;
                            newScale.Y = temp.Z;
                            newScale.Z = temp.Y;
                        } 
                        sceneObject.RootPart.Scale = newScale;

                        //Add refs to materials, mesh etc.
                        IModrexObjectsProvider rexObjects = m_scene.RequestModuleInterface<IModrexObjectsProvider>();
                        RexObjectProperties robject = rexObjects.GetObject(sceneObject.RootPart.UUID);
                        robject.RexMeshUUID = asset.FullID;
                        robject.RexDrawDistance = ent.RenderingDistance;
                        robject.RexCastShadows = ent.CastShadows;
                        robject.RexDrawType = 1;
                        
                        // Only assign physics mesh if no error
                        if ((meshLoaderError == "") && (m_useCollisionMesh == true))
                        {
                            try
                            {
                                robject.RexCollisionMeshUUID = asset.FullID;
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
                    else
                    {
                        m_log.ErrorFormat("[OGRESCENE]: Node postion was not inside the scene. Skipping object {0} with position {1}", ent.MeshName, objPos.ToString());
                        continue;
                    }
                }
            }

            if (node.Children.Count >= 0)
            {
                foreach (SceneNode child in node.Children)
                {
                    AddObjectsToScene(child, materials, additionalSearchPath);
                }
            }
        }

        public void ImportUploadedOgreScene(string fileName)
        {
            ImportOgreScene(fileName);
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

    }
}