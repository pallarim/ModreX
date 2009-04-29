using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;

namespace ModularRex.RexParts.Modules
{
    public class EntryAreaModule : IRegionModule
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Vector3 m_minPos;
        private Vector3 m_maxPos;
        private Random m_random;
        private bool m_enabled;

        private Scene m_scene;

        public void Initialise(Scene scene, IConfigSource config)
        {
            m_minPos = new Vector3();
            m_maxPos = new Vector3();
            m_random = new Random();
            m_enabled = false;

            if (config.Configs["EntryArea"] != null)
            {
                try
                {
                    m_enabled = config.Configs["EntryArea"].GetBoolean("enabled", false);

                    if (!m_enabled)
                    {
                        return;
                    }

                    m_minPos.X = config.Configs["EntryArea"].GetFloat("entry_area_min_x", 0);
                    m_minPos.Y = config.Configs["EntryArea"].GetFloat("entry_area_min_y", 0);
                    m_minPos.Z = config.Configs["EntryArea"].GetFloat("entry_area_min_z", 0);
                    m_maxPos.X = config.Configs["EntryArea"].GetFloat("entry_area_max_x", 256);
                    m_maxPos.Y = config.Configs["EntryArea"].GetFloat("entry_area_max_y", 256);
                    m_maxPos.Z = config.Configs["EntryArea"].GetFloat("entry_area_max_z", 256);

                    m_log.Info("[ENTRYAREA]: Entry area set to (" + m_minPos.X.ToString() + "," + m_minPos.Y.ToString() + "," + m_minPos.Z.ToString() +
                        ") - (" + m_maxPos.X.ToString() + "," + m_maxPos.Y.ToString() + "," + m_maxPos.Z.ToString() + ")");
                }
                catch (Exception elmeri)
                {
                    m_log.Error("[ENTRYAREA]: Error reading configuration", elmeri);
                }
            }

            m_scene = scene;
            if (m_enabled)
            {
                scene.EventManager.OnNewClient += TransferClientToEntryArea;
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            if (m_enabled)
            {
                m_scene.EventManager.OnNewClient -= TransferClientToEntryArea;
            }
        }

        public string Name
        {
            get { return "EntryAreaModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        private void TransferClientToEntryArea(IClientAPI client)
        {
            //client.StartPos = getNewStartPos();
            //m_log.Info("[ENTRYAREA]: Sent user " + client.Name + " to " + client.StartPos.ToString());
            ScenePresence sp = m_scene.GetScenePresence(client.AgentId);
            if (sp != null)
            {
                sp.Teleport(getNewStartPos());

                m_log.Info("[ENTRYAREA]: Sent user " + client.Name + " to " + sp.AbsolutePosition);
            }
        }

        private Vector3 getNewStartPos()
        {
            float X = m_random.Next(Convert.ToInt32(m_minPos.X + 1), Convert.ToInt32(m_maxPos.X - 1));
            float Y = m_random.Next(Convert.ToInt32(m_minPos.Y + 1), Convert.ToInt32(m_maxPos.Y - 1));
            float Z = m_random.Next(Convert.ToInt32(m_minPos.Z + 1), Convert.ToInt32(m_maxPos.Z - 1));

            return new Vector3(X, Y, Z);
        }
    }
}
