using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OgreSceneImporter
{
    public class SceneManager
    {
        public Entity CreateEntity(string name, string meshFile)
        {
            return new Entity(name, meshFile);
        }

        public SceneNode RootSceneNode = new SceneNode();
    }
}
