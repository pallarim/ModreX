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
        public Vector3 Position = new Vector3(0,0,0);
        public Quaternion Orientation = Quaternion.Identity;
        public Vector3 Scale = new Vector3(1,1,1);
        public Vector3 DerivedPosition;
        public Quaternion DerivedOrientation;
        public Vector3 DerivedScale;

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
            RefreshDerivedTransform();
        }
        
        public void RefreshDerivedTransform()
        {
            DerivedPosition = Position;
            DerivedOrientation = Orientation;
            DerivedScale = Scale;
            
            SceneNode node = m_parent;
            while (node != null)
            {
                DerivedOrientation = node.Orientation * DerivedOrientation;
                DerivedScale *= node.Scale;              
                DerivedPosition = (node.Scale * DerivedPosition) * node.Orientation;
                DerivedPosition += node.Position;
                
                node = node.m_parent;
            }
        }
    }
}