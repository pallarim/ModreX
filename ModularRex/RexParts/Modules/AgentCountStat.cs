using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System.Collections;
using OpenSim.Framework;

namespace ModularRex.RexParts.Modules
{
    public class AgentCountStat : IRegionModule
    {
        private List<Scene> m_scenes = new List<Scene>();

        #region IRegionModule Members

        public void Initialise(OpenSim.Region.Framework.Scenes.Scene scene, Nini.Config.IConfigSource source)
        {
            m_scenes.Add(scene);
        }

        public void PostInitialise()
        {
            MainServer.Instance.AddHTTPHandler("/AgentCountStat/", StatsPage);
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AgentCountStatModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion

        public Hashtable StatsPage(Hashtable request)
        {
            int count = 0;
            foreach (Scene s in m_scenes)
            {
                count += s.SceneGraph.GetRootAgentCount();
            }

            Hashtable reply = new Hashtable();

            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = count.ToString();
            reply["content_type"] = "text/plain";

            return reply;
        }
    }
}
