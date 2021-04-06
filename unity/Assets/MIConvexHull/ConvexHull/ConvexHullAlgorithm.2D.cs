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
using System.Runtime.CompilerServices;

namespace MIConvexHull
{
    internal static class ConvexHull2DAlgorithm
    {
        /// <summary>
        /// For 2D only: Returns the result in counter-clockwise order starting with the element with the lowest X value.
        /// If there are multiple vertices with the same minimum X, then the one with the lowest Y is chosen.
        /// </summary>
        /// <typeparam name="TVertex">The type of the vertex.</typeparam>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;TVertex&gt;.</returns>
        /// <exception cref="ArgumentException">Cannot define the 2D convex hull for less than two points.</exception>

        internal static List<TVertex> Create<TVertex>(IList<TVertex> points, double tolerance)
            where TVertex : IVertex2D, new()
        {
            // instead of calling points.Count several times, we create this variable. 
            // by the ways points is unaffected by this method
            var numPoints = points.Count;
            if (numPoints == 2) return points.ToList();
            if (numPoints < 2) throw new ArgumentException("Cannot define the 2D convex hull for less than two points.");
            #region Step 1 : Define Convex Octogon

            /* The first step is to quickly identify the three to eight vertices based on the
             * Akl-Toussaint heuristic. */
            var maxX = double.NegativeInfinity;
            var maxXIndex = -1;
            var maxY = double.NegativeInfinity;
            var maxYIndex = -1;
            var maxSum = double.NegativeInfinity;
            var maxSumIndex = -1;
            var maxDiff = double.NegativeInfinity;
            var maxDiffIndex = -1;
            var minX = double.PositiveInfinity;
            var minXIndex = -1;
            var minY = double.PositiveInfinity;
            var minYIndex = -1;
            var minSum = double.PositiveInfinity;
            var minSumIndex = -1;
            var minDiff = double.PositiveInfinity;
            var minDiffIndex = -1;
            // search of all points to find the extrema. What is stored here is the position (or index) within
            // points and the value
            for (var i = 0; i < numPoints; i++)
            {
                var p = points[i];
                var x = p.X;
                var y = p.Y;
                var sum = x + y;
                var diff = x - y;
                if (x < minX)
                {
                    minXIndex = i;
                    minX = x;
                }

                if (y < minY)
                {
                    minYIndex = i;
                    minY = y;
                }

                if (x > maxX)
                {
                    maxXIndex = i;
                    maxX = x;
                }

                if (y > maxY)
                {
                    maxYIndex = i;
                    maxY = y;
                }

                // so that's the Akl-Toussaint (to find extrema in x and y). here, we go a step 
                // further and check the sum and difference of x and y. instead of a initial convex
                // quadrilateral we have (potentially) a convex octagon. Because we are adding or substracting
                // there is a slight time penalty, but that seems to be made up in the next two parts where
                // having more sortedlists (with fewer elements each) is faster than fewer sortedlists (with more
                // elements). 
                if (sum < minSum)
                {
                    minSumIndex = i;
                    minSum = sum;
                }

                if (diff < minDiff)
                {
                    minDiffIndex = i;
                    minDiff = diff;
                }

                if (sum > maxSum)
                {
                    maxSumIndex = i;
                    maxSum = sum;
                }

                if (diff > maxDiff)
                {
                    maxDiffIndex = i;
                    maxDiff = diff;
                }
            }
            // what if all points are on a horizontal line? temporarily set to max and min Y to min X. This'll be fixed
            // in the function: FindIntermediatePointsForLongSkinny
            if (minY == maxY) minYIndex = maxYIndex = minXIndex;
            // what if all points are on a vertical line? then do the opposite
            if (minX == maxX) minXIndex = maxXIndex = minYIndex;
            //put these on a list in counter-clockwise (CCW) direction
            var extremeIndices = new List<int>(new[]
            {
                minXIndex, minSumIndex, minYIndex, maxDiffIndex,
                maxXIndex, maxSumIndex, maxYIndex, minDiffIndex
            });
            var cvxVNum = 8; //in some cases, we need to reduce from this eight to a smaller set
            // The next two loops handle this reduction from 8 to as few as 3.
            // In the first loop, simply check if any indices are repeated. Thanks to the CCW order,
            // any repeat indices are adjacent on the list. Start from the back of the loop and
            // remove towards zero.
            for (int i = cvxVNum - 1; i >= 0; i--)
            {
                var thisExtremeIndex = extremeIndices[i];
                var nextExtremeIndex = (i == cvxVNum - 1) ? extremeIndices[0] : extremeIndices[i + 1];
                if (thisExtremeIndex == nextExtremeIndex)
                {
                    cvxVNum--;
                    extremeIndices.RemoveAt(i);
                }
            }
            // before we check if points are on top of one another or have some round-off error issues, these
            // indices are stored and sorted numerically for use in the second half of part 2 where we go through
            // all the points a second time. 
            var indicesUsed = extremeIndices.OrderBy(x => x).ToArray();

            // create the list that is eventually returned by the function. Initially it will have the 3 to 8 extrema
            // (as is produced in the following loop).
            var convexHullCCW = new List<TVertex>();

            // on very rare occasions (long skinny diagonal set of points), there may only be two extrema.
            // in this case just add
            if (cvxVNum == 2)
            {
                convexHullCCW = FindIntermediatePointsForLongSkinny(points, numPoints, indicesUsed[0], indicesUsed[1],
                    out var newUsedIndices);
                if (!newUsedIndices.Any())
                    // looks like only two indices total! so all points are co-linear.
                    return new List<TVertex> { points[indicesUsed[0]], points[indicesUsed[1]] };
                newUsedIndices.Add(indicesUsed[0]);
                newUsedIndices.Add(indicesUsed[1]);
                indicesUsed = newUsedIndices.OrderBy(x => x).ToArray();
                cvxVNum = indicesUsed.Length;
            }
            else
                for (var i = cvxVNum - 1; i >= 0; i--)
                {
                    // in other rare cases, often due to some roundoff error, the extrema point will produce a concavity with its
                    // two neighbors. Here, we check that case. If it does make a concavity we don't use it in the initial convex
                    // hull (we have captured its index and will still skip it below. it will not be searched a second time).
                    // counting backwards again, we grab the previous and next point and check the "cross product" to see if the 
                    // vertex in convex. if it is we add it to the returned list. 
                    var currentPt = points[extremeIndices[i]];
                    var prevPt = points[(i == 0) ? extremeIndices[cvxVNum - 1] : extremeIndices[i - 1]];
                    var nextPt = points[(i == cvxVNum - 1) ? extremeIndices[0] : extremeIndices[i + 1]];
                    if ((nextPt.X - currentPt.X) * (prevPt.Y - currentPt.Y) +
                        (nextPt.Y - currentPt.Y) * (currentPt.X - prevPt.X) > tolerance)
                        convexHullCCW.Insert(0,
                            currentPt); //because we are counting backwards, we need to ensure that new points are added
                                        // to the front of the list
                    else
                    {
                        cvxVNum--;
                        extremeIndices.RemoveAt(i); //the only reason to do this is to ensure that - if the loop is to 
                                                    //continue - that the vectors are made to the proper new adjacent vertices
                    }
                }
            #endregion

            #region Step 2 : Create the sorted zig-zag line for each extrema edge

            /* Of the 3 to 8 vertices identified in the convex hull, ... */

            #region Set local variables for the points in the convex hull

            //This is used to limit the number of calls to convexHullCCW[] and point.X and point.Y, which 
            //can take a significant amount of time. 
            //Initialize the point locations and vectors:
            //At minimum, the convex hull must contain two points (e.g. consider three points in a near line,
            //the third point will be added later, since it was not an extreme.)
            var p0 = convexHullCCW[0];
            var p0X = p0.X;
            var p0Y = p0.Y;
            var p1 = convexHullCCW[1];
            var p1X = p1.X;
            var p1Y = p1.Y;
            double p2X = 0,
                p2Y = 0,
                p3X = 0,
                p3Y = 0,
                p4X = 0,
                p4Y = 0,
                p5X = 0,
                p5Y = 0,
                p6X = 0,
                p6Y = 0,
                p7X = 0,
                p7Y = 0;
            var v0X = p1X - p0X;
            var v0Y = p1Y - p0Y;
            double v1X,
                v1Y,
                v2X = 0,
                v2Y = 0,
                v3X = 0,
                v3Y = 0,
                v4X = 0,
                v4Y = 0,
                v5X = 0,
                v5Y = 0,
                v6X = 0,
                v6Y = 0,
                v7X = 0,
                v7Y = 0;
            //A big if statement to make sure the convex hull wraps properly, since the number of initial cvxHull points changes
            if (cvxVNum > 2)
            {
                var p2 = convexHullCCW[2];
                p2X = p2.X;
                p2Y = p2.Y;
                v1X = p2X - p1X;
                v1Y = p2Y - p1Y;
                if (cvxVNum > 3)
                {
                    var p3 = convexHullCCW[3];
                    p3X = p3.X;
                    p3Y = p3.Y;
                    v2X = p3X - p2X;
                    v2Y = p3Y - p2Y;
                    if (cvxVNum > 4)
                    {
                        var p4 = convexHullCCW[4];
                        p4X = p4.X;
                        p4Y = p4.Y;
                        v3X = p4X - p3X;
                        v3Y = p4Y - p3Y;
                        if (cvxVNum > 5)
                        {
                            var p5 = convexHullCCW[5];
                            p5X = p5.X;
                            p5Y = p5.Y;
                            v4X = p5X - p4X;
                            v4Y = p5Y - p4Y;
                            if (cvxVNum > 6)
                            {
                                var p6 = convexHullCCW[6];
                                p6X = p6.X;
                                p6Y = p6.Y;
                                v5X = p6X - p5X;
                                v5Y = p6Y - p5Y;
                                if (cvxVNum > 7)
                                {
                                    var p7 = convexHullCCW[7];
                                    p7X = p7.X;
                                    p7Y = p7.Y;
                                    v6X = p7X - p6X;
                                    v6Y = p7Y - p6Y;
                                    //Wrap around from 7
                                    v7X = p0X - p7X;
                                    v7Y = p0Y - p7Y;
                                }
                                else //Wrap around from 6
                                {
                                    v6X = p0X - p6X;
                                    v6Y = p0Y - p6Y;
                                }
                            }
                            else //Wrap around from 5
                            {
                                v5X = p0X - p5X;
                                v5Y = p0Y - p5Y;
                            }
                        }
                        else
                        {
                            //Wrap around from 4
                            v4X = p0X - p4X;
                            v4Y = p0Y - p4Y;
                        }
                    }
                    else
                    {
                        //Wrap around from 3
                        v3X = p0X - p3X;
                        v3Y = p0Y - p3Y;
                    }
                }
                else
                {
                    //Wrap around from 2
                    v2X = p0X - p2X;
                    v2Y = p0Y - p2Y;
                }
            }
            else
            {
                //Wrap around from 1
                v1X = p0X - p1X;
                v1Y = p0Y - p1Y;
            }

            #endregion

            /* An array of arrays of new convex hull points along the sides of the polygon created by the 3 to 8 points
             * above. These are to be sorted arrays and they are sorted by the distances (stored in sortedDistances) from the
             * started extrema vertex to the last. We are going to make each array really big so that we don't have to waste
             * time extending them later. The sizes array keeps the true length. */
            var sortedPoints = new TVertex[cvxVNum][];
            var sortedDistances = new double[cvxVNum][];
            var sizes = new int[cvxVNum];
            for (int i = 0; i < cvxVNum; i++)
            {
                sizes[i] = 0;
                sortedPoints[i] = new TVertex[numPoints];
                sortedDistances[i] = new double[numPoints];
            }
            var indexOfUsedIndices = 0;
            var nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
            /* Now a big loop. For each of the original vertices, check them with the 3 to 8 edges to see if they 
             * are inside or out. If they are out, add them to the proper row of the hullCands array. */
            for (var i = 0; i < numPoints; i++)
            {
                if (indexOfUsedIndices < indicesUsed.Length && i == nextUsedIndex)
                    //in order to avoid a contains function call, we know to only check with next usedIndex in order
                    nextUsedIndex =
                        indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
                else
                {
                    var point = points[i];
                    var newPointX = point.X;
                    var newPointY = point.Y;
                    if (AddToListAlong(sortedPoints[0], sortedDistances[0], ref sizes[0], point, newPointX, newPointY, p0X, p0Y, v0X, v0Y, tolerance)) continue;
                    if (AddToListAlong(sortedPoints[1], sortedDistances[1], ref sizes[1], point, newPointX, newPointY, p1X, p1Y, v1X, v1Y, tolerance)) continue;
                    if (AddToListAlong(sortedPoints[2], sortedDistances[2], ref sizes[2], point, newPointX, newPointY, p2X, p2Y, v2X, v2Y, tolerance)) continue;
                    if (cvxVNum == 3) continue;
                    if (AddToListAlong(sortedPoints[3], sortedDistances[3], ref sizes[3], point, newPointX, newPointY, p3X, p3Y, v3X, v3Y, tolerance)) continue;
                    if (cvxVNum == 4) continue;
                    if (AddToListAlong(sortedPoints[4], sortedDistances[4], ref sizes[4], point, newPointX, newPointY, p4X, p4Y, v4X, v4Y, tolerance)) continue;
                    if (cvxVNum == 5) continue;
                    if (AddToListAlong(sortedPoints[5], sortedDistances[5], ref sizes[5], point, newPointX, newPointY, p5X, p5Y, v5X, v5Y, tolerance)) continue;
                    if (cvxVNum == 6) continue;
                    if (AddToListAlong(sortedPoints[6], sortedDistances[6], ref sizes[6], point, newPointX, newPointY, p6X, p6Y, v6X, v6Y, tolerance)) continue;
                    if (cvxVNum == 7) continue;
                    if (AddToListAlong(sortedPoints[7], sortedDistances[7], ref sizes[7], point, newPointX, newPointY, p7X, p7Y, v7X, v7Y, tolerance)) continue;
                }
            }
            #endregion

            #region Step 3: now remove concave "zigs" from each sorted dictionary

            /* Now it's time to go through our array of sorted arrays. We search backwards through
             * the current convex hull points s.t. any additions will not confuse our for-loop indexers.
             * This approach is linear over the zig-zag polyline defined by each sorted list. This linear approach
             * was defined long ago by a number of authors: McCallum and Avis, Tor and Middleditch (1984), or
             * Melkman (1985) */
            for (var j = cvxVNum - 1; j >= 0; j--)
            {
                var size = sizes[j];
                if (size == 1)
                    /* If there is one and only one candidate, it must be in the convex hull. Add it now. */
                    convexHullCCW.Insert(j + 1, sortedPoints[j][0]);
                else if (size > 1)
                {
                    /* it seems a shame to have this list since it's nearly the same as the sorted array, but
                     * it is necessary for the removal of points. */
                    var pointsAlong = new List<TVertex>();
                    /* put the known starting point as the beginning of the list.  */
                    pointsAlong.Add(convexHullCCW[j]);
                    for (int k = 0; k < size; k++)
                        pointsAlong.Add(sortedPoints[j][k]);
                    /* put the ending point on the end of the list. Need to check if it wraps back around to 
                     * the first in the loop (hence the simple condition). */
                    if (j == cvxVNum - 1) pointsAlong.Add(convexHullCCW[0]);
                    else pointsAlong.Add(convexHullCCW[j + 1]);

                    /* Now starting from second from end, work backwards looks for places where the angle 
                     * between the vertices is concave (which would produce a negative value of z). */
                    var i = size;
                    while (i > 0)
                    {
                        //var currentPoint =
                        double lX = pointsAlong[i].X - pointsAlong[i - 1].X, lY = pointsAlong[i].Y - pointsAlong[i - 1].Y;
                        double rX = pointsAlong[i + 1].X - pointsAlong[i].X, rY = pointsAlong[i + 1].Y - pointsAlong[i].Y;
                        double zValue = lX * rY - lY * rX;
                        if (zValue < tolerance || (Math.Abs(lX) < tolerance && Math.Abs(lY) < tolerance))
                        {
                            /* remove any vertices that create concave angles. */
                            pointsAlong.RemoveAt(i);
                            /* but don't reduce k since we need to check the previous angle again. Well, 
                             * if you're back to the end you do need to reduce k (hence the line below). */
                            if (i == pointsAlong.Count - 1) i--;
                        }
                        /* if the angle is convex, then continue toward the start, k-- */
                        else i--;
                    }

                    /* for each of the remaining vertices in hullCands[i-1], add them to the convexHullCCW. 
                     * Here we insert them backwards (k counts down) to simplify the insert operation (k.e.
                     * since all are inserted @ i, the previous inserts are pushed up to i+1, i+2, etc. */
                    for (i = pointsAlong.Count - 2; i > 0; i--)
                        convexHullCCW.Insert(j + 1, pointsAlong[i]);
                }
            }

            #endregion

            return convexHullCCW;
        }

        private static List<TVertex> FindIntermediatePointsForLongSkinny<TVertex>(IList<TVertex> points, int numPoints,
            int usedIndex1, int usedIndex2, out List<int> newUsedIndices) where TVertex : IVertex2D
        {
            newUsedIndices = new List<int>();
            var pStartX = points[usedIndex1].X;
            var pStartY = points[usedIndex1].Y;
            var spanVectorX = points[usedIndex2].X - pStartX;
            var spanVectorY = points[usedIndex2].Y - pStartY;
            var minCross = -Constants.DefaultPlaneDistanceTolerance;
            var maxCross = Constants.DefaultPlaneDistanceTolerance;
            var minCrossIndex = -1;
            var maxCrossIndex = -1;
            for (var i = 0; i < numPoints; i++)
            {
                if (i == usedIndex1 || i == usedIndex2) continue;
                var p = points[i];
                var cross = spanVectorX * (p.Y - pStartY) + spanVectorY * (pStartX - p.X);
                if (cross < minCross)
                {
                    minCrossIndex = i;
                    minCross = cross;
                }
                if (cross > maxCross)
                {
                    maxCrossIndex = i;
                    maxCross = cross;
                }
            }

            var newCvxList = new List<TVertex>();
            newCvxList.Add(points[usedIndex1]);
            if (minCrossIndex != -1)
            {
                newUsedIndices.Add(minCrossIndex);
                newCvxList.Add(points[minCrossIndex]);
            }
            newCvxList.Add(points[usedIndex2]);
            if (maxCrossIndex != -1)
            {
                newUsedIndices.Add(maxCrossIndex);
                newCvxList.Add(points[maxCrossIndex]);
            }
            return newCvxList;
        }

        // this function adds the new point to the sorted array. The reason it is complicated is that
        // if it errors - it is because there are two points at the same distance along. So, we then
        // check if the new point or the existing one on the list should stay. Simply keep the one that is
        // furthest from the edge vector.
#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool AddToListAlong<TVertex>(TVertex[] sortedPoints, double[] sortedKeys, ref int size,
                TVertex newPoint, double newPointX, double newPointY, double basePointX, double basePointY,
                double edgeVectorX, double edgeVectorY, double tolerance) where TVertex : IVertex2D
        {
            var vectorToNewPointX = newPointX - basePointX;
            var vectorToNewPointY = newPointY - basePointY;
            var newDxOut = vectorToNewPointX * edgeVectorY - vectorToNewPointY * edgeVectorX;
            if (newDxOut <= tolerance) return false;
            var newDxAlong = edgeVectorX * vectorToNewPointX + edgeVectorY * vectorToNewPointY;
            int index = BinarySearch(sortedKeys, size, newDxAlong);
            if (index >= 0)
            {
                // non-negative values occur when the same key is found. In this case, we only want to keep
                // the one vertex that sticks out the farthest.
                var ptOnList = sortedPoints[index];
                var onListDxOut = (ptOnList.X - basePointX) * edgeVectorY - (ptOnList.Y - basePointY) * edgeVectorX;
                if (newDxOut > onListDxOut)
                    sortedPoints[index] = newPoint;
            }
            else
            {
                // here a new value is found. first, invert the index to find where to put it
                index = ~index;
                // as a slight time saver, we can check the two points that will surround this new point. 
                // If it makes a concave corner then don't add it. this part is actually in the middle 
                // condition ("else if (index < size)"). We don't need to perform this check if the insertion
                // is at either at. At the beginning ("index == 0"), we still need to increment the rest of the list
                if (index == 0)
                {
                    for (int i = size; i > index; i--)
                    {
                        sortedKeys[i] = sortedKeys[i - 1];
                        sortedPoints[i] = sortedPoints[i - 1];
                    }
                    sortedKeys[index] = newDxAlong;
                    sortedPoints[index] = newPoint;
                    size++;
                }
                else if (index < size)
                {
                    var prevPt = sortedPoints[index - 1];
                    var nextPt = sortedPoints[index];
                    double lX = newPointX - prevPt.X, lY = newPointY - prevPt.Y;
                    double rX = nextPt.X - newPointX, rY = nextPt.Y - newPointY;
                    double zValue = lX * rY - lY * rX;
                    // if cross produce is negative (well, is less than some small positive number, then new point is concave) then don't add it.
                    // also, don't add it if the point is nearly identical (again, within the tolerance) of the previous point.
                    if (zValue < tolerance || (Math.Abs(lX) < tolerance && Math.Abs(lY) < tolerance))
                    {
                        for (int i = size; i > index; i--)
                        {
                            sortedKeys[i] = sortedKeys[i - 1];
                            sortedPoints[i] = sortedPoints[i - 1];
                        }
                        sortedKeys[index] = newDxAlong;
                        sortedPoints[index] = newPoint;
                        size++;
                    }
                }
                else
                {   // if at the end, then no need to increment any other members.
                    sortedKeys[index] = newDxAlong;
                    sortedPoints[index] = newPoint;
                    size++;
                }
            }
            return true;
        }

        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int BinarySearch(double[] array, int length, double value)
        {
            var lo = 0;
            var hi = length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                var c = array[i];
                if (c == value) return i;
                if (c < value) lo = i + 1;
                else hi = i - 1;
            }
            return ~lo;
        }
    }
}
