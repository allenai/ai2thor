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
    /// Representation of the triangulation cell. Pretty much the same as ConvexFace,
    /// just wanted to distinguish the two.
    /// To declare your own face type, use class Face : DelaunayFace(of Vertex, of Face)
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TCell">The type of the t cell.</typeparam>
    /// <seealso cref="MIConvexHull.ConvexFace{TVertex, TCell}" />
    public abstract class TriangulationCell<TVertex, TCell> : ConvexFace<TVertex, TCell>
        where TVertex : IVertex
        where TCell : ConvexFace<TVertex, TCell>
    {
    }

    /// <summary>
    /// Default triangulation cell.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    public class DefaultTriangulationCell<TVertex> : TriangulationCell<TVertex, DefaultTriangulationCell<TVertex>>
        where TVertex : IVertex
    {
    }
}