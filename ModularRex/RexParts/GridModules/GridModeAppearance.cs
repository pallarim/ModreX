using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using ModularRex.RexNetwork;
using ModularRex.RexParts.Modules;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace ModularRex.RexParts.GridModules
{
    public class GridModeAppearance : IRegionModule
    {
        #region Utils
        /// <summary>
        /// method for retrieving the data from the provided URL
        /// </summary>
        /// <param name="url">URL we're scraping</param>
        /// <remarks>Snippet from: http://www.dreamincode.net/code/snippet2140.htm</remarks>
        /// <returns></returns>
        private static string LoadSiteContents(string url)
        {
            try
            {
                //create a new WebRequest object
                WebRequest request = WebRequest.Create(url);

                //create StreamReader to hold the returned request
                StreamReader stream = new StreamReader(request.GetResponse().GetResponseStream());

                //StringBuilder to hold info from the request
                StringBuilder builder = new StringBuilder();

                //now loop through the response
                while (!(stream.Peek() == 0))
                {
                    //now make sure we're not looking at a blank line
                    if (stream.ReadLine().Length > 0) builder.Append(stream.ReadLine());
                }

                //close up the StreamReader
                stream.Close();

                //return the information
                return builder.ToString();
            }
            catch (Exception ex)
            {
                //put your error handling here
                return string.Empty;
            }
        }
        #endregion

        private readonly List<Scene> m_scenes = new List<Scene>();
        private readonly Dictionary<UUID,string> m_appearances = new Dictionary<UUID, string>();
        private IConfigSource m_config;

        #region Implementation of IRegionModule

        public void Initialise(Scene scene, IConfigSource source)
        {
            if (!source.Configs["Startup"].GetBoolean("gridmode", false))
                return;

            if (!source.Configs["realXtend"].GetBoolean("enabled", false))
                return;

            lock (m_scenes)
                m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;

            m_config = source;
        }

        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            string avatarURL = GetAgentURL(client.AgentId);
            m_appearances[client.AgentId] = avatarURL;
            IClientRexAppearance rex;
            if (client.TryGet(out rex))
            {
                rex.RexAvatarURL = avatarURL;
            }

            foreach (Scene scene in m_scenes)
            {
                scene.RequestModuleInterface<ModrexAppearance>().SendAppearanceToAllUsers(client.AgentId, avatarURL,
                                                                                          false);
            }
        }

        private string GetAgentURL(UUID agent)
        {
            string url = m_config.Configs["realXtend"].GetString("GridAvatarSource") + agent;

            return LoadSiteContents(url);
        }

        public void PostInitialise()
        {
            
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "ModRex Grid Mode Appearance Module"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion
    }
}