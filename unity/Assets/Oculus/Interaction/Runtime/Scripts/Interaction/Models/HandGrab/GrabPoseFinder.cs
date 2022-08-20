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

using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Utility class used by the grab interactors to find the best matching
    /// pose from a provided list of HandGrabPoses in an object.
    /// </summary>
    public class GrabPoseFinder
    {
        /// <summary>
        /// List of HandGrabPoses that can move the provided _relativeTo Transform
        /// </summary>
        private List<HandGrabPose> _handGrabPoses;
        /// <summary>
        /// Target Transform that is grabbed
        /// </summary>
        private Transform _relativeTo;
        /// <summary>
        /// When no HandGrabPoses are provided. This transform is used
        /// as a HandGrabPoses without HandPose or Handedness.
        /// </summary>
        private Transform _fallbackTransform;
        /// <summary>
        /// Cached relative pose from the target object to the fallbacktransform
        /// to save computation when the _fallbackTransform is used.
        /// </summary>
        private Pose _cachedFallbackPose;

        private InterpolationCache _interpolationCache = new InterpolationCache();

        public GrabPoseFinder(List<HandGrabPose> handGrabPoses, Transform relativeTo, Transform fallbackTransform)
        {
            _handGrabPoses = handGrabPoses;
            _relativeTo = relativeTo;
            _fallbackTransform = fallbackTransform;

            _cachedFallbackPose = _relativeTo.Delta(fallbackTransform);
        }

        public bool UsesHandPose()
        {
            return _handGrabPoses.Count > 0 && _handGrabPoses[0].HandPose != null;
        }

        /// <summary>
        /// Finds the best valid hand-pose at this HandGrabInteractable.
        /// Remember that a HandGrabPoses can actually have a whole surface the user can snap to.
        /// </summary>
        /// <param name="userPose">Pose to compare to the snap point in world coordinates.</param>
        /// <param name="handScale">The scale of the tracked hand.</param>
        /// <param name="handedness">The handedness of the tracked hand.</param>
        /// <param name="scoringModifier">Parameters indicating how to score the different poses.</param>
        /// <param name="result">The resultant best pose found.</param>
        /// <returns>True if a good pose was found</returns>
        public bool FindBestPose(Pose userPose, float handScale, Handedness handedness, PoseMeasureParameters scoringModifier, ref HandGrabResult result)
        {
            if (_handGrabPoses.Count == 1)
            {
                return _handGrabPoses[0]
                    .CalculateBestPose(userPose, handedness, scoringModifier, ref result);
            }
            else if (_handGrabPoses.Count > 1)
            {
                return CalculateBestScaleInterpolatedPose(userPose, handedness, handScale,
                    scoringModifier, ref result);
            }
            else
            {
                result.HasHandPose = false;
                result.SnapPose = new Pose(_cachedFallbackPose.position, Quaternion.Inverse(_relativeTo.rotation) * userPose.rotation);
                result.Score = PoseUtils.Similarity(userPose, _fallbackTransform.GetPose(), scoringModifier);
                return true;
            }
        }

        public bool FindScaledHandPose(float handScale, ref HandPose handPose)
        {
            if (_handGrabPoses.Count == 1 && _handGrabPoses[0].HandPose != null)
            {
                handPose.CopyFrom(_handGrabPoses[0].HandPose);
                return true;
            }
            else if (_handGrabPoses.Count > 1)
            {
                FindInterpolationRange(handScale, _handGrabPoses, out HandGrabPose under, out HandGrabPose over, out float t);
                if (under.HandPose != null && over.HandPose != null)
                {
                    HandPose.Lerp(_interpolationCache.underResult.HandPose, _interpolationCache.overResult.HandPose, t, ref handPose);
                    return true;
                }
                else if (under.HandPose != null)
                {
                    handPose.CopyFrom(_interpolationCache.underResult.HandPose);
                    return true;
                }
                else if (over.HandPose != null)
                {
                    handPose.CopyFrom(_interpolationCache.overResult.HandPose);
                    return true;
                }

                return false;
            }

            return false;
        }
        private bool CalculateBestScaleInterpolatedPose(Pose userPose, Handedness handedness, float handScale, PoseMeasureParameters scoringModifier,
            ref HandGrabResult result)
        {
            result.HasHandPose = false;

            FindInterpolationRange(handScale, _handGrabPoses, out HandGrabPose under, out HandGrabPose over, out float t);

            bool underFound = under.CalculateBestPose(userPose, handedness, scoringModifier,
                ref _interpolationCache.underResult);

            bool overFound = over.CalculateBestPose(userPose, handedness, scoringModifier,
                ref _interpolationCache.overResult);

            if (_interpolationCache.underResult.HasHandPose && _interpolationCache.overResult.HasHandPose)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(_interpolationCache.underResult.HandPose);
                HandPose.Lerp(_interpolationCache.underResult.HandPose, _interpolationCache.overResult.HandPose, t, ref result.HandPose);
                PoseUtils.Lerp(_interpolationCache.underResult.SnapPose, _interpolationCache.overResult.SnapPose, t, ref result.SnapPose);
            }
            else if (_interpolationCache.underResult.HasHandPose)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(_interpolationCache.underResult.HandPose);
                result.SnapPose.CopyFrom(_interpolationCache.underResult.SnapPose);
            }
            else if (_interpolationCache.overResult.HasHandPose)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(_interpolationCache.overResult.HandPose);
                result.SnapPose.CopyFrom(_interpolationCache.overResult.SnapPose);
            }

            if (underFound && overFound)
            {
                result.Score = Mathf.Lerp(_interpolationCache.underResult.Score,
                    _interpolationCache.overResult.Score, t);
                return true;
            }

            if (underFound)
            {
                result.Score = _interpolationCache.underResult.Score;
                return true;
            }

            if (overFound)
            {
                result.Score = _interpolationCache.overResult.Score;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the two nearest HandGrabPose to interpolate from given a scale.
        /// The result can require an unclamped interpolation (t can be bigger than 1 or smaller than 0).
        /// </summary>
        /// <param name="scale">The user scale</param>
        /// <param name="grabPoses">The list of grab poses to interpolate from</param>
        /// <param name="from">The HandGrabInteractable with nearest scale recorder that is smaller than the provided one</param>
        /// <param name="to">The HandGrabInteractable with nearest scale recorder that is bigger than the provided one</param>
        /// <param name="t">The progress between from and to variables at which the desired scale resides</param>
        /// <returns>The HandGrabPose near under and over the scale, and the interpolation factor between them.</returns>
        public static void FindInterpolationRange(float scale, List<HandGrabPose> grabPoses, out HandGrabPose from, out HandGrabPose to, out float t)
        {
            from = grabPoses[0];
            to = grabPoses[1];

            for (int i = 2; i < grabPoses.Count; i++)
            {
                HandGrabPose point = grabPoses[i];

                if (point.Scale <= scale
                    && point.Scale > from.Scale)
                {
                    from = point;
                }
                else if (point.Scale >= scale
                    && point.Scale < to.Scale)
                {
                    to = point;
                }
            }

            t = (scale - from.Scale) / (to.Scale - from.Scale);
        }

        private class InterpolationCache
        {
            public HandGrabResult underResult = new HandGrabResult();
            public HandGrabResult overResult = new HandGrabResult();
        }
    }
}
