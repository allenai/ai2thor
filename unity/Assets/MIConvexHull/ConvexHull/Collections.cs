/******************************************************************************
 *
 * The MIT License (MIT)
 *
 * MIConvexHull, Copyright (c) 2015 David Sehnal, Matthew Campbell
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *  
 *****************************************************************************/

using System;

namespace MIConvexHull
{
    /// <summary>
    /// A more lightweight alternative to List of T.
    /// On clear, only resets the count and does not clear the references
    /// =&gt; this works because of the ObjectManager.
    /// Includes a stack functionality.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SimpleList<T>
    {
        /// <summary>
        /// The capacity
        /// </summary>
        private int capacity;

        /// <summary>
        /// The count
        /// </summary>
        public int Count;
        /// <summary>
        /// The items
        /// </summary>
        private T[] items;

        /// <summary>
        /// Get the i-th element.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns>T.</returns>
        public T this[int i]
        {
            get { return items[i]; }
            set { items[i] = value; }
        }

        /// <summary>
        /// Size matters.
        /// </summary>
        private void EnsureCapacity()
        {
            if (capacity == 0)
            {
                capacity = 32;
                items = new T[32];
            }
            else
            {
                var newItems = new T[capacity * 2];
                Array.Copy(items, newItems, capacity);
                capacity = 2 * capacity;
                items = newItems;
            }
        }

        /// <summary>
        /// Adds a vertex to the buffer.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(T item)
        {
            if (Count + 1 > capacity) EnsureCapacity();
            items[Count++] = item;
        }

        /// <summary>
        /// Pushes the value to the back of the list.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Push(T item)
        {
            if (Count + 1 > capacity) EnsureCapacity();
            items[Count++] = item;
        }

        /// <summary>
        /// Pops the last value from the list.
        /// </summary>
        /// <returns>T.</returns>
        public T Pop()
        {
            return items[--Count];
        }

        /// <summary>
        /// Sets the Count to 0, otherwise does nothing.
        /// </summary>
        public void Clear()
        {
            Count = 0;
        }
    }


    /// <summary>
    /// Class IndexBuffer.
    /// A fancy name for a list of integers.
    /// </summary>
    internal class IndexBuffer : SimpleList<int>
    {
    }

    /// <summary>
    /// A priority based linked list.
    /// </summary>
    internal sealed class FaceList
    {
        /// <summary>
        /// The last
        /// </summary>
        private ConvexFaceInternal last;

        /// <summary>
        /// Get the first element.
        /// </summary>
        /// <value>The first.</value>
        public ConvexFaceInternal First { get; private set; }

        /// <summary>
        /// Adds the element to the beginning.
        /// </summary>
        /// <param name="face">The face.</param>
        private void AddFirst(ConvexFaceInternal face)
        {
            face.InList = true;
            First.Previous = face;
            face.Next = First;
            First = face;
        }

        /// <summary>
        /// Adds a face to the list.
        /// </summary>
        /// <param name="face">The face.</param>
        public void Add(ConvexFaceInternal face)
        {
            if (face.InList)
            {
                if (First.VerticesBeyond.Count < face.VerticesBeyond.Count)
                {
                    Remove(face);
                    AddFirst(face);
                }
                return;
            }

            face.InList = true;

            if (First != null && First.VerticesBeyond.Count < face.VerticesBeyond.Count)
            {
                First.Previous = face;
                face.Next = First;
                First = face;
            }
            else
            {
                if (last != null)
                {
                    last.Next = face;
                }
                face.Previous = last;
                last = face;
                if (First == null)
                {
                    First = face;
                }
            }
        }

        /// <summary>
        /// Removes the element from the list.
        /// </summary>
        /// <param name="face">The face.</param>
        public void Remove(ConvexFaceInternal face)
        {
            if (!face.InList) return;

            face.InList = false;

            if (face.Previous != null)
            {
                face.Previous.Next = face.Next;
            }
            else if ( /*first == face*/ face.Previous == null)
            {
                First = face.Next;
            }

            if (face.Next != null)
            {
                face.Next.Previous = face.Previous;
            }
            else if ( /*last == face*/ face.Next == null)
            {
                last = face.Previous;
            }

            face.Next = null;
            face.Previous = null;
        }
    }

    /// <summary>
    /// Connector list.
    /// </summary>
    internal sealed class ConnectorList
    {
        /// <summary>
        /// The last
        /// </summary>
        private FaceConnector last;

        /// <summary>
        /// Get the first element.
        /// </summary>
        /// <value>The first.</value>
        public FaceConnector First { get; private set; }

        /// <summary>
        /// Adds the element to the beginning.
        /// </summary>
        /// <param name="connector">The connector.</param>
        private void AddFirst(FaceConnector connector)
        {
            First.Previous = connector;
            connector.Next = First;
            First = connector;
        }

        /// <summary>
        /// Adds a face to the list.
        /// </summary>
        /// <param name="element">The element.</param>
        public void Add(FaceConnector element)
        {
            if (last != null)
            {
                last.Next = element;
            }
            element.Previous = last;
            last = element;
            if (First == null)
            {
                First = element;
            }
        }

        /// <summary>
        /// Removes the element from the list.
        /// </summary>
        /// <param name="connector">The connector.</param>
        public void Remove(FaceConnector connector)
        {
            if (connector.Previous != null)
            {
                connector.Previous.Next = connector.Next;
            }
            else if ( /*first == face*/ connector.Previous == null)
            {
                First = connector.Next;
            }

            if (connector.Next != null)
            {
                connector.Next.Previous = connector.Previous;
            }
            else if ( /*last == face*/ connector.Next == null)
            {
                last = connector.Previous;
            }

            connector.Next = null;
            connector.Previous = null;
        }
    }
}