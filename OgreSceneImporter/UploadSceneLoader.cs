using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OgreSceneImporter.UploadSceneDB;
using log4net;


namespace OgreSceneImporter
{
    /// <summary>
    /// Class for loading uploaded scene when its allready in server databases
    /// </summary>
    public class UploadSceneLoader
    {
        private Scene m_scene;
        private float m_objectScale = 1.0f;
        private Vector3 m_offset = Vector3.Zero;
        private bool m_swapAxes = false;
        private bool m_useCollisionMesh = true;
        private float m_sceneRotation = 0.0f;
        private OgreSceneImportModule m_osi;

        //private OgreSceneImporter.UploadSceneDB.ISceneStorage m_SceneStorage;

        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UploadSceneLoader(Scene scene, OgreSceneImportModule osi)
        {
            m_scene = scene;
            m_osi = osi;
        }


        //public void AddObjectsToScene(SceneNode node, Dictionary<string, UUID> materials, string uploadsceneid)
        public void AddObjectsToScene(SceneNode node, List<SceneAsset> meshes, string uploadsceneid, Dictionary<string, UUID> materials, IAssetDataSaver ads)
        {
            Quaternion sceneRotQuat = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), m_osi.ToRadians(m_sceneRotation));
            node.RefreshDerivedTransform();
            
            if (node.Entities.Count >= 0)
            {
                foreach (Entity ent in node.Entities)
                {
                    // here we should read mesh assetservice, first we need to get saved identifier from db, identified by upload scene and name
                    string meshName = ent.MeshName;
                    
                    //m_SceneStorage.GetSceneAssets(
                    SceneAsset sa = GetWithNameFromList(ent.MeshName, meshes);
                    if (sa != null)
                    {
                        //byte[] data = m_scene.AssetService.GetData(sa.AssetStorageId.ToString()); // mesh data
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

                            // need to update new asset id's to uploadscene db
                            SceneObjectGroup sceneObject = m_scene.AddNewPrim(m_scene.RegionInfo.MasterAvatarAssignedUUID,
                                m_scene.RegionInfo.MasterAvatarAssignedUUID, objPos, rot, OpenSim.Framework.PrimitiveBaseShape.CreateBox());
                            

                            // update asset sa sceneEntityId
                            //m_log.Debug("Updating asset: " + sa.AssetId + " sceneId to: " + sceneObject.UUID.ToString() + " " + sa.Name);
                            //if (m_scene.Entities[sceneObject.UUID] != null) { m_log.Debug("is in scene"); }
                            m_log.Debug("--------------------------------------------");
                            m_log.Debug("new eid: " + sceneObject.UUID.ToString());
                            m_log.Debug("--------------------------------------------");

                            ads.UpdateAssetEntityId(new UUID(sa.SceneId), new UUID(sa.AssetId), sceneObject.UUID);

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
                            ModularRex.RexFramework.IModrexObjectsProvider rexObjects = m_scene.RequestModuleInterface<ModularRex.RexFramework.IModrexObjectsProvider>();
                            ModularRex.RexFramework.RexObjectProperties robject = rexObjects.GetObject(sceneObject.RootPart.UUID);
                            //UUID assetFullId = new UUID(sa.AssetStorageId);
                            UUID assetFullId = new UUID(sa.AssetId);
                            robject.RexMeshUUID = assetFullId;
                            robject.RexDrawDistance = ent.RenderingDistance;
                            robject.RexCastShadows = ent.CastShadows;
                            robject.RexDrawType = 1;

                            // Only assign physics mesh if no error
                            if ((meshLoaderError == "") && (m_useCollisionMesh == true))
                            {
                                try
                                {
                                    robject.RexCollisionMeshUUID = assetFullId;
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
                            //!!
                            sceneObject.ScheduleGroupForFullUpdate();
                        }
                        else
                        {
                            m_log.ErrorFormat("[OGRESCENE]: Node postion was not inside the scene. Skipping object {0} with position {1}", ent.MeshName, objPos.ToString());
                            continue;
                        }
                    }
                }
            }
            if (node.Children.Count >= 0)
            {
                foreach (SceneNode child in node.Children)
                {
                    AddObjectsToScene(child, meshes, uploadsceneid, materials, ads);
                }
            }

        }

        private SceneAsset GetWithNameFromList(string name, List<SceneAsset> meshes)
        {
            foreach (SceneAsset sa in meshes)
            {
                if (sa.Name == name)
                {
                    return sa;
                }
            }
            return null;
        }


    }
}
