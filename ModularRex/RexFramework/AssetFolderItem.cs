using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public class AssetFolder
    {
        public virtual int Id { get; set; }
        public virtual string ParentPath { get; set; }
        public virtual string Name { get; set; }

        public AssetFolder() { }
        public AssetFolder(string parentPath, string name)
        {
            ParentPath = parentPath;
            Name = name;
        }
    }

    public class AssetFolderItem : AssetFolder
    {
        public virtual UUID AssetID { get; set; }

        public AssetFolderItem() { }
        public AssetFolderItem(string parentPath, string name, UUID assetID)
            : base(parentPath, name)
        {
            AssetID = assetID;
        }
    }
}
 