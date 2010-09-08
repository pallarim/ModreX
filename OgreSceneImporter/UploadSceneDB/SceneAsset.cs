using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace OgreSceneImporter.UploadSceneDB
{
    public class SceneAsset
    {
        int id;

        private UUID assetId; // scnene id 

        private UUID assetStorageId; // id with assetservice, for locating created asset

        string name;

        private UUID sceneId;

        int assetType; // 1 = mesh, 2 = material, 3 = texture

        long localId;

        UUID entityId;

        public SceneAsset()
        { }

        public SceneAsset(UUID assetid, UUID sceneid, string name, int type)
        {
            this.assetId = assetid;
            this.sceneId = sceneid;
            this.name = name;
            this.assetType = type;
        }

        public SceneAsset(UUID sceneassetid, UUID sceneid, string name, int type, uint localId, UUID entityId)//, UUID Id)
        {
            this.assetId = sceneassetid;
            this.sceneId = sceneid;
            this.name = name;
            this.assetType = type;
            this.localId = localId;
            this.entityId = entityId;
            //this.assetStorageId = Id;
        }

        public virtual int Id
        {
            get { return id; }
            set { id = value; }
        }

        public virtual string AssetId
        {
            get { return assetId.ToString(); }
            set { assetId = new UUID(value); }
        }

        public virtual string Name
        {
            get {return name;}
            set {name = value;}
        }
        public virtual string SceneId
        {
            get {return sceneId.ToString();}
            set {sceneId = new UUID(value);}
        }
        public virtual int AssetType
        {
            get { return assetType; }
            set { assetType = value; }
        }

        public virtual uint LocalId
        {
            get { return (uint) localId; }
            set { localId = Convert.ToInt64(value); }
        }

        public virtual long longLocalId
        {
            get { return localId; }
            set { localId = value; }
        }

        public virtual string EntityId
        {
            get { return entityId.ToString(); }
            set { entityId = new UUID(value); }
        }

        //public virtual string stringEntityId
        //{
        //    get { return entityId.ToString(); }
        //    set { entityId = new UUID(value); }
        //}

        /*
        public virtual string AssetStorageId 
        {
            get { return assetStorageId.ToString(); }
            set { assetStorageId = new UUID(value); }
        }
        //*/        
    }
}
