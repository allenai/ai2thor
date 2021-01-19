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
using System.Linq;

namespace MIConvexHull
{
    /*
     * This part of the implementation handles initialization of the convex hull algorithm:
     * - Constructor & Process initiation 
     * - Determine the dimension by looking at length of Position vector of 10 random data points from the input. 
     * - Identify bounding box points in each direction.
     * - Pick (Dimension + 1) points from the extremes and construct the initial simplex.
     */

    /// <summary>
    /// Class ConvexHullAlgorithm.
    /// </summary>
    internal partial class ConvexHullAlgorithm
    {
        #region Starting functions and constructor


        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHullAlgorithm" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="lift">if set to <c>true</c> [lift].</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance.</param>
        /// <exception cref="System.InvalidOperationException">Dimension of the input must be 2 or greater.</exception>
        /// <exception cref="System.ArgumentException">There are too few vertices (m) for the n-dimensional space. (m must be greater  +
        /// than the n, but m is  + NumberOfVertices +  and n is  + NumOfDimensions</exception>
        /// <exception cref="InvalidOperationException">PointTranslationGenerator cannot be null if PointTranslationType is enabled.
        /// or
        /// Dimension of the input must be 2 or greater.</exception>
        /// <exception cref="ArgumentException">There are too few vertices (m) for the n-dimensional space. (m must be greater " +
        /// "than the n, but m is " + NumberOfVertices + " and n is " + Dimension</exception>
        internal ConvexHullAlgorithm(IVertex[] vertices, bool lift, double PlaneDistanceTolerance)
        {
            IsLifted = lift;
            Vertices = vertices;
            NumberOfVertices = vertices.Length;

            NumOfDimensions = DetermineDimension();
            if (IsLifted) NumOfDimensions++;
            if (NumOfDimensions < 2) throw new ConvexHullGenerationException(ConvexHullCreationResultOutcome.DimensionSmallerTwo, "Dimension of the input must be 2 or greater.");
            if (NumOfDimensions== 2) throw new ConvexHullGenerationException(ConvexHullCreationResultOutcome.DimensionTwoWrongMethod, "Dimension of the input is 2. Thus you should use the Create2D method" +
                " instead of the Create.");
            if (NumberOfVertices <= NumOfDimensions)
                throw new ConvexHullGenerationException(ConvexHullCreationResultOutcome.NotEnoughVerticesForDimension,
                    "There are too few vertices (m) for the n-dimensional space. (m must be greater " +
                    "than the n, but m is " + NumberOfVertices + " and n is " + NumOfDimensions);
            this.PlaneDistanceTolerance = PlaneDistanceTolerance;
            UnprocessedFaces = new FaceList();
            ConvexFaces = new IndexBuffer();

            FacePool = new ConvexFaceInternal[(NumOfDimensions + 1) * 10]; // must be initialized before object manager
            AffectedFaceFlags = new bool[(NumOfDimensions + 1) * 10];
            ObjectManager = new ObjectManager(this);

            Center = new double[NumOfDimensions];
            TraverseStack = new IndexBuffer();
            UpdateBuffer = new int[NumOfDimensions];
            UpdateIndices = new int[NumOfDimensions];
            EmptyBuffer = new IndexBuffer();
            AffectedFaceBuffer = new IndexBuffer();
            ConeFaceBuffer = new SimpleList<DeferredFace>();
            SingularVertices = new HashSet<int>();
            BeyondBuffer = new IndexBuffer();

            ConnectorTable = new ConnectorList[Constants.ConnectorTableSize];
            for (var i = 0; i < Constants.ConnectorTableSize; i++) ConnectorTable[i] = new ConnectorList();

            VertexVisited = new bool[NumberOfVertices];
            Positions = new double[NumberOfVertices * NumOfDimensions];
            boundingBoxPoints = new List<int>[NumOfDimensions];
            minima = new double[NumOfDimensions];
            maxima = new double[NumOfDimensions];
            mathHelper = new MathHelper(NumOfDimensions, Positions);
        }
        /// <summary>
        /// Check the dimensionality of the input data.
        /// </summary>
        /// <returns>System.Int32.</returns>
        /// <exception cref="ArgumentException">Invalid input data (non-uniform dimension).</exception>
        private int DetermineDimension()
        {
            var r = new Random();
            var dimensions = new List<int>();
            for (var i = 0; i < 10; i++)
                dimensions.Add(Vertices[r.Next(NumberOfVertices)].Position.Length);
            var dimension = dimensions.Min();
            if (dimension != dimensions.Max())
                throw new ConvexHullGenerationException(ConvexHullCreationResultOutcome.NonUniformDimension, "Invalid input data (non-uniform dimension).");
            return dimension;
        }


        /// <summary>
        /// Gets/calculates the convex hull. This is 
        /// </summary>
        internal void GetConvexHull()
        {
            // accessing a 1D array is quicker than a jagged array, so the first step is to make this array
            SerializeVerticesToPositions();
            // next the bounding box extremes are found. This is used to shift, scale and find the starting simplex.
            FindBoundingBoxPoints();
            // the positions are shifted to avoid divide by zero problems
            // and if Delaunay or Voronoi, then the parabola terms are scaled back to match the size of the other coords
            ShiftAndScalePositions();
            // Find the (dimension+1) initial points and create the simplexes.
            CreateInitialSimplex();

            // Now, the main loop. These initial faces of a simplex are replaced and expanded 
            // outwards to make the convex hull and faces.
            while (UnprocessedFaces.First != null)
            {
                var currentFace = UnprocessedFaces.First;
                CurrentVertex = currentFace.FurthestVertex;

                UpdateCenter();

                // The affected faces get tagged
                TagAffectedFaces(currentFace);

                // Create the cone from the currentVertex and the affected faces horizon.
                if (!SingularVertices.Contains(CurrentVertex) && CreateCone()) CommitCone();
                else HandleSingular();

                // Need to reset the tags
                var count = AffectedFaceBuffer.Count;
                for (var i = 0; i < count; i++) AffectedFaceFlags[AffectedFaceBuffer[i]] = false;
            }
        }

        #endregion

        /// <summary>
        /// Serializes the vertices into the 1D array, Positions. The 1D array has much quicker access in C#.
        /// </summary>
        private void SerializeVerticesToPositions()
        {
            var index = 0;
            if (IsLifted) // "Lifted" means that the last dimension is the sum of the squares of the others.
            {
                foreach (var v in Vertices)
                {
                    var parabolaTerm = 0.0; // the lifted term is a sum of squares.
                    var origNumDim = NumOfDimensions - 1;
                    for (var i = 0; i < origNumDim; i++)
                    {
                        var coordinate = v.Position[i];
                        Positions[index++] = coordinate;
                        parabolaTerm += coordinate * coordinate;
                    }
                    Positions[index++] = parabolaTerm;
                }
            }
            else
                foreach (var v in Vertices)
                {
                    for (var i = 0; i < NumOfDimensions; i++)
                        Positions[index++] = v.Position[i];
                }
        }



        /// <summary>
        /// Finds the bounding box points.
        /// </summary>
        private void FindBoundingBoxPoints()
        {
            indexOfDimensionWithLeastExtremes = -1;
            var minNumExtremes = int.MaxValue;
            for (var i = 0; i < NumOfDimensions; i++)
            {
                var minIndices = new List<int>();
                var maxIndices = new List<int>();
                double min = double.PositiveInfinity, max = double.NegativeInfinity;
                for (var j = 0; j < NumberOfVertices; j++)
                {
                    var v = GetCoordinate(j, i);
                    var difference = min - v;
                    if (difference >= PlaneDistanceTolerance)
                    {
                        // you found a better solution than before, clear out the list and store new value
                        min = v;
                        minIndices.Clear();
                        minIndices.Add(j);
                    }
                    else if (difference > 0)
                    {
                        // you found a solution slightly better than before, clear out those that are no longer on the list and store new value
                        min = v;
                        minIndices.RemoveAll(index => min - GetCoordinate(index, i) > PlaneDistanceTolerance);
                        minIndices.Add(j);
                    }
                    else if (difference > -PlaneDistanceTolerance)
                    {
                        //same or almost as good as current limit, so store it
                        minIndices.Add(j);
                    }
                    difference = v - max;
                    if (difference >= PlaneDistanceTolerance)
                    {
                        // you found a better solution than before, clear out the list and store new value
                        max = v;
                        maxIndices.Clear();
                        maxIndices.Add(j);
                    }
                    else if (difference > 0)
                    {
                        // you found a solution slightly better than before, clear out those that are no longer on the list and store new value
                        max = v;
                        maxIndices.RemoveAll(index => min - GetCoordinate(index, i) > PlaneDistanceTolerance);
                        maxIndices.Add(j);
                    }
                    else if (difference > -PlaneDistanceTolerance)
                    {
                        //same or almost as good as current limit, so store it
                        maxIndices.Add(j);
                    }
                }
                minima[i] = min;
                maxima[i] = max;
                minIndices.AddRange(maxIndices);
                if (minIndices.Count < minNumExtremes)
                {
                    minNumExtremes = minIndices.Count;
                    indexOfDimensionWithLeastExtremes = i;
                }
                boundingBoxPoints[i] = minIndices;
            }
        }

        /// <summary>
        /// Shifts and scales the Positions to avoid future errors. This does not alter the original data.
        /// </summary>
        private void ShiftAndScalePositions()
        {
            var positionsLength = Positions.Length;
            if (IsLifted)
            {
                var origNumDim = NumOfDimensions - 1;
                var parabolaScale = 2 / (minima.Sum(x => Math.Abs(x)) + maxima.Sum(x => Math.Abs(x))
                    - Math.Abs(maxima[origNumDim]) - Math.Abs(minima[origNumDim]));
                // the parabola scale is 1 / average of the sum of the other dimensions.
                // multiplying this by the parabola will scale it back to be on near similar size to the
                // other dimensions. Without this, the term is much larger than the others, which causes
                // problems for roundoff error and finding the normal of faces.
                minima[origNumDim] *= parabolaScale; // change the extreme values as well
                maxima[origNumDim] *= parabolaScale;
                // it is done here because
                for (int i = origNumDim; i < positionsLength; i += NumOfDimensions)
                    Positions[i] *= parabolaScale;
            }
            var shiftAmount = new double[NumOfDimensions];
            for (int i = 0; i < NumOfDimensions; i++)
                // now the entire model is shifted to all positive numbers...plus some more.
                // why? 
                // 1) to avoid dealing with a point at the origin {0,0,...,0} which causes problems 
                //    for future normal finding
                // 2) note that weird shift that is used (max - min - min). This is to avoid scaling
                //    issues. this shift means that the minima in a dimension will always be a positive
                //    number (no points at zero), and the minima [in a given dimension] will always be
                //    half of the maxima. 'Half' is much preferred to 'thousands of times'
                //    Think of the first term as the range (max - min), then the second term avoids cases
                //    where there are both positive and negative numbers.
                if (maxima[i] == minima[i]) shiftAmount[i] = 0.0;
                else shiftAmount[i] = (maxima[i] - minima[i]) - minima[i];
            for (int i = 0; i < positionsLength; i++)
                Positions[i] += shiftAmount[i % NumOfDimensions];
        }

        /// <summary>
        /// Find the (dimension+1) initial points and create the simplexes.
        /// Creates the initial simplex of n+1 vertices by using points from the bounding box.
        /// Special care is taken to ensure that the vertices chosen do not result in a degenerate shape
        /// where vertices are collinear (co-planar, etc). This would technically be resolved when additional
        /// vertices are checked in the main loop, but: 1) a degenerate simplex would not eliminate any other
        /// vertices (thus no savings there), 2) the creation of the face normal is prone to error.
        /// </summary>
        private void CreateInitialSimplex()
        {
            var initialPoints = FindInitialPoints();
            #region Create the first faces from (dimension + 1) vertices.

            var faces = new int[NumOfDimensions + 1];

            for (var i = 0; i < NumOfDimensions + 1; i++)
            {
                var vertices = new int[NumOfDimensions];
                for (int j = 0, k = 0; j <= NumOfDimensions; j++)
                {
                    if (i != j) vertices[k++] = initialPoints[j];
                }
                var newFace = FacePool[ObjectManager.GetFace()];
                newFace.Vertices = vertices;
                Array.Sort(vertices);
                mathHelper.CalculateFacePlane(newFace, Center);
                faces[i] = newFace.Index;
            }
            // update the adjacency (check all pairs of faces)
            for (var i = 0; i < NumOfDimensions; i++)
                for (var j = i + 1; j < NumOfDimensions + 1; j++) UpdateAdjacency(FacePool[faces[i]], FacePool[faces[j]]);

            #endregion

            #region Init the vertex beyond buffers.

            foreach (var faceIndex in faces)
            {
                var face = FacePool[faceIndex];
                FindBeyondVertices(face);
                if (face.VerticesBeyond.Count == 0) ConvexFaces.Add(face.Index); // The face is on the hull
                else UnprocessedFaces.Add(face);
            }

            #endregion

            // Set all vertices to false (unvisited).
            foreach (var vertex in initialPoints) VertexVisited[vertex] = false;
        }

        /// <summary>
        /// Finds (dimension + 1) initial points.
        /// </summary>
        /// <returns>List&lt;System.Int32&gt;.</returns>
        /// <exception cref="System.ArgumentException">The input data is degenerate. It appears to exist in " + NumOfDimensions +
        ///                     " dimensions, but it is a " + (NumOfDimensions - 1) + " dimensional set (i.e. the point of collinear,"
        ///                     + " coplanar, or co-hyperplanar.)</exception>
        private List<int> FindInitialPoints()
        {
            var bigNumber = maxima.Sum() * NumOfDimensions * NumberOfVertices;
            // the first two points are taken from the dimension that had the fewest extremes
            // well, in most cases there will only be 2 in all dimensions: one min and one max
            // but a lot of engineering part shapes are nice and square and can have hundreds of 
            // parallel vertices at the extremes
            var vertex1 = boundingBoxPoints[indexOfDimensionWithLeastExtremes].First(); // these are min and max vertices along
            var vertex2 = boundingBoxPoints[indexOfDimensionWithLeastExtremes].Last(); // the dimension that had the fewest points
            boundingBoxPoints[indexOfDimensionWithLeastExtremes].RemoveAt(0);
            boundingBoxPoints[indexOfDimensionWithLeastExtremes].RemoveAt(boundingBoxPoints[indexOfDimensionWithLeastExtremes].Count - 1);
            var initialPoints = new List<int> { vertex1, vertex2 };
            VertexVisited[vertex1] = VertexVisited[vertex2] = true;
            CurrentVertex = vertex1; UpdateCenter();
            CurrentVertex = vertex2; UpdateCenter();
            var edgeVectors = new double[NumOfDimensions][];
            edgeVectors[0] = mathHelper.VectorBetweenVertices(vertex2, vertex1);
            // now the remaining vertices are just combined in one big list
            var extremes = boundingBoxPoints.SelectMany(x => x).ToList();
            // otherwise find the remaining points by maximizing the initial simplex volume
            var index = 1;
            while (index < NumOfDimensions && extremes.Any())
            {
                var bestVertex = -1;
                var bestEdgeVector = new double[] { };
                var maxVolume = Constants.DefaultPlaneDistanceTolerance;
                for (var i = extremes.Count - 1; i >= 0; i--)
                {
                    // count backwards in order to remove potential duplicates
                    var vIndex = extremes[i];
                    if (initialPoints.Contains(vIndex)) extremes.RemoveAt(i);
                    else
                    {
                        edgeVectors[index] = mathHelper.VectorBetweenVertices(vIndex, vertex1);
                        var volume = mathHelper.GetSimplexVolume(edgeVectors, index, bigNumber);
                        if (maxVolume < volume)
                        {
                            maxVolume = volume;
                            bestVertex = vIndex;
                            bestEdgeVector = edgeVectors[index];
                        }
                    }
                }
                extremes.Remove(bestVertex);
                if (bestVertex == -1) break;
                initialPoints.Add(bestVertex);
                edgeVectors[index++] = bestEdgeVector;
                CurrentVertex = bestVertex; UpdateCenter();
            }
            // hmm, there are not enough points on the bounding box to make a simplex. It is rare but entirely possibly.
            // As an extreme, the bounding box can be made in n dimensions from only 2 unique points. When we can't find
            // enough unique points, we start again with ALL the vertices. The following is a near replica of the code 
            // above, but instead of extremes, we consider "allVertices".
            if (initialPoints.Count <= NumOfDimensions && !IsLifted)
            {
                var allVertices = Enumerable.Range(0, NumberOfVertices).ToList();
                while (index < NumOfDimensions && allVertices.Any())
                {
                    var bestVertex = -1;
                    var bestEdgeVector = new double[] { };
                    var maxVolume = 0.0;
                    for (var i = allVertices.Count - 1; i >= 0; i--)
                    {
                        // count backwards in order to remove potential duplicates
                        var vIndex = allVertices[i];
                        if (initialPoints.Contains(vIndex)) allVertices.RemoveAt(i);
                        else
                        {
                            edgeVectors[index] = mathHelper.VectorBetweenVertices(vIndex, vertex1);
                            var volume = mathHelper.GetSimplexVolume(edgeVectors, index, bigNumber);
                            if (maxVolume < volume)
                            {
                                maxVolume = volume;
                                bestVertex = vIndex;
                                bestEdgeVector = edgeVectors[index];
                            }
                        }
                    }
                    allVertices.Remove(bestVertex);
                    if (bestVertex == -1) break;
                    initialPoints.Add(bestVertex);
                    edgeVectors[index++] = bestEdgeVector;
                    CurrentVertex = bestVertex; UpdateCenter();
                }
            }
            if (initialPoints.Count <= NumOfDimensions && IsLifted)
            {
                var allVertices = Enumerable.Range(0, NumberOfVertices).ToList();
                while (index < NumOfDimensions && allVertices.Any())
                {
                    var bestVertex = -1;
                    var bestEdgeVector = new double[] { };
                    var maxVolume = 0.0;
                    for (var i = allVertices.Count - 1; i >= 0; i--)
                    {
                        // count backwards in order to remove potential duplicates
                        var vIndex = allVertices[i];
                        if (initialPoints.Contains(vIndex)) allVertices.RemoveAt(i);
                        else
                        {
                            mathHelper.RandomOffsetToLift(vIndex, maxima.Last() - minima.Last());
                            edgeVectors[index] = mathHelper.VectorBetweenVertices(vIndex, vertex1);
                            var volume = mathHelper.GetSimplexVolume(edgeVectors, index, bigNumber);
                            if (maxVolume < volume)
                            {
                                maxVolume = volume;
                                bestVertex = vIndex;
                                bestEdgeVector = edgeVectors[index];
                            }
                        }
                    }
                    allVertices.Remove(bestVertex);
                    if (bestVertex == -1) break;
                    initialPoints.Add(bestVertex);
                    edgeVectors[index++] = bestEdgeVector;
                    CurrentVertex = bestVertex; UpdateCenter();
                }
            }
            if (initialPoints.Count <= NumOfDimensions && IsLifted)
                throw new ConvexHullGenerationException(ConvexHullCreationResultOutcome.DegenerateData, "The input data is degenerate. It appears to exist in " + NumOfDimensions +
                    " dimensions, but it is a " + (NumOfDimensions - 1) + " dimensional set (i.e. the point of collinear,"
                    + " coplanar, or co-hyperplanar.)");
            return initialPoints;
        }

        /// <summary>
        /// Check if 2 faces are adjacent and if so, update their AdjacentFaces array.
        /// </summary>
        /// <param name="l">The l.</param>
        /// <param name="r">The r.</param>
        private void UpdateAdjacency(ConvexFaceInternal l, ConvexFaceInternal r)
        {
            var lv = l.Vertices;
            var rv = r.Vertices;
            int i;

            // reset marks on the 1st face
            for (i = 0; i < lv.Length; i++) VertexVisited[lv[i]] = false;

            // mark all vertices on the 2nd face
            for (i = 0; i < rv.Length; i++) VertexVisited[rv[i]] = true;

            // find the 1st false index
            for (i = 0; i < lv.Length; i++) if (!VertexVisited[lv[i]]) break;

            // no vertex was marked
            if (i == NumOfDimensions) return;

            // check if only 1 vertex wasn't marked
            for (var j = i + 1; j < lv.Length; j++) if (!VertexVisited[lv[j]]) return;

            // if we are here, the two faces share an edge
            l.AdjacentFaces[i] = r.Index;

            // update the adj. face on the other face - find the vertex that remains marked
            for (i = 0; i < lv.Length; i++) VertexVisited[lv[i]] = false;
            for (i = 0; i < rv.Length; i++)
            {
                if (VertexVisited[rv[i]]) break;
            }
            r.AdjacentFaces[i] = l.Index;
        }

        /// <summary>
        /// Used in the "initialization" code.
        /// </summary>
        /// <param name="face">The face.</param>
        private void FindBeyondVertices(ConvexFaceInternal face)
        {
            var beyondVertices = face.VerticesBeyond;
            MaxDistance = double.NegativeInfinity;
            FurthestVertex = 0;
            for (var i = 0; i < NumberOfVertices; i++)
            {
                if (VertexVisited[i]) continue;
                IsBeyond(face, beyondVertices, i);
            }

            face.FurthestVertex = FurthestVertex;
        }
        #region Fields

        /// <summary>
        /// Corresponds to the dimension of the data.
        /// When the "lifted" hull is computed, Dimension is automatically incremented by one.
        /// </summary>
        internal readonly int NumOfDimensions;

        /// <summary>
        /// Are we on a paraboloid?
        /// </summary>
        private readonly bool IsLifted;

        /// <summary>
        /// Explained in ConvexHullComputationConfig.
        /// </summary>
        private readonly double PlaneDistanceTolerance;

        /*
         * Representation of the input vertices.
         * 
         * - In the algorithm, a vertex is represented by its index in the Vertices array.
         *   This makes the algorithm a lot faster (up to 30%) than using object reference everywhere.
         * - Positions are stored as a single array of values. Coordinates for vertex with index i
         *   are stored at indices <i * Dimension, (i + 1) * Dimension)
         * - VertexMarks are used by the algorithm to help identify a set of vertices that is "above" (or "beyond") 
         *   a specific face.
         */
        /// <summary>
        /// The vertices
        /// </summary>
        private readonly IVertex[] Vertices;
        /// <summary>
        /// The positions
        /// </summary>
        private double[] Positions;
        /// <summary>
        /// The vertex marks
        /// </summary>
        private readonly bool[] VertexVisited;

        private readonly int NumberOfVertices;

        /*
         * The triangulation faces are represented in a single pool for objects that are being reused.
         * This allows for represent the faces as integers and significantly speeds up many computations.
         * - AffectedFaceFlags are used to mark affected faces/
         */
        /// <summary>
        /// The face pool
        /// </summary>
        internal ConvexFaceInternal[] FacePool;
        /// <summary>
        /// The affected face flags
        /// </summary>
        internal bool[] AffectedFaceFlags;

        /// <summary>
        /// Used to track the size of the current hull in the Update/RollbackCenter functions.
        /// </summary>
        private int ConvexHullSize;

        /// <summary>
        /// A list of faces that that are not a part of the final convex hull and still need to be processed.
        /// </summary>
        private readonly FaceList UnprocessedFaces;

        /// <summary>
        /// A list of faces that form the convex hull.
        /// </summary>
        private readonly IndexBuffer ConvexFaces;

        /// <summary>
        /// The vertex that is currently being processed.
        /// </summary>
        private int CurrentVertex;

        /// <summary>
        /// A helper variable to determine the furthest vertex for a particular convex face.
        /// </summary>
        private double MaxDistance;

        /// <summary>
        /// A helper variable to help determine the index of the vertex that is furthest from the face that is currently being
        /// processed.
        /// </summary>
        private int FurthestVertex;

        /// <summary>
        /// The centroid of the currently computed hull.
        /// </summary>
        private readonly double[] Center;

        /*
         * Helper arrays to store faces for adjacency update.
         * This is just to prevent unnecessary allocations.
         */
        /// <summary>
        /// The update buffer
        /// </summary>
        private readonly int[] UpdateBuffer;
        /// <summary>
        /// The update indices
        /// </summary>
        private readonly int[] UpdateIndices;

        /// <summary>
        /// Used to determine which faces need to be updated at each step of the algorithm.
        /// </summary>
        private readonly IndexBuffer TraverseStack;

        /// <summary>
        /// Used for VerticesBeyond for faces that are on the convex hull.
        /// </summary>
        private readonly IndexBuffer EmptyBuffer;

        /// <summary>
        /// Used to determine which vertices are "above" (or "beyond") a face
        /// </summary>
        private IndexBuffer BeyondBuffer;

        /// <summary>
        /// Stores faces that are visible from the current vertex.
        /// </summary>
        private readonly IndexBuffer AffectedFaceBuffer;

        /// <summary>
        /// Stores faces that form a "cone" created by adding new vertex.
        /// </summary>
        private readonly SimpleList<DeferredFace> ConeFaceBuffer;

        /// <summary>
        /// Stores a list of "singular" (or "generate", "planar", etc.) vertices that cannot be part of the hull.
        /// </summary>
        private readonly HashSet<int> SingularVertices;

        /// <summary>
        /// The connector table helps to determine the adjacency of convex faces.
        /// Hashing is used instead of pairwise comparison. This significantly speeds up the computations,
        /// especially for higher dimensions.
        /// </summary>
        private readonly ConnectorList[] ConnectorTable;

        /// <summary>
        /// Manages the memory allocations and storage of unused objects.
        /// Saves the garbage collector a lot of work.
        /// </summary>
        private readonly ObjectManager ObjectManager;

        /// <summary>
        /// Helper class for handling math related stuff.
        /// </summary>
        private readonly MathHelper mathHelper;
        private readonly List<int>[] boundingBoxPoints;
        private int indexOfDimensionWithLeastExtremes;
        private readonly double[] minima;
        private readonly double[] maxima;
        #endregion
    }
}