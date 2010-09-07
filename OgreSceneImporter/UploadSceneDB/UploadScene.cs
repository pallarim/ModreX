using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace OgreSceneImporter.UploadSceneDB
{
    
    public class UploadScene
    {
        int id;

        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        string name;
        string xmlfile;

        UUID sceneId;

        public UploadScene()
        { }

        public UploadScene(UUID sceneid, string name, string xml)
        {
            this.sceneId = sceneid;
            this.name = name;
            this.xmlfile = xml;
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual string SceneId
        {
            get { return sceneId.ToString(); }
            set { sceneId = new UUID(value); }
        }

        public virtual string XmlFile
        {
            get { return xmlfile; }
            set { xmlfile = value; }
        }


    }
}
