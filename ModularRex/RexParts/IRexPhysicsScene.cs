using System;
using OpenSim.Region.Physics.Manager;

namespace ModularRex.RexParts
{
    public interface IRexPhysicsScene
    {
        uint Raycast(PhysicsVector pos, PhysicsVector dir, float raylength, uint ignoreId);
        void SetMaxFlightHeight(float maxheight);
    }
}
