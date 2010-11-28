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
using System.Text;

namespace RexDotMeshLoader
{
    public class DotMeshLoader
    {
        public static void ReadDotMeshModel(byte[] vData, out float[] vertexList, out int[] indexList, out float[] boundsInfo, out string errorMessage)
        {
            vertexList = null;
            indexList = null;
            boundsInfo = null;
            errorMessage = "";

            try
            {
                MeshSerializerImpl TempSerializer = new MeshSerializerImpl();
                OMesh mesh = TempSerializer.ImportMesh(vData);

                if (mesh == null)
                    return;

                // bounds
                boundsInfo = new float[7];
                boundsInfo[0] = mesh.boundingBoxMin.X;
                boundsInfo[1] = mesh.boundingBoxMin.Y;
                boundsInfo[2] = mesh.boundingBoxMin.Z;
                boundsInfo[3] = mesh.boundingBoxMax.X;
                boundsInfo[4] = mesh.boundingBoxMax.Y;
                boundsInfo[5] = mesh.boundingBoxMax.Z;
                boundsInfo[6] = mesh.BoundingSphereRadius;

                Vector3 vert = new Vector3();

                // Only first submesh used in collision at the moment!
                // for(int i=0;i< mesh.subMeshList.Count;i++)
                if (mesh.subMeshList.Count > 0)
                {
                    int i = 0;
                    SubMesh sm = mesh.subMeshList[i];
                    int numFaces = sm.indexData.indexCount / 3;
                    int vCount = 0;
                    int posInc = sm.vertexData.vertexDeclaration.GetVertexSize(0); // fixme, bindindex, where to get it?
                    int index = 0;
                    VertexElement elemPos = sm.vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);

                    vertexList = new float[sm.vertexData.vertexCount * 3];
                    indexList = new int[sm.indexData.indexCount];

                    for (int k = 0; k < sm.vertexData.vertexCount; k++)
                    {
                        index = elemPos.Offset + (posInc * k);
                        vert.X = (float)(BitConverter.ToSingle(sm.vertexData.vertexBuffer, index));
                        vert.Y = (float)(BitConverter.ToSingle(sm.vertexData.vertexBuffer, index + 4));
                        vert.Z = (float)(BitConverter.ToSingle(sm.vertexData.vertexBuffer, index + 8));
                        vertexList[vCount++] = vert.X;
                        vertexList[vCount++] = vert.Y;
                        vertexList[vCount++] = vert.Z;
                    }

                    for (int k = 0; k < sm.indexData.indexCount; k++)
                    {
                        if (sm.indices_i != null)
                        {
                            indexList[k] = sm.indices_i[k];
                        }
                        else
                        {
                            indexList[k] = sm.indices_s[k];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }

        public static void ReadDotMeshMaterialNames(byte[] vData, out List<string> materials, out string errorMessage)
        {
            materials = new List<string>();
            errorMessage = "";

            try
            {
                MeshSerializerImpl TempSerializer = new MeshSerializerImpl();
                OMesh mesh = TempSerializer.ImportMesh(vData);

                if (mesh == null)
                    return;

                foreach (SubMesh submesh in mesh.subMeshList)
                {
                    materials.Add(submesh.MaterialName);
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }
    }
}
