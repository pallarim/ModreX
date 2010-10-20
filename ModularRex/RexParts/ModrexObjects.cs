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
using OpenSim.Framework;
using ModularRex.NHibernate;
using OpenSim.Framework.Communications.Cache;

namespace ModularRex.RexParts
{
    public class ModrexObjects : IRegionModule, IRexObjectPropertiesEventManager, IModrexObjectsProvider
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

            scene.RegisterModuleInterface<ModrexObjects>(this);
            scene.RegisterModuleInterface<IModrexObjectsProvider>(this);
            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;
            scene.SceneContents.OnObjectDuplicate += SceneGraph_OnObjectDuplicate;
            scene.SceneContents.OnObjectRemove += SceneGraph_OnObjectRemove;
            scene.EventManager.OnObjectBeingRemovedFromScene += EventManager_OnObjectBeingRemovedFromScene;

            scene.AddCommand(this, "modreximport", "load 0.4 rex database <connstring>", "conn string example: SQLiteDialect;SQLite20Driver;Data Source=beneath_the_waves.db;Version=3", HandleLoadRexLegacyData);

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

        void EventManager_OnObjectBeingRemovedFromScene(SceneObjectGroup obj)
        {
            if (RexObjectPropertiesCache.ContainsKey(obj.UUID))
            {
                RexObjectPropertiesCache.Remove(obj.UUID);
            }
        }

        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            RexClientViewBase rcv;
            if (client.TryGet(out rcv))
            {
                rcv.OnRexObjectProperties += rcv_OnRexObjectProperties;
                rcv.OnPrimFreeData += rcv_OnPrimFreeData;
                //rcv.OnChatFromClient += rcv_OnChatFromClient;

                // Send them the current Scene.
                SendAllPropertiesToUser(rcv);
            }   
        }

        private void rcv_OnPrimFreeData(IClientAPI sender, List<string> parameters)
        {
            m_log.Info("[REXOBJS] Received Prim free data");
            if (parameters.Count >= 2)
            {
                UUID primID = new UUID(parameters[0]);
                string data = String.Empty;
                if(parameters.Count == 2)
                {
                    data = parameters[1];
                }else
                {
                    for (int i = 1; i < parameters.Count; i++)
                    {
                        data += parameters[i];
                    }
                }

                RexObjectProperties props = GetObject(primID);
                props.RexData = data;

                SendPrimFreeDataToAllUsers(primID, data);
            }
            else
            {
                m_log.Warn("[REXOBJS] unexpected number of parameters");
            }
        }

        public void SendPrimFreeDataToAllUsers(UUID id, string data)
        {
            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                    {
                        RexClientViewBase rex;
                        if (avatar.ClientView.TryGet(out rex))
                        {
                            rex.SendRexPrimFreeData(id, data);
                        }
                    });
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
            //this object group was removed from scene, but part is not necessarily removed from all groups
            //DeleteObject(obj.UUID);
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
                        RexClientViewBase rex;
                        if (avatar.ClientView.TryGet(out rex))
                        {
                            rex.SendRexObjectProperties(id,props);
                        }
                    });
            }
        }

        private void SendPreloadAssetsToUser(RexClientViewBase user)
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

        public void SendAllPropertiesToUser(RexClientViewBase user)
        {
            SendPreloadAssetsToUser(user);

            foreach (RexObjectProperties p in GetObjects())
            {
                user.SendRexObjectProperties(p.ParentObjectID, p);
                if (p.RexData.Length > 0) //send rex data also if exists
                {
                    user.SendRexPrimFreeData(p.ParentObjectID, p.RexData);
                }
            }
        }

        void rcv_OnRexObjectProperties(IClientAPI sender, UUID id, RexObjectProperties props)
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
            SceneObjectPart sop = null;// = m_scenes[0].GetSceneObjectPart(id);
            foreach (Scene scene in m_scenes)
            {
                SceneObjectPart part = scene.GetSceneObjectPart(id);
                if (part != null)
                {
                    sop = part;
                    break;
                }
            }

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
            SceneObjectPart sop = null;
            foreach (Scene s in m_scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(id);
                if (part != null)
                {
                    sop = part;
                    break;
                }
            }

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

            //instead of saving right away, set timer to save after a sec.
            //m_db.StoreObject(props);
            SendPropertiesToAllUsers(id,props);
            props.ScheduleSave();
        }
        
        public void TriggerOnChangeRexObjectMetaData(UUID id)
        {
            RexObjectProperties props = GetObject(id);        
        
            m_db.StoreObject(props);
            // tucofixme, send metadata to all users
            SendPrimFreeDataToAllUsers(id, props.RexData); // Pforce fixemup?!
        }

        public void RexUpdateCollisionMesh(UUID id)
        {
            // tucofixme, add
            //if (!GlobalSettings.Instance.m_3d_collision_models)
            //    return;

            RexObjectProperties p = GetObject(id);
            SceneObjectPart sop = null;// m_scenes[0].GetSceneObjectPart(id);
            foreach (Scene s in m_scenes)
            {
                SceneObjectPart part = s.GetSceneObjectPart(id);
                if (part != null)
                {
                    sop = part;
                    break;
                }
            }

            if (sop == null)
            {
                m_log.Error("[REXOBJS] RexUpdateCollisionMesh, no SceneObjectPart for id:" + id.ToString());
                return;
            }

            if (p.RexCollisionMeshUUID != UUID.Zero && sop.PhysActor is IRexPhysicsActor)
            {
                AssetBase tempmodel = sop.ParentGroup.Scene.AssetService.Get(p.RexCollisionMeshUUID.ToString());
                if (tempmodel != null)
                    ((IRexPhysicsActor)sop.PhysActor).SetCollisionMesh(tempmodel.Data, tempmodel.Name, p.RexScaleToPrim);
            }
        }
        
        public sbyte GetAssetType(UUID assetid)
        {
            AssetBase tempmodel = m_scenes[0].AssetService.Get(assetid.ToString());
            if (tempmodel == null)
                m_scenes[0].AssetService.Get(assetid.ToString());

            if (tempmodel != null)
                return tempmodel.Type;
            else
                return 0;
        }


        public void TriggerOnSaveObject(UUID id)
        {
            RexObjectProperties props = GetObject(id);
            m_db.StoreObject(props);
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
                    if (e is SceneObjectGroup && !RexObjectPropertiesCache.ContainsKey(e.UUID))
                    {
                        SceneObjectGroup oGroup = (SceneObjectGroup)e;
                        
                        foreach (SceneObjectPart part in oGroup.GetParts())
                        {
                            RexObjectProperties p = LoadObject(part.UUID);
                            p.ParentObjectID = part.UUID;
                            p.SetRexEventManager(this);
                            if(!RexObjectPropertiesCache.ContainsKey(part.UUID))
                                RexObjectPropertiesCache.Add(part.UUID, p);

                            // Since loaded objects have their properties already set, any initialization that needs to be done should be here.
                            if (p.RexCollisionMeshUUID != UUID.Zero)
                                TriggerOnChangeCollisionMesh(part.UUID);

                            if (p.RexClassName.Length > 0)
                            {
                                part.SetScriptEvents(p.ParentObjectID, (int)scriptEvents.touch_start);
                                TriggerOnChangePythonClass(part.UUID);
                            }

                            SendPropertiesToAllUsers(part.UUID, p);
                        }
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
                //Objects that are not in the scene can't be found from cache.
                //So before we create new properties, we try to find them from db
                props = m_db.LoadObject(id);
                if (props == null)
                {
                    props = new RexObjectProperties(id, this);
                }
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


        public void HandleLoadRexLegacyData(string module, string[] args)
        {
            NHibernateRexLegacyData legacydata = new NHibernateRexLegacyData();

            legacydata.Initialise(args[1]);
            if(!legacydata.Inizialized)
            {
                m_log.Info("[MODREXOBJECTS]: Legacy database failed to initialize.");
                return;            
            }

            List<RexLegacyPrimData> rexprimdata = legacydata.LoadAllRexPrimData();
            m_log.Info("[MODREXOBJECTS]: Legacy rexprimdata objects loaded:" + rexprimdata.Count.ToString());

            List<RexLegacyPrimMaterialData> rexprimmaterialdata = legacydata.LoadAllRexPrimMaterialData();
            m_log.Info("[MODREXOBJECTS]: Legacy rexprimmaterialdata objects loaded:" + rexprimmaterialdata.Count.ToString());

            foreach (RexLegacyPrimData prim in rexprimdata)
            {
                RexObjectProperties p = GetObject(prim.UUID);
                p.SetRexPrimDataFromLegacyData(prim);
            }

            foreach (RexLegacyPrimMaterialData primmat in rexprimmaterialdata)
            {
                RexObjectProperties p = GetObject(primmat.UUID);
                p.RexMaterials.AddMaterial((uint)primmat.MaterialIndex,primmat.MaterialUUID);
            }
        }

    }
}
