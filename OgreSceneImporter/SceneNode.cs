using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace OgreSceneImporter
{
    public class SceneNode
    {
        private string m_name = String.Empty;
        private SceneNode m_parent = null;
        public List<Entity> Entities = new List<Entity>();
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 Scale;
        public List<SceneNode> Children = new List<SceneNode>();

        public SceneNode()
        {
        }

        public SceneNode(SceneNode parent)
        {
            m_parent = parent;
        }

        public SceneNode(SceneNode parent, string name)
        {
            m_parent = parent;
            m_name = name;
        }

        internal void AttachObject(Entity pEntity)
        {
            Entities.Add(pEntity);
        }

        internal SceneNode CreateChildSceneNode()
        {
            SceneNode newNode = new SceneNode(this);
            Children.Add(newNode);
            return newNode;
        }

        internal SceneNode CreateChildSceneNode(string name)
        {
            SceneNode newNode = new SceneNode(this, name);
            Children.Add(newNode);
            return newNode;
        }

        internal void SetInitialState()
        {
        }

        internal void SetScale(Vector3 vector3)
        {
            Scale = vector3;
        }
    }
}
