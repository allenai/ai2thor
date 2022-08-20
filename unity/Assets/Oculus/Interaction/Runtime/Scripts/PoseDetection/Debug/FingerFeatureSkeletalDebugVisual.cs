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
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class FingerFeatureSkeletalDebugVisual : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _activeColor = Color.green;

        [SerializeField]
        private float _lineWidth = 0.005f;

        private IHand _hand;
        private FingerFeatureStateProvider _featureState;

        private bool _lastFeatureActiveValue = false;

        private IReadOnlyList<HandJointId> _jointsCovered = null;
        private HandFinger _finger;
        private ShapeRecognizer.FingerFeatureConfig _fingerFeatureConfig;
        private bool _initializedPositions;
        private bool _initialized;

        protected virtual void Awake()
        {
            Assert.IsNotNull(_lineRenderer);
            UpdateFeatureActiveValueAndVisual(false);
        }

        private void UpdateFeatureActiveValueAndVisual(bool newValue)
        {
            var colorToUse = newValue ? _activeColor : _normalColor;
            _lineRenderer.startColor = colorToUse;
            _lineRenderer.endColor = colorToUse;
            _lastFeatureActiveValue = newValue;
        }

        public void Initialize(
            IHand hand,
            HandFinger finger,
            ShapeRecognizer.FingerFeatureConfig fingerFeatureConfig)
        {
            _hand = hand;
            _initialized = true;

            bool foundAspect = hand.GetHandAspect(out _featureState);
            Assert.IsTrue(foundAspect);

            var featureValueProvider = _featureState.GetValueProvider(finger);

            _jointsCovered = featureValueProvider.GetJointsAffected(
                finger,
                fingerFeatureConfig.Feature);
            _finger = finger;
            _fingerFeatureConfig = fingerFeatureConfig;

            _initializedPositions = false;
        }

        protected virtual void Update()
        {
            if (!_initialized || !_hand.IsTrackedDataValid)
            {
                ToggleLineRendererEnableState(false);
                return;
            }

            ToggleLineRendererEnableState(true);
            UpdateDebugSkeletonLineRendererJoints();
            UpdateFeatureActiveValue();
        }

        private void ToggleLineRendererEnableState(bool enableState)
        {
            if (_lineRenderer.enabled == enableState)
            {
                return;
            }
            _lineRenderer.enabled = enableState;
        }

        private void UpdateDebugSkeletonLineRendererJoints()
        {
            if (!_initializedPositions)
            {
                _lineRenderer.positionCount = _jointsCovered.Count;
                _initializedPositions = true;
            }

            if (Mathf.Abs(_lineRenderer.startWidth - _lineWidth) > Mathf.Epsilon)
            {
                _lineRenderer.startWidth = _lineWidth;
                _lineRenderer.endWidth = _lineWidth;
            }

            int numJoints = _jointsCovered.Count;
            for (int i = 0; i < numJoints; i++)
            {
                if (_hand.GetJointPose(_jointsCovered[i], out Pose jointPose))
                {
                    _lineRenderer.SetPosition(i, jointPose.position);
                }
            }
        }

        private void UpdateFeatureActiveValue()
        {
            bool isActive = _featureState.IsStateActive(_finger, _fingerFeatureConfig.Feature,
                _fingerFeatureConfig.Mode, _fingerFeatureConfig.State);
            if (isActive != _lastFeatureActiveValue)
            {
                UpdateFeatureActiveValueAndVisual(isActive);
            }
        }
    }
}
