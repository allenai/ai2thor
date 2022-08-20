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

using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    [Serializable]
    public struct PoseTravelData
    {
        /// <summary>
        /// When attracting the object, indicates the  rate it will take for the object to realign with the hand after a grab
        /// </summary>
        [Tooltip("When attracting the object, indicates the rate (in m/s, or seconds if UseFixedTravelTime is enabled) for the object to realign with the hand after a grab.")]
        [SerializeField]
        private float _travelSpeed;
        /// <summary>
        /// Changes the units of the TravelSpeed, disabled means m/s while enabled is fixed seconds
        /// </summary>
        [Tooltip("Changes the units of the TravelSpeed, disabled means m/s while enabled is fixed seconds")]
        [SerializeField]
        private bool _useFixedTravelTime;
        /// <summary>
        /// Animation to use in conjunction with TravelSpeed to define the traveling motion speeds.
        /// </summary>
        [Tooltip("Animation to use in conjunction with TravelSpeed to define the traveling motion.")]
        [SerializeField]
        private AnimationCurve _travelCurve;

        private const float DEGREES_TO_PERCEIVED_METERS = 0.5f / 360f;

        public static PoseTravelData DEFAULT => new PoseTravelData()
        {
            _travelSpeed = 1f,
            _useFixedTravelTime = false,
            _travelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };

        public static PoseTravelData FAST => new PoseTravelData()
        {
            _travelSpeed = 0.1f,
            _useFixedTravelTime = true,
            _travelCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
        };

        public Tween CreateTween(in Pose from, in Pose to)
        {
            float tweenTime = _travelSpeed;
            if (!_useFixedTravelTime && _travelSpeed != 0f)
            {
                float travelDistance = PerceivedDistance(from, to);
                tweenTime = travelDistance / _travelSpeed;
            }
            Tween tween = new Tween(from, tweenTime, tweenTime * 0.5f, _travelCurve);
            tween.MoveTo(to);
            return tween;
        }

        private static float PerceivedDistance(in Pose from, in Pose to)
        {
            Pose grabOffset = PoseUtils.Delta(from, to);
            float translationDistance = grabOffset.position.magnitude;

            float rotationDistance = DEGREES_TO_PERCEIVED_METERS * Mathf.Max(
                Mathf.Max(Vector3.Angle(from.forward, to.forward),
                Vector3.Angle(from.up, to.up),
                Vector3.Angle(from.right, to.right)));

            float travelDistance = Mathf.Max(translationDistance, rotationDistance);

            return travelDistance;
        }
    }
}
