using System;
using System.Collections.Generic;
using System.Text;
using NHibernate;
using NHibernate.Criterion;
using Nini.Config;
using log4net;
using System.Reflection;

namespace OgreSceneImporter.UploadSceneDB
{
    public class NHibernateSceneStorage : ISceneStorage, IAssetDataSaver
    {
        NHibernateManager storageModule;
        private string connectionString;

        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public NHibernateSceneStorage(IConfigSource config)
        {
            IConfig serverConfig = config.Configs["UploadSceneConfig"];
            connectionString = serverConfig.GetString("ConnectionString");

            storageModule = new NHibernateManager(connectionString, "");
            //storageModule.Initialise(connectionString);
            // test db
            try
            {
                GetScenes();
            }
            catch (Exception)
            {
                // try creating tables
                storageModule.CreateDBTables();
            }
        }

        #region ISceneStorage Members

        public bool SaveScene(UploadScene scene)
        {
            object obj = storageModule.Insert(scene);
            if (obj == null) { return false; } else return true;
        }

        public List<UploadScene> GetRegionsScenes(OpenMetaverse.UUID regionid)
        {
            List<UploadScene> retVals = new List<UploadScene>();

            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(RegionScene));
            criteria.Add(Expression.Eq("RegionId", regionid.ToString()));
            System.Collections.IList list = criteria.List();
            if (list.Count > 0)
            {
                // read Upload scenes
                foreach (RegionScene rs in list)
                {
                    ICriteria criteria2 = storageModule.GetSession().CreateCriteria(typeof(UploadScene));
                    criteria2.Add(Expression.Eq("SceneId", rs.SceneId));
                    System.Collections.IList list2 = criteria2.List();
                    if (list2.Count > 0)
                    {
                        if (criteria2.List()[0] != null)
                        {
                            object obj = criteria2.List()[0];
                            string typename = obj.GetType().Name;
                            retVals.Add((UploadScene)criteria2.List()[0]);
                        }
                    }
                    else { // corrupted db: scene regionscene table but not in uploadscenetable
                    }
                }
            }
            return retVals;
        }

        public List<RegionScene> GetRegionSceneList() 
        {
            List<RegionScene> list = new List<RegionScene>();
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(RegionScene));
            System.Collections.IList rslist = criteria.List();
            foreach (RegionScene rs in rslist)
            {
                list.Add(rs);
            }
            return list;
        }

        public UploadScene GetScene(string scene_id)
        {
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(UploadScene));
            criteria.Add(Expression.Eq("SceneId", scene_id));
            System.Collections.IList list = criteria.List();
            if (list.Count > 0)
            {
                return (UploadScene)list[0];
            }
            return null;
        }

        public List<UploadScene> GetScenes()
        {
            List<UploadScene> retVals = new List<UploadScene>();
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(UploadScene));
            System.Collections.IList list = criteria.List();
            foreach(UploadScene us in list)
            {
                retVals.Add(us);
            }
            return retVals;
        }

        public List<SceneAsset> GetSceneAssets(string scene_id)
        {
            List<SceneAsset> retVals = new List<SceneAsset>();
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(SceneAsset));
            criteria.Add(Expression.Eq("SceneId", scene_id));
            System.Collections.IList list = criteria.List();
            if (list.Count > 0)
            {
                foreach (SceneAsset sa in list)
                {
                    retVals.Add(sa);
                }
            }
            return retVals;
        }
        public List<string> GetScenesRegionIds(string scene_id)
        {
            List<string> ids = new List<string>();
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(RegionScene));
            criteria.Add(Expression.Eq("SceneId", scene_id));
            System.Collections.IList list = criteria.List();
            if (list.Count > 0)
            {
                foreach (RegionScene item in list)
                {
                    ids.Add(item.RegionId);
                }
            }
            return ids;
        }

        public bool SetSceneToRegion(string sceneid, string regionid)
        {
            RegionScene rs = new RegionScene(regionid, sceneid);
            object obj = storageModule.Insert(rs);
            if (obj == null) { return false; } else return true;
        }

        public bool RemoveSceneFromRegion(string sceneid, string regionid)
        {
            //RegionScene rs = new RegionScene(regionid, sceneid);
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(RegionScene));
            criteria.Add(Expression.Eq("SceneId", sceneid));
            criteria.Add(Expression.Eq("RegionId", regionid));
            System.Collections.IList list = criteria.List();
            if (list.Count > 0)
            {
                RegionScene rs = (RegionScene)list[0];
                object obj = storageModule.Delete(rs);
                if (obj == null) { return false; } else return true;
            }
            return false;
        }

        public bool DeleteScene(string sceneid)
        {
            // del assets and scene
            List<SceneAsset> assets = GetSceneAssets(sceneid);
            foreach (SceneAsset sa in assets)
            {
                object obj = storageModule.Delete(sa);
                if (obj == null) { return false; };
            }
            // del scene
            UploadScene us = GetScene(sceneid);
            if (us == null) { return false; }
            object obj2 = storageModule.Delete(us);
            if (obj2 == null) { return false; };

            return true;
        }

        #endregion

        #region IAssetDataSaver Members

        public bool SaveAssetData(OpenMetaverse.UUID sceneid, OpenMetaverse.UUID assetid, string name, int type)
        {
            SceneAsset sa = new SceneAsset(assetid, sceneid, name, type);
            object obj = storageModule.Insert(sa);
            if (obj == null) { return false; } else return true;
        }

        public bool SaveAssetData(OpenMetaverse.UUID sceneid, OpenMetaverse.UUID sceneassetid, string name, int type, uint localId, OpenMetaverse.UUID entityId)
        {
            SceneAsset sa = new SceneAsset(sceneassetid, sceneid, name, type, localId, entityId);
            object obj = storageModule.Insert(sa);
            if (obj == null) { return false; } else return true;
        }

        public bool UpdateAssetEntityId(OpenMetaverse.UUID sceneid, OpenMetaverse.UUID assetid, OpenMetaverse.UUID entityId)
        {
            ICriteria criteria = storageModule.GetSession().CreateCriteria(typeof(SceneAsset));
            criteria.Add(Expression.Eq("SceneId", sceneid.ToString()));
            criteria.Add(Expression.Eq("AssetId", assetid.ToString()));
            System.Collections.IList list = criteria.List();
            if (list.Count > 0)
            {
                SceneAsset sa = (SceneAsset)list[0];
                sa.EntityId = entityId.ToString();
                storageModule.Update(sa);
                m_log.InfoFormat("[OGRESCENE]: Set assets {0} entityid to {1}", assetid.ToString(), entityId.ToString());
            }
            else 
            {
                m_log.WarnFormat("[OGRESCENE]: Warning did not find asset to update entityid, for asset {0}, with entityid {1} \n"
                    + "SceneId: {2}", assetid.ToString(), entityId.ToString(), sceneid.ToString()); 
            }
            
            return false;
        }

        #endregion
    }
}
