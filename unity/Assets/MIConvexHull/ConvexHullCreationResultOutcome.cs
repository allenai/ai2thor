namespace MIConvexHull
{
    /// <summary>
    /// Enum ConvexHullCreationResultOutcome
    /// </summary>
    public enum ConvexHullCreationResultOutcome
    {
        /// <summary>
        /// Successfully created.
        /// </summary>
        Success,
        /// <summary>
        ///  dimension smaller two
        /// </summary>
        DimensionSmallerTwo,
        /// <summary>
        ///  dimension smaller two
        /// </summary>
        DimensionTwoWrongMethod,
        /// <summary>
        ///  not enough vertices for dimension
        /// </summary>
        NotEnoughVerticesForDimension,
        /// <summary>
        ///  non uniform dimension
        /// </summary>
        NonUniformDimension,
        /// <summary>
        ///  degenerate data
        /// </summary>
        DegenerateData,
        /// <summary>
        ///  unknown error
        /// </summary>
        UnknownError
    }
}