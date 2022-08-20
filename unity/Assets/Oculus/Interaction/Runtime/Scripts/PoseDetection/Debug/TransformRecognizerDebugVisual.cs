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
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class TransformRecognizerDebugVisual : MonoBehaviour
    {
        [SerializeField]
        private Hand _hand;

        [SerializeField]
        private TransformRecognizerActiveState[] _transformRecognizerActiveStates;

        [SerializeField]
        private Renderer _target;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _activeColor = Color.green;

        [SerializeField]
        private GameObject _transformFeatureDebugVisualPrefab;

        [SerializeField]
        private Transform _debugVisualParent;

        [SerializeField]
        private Vector3 _featureSpacingVec = new Vector3(1.0f, 0.0f, 0.0f);

        [SerializeField]
        private Vector3 _featureDebugLocalScale = new Vector3(0.3f, 0.3f, 0.3f);

        [SerializeField]
        private TextMeshPro _targetText;

        private Material _material;
        private bool _lastActiveValue = false;

        protected virtual void Awake()
        {
            Assert.IsNotNull(_hand);
            Assert.IsTrue(_transformRecognizerActiveStates != null &&
                _transformRecognizerActiveStates.Length > 0);
            Assert.IsNotNull(_target);
            Assert.IsNotNull(_transformFeatureDebugVisualPrefab);
            Assert.IsNotNull(_targetText);
            _material = _target.material;

            _material.color = _lastActiveValue ? _activeColor : _normalColor;

            if (_debugVisualParent == null)
            {
                _debugVisualParent = transform;
            }
        }

        protected virtual void Start()
        {

            Vector3 totalDisp = Vector3.zero;
            string shapeNames = "";

            foreach (var activeState in _transformRecognizerActiveStates)
            {
                bool foundAspect = activeState.Hand.GetHandAspect(out TransformFeatureStateProvider stateProvider);
                Assert.IsTrue(foundAspect);

                var featureConfigs = activeState.FeatureConfigs;
                foreach (var featureConfig in featureConfigs)
                {
                    var featureDebugVis = Instantiate(_transformFeatureDebugVisualPrefab, _debugVisualParent);
                    var debugVisComp = featureDebugVis.GetComponent<TransformFeatureDebugVisual>();

                    debugVisComp.Initialize(activeState.Hand.Handedness, featureConfig, stateProvider,
                        activeState);
                    var debugVisTransform = debugVisComp.transform;
                    debugVisTransform.localScale = _featureDebugLocalScale;
                    debugVisTransform.localRotation = Quaternion.identity;
                    debugVisTransform.localPosition = totalDisp;

                    totalDisp += _featureSpacingVec;

                    if (!String.IsNullOrEmpty(shapeNames)) { shapeNames += "\n  "; }
                    shapeNames += $"{featureConfig.Mode} {featureConfig.State} ({activeState.Hand.Handedness})";
                }
            }

            _targetText.text = $"{shapeNames}";
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        private bool AllActive()
        {
            foreach (var activeState in _transformRecognizerActiveStates)
            {
                if (!activeState.Active)
                {
                    return false;
                }
            }
            return true;
        }

        protected virtual void Update()
        {
            bool isActive = AllActive();
            if (_lastActiveValue != isActive)
            {
                _material.color = isActive ? _activeColor : _normalColor;
                _lastActiveValue = isActive;
            }
        }
    }
}
