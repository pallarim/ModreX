using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using ModularRex.RexFramework;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Data.NHibernate;
using OpenSim.Framework;

namespace ModularRex.RexParts
{
    public class ModrexObjects : IRegionModule, IRexObjectPropertiesEventManager 
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RexObjectPropertiesManager RexObjectPropertiesCache = new RexObjectPropertiesManager();
        private List<Scene> m_scenes = new List<Scene>();

        private NHibernateRexObjectData m_db;
        private string m_db_connectionstring;

        public delegate void OnChangePythonClassDelegate(UUID id);
        public event OnChangePythonClassDelegate OnPythonClassChange;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;
            scene.SceneContents.OnObjectDuplicate += SceneGraph_OnObjectDuplicate;
            scene.SceneContents.OnObjectRemove += SceneGraph_OnObjectRemove;


            if (m_db == null)
            {
                m_db = new NHibernateRexObjectData();
            }

            if (source != null)
            {
                try
                {
                    m_db_connectionstring = source.Configs["realXtend"].GetString("db_connectionstring", "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3");

                }
                catch (Exception)
                {

                    m_db_connectionstring = "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3";
                }
            }
            else
            {
                m_db_connectionstring = "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3";
            }
        }

        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            RexClientView rcv;
            if (client.TryGet(out rcv))
            {
                rcv.OnRexObjectProperties += rcv_OnRexObjectProperties;
                //rcv.OnChatFromClient += rcv_OnChatFromClient;

                // Send them the current Scene.
                SendAllPropertiesToUser(rcv);
            }
        }

        void SceneGraph_OnObjectDuplicate(EntityBase original, EntityBase clone)
        {
            RexObjectProperties origprops = GetObject(original.UUID);
            RexObjectProperties cloneprops = GetObject(clone.UUID);

            cloneprops.SetRexPrimDataFromObject(origprops);
        }

        void SceneGraph_OnObjectRemove(EntityBase obj)
        {
            DeleteObject(obj.UUID);
        }


        //void rcv_OnChatFromClient(object sender, OpenSim.Framework.OSChatMessage e)
        //{
        //    if (e.Message.StartsWith("/rexobj "))
        //    {
        //        string uuid = e.Message.Split(' ')[1];
        //        string asset = e.Message.Split(' ')[2];

        //        UUID prim = new UUID(uuid);
        //        UUID assetID = new UUID(asset);

        //        m_objs[prim].RexMeshUUID = assetID;
        //        SendPropertiesToAllUsers(prim, m_objs[prim]);
        //    }
        //}

        void SendPropertiesToAllUsers(UUID id, RexObjectProperties props)
        {
            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                    {
                        RexClientView rex;
                        if (avatar.ClientView.TryGet(out rex))
                        {
                            rex.SendRexObjectProperties(id,props);
                        }
                    });
            }
        }

        private void SendPreloadAssetsToUser(RexClientView user)
        {
            try
            {
                Scene ourScene = null;
                foreach (Scene s in m_scenes)
                {
                    if (user.Scene.RegionInfo.RegionHandle == s.RegionInfo.RegionHandle)
                        ourScene = s;
                }

                if (ourScene != null)
                {
                    if (ourScene.Modules.ContainsKey("RexAssetPreload"))
                    {
                        RexAssetPreload module = (RexAssetPreload)ourScene.Modules["RexAssetPreload"];
                        if (module.PreloadAssetDictionary.Count > 0)
                        {
                            user.SendRexPreloadAssets(module.PreloadAssetDictionary);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                m_log.Error("[MODREXOBJECTS]: Sending preload assets failed.", e);
            }
        }

        void SendAllPropertiesToUser(RexClientView user)
        {
            SendPreloadAssetsToUser(user);

            foreach (RexObjectProperties p in GetObjects())
            {
                user.SendRexObjectProperties(p.ParentObjectID, p);
            }
        }

        void rcv_OnRexObjectProperties(RexClientView sender, UUID id, RexObjectProperties props)
        {
            m_log.Info("[REXOBJS] Received RexObjData for " + id);
            if (props.ParentObjectID == UUID.Zero)
                props.ParentObjectID = id;
            
            // debugdata props.PrintRexPrimdata();          
       
            RexObjectProperties currentprops = GetObject(id);
            currentprops.SetRexPrimDataFromObject(props);
        }

        public void PostInitialise()
        {
            lock (m_db)
            {
                if (!m_db.Inizialized)
                {
                    m_db.Initialise(m_db_connectionstring);
                }
            }
            LoadRexObjectPropertiesToCache();  
        }

        public void Close()
        {
            ;
        }

        public string Name
        {
            get { return "RexObjectsModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }



        #region Trigger/handle rexobjectproperties events     
        public void TriggerOnChangePythonClass(UUID id)
        {
            if (OnPythonClassChange != null)
                OnPythonClassChange(id);
        }

        public void TriggerOnChangeCollisionMesh(UUID id)
        {
            // tucofixme, add
            //if (!GlobalSettings.Instance.m_3d_collision_models)
            //    return;

            RexObjectProperties p = GetObject(id);
            SceneObjectPart sop = m_scenes[0].GetSceneObjectPart(id);
            if (sop == null)
            {
                m_log.Error("[REXOBJS] TriggerOnChangeCollisionMesh, no SceneObjectPart for id:" + id.ToString());
                return;
            }

            if (sop.ParentGroup != null && sop.PhysActor is IRexPhysicsActor)
            {
                if (p.RexCollisionMeshUUID != UUID.Zero)
                    RexUpdateCollisionMesh(id);
                else
                    ((IRexPhysicsActor)sop.PhysActor).SetCollisionMesh(null, "", false);
            }       
        }

        public void TriggerOnChangeScaleToPrim(UUID id)
        {
            // tucofixme, add
            //if (!GlobalSettings.Instance.m_3d_collision_models)
            //    return;        

            RexObjectProperties p = GetObject(id);
            SceneObjectPart sop = m_scenes[0].GetSceneObjectPart(id);
            if (sop == null)
            {
                m_log.Error("[REXOBJS] TriggerOnChangeScaleToPrim, no SceneObjectPart for id:" + id.ToString());
                return;
            }

            if (sop.ParentGroup != null && sop.PhysActor is IRexPhysicsActor)
            {
                ((IRexPhysicsActor)sop.PhysActor).SetBoundsScaling(p.RexScaleToPrim);
                sop.ParentGroup.Scene.PhysicsScene.AddPhysicsActorTaint(sop.PhysActor);
            }
        }

        public void TriggerOnChangeRexObjectProperties(UUID id)
        {
            RexObjectProperties props = GetObject(id);

            m_db.StoreObject(props);
            SendPropertiesToAllUsers(id,props);
        }
        
        public void TriggerOnChangeRexObjectMetaData(UUID id)
        {
            RexObjectProperties props = GetObject(id);        
        
            m_db.StoreObject(props);
            // tucofixme, send metadata to all users
        }



        public void RexUpdateCollisionMesh(UUID id)
        {
            // tucofixme, add
            //if (!GlobalSettings.Instance.m_3d_collision_models)
            //    return;

            RexObjectProperties p = GetObject(id);
            SceneObjectPart sop = m_scenes[0].GetSceneObjectPart(id);
            if (sop == null)
            {
                m_log.Error("[REXOBJS] RexUpdateCollisionMesh, no SceneObjectPart for id:" + id.ToString());
                return;
            }

            if (p.RexCollisionMeshUUID != UUID.Zero && sop.PhysActor is IRexPhysicsActor)
            {
                AssetBase tempmodel = sop.ParentGroup.Scene.CommsManager.AssetCache.GetAsset(p.RexCollisionMeshUUID, false);
                if (tempmodel != null)
                    ((IRexPhysicsActor)sop.PhysActor).SetCollisionMesh(tempmodel.Data, tempmodel.Name, p.RexScaleToPrim);
            }
        }
        
        public byte GetAssetType(UUID assetid)
        {
            AssetBase tempmodel = m_scenes[0].CommsManager.AssetCache.GetAsset(assetid, true);
            if (tempmodel == null)
                m_scenes[0].CommsManager.AssetCache.GetAsset(assetid, false);

            if (tempmodel != null)
                return(byte)(tempmodel.Type);
            else
                return 0;
        }
        #endregion


        #region RexObjectProperties Cache

        private void LoadRexObjectPropertiesToCache()
        {
            if (!m_db.Inizialized)
            {
                m_log.ErrorFormat("LoadRexObjectPropertiesToCache failed, db not initialized");
                return;
            }
            
            foreach (Scene s in m_scenes)
            {
                foreach (EntityBase e in s.Entities)
                {
                    RexObjectProperties p = LoadObject(e.UUID);
                    p.ParentObjectID = e.UUID;
                    p.SetRexEventManager(this);
                    RexObjectPropertiesCache.Add(e.UUID,p);
                    
                    // Since loaded objects have their properties already set, any initialization that needs to be done should be here.
                    if(p.RexCollisionMeshUUID != UUID.Zero)
                        TriggerOnChangeCollisionMesh(e.UUID);

                    if (p.RexClassName.Length > 0)
                    {
                        SceneObjectPart sop = m_scenes[0].GetSceneObjectPart(p.ParentObjectID);
                        if (sop != null)
                            sop.SetScriptEvents(p.ParentObjectID, (int)scriptEvents.touch_start);
                    }      
                }
            }
        }

        private RexObjectProperties LoadObject(UUID id)
        {
            RexObjectProperties robject = m_db.LoadObject(id);
            if (robject == null)
            {
                robject = new RexObjectProperties();
                robject.ParentObjectID = id;
            }
            return robject;
        }        
        
        public RexObjectProperties GetObject(UUID id)
        {
            RexObjectProperties props = RexObjectPropertiesCache[id];
            if (props == null)
            {
                props = new RexObjectProperties(id, this);
                RexObjectPropertiesCache.Add(id, props);
            }
            return props;
        }

        public List<RexObjectProperties> GetObjects()
        {
            return RexObjectPropertiesCache.GetAllRexObjectProperties();
        }

        public bool DeleteObject(UUID id)
        {
            if (RexObjectPropertiesCache.ContainsKey(id))
            {
                RexObjectPropertiesCache.Remove(id);
                m_db.RemoveObject(id);
                return true;
            }
            return false;
        }


        #endregion

    }
}
