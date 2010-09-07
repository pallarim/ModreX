using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace OgreSceneImporter.UploadSceneDB
{
    public class RegionScene
    {
        int id;
        private UUID m_regionId;
        private UUID m_sceneId;
        //char[] regionId;
        //char[] sceneId;

        public RegionScene()
        { }

        public RegionScene(UUID regionid, UUID sceneid)
        {
            this.m_regionId = regionid;
            this.m_sceneId = sceneid;
        }

        public RegionScene(string regionid, string sceneid)
        {
            this.m_regionId = new UUID(regionid);
            this.m_sceneId = new UUID(sceneid);
        }


        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string RegionId
        {
            get {
                return m_regionId.ToString();
            }
            set { 
                m_regionId = new UUID(value);
            }
        }
    
        public virtual string SceneId
        {
            get {
                //return m_sceneId.ToString().ToCharArray();
                return m_sceneId.ToString();
                //return sceneId; 
            }
            set {
                //m_sceneId = new UUID(new String(value));
                m_sceneId = new UUID(value);
                //sceneId = value; 
            }
        }

    }
}
