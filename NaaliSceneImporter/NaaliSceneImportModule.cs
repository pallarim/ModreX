using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;

using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;

using OpenMetaverse;
using log4net;

using ModularRex.RexFramework;

namespace NaaliSceneImporter
{
    public class NaaliSceneImportModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_scenes = new List<Scene>();
        private Scene m_scene;
        private Dictionary<UUID, RegisterCaps> m_scene_caps = new Dictionary<UUID, RegisterCaps>();

        private NaaliSceneParser parser = new NaaliSceneParser();

        #region IRegionModule Members

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scenes.Add(scene);
            scene.AddCommand(this, "naaliscene", "naaliscene <action> <filename>", "Type \"naaliscene help\" to view longer help", NaaliSceneCommand);
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
            get { return "NaaliSceneImportModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion

        public List<Scene> GetScenes()
        {
            return this.m_scenes;
        }

        private void NaaliSceneCommand(string module, string[] cmdparams)
        {
            if (OpenSim.Framework.Console.MainConsole.Instance.ConsoleScene != null && OpenSim.Framework.Console.MainConsole.Instance.ConsoleScene is Scene)
            {
                m_scene = (Scene)OpenSim.Framework.Console.MainConsole.Instance.ConsoleScene;
            }
            else
            {
                if (m_scenes.Count == 1)
                    m_scene = m_scenes[0];
                else
                {
                    m_log.ErrorFormat("[NAALISCENE]: More than one scene in region. Set current scene with command \"change region <region name>\"");
                    return;
                }
            }

            try
            {
                bool showHelp = false;
                if (cmdparams.Length > 1)
                {
                    string command = cmdparams[1].ToLower(); //[0] == naaliscene
                    switch (command)
                    {
                        case "help":
                            showHelp = true;
                            break;
                        case "import":
                            ImportNaaliScene(cmdparams[2]);
                            break;
                        default:
                            showHelp = true;
                            break;
                    }
                }
                else 
                    showHelp = true;
                
                if (showHelp)
                    m_log.Info("[NAALISCENE]: Available commands: import <filename>");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[NAALISCENE]: Failed to execute NAALISCENE command. Exception {0} was thrown.", e);
            }

            m_scene = null;
        }

        private void ImportNaaliScene(string filename)
        {
            List<NaaliEntity> entities = parser.ParseXml(filename);
            m_log.InfoFormat("[NAALISCENE]: Adding {0} objects with RexObjectProperties to scene", entities.Count.ToString());
            foreach (NaaliEntity entity in entities)
            {
                AddEntityToScene(entity);
            }
        }

        public void ImportNaaliScene(byte[] data, Scene scene)
        {
            m_scene = scene;

            List<NaaliEntity> entities = parser.ParseXml(data);
            m_log.InfoFormat("[NAALISCENE]: Adding {0} objects with RexObjectProperties to scene", entities.Count.ToString());
            foreach (NaaliEntity entity in entities)
            {
                AddEntityToScene(entity);
            }

            scene = null;
        }

        private void AddEntityToScene(NaaliEntity entity)
        {
            Vector3 pos = entity.SceneData.position;
            if (pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 && pos.X <= 256 && pos.Y <= 256)
            {
                // Create new object
                SceneObjectGroup sceneObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID, m_scene.RegionInfo.MasterAvatarAssignedUUID,
                                                                  pos, entity.SceneData.orientation, PrimitiveBaseShape.CreateBox());
                SceneObjectPart root = sceneObject.RootPart;
                root.Scale = entity.SceneData.scale;

                // Create rex properties
                AddRexObjectProperties(sceneObject, entity);

                // Create and link children
                if (entity.Children.Count > 0)
                {
                    m_log.DebugFormat("[NAALISCENE]: >> Object {0} has {1} children, generating a linked SceneObjectGroup", entity.ImportId.ToString(), entity.Children.Count.ToString());
                    foreach (NaaliEntity childEntity in entity.Children)
                    {
                        SceneObjectGroup childObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID, m_scene.RegionInfo.MasterAvatarAssignedUUID,
                                                                          childEntity.SceneData.position, childEntity.SceneData.orientation, PrimitiveBaseShape.CreateBox());
                        childObject.RootPart.Scale = childEntity.SceneData.scale;
                        childObject.RootPart.UpdateFlag = 1;
                        root.ParentGroup.LinkToGroup(childObject);

                        // Create rex properties
                        AddRexObjectProperties(childObject, childEntity);
                    }
                    root.ParentGroup.RootPart.AddFlag(OpenMetaverse.PrimFlags.CreateSelected);
                    root.ParentGroup.HasGroupChanged = true;
                    root.ParentGroup.ScheduleGroupForFullUpdate();
                }
            }
            else
            {
                m_log.InfoFormat("[NAALISCENE]: >> Object {0} position was outside of the scene, skipping creation", entity.ImportId.ToString());
            }
        }

        private void AddRexObjectProperties(SceneObjectGroup sceneObject, NaaliEntity entity)
        {
            IModrexObjectsProvider rexObjects = m_scene.RequestModuleInterface<IModrexObjectsProvider>();
            RexObjectProperties robject = rexObjects.GetObject(sceneObject.UUID);
            robject.SuppressDataSending = true;
            robject.RexMaterials.Clear();

            NaaliObjectData data = entity.ObjectData;
            
            // Visuals
            UUID RefUUID;
            if (UUID.TryParse(data.MeshRef, out RefUUID))
                robject.RexMeshUUID = RefUUID;
            else
                robject.RexMeshURI = data.MeshRef;
            if (UUID.TryParse(data.SkeletonRef, out RefUUID))
                robject.RexAnimationPackageUUID = RefUUID;
            else
                robject.RexAnimationPackageURI = data.SkeletonRef;
            if (UUID.TryParse(data.ParticleRef, out RefUUID))
                robject.RexParticleScriptUUID = RefUUID;
            else
                robject.RexParticleScriptURI = data.ParticleRef;
            for (int index = 0; index < data.Materials.Count; index++)
            {
                RexMaterialsDictionaryItem item = new RexMaterialsDictionaryItem();
                string materialRef = data.Materials[index];
                if (UUID.TryParse(materialRef, out RefUUID))
                    item.AssetID = RefUUID;
                else
                    item.AssetURI = materialRef;
                if (data.MaterialTypes.Count > index)
                    item.Num = data.MaterialTypes[index];
                else
                    item.Num = 45; // Just guessing at this point, default to material script?
                robject.RexMaterials.Add((uint)index, item);
            }

            // Sound
            if (UUID.TryParse(data.SoundID, out RefUUID))
                robject.RexSoundUUID = RefUUID;
            else
                robject.RexSoundURI = data.SoundID;
            robject.RexSoundVolume = data.SoundVolume;
            robject.RexSoundRadius = data.SoundRadius;

            // Animation
            robject.RexAnimationName = data.AnimationName;
            robject.RexAnimationRate = data.AnimationRate;

            // Drawing
            robject.RexDrawType = (byte)data.DrawType;
            robject.RexDrawDistance = data.DrawDistance;
            robject.RexLOD = data.LOD;
            robject.RexScaleToPrim = data.ScaleToPrim;
            robject.RexCastShadows = data.CastShadows;
            robject.RexLightCreatesShadows = data.LightCreatesShadows;
            robject.RexIsVisible = data.Visible;

            // Send all this data to clients now
            robject.SuppressDataSending = false;
            robject.TriggerChangedRexObjectProperties();
            
            // Insert freedata, insert correct local id to the xml
            // Note: this will trigger sending the whole data to all clients after my fix to the setter
            if (entity.ComponentData != string.Empty)
                robject.RexData = entity.ComponentData.Replace("REPLACE_ENTITY_LOCAL_ID", sceneObject.LocalId.ToString());
        }
    }
}