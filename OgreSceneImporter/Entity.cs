using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OgreSceneImporter
{
    public class Entity
    {
        private List<string> m_materials = new List<string>();
        public bool Visible;
        public bool CastShadows;
        public float RenderingDistance;

        public string Name = String.Empty;
        public string MeshName = String.Empty;

        public Entity()
        {
        }

        public Entity(string name, string meshName)
        {
            Name = name;
            MeshName = meshName;
        }

        internal void SetMaterialName(string mat)
        {
            m_materials.Add(mat);
        }
    }
}
