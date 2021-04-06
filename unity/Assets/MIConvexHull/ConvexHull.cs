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
    /// Factory class for computing convex hulls.
    /// </summary>
    public static class ConvexHull
    {
        /// <summary>
        /// Creates a convex hull of the input data.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <typeparam name="TFace">The type of the t face.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The plane distance tolerance (default is 1e-10). If too high, points 
        /// will be missed. If too low, the algorithm may break. Only adjust if you notice problems.</param>
        /// <returns>
        /// ConvexHull&lt;TVertex, TFace&gt;.
        /// </returns>
        public static ConvexHullCreationResult<TVertex, TFace> Create<TVertex, TFace>(IList<TVertex> data,
            double tolerance = Constants.DefaultPlaneDistanceTolerance)
            where TVertex : IVertex
            where TFace : ConvexFace<TVertex, TFace>, new()
        {
            return ConvexHull<TVertex, TFace>.Create(data, tolerance);
        }

        /// <summary>
        /// Creates a convex hull of the input data.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The plane distance tolerance (default is 1e-10). If too high, points 
        /// will be missed. If too low, the algorithm may break. Only adjust if you notice problems.</param>
        /// <returns>
        /// ConvexHull&lt;TVertex, DefaultConvexFace&lt;TVertex&gt;&gt;.
        /// </returns>
        public static ConvexHullCreationResult<TVertex, DefaultConvexFace<TVertex>> Create<TVertex>(IList<TVertex> data,
            double tolerance = Constants.DefaultPlaneDistanceTolerance)
            where TVertex : IVertex
        {
            return ConvexHull<TVertex, DefaultConvexFace<TVertex>>.Create(data, tolerance);
        }

        /// <summary>
        /// Creates a convex hull of the input data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The plane distance tolerance (default is 1e-10). If too high, points 
        /// will be missed. If too low, the algorithm may break. Only adjust if you notice problems.</param>
        /// <returns>
        /// ConvexHull&lt;DefaultVertex, DefaultConvexFace&lt;DefaultVertex&gt;&gt;.
        /// </returns>
        public static ConvexHullCreationResult<DefaultVertex, DefaultConvexFace<DefaultVertex>> Create(IList<double[]> data,
            double tolerance = Constants.DefaultPlaneDistanceTolerance)
        {
            var points = data.Select(p => new DefaultVertex { Position = p })
                             .ToList();
            return ConvexHull<DefaultVertex, DefaultConvexFace<DefaultVertex>>.Create(points, tolerance);
        }

        /// <summary>
        /// Creates the 2D convex hull of the input data.
        /// </summary>
        /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;TVertex&gt;.</returns>
        public static ConvexHullCreationResult<TVertex> Create2D<TVertex>(IList<TVertex> data, double tolerance = Constants.DefaultPlaneDistanceTolerance)
            where TVertex : IVertex2D, new()
        {
            return ConvexHull<TVertex>.Create(data, tolerance);
        }

        /// <summary>
        /// Creates the 2D convex hull of the input data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;TVertex&gt;.</returns>
        public static ConvexHullCreationResult<DefaultVertex2D> Create2D(IList<double[]> data, double tolerance = Constants.DefaultPlaneDistanceTolerance)
        {
            var points = data.Select(p => new DefaultVertex2D(p)).ToList();
            return Create2D(points, tolerance);
        }

    }

    /// <summary>
    /// Representation of a convex hull.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TFace">The type of the t face.</typeparam>
    public class ConvexHull<TVertex, TFace> where TVertex : IVertex
                                            where TFace : ConvexFace<TVertex, TFace>, new()
    {
        /// <summary>
        /// Can only be created using a factory method.
        /// </summary>
        internal ConvexHull()
        {
        }

        /// <summary>
        /// Points of the convex hull.
        /// </summary>
        /// <value>The points.</value>
        public IEnumerable<TVertex> Points { get; internal set; }

        /// <summary>
        /// Faces of the convex hull.
        /// </summary>
        /// <value>The faces.</value>
        public IEnumerable<TFace> Faces { get; internal set; }


        /// <summary>
        /// Creates the convex hull.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The plane distance tolerance.</param>
        /// <returns>
        /// ConvexHullCreationResult&lt;TVertex, TFace&gt;.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The supplied data is null.</exception>
        /// <exception cref="ArgumentNullException">data</exception>
        internal static ConvexHullCreationResult<TVertex, TFace> Create(IList<TVertex> data, double tolerance)
        {
            if (data == null)
            {
                throw new ArgumentNullException("The supplied data is null.");
            }

            try
            {
                var ch = new ConvexHullAlgorithm(data.Cast<IVertex>().ToArray(), false, tolerance);
                // todo: can this cast be avoided by changing ConvexHullAlgorithm to use TVertex?
                ch.GetConvexHull();

                var convexHull = new ConvexHull<TVertex, TFace>
                {
                    Points = ch.GetHullVertices(data),
                    Faces = ch.GetConvexFaces<TVertex, TFace>()
                };
                return new ConvexHullCreationResult<TVertex, TFace>(convexHull, ConvexHullCreationResultOutcome.Success);
            }
            catch (ConvexHullGenerationException e)
            {
                return new ConvexHullCreationResult<TVertex, TFace>(null, e.Error, e.ErrorMessage);
            }
            catch (Exception e)
            {
                return new ConvexHullCreationResult<TVertex, TFace>(null, ConvexHullCreationResultOutcome.UnknownError, e.Message);
            }
        }
    }

    /// <summary>
    /// Representation of a convex hull.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TFace">The type of the t face.</typeparam>
    public class ConvexHull<TVertex> where TVertex : IVertex2D, new()
    {
        /// <summary>
        /// Can only be created using a factory method.
        /// </summary>
        internal ConvexHull()
        {
        }

        /// <summary>
        /// Points of the convex hull.
        /// </summary>
        /// <value>The points.</value>
        public IEnumerable<TVertex> Points { get; internal set; }



        /// <summary>
        /// Creates the convex hull.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="tolerance">The plane distance tolerance.</param>
        /// <returns>
        /// ConvexHullCreationResult&lt;TVertex, TFace&gt;.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The supplied data is null.</exception>
        /// <exception cref="ArgumentNullException">data</exception>
        internal static ConvexHullCreationResult<TVertex> Create(IList<TVertex> data, double tolerance)
        {
            if (data == null)
            {
                throw new ArgumentNullException("The supplied data is null.");
            }

            try
            {
                var points = ConvexHull2DAlgorithm.Create(data, tolerance);

                return new ConvexHullCreationResult<TVertex>(points, ConvexHullCreationResultOutcome.Success);
            }
            catch (ConvexHullGenerationException e)
            {
                return new ConvexHullCreationResult<TVertex>(null, e.Error, e.ErrorMessage);
            }
            catch (Exception e)
            {
                return new ConvexHullCreationResult<TVertex>(null, ConvexHullCreationResultOutcome.UnknownError, e.Message);
            }
        }
    }

    /// <summary>
    /// Class ConvexHullCreationResult.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TFace">The type of the t face.</typeparam>
    public class ConvexHullCreationResult<TVertex, TFace> where TVertex : IVertex
                                                          where TFace : ConvexFace<TVertex, TFace>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHullCreationResult{TVertex, TFace}" /> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="outcome">The outcome.</param>
        /// <param name="errorMessage">The error message.</param>
        public ConvexHullCreationResult(ConvexHull<TVertex, TFace> result, ConvexHullCreationResultOutcome outcome, string errorMessage = "")
        {
            Result = result;
            Outcome = outcome;
            ErrorMessage = errorMessage;
        }

        //this could be null
        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        public ConvexHull<TVertex, TFace> Result { get; }

        /// <summary>
        /// Gets the outcome.
        /// </summary>
        /// <value>The outcome.</value>
        public ConvexHullCreationResultOutcome Outcome { get; }
        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string ErrorMessage { get; }
    }
    /// <summary>
    /// Class ConvexHullCreationResult.
    /// </summary>
    /// <typeparam name="TVertex">The type of the t vertex.</typeparam>
    /// <typeparam name="TFace">The type of the t face.</typeparam>
    public class ConvexHullCreationResult<TVertex> where TVertex : IVertex2D, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHullCreationResult{TVertex, TFace}" /> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="outcome">The outcome.</param>
        /// <param name="errorMessage">The error message.</param>
        public ConvexHullCreationResult(IList<TVertex> result, ConvexHullCreationResultOutcome outcome, string errorMessage = "")
        {
            Result = result;
            Outcome = outcome;
            ErrorMessage = errorMessage;
        }

        //this could be null
        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        public IList<TVertex> Result { get; }

        /// <summary>
        /// Gets the outcome.
        /// </summary>
        /// <value>The outcome.</value>
        public ConvexHullCreationResultOutcome Outcome { get; }
        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string ErrorMessage { get; }
    }
}