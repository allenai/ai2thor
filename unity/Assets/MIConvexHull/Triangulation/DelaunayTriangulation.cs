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
    /// <summary>
    /// Calculation and representation of Delaunay triangulation.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TCell">The type of the t cell.</typeparam>
    /// <seealso cref="MIConvexHull.ITriangulation{TVertex, TCell}" />
    public class DelaunayTriangulation<TVertex, TCell> : ITriangulation<TVertex, TCell>
        where TCell : TriangulationCell<TVertex, TCell>, new()
        where TVertex : IVertex
    {
        /// <summary>
        /// Can only be created using a factory method.
        /// </summary>
        private DelaunayTriangulation()
        {
        }

        /// <summary>
        /// Cells of the triangulation.
        /// </summary>
        /// <value>The cells.</value>
        public IEnumerable<TCell> Cells { get; private set; }

        /// <summary>
        /// Creates the Delaunay triangulation of the input data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance (default is 1e-10). If too high, points 
        /// will be missed. If too low, the algorithm may break. Only adjust if you notice problems.</param>
        /// <returns>DelaunayTriangulation&lt;TVertex, TCell&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        /// <exception cref="ArgumentNullException">data</exception>
        public static DelaunayTriangulation<TVertex, TCell> Create(IList<TVertex> data,
            double PlaneDistanceTolerance)// = Constants.DefaultPlaneDistanceTolerance)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Count == 0) return new DelaunayTriangulation<TVertex, TCell> { Cells = new TCell[0] };

            var cells = ConvexHullAlgorithm.GetDelaunayTriangulation<TVertex, TCell>(data, PlaneDistanceTolerance);

            return new DelaunayTriangulation<TVertex, TCell> { Cells = cells };
        }
    }
}