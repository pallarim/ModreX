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
using System.Collections.Generic;

namespace RexDotMeshLoader
{
    public class Vector3
    {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;      
    }
    
    public class Vector4
    {
        public float X,Y,Z,W;
    }

    public class Quaternion
    {
        public float X, Y, Z, W;
    }

    public enum VertexElementSemantic
    {
        Position = 1,
        BlendWeights = 2,
        BlendIndices = 3,
        Normal = 4,
        Diffuse = 5,
        Specular = 6,
        TexCoords = 7,
        Binormal = 8,
        Tangent = 9
    }

    public enum VertexElementType
    {
        Float1,
        Float2,
        Float3,
        Float4,
        Color,
        Short1,
        Short2,
        Short3,
        Short4,
        UByte4
    }

    public enum MeshChunkID : ushort
    {
        Header = 0x1000,
        Mesh = 0x3000,
        SubMesh = 0x4000,
        SubMeshOperation = 0x4010,
        SubMeshBoneAssignment = 0x4100,
        Geometry = 0x5000,
        GeometryVertexDeclaration = 0x5100,
        GeometryNormals = 0x5100,
        GeometryVertexElement = 0x5110,
        GeometryVertexBuffer = 0x5200,
        GeometryColors = 0x5200,
        GeometryVertexBufferData = 0x5210,
        GeometryTexCoords = 0x5300,
        MeshSkeletonLink = 0x6000,
        MeshBoneAssignment = 0x7000,
        MeshLOD = 0x8000,
        MeshLODUsage = 0x8100,
        MeshLODManual = 0x8110,
        MeshLODGenerated = 0x8120,
        MeshBounds = 0x9000,
        SubMeshNameTable = 0xA000,
        SubMeshNameTableElement = 0xA100,
        EdgeLists = 0xB000,
        EdgeListLOD = 0xB100,
        EdgeListGroup = 0xB110,
    };
}
