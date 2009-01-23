using System;
using System.Collections.Generic;
using System.Text;
using Nini.Config;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts
{
    public class ModrexSuperobjects : IRegionModule 
    {
        /*
        public byte[] GetRexPrimDataToBytes()
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

                // Build byte array
                byte[] buffer = new byte[size];
                int idx = 0;

                buffer[idx++] = m_RexDrawType;
                System.BitConverter.GetBytes(m_RexIsVisible).CopyTo(buffer, idx);
                idx += sizeof(bool);
                System.BitConverter.GetBytes(m_RexCastShadows).CopyTo(buffer, idx);
                idx += sizeof(bool);
                System.BitConverter.GetBytes(m_RexLightCreatesShadows).CopyTo(buffer, idx);
                idx += sizeof(bool);
                System.BitConverter.GetBytes(m_RexDescriptionTexture).CopyTo(buffer, idx);
                idx += sizeof(bool);
                System.BitConverter.GetBytes(m_RexScaleToPrim).CopyTo(buffer, idx);
                idx += sizeof(bool);

                System.BitConverter.GetBytes(m_RexDrawDistance).CopyTo(buffer, idx);
                idx += sizeof(float);
                System.BitConverter.GetBytes(m_RexLOD).CopyTo(buffer, idx);
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
                System.BitConverter.GetBytes(m_RexAnimationRate).CopyTo(buffer, idx);
                idx += sizeof(float);

                buffer[idx++] = (byte)materials.Values.Count;
                foreach (KeyValuePair<uint, LLUUID> kvp in materials)
                {
                    AssetBase tempmodel = m_parentGroup.Scene.AssetCache.FetchAsset(kvp.Value); // materialassettype
                    if (tempmodel != null)
                    {
                        byte temptype = (byte)(tempmodel.Type);
                        buffer[idx++] = temptype;
                    }
                    else
                        buffer[idx++] = 0;

                    kvp.Value.GetBytes().CopyTo(buffer, idx); // matuuid 
                    idx += 16;

                    byte tempindex = (byte)kvp.Key; // matindex
                    buffer[idx++] = tempindex;
                }

                Encoding.ASCII.GetBytes(m_RexClassName).CopyTo(buffer, idx);
                idx += (m_RexClassName.Length);
                buffer[idx++] = 0;

                m_RexSoundUUID.GetBytes().CopyTo(buffer, idx);
                idx += 16;
                System.BitConverter.GetBytes(m_RexSoundVolume).CopyTo(buffer, idx);
                idx += sizeof(float);
                System.BitConverter.GetBytes(m_RexSoundRadius).CopyTo(buffer, idx);
                idx += sizeof(float);

                System.BitConverter.GetBytes(m_RexSelectPriority).CopyTo(buffer, idx);
                idx += sizeof(int);

                return buffer;
            }
            catch (Exception e)
            {
                m_log.Error(e.ToString());
                return null;
            }
        }
        */

        public void Initialise(Scene scene, IConfigSource source)
        {
            
        }

        public void PostInitialise()
        {
            
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "ModRexSuperObjectModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
