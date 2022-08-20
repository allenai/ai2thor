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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class HandShapeSkeletalDebugVisual : MonoBehaviour
    {
        [SerializeField]
        private ShapeRecognizerActiveState _shapeRecognizerActiveState;

        [SerializeField]
        private GameObject _fingerFeatureDebugVisualPrefab;

        protected virtual void Awake()
        {
            Assert.IsNotNull(_shapeRecognizerActiveState);
            Assert.IsNotNull(_fingerFeatureDebugVisualPrefab);
        }

        protected virtual void Start()
        {
            var statesByFinger = AllFeatureStates()
                .GroupBy(s => s.Item1)
                .Select(group => new
                {
                    HandFinger = group.Key,
                    FingerFeatures = group.SelectMany(item => item.Item2)
                });
            foreach (var g in statesByFinger)
            {
                foreach (var feature in g.FingerFeatures)
                {
                    var boneDebugObject = Instantiate(_fingerFeatureDebugVisualPrefab);
                    var skeletalComp = boneDebugObject.GetComponent<FingerFeatureSkeletalDebugVisual>();

                    skeletalComp.Initialize(_shapeRecognizerActiveState.Hand, g.HandFinger, feature);

                    var debugVisTransform = boneDebugObject.transform;

                    debugVisTransform.parent = this.transform;

                    debugVisTransform.localScale = Vector3.one;
                    debugVisTransform.localRotation = Quaternion.identity;
                    debugVisTransform.localPosition = Vector3.zero;
                }
            }
        }

        private IEnumerable<ValueTuple<HandFinger, IReadOnlyList<ShapeRecognizer.FingerFeatureConfig>>> AllFeatureStates()
        {
            foreach (ShapeRecognizer shapeRecognizer in _shapeRecognizerActiveState.Shapes)
            {
                foreach (var handFingerConfigs in shapeRecognizer.GetFingerFeatureConfigs())
                {
                    yield return handFingerConfigs;
                }
            }
        }
    }
}
