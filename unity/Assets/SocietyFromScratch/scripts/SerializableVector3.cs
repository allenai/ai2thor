using UnityEngine;

 /// <summary>
    /// A `System.Serializable` wrapper for the `Vector3` class.
    /// </summary>
    [System.Serializable]
    public class SerializableVector3
    {
        float

                x,
                y,
                z;

        /// <summary>
        /// Copy over the state from an existing Vector3.
        /// </summary>
        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        /// <summary>
        /// Create a Vector3 from this class' state.
        /// /// </summary>
        /// <returns></returns>
        public Vector3 ToVector()
        {
            return new Vector3(x, y, z);
        }
    }
