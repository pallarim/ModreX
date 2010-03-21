using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using ModularRex.RexFramework;
using OpenMetaverse;

namespace ModularRex.RexParts
{
    public class AddUrlsToROP : IRegionModule
    {
        private Scene m_scene;
        private IModrexObjectsProvider m_modrexObjects;
        private string m_httpbaseurl = String.Empty;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
            m_scene.AddCommand(this, "addurls", "addurls", "Adds urls to all Rex Object Properties. The url is for this simulator. This removes all existing urls.", HandleAddUrls);
            m_httpbaseurl = "http://" + m_scene.RegionInfo.ExternalHostName + ":" + m_scene.RegionInfo.HttpPort + "/assets/";
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "AddUrlsToROP module"; }
        }

        public void PostInitialise()
        {
            m_modrexObjects = m_scene.RequestModuleInterface<IModrexObjectsProvider>();
        }

        #endregion

        private void HandleAddUrls(string module, string[] cmd)
        {
            foreach (EntityBase ent in m_scene.Entities)
            {
                if (ent is SceneObjectGroup)
                {
                    foreach (SceneObjectPart part in ((SceneObjectGroup)ent).GetParts())
                    {
                        AddUrlsToRexObject(part.UUID);
                    }
                }
            }
        }

        private void AddUrlsToRexObject(UUID rexObjectId)
        {
            RexObjectProperties rop = m_modrexObjects.GetObject(rexObjectId);
            if (rop.RexAnimationPackageUUID != UUID.Zero)
            {
                rop.RexAnimationPackageURI = m_httpbaseurl + rop.RexAnimationPackageUUID.ToString() + "/data";
            }

            if (rop.RexCollisionMeshUUID != UUID.Zero)
            {
                rop.RexCollisionMeshURI = m_httpbaseurl + rop.RexCollisionMeshUUID.ToString() + "/data";
            }

            if (rop.RexMeshUUID != UUID.Zero)
            {
                rop.RexMeshURI = m_httpbaseurl + rop.RexMeshUUID.ToString() + "/data";
            }

            if (rop.RexParticleScriptUUID != UUID.Zero)
            {
                rop.RexParticleScriptURI = m_httpbaseurl + rop.RexParticleScriptUUID.ToString() + "/data";
            }

            if (rop.RexSoundUUID != UUID.Zero)
            {
                rop.RexSoundURI = m_httpbaseurl + rop.RexSoundUUID.ToString() + "/data";
            }

            RexMaterialsDictionary materials = rop.GetRexMaterials();
            rop.RexMaterials = new RexMaterialsDictionary();
            foreach (KeyValuePair<uint, RexMaterialsDictionaryItem> item in materials)
            {
                string materialUrl = m_httpbaseurl + item.Value.AssetID + "/data";
                rop.RexMaterials.AddMaterial(item.Key, item.Value.AssetID, materialUrl);
            }
        }
    }
}
