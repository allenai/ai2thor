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
    /// A convex face representation containing adjacency information.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TFace">The type of the t face.</typeparam>
    public abstract class ConvexFace<TVertex, TFace>
        where TVertex : IVertex
        where TFace : ConvexFace<TVertex, TFace>
    {
        /// <summary>
        /// Adjacency. Array of length "dimension".
        /// If F = Adjacency[i] then the vertices shared with F are Vertices[j] where j != i.
        /// In the context of triangulation, can be null (indicates the cell is at boundary).
        /// </summary>
        /// <value>The adjacency.</value>
        public TFace[] Adjacency { get; set; }

        /// <summary>
        /// The vertices stored in clockwise order for dimensions 2 - 4, in higher dimensions the order is arbitrary.
        /// Unless I accidentally switch some index somewhere in which case the order is CCW. Either way, it is consistent.
        /// 3D Normal = (V[1] - V[0]) x (V[2] - V[1]).
        /// </summary>
        /// <value>The vertices.</value>
        public TVertex[] Vertices { get; set; }

        /// <summary>
        /// The normal vector of the face. Null if used in triangulation.
        /// </summary>
        /// <value>The normal.</value>
        public double[] Normal { get; set; }
    }

    /// <summary>
    /// A default convex face representation.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    public class DefaultConvexFace<TVertex> : ConvexFace<TVertex, DefaultConvexFace<TVertex>>
        where TVertex : IVertex
    {
    }
}