using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using ModularRex.RexFramework;

namespace ModularRex.RexParts.Helpers
{
    public static class AssetsHelper
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Gets list of assets
        /// </summary>
        /// <param name="scene">The scene where to get the assets</param>
        /// <param name="assetType">Type of the assets to get</param>
        /// <returns>List of assets found</returns>
        public static List<AssetBase> GetAssetList(Scene scene, int assetType)
        {
            Dictionary<UUID, int> assetUuids = new Dictionary<UUID, int>();

            List<EntityBase> entities = scene.GetEntities();
            List<SceneObjectGroup> sceneObjects = new List<SceneObjectGroup>();

            List<AssetBase> foundObjects = new List<AssetBase>();

            foreach (EntityBase entity in entities)
            {
                if (entity is SceneObjectGroup)
                {
                    SceneObjectGroup sceneObject = (SceneObjectGroup)entity;

                    if (!sceneObject.IsDeleted && !sceneObject.IsAttachment)
                        sceneObjects.Add((SceneObjectGroup)entity);
                }
            }

            UuidGatherer assetGatherer = new UuidGatherer(scene.AssetService);


            if (assetType == 0 || assetType == 1) //do this only for textures and sounds
            {
                foreach (SceneObjectGroup sceneObject in sceneObjects)
                {
                    assetGatherer.GatherAssetUuids(sceneObject, assetUuids);
                }
            }

            ModrexObjects module = scene.RequestModuleInterface<ModrexObjects>();
            if (module != null)
            {
                foreach (SceneObjectGroup sceneObject in sceneObjects)
                {
                    RexObjectProperties rop = module.GetObject(sceneObject.RootPart.UUID);
                    AssetBase asset;
                    switch (assetType)
                    {
                        case 1: //sound
                            if (rop.RexSoundUUID != UUID.Zero)
                            {
                                asset = scene.AssetService.Get(rop.RexSoundUUID.ToString());
                                if (asset != null && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            break;
                        case 6: //3d
                            if (rop.RexMeshUUID != UUID.Zero)
                            {
                                asset = scene.AssetService.Get(rop.RexMeshUUID.ToString());
                                if (asset != null && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            if (rop.RexCollisionMeshUUID != UUID.Zero)
                            {
                                asset = scene.AssetService.Get(rop.RexCollisionMeshUUID.ToString());
                                if (asset != null && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            break;
                        case 0: //texture
                            foreach (KeyValuePair<uint, RexMaterialsDictionaryItem> kvp in rop.GetRexMaterials())
                            {
                                asset = scene.AssetService.Get(kvp.Value.AssetID.ToString());
                                if (asset != null && (int)asset.Type == assetType && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            break;
                        case 41: //Material && Particle, WTF??!
                            foreach (KeyValuePair<uint, RexMaterialsDictionaryItem> kvp in rop.GetRexMaterials())
                            {
                                asset = scene.AssetService.Get(kvp.Value.AssetID.ToString());
                                if (asset != null && (int)asset.Type == assetType && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            if (rop.RexParticleScriptUUID != UUID.Zero)
                            {
                                asset = scene.AssetService.Get(rop.RexParticleScriptUUID.ToString());
                                if (asset != null && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            break;
                        case 19: //3d anim
                            if (rop.RexAnimationPackageUUID != UUID.Zero)
                            {
                                asset = scene.AssetService.Get(rop.RexAnimationPackageUUID.ToString());
                                if (asset != null && !foundObjects.Contains(asset))
                                {
                                    foundObjects.Add(asset);
                                }
                            }
                            break;

                        case 42: //flash
                            //No way to fetch flash animation from scene, since no reference to it is kept in scene
                            break;
                        default:
                            m_log.Warn("[ASSETS]: Requested list of unknown asset type");
                            break;
                    }
                }
            }


            foreach (KeyValuePair<UUID, int> kvp in assetUuids)
            {
                if (kvp.Value == assetType)
                {
                    AssetBase asset = scene.AssetService.Get(kvp.Key.ToString());
                    if (asset != null)
                    {
                        foundObjects.Add(asset);
                    }
                }
            }

            return foundObjects;
        }
    }
}
