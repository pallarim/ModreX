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
using System.Collections;

namespace RexDotMeshLoader
{
    public class SubMesh
    {
        public short[] indices_s = null;
        public int[] indices_i = null;
        public OMesh Parent;
        public string MaterialName;
        internal string name;
        protected internal short numFaces;
        public VertexData vertexData;
        public IndexData indexData = new IndexData();
        public bool useSharedVertices;
     
        public SubMesh( string vName )
        {
            name = vName;
            useSharedVertices = true;
        }
    }
}
