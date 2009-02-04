using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public class RexAssetData
    {
        public RexAssetData()
        {
        }

        public RexAssetData(UUID assetID)
        {
            this.assetID = assetID;
        }

        public RexAssetData(UUID assetID, string mediaURL, byte refreshRate)
        {
            this.assetID = assetID;
            this.mediaUrl = mediaURL;
            this.refreshRate = refreshRate;
        }

        private UUID assetID;
        public UUID AssetID
        {
            get { return assetID; }
            set { assetID = value; }
        }

        private string mediaUrl = string.Empty;
        public string MediaURL
        {
            get { return mediaUrl; }
            set { mediaUrl = value; }
        }

        private byte refreshRate = 0;
        public byte RefreshRate
        {
            get { return refreshRate; }
            set { refreshRate = value; }
        }

        /// <summary>
        /// This was intented to point to a URL wherefrom client could have downloaded
        /// the asset via TCP insted of the unefficient UDP system.
        /// 
        /// Commented out for now, because doesn't exist yet. Add migration files to NHibernate
        /// module when adding back in.
        /// </summary>
        //public string AssetURL;
    }
}
