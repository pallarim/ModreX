using System;
using OpenMetaverse;
using System.Runtime.Serialization;

namespace ModularRex.RexFramework
{
    public class RexLegacyPrimData
    {
        public RexLegacyPrimData(){    }

        private UUID _UUID;
        public UUID UUID
        {
            get { return _UUID; }
            set { _UUID = value; }
        }

        private string _DrawType;
        public string DrawType
        {
            get { return _DrawType; }
            set { _DrawType = value; }        
        }

        private string _IsVisible;
        public string IsVisible
        {
            get { return _IsVisible; }
            set { _IsVisible = value; }           
        }

        private string _CastShadows;
        public string CastShadows
        {
            get { return _CastShadows; }
            set { _CastShadows = value; }
        }

        private string _LightCreatesShadows;
        public string LightCreatesShadows
        {
            get { return _LightCreatesShadows; }
            set { _LightCreatesShadows = value; }
        }

        private string _DescriptionTexture;
        public string DescriptionTexture
        {
            get { return _DescriptionTexture; }
            set { _DescriptionTexture = value; }
        }

        private string _ScaleToPrim;
        public string ScaleToPrim
        {
            get { return _ScaleToPrim; }
            set { _ScaleToPrim = value; }
        }

        private float _DrawDistance;        
        public float DrawDistance
        {
            get { return _DrawDistance; }
            set { _DrawDistance = value; }
        }

        private float _LODBias;
        public float LODBias
        {
            get { return _LODBias; }
            set { _LODBias = value; }
        }

        private UUID _Mesh;
        public UUID Mesh
        {
            get { return _Mesh; }
            set { _Mesh = value; }
        }

        private UUID _CollisionMesh;
        public UUID CollisionMesh
        {
            get { return _CollisionMesh; }
            set { _CollisionMesh = value; }
        }

        private UUID _ParticleScript;
        public UUID ParticleScript
        {
            get { return _ParticleScript; }
            set { _ParticleScript = value; }
        }

        private UUID _AnimationPackage;
        public UUID AnimationPackage
        {
            get { return _AnimationPackage; }
            set { _AnimationPackage = value; }
        }        

        private string _AnimationName;        
        public string AnimationName
        {
            get { return _AnimationName; }
            set { _AnimationName = value; }
        }            

        private float _AnimationRate;        
        public float AnimationRate
        {
            get { return _AnimationRate; }
            set { _AnimationRate = value; }
        }         

        private string _ClassName;        
        public string ClassName
        {
            get { return _ClassName; }
            set { _ClassName = value; }
        }

        private UUID _Sound;
        public UUID Sound
        {
            get { return _Sound; }
            set { _Sound = value; }
        }

        private float _SoundVolume;        
        public float SoundVolume
        {
            get { return _SoundVolume; }
            set { _SoundVolume = value; }
        }        

        private float _SoundRadius;        
        public float SoundRadius
        {
            get { return _SoundRadius; }
            set { _SoundRadius = value; }
        }

        private byte[] _RexExtraPrimData;
        public byte[] RexExtraPrimData
        {
            get { return _RexExtraPrimData; }
            set { _RexExtraPrimData = value; }
        }

        private int _SelectPriority;        
        public int SelectPriority
        {
            get { return _SelectPriority; }
            set { _SelectPriority = value; }
        }        
    }

    public class RexLegacyPrimMaterialData : ISerializable
    {
        public RexLegacyPrimMaterialData() { }

        public override bool Equals(object obj)
        {
            return (_UUID == ((RexLegacyPrimMaterialData)obj).UUID && _MaterialIndex == ((RexLegacyPrimMaterialData)obj).MaterialIndex);
        }

        public override int GetHashCode()
        {
            return (_UUID.GetHashCode() ^ _MaterialIndex.GetHashCode());
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("UUID", _UUID);
            info.AddValue("MaterialUUID", _MaterialUUID);
            info.AddValue("MaterialIndex", _MaterialIndex);
        }

        private UUID _UUID;    
        public UUID UUID
        {
            get { return _UUID; }
            set { _UUID = value; }
        }

        private UUID _MaterialUUID;        
        public UUID MaterialUUID
        {
            get { return _MaterialUUID; }
            set { _MaterialUUID = value; }
        }

        private int _MaterialIndex;        
        public int MaterialIndex
        {
            get { return _MaterialIndex; }
            set { _MaterialIndex = value; }
        }        
    }
}
