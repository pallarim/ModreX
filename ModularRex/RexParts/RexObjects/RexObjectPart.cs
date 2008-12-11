using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using OpenMetaverse;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Physics.Manager;
using OpenSim.Framework;

namespace ModularRex.RexParts.RexObjects
{
    public class RexObjectPart : SceneObjectPart 
    {
        private UUID m_rexVisualMesh;
        private UUID m_rexCollisionMesh;
        private List<UUID> m_materials = new List<UUID>(10);

        public void ConvertFromSceneObjectPart(SceneObjectPart origin)
        {
            this.Acceleration = origin.Acceleration;
            this.AngularVelocity = origin.AngularVelocity;
            this.BaseMask = origin.BaseMask;
            this.Category = origin.Category;
            this.ClickAction = origin.ClickAction;
            //this.Color = origin.Color;
            this.CreationDate = origin.CreationDate;
            this.CreatorID = origin.CreatorID;
            this.Description = origin.Description;
           



        }

        public new RexObjectGroup ParentGroup;

        public static RexObjectPart FromRexXml(XmlReader xmlReader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (RexObjectPart));
            RexObjectPart newobject = (RexObjectPart)serializer.Deserialize(xmlReader);
            return newobject;
        }


        [XmlIgnore]
        public uint TimeStampRexPrim = 0;
        [XmlIgnore]
        public uint TimeStampRexPrimFreeData = 0;
        private bool bProcessingRexPrimData = false;

        private bool m_RexUpdateFlagPrim = false;
        public bool RexUpdateFlagPrim
        {
            get { return m_RexUpdateFlagPrim; }
            set { m_RexUpdateFlagPrim = value; }
        }

        private bool m_RexUpdateFlagFreeData = false;
        public bool RexUpdateFlagFreeData
        {
            get { return m_RexUpdateFlagFreeData; }
            set { m_RexUpdateFlagFreeData = value; }
        }

        private byte m_RexDrawType = 0;
        public byte RexDrawType
        {
            get { return m_RexDrawType; }
            set
            {
                m_RexDrawType = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private bool m_RexIsVisible = true;
        public bool RexIsVisible
        {
            get { return m_RexIsVisible; }
            set
            {
                m_RexIsVisible = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private bool m_RexCastShadows = false;
        public bool RexCastShadows
        {
            get { return m_RexCastShadows; }
            set
            {
                m_RexCastShadows = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private bool m_RexLightCreatesShadows = false;
        public bool RexLightCreatesShadows
        {
            get { return m_RexLightCreatesShadows; }
            set
            {
                m_RexLightCreatesShadows = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private bool m_RexDescriptionTexture = false;
        public bool RexDescriptionTexture
        {
            get { return m_RexDescriptionTexture; }
            set
            {
                m_RexDescriptionTexture = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private bool m_RexScaleToPrim = false;
        public bool RexScaleToPrim
        {
            get { return m_RexScaleToPrim; }
            set
            {
                bool OldMeshScaling = m_RexScaleToPrim;
                m_RexScaleToPrim = value;

                if (m_parentGroup != null)
                {
                    throw new NotImplementedException("Rex Scale to Prim not implemented");
                    //if (GlobalSettings.Instance.m_3d_collision_models && (m_RexScaleToPrim != OldMeshScaling) && PhysActor != null)
                    //{
                    //    PhysActor.SetBoundsScaling(m_RexScaleToPrim);
                    //    m_parentGroup.Scene.PhysicsScene.AddPhysicsActorTaint(PhysActor);
                    //}
                }
                ScheduleRexPrimUpdate(true);
            }
        }

        private float m_RexDrawDistance = 0.0F;
        public float RexDrawDistance
        {
            get { return m_RexDrawDistance; }
            set
            {
                m_RexDrawDistance = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private float m_RexLOD = 1.0F;
        public float RexLOD
        {
            get { return m_RexLOD; }
            set
            {
                m_RexLOD = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private UUID m_RexMeshUUID = UUID.Zero;
        public UUID RexMeshUUID
        {
            get { return m_RexMeshUUID; }
            set
            {
                m_RexMeshUUID = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private UUID m_RexCollisionMeshUUID = UUID.Zero;
        public UUID RexCollisionMeshUUID
        {
            get { return m_RexCollisionMeshUUID; }
            set
            {
                UUID OldColMesh = m_RexCollisionMeshUUID;
                m_RexCollisionMeshUUID = value;

                if (m_parentGroup != null)
                {
                    throw new NotImplementedException("Rex Collision mesh not implemented");
                    //if (GlobalSettings.Instance.m_3d_collision_models && (m_RexCollisionMeshUUID != OldColMesh) && PhysActor != null)
                    //{
                    //    if (m_RexCollisionMeshUUID != UUID.Zero)
                    //        RexUpdateCollisionMesh();
                    //    else
                    //        PhysActor.SetCollisionMesh(null, "", false);
                    //}
                }
                ScheduleRexPrimUpdate(true);
            }
        }

        private UUID m_RexParticleScriptUUID = UUID.Zero;
        public UUID RexParticleScriptUUID
        {
            get { return m_RexParticleScriptUUID; }
            set
            {
                m_RexParticleScriptUUID = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private UUID m_RexAnimationPackageUUID = UUID.Zero;
        public UUID RexAnimationPackageUUID
        {
            get { return m_RexAnimationPackageUUID; }
            set
            {
                m_RexAnimationPackageUUID = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private string m_RexAnimationName = String.Empty;
        public string RexAnimationName
        {
            get { return m_RexAnimationName; }
            set
            {
                m_RexAnimationName = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private float m_RexAnimationRate = 1.0F;
        public float RexAnimationRate
        {
            get { return m_RexAnimationRate; }
            set
            {
                m_RexAnimationRate = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        [XmlIgnore]
        public RexMaterialsDictionary RexMaterials = new RexMaterialsDictionary();
        public RexMaterialsDictionary GetRexMaterials()
        {
            return (RexMaterialsDictionary)RexMaterials.Clone();
        }

        public delegate void OnChangePythonClassDelegate(uint localID);
        public event OnChangePythonClassDelegate OnChangePythonClass;
        private string m_RexClassName = String.Empty;
        public string RexClassName
        {
            get { return m_RexClassName; }
            set
            {
                string OldPythonClass = m_RexClassName;
                m_RexClassName = value;

                if (m_parentGroup != null)
                {
                    if (m_RexClassName != OldPythonClass)
                    {
                        if (OnChangePythonClass != null)
                        {
                            OnChangePythonClass(this.m_localId);
                        }
                    }
                }
                ScheduleRexPrimUpdate(true);
            }
        }

        private UUID m_RexSoundUUID = UUID.Zero;
        public UUID RexSoundUUID
        {
            get { return m_RexSoundUUID; }
            set
            {
                m_RexSoundUUID = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private float m_RexSoundVolume = 0.0F;
        public float RexSoundVolume
        {
            get { return m_RexSoundVolume; }
            set
            {
                m_RexSoundVolume = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private float m_RexSoundRadius = 0.0F;
        public float RexSoundRadius
        {
            get { return m_RexSoundRadius; }
            set
            {
                m_RexSoundRadius = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        private string m_rexData = String.Empty;
        public string RexData
        {
            get { return m_rexData; }
            set
            {
                if (value.Length > 3000)
                    m_rexData = value.Substring(0, 3000);
                else
                    m_rexData = value;

                ScheduleRexPrimFreeDataUpdate(true);
            }
        }

        private int m_RexSelectPriority = 0;
        public int RexSelectPriority
        {
            get { return m_RexSelectPriority; }
            set
            {
                m_RexSelectPriority = value;
                ScheduleRexPrimUpdate(true);
            }
        }

        public void ScheduleRexPrimUpdate(bool vbSaveToDb)
        {
            if (bProcessingRexPrimData)
                return;

            if (m_parentGroup != null)
            {
                if (vbSaveToDb)
                    m_parentGroup.HasGroupChanged = true;

                TimeStampRexPrim = (uint)Util.UnixTimeSinceEpoch();
                m_parentGroup.QueueForUpdateCheck();
            }
            m_RexUpdateFlagPrim = true;
        }

        public void RexUpdateCollisionMesh()
        {
            throw new NotImplementedException("Rex update collision mesh not implemented");
            //if (!GlobalSettings.Instance.m_3d_collision_models)
            //    return;

            //if (m_RexCollisionMeshUUID != UUID.Zero && PhysActor != null)
            //{
            //    AssetBase tempmodel = m_parentGroup.Scene.AssetCache.FetchAsset(m_RexCollisionMeshUUID);
            //    if (tempmodel != null)
            //        PhysActor.SetCollisionMesh(tempmodel.Data, tempmodel.Name, RexScaleToPrim);
            //}
        }

        public void ScheduleRexPrimFreeDataUpdate(bool vbSaveToDb)
        {
            if (m_parentGroup != null)
            {
                if (vbSaveToDb)
                    m_parentGroup.HasGroupChanged = true;

                TimeStampRexPrimFreeData = (uint)Util.UnixTimeSinceEpoch();
                m_parentGroup.QueueForUpdateCheck();
            }
            m_RexUpdateFlagFreeData = true;
        }

        internal void SetUsePrimVolumeCollision(bool vUseVolumeCollision)
        {
            throw new NotImplementedException();
        }

        internal bool GetUsePrimVolumeCollision()
        {
            throw new NotImplementedException();
        }

        internal void SetMass(float vMass)
        {
            throw new NotImplementedException();
        }
    }
}
