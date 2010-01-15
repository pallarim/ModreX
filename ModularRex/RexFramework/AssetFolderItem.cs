using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public class AssetFolder
    {
        private int m_id = 0;
        private string m_parentPath = String.Empty;
        private string m_name = String.Empty;

        public virtual int Id
        {
            get { return m_id; }
            set { m_id = value; }
        }
        public virtual string ParentPath
        {
            get { return m_parentPath; }
            set { m_parentPath = value; }
        }
        public virtual string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public AssetFolder() { }
        public AssetFolder(string parentPath, string name)
        {
            ParentPath = parentPath;
            Name = name;
        }
    }

    public class AssetFolderItem : AssetFolder
    {
        private UUID m_assetId = UUID.Zero;
        public virtual UUID AssetID
        {
            get { return m_assetId; }
            set { m_assetId = value; }
        }

        public AssetFolderItem() { }
        public AssetFolderItem(string parentPath, string name, UUID assetID)
            : base(parentPath, name)
        {
            AssetID = assetID;
        }
    }
}
 