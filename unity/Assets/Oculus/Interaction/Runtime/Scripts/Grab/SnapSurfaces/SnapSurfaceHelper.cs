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
    public static class SnapSurfaceHelper
    {
        public delegate Pose PoseCalculator(in Pose desiredPose, in Pose snapPose);

        /// <summary>
        /// Finds the best pose comparing the one that requires the minimum rotation
        /// and minimum translation.
        /// </summary>
        /// <param name="desiredPose">Pose to measure from.</param>
        /// <param name="snapPose">Reference pose of the surface.</param>
        /// <param name="bestPose">Nearest pose to the desired one at the surface.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="minimalTranslationPoseCalculator">Delegate to calculate the nearest, by position, pose at a surface.</param>
        /// <param name="minimalRotationPoseCalculator">Delegate to calculate the nearest, by rotation, pose at a surface.</param>
        /// <returns>The score, normalized, of the best pose.</returns>
        public static float CalculateBestPoseAtSurface(in Pose desiredPose, in Pose snapPose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier,
            PoseCalculator minimalTranslationPoseCalculator, PoseCalculator minimalRotationPoseCalculator)
        {
            float bestScore;
            Pose minimalRotationPose = minimalRotationPoseCalculator(desiredPose, snapPose);
            if (scoringModifier.MaxDistance > 0)
            {
                Pose minimalTranslationPose = minimalTranslationPoseCalculator(desiredPose, snapPose);

                bestPose = SelectBestPose(minimalRotationPose, minimalTranslationPose, desiredPose, scoringModifier, out bestScore);
            }
            else
            {
                bestPose = minimalRotationPose;
                bestScore = PoseUtils.RotationalSimilarity(desiredPose.rotation, bestPose.rotation);
            }
            return bestScore;
        }

        /// <summary>
        /// Compares two poses to a reference and returns the most similar one
        /// </summary>
        /// <param name="a">First pose to compare with the reference.</param>
        /// <param name="b">Second pose to compare with the reference.</param>
        /// <param name="reference">Reference pose to measure from.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="maxDistance">Max distance to measure the score.</param>
        /// <param name="bestScore">Out value with the score of the best pose.</param>
        /// <returns>The most similar pose to reference out of a and b</returns>
        public static Pose SelectBestPose(in Pose a, in Pose b, in Pose reference, PoseMeasureParameters scoringModifier, out float bestScore)
        {
            float aScore = PoseUtils.Similarity(reference, a, scoringModifier);
            float bScore = PoseUtils.Similarity(reference, b, scoringModifier);
            if (aScore >= bScore)
            {
                bestScore = aScore;
                return a;
            }
            bestScore = bScore;
            return b;
        }
    }
}
