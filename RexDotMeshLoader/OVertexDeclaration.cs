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
    public class VertexDeclaration 
    {
        protected ArrayList elements = new ArrayList();

        #region Methods
        public VertexElement AddElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic )
        {
            return AddElement( source, offset, type, semantic, 0 );
        }

        public virtual VertexElement AddElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            VertexElement element = new VertexElement( source, offset, type, semantic, index );
            elements.Add( element );
            return element;
        }

        public VertexElement FindElementBySemantic( VertexElementSemantic semantic )
        {
            return FindElementBySemantic( semantic, 0 );
        }

        public virtual VertexElement FindElementBySemantic( VertexElementSemantic semantic, short index )
        {
            for ( int i = 0; i < elements.Count; i++ )
            {
                VertexElement element = (VertexElement)elements[ i ];
                if ( element.Semantic == semantic && element.Index == index )
                    return element;
            }
            return null;
        }

        public virtual ArrayList FindElementBySource(ushort source)
        {
            for ( int i = 0; i < elements.Count; i++ )
            {
                VertexElement element = (VertexElement)elements[ i ];

                if ( element.Source == source )
                    elements.Add( element );
            }


            return elements;
        }

        public VertexElement GetElement(int vIndex)
        {
            if (vIndex < elements.Count && vIndex >= 0)
                return (VertexElement)elements[vIndex];
            else
                return null;
        }

        public virtual int GetVertexSize(short source)
        {
            int size = 0;
            for ( int i = 0; i < elements.Count; i++ )
            {
                VertexElement element = (VertexElement)elements[ i ];

                if ( element.Source == source )
                    size += element.Size;
            }
            return size;
        }

        public VertexElement InsertElement( int position, short source, int offset, VertexElementType type, VertexElementSemantic semantic )
        {
            return InsertElement( position, source, offset, type, semantic, 0 );
        }


        public virtual VertexElement InsertElement( int position, short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            if ( position >= elements.Count )
            {
                return AddElement( source, offset, type, semantic, index );
            }

            VertexElement element = new VertexElement( source, offset, type, semantic, index );
            elements.Insert( position, element );
            return element;
        }

        public virtual void RemoveElement(int vIndex)
        {
            if(vIndex >= 0 && vIndex < elements.Count)
                elements.RemoveAt(vIndex);
        }

        public void ModifyElement( int elemIndex, short source, int offset, VertexElementType type, VertexElementSemantic semantic )
        {
            ModifyElement( elemIndex, source, offset, type, semantic, 0 );
        }

        public virtual void ModifyElement( int elemIndex, short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            elements[ elemIndex ] = new VertexElement( source, offset, type, semantic, index );
        }

        public void RemoveElement( VertexElementSemantic semantic )
        {
            RemoveElement( semantic, 0 );
        }

        public virtual void RemoveElement( VertexElementSemantic semantic, int index )
        {
            for ( int i = elements.Count - 1; i >= 0; i-- )
            {
                VertexElement element = (VertexElement)elements[ i ];

                if ( element.Semantic == semantic && element.Index == index )
                {
                    elements.RemoveAt( i );
                }
            }
        }

        public static bool operator ==( VertexDeclaration left, VertexDeclaration right )
        {
            if ( left.elements.Count != right.elements.Count )
                return false;

            for ( int i = 0; i < right.elements.Count; i++ )
            {
                VertexDeclaration a = (VertexDeclaration)left.elements[ i ];
                VertexDeclaration b = (VertexDeclaration)right.elements[ i ];

                if ( !( a == b ) )
                    return false;
            }
            return true;
        }

        public static bool operator !=( VertexDeclaration left, VertexDeclaration right )
        {
            return !( left == right );
        }

        #endregion

        public int ElementCount
        {
            get
            {
                return elements.Count;
            }
        }
        
        public override bool Equals( object obj )
        {
            VertexDeclaration decl = obj as VertexDeclaration;

            return ( decl == this );
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}
