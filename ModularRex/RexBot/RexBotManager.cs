/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Examples.RexBot
{
    public class RexBotManager : IRegionModule
    {
        #region IRegionModule Members

        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const string DEFAULT_CONFIG_FILENAME = "RexBots.xml";

        private Scene m_scene;
        private List<RexBot> m_bots;

        private AgentCircuitData m_aCircuitData;

        private NavMeshManager m_navMeshManager;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_navMeshManager = new NavMeshManager();

            m_scene = scene;
            m_aCircuitData = new AgentCircuitData();
            m_aCircuitData.child = false;
            m_bots = new List<RexBot>();
        }

        public void PostInitialise()
        {
            RegionInfo regionInfo = m_scene.RegionInfo;

            Vector3 pos = new Vector3(110, 129, 27);

            AddAvatars();
        }

        // add avatar defined in config file to scene
        private void AddAvatars()
        {
            readBotConfig();
        }

        // read bot data from config file and add avatars to scene
        private void readBotConfig()
        {
            //No need to warn
            //m_log.Warn("[RexBotManager]: Reading bot config file.");
            XmlDocument xml = new XmlDocument();
            try
            {
                string file = Path.Combine(Util.configDir(), DEFAULT_CONFIG_FILENAME);
                xml.Load(file);

                XmlNodeList paths = xml.GetElementsByTagName("navi_mesh");
                foreach (XmlNode node in paths)
                {
                    createNavMesh(node);
                }

                XmlNodeList bots = xml.GetElementsByTagName("bot");
                foreach (XmlNode node in bots)
                {
                    createAvatar(node);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                m_log.InfoFormat("[RexBotManager]: Bot config file {0} not present. Bots not loaded.", DEFAULT_CONFIG_FILENAME);
            }
            catch (System.IO.IOException e)
            {
                m_log.Warn("[RexBotManager]: Failed to load bot config file: " + DEFAULT_CONFIG_FILENAME + ". Reason: " + e.Message);
            }
            catch (System.Xml.XmlException e)
            {
                m_log.Error("[RexBotManager]: Failed to parse bot config file: " + DEFAULT_CONFIG_FILENAME + ". Reason: " + e.Message);
            }
        }

        // read config data for single navmesh
        private void createNavMesh(XmlNode node)
        {
            NavMesh mesh = new NavMesh();
            NavMeshSerializer serializer = new NavMeshSerializer();
            serializer.Import(mesh, node);
            try
            {
                m_navMeshManager.AddNavMesh(mesh);
            }
            catch (System.ArgumentException)
            {
                m_log.Error("[RexBotManager]: NavMesh with the name: " + mesh.Name + " already exists! NavMeshes must be unique.");
            }
        }

        // read config data for single bot and add the avatar to the scene
        private void createAvatar(XmlNode node)
        {
            RexBotSerializer serializer = new RexBotSerializer();

            //first get region name so we know if we add this bot to this scene
            //if value is null we'll add it to all scenes
            string regionName = serializer.GetRegionName(node);
            if (regionName != null)
            {
                if (regionName != m_scene.RegionInfo.RegionName)
                    return;
            }

            RexBot m_character = new RexBot(m_scene, m_navMeshManager);
            
            serializer.ImportName(m_character, node); // import avatar name from file, we need it early

            m_aCircuitData.firstname = m_character.FirstName;
            m_aCircuitData.lastname = m_character.LastName;
            m_aCircuitData.circuitcode = m_character.CircuitCode;
            m_scene.AuthenticateHandler.AgentCircuits.Add(m_character.CircuitCode, m_aCircuitData);

            m_scene.AddNewClient(m_character);
            m_character.Initialize();
            m_bots.Add(m_character);

            serializer.ImportRexBot(m_character, node); // import after bot has been properly added to scene, because import might call some code that needs it

            m_log.Info("[RexBotManager]: Added bot " + m_character.Name + " to scene.");
        }

        public void Close()
        {

            m_scene = null;
            m_bots.Clear();
        }

        public string Name
        {
            get { return GetType().AssemblyQualifiedName; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion
    }
}
