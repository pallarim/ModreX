using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using ModularRex.RexFramework;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Data.NHibernate;

namespace ModularRex.RexParts
{
    public class ModrexObjects : IRegionModule 
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //private Dictionary<UUID,RexObjectProperties> m_objs = new Dictionary<UUID, RexObjectProperties>();
        private List<Scene> m_scenes = new List<Scene>();

        private NHibernateRexObjectData m_db;
        private string m_db_connectionstring;

        public event OnChangePythonClassDelegate OnPythonClassChange;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;


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

        void SendAllPropertiesToUser(RexClientView user)
        {
            if (m_db.Inizialized)
            {
                foreach (Scene s in m_scenes)
                {
                    foreach (EntityBase e in s.Entities)
                    {
                        RexObjectProperties p = m_db.LoadObject(e.UUID);
                        if (p != null)
                        {
                            user.SendRexObjectProperties(e.UUID, p);
                            p.OnPythonClassChange += PythonClassNameChanged;
                        }
                    }
                }
            }
        }

        private void PythonClassNameChanged(UUID id)
        {
            if (OnPythonClassChange != null)
            {
                OnPythonClassChange(id);
            }
        }


        void rcv_OnRexObjectProperties(RexClientView sender, UUID id, RexObjectProperties props)
        {
            m_log.Info("[REXOBJS] Recieved RexObjData for " + id);
            if (props.ParentObjectID == UUID.Zero)
                props.ParentObjectID = id;
            props.PrintRexPrimdata();

            m_db.StoreObject(props);
            SendPropertiesToAllUsers(id, props);
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

        public RexObjectProperties Load(UUID id)
        {
            RexObjectProperties robject = m_db.LoadObject(id);
            if (robject == null)
            {
                robject = new RexObjectProperties();
                robject.ParentObjectID = id;
            }
            return robject;
        }

        public void Save(RexObjectProperties obj)
        {
            m_db.StoreObject(obj);
        }
    }
}
