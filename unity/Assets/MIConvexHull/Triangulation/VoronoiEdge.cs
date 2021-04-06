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

namespace MIConvexHull
{
    /// <summary>
    /// A class representing an (undirected) edge of the Voronoi graph.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TCell">The type of the t cell.</typeparam>
    public class VoronoiEdge<TVertex, TCell>
        where TVertex : IVertex
        where TCell : TriangulationCell<TVertex, TCell>
    {
        /// <summary>
        /// Create an instance of the edge.
        /// </summary>
        public VoronoiEdge()
        {
        }

        /// <summary>
        /// Create an instance of the edge.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public VoronoiEdge(TCell source, TCell target)
        {
            Source = source;
            Target = target;
        }

        /// <summary>
        /// Source of the edge.
        /// </summary>
        /// <value>The source.</value>
        public TCell Source { get; internal set; }

        /// <summary>
        /// Target of the edge.
        /// </summary>
        /// <value>The target.</value>
        public TCell Target { get; internal set; }

        /// <summary>
        /// ...
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as VoronoiEdge<TVertex, TCell>;
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return (Source == other.Source && Target == other.Target)
                   || (Source == other.Target && Target == other.Source);
        }

        /// <summary>
        /// ...
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            var hash = 23;
            hash = hash * 31 + Source.GetHashCode();
            return hash * 31 + Target.GetHashCode();
        }
    }
}