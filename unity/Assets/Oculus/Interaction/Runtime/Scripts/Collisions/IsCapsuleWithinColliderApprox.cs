/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Oculus.Interaction
{
    public static partial class Collisions
    {
        /// <summary>
        /// Approximate capsule collision by doing sphere collisions down the capsule length
        /// </summary>
        /// <param name="p0">Capsule Start</param>
        /// <param name="p1">Capsule End</param>
        /// <param name="radius">Capsule Radius</param>
        /// <param name="collider">Collider to check against</param>
        /// <returns>Whether or not an approximate collision occured.</returns>
        public static bool IsCapsuleWithinColliderApprox(Vector3 p0, Vector3 p1, float radius, Collider collider)
        {
            int divisions = Mathf.CeilToInt((p1 - p0).magnitude / radius) * 2;

            if (divisions == 0)
            {
                return IsSphereWithinCollider(p0, radius, collider);
            }

            float tStep = 1f / divisions;
            for (int i = 0; i <= divisions; i++)
            {
                Vector3 point = Vector3.Lerp(p0, p1, tStep * i);
                if (IsSphereWithinCollider(point, radius, collider))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
