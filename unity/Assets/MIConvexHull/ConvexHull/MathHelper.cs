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
    /// A helper class mostly for normal computation. If convex hulls are computed
    /// in higher dimensions, it might be a good idea to add a specific
    /// FindNormalVectorND function.
    /// </summary>
    internal class MathHelper
    {
        /// <summary>
        /// The dimension
        /// </summary>
        private readonly int Dimension;
        /// <summary>
        /// The matrix pivots
        /// </summary>
        private readonly int[] matrixPivots;
        /// <summary>
        /// The n d matrix
        /// </summary>
        private readonly double[] nDMatrix;
        /// <summary>
        /// The n d normal helper vector
        /// </summary>
        private readonly double[] nDNormalHelperVector;

        /// <summary>
        /// The nt x
        /// </summary>
        private readonly double[] ntX;
        /// <summary>
        /// The nt y
        /// </summary>
        private readonly double[] ntY;
        /// <summary>
        /// The nt z
        /// </summary>
        private readonly double[] ntZ;

        /// <summary>
        /// The position data
        /// </summary>
        private readonly double[] PositionData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MathHelper"/> class.
        /// </summary>
        /// <param name="dimension">The dimension.</param>
        /// <param name="positions">The positions.</param>
        internal MathHelper(int dimension, double[] positions)
        {
            PositionData = positions;
            Dimension = dimension;

            ntX = new double[Dimension];
            ntY = new double[Dimension];
            ntZ = new double[Dimension];

            nDNormalHelperVector = new double[Dimension];
            nDMatrix = new double[Dimension * Dimension];
            matrixPivots = new int[Dimension];
        }

        /// <summary>
        /// Calculates the normal and offset of the hyper-plane given by the face's vertices.
        /// </summary>
        /// <param name="face">The face.</param>
        /// <param name="center">The center.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool CalculateFacePlane(ConvexFaceInternal face, double[] center)
        {
            var vertices = face.Vertices;
            var normal = face.Normal;
            FindNormalVector(vertices, normal);

            if (double.IsNaN(normal[0]))
            {
                return false;
            }

            var offset = 0.0;
            var centerDistance = 0.0;
            var fi = vertices[0] * Dimension;
            for (var i = 0; i < Dimension; i++)
            {
                var n = normal[i];
                offset += n * PositionData[fi + i];
                centerDistance += n * center[i];
            }
            face.Offset = -offset;
            centerDistance -= offset;

            if (centerDistance > 0)
            {
                for (var i = 0; i < Dimension; i++) normal[i] = -normal[i];
                face.Offset = offset;
                face.IsNormalFlipped = true;
            }
            else face.IsNormalFlipped = false;

            return true;
        }

        /// <summary>
        /// Check if the vertex is "visible" from the face.
        /// The vertex is "over face" if the return value is &gt; Constants.PlaneDistanceTolerance.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="f">The f.</param>
        /// <returns>The vertex is "over face" if the result is positive.</returns>
        internal double GetVertexDistance(int v, ConvexFaceInternal f)
        {
            var normal = f.Normal;
            var x = v * Dimension;
            var distance = f.Offset;
            for (var i = 0; i < normal.Length; i++) distance += normal[i] * PositionData[x + i];
            return distance;
        }

        /// <summary>
        /// Returns the vector the between vertices.
        /// </summary>
        /// <param name="toIndex">To index.</param>
        /// <param name="fromIndex">From index.</param>
        /// <returns>System.Double[].</returns>
        internal double[] VectorBetweenVertices(int toIndex, int fromIndex)
        {
            var target = new double[Dimension];
            VectorBetweenVertices(toIndex, fromIndex, target);
            return target;
        }
        /// <summary>
        /// Returns the vector the between vertices.
        /// </summary>
        /// <param name="fromIndex">From index.</param>
        /// <param name="toIndex">To index.</param>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        private void VectorBetweenVertices(int toIndex, int fromIndex, double[] target)
        {
            int u = toIndex * Dimension, v = fromIndex * Dimension;
            for (var i = 0; i < Dimension; i++)
            {
                target[i] = PositionData[u + i] - PositionData[v + i];
            }
        }

        internal void RandomOffsetToLift(int index, double maxHeight)
        {
            var random = new Random();
            var liftIndex = (index * Dimension) + Dimension - 1;
            PositionData[liftIndex] += 0.0001 * maxHeight * (random.NextDouble() - 0.5);
        }
        #region Find the normal vector of the face
        /// <summary>
        /// Finds normal vector of a hyper-plane given by vertices.
        /// Stores the results to normalData.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normalData">The normal data.</param>
        private void FindNormalVector(int[] vertices, double[] normalData)
        {
            switch (Dimension)
            {
                case 2:
                    FindNormalVector2D(vertices, normalData);
                    break;
                case 3:
                    FindNormalVector3D(vertices, normalData);
                    break;
                case 4:
                    FindNormalVector4D(vertices, normalData);
                    break;
                default:
                    FindNormalVectorND(vertices, normalData);
                    break;
            }
        }
        /// <summary>
        /// Finds 2D normal vector.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">The normal.</param>
        private void FindNormalVector2D(int[] vertices, double[] normal)
        {
            VectorBetweenVertices(vertices[1], vertices[0], ntX);

            var nx = -ntX[1];
            var ny = ntX[0];

            var norm = Math.Sqrt(nx * nx + ny * ny);

            var f = 1.0 / norm;
            normal[0] = f * nx;
            normal[1] = f * ny;
        }
        /// <summary>
        /// Finds 3D normal vector.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">The normal.</param>
        private void FindNormalVector3D(int[] vertices, double[] normal)
        {
            VectorBetweenVertices(vertices[1], vertices[0], ntX);
            VectorBetweenVertices(vertices[2], vertices[1], ntY);

            var nx = ntX[1] * ntY[2] - ntX[2] * ntY[1];
            var ny = ntX[2] * ntY[0] - ntX[0] * ntY[2];
            var nz = ntX[0] * ntY[1] - ntX[1] * ntY[0];

            var norm = Math.Sqrt(nx * nx + ny * ny + nz * nz);

            var f = 1.0 / norm;
            normal[0] = f * nx;
            normal[1] = f * ny;
            normal[2] = f * nz;
        }
        /// <summary>
        /// Finds 4D normal vector.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">The normal.</param>
        private void FindNormalVector4D(int[] vertices, double[] normal)
        {
            VectorBetweenVertices(vertices[1], vertices[0], ntX);
            VectorBetweenVertices(vertices[2], vertices[1], ntY);
            VectorBetweenVertices(vertices[3], vertices[2], ntZ);

            var x = ntX;
            var y = ntY;
            var z = ntZ;

            // This was generated using Mathematica
            var nx = x[3] * (y[2] * z[1] - y[1] * z[2])
                     + x[2] * (y[1] * z[3] - y[3] * z[1])
                     + x[1] * (y[3] * z[2] - y[2] * z[3]);
            var ny = x[3] * (y[0] * z[2] - y[2] * z[0])
                     + x[2] * (y[3] * z[0] - y[0] * z[3])
                     + x[0] * (y[2] * z[3] - y[3] * z[2]);
            var nz = x[3] * (y[1] * z[0] - y[0] * z[1])
                     + x[1] * (y[0] * z[3] - y[3] * z[0])
                     + x[0] * (y[3] * z[1] - y[1] * z[3]);
            var nw = x[2] * (y[0] * z[1] - y[1] * z[0])
                     + x[1] * (y[2] * z[0] - y[0] * z[2])
                     + x[0] * (y[1] * z[2] - y[2] * z[1]);

            var norm = Math.Sqrt(nx * nx + ny * ny + nz * nz + nw * nw);

            var f = 1.0 / norm;
            normal[0] = f * nx;
            normal[1] = f * ny;
            normal[2] = f * nz;
            normal[3] = f * nw;
        }

        /// <summary>
        /// Finds the normal vector nd.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">The normal.</param>
        private void FindNormalVectorND(int[] vertices, double[] normal)
        {
            /* We need to solve the matrix A n = B where
             *  - A contains coordinates of vertices as columns
             *  - B is vector with all 1's. Really, it should be the distance of 
             *      the plane from the origin, but - since we're not worried about that
             *      here and we will normalize the normal anyway - all 1's suffices.
             */
            var iPiv = matrixPivots;
            var data = nDMatrix;
            var norm = 0.0;

            // Solve determinants by replacing x-th column by all 1.
            for (var x = 0; x < Dimension; x++)
            {
                for (var i = 0; i < Dimension; i++)
                {
                    var offset = vertices[i] * Dimension;
                    for (var j = 0; j < Dimension; j++)
                    {
                        // maybe I got the i/j mixed up here regarding the representation Math.net uses...
                        // ...but it does not matter since Det(A) = Det(Transpose(A)).
                        data[Dimension * i + j] = j == x ? 1.0 : PositionData[offset + j];
                    }
                }
                LUFactor(data, Dimension, iPiv, nDNormalHelperVector);
                var coord = 1.0;
                for (var i = 0; i < Dimension; i++)
                {
                    if (iPiv[i] != i) coord *= -data[Dimension * i + i]; // the determinant sign changes on row swap.
                    else coord *= data[Dimension * i + i];
                }
                normal[x] = coord;
                norm += coord * coord;
            }

            // Normalize the result
            var f = 1.0 / Math.Sqrt(norm);
            for (var i = 0; i < normal.Length; i++) normal[i] *= f;
        }
        #endregion

        #region Simplex Volume
        /// <summary>
        /// Gets the simplex volume. Prior to having enough edge vectors, the method pads the remaining with all
        /// "other numbers". So, yes, this method is not really finding the volume. But a relative volume-like measure. It
        /// uses the magnitude of the determinant as the volume stand-in following the Cayley-Menger theorem.
        /// </summary>
        /// <param name="edgeVectors">The edge vectors.</param>
        /// <param name="lastIndex">The last index.</param>
        /// <param name="bigNumber">The big number.</param>
        /// <returns>System.Double.</returns>
        internal double GetSimplexVolume(double[][] edgeVectors, int lastIndex, double bigNumber)
        {
            var A = new double[Dimension * Dimension];
            var index = 0;
            for (int i = 0; i < Dimension; i++)
                for (int j = 0; j < Dimension; j++)
                    if (i <= lastIndex)
                        A[index++] = edgeVectors[i][j];
                    else A[index] = (Math.Pow(-1, index) * index++) / bigNumber;
            // this last term is used for all the vertices in the comparison for the yet determined vertices
            // the idea is to come up with sets of numbers that are orthogonal so that an non-zero value will result
            // and to choose smallish numbers since the choice of vectors will affect what the end volume is.
            // A better way (todo?) is to solve a smaller matrix. However, cases were found in which the obvious smaller vector
            // (the upper left) had too many zeros. So, one would need to find the right subset. Indeed choosing a subset
            // biases the first dimensions of the others. Perhaps a larger volume would be created from a different vertex
            // if another subset of dimensions were used. 
            return Math.Abs(DeterminantDestructive(A));
        }

        /// <summary>
        /// Determinants the destructive.
        /// </summary>
        /// <param name="A">a.</param>
        /// <returns>System.Double.</returns>
        private double DeterminantDestructive(double[] A)
        {
            switch (Dimension)
            {
                case 0:
                    return 0.0;
                case 1:
                    return A[0];
                case 2:
                    return A[0] * A[3] - A[1] * A[2];
                case 3:
                    return A[0] * A[4] * A[8] + A[1] * A[5] * A[6] + A[2] * A[3] * A[7]
                           - A[0] * A[5] * A[7] - A[1] * A[3] * A[8] - A[2] * A[4] * A[6];
                default:
                    {
                        var iPiv = new int[Dimension];
                        var helper = new double[Dimension];
                        LUFactor(A, Dimension, iPiv, helper);
                        var det = 1.0;
                        for (var i = 0; i < iPiv.Length; i++)
                        {
                            det *= A[Dimension * i + i];
                            if (iPiv[i] != i) det *= -1; // the determinant sign changes on row swap.
                        }
                        return det;
                    }
            }
        }
        #endregion


        // Modified from Math.NET
        // Copyright (c) 2009-2013 Math.NET
        /// <summary>
        /// Lus the factor.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="order">The order.</param>
        /// <param name="ipiv">The ipiv.</param>
        /// <param name="vecLUcolj">The vec l ucolj.</param>
        private static void LUFactor(double[] data, int order, int[] ipiv, double[] vecLUcolj)
        {
            // Initialize the pivot matrix to the identity permutation.
            for (var i = 0; i < order; i++)
            {
                ipiv[i] = i;
            }

            // Outer loop.
            for (var j = 0; j < order; j++)
            {
                var indexj = j * order;
                var indexjj = indexj + j;

                // Make a copy of the j-th column to localize references.
                for (var i = 0; i < order; i++)
                {
                    vecLUcolj[i] = data[indexj + i];
                }

                // Apply previous transformations.
                for (var i = 0; i < order; i++)
                {
                    // Most of the time is spent in the following dot product.
                    var kmax = Math.Min(i, j);
                    var s = 0.0;
                    for (var k = 0; k < kmax; k++)
                    {
                        s += data[k * order + i] * vecLUcolj[k];
                    }

                    data[indexj + i] = vecLUcolj[i] -= s;
                }

                // Find pivot and exchange if necessary.
                var p = j;
                for (var i = j + 1; i < order; i++)
                {
                    if (Math.Abs(vecLUcolj[i]) > Math.Abs(vecLUcolj[p]))
                    {
                        p = i;
                    }
                }

                if (p != j)
                {
                    for (var k = 0; k < order; k++)
                    {
                        var indexk = k * order;
                        var indexkp = indexk + p;
                        var indexkj = indexk + j;
                        var temp = data[indexkp];
                        data[indexkp] = data[indexkj];
                        data[indexkj] = temp;
                    }

                    ipiv[j] = p;
                }

                // Compute multipliers.
                if (j < order & data[indexjj] != 0.0)
                {
                    for (var i = j + 1; i < order; i++)
                    {
                        data[indexj + i] /= data[indexjj];
                    }
                }
            }
        }
    }
}