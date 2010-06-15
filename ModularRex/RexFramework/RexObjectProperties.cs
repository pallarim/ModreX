using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using log4net;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public class RexObjectProperties
    {
        private IRexObjectPropertiesEventManager RexEventManager = null;
        private bool mProcessingPacketData = false;
    
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Properties

        private UUID parentObjectID = UUID.Zero;
        public UUID ParentObjectID
        {
            get { return parentObjectID; }
            set { parentObjectID = value; }
        }

        private byte m_RexDrawType = 0;
        public byte RexDrawType
        {
            get { return m_RexDrawType; }
            set
            {
                m_RexDrawType = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private bool m_RexIsVisible = true;
        public bool RexIsVisible
        {
            get { return m_RexIsVisible; }
            set
            {
                m_RexIsVisible = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private bool m_RexCastShadows = false;
        public bool RexCastShadows
        {
            get { return m_RexCastShadows; }
            set
            {
                m_RexCastShadows = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private bool m_RexLightCreatesShadows = false;
        public bool RexLightCreatesShadows
        {
            get { return m_RexLightCreatesShadows; }
            set
            {
                m_RexLightCreatesShadows = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private bool m_RexDescriptionTexture = false;
        public bool RexDescriptionTexture
        {
            get { return m_RexDescriptionTexture; }
            set
            {
                m_RexDescriptionTexture = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private bool m_RexScaleToPrim = false;
        public bool RexScaleToPrim
        {
            get { return m_RexScaleToPrim; }
            set
            {
                bool oldscale = m_RexScaleToPrim;
                m_RexScaleToPrim = value;

                if (oldscale != m_RexScaleToPrim && RexEventManager != null)
                    RexEventManager.TriggerOnChangeScaleToPrim(parentObjectID);                
                    
                TriggerChangedRexObjectProperties();
            }
        }

        private float m_RexDrawDistance = 0;
        public float RexDrawDistance
        {
            get { return m_RexDrawDistance; }
            set
            {
                m_RexDrawDistance = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private float m_RexLOD = 1.0F;
        public float RexLOD
        {
            get { return m_RexLOD; }
            set
            {
                m_RexLOD = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private UUID m_RexMeshUUID = UUID.Zero;
        public UUID RexMeshUUID
        {
            get { return m_RexMeshUUID; }
            set
            {
                m_RexMeshUUID = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private UUID m_RexCollisionMeshUUID = UUID.Zero;
        public UUID RexCollisionMeshUUID
        {
            get { return m_RexCollisionMeshUUID; }
            set
            {
                UUID oldcollision = m_RexCollisionMeshUUID; 
                m_RexCollisionMeshUUID = value;

                if (oldcollision != m_RexCollisionMeshUUID && RexEventManager != null)
                    RexEventManager.TriggerOnChangeCollisionMesh(parentObjectID);

                TriggerChangedRexObjectProperties();
            }
        }

        private UUID m_RexParticleScriptUUID = UUID.Zero;
        public UUID RexParticleScriptUUID
        {
            get { return m_RexParticleScriptUUID; }
            set
            {
                m_RexParticleScriptUUID = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private UUID m_RexAnimationPackageUUID = UUID.Zero;
        public UUID RexAnimationPackageUUID
        {
            get { return m_RexAnimationPackageUUID; }
            set
            {
                m_RexAnimationPackageUUID = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private string m_RexAnimationName = String.Empty;
        public string RexAnimationName
        {
            get { return m_RexAnimationName; }
            set
            {
                m_RexAnimationName = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private float m_RexAnimationRate = 1.0F;
        public float RexAnimationRate
        {
            get { return m_RexAnimationRate; }
            set
            {
                m_RexAnimationRate = value;
                TriggerChangedRexObjectProperties();
            }
        }


        #region Material Stuff

        public RexMaterialsDictionary RexMaterials = new RexMaterialsDictionary();
        public RexMaterialsDictionary GetRexMaterials()
        {
            return (RexMaterialsDictionary)RexMaterials.Clone();
        }

        /// <summary>
        /// This is only to be used from NHibernate. Use RexMaterials instead in other cases.
        /// </summary>
        public IList<RexMaterialsDictionaryItem> RexMaterialDictionaryItems
        {
            get
            {
                IList<RexMaterialsDictionaryItem> tempRexMaterialDictionaryItems = new List<RexMaterialsDictionaryItem>();
                foreach (KeyValuePair<uint, RexMaterialsDictionaryItem> entry in RexMaterials)
                {
                    tempRexMaterialDictionaryItems.Add(new RexMaterialsDictionaryItem(entry));
                }
                return tempRexMaterialDictionaryItems;
            }
            set
            {
                //rexMaterialDictionary = new Dictionary<uint, UUID>();
                if (value != null)
                {
                    foreach (RexMaterialsDictionaryItem e in value)
                    {
                        //rexMaterialDictionary.Add(e.Num, e.AssetID);
                        RexMaterials.Add(e.Num, e);
                    }
                }
            }
        }

        #endregion

        private string m_RexClassName = String.Empty;
        public string RexClassName
        {
            get { return m_RexClassName; }
            set
            {
                string oldclass = m_RexClassName;
                m_RexClassName = value;

                if (m_RexClassName != oldclass && RexEventManager != null)
                    RexEventManager.TriggerOnChangePythonClass(parentObjectID);

                TriggerChangedRexObjectProperties();
            }
        }

        private UUID m_RexSoundUUID = UUID.Zero;
        public UUID RexSoundUUID
        {
            get { return m_RexSoundUUID; }
            set
            {
                m_RexSoundUUID = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private float m_RexSoundVolume = 0;
        public float RexSoundVolume
        {
            get { return m_RexSoundVolume; }
            set
            {
                m_RexSoundVolume = value;
                TriggerChangedRexObjectProperties();
            }
        }

        private float m_RexSoundRadius = 0;
        public float RexSoundRadius
        {
            get { return m_RexSoundRadius; }
            set
            {
                m_RexSoundRadius = value;
                TriggerChangedRexObjectProperties();
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
                    
                if (RexEventManager != null)
                    RexEventManager.TriggerOnChangeRexObjectMetaData(parentObjectID);

                TriggerChangedRexObjectProperties();
            }
        }

        private int m_RexSelectPriority = 0;
        public int RexSelectPriority
        {
            get { return m_RexSelectPriority; }
            set
            {
                m_RexSelectPriority = value;
                TriggerChangedRexObjectProperties();
            }
        }

        #region Asset URIS

        //These URLs are offered to NG-clients in SendRexObjectUpdate

        private string m_rexMeshURL = String.Empty;
        private string m_rexCollisionMeshURL = String.Empty;
        private string m_rexParticleScriptURL = String.Empty;
        private string m_rexAnimationPackageURL = String.Empty;
        private string m_rexSoundURL = String.Empty;

        public string RexMeshURI
        {
            get { return m_rexMeshURL; }
            set
            {
                if (value != null)
                    m_rexMeshURL = value;
                else
                    m_rexMeshURL = String.Empty;
                TriggerChangedRexObjectProperties();
            }
        }

        public string RexCollisionMeshURI
        {
            get { return m_rexCollisionMeshURL; }
            set
            {
                if (value != null)
                    m_rexCollisionMeshURL = value;
                else
                    m_rexCollisionMeshURL = String.Empty;
                TriggerChangedRexObjectProperties();
            }
        }

        public string RexParticleScriptURI
        {
            get { return m_rexParticleScriptURL; }
            set
            {
                if (value != null)
                    m_rexParticleScriptURL = value;
                else
                    m_rexParticleScriptURL = String.Empty;
                TriggerChangedRexObjectProperties();
            }
        }

        public string RexAnimationPackageURI
        {
            get { return m_rexAnimationPackageURL; }
            set
            {
                if (value != null)
                    m_rexAnimationPackageURL = value;
                else
                    m_rexAnimationPackageURL = String.Empty;
                TriggerChangedRexObjectProperties();
            }
        }

        public string RexSoundURI
        {
            get { return m_rexSoundURL; }
            set
            {
                if (value != null)
                    m_rexSoundURL = value;
                else
                    m_rexSoundURL = String.Empty;
                TriggerChangedRexObjectProperties();
            }
        }

        #endregion

        #endregion

        System.Timers.Timer m_saveProperties = null;

        #region Constructors
        /// <summary>
        /// Initialises a new RexObjectProperties class from
        /// the specified binary array. Unpacks the array
        /// according to the viewer-specified format into
        /// the properties.
        /// </summary>
        /// <param name="data"></param>
        public RexObjectProperties(byte[] data, bool containsURIs)
        {
            SetRexPrimDataFromBytes(data, containsURIs);
            RexMaterials.SetSceneObjectPart(this);
            m_saveProperties = new System.Timers.Timer();
            m_saveProperties.Interval = 1000;
            m_saveProperties.Elapsed += m_saveProperties_Elapsed;
        }

        public RexObjectProperties() 
        {
            RexMaterials.SetSceneObjectPart(this);
            m_saveProperties = new System.Timers.Timer();
            m_saveProperties.Interval = 1000;
            m_saveProperties.Elapsed += m_saveProperties_Elapsed;
        }

        public RexObjectProperties(UUID parentid,IRexObjectPropertiesEventManager newrexeventmanager) 
        {
            ParentObjectID = parentid;
            RexEventManager = newrexeventmanager;
            RexMaterials.SetSceneObjectPart(this);
            m_saveProperties = new System.Timers.Timer();
            m_saveProperties.Interval = 1000;
            m_saveProperties.Elapsed += m_saveProperties_Elapsed;
        }

        #endregion

        void m_saveProperties_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_saveProperties.Stop();
            RexEventManager.TriggerOnSaveObject(this.parentObjectID);
        }

        public void SetRexEventManager(IRexObjectPropertiesEventManager newrexeventmanager)
        {
            RexEventManager = newrexeventmanager;
        }

        public void SetRexPrimDataFromObject(RexObjectProperties source)
        {
            try
            {
                mProcessingPacketData = true;
                RexDrawType = source.RexDrawType;  
                RexIsVisible = source.RexIsVisible;
                RexCastShadows = source.RexCastShadows;
                RexLightCreatesShadows = source.RexLightCreatesShadows;
                RexDescriptionTexture = source.RexDescriptionTexture;
                RexScaleToPrim = source.RexScaleToPrim;
                RexDrawDistance = source.RexDrawDistance;
                RexLOD = source.RexLOD;
                RexMeshUUID = source.RexMeshUUID;
                RexCollisionMeshUUID = source.RexCollisionMeshUUID;
                RexParticleScriptUUID = source.RexParticleScriptUUID;
                RexAnimationPackageUUID = source.RexAnimationPackageUUID;
                RexAnimationName = source.RexAnimationName;
                RexAnimationRate = source.RexAnimationRate;
                RexMaterials.ClearMaterials();
                RexMaterials = (RexMaterialsDictionary)source.RexMaterials.Clone();
                RexClassName = source.RexClassName;
                RexSoundUUID = source.RexSoundUUID;
                RexSoundVolume = source.RexSoundVolume;
                RexSoundRadius = source.RexSoundRadius;
                RexSelectPriority = source.RexSelectPriority;
                mProcessingPacketData = false;
                m_rexAnimationPackageURL = source.RexAnimationPackageURI;
                m_rexCollisionMeshURL = source.RexCollisionMeshURI;
                m_rexMeshURL = source.RexMeshURI;
                m_rexParticleScriptURL = source.RexParticleScriptURI;
                m_rexSoundURL = source.RexSoundURI;
                
                TriggerChangedRexObjectProperties();
            }
            catch (Exception e)
            {
                mProcessingPacketData = false;
                m_log.Error(e.ToString());
            }
        }

        public void SetRexPrimDataFromLegacyData(RexLegacyPrimData source)
        {
            try
            {
                mProcessingPacketData = true;
                ParentObjectID = source.UUID;
                RexDrawType = Convert.ToByte(source.DrawType);
                RexIsVisible = ConvertStringToBoolean(source.IsVisible);
                RexCastShadows = ConvertStringToBoolean(source.CastShadows);
                RexLightCreatesShadows = ConvertStringToBoolean(source.LightCreatesShadows);
                RexDescriptionTexture = ConvertStringToBoolean(source.DescriptionTexture);
                RexScaleToPrim = ConvertStringToBoolean(source.ScaleToPrim);
                RexDrawDistance = source.DrawDistance;
                RexLOD = source.LODBias;
                RexMeshUUID = source.Mesh;
                RexCollisionMeshUUID = source.CollisionMesh;
                RexParticleScriptUUID = source.ParticleScript;
                RexAnimationPackageUUID = source.AnimationPackage;
                RexAnimationName = source.AnimationName;
                RexAnimationRate = source.AnimationRate;
                RexMaterials.ClearMaterials();                
                RexClassName = source.ClassName;
                RexSoundUUID = source.Sound;
                RexSoundVolume = source.SoundVolume;
                RexSoundRadius = source.SoundRadius;

                if(source.RexExtraPrimData != null && source.RexExtraPrimData.Length > 0)
                    RexData = Encoding.UTF8.GetString(source.RexExtraPrimData);
                
                RexSelectPriority = source.SelectPriority;
                mProcessingPacketData = false;

                TriggerChangedRexObjectProperties();
            }
            catch (Exception e)
            {
                mProcessingPacketData = false;
                m_log.Error(e.ToString());
            }
        }        
        
        private bool ConvertStringToBoolean(string vValue)
        {        
            if(vValue.Length == 0 || vValue == "0")
                return false;
            else
                return true;
        }
        
        
        
        #region Old RexServer ToByte/FromByte methods
        public byte[] GetRexPrimDataToBytes(bool sendURLs)
        {
            try
            {
                // Display
                int size = sizeof(byte) + // drawtype
                    sizeof(bool) + sizeof(bool) + sizeof(bool) + sizeof(bool) + sizeof(bool) + // visible,castshadows,lightcreatesshadows,desctex,scaletoprim
                    sizeof(float) + sizeof(float) + // drawdist,lod
                    16 + 16 + 16 + 16 +  // meshuuid,colmeshuuid,particleuuid,animpackuuid
                    sizeof(int); // selectpriority

                // Animname,animrate
                size += (m_RexAnimationName.Length + 1 + sizeof(float));

                // Materialdata
                size += sizeof(byte); // Number of materials
                RexMaterialsDictionary materials = GetRexMaterials();
                size += (materials.Values.Count * (sizeof(byte) + 16 + sizeof(byte))); // materialassettype,matuuid,matindex

                // Misc
                size = size + m_RexClassName.Length + 1 + // classname & endbyte
                    16 + sizeof(float) + sizeof(float); // sounduuid,sndvolume,sndradius

                //add url sizes and their endbyte to size
                if (sendURLs)
                {
                    size +=
                        m_rexMeshURL.Length + 1 +
                        m_rexCollisionMeshURL.Length + 1 +
                        m_rexParticleScriptURL.Length + 1 +
                        m_rexAnimationPackageURL.Length + 1 +
                        m_rexSoundURL.Length + 1;

                    //Add material url lengths
                    foreach(RexMaterialsDictionaryItem item in materials.Values)
                    {
                        size += item.AssetURI.Length + 1;
                    }
                }

                // Build byte array
                byte[] buffer = new byte[size];
                int idx = 0;

                buffer[idx++] = m_RexDrawType;
                BitConverter.GetBytes(m_RexIsVisible).CopyTo(buffer, idx);
                idx += sizeof(bool);
                BitConverter.GetBytes(m_RexCastShadows).CopyTo(buffer, idx);
                idx += sizeof(bool);
                BitConverter.GetBytes(m_RexLightCreatesShadows).CopyTo(buffer, idx);
                idx += sizeof(bool);
                BitConverter.GetBytes(m_RexDescriptionTexture).CopyTo(buffer, idx);
                idx += sizeof(bool);
                BitConverter.GetBytes(m_RexScaleToPrim).CopyTo(buffer, idx);
                idx += sizeof(bool);

                BitConverter.GetBytes(m_RexDrawDistance).CopyTo(buffer, idx);
                idx += sizeof(float);
                BitConverter.GetBytes(m_RexLOD).CopyTo(buffer, idx);
                idx += sizeof(float);

                m_RexMeshUUID.GetBytes().CopyTo(buffer, idx);
                idx += 16;
                m_RexCollisionMeshUUID.GetBytes().CopyTo(buffer, idx);
                idx += 16;
                m_RexParticleScriptUUID.GetBytes().CopyTo(buffer, idx);
                idx += 16;

                m_RexAnimationPackageUUID.GetBytes().CopyTo(buffer, idx);
                idx += 16;
                Encoding.ASCII.GetBytes(m_RexAnimationName).CopyTo(buffer, idx);
                idx += (m_RexAnimationName.Length);
                buffer[idx++] = 0;
                BitConverter.GetBytes(m_RexAnimationRate).CopyTo(buffer, idx);
                idx += sizeof(float);

                buffer[idx++] = (byte)materials.Values.Count;
                foreach (KeyValuePair<uint, RexMaterialsDictionaryItem> kvp in materials)
                {
                    // Client needs assettype so that it knows if this is texture or material script
                    byte assettype = 0;
                    if (RexEventManager != null)
                    {
                        sbyte asstype = RexEventManager.GetAssetType(kvp.Value.AssetID);
                        assettype = (asstype == -1) ? (byte)0 : (byte)asstype;
                    }
                                            
                    buffer[idx++] = assettype;
                    kvp.Value.AssetID.GetBytes().CopyTo(buffer, idx); // matuuid 
                    idx += 16;

                    byte tempindex = (byte)kvp.Key; // matindex
                    buffer[idx++] = tempindex;
                }

                Encoding.ASCII.GetBytes(m_RexClassName).CopyTo(buffer, idx);
                idx += (m_RexClassName.Length);
                buffer[idx++] = 0;

                m_RexSoundUUID.GetBytes().CopyTo(buffer, idx);
                idx += 16;
                BitConverter.GetBytes(m_RexSoundVolume).CopyTo(buffer, idx);
                idx += sizeof(float);
                BitConverter.GetBytes(m_RexSoundRadius).CopyTo(buffer, idx);
                idx += sizeof(float);

                BitConverter.GetBytes(m_RexSelectPriority).CopyTo(buffer, idx);
                idx += sizeof(int);

                //Add URL to packet
                if (sendURLs)
                {
                    Encoding.ASCII.GetBytes(m_rexMeshURL).CopyTo(buffer, idx);
                    idx += (m_rexMeshURL.Length);
                    buffer[idx++] = 0;

                    Encoding.ASCII.GetBytes(m_rexCollisionMeshURL).CopyTo(buffer, idx);
                    idx += (m_rexCollisionMeshURL.Length);
                    buffer[idx++] = 0;

                    Encoding.ASCII.GetBytes(m_rexParticleScriptURL).CopyTo(buffer, idx);
                    idx += (m_rexParticleScriptURL.Length);
                    buffer[idx++] = 0;

                    Encoding.ASCII.GetBytes(m_rexAnimationPackageURL).CopyTo(buffer, idx);
                    idx += (m_rexAnimationPackageURL.Length);
                    buffer[idx++] = 0;

                    Encoding.ASCII.GetBytes(m_rexSoundURL).CopyTo(buffer, idx);
                    idx += (m_rexSoundURL.Length);
                    buffer[idx++] = 0;

                    //And finally, the material urls
                    foreach (RexMaterialsDictionaryItem item in materials.Values)
                    {
                        Encoding.ASCII.GetBytes(item.AssetURI).CopyTo(buffer, idx);
                        idx += (item.AssetURI.Length);
                        buffer[idx++] = 0;
                    }
                }

                return buffer;
            }
            catch (Exception e)
            {
                m_log.Error(e.ToString());
                return null;
            }
        }

        public void SetRexPrimDataFromBytes(byte[] bytes, bool containsURIs)
        {
            mProcessingPacketData = true;
        
            try
            {
                int idx = 0;
                m_RexDrawType = bytes[idx++];

                m_RexIsVisible = BitConverter.ToBoolean(bytes, idx);
                idx += sizeof(bool);
                m_RexCastShadows = BitConverter.ToBoolean(bytes, idx);
                idx += sizeof(bool);
                m_RexLightCreatesShadows = BitConverter.ToBoolean(bytes, idx);
                idx += sizeof(bool);
                m_RexDescriptionTexture = BitConverter.ToBoolean(bytes, idx);
                idx += sizeof(bool);
                m_RexScaleToPrim = BitConverter.ToBoolean(bytes, idx);
                idx += sizeof(bool);

                m_RexDrawDistance = BitConverter.ToSingle(bytes, idx);
                idx += sizeof(float);
                m_RexLOD = BitConverter.ToSingle(bytes, idx);
                idx += sizeof(float);

                m_RexMeshUUID = new UUID(bytes, idx);
                idx += 16;
                m_RexCollisionMeshUUID = new UUID(bytes, idx);
                idx += 16;
                m_RexParticleScriptUUID = new UUID(bytes, idx);
                idx += 16;

                // animation
                m_RexAnimationPackageUUID = new UUID(bytes, idx);
                idx += 16;
                StringBuilder bufferanimname = new StringBuilder();
                while ((idx < bytes.Length) && (bytes[idx] != 0))
                {
                    char c = (char)bytes[idx++];
                    bufferanimname.Append(c);
                }
                m_RexAnimationName = bufferanimname.ToString();
                idx++;
                m_RexAnimationRate = BitConverter.ToSingle(bytes, idx);
                idx += sizeof(float);

                // materials, before setting materials clear them
                RexMaterials.ClearMaterials();
                Dictionary<uint, UUID> materialData = new Dictionary<uint, UUID>();
                byte matcount = bytes[idx++];
                for (int i = 0; i < matcount; i++)
                {
                    idx++; // skip type
                    UUID matuuid = new UUID(bytes, idx);
                    idx += 16;
                    byte matindex = bytes[idx++];
                    if (!containsURIs)
                    {
                        RexMaterials.AddMaterial(Convert.ToUInt32(matindex), matuuid); //old clients do it like this
                    }
                    else
                    {
                        materialData.Add(Convert.ToUInt32(matindex), matuuid);
                        //add to temporary dictinary so the information can be found when adding materials with uris
                    }
                }

                // misc
                StringBuilder buffer = new StringBuilder();
                while ((idx < bytes.Length) && (bytes[idx] != 0))
                {
                    char c = (char)bytes[idx++];
                    buffer.Append(c);
                }
                m_RexClassName = buffer.ToString();
                idx++;

                m_RexSoundUUID = new UUID(bytes, idx);
                idx += 16;
                m_RexSoundVolume = BitConverter.ToSingle(bytes, idx);
                idx += sizeof(float);
                m_RexSoundRadius = BitConverter.ToSingle(bytes, idx);
                idx += sizeof(float);

                if (bytes.Length >= (idx + sizeof(int)))
                {
                    m_RexSelectPriority = BitConverter.ToInt32(bytes, idx);
                    idx += sizeof(int);
                }

                if (containsURIs)
                {
                    m_rexMeshURL = BuildStringFromBytes(bytes, ref idx);
                    m_rexCollisionMeshURL = BuildStringFromBytes(bytes, ref idx);
                    m_rexParticleScriptURL = BuildStringFromBytes(bytes, ref idx);
                    m_rexAnimationPackageURL = BuildStringFromBytes(bytes, ref idx);
                    m_rexSoundURL = BuildStringFromBytes(bytes, ref idx);

                    foreach (KeyValuePair<uint, UUID> kvp in materialData)
                    {
                        string uri = BuildStringFromBytes(bytes, ref idx);
                        RexMaterials.AddMaterial(kvp.Key, kvp.Value, uri);
                    }
                }

                mProcessingPacketData = false;
                TriggerChangedRexObjectProperties();
            }
            catch (Exception e)
            {
                mProcessingPacketData = false;
                m_log.Error(e.ToString());
            }
        }

        private string BuildStringFromBytes(byte[] bytes, ref int idx)
        {
            StringBuilder uribuilder = new StringBuilder();
            while ((idx < bytes.Length) && (bytes[idx] != 0))
            {
                char c = (char)bytes[idx++];
                uribuilder.Append(c);
            }
            idx++;
            return uribuilder.ToString();
        }

        #endregion

        #region Debug
        public void PrintRexPrimdata()
        {
            try
            {
                m_log.Warn("RexDrawType:" + RexDrawType);
                m_log.Warn("RexIsVisible:" + RexIsVisible);
                m_log.Warn("RexCastShadows:" + RexCastShadows);
                m_log.Warn("RexLightCreatesShadows:" + RexLightCreatesShadows);
                m_log.Warn("RexDescriptionTexture:" + RexDescriptionTexture);
                m_log.Warn("RexScaleToPrim:" + RexScaleToPrim);
                m_log.Warn("RexDrawDistance:" + RexDrawDistance);
                m_log.Warn("RexLOD" + RexLOD);
                m_log.Warn("RexMeshUUID:" + RexMeshUUID);
                m_log.Warn("RexCollisionMeshUUID:" + RexCollisionMeshUUID);
                m_log.Warn("RexParticleScriptUUID:" + RexParticleScriptUUID);
                m_log.Warn("RexAnimationPackageUUID:" + RexAnimationPackageUUID);
                m_log.Warn("RexAnimationName:" + RexAnimationName);
                m_log.Warn("RexAnimationRate:" + RexAnimationRate);
                m_log.Warn("RexMaterials:" + RexMaterials);
                m_log.Warn("RexClassName:" + RexClassName);
                m_log.Warn("RexSoundUUID:" + RexSoundUUID);
                m_log.Warn("RexSoundVolume:" + RexSoundVolume);
                m_log.Warn("RexSoundRadius:" + RexSoundRadius);
                m_log.Warn("RexSelectPriority:" + RexSelectPriority);
            }
            catch (Exception e)
            {
                m_log.Error(e.ToString());
            }
        }
        #endregion

        public void TriggerChangedRexObjectProperties()
        {
            if (mProcessingPacketData)
                return;
            if (RexEventManager != null)
                RexEventManager.TriggerOnChangeRexObjectProperties(parentObjectID);       
        }

        public void ScheduleSave()
        {
            if (!m_saveProperties.Enabled)
            {
                m_saveProperties.Start();
            }
        }
    }
}
