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

namespace Oculus.Interaction.HandGrab.SnapSurfaces
{
    /// <summary>
    /// This interface defines the method needed to use snap surfaces. They allow finding the
    /// nearest poses at the surface to a given set of parameters as well as duplicating and
    /// mirroring the surface.
    /// </summary>
    public interface ISnapSurface
    {
        /// <summary>
        /// Finds the Pose at the surface that is the closest to the given pose.
        /// </summary>
        /// <param name="targetPose">The pose to find the nearest to.</param>
        /// <param name="reference">The reference snap point to use for measuring at the surface.</param>
        /// <param name="bestPose">The best found pose at the surface.<</param>
        /// <returns>The score indicating how good the found pose was, -1 for invalid result.</returns>
        float CalculateBestPoseAtSurface(in Pose targetPose, in Pose reference, out Pose bestPose, in PoseMeasureParameters scoringModifier);

        /// <summary>
        /// Finds the Pose at the surface that is the closest to the given ray.
        /// </summary>
        /// <param name="targetRay">Ray searching for the nearest snap pose</param>
        /// <param name="reference">The reference snap point to use for measuring at the surface.</param>
        /// <param name="bestPose">The best found pose at the surface.</param>
        /// <returns>True if the pose was found</returns>
        bool CalculateBestPoseAtSurface(Ray targetRay, in Pose reference, out Pose bestPose);

        /// <summary>
        /// Method for mirroring a Pose around the surface.
        /// Different surfaces will prefer mirroring along different axis.
        /// </summary>
        /// <param name="gripPose">The Pose to be mirrored.</param>
        /// <returns>A new pose mirrored at this surface.</returns>
        Pose MirrorPose(in Pose gripPose);

        /// <summary>
        /// Creates a new ISnapSurface under the selected gameobject
        /// that is a mirror version of the current.
        /// </summary>
        /// <param name="gameObject">The gameobject in which to place the new ISnapSurface.</param>
        /// <returns>A mirror of this ISnapSurface.</returns>
        ISnapSurface CreateMirroredSurface(GameObject gameObject);

        /// <summary>
        /// Creates a new ISnapSurface under the selected gameobject
        /// with the same data as this one.
        /// </summary>
        /// <param name="gameObject">The gameobject in which to place the new ISnapSurface.</param>
        /// <returns>A clone of this ISnapSurface.</returns>
        ISnapSurface CreateDuplicatedSurface(GameObject gameObject);
    }
}
