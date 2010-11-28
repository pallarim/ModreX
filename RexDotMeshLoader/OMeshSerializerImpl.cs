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

namespace RexDotMeshLoader
{
    public class MeshSerializerImpl : Serializer
    {
        private OMesh mesh;

        public MeshSerializerImpl()
        {
            version = "[MeshSerializer_v1.40]";
        }

        public OMesh ImportMesh(byte[] vData)
        {
            mesh = new OMesh("mesh");

            Stream stream = new MemoryStream(vData);
            BinaryReader reader = new BinaryReader(stream);

            // header    
            short headerID = 0;
            headerID = reader.ReadInt16();
            if(headerID == (short)MeshChunkID.Header)
            {
                string fileVersion = ReadString(reader);
                if (fileVersion != "[MeshSerializer_v1.30]" && fileVersion != "[MeshSerializer_v1.40]")
                {
                    throw new Exception("Only supporting .mesh versions 1.3 & 1.4");
                }
            }
            else
            {
                throw new Exception("No header found");
            }
       
            MeshChunkID cid = 0;
            while (!IsEOF(reader))
            {
                try
                {
                    cid = ReadChunk(reader);
                    if (cid == MeshChunkID.Mesh)
                        ReadMesh(reader);

                }
                catch (System.IO.EndOfStreamException e)
                {
                    return mesh;
                }
            }
            return mesh;
        }
        
        protected virtual void ReadSubMeshNameTable( BinaryReader reader )
        {
            if (!IsEOF(reader))
            {
                MeshChunkID cid = ReadChunk(reader);
                while (!IsEOF(reader) && (cid == MeshChunkID.SubMeshNameTableElement))
                {
                    short index = ReadShort(reader);
                    string name = ReadString(reader);
                    SubMesh sub = mesh.GetSubMesh(index);
                    if(sub!= null)
                        sub.name = name;

                    if(!IsEOF(reader))
                        cid = ReadChunk(reader);  
                }
                if(!IsEOF(reader))
                    Seek( reader,-ChunkOverheadSize); 
            }
        }
         
        protected virtual void ReadMesh( BinaryReader reader )
        {
            MeshChunkID cid;

            bool SkelAnimed = ReadBool(reader);

            if ( !IsEOF( reader ) )
            {
                cid = ReadChunk(reader);

                while (!IsEOF(reader) &&
                    (cid == MeshChunkID.Geometry || cid == MeshChunkID.SubMesh ||
                     cid == MeshChunkID.MeshSkeletonLink || cid == MeshChunkID.MeshBoneAssignment ||
                     cid == MeshChunkID.MeshLOD || cid == MeshChunkID.MeshBounds ||
                     cid == MeshChunkID.SubMeshNameTable || cid == MeshChunkID.EdgeLists ) )
                {
                    switch (cid)
                    {
                        case MeshChunkID.Geometry:
                            mesh.SharedVertexData = new VertexData();
                            ReadGeometry( reader, mesh.SharedVertexData );
                            break;
                        case MeshChunkID.SubMesh: 
                            ReadSubMesh( reader ); 
                            break;
                        case MeshChunkID.MeshSkeletonLink:
                            ReadSkeletonLink( reader );
                            break;
                        case MeshChunkID.MeshBoneAssignment:
                            ReadMeshBoneAssignment( reader );
                            break;
                        case MeshChunkID.MeshLOD:
                            ReadMeshLodInfo( reader );
                            break;
                        case MeshChunkID.MeshBounds:
                            ReadBoundsInfo( reader );
                            break;
                        case MeshChunkID.SubMeshNameTable:
                            ReadSubMeshNameTable( reader );
                            break;
                        case MeshChunkID.EdgeLists:
                            ReadEdgeList( reader );
                            break; 
                    }
                    if(!IsEOF(reader))
                        cid = ReadChunk(reader);
                } 
                if(!IsEOF(reader))
                    Seek(reader,-ChunkOverheadSize);
            }
        }

        
        protected virtual void ReadSubMesh( BinaryReader reader )
        { 
            MeshChunkID cid;
            SubMesh subMesh = mesh.CreateSubMesh();

            string materialName = ReadString(reader);
            subMesh.MaterialName = materialName;
            subMesh.useSharedVertices = ReadBool(reader);
            subMesh.indexData.indexStart = 0;
            subMesh.indexData.indexCount = ReadInt(reader);

            bool idx32bit = ReadBool(reader);
            if (idx32bit)
            {               
                subMesh.indices_i = new int[subMesh.indexData.indexCount];
                ReadInts(reader, subMesh.indexData.indexCount, subMesh.indices_i);
            }
            else
            {
                subMesh.indices_s = new short[subMesh.indexData.indexCount];
                ReadShorts(reader, subMesh.indexData.indexCount, subMesh.indices_s);
            }
           
            if (!subMesh.useSharedVertices)
            {
                cid = ReadChunk(reader);
                if(cid != MeshChunkID.Geometry)
                    throw new Exception("Missing geometry data");
                   
                subMesh.vertexData = new VertexData();
                ReadGeometry(reader,subMesh.vertexData);
            }
         
            cid = ReadChunk(reader);
            while (!IsEOF(reader) &&
                ( cid == MeshChunkID.SubMeshBoneAssignment || cid == MeshChunkID.SubMeshOperation))
            {
                switch (cid)
                { 
                    case MeshChunkID.SubMeshBoneAssignment:
                        ReadSubMeshBoneAssignment( reader, subMesh );
                        break;
                    case MeshChunkID.SubMeshOperation:
                        ReadSubMeshOperation( reader, subMesh );
                        break;
                }
                if(!IsEOF(reader))
                    cid = ReadChunk(reader);
            }
            if(!IsEOF(reader))
                Seek(reader,-ChunkOverheadSize);
        }

        // Just reads, does not save anything!
        protected virtual void ReadSubMeshOperation(BinaryReader reader, SubMesh sub)
        {
            ReadShort(reader);
        }
         
        protected virtual void ReadGeometry( BinaryReader reader, VertexData data)
        {
            data.vertexStart = 0;
            data.vertexCount = ReadInt(reader);

            if (!IsEOF(reader))
            {
                MeshChunkID cid = ReadChunk(reader);

                while ( !IsEOF( reader ) &&
                    ( cid == MeshChunkID.GeometryVertexDeclaration ||
                    cid == MeshChunkID.GeometryVertexBuffer ) )
                {

                    switch ( cid )
                    {
                        case MeshChunkID.GeometryVertexDeclaration:
                            ReadGeometryVertexDeclaration( reader, data );
                            break;
                        case MeshChunkID.GeometryVertexBuffer:
                            ReadGeometryVertexBuffer( reader, data );
                            break;
                    }
                    if ( !IsEOF( reader ) )
                        cid = ReadChunk( reader );
                }
                if(!IsEOF(reader))
                    Seek(reader,-ChunkOverheadSize);
            }
        }

        protected virtual void ReadGeometryVertexDeclaration( BinaryReader reader, VertexData data )
        {
            if ( !IsEOF( reader ) )
            {
                MeshChunkID cid = ReadChunk( reader );
                while ( !IsEOF( reader ) &&
                    ( cid == MeshChunkID.GeometryVertexElement ) )
                {
                    switch ( cid )
                    {
                        case MeshChunkID.GeometryVertexElement:
                            ReadGeometryVertexElement( reader, data );
                            break;
                    }
                    if ( !IsEOF( reader ) )
                        cid = ReadChunk( reader );
                }
                if ( !IsEOF( reader ) )
                    Seek( reader, -ChunkOverheadSize );
            }
        }

        protected virtual void ReadGeometryVertexElement(BinaryReader reader, VertexData data)
        {
            short source = ReadShort(reader);
            VertexElementType type = (VertexElementType)ReadShort(reader);
            VertexElementSemantic semantic = (VertexElementSemantic)ReadShort(reader);
            short offset = ReadShort(reader);
            short index = ReadShort(reader);
            data.vertexDeclaration.AddElement(source, offset, type, semantic, index);              
        }

        protected virtual void ReadGeometryVertexBuffer( BinaryReader reader, VertexData data )
        {
            short bindIdx = ReadShort( reader );
            short vertexSize = ReadShort( reader );

            MeshChunkID cid = ReadChunk( reader );
            if ( cid != MeshChunkID.GeometryVertexBufferData )
                throw new Exception("Missing vertex buffer data"); 
            
            if(data.vertexDeclaration.GetVertexSize( bindIdx ) != vertexSize)
                throw new Exception("Vertex decl size and vertex buffer size mismatch");
            
            data.vertexBuffer = new byte[data.vertexCount * vertexSize];
            ReadBytes(reader, data.vertexCount * vertexSize, data.vertexBuffer);
        }

        
        protected virtual void ReadSkeletonLink( BinaryReader reader )
        {
            ReadString(reader); // removed
        }
         
        
        protected virtual void ReadMeshBoneAssignment( BinaryReader reader )
        {         
            ReadInt(reader); // removed
            ReadUShort(reader);
            ReadFloat(reader);
        }
        protected virtual void ReadSubMeshBoneAssignment( BinaryReader reader, SubMesh sub )
        {
            ReadInt(reader); // removed
            ReadUShort(reader);
            ReadFloat(reader);
        }

        // Just reads, does not save anything!
        protected virtual void ReadMeshLodInfo( BinaryReader reader )
        {
            MeshChunkID cid;

            mesh.numLods = ReadShort(reader);
            mesh.isLodManual = ReadBool(reader);
            for (int i = 1;i < mesh.numLods;i++)
            {
                cid = ReadChunk( reader );
                if (cid != MeshChunkID.MeshLODUsage)
                {
                    // log no meshlodusage 
                }

                MeshLodUsage usage = new MeshLodUsage();
                ReadFloat(reader); // usage.fromSquaredDepth = 

                if(mesh.isLodManual)
                    ReadMeshLodUsageManual( reader, i, ref usage );
                else
                    ReadMeshLodUsageGenerated( reader, i, ref usage );
               
                // removed: mesh.lodUsageList.Add( usage );
            }
            
        }

        // Just reads, does not save anything!
        protected virtual void ReadMeshLodUsageManual( BinaryReader reader, int lodNum, ref MeshLodUsage usage )
        {
            MeshChunkID cid = ReadChunk(reader);

            if ( cid != MeshChunkID.MeshLODManual)
            {
                // log missing manual meshlod
            }
            ReadString(reader); // usage.manualName =           
        }

        // Just reads, does not save anything!
        protected virtual void ReadMeshLodUsageGenerated( BinaryReader reader, int lodNum, ref MeshLodUsage usage )
        {  
            MeshChunkID cid;
            for (int i = 0; i < mesh.subMeshList.Count; i++)
            {
                cid = ReadChunk( reader );
                if ( cid != MeshChunkID.MeshLODGenerated )
                {
                    // log no generated lod 
                }
                // SubMesh sm = mesh.GetSubMesh( i );
                // IndexData indexData = new IndexData();
                // sm.lodFaceList[ lodNum - 1 ] = indexData;
                int tempindexcount = ReadInt(reader); // indexData.indexCount
                bool is32bit = ReadBool( reader );
                if ( is32bit )
                {
                    int[] barray = new int[tempindexcount];
                    ReadInts(reader, tempindexcount, barray);
                }
                else
                {
                    short[] barray = new short[tempindexcount];
                    ReadShorts(reader, tempindexcount, barray);
                }
            } 
        }
        
        protected virtual void ReadBoundsInfo(BinaryReader reader)
        {
            mesh.boundingBoxMin = ReadVector3( reader );
            mesh.boundingBoxMax = ReadVector3(reader);
            mesh.BoundingSphereRadius = ReadFloat( reader );
        }

        protected virtual void ReadEdgeList(BinaryReader reader)
        {
            if (!IsEOF(reader))
            {
                MeshChunkID cid = ReadChunk(reader);

                while(!IsEOF(reader) && cid == MeshChunkID.EdgeListLOD)
                {
                    short lodIndex = ReadShort(reader);
                    bool isManual = ReadBool(reader);

                    if ( !isManual )
                    {
                        bool isClosed = ReadBool(reader);


                        // MeshLodUsage usage = mesh.GetLodLevel( lodIndex );
                        // usage.edgeData = new EdgeData();
                        int triCount = ReadInt(reader);
                        int edgeGroupCount = ReadInt(reader);

                        for(int i=0;i<triCount; i++)
                        {
                            // EdgeData.Triangle tri = new EdgeData.Triangle();
                            ReadInt(reader); // tri.indexSet =
                            ReadInt(reader); // tri.vertexSet = 
                            ReadInt(reader); // tri.vertIndex[ 0 ] =
                            ReadInt(reader); // tri.vertIndex[ 1 ] =
                            ReadInt(reader); // tri.vertIndex[ 2 ] =
                            ReadInt(reader); // tri.sharedVertIndex[ 0 ] =
                            ReadInt(reader); // tri.sharedVertIndex[ 1 ] =
                            ReadInt(reader); // tri.sharedVertIndex[ 2 ]
                            ReadVector4(reader); // tri.normal =
                            // usage.edgeData.triangles.Add( tri );
                        }

                        for(int eg = 0; eg<edgeGroupCount; eg++)
                        {
                            cid = ReadChunk(reader);
                            if(cid != MeshChunkID.EdgeListGroup)
                                throw new Exception("Missing EdgeListGroup chunk");

                            // EdgeData.EdgeGroup edgeGroup = new EdgeData.EdgeGroup();

                            ReadInt(reader); // edgeGroup.vertexSet = 
                            int edgeTriStart = ReadInt(reader);
                            int edgeTriCount = ReadInt(reader);
                            int edgeCount = ReadInt(reader);
                            for (int e = 0; e<edgeCount;e++)
                            {
                                // EdgeData.Edge edge = new EdgeData.Edge();
                                ReadInt(reader); // edge.triIndex[ 0 ] =
                                ReadInt(reader); // edge.triIndex[ 1 ] =
                                ReadInt(reader); // edge.vertIndex[ 0 ] =
                                ReadInt(reader); // edge.vertIndex[ 1 ] =
                                ReadInt(reader); // edge.sharedVertIndex[ 0 ] =
                                ReadInt(reader); // edge.sharedVertIndex[ 1 ] =
                                ReadBool(reader); // edge.isDegenerate =
                                // edgeGroup.edges.Add( edge );
                            }

                            
                            if ( mesh.SharedVertexData != null )
                            {
                                /*
                                if ( edgeGroup.vertexSet == 0 )
                                {
                                    edgeGroup.vertexData = mesh.SharedVertexData;
                                }
                                else
                                {
                                    edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet - 1 ).vertexData;
                                }
                                 */ 
                            }
                            else
                            {
                                // edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet ).vertexData;
                            }
                            
                            // usage.edgeData.edgeGroups.Add( edgeGroup );
                        }
                    }

                   
                    if (!IsEOF(reader))
                        cid = ReadChunk(reader);
                }
                if (!IsEOF(reader))
                    Seek(reader, -ChunkOverheadSize);
            }
            // mesh.edgeListsBuilt = true;
        }
    }







    class MeshSerializerImplv12 : MeshSerializerImpl
    {
        public MeshSerializerImplv12()
        {
            version = "[MeshSerializer_v1.20]";
        }

        protected override void ReadMesh( BinaryReader reader )
        {
            base.ReadMesh( reader );
            // removed: mesh.AutoBuildEdgeLists = true;
        }

        protected override void ReadGeometry( BinaryReader reader, VertexData data )
        {
            ushort texCoordSet = 0;
            short bindIdx = 0;

            data.vertexStart = 0;
            data.vertexCount = ReadInt(reader);
            ReadGeometryPositions( bindIdx++, reader, data );

            if ( !IsEOF( reader ) )
            {
                MeshChunkID cid = ReadChunk( reader );
                while ( !IsEOF( reader ) &&
                    ( cid == MeshChunkID.GeometryNormals ||
                    cid == MeshChunkID.GeometryColors ||
                    cid == MeshChunkID.GeometryTexCoords ) )
                {
                    switch ( cid )
                    {
                        case MeshChunkID.GeometryNormals: ReadGeometryNormals( bindIdx++, reader, data ); break;
                        case MeshChunkID.GeometryColors: ReadGeometryColors( bindIdx++, reader, data ); break;
                        case MeshChunkID.GeometryTexCoords: ReadGeometryTexCoords( bindIdx++, reader, data, texCoordSet++ ); break;
                    }
                    if ( !IsEOF( reader ) )
                        cid = ReadChunk( reader );
                } 

                if(!IsEOF(reader))
                    Seek(reader,-ChunkOverheadSize);
            }
        }

        protected virtual void ReadGeometryPositions( short bindIdx, BinaryReader reader, VertexData data )
        {
            data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Position );
            // Reading to nothing!
            float[] barray = new float[data.vertexCount * 3];
            ReadFloats(reader, data.vertexCount * 3, barray);
        }

        protected virtual void ReadGeometryNormals( short bindIdx, BinaryReader reader, VertexData data )
        {
            data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Normal );
            // reading to nothing!
            float[] barray = new float[data.vertexCount * 3];
            ReadFloats(reader, data.vertexCount * 3, barray);
        }

        protected virtual void ReadGeometryColors( short bindIdx, BinaryReader reader, VertexData data )
        {
            data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );
            // reading to nothing!
            int[] barray = new int[data.vertexCount];
            ReadInts(reader, data.vertexCount, barray);
        }

        protected virtual void ReadGeometryTexCoords( short bindIdx, BinaryReader reader, VertexData data, int coordSet )
        {
            short dim = ReadShort( reader );

            data.vertexDeclaration.AddElement(
                bindIdx, 0,
                VertexElement.MultiplyTypeCount( VertexElementType.Float1, dim ),
                VertexElementSemantic.TexCoords,
                coordSet );
            
            // reading to nothing!
            float[] barray = new float[data.vertexCount * dim];
            ReadFloats(reader, data.vertexCount * dim, barray);
        }
    }



    class MeshSerializerImplv11 : MeshSerializerImplv12
    {
        public MeshSerializerImplv11()
        {
            version = "[MeshSerializer_v1.10]";
        }

        protected override void ReadGeometryTexCoords( short bindIdx, BinaryReader reader, VertexData data, int coordSet )
        {
            short dim = ReadShort(reader);

            data.vertexDeclaration.AddElement(
                bindIdx, 0, VertexElement.MultiplyTypeCount( VertexElementType.Float1, dim),
                VertexElementSemantic.TexCoords,coordSet);
   
           
            // reading to nothing!
            float[] barray = new float[data.vertexCount * dim];
            ReadFloats(reader, data.vertexCount * dim, barray);
            /*
            if ( dim == 2 )
            {
                int count = 0;

                unsafe
                {
                    float* pTex = (float*)texCoords.ToPointer();

                    for ( int i = 0; i < data.vertexCount; i++ )
                    {
                        count++; // skip u
                        pTex[ count ] = 1.0f - pTex[ count ]; // v = 1 - v
                        count++;
                    }
                }
            }
            */ 
        }
    }
}
