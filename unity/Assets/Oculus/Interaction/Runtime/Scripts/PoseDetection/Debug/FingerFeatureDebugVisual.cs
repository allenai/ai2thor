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
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class FingerFeatureDebugVisual : MonoBehaviour
    {
        [SerializeField]
        private Renderer _target;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _activeColor = Color.green;

        [SerializeField]
        private TextMeshPro _targetText;

        private FingerFeatureStateProvider _fingerFeatureState;

        private Material _material;

        private bool _lastActiveValue;
        private HandFinger _handFinger;
        private ShapeRecognizer.FingerFeatureConfig _featureConfig;
        private bool _initialized;

        protected virtual void Awake()
        {
            _material = _target.material;
            Assert.IsNotNull(_material);
            Assert.IsNotNull(_targetText);

            _material.color = _lastActiveValue ? _activeColor : _normalColor;
        }

        protected virtual void OnDestroy()
        {
            Destroy(_material);
        }

        public void Initialize(HandFinger handFinger,
            ShapeRecognizer.FingerFeatureConfig config,
            FingerFeatureStateProvider fingerFeatureState)
        {
            _initialized = true;
            _handFinger = handFinger;
            _featureConfig = config;
            _fingerFeatureState = fingerFeatureState;
        }

        protected virtual void Update()
        {
            if (!_initialized)
            {
                return;
            }

            FingerFeature feature = _featureConfig.Feature;
            bool isActive = false;
            if (_fingerFeatureState.GetCurrentState(_handFinger, feature,
                out string currentState))
            {
                float? featureVal = _fingerFeatureState.GetFeatureValue(_handFinger, feature);
                isActive = _fingerFeatureState.IsStateActive(_handFinger, feature, _featureConfig.Mode, _featureConfig.State);
                string featureValStr = featureVal.HasValue ? featureVal.Value.ToString("F2") : "--";
                _targetText.text = $"{_handFinger} {feature}" + $"{currentState} ({featureValStr})";
            }
            else
            {
                _targetText.text = $"{_handFinger} {feature}\n";
            }

            if (isActive != _lastActiveValue)
            {
                _material.color = isActive ? _activeColor : _normalColor;
                _lastActiveValue = isActive;
            }

        }
    }
}
