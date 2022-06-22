namespace UnityEngine.XR.Interaction.Toolkit.Inputs
{
    /// <summary>
    /// One of the four primary directions.
    /// </summary>
    /// <seealso cref="CardinalUtility"/>
    public enum Cardinal
    {
        /// <summary>
        /// North direction, e.g. forward on a thumbstick.
        /// </summary>
        North,

        /// <summary>
        /// South direction, e.g. back on a thumbstick.
        /// </summary>
        South,

        /// <summary>
        /// East direction, e.g. right on a thumbstick.
        /// </summary>
        East,

        /// <summary>
        /// West direction, e.g. left on a thumbstick.
        /// </summary>
        West,
    }

    /// <summary>
    /// Utility functions related to <see cref="Cardinal"/> directions.
    /// </summary>
    public static class CardinalUtility
    {
        /// <summary>
        /// Get the nearest cardinal direction for a given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Input vector, such as from a thumbstick.</param>
        /// <returns>Returns the nearest <see cref="Cardinal"/> direction.</returns>
        /// <remarks>
        /// Arbitrarily biases towards <see cref="Cardinal.North"/> and <see cref="Cardinal.South"/>
        /// to disambiguate when angle is exactly equidistant between directions.
        /// </remarks>
        public static Cardinal GetNearestCardinal(Vector2 value)
        {
            var angle = Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
            var absAngle = Mathf.Abs(angle);
            if (absAngle < 45f)
                return Cardinal.East;
            if (absAngle > 135f)
                return Cardinal.West;
            return angle >= 0f ? Cardinal.North : Cardinal.South;
        }
    }
}
