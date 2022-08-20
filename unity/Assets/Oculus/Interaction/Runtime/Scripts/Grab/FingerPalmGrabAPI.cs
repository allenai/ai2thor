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
using Oculus.Interaction.PoseDetection;
using UnityEngine;

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// This Finger API uses the curl value of the fingers to detect if they are grabbing
    /// </summary>
    public class FingerPalmGrabAPI : IFingerAPI
    {
        private Vector3 _poseVolumeCenterOffset = Vector3.zero;

        private static readonly Vector3 POSE_VOLUME_OFFSET_RIGHT = new Vector3(0.07f, -0.03f, 0.0f);
        private static readonly Vector3 POSE_VOLUME_OFFSET_LEFT = new Vector3(-0.07f, 0.03f, 0.0f);

        private static readonly float START_THRESHOLD = 0.9f;
        private static readonly float RELEASE_THRESHOLD = 0.6f;

        private static readonly Vector2[] CURL_RANGE = new Vector2[5]
        {
            new Vector2(190f, 220f),
            new Vector2(180f, 250f),
            new Vector2(180f, 250f),
            new Vector2(180f, 250f),
            new Vector2(180f, 245f),
        };

        private FingerShapes _fingerShapes = new FingerShapes();

        private class FingerGrabData
        {
            private readonly HandFinger _fingerID;
            private readonly Vector2 _curlNormalizationParams;
            public float GrabStrength;
            public bool IsGrabbing;
            public bool IsGrabbingChanged { get; private set; }

            public FingerGrabData(HandFinger fingerId)
            {
                _fingerID = fingerId;
                Vector2 range = CURL_RANGE[(int)_fingerID];
                _curlNormalizationParams = new Vector2(range.x, range.y - range.x);
            }

            public void UpdateGrabStrength(IHand hand, FingerShapes fingerShapes)
            {
                float curlAngle = fingerShapes.GetCurlValue(_fingerID, hand);

                if (_fingerID != HandFinger.Thumb)
                {
                    curlAngle = (curlAngle * 2 + fingerShapes.GetFlexionValue(_fingerID, hand)) / 3f;
                }

                GrabStrength = Mathf.Clamp01((curlAngle - _curlNormalizationParams.x) / _curlNormalizationParams.y);
            }

            public void UpdateIsGrabbing(float startThreshold, float releaseThreshold)
            {
                if (GrabStrength > startThreshold)
                {
                    if (!IsGrabbing)
                    {
                        IsGrabbing = true;
                        IsGrabbingChanged = true;
                    }
                    return;
                }

                if (GrabStrength < releaseThreshold)
                {
                    if (IsGrabbing)
                    {
                        IsGrabbing = false;
                        IsGrabbingChanged = true;
                    }
                }
            }

            public void ClearState()
            {
                IsGrabbingChanged = false;
            }
        }

        private readonly FingerGrabData[] _fingersGrabData = {
            new FingerGrabData(HandFinger.Thumb),
            new FingerGrabData(HandFinger.Index),
            new FingerGrabData(HandFinger.Middle),
            new FingerGrabData(HandFinger.Ring),
            new FingerGrabData(HandFinger.Pinky)
        };

        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            return _fingersGrabData[(int)finger].IsGrabbing;
        }

        public bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetGrabState)
        {
            return _fingersGrabData[(int)finger].IsGrabbingChanged &&
                   _fingersGrabData[(int)finger].IsGrabbing == targetGrabState;
        }

        public float GetFingerGrabScore(HandFinger finger)
        {
            return _fingersGrabData[(int)finger].GrabStrength;
        }

        public void Update(IHand hand)
        {
            ClearState();

            if (hand == null || !hand.IsTrackedDataValid)
            {
                return;
            }

            UpdateVolumeCenter(hand);

            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersGrabData[i].UpdateGrabStrength(hand, _fingerShapes);
                _fingersGrabData[i].UpdateIsGrabbing(START_THRESHOLD, RELEASE_THRESHOLD);
            }
        }

        private void UpdateVolumeCenter(IHand hand)
        {
            _poseVolumeCenterOffset = hand.Handedness == Handedness.Left
                ? POSE_VOLUME_OFFSET_LEFT
                : POSE_VOLUME_OFFSET_RIGHT;
        }

        private void ClearState()
        {
            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersGrabData[i].ClearState();
            }
        }

        public Vector3 GetCenterOffset()
        {
            return _poseVolumeCenterOffset;
        }
    }
}
