using System;

namespace MIConvexHull
{
    /// <summary>
    /// Class ConvexHullGenerationException.
    /// Implements the <see cref="System.Exception" />
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ConvexHullGenerationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHullGenerationException"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="errorMessage">The error message.</param>
        public ConvexHullGenerationException(ConvexHullCreationResultOutcome error, string errorMessage)
        {
            ErrorMessage = errorMessage;
            Error = error;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>The error.</value>
        public ConvexHullCreationResultOutcome Error { get; }
    }
}