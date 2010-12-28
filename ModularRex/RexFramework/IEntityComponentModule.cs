using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public delegate bool UpdateECData(object sender, ref ECData data);
    public delegate bool RemoveECData(object sender, UUID entityId, string componentType, string componentName);

    public interface IEntityComponentModule
    {
        List<ECData> GetData(UUID id);
        ECData GetData(UUID id, string typeName, string name);
        bool RemoveECData(object sender, ECData component);
        bool SaveECData(object sender, ECData component);

        void RegisterECUpdateCallback(string componentType, UpdateECData callback);
        void RegisterECRemoveCallback(string componentType, RemoveECData callback);
    }
}
