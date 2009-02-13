using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using OpenMetaverse;
using Nini.Config;
using log4net;

using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Framework;

using ModularRex.RexNetwork;
using ModularRex.RexFramework;

namespace ModularRex
{
    public class RexAssetPreload : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RexSceneProperties rexSceneProperties;
        private Scene scene;

        public Dictionary<UUID, uint> PreloadAssetDictionary
        {
            get { return this.rexSceneProperties.PreloadAssetDictionary; }
        }

        public void Initialise(Scene scene, IConfigSource source)
        {
            this.scene = scene;
            this.rexSceneProperties = new RexSceneProperties();
            //scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
            //m_controllingClient.OnCompleteMovementToRegion += CompleteMovement;
            //scene.m_sceneGridService.OnAvatarCrossingIntoRegion += AgentCrossing;
        }

        public void PostInitialise()
        { 
        }

        public void Close()
        {
            this.scene = null;
            this.rexSceneProperties = null;
        }

        public string Name
        {
            get { return "RexAssetPreload"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public void AddPreloadAsset(UUID vAssetID)
        {
            if (this.rexSceneProperties.PreloadAssetDictionary.ContainsKey(vAssetID))
                return;

            AssetBase tempAsset;
            if (scene.AssetCache.TryGetCachedAsset(vAssetID, out tempAsset))
            {
                if (tempAsset != null)
                    this.rexSceneProperties.PreloadAssetDictionary.Add(tempAsset.FullID, (uint)vAssetID.GetULong());             
            }
            else
            {
                m_log.Error("[REXSCENEPROPERTIES]: RexAddPreloadAsset failed, asset not found from the asset cache. Asset: " + tempAsset.FullID.ToString());
            }
        }

        public void RemovePreloadAsset(UUID vAssetID)
        {
            if (this.rexSceneProperties.PreloadAssetDictionary.ContainsKey(vAssetID))
                this.rexSceneProperties.PreloadAssetDictionary.Remove(vAssetID);
        }
    }
}
