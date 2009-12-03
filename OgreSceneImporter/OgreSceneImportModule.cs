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
                        case "test":
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

        private void ImportOgreScene(string fileName)
        {
            DotSceneLoader loader = new DotSceneLoader();
            SceneManager ogreSceneManager = new SceneManager();
            //TODO: Should first check that file exists
            loader.ParseDotScene(fileName+".scene", "General", ogreSceneManager);
            
            //Load&parse materials & textures
            //TODO: Should first check that file exists
            System.IO.StreamReader sreader = System.IO.File.OpenText(fileName+".material");
            string data = sreader.ReadToEnd();
            OgreMaterialParser parser = new OgreMaterialParser(m_scene);
            Dictionary<string, UUID> materials = null;
            Dictionary<string, UUID> textures = null;
            parser.ParseAndSaveMaterial(data, out materials, out textures);
            LoadAndSaveTextures(textures);
            m_log.InfoFormat("Found {0} materials and {1] textures", materials.Count, textures.Count);

            //TODO: Load&parse meshes
            m_log.Info("Loading OGRE stuff to scene");
            AddObjectsToScene(ogreSceneManager.RootSceneNode, materials);
        }

        private void LoadAndSaveTextures(Dictionary<string, UUID> textures)
        {
            foreach (KeyValuePair<string, UUID> texture in textures)
            {
                //Load file
                //TODO: Should first check that file exists
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

        private void GetMeshNames(SceneNode sceneNode, ref List<string> meshNames)
        {
            if (sceneNode.Entities.Count >= 0) //add this to scene and do stuff
            {
                foreach (Entity ent in sceneNode.Entities)
                {
                    meshNames.Add(ent.MeshName);
                }
            }

            if (sceneNode.Children.Count >= 0)
            {
                foreach (SceneNode child in sceneNode.Children)
                {
                    GetMeshNames(child, ref meshNames);
                }
            }
        }

        private void AddObjectsToScene(SceneNode node, Dictionary<string, UUID> materials)
        {
            if (node.Entities.Count >= 0) //add this to scene and do stuff
            {
                foreach (Entity ent in node.Entities)
                {
                    //Load mesh object
                    //TODO: Should first check that file exists
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
                        throw new Exception("Could not parse mesh, because: " + meshLoaderError);
                    }
                    //Add object to scene
                    //hackhackhack
                    node.Position.X += 20;
                    node.Position.Y += 20;
                    node.Position.Z += 20;
                    SceneObjectGroup sceneObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID,
                        m_scene.RegionInfo.MasterAvatarAssignedUUID, node.Position, node.Orientation, PrimitiveBaseShape.CreateBox());
                    sceneObject.RootPart.Scale = node.Scale;
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
                        robject.RexMaterials.AddMaterial((uint)i, materials[materialNames[i]]);
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
