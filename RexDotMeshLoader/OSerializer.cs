/*
Modified .mesh loader based on the Axiom Graphics Engine Library
which is based on the open source Object Oriented Graphics Engine OGRE. 

RexDotMeshLoader by realXtend project.

Axiom Graphics Engine Library 
Copyright (C) 2003-2006 Axiom Project Team
The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.
*/

using System;
using System.IO;
using System.Text;

namespace RexDotMeshLoader
{
    public class Serializer
    {
        protected string version;
        protected int currentChunkLength;
        public const int ChunkOverheadSize = 6;

        public Serializer()
        {
            version = "[Serializer_v1.00]";
        }

        protected void IgnoreCurrentChunk( BinaryReader vReader)
        {
            Seek(vReader, currentChunkLength - ChunkOverheadSize);
        }

        protected void ReadBytes(BinaryReader vReader, int count, byte[] dest) 
        {
            for (int i = 0; i < count; i++)
                dest[i] = vReader.ReadByte();
        }

        protected void ReadFloats( BinaryReader vReader, int count, float[] dest )
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = vReader.ReadSingle();
            }
        }


        protected void ReadFloats( BinaryReader vReader, int count, float[] dest, float[] destArray ) 
        {
            for (int i = 0; i < count; i++)
            {
                float val = vReader.ReadSingle();
                dest[i] = val;
                destArray[i] = val;
            }
        }

        protected bool ReadBool( BinaryReader vReader )
        {
            return vReader.ReadBoolean();
        }

        protected float ReadFloat( BinaryReader vReader )
        {
            return vReader.ReadSingle();
        }

        protected int ReadInt( BinaryReader vReader )
        {
            return vReader.ReadInt32();
        }

        protected uint ReadUInt( BinaryReader vReader )
        {
            return vReader.ReadUInt32();
        }

        protected long ReadLong( BinaryReader vReader )
        {
            return vReader.ReadInt64();
        }

        protected ulong ReadULong( BinaryReader vReader )
        {
            return vReader.ReadUInt64();
        }

        protected short ReadShort( BinaryReader vReader )
        {
            return vReader.ReadInt16();
        }

        protected ushort ReadUShort( BinaryReader vReader )
        {
            return vReader.ReadUInt16();
        }

        protected void ReadInts( BinaryReader vReader, int count, int[] dest ) 
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = vReader.ReadInt32();
            }
        }

        protected void ReadShorts( BinaryReader vReader, int count, short[] dest )
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = vReader.ReadInt16();
            }
        }


        protected string ReadString( BinaryReader vReader )
        {
            return ReadString(vReader,'\n');
        }

        protected string ReadString( BinaryReader vReader, char delimiter )
        {
            StringBuilder sb = new StringBuilder();

            char c;
            while ((c = vReader.ReadChar()) != delimiter)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }

        protected Quaternion ReadQuat(BinaryReader vReader)
        {
            Quaternion quat = new Quaternion();

            quat.X = vReader.ReadSingle();
            quat.Y = vReader.ReadSingle();
            quat.Z = vReader.ReadSingle();
            quat.W = vReader.ReadSingle();

            return quat;
        }

        protected Vector3 ReadVector3(BinaryReader vReader)
        {
            Vector3 vector = new Vector3();

            vector.X = ReadFloat(vReader);
            vector.Y = ReadFloat(vReader);
            vector.Z = ReadFloat(vReader);

            return vector;
        }

        protected Vector4 ReadVector4(BinaryReader vReader)
        {
            Vector4 vector = new Vector4();

            vector.X = ReadFloat(vReader);
            vector.Y = ReadFloat(vReader);
            vector.Z = ReadFloat(vReader);
            vector.W = ReadFloat(vReader);
            return vector;
        }

        protected MeshChunkID ReadChunk(BinaryReader vReader)
        {
            short id = vReader.ReadInt16();
            currentChunkLength = vReader.ReadInt32();
            return (MeshChunkID)id;
        }

        protected void Seek(BinaryReader vReader,long length)
        {
            Seek( vReader, length, SeekOrigin.Current);
        }

        protected void Seek(BinaryReader vReader, long length, SeekOrigin origin)
        {
            if(vReader.BaseStream.CanSeek)
                vReader.BaseStream.Seek(length,origin);
            else
                throw new Exception("Missing canseek from stream");
        }

        protected bool IsEOF(BinaryReader vReader)
        {
			return vReader.BaseStream.Position >= vReader.BaseStream.Length;
        }

    }
}
