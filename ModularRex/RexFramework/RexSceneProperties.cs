using System.Collections.Generic;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public class RexSceneProperties
    {
        private Dictionary<UUID, uint> preloadAssetDictionary = new Dictionary<UUID, uint>();

        public Dictionary<UUID, uint> PreloadAssetDictionary
        {
            get { return preloadAssetDictionary; }
        }

        public RexSceneProperties()
        {
        }
    }
}
