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

        private Scene m_scene;
        private float m_objectImportScale = 1.0f;

        #region IRegionModule Members

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
        }

        public void PostInitialise()
        {
            m_scene.AddCommand(this, "ogrescene", "ogrescene <action> <filename>", "Only command supported currently is import", OgreSceneCommand);
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
            get { return false; }
        }

        #endregion

        private void OgreSceneCommand(string module, string[] cmdparams)
        {
            try
            {
                if (cmdparams.Length >= 1)
                {
                    string command = cmdparams[1].ToLower(); //[0] == ogrescene
                    switch (command)
                    {
                        case "import":
                            ImportOgreScene(cmdparams[2]);
                            break;
                        case "setscale":
                            SetScale(cmdparams[2]);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("Failed to execute ogrescene command. Exception {0} was thrown.", e);
            }
        }

        private void SetScale(string p)
        {
            if (p == "help")
            {
                m_log.InfoFormat("Set object scale on import. By default this is 1.0. Current value {0}", m_objectImportScale);
            }
            else
            {
                try
                {
                    float newScale = Convert.ToSingle(p);
                    m_objectImportScale = newScale;
                }
                catch (Exception)
                {
                    m_log.ErrorFormat("Error parsin scale from value {0}", p);
                }
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
                m_log.ErrorFormat("Could not find scene file {0}.scene", fileName);
                return;
            }
            
            //Load&parse materials & textures
            //check that file exists
            if (!System.IO.File.Exists(fileName + ".material"))
            {
                m_log.ErrorFormat("Could not find material file {0}.material", fileName);
                return;
            }
            System.IO.StreamReader sreader = System.IO.File.OpenText(fileName+".material");
            string data = sreader.ReadToEnd();
            OgreMaterialParser parser = new OgreMaterialParser(m_scene);
            Dictionary<string, UUID> materials = null;
            Dictionary<string, UUID> textures = null;
            if (!parser.ParseAndSaveMaterial(data, out materials, out textures))
            {
                m_log.Error("Material parsing failed. Ending operation");
                return;
            }

            if (!LoadAndSaveTextures(textures))
            {
                m_log.ErrorFormat("Aborting ogre scene importing, because there were some errors in loading textures");
                return;
            }
            m_log.InfoFormat("Found and loaded {0} materials and {1} textures", materials.Count, textures.Count);

            //Load&parse meshes and add them to scene
            m_log.Info("Loading OGRE stuff to scene");
            AddObjectsToScene(ogreSceneManager.RootSceneNode, materials);
        }

        private bool LoadAndSaveTextures(Dictionary<string, UUID> textures)
        {
            foreach (KeyValuePair<string, UUID> texture in textures)
            {
                try
                {
                    //check that file exists
                    if (!System.IO.File.Exists(texture.Key))
                    {
                        m_log.ErrorFormat("Could not load file {0}, because it doesn't exist in working directory", texture.Key);
                        return false;
                    }

                    //Load file
                    byte[] data = System.IO.File.ReadAllBytes(texture.Key);

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
                    m_log.ErrorFormat("Could not load texture {0}, because {1}", texture.Key, e);
                    return false;
                }
            }
            return true;
        }

        private Bitmap ResizeImage(Bitmap imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return b;
        }

        private void AddObjectsToScene(SceneNode node, Dictionary<string, UUID> materials)
        {
            if (node.Entities.Count >= 0) //add this to scene and do stuff
            {
                foreach (Entity ent in node.Entities)
                {
                    //first check that file exists
                    if (!System.IO.File.Exists(ent.MeshName))
                    {
                        m_log.ErrorFormat("Could not find mesh file {0}. Skipping", ent.MeshName);
                        continue;
                    }

                    //Load mesh object
                    byte[] data = System.IO.File.ReadAllBytes(ent.MeshName);

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
                        m_log.ErrorFormat("Error occurred while parsing material names from mesh. Skipping object {0}", ent.MeshName);
                        continue;
                    }

                    //check that postition of the object is inside scene
                    if (node.Position.X >= 0 && node.Position.Y >= 0 && node.Position.Z >= 0 &&
                        node.Position.X <= 256 && node.Position.Y <= 256 && node.Position.Z <= 256)
                    {
                        if (node.Position.Z < 20)
                            m_log.WarnFormat("Inserting object {1} to height {0}. This object might be under water", node.Position.Z, ent.MeshName);

                        //Add object to scene
                        SceneObjectGroup sceneObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID,
                            m_scene.RegionInfo.MasterAvatarAssignedUUID, node.Position, node.Orientation, PrimitiveBaseShape.CreateBox());
                        Vector3 newScale = new Vector3();
                        newScale.X = node.Scale.X * m_objectImportScale;
                        newScale.Y = node.Scale.Y * m_objectImportScale;
                        newScale.Z = node.Scale.Z * m_objectImportScale;
                        sceneObject.RootPart.Scale = newScale;

                        //Add refs to materials, mesh etc.
                        IModrexObjectsProvider rexObjects = m_scene.RequestModuleInterface<IModrexObjectsProvider>();
                        RexObjectProperties robject = rexObjects.GetObject(sceneObject.RootPart.UUID);
                        robject.RexMeshUUID = asset.FullID;
                        robject.RexDrawDistance = ent.RenderingDistance;
                        robject.RexCastShadows = ent.CastShadows;
                        robject.RexDrawType = 1;
                        robject.RexCollisionMeshUUID = asset.FullID;
                        for (int i = 0; i < materialNames.Count; i++)
                        {
                            UUID materilUUID;
                            if (materials.TryGetValue(materialNames[i], out materilUUID))
                            {
                                robject.RexMaterials.AddMaterial((uint)i, materilUUID);
                            }
                            else
                            {
                                m_log.ErrorFormat("Could not find material UUID for material {0}. Skipping material", materialNames[i]);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        m_log.ErrorFormat("Node postion was not inside the scene. Skipping object {0} with position {1}", ent.MeshName, node.Position.ToString());
                        continue;
                    }
                }
            }

            if (node.Children.Count >= 0)
            {
                foreach (SceneNode child in node.Children)
                {
                    AddObjectsToScene(child, materials);
                }
            }
        }
    }
}
