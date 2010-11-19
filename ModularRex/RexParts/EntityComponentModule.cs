using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using ModularRex.NHibernate;
using ModularRex.RexNetwork;
using OpenMetaverse;
using OpenSim.Framework;
using ModularRex.RexFramework;
using log4net;
using System.Reflection;

namespace ModularRex.RexParts
{
    public delegate bool UpdateECData(object sender, ref ECData data);
    public delegate bool RemoveECData(object sender, UUID entityId, string componentType, string componentName);

    public class EntityComponentModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected class Entity
        {
            public Entity(UUID id)
            {
                Id = id;
            }

            public UUID Id;
            public Dictionary<KeyValuePair<string, string>, ECData> Components = new Dictionary<KeyValuePair<string, string>, ECData>();
        }

        private List<Scene> m_scenes = new List<Scene>();
        private NHibernateECData m_db;
        private string m_db_connectionstring;
        private bool m_db_initialized = false;
        private Dictionary<UUID, Entity> m_entity_components = new Dictionary<UUID, Entity>();
        private Dictionary<string, UpdateECData> m_ec_update_callbacks = new Dictionary<string, UpdateECData>();
        private Dictionary<string, RemoveECData> m_ec_remove_callbacks = new Dictionary<string, RemoveECData>();

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
        }

        #endregion

        #region IRegionModuleBase Members

        public string Name
        {
            get { return "EntityComponentModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(Nini.Config.IConfigSource source)
        {
            //read configuration
            try
            {
                m_db_connectionstring = source.Configs["realXtend"].GetString("db_connectionstring", "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3");
            }
            catch (Exception)
            {
                m_db_connectionstring = "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3";
            }

            if (m_db_connectionstring != "null")
            {
                m_db = new NHibernateECData();
                m_db.Initialise(m_db_connectionstring);
                m_db_initialized = true;
            }
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);

            if (m_db_initialized)
            {
                foreach (EntityBase eb in scene.Entities)
                {
                    foreach (ECData data in m_db.GetComponents(eb.UUID))
                    {
                        SaveLocal(data);
                    }
                }
            }
        }

        public void RemoveRegion(Scene scene)
        {
            m_scenes.Remove(scene);
            scene.EventManager.OnClientConnect -= new EventManager.OnClientConnectCoreDelegate(HandleNewClient);
        }

        public void RegionLoaded(Scene scene)
        {
            scene.EventManager.OnClientConnect += new EventManager.OnClientConnectCoreDelegate(HandleNewClient);
        }

        #endregion

        #region Event handlers

        private void HandleNewClient(OpenSim.Framework.Client.IClientCore client)
        {
            NaaliClientView naali;
            if (client.TryGet<NaaliClientView>(out naali))
            {
                naali.OnBinaryGenericMessage += new OpenSim.Region.ClientStack.LindenUDP.LLClientView.BinaryGenericMessage(HandleGenericBinaryMessage);
                naali.AddGenericPacketHandler("ECString", DummyGenericMessageHandler);
                naali.AddGenericPacketHandler("ECRemove", HandleEcRemoveGenericMessage);
                naali.AddGenericPacketHandler("ECSync", DummyGenericMessageHandler);

                SendAllData(naali);
            }
        }

        private void DummyGenericMessageHandler(object sender, string method, List<string> args)
        {
            return; //Handle this really in BinaryGenericMessageHandler
        }

        private void HandleGenericBinaryMessage(object sender, string method, byte[][] args)
        {
            UUID entityId;
            string componentName;
            string componentType;
            byte[] data;

            switch (method.ToLower())
            {
                case "ecsync":
                    if (args.Length >= 2)
                    {
                        entityId = new UUID(Util.FieldToString(args[0]));
                        int rpdLen = 0;
                        int idx = 0;

                        //calculate array length
                        for (int i = 1; i < args.Length; i++)
                        {
                            rpdLen += args[i].Length;
                        }
                        data = new byte[rpdLen];

                        //copy rest of the arrays to one arrays
                        for (int i = 1; i < args.Length; i++)
                        {
                            args[i].CopyTo(data, idx);
                            idx += args[i].Length;
                        }
                        idx = 0;

                        //parse componentType from array
                        StringBuilder buffer = new StringBuilder();
                        while ((idx < data.Length) && (data[idx] != 0))
                        {
                            char c = (char)data[idx++];
                            buffer.Append(c);
                        }
                        componentType = buffer.ToString();
                        idx++;

                        //parse componentName from array
                        buffer = new StringBuilder();
                        while ((idx < data.Length) && (data[idx] != 0))
                        {
                            char c = (char)data[idx++];
                            buffer.Append(c);
                        }
                        componentName = buffer.ToString();
                        idx++;

                        byte[] ecdata = new byte[data.Length - idx];
                        Buffer.BlockCopy(data, idx, ecdata, 0, data.Length - idx);

                        SaveECData(sender, new ECData(entityId, componentType, componentName, ecdata, false));
                    }
                    break;
                case "ecstring":

                    List<string> sargs = new List<string>();
                    foreach (byte[] arg in args)
                    {
                        string s = FieldToUTF8String(arg);
                        sargs.Add(s);
                    }
                    HandleEcStringGenericMessage(sender, method, sargs);
                    break;
                default:
                    return;
            }
        }

        private void HandleEcStringGenericMessage(object sender, string method, List<string> args)
        {
            if (method.ToLower() == "ecstring")
            {
                if (args.Count >= 4)
                {
                    UUID entityId = new UUID(args[0]);
                    string componentType = args[1];
                    string componentName = args[2];
                    string data = String.Empty;
                    for (int i = 3; i < args.Count; i++)
                    {
                        data += args[i];
                    }

                    ECData component = new ECData(entityId, componentType, componentName, data);
                    SaveECData(sender, component);
                }
            }
        }

        private void HandleEcRemoveGenericMessage(object sender, string method, List<string> args)
        {
            if (method.ToLower() == "ecremove")
            {
                if (args.Count >= 3)
                {
                    UUID entityId = new UUID(args[0]);
                    string componentType = args[1];
                    string componentName = args[2];

                    ECData component = new ECData(entityId, componentType, componentName, String.Empty);
                    RemoveECData(sender, component);
                }
            }
        }

        #endregion

        protected static string FieldToUTF8String(byte[] bytes)
        {
            if (bytes.Length > 0 && bytes[bytes.Length - 1] == 0x00)
                return UTF8Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
            else
                return UTF8Encoding.UTF8.GetString(bytes);
        }

        private void SaveLocal(ECData component)
        {
            if (!m_entity_components.ContainsKey(component.EntityID))
            {
                m_entity_components[component.EntityID] = new Entity(component.EntityID);
            }
            m_entity_components[component.EntityID].Components[new KeyValuePair<string, string>(component.ComponentType, component.ComponentName)] = component;
        }

        public void SendAllData(NaaliClientView client)
        {
            foreach (KeyValuePair<UUID, Entity> entity in m_entity_components)
            {
                if (entity.Value != null)
                {
                    foreach (KeyValuePair<KeyValuePair<string, string>, ECData> data in entity.Value.Components)
                    {
                        client.SendECData(data.Value);
                    }
                }
            }
        }

        public void SendECDataToAll(ECData component)
        {
            foreach (Scene scene in m_scenes)
            {
                scene.ForEachClient(
                    delegate(IClientAPI client)
                    {
                        if (client is NaaliClientView)
                        {
                            ((NaaliClientView)client).SendECData(component);
                        }
                    }, true
                );
            }
        }

        public void SendECRemovedToAll(UUID entityId, string componentType, string componentName)
        {
            List<string> msgArgs = new List<string>();
            msgArgs.Add(entityId.ToString());
            msgArgs.Add(componentType);
            msgArgs.Add(componentName);

            foreach (Scene scene in m_scenes)
            {
                scene.ForEachClient(
                    delegate(IClientAPI client)
                    {
                        if (client is NaaliClientView)
                        {
                            ((NaaliClientView)client).SendGenericMessage("ecremove", msgArgs);
                        }
                    }, true
                );
            }
        }

        public bool SaveECData(object sender, ECData component)
        {
            try
            {
                bool save = true;
                if (m_ec_update_callbacks.ContainsKey(component.ComponentType) && m_ec_update_callbacks[component.ComponentType] != null)
                {
                    save = m_ec_update_callbacks[component.ComponentType](sender, ref component);
                }

                if (save)
                {
                    SaveLocal(component);

                    SendECDataToAll(component);

                    if (m_db_initialized)
                        return m_db.StoreComponent(component);
                }

                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[ECDATA]: Error saving ECData for component because Exception {0} occurred: {1}", e.Message, e.StackTrace);
                return false;
            }
        }

        public bool RemoveECData(object sender, ECData component)
        {
            try
            {
                bool remove = true;
                if (m_ec_update_callbacks.ContainsKey(component.ComponentType) && m_ec_remove_callbacks[component.ComponentType] != null)
                {
                    remove = m_ec_remove_callbacks[component.ComponentType](sender, component.EntityID, component.ComponentType, component.ComponentName);
                }

                if (remove)
                {
                    if (m_entity_components[component.EntityID] != null)
                    {
                        return false;
                    }

                    m_entity_components[component.EntityID].Components.Remove(new KeyValuePair<string, string>(component.ComponentType, component.ComponentName));

                    SendECRemovedToAll(component.EntityID, component.ComponentType, component.ComponentName);

                    if (m_db_initialized)
                        return m_db.RemoveComponent(component);
                }

                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[ECDATA]: Error removing ECData because Exception {0} occurred: {1}", e.Message, e.StackTrace);
                return false;
            }
        }

        public void RegisterECUpdateCallback(string componentType, UpdateECData callback)
        {
            m_ec_update_callbacks[componentType] = callback;
        }

        public void RegisterECRemoveCallback(string componentType, RemoveECData callback)
        {
            m_ec_remove_callbacks[componentType] = callback;
        }
    }
}
