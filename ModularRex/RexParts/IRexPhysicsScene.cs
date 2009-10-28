using System;
using OpenSim.Region.Physics.Manager;
using OpenMetaverse;

namespace ModularRex.RexParts
{
    public interface IRexPhysicsScene
    {
        uint Raycast(Vector3 pos, Vector3 dir, float raylength, uint ignoreId);
        void SetMaxFlightHeight(float maxheight);
    }
}
