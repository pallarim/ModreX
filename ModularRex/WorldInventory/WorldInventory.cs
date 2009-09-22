using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;

namespace ModularRex.WorldInventory
{
    public class WorldInventory : IRegionModule
    {
        #region IRegionModule Members

        private List<Scene> m_scenes = new List<Scene>();

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scenes.Add(scene);
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "WorldInventory"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion
    }
}
