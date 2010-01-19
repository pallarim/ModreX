using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules.Scripting.WorldComm;
using OpenSim.Framework;
using OpenMetaverse;

namespace ModularRex.RexParts.RexPython
{
    public delegate void ChatToRexScript(int channel, string name, UUID objectId, string message, UUID senderId);

    public class ScriptListener : IRegionModule
    {
        private Scene m_scene = null;
        private ListenerManager m_listenerManager;
        private int m_whisperdistance = 10;
        private int m_saydistance = 30;
        private int m_shoutdistance = 100;

        public event ChatToRexScript OnNewMessage = null;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource config)
        {
            m_scene = scene;

            int maxlisteners = 1000;
            int maxhandles = 64;
            try
            {
                m_whisperdistance = config.Configs["Chat"].GetInt("whisper_distance", m_whisperdistance);
                m_saydistance = config.Configs["Chat"].GetInt("say_distance", m_saydistance);
                m_shoutdistance = config.Configs["Chat"].GetInt("shout_distance", m_shoutdistance);
                maxlisteners = config.Configs["LL-Functions"].GetInt("max_listens_per_region", maxlisteners);
                maxhandles = config.Configs["LL-Functions"].GetInt("max_listens_per_script", maxhandles);
            }
            catch (Exception)
            {
            }
            if (maxlisteners < 1) maxlisteners = int.MaxValue;
            if (maxhandles < 1) maxhandles = int.MaxValue;

            m_scene.RegisterModuleInterface<ScriptListener>(this);
            m_listenerManager = new ListenerManager(maxlisteners, maxhandles);
            m_scene.EventManager.OnChatFromClient += DeliverClientMessage;
            m_scene.EventManager.OnChatBroadcast += DeliverClientMessage;
            m_scene.EventManager.OnChatFromWorld += DeliverClientMessage;
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "RexScriptListener"; }
        }

        public void PostInitialise()
        {
        }

        #endregion

        public int Listen(uint localID, UUID itemID, UUID hostID, int channel, string name, UUID id, string msg)
        {
            return m_listenerManager.AddListener(localID, itemID, hostID, channel, name, id, msg);
        }

        private void DeliverClientMessage(Object sender, OSChatMessage e)
        {
            if (null != e.Sender)
                DeliverMessage(e.Type, e.Channel, e.Sender.Name, e.Sender.AgentId, e.Message, e.Position);
            else
                DeliverMessage(e.Type, e.Channel, e.From, UUID.Zero, e.Message, e.Position);
        }

        private void DeliverMessage(ChatTypeEnum type, int channel, string name, UUID id, string msg, Vector3 position)
        {
            foreach (ListenerInfo li in m_listenerManager.GetListeners(UUID.Zero, channel, name, id, msg))
            {
                // Dont process if this message is from yourself!
                // This case might however change in some situations.
                // For example when object has both rex and ll script.
                // In that case if scripts want to communicate with each other
                // with chat msg, then they would need to change this value
                if (li.GetHostID().Equals(id))
                    continue;

                SceneObjectPart sPart = m_scene.GetSceneObjectPart(li.GetHostID());
                if (sPart == null)
                    continue;

                double dis = Util.GetDistanceTo(sPart.AbsolutePosition, position);
                switch (type)
                {
                    case ChatTypeEnum.Whisper:
                        if (dis < m_whisperdistance)
                        {
                            if (OnNewMessage != null)
                            {
                                OnNewMessage(channel, name, li.GetHostID(), msg, id);
                            }
                            //lock (m_pending.SyncRoot)
                            //{
                            //    m_pending.Enqueue(new ListenerInfo(li, name, id, msg));
                            //}
                        }
                        break;

                    case ChatTypeEnum.Say:
                        if (dis < m_saydistance)
                        {
                            if (OnNewMessage != null)
                            {
                                OnNewMessage(channel, name, li.GetHostID(), msg, id);
                            }
                        }
                        break;

                    case ChatTypeEnum.Shout:
                        if (dis < m_shoutdistance)
                        {
                            if (OnNewMessage != null)
                            {
                                OnNewMessage(channel, name, li.GetHostID(), msg, id);
                            }
                        }
                        break;

                    case ChatTypeEnum.Region:
                        if (OnNewMessage != null)
                        {
                            OnNewMessage(channel, name, li.GetHostID(), msg, id);
                        }
                        break;
                }
            }
        }
    }
}
