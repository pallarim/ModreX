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
using System.Collections.Generic;

namespace RexDotMeshLoader
{
    public class MeshLodUsage { } 
    public class MeshLodUsageList { }

    public class OMesh
    {
        protected string name;
        public VertexData SharedVertexData;
        public List<SubMesh> subMeshList = new List<SubMesh>();
        public bool IsManuallyDefined = false;
        public bool isLodManual;
        public float BoundingSphereRadius;

        public Vector3 boundingBoxMin, boundingBoxMax;

        // skeletons removed.

        // lod
        protected internal int numLods;
        protected internal MeshLodUsageList lodUsageList = new MeshLodUsageList();

        public OMesh(string vName)
        {
            name = vName;
        }
         
        public SubMesh GetSubMesh(int index)
        {
            if (index < subMeshList.Count)
                return subMeshList[index];
            else
                return null;
        }

        /*
        public SubMesh GetSubMesh( string name )
        {
            for(int i = 0; i < subMeshList.Count; i++ )
            {
                if (subMeshList[i].name == name)
                {
                    return subMeshList[i];
                }
            }
            throw new Exception("Submesh not found with name "+name); 
        }
        */

        public SubMesh CreateSubMesh(string vName)
        {
            SubMesh subMesh = new SubMesh(vName);
            subMesh.Parent = this;
            subMeshList.Add(subMesh);

            return subMesh;
        }
        
        public SubMesh CreateSubMesh()
        {
            string tempname = string.Format( "{0}_SubMesh(1)", this.name, subMeshList.Count );

            SubMesh subMesh = new SubMesh(tempname);
            subMesh.Parent = this;
            subMeshList.Add(subMesh);

            return subMesh;
        }
    }
}
