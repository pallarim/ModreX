using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace OgreSceneImporter.UploadSceneDB
{
    public interface ISceneStorage
    {
        bool SaveScene(UploadScene scene);
        List<UploadScene> GetRegionsScenes(UUID regionid);
        List<RegionScene> GetRegionSceneList();
        UploadScene GetScene(string scene_id);
        List<UploadScene> GetScenes();
        List<SceneAsset> GetSceneAssets(string scene_id);
        List<string> GetScenesRegionIds(string scene_id);
        bool SetSceneToRegion(string sceneid, string regionid);
        bool RemoveSceneFromRegion(string sceneid, string regionid);
        bool DeleteScene(string sceneid);

    }


    public interface IAssetDataSaver
    {
        bool SaveAssetData(UUID sceneid, UUID assetid, string name, int type);
        bool SaveAssetData(UUID sceneid, UUID scneneassetid, string name, int type, uint localId, UUID entityId);
        bool UpdateAssetEntityId(UUID sceneid, UUID assetid, UUID entityId);
    }
}
