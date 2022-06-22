namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Utility functions related to <see cref="Gizmos"/>.
    /// </summary>
    public static class GizmoHelpers
    {
        /// <summary>
        /// Draws oriented wire plane.
        /// </summary>
        /// <param name="position"> Position of the plane.</param>
        /// <param name="rotation"> Rotation of the plane.</param>
        /// <param name="size"> Size of the plane.</param>
        public static void DrawWirePlaneOriented(Vector3 position, Quaternion rotation, float size)
        {
            var halfSize = size / 2f;
            var tl = new Vector3(halfSize, 0f, -halfSize);
            var tr = new Vector3(halfSize, 0f, halfSize);
            var bl = new Vector3(-halfSize, 0f, -halfSize);
            var br = new Vector3(-halfSize, 0f, halfSize);

            Gizmos.DrawLine((rotation * tl) + position,
                (rotation * tr) + position);

            Gizmos.DrawLine((rotation * tr) + position,
                (rotation * br) + position);

            Gizmos.DrawLine((rotation * br) + position,
                (rotation * bl) + position);

            Gizmos.DrawLine((rotation * bl) + position,
                (rotation * tl) + position);
        }

        /// <summary>
        /// Draws oriented wire cube.
        /// </summary>
        /// <param name="position"> Position of the cube.</param>
        /// <param name="rotation"> Rotation of the cube.</param>
        /// <param name="size"> Size of the cube.</param>
        public static void DrawWireCubeOriented(Vector3 position, Quaternion rotation, float size)
        {

            var halfSize = size / 2f;
            var tl = new Vector3(halfSize, 0f, -halfSize);
            var tr = new Vector3(halfSize, 0f, halfSize);
            var bl = new Vector3(-halfSize, 0f, -halfSize);
            var br = new Vector3(-halfSize, 0f, halfSize);

            var tlt = new Vector3(halfSize, size, -halfSize);
            var trt = new Vector3(halfSize, size, halfSize);
            var blt = new Vector3(-halfSize, size, -halfSize);
            var brt = new Vector3(-halfSize, size, halfSize);


            Gizmos.DrawLine((rotation * tl) + position, (rotation * tr) + position);

            Gizmos.DrawLine((rotation * tr) + position, (rotation * br) + position);

            Gizmos.DrawLine((rotation * br) + position, (rotation * bl) + position);

            Gizmos.DrawLine((rotation * bl) + position, (rotation * tl) + position);

            Gizmos.DrawLine((rotation * tlt) + position, (rotation * trt) + position);

            Gizmos.DrawLine((rotation * trt) + position, (rotation * brt) + position);

            Gizmos.DrawLine((rotation * brt) + position, (rotation * blt) + position);

            Gizmos.DrawLine((rotation * blt) + position, (rotation * tlt) + position);

            Gizmos.DrawLine((rotation * tlt) + position, (rotation * tl) + position);

            Gizmos.DrawLine((rotation * trt) + position, (rotation * tr) + position);

            Gizmos.DrawLine((rotation * brt) + position, (rotation * br) + position);

            Gizmos.DrawLine((rotation * blt) + position, (rotation * bl) + position);
        }

        /// <summary>
        /// Draws world space standard basis vectors at <paramref name="transform"/>.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to represent.</param>
        /// <param name="size">Length of each ray.</param>
        public static void DrawAxisArrows(Transform transform, float size)
        {
            var position = transform.position;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, transform.forward * size);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, transform.up * size);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(position, transform.right * size);
        }
    }
}
