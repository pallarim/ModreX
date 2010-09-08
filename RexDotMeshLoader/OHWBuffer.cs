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
    public class HWBufferManager
    {
        private static HWBufferManager inst;
        protected ArrayList vertexDeclarations = new ArrayList();

        protected internal HWBufferManager()
        {
            if (inst == null)
                inst = this;
        }

        public static HWBufferManager Instance
        {
            get
            {
                if (inst == null)
                    inst = new HWBufferManager();
                return inst;
            }
        }

        public virtual VertexDeclaration CreateVertexDeclaration()
        {
            VertexDeclaration d = new VertexDeclaration();
            vertexDeclarations.Add(d);
            return d;
        }
    }

}
