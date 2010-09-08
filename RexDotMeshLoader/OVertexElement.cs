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
using System.Runtime.InteropServices;

namespace RexDotMeshLoader
{
    public class VertexElement
    {
        protected short source;
        protected int offset;
        protected VertexElementType type;
        protected VertexElementSemantic semantic;
        protected int index;

        public VertexElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic )
            : this( source, offset, type, semantic, 0 )
        {
        }

        public VertexElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            this.source = source;
            this.offset = offset;
            this.type = type;
            this.semantic = semantic;
            this.index = index;
        }

  
        public static int GetTypeSize( VertexElementType type )
        {
            switch ( type )
            {
                case VertexElementType.Color:
                    return Marshal.SizeOf( typeof( int ) );
                case VertexElementType.Float1:
                    return Marshal.SizeOf( typeof( float ) );
                case VertexElementType.Float2:
                    return Marshal.SizeOf( typeof( float ) ) * 2;
                case VertexElementType.Float3:
                    return Marshal.SizeOf( typeof( float ) ) * 3;
                case VertexElementType.Float4:
                    return Marshal.SizeOf( typeof( float ) ) * 4;
                case VertexElementType.Short1:
                    return Marshal.SizeOf( typeof( short ) );
                case VertexElementType.Short2:
                    return Marshal.SizeOf( typeof( short ) ) * 2;
                case VertexElementType.Short3:
                    return Marshal.SizeOf( typeof( short ) ) * 3;
                case VertexElementType.Short4:
                    return Marshal.SizeOf( typeof( short ) ) * 4;
                case VertexElementType.UByte4:
                    return Marshal.SizeOf( typeof( byte ) ) * 4;
            }
            return 0;
        }

        public static int GetTypeCount( VertexElementType type )
        {
            switch ( type )
            {
                case VertexElementType.Color:
                    return 1;
                case VertexElementType.Float1:
                    return 1;
                case VertexElementType.Float2:
                    return 2;
                case VertexElementType.Float3:
                    return 3;
                case VertexElementType.Float4:
                    return 4;
                case VertexElementType.Short1:
                    return 1;
                case VertexElementType.Short2:
                    return 2;
                case VertexElementType.Short3:
                    return 3;
                case VertexElementType.Short4:
                    return 4;
                case VertexElementType.UByte4:
                    return 4;
            } 		
            return 0;
        }

        public static VertexElementType MultiplyTypeCount( VertexElementType type, int count )
        {
            switch ( type )
            {
                case VertexElementType.Float1:
                    switch ( count )
                    {
                        case 1:
                            return VertexElementType.Float1;
                        case 2:
                            return VertexElementType.Float2;
                        case 3:
                            return VertexElementType.Float3;
                        case 4:
                            return VertexElementType.Float4;
                    }
                    break;

                case VertexElementType.Short1:
                    switch ( count )
                    {
                        case 1:
                            return VertexElementType.Short1;
                        case 2:
                            return VertexElementType.Short2;
                        case 3:
                            return VertexElementType.Short3;
                        case 4:
                            return VertexElementType.Short4;
                    }
                    break;
            }
            throw new Exception("Error multiplying base vertex element type: " + type.ToString());
        }

        public short Source
        {
            get
            {
                return source;
            }
        }

        public int Offset
        {
            get
            {
                return offset;
            }
        }

        public VertexElementType Type
        {
            get
            {
                return type;
            }
        }

        public VertexElementSemantic Semantic
        {
            get
            {
                return semantic;
            }
        }

        public int Index
        {
            get
            {
                return index;
            }
        }

        public int Size
        {
            get
            {
                return GetTypeSize( type );
            }
        }    
    }
}
