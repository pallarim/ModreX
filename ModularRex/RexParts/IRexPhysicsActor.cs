using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Physics.Manager;

namespace ModularRex.RexParts
{
    public interface IRexPhysicsActor
    {
        void SetCollisionMesh(byte[] meshdata, string meshname, bool scalemesh);
        void SetBoundsScaling(bool vbScaleMesh);
    }
}
