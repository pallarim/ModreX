using OpenMetaverse;
using System.Collections.Generic;

namespace ModularRex.RexFramework
{
    public interface IModrexObjectsProvider
    {
        RexObjectProperties GetObject(UUID id);
        List<RexObjectProperties> GetObjects();
        bool DeleteObject(UUID id);
    }
}
