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
using System.Collections.Generic;

namespace MIConvexHull
{
    /*
     * Main part of the algorithm 
     * Basic idea:
     * - Create the initial hull (done in Initialize.cs)
     * 
     *   For each face there are "vertices beyond" which are "visible" from it. 
     *   If there are no such vertices, the face is on the hull.
     *   
     * - While there is at least one face with at least one "vertex beyond":
     *   * Pick the furthest beyond vertex
     *   * For this vertex:
     *     > find all faces that are visible from it (TagAffectedFaces)
     *     > remove them and replace them with a "cone" created by the vertex and the boundary
     *       of the affected faces, and for each new face, compute "beyond vertices" 
     *       (CreateCone + CommitCone)
     * 
     * + Implement it in way that is fast, but hard to understand and maintain.
     */

    /// <summary>
    /// Class ConvexHullAlgorithm.
    /// </summary>
    internal partial class ConvexHullAlgorithm
    {
        /// <summary>
        /// Tags all faces seen from the current vertex with 1.
        /// </summary>
        /// <param name="currentFace">The current face.</param>
        private void TagAffectedFaces(ConvexFaceInternal currentFace)
        {
            AffectedFaceBuffer.Clear();
            AffectedFaceBuffer.Add(currentFace.Index);
            TraverseAffectedFaces(currentFace.Index);
        }

        /// <summary>
        /// Recursively traverse all the relevant faces.
        /// </summary>
        /// <param name="currentFace">The current face.</param>
        private void TraverseAffectedFaces(int currentFace)
        {
            TraverseStack.Clear();
            TraverseStack.Push(currentFace);
            AffectedFaceFlags[currentFace] = true;

            while (TraverseStack.Count > 0)
            {
                var top = FacePool[TraverseStack.Pop()];
                for (var i = 0; i < NumOfDimensions; i++)
                {
                    var adjFace = top.AdjacentFaces[i];

                    if (!AffectedFaceFlags[adjFace] &&
                        mathHelper.GetVertexDistance(CurrentVertex, FacePool[adjFace]) >= PlaneDistanceTolerance)
                    {
                        AffectedFaceBuffer.Add(adjFace);
                        AffectedFaceFlags[adjFace] = true;
                        TraverseStack.Push(adjFace);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new deferred face.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="faceIndex">Index of the face.</param>
        /// <param name="pivot">The pivot.</param>
        /// <param name="pivotIndex">Index of the pivot.</param>
        /// <param name="oldFace">The old face.</param>
        /// <returns>DeferredFace.</returns>
        private DeferredFace MakeDeferredFace(ConvexFaceInternal face, int faceIndex, ConvexFaceInternal pivot,
            int pivotIndex, ConvexFaceInternal oldFace)
        {
            var ret = ObjectManager.GetDeferredFace();

            ret.Face = face;
            ret.FaceIndex = faceIndex;
            ret.Pivot = pivot;
            ret.PivotIndex = pivotIndex;
            ret.OldFace = oldFace;

            return ret;
        }

        /// <summary>
        /// Connect faces using a connector.
        /// </summary>
        /// <param name="connector">The connector.</param>
        private void ConnectFace(FaceConnector connector)
        {
            var index = connector.HashCode % Constants.ConnectorTableSize;
            var list = ConnectorTable[index];

            for (var current = list.First; current != null; current = current.Next)
            {
                if (FaceConnector.AreConnectable(connector, current, NumOfDimensions))
                {
                    list.Remove(current);
                    FaceConnector.Connect(current, connector);
                    current.Face = null;
                    connector.Face = null;
                    ObjectManager.DepositConnector(current);
                    ObjectManager.DepositConnector(connector);
                    return;
                }
            }

            list.Add(connector);
        }

        /// <summary>
        /// Removes the faces "covered" by the current vertex and adds the newly created ones.
        /// </summary>
        /// <returns><c>true</c> if possible, <c>false</c> otherwise.</returns>
        private bool CreateCone()
        {
            var currentVertexIndex = CurrentVertex;
            ConeFaceBuffer.Clear();

            for (var fIndex = 0; fIndex < AffectedFaceBuffer.Count; fIndex++)
            {
                var oldFaceIndex = AffectedFaceBuffer[fIndex];
                var oldFace = FacePool[oldFaceIndex];

                // Find the faces that need to be updated
                var updateCount = 0;
                for (var i = 0; i < NumOfDimensions; i++)
                {
                    var af = oldFace.AdjacentFaces[i];
                    if (!AffectedFaceFlags[af]) // Tag == false when oldFaces does not contain af
                    {
                        UpdateBuffer[updateCount] = af;
                        UpdateIndices[updateCount] = i;
                        ++updateCount;
                    }
                }

                for (var i = 0; i < updateCount; i++)
                {
                    var adjacentFace = FacePool[UpdateBuffer[i]];

                    var oldFaceAdjacentIndex = 0;
                    var adjFaceAdjacency = adjacentFace.AdjacentFaces;
                    for (var j = 0; j < adjFaceAdjacency.Length; j++)
                    {
                        if (oldFaceIndex == adjFaceAdjacency[j])
                        {
                            oldFaceAdjacentIndex = j;
                            break;
                        }
                    }

                    var forbidden = UpdateIndices[i]; // Index of the face that corresponds to this adjacent face

                    var newFaceIndex = ObjectManager.GetFace();
                    var newFace = FacePool[newFaceIndex];
                    var vertices = newFace.Vertices;
                    for (var j = 0; j < NumOfDimensions; j++) vertices[j] = oldFace.Vertices[j];
                    var oldVertexIndex = vertices[forbidden];

                    int orderedPivotIndex;

                    // correct the ordering
                    if (currentVertexIndex < oldVertexIndex)
                    {
                        orderedPivotIndex = 0;
                        for (var j = forbidden - 1; j >= 0; j--)
                        {
                            if (vertices[j] > currentVertexIndex) vertices[j + 1] = vertices[j];
                            else
                            {
                                orderedPivotIndex = j + 1;
                                break;
                            }
                        }
                    }
                    else
                    {
                        orderedPivotIndex = NumOfDimensions - 1;
                        for (var j = forbidden + 1; j < NumOfDimensions; j++)
                        {
                            if (vertices[j] < currentVertexIndex) vertices[j - 1] = vertices[j];
                            else
                            {
                                orderedPivotIndex = j - 1;
                                break;
                            }
                        }
                    }

                    vertices[orderedPivotIndex] = CurrentVertex;

                    if (!mathHelper.CalculateFacePlane(newFace, Center))
                    {
                        return false;
                    }

                    ConeFaceBuffer.Add(MakeDeferredFace(newFace, orderedPivotIndex, adjacentFace, oldFaceAdjacentIndex,
                        oldFace));
                }
            }

            return true;
        }

        /// <summary>
        /// Commits a cone and adds a vertex to the convex hull.
        /// </summary>
        private void CommitCone()
        {
            // Fill the adjacency.
            for (var i = 0; i < ConeFaceBuffer.Count; i++)
            {
                var face = ConeFaceBuffer[i];

                var newFace = face.Face;
                var adjacentFace = face.Pivot;
                var oldFace = face.OldFace;
                var orderedPivotIndex = face.FaceIndex;

                newFace.AdjacentFaces[orderedPivotIndex] = adjacentFace.Index;
                adjacentFace.AdjacentFaces[face.PivotIndex] = newFace.Index;

                // let there be a connection.
                for (var j = 0; j < NumOfDimensions; j++)
                {
                    if (j == orderedPivotIndex) continue;
                    var connector = ObjectManager.GetConnector();
                    connector.Update(newFace, j, NumOfDimensions);
                    ConnectFace(connector);
                }

                // the id adjacent face on the hull? If so, we can use simple method to find beyond vertices.
                if (adjacentFace.VerticesBeyond.Count == 0)
                    FindBeyondVertices(newFace, oldFace.VerticesBeyond);
                // it is slightly more effective if the face with the lower number of beyond vertices comes first.
                else if (adjacentFace.VerticesBeyond.Count < oldFace.VerticesBeyond.Count)
                    FindBeyondVertices(newFace, adjacentFace.VerticesBeyond, oldFace.VerticesBeyond);
                else
                    FindBeyondVertices(newFace, oldFace.VerticesBeyond, adjacentFace.VerticesBeyond);

                // This face will definitely lie on the hull
                if (newFace.VerticesBeyond.Count == 0)
                {
                    ConvexFaces.Add(newFace.Index);
                    UnprocessedFaces.Remove(newFace);
                    ObjectManager.DepositVertexBuffer(newFace.VerticesBeyond);
                    newFace.VerticesBeyond = EmptyBuffer;
                }
                else // Add the face to the list
                {
                    UnprocessedFaces.Add(newFace);
                }

                // recycle the object.
                ObjectManager.DepositDeferredFace(face);
            }

            // Recycle the affected faces.
            for (var fIndex = 0; fIndex < AffectedFaceBuffer.Count; fIndex++)
            {
                var face = AffectedFaceBuffer[fIndex];
                UnprocessedFaces.Remove(FacePool[face]);
                ObjectManager.DepositFace(face);
            }
        }

        /// <summary>
        /// Check whether the vertex v is beyond the given face. If so, add it to beyondVertices.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="beyondVertices">The beyond vertices.</param>
        /// <param name="v">The v.</param>
        private void IsBeyond(ConvexFaceInternal face, IndexBuffer beyondVertices, int v)
        {
            var distance = mathHelper.GetVertexDistance(v, face);
            if (distance >= PlaneDistanceTolerance)
            {
                if (distance > MaxDistance)
                {
                    // If it's within the tolerance distance, use the lex. larger point
                    if (distance - MaxDistance < PlaneDistanceTolerance)
                    { // todo: why is this LexCompare necessary. Would seem to favor x over y over z (etc.)?
                        if (LexCompare(v, FurthestVertex) > 0)
                        {
                            MaxDistance = distance;
                            FurthestVertex = v;
                        }
                    }
                    else
                    {
                        MaxDistance = distance;
                        FurthestVertex = v;
                    }
                }
                beyondVertices.Add(v);
            }
        }


        /// <summary>
        /// Compares the values of two vertices. The return value (-1, 0 or +1) are found
        /// by first checking the first coordinate and then progressing through the rest.
        /// In this way {2, 8} will be a "-1" (less than) {3, 1}.
        /// </summary>
        /// <param name="u">The base vertex index, u.</param>
        /// <param name="v">The compared vertex index, v.</param>
        /// <returns>System.Int32.</returns>
        private int LexCompare(int u, int v)
        {
            int uOffset = u * NumOfDimensions, vOffset = v * NumOfDimensions;
            for (var i = 0; i < NumOfDimensions; i++)
            {
                double x = Positions[uOffset + i], y = Positions[vOffset + i];
                var comp = x.CompareTo(y);
                if (comp != 0) return comp;
            }
            return 0;
        }


        /// <summary>
        /// Used by update faces.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="beyond">The beyond.</param>
        /// <param name="beyond1">The beyond1.</param>
        private void FindBeyondVertices(ConvexFaceInternal face, IndexBuffer beyond, IndexBuffer beyond1)
        {
            var beyondVertices = BeyondBuffer;

            MaxDistance = double.NegativeInfinity;
            FurthestVertex = 0;
            int v;

            for (var i = 0; i < beyond1.Count; i++) VertexVisited[beyond1[i]] = true;
            VertexVisited[CurrentVertex] = false;
            for (var i = 0; i < beyond.Count; i++)
            {
                v = beyond[i];
                if (v == CurrentVertex) continue;
                VertexVisited[v] = false;
                IsBeyond(face, beyondVertices, v);
            }

            for (var i = 0; i < beyond1.Count; i++)
            {
                v = beyond1[i];
                if (VertexVisited[v]) IsBeyond(face, beyondVertices, v);
            }

            face.FurthestVertex = FurthestVertex;

            // Pull the old switch a roo (switch the face beyond buffers)
            var temp = face.VerticesBeyond;
            face.VerticesBeyond = beyondVertices;
            if (temp.Count > 0) temp.Clear();
            BeyondBuffer = temp;
        }

        /// <summary>
        /// Finds the beyond vertices.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="beyond">The beyond.</param>
        private void FindBeyondVertices(ConvexFaceInternal face, IndexBuffer beyond)
        {
            var beyondVertices = BeyondBuffer;

            MaxDistance = double.NegativeInfinity;
            FurthestVertex = 0;

            for (var i = 0; i < beyond.Count; i++)
            {
                var v = beyond[i];
                if (v == CurrentVertex) continue;
                IsBeyond(face, beyondVertices, v);
            }

            face.FurthestVertex = FurthestVertex;

            // Pull the old switch a roo (switch the face beyond buffers)
            var temp = face.VerticesBeyond;
            face.VerticesBeyond = beyondVertices;
            if (temp.Count > 0) temp.Clear();
            BeyondBuffer = temp;
        }

        /// <summary>
        /// Recalculates the centroid of the current hull.
        /// </summary>
        private void UpdateCenter()
        {
            for (var i = 0; i < NumOfDimensions; i++) Center[i] *= ConvexHullSize;
            ConvexHullSize += 1;
            var f = 1.0 / ConvexHullSize;
            var co = CurrentVertex * NumOfDimensions;
            for (var i = 0; i < NumOfDimensions; i++) Center[i] = f * (Center[i] + Positions[co + i]);
        }

        /// <summary>
        /// Removes the last vertex from the center.
        /// </summary>
        private void RollbackCenter()
        {
            for (var i = 0; i < NumOfDimensions; i++) Center[i] *= ConvexHullSize;
            ConvexHullSize -= 1;
            var f = ConvexHullSize > 0 ? 1.0 / ConvexHullSize : 0.0;
            var co = CurrentVertex * NumOfDimensions;
            for (var i = 0; i < NumOfDimensions; i++) Center[i] = f * (Center[i] - Positions[co + i]);
        }

        /// <summary>
        /// Handles singular vertex.
        /// </summary>
        private void HandleSingular()
        {
            RollbackCenter();
            SingularVertices.Add(CurrentVertex);

            // This means that all the affected faces must be on the hull and that all their "vertices beyond" are singular.
            for (var fIndex = 0; fIndex < AffectedFaceBuffer.Count; fIndex++)
            {
                var face = FacePool[AffectedFaceBuffer[fIndex]];
                var vb = face.VerticesBeyond;
                for (var i = 0; i < vb.Count; i++)
                {
                    SingularVertices.Add(vb[i]);
                }

                ConvexFaces.Add(face.Index);
                UnprocessedFaces.Remove(face);
                ObjectManager.DepositVertexBuffer(face.VerticesBeyond);
                face.VerticesBeyond = EmptyBuffer;
            }
        }

        /// <summary>
        /// Get a vertex coordinate. In order to reduce speed, all vertex coordinates
        /// have been placed in a single array.
        /// </summary>
        /// <param name="vIndex">The vertex index.</param>
        /// <param name="dimension">The index of the dimension.</param>
        /// <returns>System.Double.</returns>
        private double GetCoordinate(int vIndex, int dimension)
        {
            return Positions[vIndex * NumOfDimensions + dimension];
        }

        #region Returning the Results in the proper format

        /// <summary>
        /// Gets the hull vertices.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>TVertex[].</returns>
        internal TVertex[] GetHullVertices<TVertex>(IList<TVertex> data)
        {
            var cellCount = ConvexFaces.Count;
            var hullVertexCount = 0;

            for (var i = 0; i < NumberOfVertices; i++) VertexVisited[i] = false;

            for (var i = 0; i < cellCount; i++)
            {
                var vs = FacePool[ConvexFaces[i]].Vertices;
                for (var j = 0; j < vs.Length; j++)
                {
                    var v = vs[j];
                    if (!VertexVisited[v])
                    {
                        VertexVisited[v] = true;
                        hullVertexCount++;
                    }
                }
            }

            var result = new TVertex[hullVertexCount];
            for (var i = 0; i < NumberOfVertices; i++)
            {
                if (VertexVisited[i]) result[--hullVertexCount] = data[i];
            }

            return result;
        }

        /// <summary>
        /// Finds the convex hull and creates the TFace objects.
        /// </summary>
        /// <typeparam name="TFace">The type of the t face.</typeparam>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <returns>TFace[].</returns>
        internal TFace[] GetConvexFaces<TVertex, TFace>()
            where TFace : ConvexFace<TVertex, TFace>, new()
            where TVertex : IVertex
        {
            var faces = ConvexFaces;
            var cellCount = faces.Count;
            var cells = new TFace[cellCount];

            for (var i = 0; i < cellCount; i++)
            {
                var face = FacePool[faces[i]];
                var vertices = new TVertex[NumOfDimensions];
                for (var j = 0; j < NumOfDimensions; j++)
                {
                    vertices[j] = (TVertex)Vertices[face.Vertices[j]];
                }

                cells[i] = new TFace
                {
                    Vertices = vertices,
                    Adjacency = new TFace[NumOfDimensions],
                    Normal = IsLifted ? null : face.Normal
                };
                face.Tag = i;
            }

            for (var i = 0; i < cellCount; i++)
            {
                var face = FacePool[faces[i]];
                var cell = cells[i];
                for (var j = 0; j < NumOfDimensions; j++)
                {
                    if (face.AdjacentFaces[j] < 0) continue;
                    cell.Adjacency[j] = cells[FacePool[face.AdjacentFaces[j]].Tag];
                }

                // Fix the vertex orientation.
                if (face.IsNormalFlipped)
                {
                    var tempVertex = cell.Vertices[0];
                    cell.Vertices[0] = cell.Vertices[NumOfDimensions - 1];
                    cell.Vertices[NumOfDimensions - 1] = tempVertex;

                    var tempAdj = cell.Adjacency[0];
                    cell.Adjacency[0] = cell.Adjacency[NumOfDimensions - 1];
                    cell.Adjacency[NumOfDimensions - 1] = tempAdj;
                }
            }

            return cells;
        }

        #endregion
    }
}
