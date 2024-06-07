/*
Code adapted from the "ShaderTutorials" repository of Ronja Böhringer,
see https://github.com/ronja-tutorials/ShaderTutorials.
*/

using UnityEngine;

namespace Ronja
{
    [ExecuteAlways]
    public class ClippingPlane : MonoBehaviour
    {
        // Materials we pass the values to
        public Material[] materials = null;
        public bool shouldClip = true;

        // Execute every frame
        void Update()
        {
            // Create plane
            Plane plane = new Plane(transform.up, transform.position);
            // Transfer values from plane to vector4
            Vector4 planeRepresentation = new Vector4(
                plane.normal.x,
                plane.normal.y,
                plane.normal.z,
                plane.distance
            );
            // Pass vector to shader
            if (materials != null)
            {
                foreach (Material mat in materials)
                {
                    mat.SetVector("_Plane", planeRepresentation);
                    mat.SetInt("_IsEnabled", (shouldClip ? 1 : 0));
                }
            }
        }
    }
}
