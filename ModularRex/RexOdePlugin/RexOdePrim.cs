using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;
using OpenMetaverse;
using Ode.NET;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Physics.OdePlugin;
using OpenSim.Region.Physics.Meshing;


namespace ModularRex.RexOdePlugin
{
    public class RexOdePrim : OdePrim
    {
        protected Mesh m_OriginalMesh = null;
    
        protected bool m_DotMeshCollision = false;
        protected bool m_PrimVolume = false;
        protected bool m_ReCreateCollision = false;
        
        protected bool m_BoundsScaling = false;
        protected PhysicsVector m_BoundsMin = new PhysicsVector(0, 0, 0);
        protected PhysicsVector m_BoundsMax = new PhysicsVector(0, 0, 0);
    
        public RexOdePrim(String primName, OdeScene parent_scene, PhysicsVector pos, PhysicsVector size,
                       Quaternion rotation, IMesh mesh, PrimitiveBaseShape pbs, bool pisPhysical, CollisionLocker dode)
                       :base(primName, parent_scene, pos, size,rotation, mesh, pbs, pisPhysical, dode)
        {
        
        }

        // Override, take collisionmesh into account
        public override void ProcessTaints(float timestep)
        {
            if (m_taintadd)
            {
                changeadd(timestep);
            }
            if (prim_geom != IntPtr.Zero)
            {
                if (!_position.IsIdentical(m_taintposition, 0f))
                    changemove(timestep);

                if (m_taintrot != _orientation)
                {
                    if(m_DotMeshCollision) // rex
                        rotateogremesh(timestep); // rex 
                    else
                        rotate(timestep);
                }
                //

                if (m_taintPhysics != m_isphysical && !(m_taintparent != _parent))
                    changePhysicsStatus(timestep);
                //

                if (!_size.IsIdentical(m_taintsize, 0))
                {
                    if(m_DotMeshCollision)
                        changesizeogremesh(timestep);
                    else 
                        changesize(timestep);
                }
                if (m_ReCreateCollision) // rex start
                {
                    if (m_DotMeshCollision)
                        changesizeogremesh(timestep);
                    else
                        changesize(timestep);           
                } // rex, end
                //

                if (m_taintshape)
                    changeshape(timestep);
                //

                if (m_taintforce)
                    changeAddForce(timestep);

                if (m_taintaddangularforce)
                    changeAddAngularForce(timestep);

                if (!m_taintTorque.IsIdentical(PhysicsVector.Zero, 0.001f))
                    changeSetTorque(timestep);

                if (m_taintdisable)
                    changedisable(timestep);

                if (m_taintselected != m_isSelected)
                    changeSelectedStatus(timestep);

                if (!m_taintVelocity.IsIdentical(PhysicsVector.Zero, 0.001f))
                    changevelocity(timestep);

                if (m_taintparent != _parent)
                    changelink(timestep);

                if (m_taintCollidesWater != m_collidesWater)
                    changefloatonwater(timestep);

                if (!m_angularlock.IsIdentical(m_taintAngularLock, 0))
                    changeAngularLock(timestep);
            }
            else
            {
                m_log.Error("[REXODEPHYSICS]: The scene reused a disposed PhysActor! *waves finger*, Don't be evil.  A couple of things can cause this.   An improper prim breakdown(be sure to set prim_geom to zero after d.GeomDestroy!   An improper buildup (creating the geom failed).   Or, the Scene Reused a physics actor after disposing it.)");
            }
        }


        // This function should be called only outside of simulation loop -> OdeLock used.
        public override void SetCollisionMesh(byte[] meshdata, string meshname, bool scalemesh)
        {
            lock (_parent_scene.OdeLock)
            {
                m_DotMeshCollision = false;
                if (m_OriginalMesh != null)
                {
                    // Never pinned so skip m_OriginalMesh.releasePinned();
                    m_OriginalMesh = null;
                }

                if (meshdata != null && CreateOSMeshFromDotMesh(meshdata, meshname, scalemesh))
                    m_DotMeshCollision = true;
                    
                m_ReCreateCollision = true;         
            }

            _parent_scene.AddPhysicsActorTaint(this); 
        }
        
        public override void SetBoundsScaling(bool vbScaleMesh)
        {
            if (m_DotMeshCollision)
            {
                m_BoundsScaling = vbScaleMesh;
                m_ReCreateCollision = true;
            }
        }

        private bool CreateOSMeshFromDotMesh(byte[] vData, string vMeshName, bool vbScaleMesh)
        {
            float[] tempVertexList;
            float[] tempBounds;
            int[] tempIndexList;
            string errorMessage;

            RexDotMeshLoader.DotMeshLoader.ReadDotMeshModel(vData, out tempVertexList, out tempIndexList, out tempBounds, out errorMessage);
            
            if (tempVertexList == null || tempIndexList == null)
            {
                m_log.Error("[REXODEPHYSICS]: Error importing mesh:" + vMeshName + ", " + errorMessage);
                return false;            
            }

            m_OriginalMesh = new Mesh();
            
            m_BoundsScaling = vbScaleMesh;
            m_BoundsMin.X = tempBounds[0];
            m_BoundsMin.Y = tempBounds[1];
            m_BoundsMin.Z = tempBounds[2];
            m_BoundsMax.X = tempBounds[3];
            m_BoundsMax.Y = tempBounds[4];
            m_BoundsMax.Z = tempBounds[5];
           
            for (int i = 0; i < tempVertexList.GetLength(0); i=i+3)
            {                
                Vertex vert = new Vertex(tempVertexList[i], tempVertexList[i+1], tempVertexList[i+2]);
                m_OriginalMesh.vertices.Add(vert);
            }

            for (int i = 0; i < tempIndexList.GetLength(0); i=i+3)
            {                
                Triangle tria = new Triangle(m_OriginalMesh.vertices[(tempIndexList[i])], m_OriginalMesh.vertices[(tempIndexList[i+1])], m_OriginalMesh.vertices[(tempIndexList[i+2])]);
                m_OriginalMesh.triangles.Add(tria);
            }           
            return true;
        }

        private Mesh CreateMeshFromOriginal()
        {
            float[] scalefactor = new float[3];        
        
            if (m_OriginalMesh != null)
            {
                Mesh newmesh = m_OriginalMesh.Clone();

                PhysicsVector scalingvector = new PhysicsVector(_size.X, _size.Y, _size.Z);
                if (m_BoundsScaling)
                {
                    PhysicsVector boundssize = m_BoundsMax - m_BoundsMin;
                    if (boundssize.X != 0)
                        scalingvector.X /= boundssize.X;
                    if (boundssize.Y != 0)
                        scalingvector.Z /= boundssize.Y;
                    if (boundssize.Z != 0)
                        scalingvector.Y /= boundssize.Z;
                }

                scalefactor[0] = scalingvector.X;
                scalefactor[1] = scalingvector.Z;
                scalefactor[2] = scalingvector.Y;

                for (int i = 0; i < newmesh.vertices.Count;i++)
                {
                    newmesh.vertices[i].X *= scalefactor[0];
                    newmesh.vertices[i].Y *= scalefactor[1];
                    newmesh.vertices[i].Z *= scalefactor[2];                    
                }
                return newmesh; 
            }
            else
                return null; 
           
        }

        private void rotateogremesh(float timestep)
        {
            d.Quaternion myrot = new d.Quaternion();
            Quaternion meshRotA = Quaternion.CreateFromAxisAngle(new Vector3(1,0,0),1.5705f); 
            Quaternion meshRotB = Quaternion.CreateFromAxisAngle(new Vector3(0,1,0), 3.1415f);
            Quaternion mytemprot = _orientation * meshRotA * meshRotB;

            myrot.W = mytemprot.W;
            myrot.X = mytemprot.X;
            myrot.Y = mytemprot.Y;
            myrot.Z = mytemprot.Z;
            d.GeomSetQuaternion(prim_geom, ref myrot);
            
            if (m_isphysical && Body != IntPtr.Zero)
            {
                d.BodySetQuaternion(Body, ref myrot);
                if (!m_angularlock.IsIdentical(new PhysicsVector(1, 1, 1), 0))
                    createAMotor(m_angularlock);
            }

            resetCollisionAccounting();
            m_taintrot = _orientation;
        }

        public void changesizeogremesh(float timestamp)
        {
            //if (!_parent_scene.geom_name_map.ContainsKey(prim_geom))
            //{
            // m_taintsize = _size;
            //return;
            //}
            string oldname = _parent_scene.geom_name_map[prim_geom];

            if (_size.X <= 0) _size.X = 0.01f;
            if (_size.Y <= 0) _size.Y = 0.01f;
            if (_size.Z <= 0) _size.Z = 0.01f;

            // Cleanup of old prim geometry
            if (_mesh != null)
            {
                // Cleanup meshing here
            }
            //kill body to rebuild
            if (IsPhysical && Body != IntPtr.Zero)
            {
                if (childPrim)
                {
                    if (_parent != null)
                    {
                        RexOdePrim parent = (RexOdePrim)_parent;
                        parent.ChildDelink(this);
                    }
                }
                else
                {
                    disableBody();
                }
            }
            if (d.SpaceQuery(m_targetSpace, prim_geom))
            {
                _parent_scene.waitForSpaceUnlock(m_targetSpace);
                d.SpaceRemove(m_targetSpace, prim_geom);
            }
            d.GeomDestroy(prim_geom);
            prim_geom = IntPtr.Zero;
            // we don't need to do space calculation because the client sends a position update also.

            // Construction of new prim        
            Mesh mesh = CreateMeshFromOriginal();
            
            CreateGeom(m_targetSpace, mesh);
            d.GeomSetPosition(prim_geom, _position.X, _position.Y, _position.Z);

            d.Quaternion myrot = new d.Quaternion();
            Quaternion meshRotA = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), 1.5705f); 
            Quaternion meshRotB = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), 3.1415f);
            Quaternion mytemprot = _orientation * meshRotA * meshRotB;

            myrot.W = mytemprot.W;
            myrot.X = mytemprot.X;
            myrot.Y = mytemprot.Y;
            myrot.Z = mytemprot.Z;
            d.GeomSetQuaternion(prim_geom, ref myrot);             
                
            //d.GeomBoxSetLengths(prim_geom, _size.X, _size.Y, _size.Z);
            if (IsPhysical && Body == IntPtr.Zero && !childPrim)
            {
                // Re creates body on size.
                // EnableBody also does setMass()
                enableBody();
                d.BodyEnable(Body);
            }

            _parent_scene.geom_name_map[prim_geom] = oldname;

            changeSelectedStatus(timestamp);
            if (childPrim)
            {
                if (_parent is OdePrim)
                {
                    OdePrim parent = (OdePrim)_parent;
                    parent.ChildSetGeom(this);
                }
            }
            resetCollisionAccounting();
            m_taintsize = _size;
            m_ReCreateCollision = false;
        }        
        
    }
}
