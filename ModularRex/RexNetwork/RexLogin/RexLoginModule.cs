using System;
using System.Collections.Generic;
using System.Text;
using Nini.Config;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexNetwork.RexLogin
{
    class RexLoginModule : IRegionModule 
    {
        private RexLocalLoginService m_lls;
        private Scene m_firstScene;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_firstScene = scene;
        }

        public void PostInitialise()
        {
            //m_lls = new RexLocalLoginService();
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RexLoginOverrider"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
