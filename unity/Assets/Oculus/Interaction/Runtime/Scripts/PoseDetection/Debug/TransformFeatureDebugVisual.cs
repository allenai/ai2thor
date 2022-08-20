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
    public class TransformFeatureDebugVisual : MonoBehaviour
    {
        [SerializeField]
        private Renderer _target;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _activeColor = Color.green;

        [SerializeField]
        private TextMeshPro _targetText;

        private TransformFeatureStateProvider _transformFeatureStateProvider;
        private TransformRecognizerActiveState _transformRecognizerActiveState;

        private Material _material;

        private bool _lastActiveValue;
        private TransformFeatureConfig _targetConfig;
        private bool _initialized;
        private Handedness _handedness;

        protected virtual void Awake()
        {
            _material = _target.material;
            Assert.IsNotNull(_material);
            Assert.IsNotNull(_targetText);

            _material.color = _lastActiveValue ? _activeColor : _normalColor;
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        public void Initialize(
            Handedness handedness,
            TransformFeatureConfig targetConfig,
            TransformFeatureStateProvider transformFeatureStateProvider,
            TransformRecognizerActiveState transformActiveState)
        {
            _handedness = handedness;
            _initialized = true;
            _transformFeatureStateProvider = transformFeatureStateProvider;
            _transformRecognizerActiveState = transformActiveState;
            _targetConfig = targetConfig;
        }

        protected virtual void Update()
        {
            if (!_initialized)
            {
                return;
            }

            bool isActive = false;
            TransformFeature feature = _targetConfig.Feature;
            if (_transformFeatureStateProvider.GetCurrentState(
                _transformRecognizerActiveState.TransformConfig,
                feature,
                out string currentState))
            {
                float? featureVal = _transformFeatureStateProvider.GetFeatureValue(
                    _transformRecognizerActiveState.TransformConfig, feature);

                isActive = _transformFeatureStateProvider.IsStateActive(
                    _transformRecognizerActiveState.TransformConfig,
                    feature,
                    _targetConfig.Mode,
                    _targetConfig.State);

                string featureValStr = featureVal.HasValue ? featureVal.Value.ToString("F2") : "--";
                _targetText.text = $"{feature}\n" +
                                   $"{currentState} ({featureValStr})";
            }
            else
            {
                _targetText.text = $"{feature}\n";
            }

            if (isActive != _lastActiveValue)
            {
                _material.color = isActive ? _activeColor : _normalColor;
                _lastActiveValue = isActive;
            }
        }
    }
}
