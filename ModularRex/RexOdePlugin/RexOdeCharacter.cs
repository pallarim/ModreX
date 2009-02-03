using System;
using OpenSim.Region.Physics.OdePlugin;
using OpenSim.Region.Physics.Manager;
using Ode.NET;

namespace ModularRex.RexOdePlugin
{
    public class RexOdeCharacter : OdeCharacter
    {
        public RexOdeCharacter(String avName, OdeScene parent_scene, PhysicsVector pos, CollisionLocker dode, PhysicsVector size, float pid_d, float pid_p, float capsule_radius, float tensor, float density, float height_fudge_factor, float walk_divisor, float rundivisor):
            base(avName, parent_scene, pos, dode, size, pid_d, pid_p, capsule_radius, tensor, density, height_fudge_factor, walk_divisor, rundivisor)
        {
        
        }

        public override void Move(float timeStep)
        {
            //  no lock; for now it's only called from within Simulate()

            // If the PID Controller isn't active then we set our force
            // calculating base velocity to the current position

            if (Body == IntPtr.Zero)
                return;

            if (m_pidControllerActive == false)
            {
                _zeroPosition = d.BodyGetPosition(Body);
            }
            //PidStatus = true;

            // rex, added height check
            // Done in a rather ugly way, direct setting of position / linear vel.
            d.Vector3 tempPos = d.BodyGetPosition(Body);
            if (tempPos.Z > ((RexOdeScene)_parent_scene).m_flightCeilingHeight)
            {
                tempPos.Z = ((RexOdeScene)_parent_scene).m_flightCeilingHeight;
                d.BodySetPosition(Body, tempPos.X, tempPos.Y, tempPos.Z);
                d.Vector3 tempVel = d.BodyGetLinearVel(Body);
                if (tempVel.Z > 0.0f)
                {
                    tempVel.Z = 0.0f;
                    d.BodySetLinearVel(Body, tempVel.X, tempVel.Y, tempVel.Z);
                }
                if (_target_velocity.Z > 0.0f)
                    _target_velocity.Z = 0.0f;
            }
            // endrex
            base.Move(timeStep);
        }            
    }
}
