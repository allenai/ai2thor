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
    /// A helper class for object allocation/storage.
    /// This helps the GC a lot as it prevents the creation of about 75% of
    /// new face objects (in the case of ConvexFaceInternal). In the case of
    /// FaceConnectors and DefferedFaces, the difference is even higher (in most
    /// cases O(1) vs O(number of created faces)).
    /// </summary>
    internal class ObjectManager
    {
        /// <summary>
        /// The dimension
        /// </summary>
        private readonly int Dimension;
        /// <summary>
        /// The connector stack
        /// </summary>
        private FaceConnector ConnectorStack;
        /// <summary>
        /// The deferred face stack
        /// </summary>
        private readonly SimpleList<DeferredFace> DeferredFaceStack;
        /// <summary>
        /// The empty buffer stack
        /// </summary>
        private readonly SimpleList<IndexBuffer> EmptyBufferStack;
        /// <summary>
        /// The face pool
        /// </summary>
        private ConvexFaceInternal[] FacePool;
        /// <summary>
        /// The face pool size
        /// </summary>
        private int FacePoolSize;
        /// <summary>
        /// The face pool capacity
        /// </summary>
        private int FacePoolCapacity;
        /// <summary>
        /// The free face indices
        /// </summary>
        private readonly IndexBuffer FreeFaceIndices;

        /// <summary>
        /// The hull
        /// </summary>
        private readonly ConvexHullAlgorithm Hull;

        /// <summary>
        /// Create the manager.
        /// </summary>
        /// <param name="hull">The hull.</param>
        public ObjectManager(ConvexHullAlgorithm hull)
        {
            Dimension = hull.NumOfDimensions;
            Hull = hull;
            FacePool = hull.FacePool;
            FacePoolSize = 0;
            FacePoolCapacity = hull.FacePool.Length;
            FreeFaceIndices = new IndexBuffer();

            EmptyBufferStack = new SimpleList<IndexBuffer>();
            DeferredFaceStack = new SimpleList<DeferredFace>();
        }

        /// <summary>
        /// Return the face to the pool for later use.
        /// </summary>
        /// <param name="faceIndex">Index of the face.</param>
        public void DepositFace(int faceIndex)
        {
            var face = FacePool[faceIndex];
            var af = face.AdjacentFaces;
            for (var i = 0; i < af.Length; i++)
            {
                af[i] = -1;
            }
            FreeFaceIndices.Push(faceIndex);
        }

        /// <summary>
        /// Reallocate the face pool, including the AffectedFaceFlags
        /// </summary>
        private void ReallocateFacePool()
        {
            var newPool = new ConvexFaceInternal[2 * FacePoolCapacity];
            var newTags = new bool[2 * FacePoolCapacity];
            Array.Copy(FacePool, newPool, FacePoolCapacity);
            Buffer.BlockCopy(Hull.AffectedFaceFlags, 0, newTags, 0, FacePoolCapacity * sizeof(bool));
            FacePoolCapacity = 2 * FacePoolCapacity;
            Hull.FacePool = newPool;
            FacePool = newPool;
            Hull.AffectedFaceFlags = newTags;
        }

        /// <summary>
        /// Create a new face and put it in the pool.
        /// </summary>
        /// <returns>System.Int32.</returns>
        private int CreateFace()
        {
            var index = FacePoolSize;
            var face = new ConvexFaceInternal(Dimension, index, GetVertexBuffer());
            FacePoolSize++;
            if (FacePoolSize > FacePoolCapacity) ReallocateFacePool();
            FacePool[index] = face;
            return index;
        }

        /// <summary>
        /// Return index of an unused face or creates a new one.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetFace()
        {
            if (FreeFaceIndices.Count > 0) return FreeFaceIndices.Pop();
            return CreateFace();
        }

        /// <summary>
        /// Store a face connector in the "embedded" linked list.
        /// </summary>
        /// <param name="connector">The connector.</param>
        public void DepositConnector(FaceConnector connector)
        {
            if (ConnectorStack == null)
            {
                connector.Next = null;
                ConnectorStack = connector;
            }
            else
            {
                connector.Next = ConnectorStack;
                ConnectorStack = connector;
            }
        }

        /// <summary>
        /// Get an unused face connector. If none is available, create it.
        /// </summary>
        /// <returns>FaceConnector.</returns>
        public FaceConnector GetConnector()
        {
            if (ConnectorStack == null) return new FaceConnector(Dimension);

            var ret = ConnectorStack;
            ConnectorStack = ConnectorStack.Next;
            ret.Next = null;
            return ret;
        }

        /// <summary>
        /// Deposit the index buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public void DepositVertexBuffer(IndexBuffer buffer)
        {
            buffer.Clear();
            EmptyBufferStack.Push(buffer);
        }

        /// <summary>
        /// Get a store index buffer or create a new instance.
        /// </summary>
        /// <returns>IndexBuffer.</returns>
        public IndexBuffer GetVertexBuffer()
        {
            return EmptyBufferStack.Count != 0 ? EmptyBufferStack.Pop() : new IndexBuffer();
        }

        /// <summary>
        /// Deposit the deferred face.
        /// </summary>
        /// <param name="face">The face.</param>
        public void DepositDeferredFace(DeferredFace face)
        {
            DeferredFaceStack.Push(face);
        }

        /// <summary>
        /// Get the deferred face.
        /// </summary>
        /// <returns>DeferredFace.</returns>
        public DeferredFace GetDeferredFace()
        {
            return DeferredFaceStack.Count != 0 ? DeferredFaceStack.Pop() : new DeferredFace();
        }
    }
}