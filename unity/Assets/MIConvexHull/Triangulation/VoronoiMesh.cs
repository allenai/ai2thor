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
    /// <summary>
    /// A factory class for creating a Voronoi mesh.
    /// </summary>
    public static class VoronoiMesh
    {
        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <typeparam name="TCell">The type of the t cell.</typeparam>
        /// <typeparam name="TEdge">The type of the t edge.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance.</param>
        /// <returns>VoronoiMesh&lt;TVertex, TCell, TEdge&gt;.</returns>
        public static VoronoiMesh<TVertex, TCell, TEdge> Create<TVertex, TCell, TEdge>(IList<TVertex> data,
            double PlaneDistanceTolerance = Constants.DefaultPlaneDistanceTolerance)
            where TCell : TriangulationCell<TVertex, TCell>, new()
            where TVertex : IVertex
            where TEdge : VoronoiEdge<TVertex, TCell>, new()
        {
            return VoronoiMesh<TVertex, TCell, TEdge>.Create(data, PlaneDistanceTolerance);
        }

        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance.</param>
        /// <returns>VoronoiMesh&lt;TVertex, DefaultTriangulationCell&lt;TVertex&gt;, VoronoiEdge&lt;TVertex, DefaultTriangulationCell&lt;TVertex&gt;&gt;&gt;.</returns>
        public static
            VoronoiMesh
                <TVertex, DefaultTriangulationCell<TVertex>, VoronoiEdge<TVertex, DefaultTriangulationCell<TVertex>>>
            Create<TVertex>(IList<TVertex> data,
                double PlaneDistanceTolerance = Constants.DefaultPlaneDistanceTolerance)
            where TVertex : IVertex
        {
            return
                VoronoiMesh
                    <TVertex, DefaultTriangulationCell<TVertex>, VoronoiEdge<TVertex, DefaultTriangulationCell<TVertex>>
                        >.Create(data, PlaneDistanceTolerance);
        }

        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance.</param>
        /// <returns>VoronoiMesh&lt;DefaultVertex, DefaultTriangulationCell&lt;DefaultVertex&gt;, VoronoiEdge&lt;DefaultVertex, DefaultTriangulationCell&lt;DefaultVertex&gt;&gt;&gt;.</returns>
        public static
            VoronoiMesh
                <DefaultVertex, DefaultTriangulationCell<DefaultVertex>,
                    VoronoiEdge<DefaultVertex, DefaultTriangulationCell<DefaultVertex>>>
            Create(IList<double[]> data,
                double PlaneDistanceTolerance = Constants.DefaultPlaneDistanceTolerance)
        {
            var points = data.Select(p => new DefaultVertex { Position = p.ToArray() }).ToList();
            return
                VoronoiMesh
                    <DefaultVertex, DefaultTriangulationCell<DefaultVertex>,
                        VoronoiEdge<DefaultVertex, DefaultTriangulationCell<DefaultVertex>>>.Create(points, PlaneDistanceTolerance);
        }

        /// <summary>
        /// Create the voronoi mesh.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <typeparam name="TCell">The type of the t cell.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance.</param>
        /// <returns>VoronoiMesh&lt;TVertex, TCell, VoronoiEdge&lt;TVertex, TCell&gt;&gt;.</returns>
        public static VoronoiMesh<TVertex, TCell, VoronoiEdge<TVertex, TCell>> Create<TVertex, TCell>(
            IList<TVertex> data,
            double PlaneDistanceTolerance = Constants.DefaultPlaneDistanceTolerance)
            where TVertex : IVertex
            where TCell : TriangulationCell<TVertex, TCell>, new()
        {
            return VoronoiMesh<TVertex, TCell, VoronoiEdge<TVertex, TCell>>.Create(data, PlaneDistanceTolerance);
        }
    }

    /// <summary>
    /// A representation of a voronoi mesh.
    /// </summary>
    /// <typeparam name="TEdge">The type of the t edge.</typeparam>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TCell">The type of the t cell.</typeparam>
    public class VoronoiMesh<TVertex, TCell, TEdge>
        where TCell : TriangulationCell<TVertex, TCell>, new()
        where TVertex : IVertex
        where TEdge : VoronoiEdge<TVertex, TCell>, new()
    {
        /// <summary>
        /// Can only be created using a factory method.
        /// </summary>
        private VoronoiMesh()
        {
        }

        /// <summary>
        /// Vertices of the diagram.
        /// </summary>
        /// <value>The vertices.</value>
        public IEnumerable<TCell> Vertices { get; private set; }

        /// <summary>
        /// Edges connecting the cells.
        /// The same information can be retrieved Cells' Adjacency.
        /// </summary>
        /// <value>The edges.</value>
        public IEnumerable<TEdge> Edges { get; private set; }

        /// <summary>
        /// Create a Voronoi diagram of the input data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="PlaneDistanceTolerance">The plane distance tolerance (default is 1e-10). If too high, points 
        /// will be missed. If too low, the algorithm may break. Only adjust if you notice problems.</param>
        /// <returns>VoronoiMesh&lt;TVertex, TCell, TEdge&gt;.</returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        /// <exception cref="ArgumentNullException">data</exception>
        public static VoronoiMesh<TVertex, TCell, TEdge> Create(IList<TVertex> data,
            double PlaneDistanceTolerance = Constants.DefaultPlaneDistanceTolerance)
        {
            if (data == null) throw new ArgumentNullException("data");

            var t = DelaunayTriangulation<TVertex, TCell>.Create(data, PlaneDistanceTolerance);
            var vertices = t.Cells.ToList();
            var edges = new HashSet<TEdge>(new EdgeComparer());

            foreach (var f in vertices)
            {
                for (var i = 0; i < f.Adjacency.Length; i++)
                {
                    var af = f.Adjacency[i];
                    if (af != null) edges.Add(new TEdge { Source = f, Target = af });
                }
            }

            return new VoronoiMesh<TVertex, TCell, TEdge>
            {
                Vertices = vertices,
                Edges = edges.ToList()
            };
        }

        /// <summary>
        /// This is probably not needed, but might make things a tiny bit faster.
        /// </summary>
        /// <seealso cref="System.Collections.Generic.IEqualityComparer{TEdge}" />
        private class EdgeComparer : IEqualityComparer<TEdge>
        {
            /// <summary>
            /// Equals the specified x.
            /// </summary>
            /// <param name="x">The x.</param>
            /// <param name="y">The y.</param>
            /// <returns>System.Boolean.</returns>
            public bool Equals(TEdge x, TEdge y)
            {
                return (x.Source == y.Source && x.Target == y.Target) || (x.Source == y.Target && x.Target == y.Source);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
            /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
            public int GetHashCode(TEdge obj)
            {
                return obj.Source.GetHashCode() ^ obj.Target.GetHashCode();
            }
        }
    }
}