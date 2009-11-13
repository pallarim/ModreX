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

        public static int GetAssetTypeFromMimeType(string contentType)
        {
            switch (contentType)
            {
                case "image/x-j2c":
                case "image/jp2":
                    return (int)AssetType.Texture;
                case "application/ogg":
                    return (int)AssetType.Sound;
                case "application/vnd.ll.callingcard":
                case "application/x-metaverse-callingcard":
                    return (int)AssetType.CallingCard;
                case "application/vnd.ll.landmark":
                case "application/x-metaverse-landmark":
                    return (int)AssetType.Landmark;
                case "application/vnd.ll.clothing":
                case "application/x-metaverse-clothing":
                    return (int)AssetType.Clothing;
                case "application/vnd.ll.primitive":
                case "application/x-metaverse-primitive":
                    return (int)AssetType.Object;
                case "application/vnd.ll.notecard":
                case "application/x-metaverse-notecard":
                    return (int)AssetType.Notecard;
                case "application/vnd.ll.folder":
                    return (int)AssetType.Folder;
                case "application/vnd.ll.rootfolder":
                    return (int)AssetType.RootFolder;
                case "application/vnd.ll.lsltext":
                case "application/x-metaverse-lsl":
                    return (int)AssetType.LSLText;
                case "application/vnd.ll.lslbyte":
                case "application/x-metaverse-lso":
                    return (int)AssetType.LSLBytecode;
                case "image/tga":
                    // Note that AssetType.TextureTGA will be converted to AssetType.ImageTGA
                    return (int)AssetType.ImageTGA;
                case "application/vnd.ll.bodypart":
                case "application/x-metaverse-bodypart":
                    return (int)AssetType.Bodypart;
                case "application/vnd.ll.trashfolder":
                    return (int)AssetType.TrashFolder;
                case "application/vnd.ll.snapshotfolder":
                    return (int)AssetType.SnapshotFolder;
                case "application/vnd.ll.lostandfoundfolder":
                    return (int)AssetType.LostAndFoundFolder;
                case "audio/x-wav":
                    return (int)AssetType.SoundWAV;
                case "image/jpeg":
                    return (int)AssetType.ImageJPEG;
                case "application/vnd.ll.animation":
                case "application/x-metaverse-animation":
                    return (int)AssetType.Animation;
                case "application/vnd.ll.gesture":
                case "application/x-metaverse-gesture":
                    return (int)AssetType.Gesture;
                case "application/x-metaverse-simstate":
                    return (int)AssetType.Simstate;
                case "application/vnd.rex.ogremate":
                    return 45;
                case "application/vnd.rex.ogremesh":
                    return 43;
                case "application/vnd.rex.ogrepart":
                    return 47;
                case "application/vnd.rex.ogreskel":
                    return 44;
                case "application/x-shockwave-flash":
                    return 49;
                case "application/xml":
                case "text/xml":
                    return 46;
                case "application/octet-stream":
                default:
                    return (sbyte)AssetType.Unknown;
            }
        }

        public static int GetAssetTypeFromFileExtension(string fileExtension)
        {
            switch (fileExtension)
            {
                //Standard file types
                case "j2k":
                case "jp2":
                case "jpeg2000":
                case "j2c":
                case "texture":
                    return (int)AssetType.Texture;
                case "jpe":
                case "jpg":
                case "jpeg":
                    return (int)AssetType.ImageJPEG;
                case "tga":
                    return (int)AssetType.ImageTGA;
                case "wav":
                    return (int)AssetType.SoundWAV;
                case "ogg":
                case "sound":
                    return (int)AssetType.Sound;

                //LL/OpenSim spesific file types
                case "callingcard":
                    return (int)AssetType.CallingCard;
                case "landmark":
                    return (int)AssetType.Landmark;
                case "clothing":
                    return (int)AssetType.Clothing;
                case "primitive":
                    return (int)AssetType.Object;
                case "notecard":
                    return (int)AssetType.Notecard;
                case "lsl":
                    return (int)AssetType.LSLText;
                case "lso":
                    return (int)AssetType.LSLBytecode;
                case "bodypart":
                    return (int)AssetType.Bodypart;
                case "bvh": //realXtend addition
                case "animation":
                    return (int)AssetType.Animation;
                case "gesture":
                    return (int)AssetType.Gesture;
                case "simstate":
                    return (int)AssetType.Simstate;

                //realXtend spesific file types
                case "mesh":
                    return 43;
                case "xml":
                    return 46;
                case "swf":
                    return 49;
                case "particle":
                    return 47;
                case "skeleton":
                    return 44;
                case "material":
                    return 45;
                default:
                    return (int)AssetType.Unknown;
            }
        }
    }
}
