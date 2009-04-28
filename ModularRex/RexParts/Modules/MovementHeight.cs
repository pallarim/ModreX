using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using log4net;
using System.Reflection;

namespace ModularRex.RexParts.Modules
{
    public class MovementHeight : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private float m_maxHeight = 0;

        #region IRegionModule Members

        public void Close()
        {
        }

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            m_scene = scene;
            if (source.Configs["realXtend"] != null)
            {
                m_maxHeight = source.Configs["realXtend"].GetFloat("FlightCeilingHeight", 0);
            }
            m_scene.AddCommand(this, "flightceiling", "flightceiling <float>", "Set maximum movement height. Zero is disabled", SetFlightCeilingHeight);
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public string Name
        {
            get { return "MovementHeight"; }
        }

        public void PostInitialise()
        {
            m_scene.EventManager.OnNewClient += HandleNewClient;
        }

        private void HandleNewClient(OpenSim.Framework.IClientAPI client)
        {
            client.OnAgentUpdate += HandleAgentUpdate;
        }

        private void HandleAgentUpdate(OpenSim.Framework.IClientAPI remoteClient, OpenSim.Framework.AgentUpdateArgs agentData)
        {
            if (m_maxHeight != 0)
            {
                ScenePresence sp = m_scene.GetScenePresence(remoteClient.AgentId);
                if (sp.AbsolutePosition.Z > m_maxHeight)
                {
                    Vector3 newPos = sp.AbsolutePosition;
                    newPos.Z = m_maxHeight;
                    sp.Teleport(newPos);
                }
            }
        }

        private void SetFlightCeilingHeight(string module, string[] cmd)
        {
            if (cmd.Length >= 1 && cmd[0].ToLower() == "flightceiling")
            {
                if (cmd.Length >= 2)
                {
                    float height = Convert.ToSingle(cmd[1]);
                    m_maxHeight = height;
                }
                else
                {
                    m_log.InfoFormat("[MovementHeight]: Current flight ceiling is set to {0}", m_maxHeight);
                }
            }
        }

        #endregion
    }
}
