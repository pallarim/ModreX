using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexParts.Helpers
{
    public static class MimeTypeConverter
    {
        public static string GetContentType(int assetType)
        {
            switch ((AssetType)assetType)
            {
                case AssetType.Texture:
                    return "image/x-j2c";
                case AssetType.Sound:
                    return "application/ogg";
                case AssetType.CallingCard:
                    return "application/vnd.ll.callingcard";
                case AssetType.Landmark:
                    return "application/vnd.ll.landmark";
                case AssetType.Clothing:
                    return "application/vnd.ll.clothing";
                case AssetType.Object:
                    return "application/vnd.ll.primitive";
                case AssetType.Notecard:
                    return "application/vnd.ll.notecard";
                case AssetType.Folder:
                    return "application/vnd.ll.folder";
                case AssetType.RootFolder:
                    return "application/vnd.ll.rootfolder";
                case AssetType.LSLText:
                    return "application/vnd.ll.lsltext";
                case AssetType.LSLBytecode:
                    return "application/vnd.ll.lslbyte";
                case AssetType.TextureTGA:
                case AssetType.ImageTGA:
                    return "image/tga";
                case AssetType.Bodypart:
                    return "application/vnd.ll.bodypart";
                case AssetType.TrashFolder:
                    return "application/vnd.ll.trashfolder";
                case AssetType.SnapshotFolder:
                    return "application/vnd.ll.snapshotfolder";
                case AssetType.LostAndFoundFolder:
                    return "application/vnd.ll.lostandfoundfolder";
                case AssetType.SoundWAV:
                    return "audio/x-wav";
                case AssetType.ImageJPEG:
                    return "image/jpeg";
                case AssetType.Animation:
                    return "application/vnd.ll.animation";
                case AssetType.Gesture:
                    return "application/vnd.ll.gesture";
                case (AssetType)45:
                    return "application/vnd.rex.ogremate";
                case (AssetType)43:
                    return "application/vnd.rex.ogremesh";
                case (AssetType)47:
                    return "application/vnd.rex.ogrepart";
                case (AssetType)44:
                    return "application/vnd.rex.ogreskel";
                case (AssetType)49:
                    return "application/x-shockwave-flash";
                case AssetType.Simstate:
                    return "application/x-metaverse-simstate";
                case (AssetType)46:
                    return "application/xml";
                case AssetType.Unknown:
                default:
                    return "application/octet-stream";
            }
        }
    }
}
