using System;
using System.Collections.Generic;

using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Physics.OdePlugin;

using System.Runtime.InteropServices;
using Nini.Config;
using Ode.NET;
using OpenMetaverse;
using OpenSim.Framework;

namespace ModularRex.RexOdePlugin
{
 
    public class RexOdePlugin : IPhysicsPlugin
    {
        //protected static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected CollisionLocker ode;
        protected OdeScene _mScene;

        public RexOdePlugin()
        {
            ode = new CollisionLocker();
        }

        public bool Init()
        {
            return true;
        }

        public PhysicsScene GetScene(String sceneIdentifier)
        {
            if (_mScene == null)
            {
                // Initializing ODE only when a scene is created allows alternative ODE plugins to co-habit (according to
                // http://opensimulator.org/mantis/view.php?id=2750).
                d.InitODE();

                _mScene = new RexOdeScene(ode, sceneIdentifier);
            }
            return (_mScene);
        }

        public string GetName()
        {
            return ("RexOpenDynamicsEngine");
        }

        public void Dispose()
        {
        }
    }










    public class RexOdeScene : OdeScene
    {
        public float m_flightCeilingHeight = 2048.0f;
    
        protected IntPtr mCollisionRay;
        protected uint mCollisionRayObjId, mCollisionRayIgnoreId;   
    
        public RexOdeScene(CollisionLocker dode, string sceneIdentifier):base(dode,sceneIdentifier)
        {
            nearCallback = Rexnear;
        }

        public override void Initialise(IMesher meshmerizer, IConfigSource config)
        {
            base.Initialise(meshmerizer,config);
            
            mCollisionRay = d.CreateRay(IntPtr.Zero, 1);
        }


        PhysicsActor AddRexPrim(String name, PhysicsVector position, PhysicsVector size, Quaternion rotation,
                                     IMesh mesh, PrimitiveBaseShape pbs, bool isphysical)
        {
            PhysicsVector pos = new PhysicsVector(position.X, position.Y, position.Z);
            PhysicsVector siz = new PhysicsVector();
            siz.X = size.X;
            siz.Y = size.Y;
            siz.Z = size.Z;
            Quaternion rot = rotation;

            RexOdePrim newPrim;
            lock (OdeLock)
            {
                newPrim = new RexOdePrim(name, this, pos, siz, rot, mesh, pbs, isphysical, ode);

                lock (_prims)
                    _prims.Add(newPrim);
            }

            return newPrim;
        }

        public override PhysicsActor AddPrimShape(string primName, PrimitiveBaseShape pbs, PhysicsVector position,
                                                  PhysicsVector size, Quaternion rotation, bool isPhysical)
        {
            PhysicsActor result;
            IMesh mesh = null;

            if (needsMeshing(pbs))
                mesh = mesher.CreateMesh(primName, pbs, size, 32f, isPhysical);

            result = AddRexPrim(primName, position, size, rotation, mesh, pbs, isPhysical);

            return result;
        }

        public override PhysicsActor AddAvatar(string avName, PhysicsVector position, PhysicsVector size)
        {
            PhysicsVector pos = new PhysicsVector();
            pos.X = position.X;
            pos.Y = position.Y;
            pos.Z = position.Z;
            OdeCharacter newAv = new RexOdeCharacter(avName, this, pos, ode, size, avPIDD, avPIDP, avCapRadius, avStandupTensor, avDensity, avHeightFudgeFactor, avMovementDivisorWalk, avMovementDivisorRun);
            _characters.Add(newAv);
            return newAv;
        }

        protected void Rexnear(IntPtr space, IntPtr g1, IntPtr g2)
        {
            //  no lock here!  It's invoked from within Simulate(), which is thread-locked

            // Test if we're colliding a geom with a space.
            // If so we have to drill down into the space recursively

            if (d.GeomIsSpace(g1) || d.GeomIsSpace(g2))
            {
                if (g1 == IntPtr.Zero || g2 == IntPtr.Zero)
                    return;

                // Separating static prim geometry spaces.
                // We'll be calling near recursivly if one
                // of them is a space to find all of the
                // contact points in the space
                try
                {
                    d.SpaceCollide2(g1, g2, IntPtr.Zero, nearCallback);
                }
                catch (AccessViolationException)
                {
                    m_log.Warn("[PHYSICS]: Unable to collide test a space");
                    return;
                }
                //Colliding a space or a geom with a space or a geom. so drill down

                //Collide all geoms in each space..
                //if (d.GeomIsSpace(g1)) d.SpaceCollide(g1, IntPtr.Zero, nearCallback);
                //if (d.GeomIsSpace(g2)) d.SpaceCollide(g2, IntPtr.Zero, nearCallback);
                return;
            }

            if (g1 == IntPtr.Zero || g2 == IntPtr.Zero)
                return;

            IntPtr b1 = d.GeomGetBody(g1);
            IntPtr b2 = d.GeomGetBody(g2);

            // d.GeomClassID id = d.GeomGetClass(g1);

            String name1 = null;
            String name2 = null;

            if (!geom_name_map.TryGetValue(g1, out name1))
            {
                name1 = "null";
            }
            if (!geom_name_map.TryGetValue(g2, out name2))
            {
                name2 = "null";
            }

            //if (id == d.GeomClassId.TriMeshClass)
            //{
            //               m_log.InfoFormat("near: A collision was detected between {1} and {2}", 0, name1, name2);
            //System.Console.WriteLine("near: A collision was detected between {1} and {2}", 0, name1, name2);
            //}

            // Figure out how many contact points we have
            int count = 0;
            try
            {
                // Colliding Geom To Geom
                // This portion of the function 'was' blatantly ripped off from BoxStack.cs

                if (g1 == g2)
                    return; // Can't collide with yourself

                if (b1 != IntPtr.Zero && b2 != IntPtr.Zero && d.AreConnectedExcluding(b1, b2, d.JointType.Contact))
                    return;

                lock (contacts)
                {
                    count = d.Collide(g1, g2, contacts.GetLength(0), contacts, d.ContactGeom.SizeOf);
                }
            }
            catch (SEHException)
            {
                m_log.Error("[PHYSICS]: The Operating system shut down ODE because of corrupt memory.  This could be a result of really irregular terrain.  If this repeats continuously, restart using Basic Physics and terrain fill your terrain.  Restarting the sim.");
                ode.drelease(world);
                base.TriggerPhysicsBasedRestart();
            }
            catch (AccessViolationException)
            {
                m_log.Warn("[PHYSICS]: Unable to collide test an object");
                return;
            }

            PhysicsActor p1;
            PhysicsActor p2;

            if (!actor_name_map.TryGetValue(g1, out p1))
            {
                p1 = PANull;
            }

            if (!actor_name_map.TryGetValue(g2, out p2))
            {
                p2 = PANull;
            }

            float max_collision_depth = 0f;
            if (p1.CollisionScore + count >= float.MaxValue)
                p1.CollisionScore = 0;
            p1.CollisionScore += count;

            if (p2.CollisionScore + count >= float.MaxValue)
                p2.CollisionScore = 0;
            p2.CollisionScore += count;

            // rex, rayclass collision stop here
            if (d.GeomGetClass(g1) == d.GeomClassID.RayClass || d.GeomGetClass(g2) == d.GeomClassID.RayClass)
            {
                if (g1 != WaterGeom && g2 != WaterGeom)
                {
                    if (count >= 1)
                    {
                        if (d.GeomGetClass(g1) == d.GeomClassID.RayClass)
                        {
                            if (p2 is RexOdePrim)
                            {
                                if (mCollisionRayIgnoreId != ((RexOdePrim)p2).m_localID)
                                    mCollisionRayObjId = ((RexOdePrim)p2).m_localID;    
                            }
                            else if (p2 is OdeCharacter)
                            {
                                if (mCollisionRayIgnoreId != ((OdeCharacter)p2).m_localID)
                                    mCollisionRayObjId = ((OdeCharacter)p2).m_localID;                               
                            }
                        }
                        else
                        {
                            if (p1 is RexOdePrim)
                            {
                                if (mCollisionRayIgnoreId != ((RexOdePrim)p1).m_localID)
                                    mCollisionRayObjId = ((RexOdePrim)p1).m_localID;
                            }
                            else if (p1 is OdeCharacter)
                            {
                                if (mCollisionRayIgnoreId != ((OdeCharacter)p1).m_localID)
                                    mCollisionRayObjId = ((OdeCharacter)p1).m_localID;
                            }
                        }
                    }
                    return;
                }
            } // endrex

            for (int i = 0; i < count; i++)
            {


                max_collision_depth = (contacts[i].depth > max_collision_depth) ? contacts[i].depth : max_collision_depth;
                //m_log.Warn("[CCOUNT]: " + count);
                IntPtr joint;
                // If we're colliding with terrain, use 'TerrainContact' instead of contact.
                // allows us to have different settings

                // We only need to test p2 for 'jump crouch purposes'
                p2.IsColliding = true;

                //if ((framecount % m_returncollisions) == 0)

                switch (p1.PhysicsActorType)
                {
                    case (int)ActorTypes.Agent:
                        p2.CollidingObj = true;
                        break;
                    case (int)ActorTypes.Prim:
                        if (p2.Velocity.X > 0 || p2.Velocity.Y > 0 || p2.Velocity.Z > 0)
                            p2.CollidingObj = true;
                        break;
                    case (int)ActorTypes.Unknown:
                        p2.CollidingGround = true;
                        break;
                    default:
                        p2.CollidingGround = true;
                        break;
                }

                // we don't want prim or avatar to explode

                #region InterPenetration Handling - Unintended physics explosions

                if (contacts[i].depth >= 0.08f)
                {
                    //This is disabled at the moment only because it needs more tweaking
                    //It will eventually be uncommented

                    if (contacts[i].depth >= 1.00f)
                    {
                        //m_log.Debug("[PHYSICS]: " + contacts[i].depth.ToString());
                    }

                    //If you interpenetrate a prim with an agent
                    if ((p2.PhysicsActorType == (int)ActorTypes.Agent &&
                         p1.PhysicsActorType == (int)ActorTypes.Prim) ||
                        (p1.PhysicsActorType == (int)ActorTypes.Agent &&
                         p2.PhysicsActorType == (int)ActorTypes.Prim))
                    {
                        # region disabled code1
                        //contacts[i].depth = contacts[i].depth * 4.15f;
                        
                        //if (p2.PhysicsActorType == (int) ActorTypes.Agent)
                        //{
                        //    p2.CollidingObj = true;
                        //    contacts[i].depth = 0.003f;
                        //    p2.Velocity = p2.Velocity + new PhysicsVector(0, 0, 2.5f);
                        //    OdeCharacter character = (OdeCharacter) p2;
                        //    character.SetPidStatus(true);
                        //    contacts[i].pos = new d.Vector3(contacts[i].pos.X + (p1.Size.X / 2), contacts[i].pos.Y + (p1.Size.Y / 2), contacts[i].pos.Z + (p1.Size.Z / 2));
                        //
                        //}
                        //else
                        //{
                        //
                            //contacts[i].depth = 0.0000000f;
                        //}
                        //if (p1.PhysicsActorType == (int) ActorTypes.Agent)
                        //{

                            //p1.CollidingObj = true;
                            //contacts[i].depth = 0.003f;
                            //p1.Velocity = p1.Velocity + new PhysicsVector(0, 0, 2.5f);
                            //contacts[i].pos = new d.Vector3(contacts[i].pos.X + (p2.Size.X / 2), contacts[i].pos.Y + (p2.Size.Y / 2), contacts[i].pos.Z + (p2.Size.Z / 2));
                            //OdeCharacter character = (OdeCharacter)p1;
                            //character.SetPidStatus(true);
                        //}
                        //else
                        //{

                            //contacts[i].depth = 0.0000000f;
                        //}
                          
                        #endregion
                    }

                    // If you interpenetrate a prim with another prim
                    if (p1.PhysicsActorType == (int)ActorTypes.Prim && p2.PhysicsActorType == (int)ActorTypes.Prim)
                    {
                        #region disabledcode2
                        //OdePrim op1 = (OdePrim)p1;
                        //OdePrim op2 = (OdePrim)p2;
                        //op1.m_collisionscore++;
                        //op2.m_collisionscore++;

                        //if (op1.m_collisionscore > 8000 || op2.m_collisionscore > 8000)
                        //{
                        //op1.m_taintdisable = true;
                        //AddPhysicsActorTaint(p1);
                        //op2.m_taintdisable = true;
                        //AddPhysicsActorTaint(p2);
                        //}

                        //if (contacts[i].depth >= 0.25f)
                        //{
                        // Don't collide, one or both prim will expld.

                        //op1.m_interpenetrationcount++;
                        //op2.m_interpenetrationcount++;
                        //interpenetrations_before_disable = 200;
                        //if (op1.m_interpenetrationcount >= interpenetrations_before_disable)
                        //{
                        //op1.m_taintdisable = true;
                        //AddPhysicsActorTaint(p1);
                        //}
                        //if (op2.m_interpenetrationcount >= interpenetrations_before_disable)
                        //{
                        // op2.m_taintdisable = true;
                        //AddPhysicsActorTaint(p2);
                        //}

                        //contacts[i].depth = contacts[i].depth / 8f;
                        //contacts[i].normal = new d.Vector3(0, 0, 1);
                        //}
                        //if (op1.m_disabled || op2.m_disabled)
                        //{
                        //Manually disabled objects stay disabled
                        //contacts[i].depth = 0f;
                        //}
                        #endregion
                    }

                    if (contacts[i].depth >= 1.00f)
                    {
                        //m_log.Info("[P]: " + contacts[i].depth.ToString());
                        if ((p2.PhysicsActorType == (int)ActorTypes.Agent &&
                             p1.PhysicsActorType == (int)ActorTypes.Unknown) ||
                            (p1.PhysicsActorType == (int)ActorTypes.Agent &&
                             p2.PhysicsActorType == (int)ActorTypes.Unknown))
                        {
                            if (p2.PhysicsActorType == (int)ActorTypes.Agent)
                            {
                                OdeCharacter character = (OdeCharacter)p2;

                                //p2.CollidingObj = true;
                                contacts[i].depth = 0.00000003f;
                                p2.Velocity = p2.Velocity + new PhysicsVector(0, 0, 0.5f);
                                contacts[i].pos =
                                    new d.Vector3(contacts[i].pos.X + (p1.Size.X / 2),
                                                  contacts[i].pos.Y + (p1.Size.Y / 2),
                                                  contacts[i].pos.Z + (p1.Size.Z / 2));
                                character.SetPidStatus(true);
                            }
                            else
                            {
                            }

                            if (p1.PhysicsActorType == (int)ActorTypes.Agent)
                            {
                                OdeCharacter character = (OdeCharacter)p1;

                                //p2.CollidingObj = true;
                                contacts[i].depth = 0.00000003f;
                                p1.Velocity = p1.Velocity + new PhysicsVector(0, 0, 0.5f);
                                contacts[i].pos =
                                    new d.Vector3(contacts[i].pos.X + (p1.Size.X / 2),
                                                  contacts[i].pos.Y + (p1.Size.Y / 2),
                                                  contacts[i].pos.Z + (p1.Size.Z / 2));
                                character.SetPidStatus(true);
                            }
                            else
                            {
                                //contacts[i].depth = 0.0000000f;
                            }
                        }
                    }
                }

                #endregion

                // Logic for collision handling
                // Note, that if *all* contacts are skipped (VolumeDetect)
                // The prim still detects (and forwards) collision events but 
                // appears to be phantom for the world
                Boolean skipThisContact = false;

                if ((p1 is OdePrim) && (((OdePrim)p1).m_isVolumeDetect))
                    skipThisContact = true;   // No collision on volume detect prims

                if (!skipThisContact && (p2 is OdePrim) && (((OdePrim)p2).m_isVolumeDetect))
                    skipThisContact = true;   // No collision on volume detect prims

                if (!skipThisContact && contacts[i].depth < 0f)
                    skipThisContact = true;

                if (!skipThisContact && checkDupe(contacts[i], p2.PhysicsActorType))
                    skipThisContact = true;

                if (!skipThisContact)
                {
                    // If we're colliding against terrain
                    if (name1 == "Terrain" || name2 == "Terrain")
                    {
                        // If we're moving
                        if ((p2.PhysicsActorType == (int)ActorTypes.Agent) &&
                            (Math.Abs(p2.Velocity.X) > 0.01f || Math.Abs(p2.Velocity.Y) > 0.01f))
                        {
                            // Use the movement terrain contact
                            AvatarMovementTerrainContact.geom = contacts[i];
                            _perloopContact.Add(contacts[i]);
                            joint = d.JointCreateContact(world, contactgroup, ref AvatarMovementTerrainContact);
                        }
                        else
                        {
                            // Use the non moving terrain contact
                            TerrainContact.geom = contacts[i];
                            _perloopContact.Add(contacts[i]);
                            joint = d.JointCreateContact(world, contactgroup, ref TerrainContact);
                        }
                        //if (p2.PhysicsActorType == (int)ActorTypes.Prim)
                        //{
                        //m_log.Debug("[PHYSICS]: prim contacting with ground");
                        //}
                    }
                    else if (name1 == "Water" || name2 == "Water")
                    {
                        if ((p2.PhysicsActorType == (int)ActorTypes.Prim))
                        {
                        }
                        else
                        {
                        }

                        //WaterContact.surface.soft_cfm = 0.0000f;
                        //WaterContact.surface.soft_erp = 0.00000f;
                        if (contacts[i].depth > 0.1f)
                        {
                            contacts[i].depth *= 52;
                            //contacts[i].normal = new d.Vector3(0, 0, 1);
                            //contacts[i].pos = new d.Vector3(0, 0, contacts[i].pos.Z - 5f);
                        }
                        WaterContact.geom = contacts[i];
                        _perloopContact.Add(contacts[i]);
                        joint = d.JointCreateContact(world, contactgroup, ref WaterContact);

                        //m_log.Info("[PHYSICS]: Prim Water Contact" + contacts[i].depth);
                    }
                    else
                    {   // we're colliding with prim or avatar
                        // check if we're moving
                        if ((p2.PhysicsActorType == (int)ActorTypes.Agent) &&
                            (Math.Abs(p2.Velocity.X) > 0.01f || Math.Abs(p2.Velocity.Y) > 0.01f))
                        {
                            // Use the Movement prim contact
                            AvatarMovementprimContact.geom = contacts[i];
                            _perloopContact.Add(contacts[i]);
                            joint = d.JointCreateContact(world, contactgroup, ref AvatarMovementprimContact);
                        }
                        else
                        {   // Use the non movement contact
                            contact.geom = contacts[i];
                            _perloopContact.Add(contacts[i]);
                            joint = d.JointCreateContact(world, contactgroup, ref contact);
                        }
                    }
                    d.JointAttach(joint, b1, b2);
                }
                collision_accounting_events(p1, p2, max_collision_depth);
                if (count > geomContactPointsStartthrottle)
                {
                    // If there are more then 3 contact points, it's likely
                    // that we've got a pile of objects, so ...
                    // We don't want to send out hundreds of terse updates over and over again
                    // so lets throttle them and send them again after it's somewhat sorted out.
                    p2.ThrottleUpdates = true;
                }
                //System.Console.WriteLine(count.ToString());
                //System.Console.WriteLine("near: A collision was detected between {1} and {2}", 0, name1, name2);
            }
        }

        public override uint Raycast(PhysicsVector pos, PhysicsVector dir, float rayLength, uint ignoreId)
        {
            try
            {
                mCollisionRayObjId = 0;
                mCollisionRayIgnoreId = ignoreId;

                lock (OdeLock)
                {
                    d.GeomRaySet(mCollisionRay, pos.X, pos.Y, pos.Z, dir.X, dir.Y, dir.Z);
                    d.GeomRaySetLength(mCollisionRay, rayLength);
                    try
                    {
                        d.SpaceCollide2(space, mCollisionRay, IntPtr.Zero, nearCallback);
                    }
                    catch (Exception e)
                    {
                        m_log.Warn("[PHYSICS]: Unable to Raycast:" + e.ToString());
                    }
                    d.GeomRaySetLength(mCollisionRay, 0);
                }
            }
            catch (Exception e)
            {
                m_log.Warn("[PHYSICS]: RexRaycast error:" + e.ToString());
            }
            return mCollisionRayObjId;
        }

        public override void SetMaxFlightHeight(float maxheight)
        {
            m_flightCeilingHeight = maxheight;
        }
    }
}