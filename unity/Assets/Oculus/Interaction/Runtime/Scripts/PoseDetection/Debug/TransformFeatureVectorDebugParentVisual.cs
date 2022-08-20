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
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class TransformFeatureVectorDebugParentVisual : MonoBehaviour
    {
        [SerializeField]
        private TransformRecognizerActiveState _transformRecognizerActiveState;
        [SerializeField]
        private GameObject _vectorVisualPrefab;

        public void GetTransformFeatureVectorAndWristPos(TransformFeature feature,
            bool isHandVector, ref Vector3? featureVec, ref Vector3? wristPos)
        {
            _transformRecognizerActiveState.GetFeatureVectorAndWristPos(feature, isHandVector,
                ref featureVec, ref wristPos);
        }

        protected virtual void Awake()
        {
            Assert.IsNotNull(_transformRecognizerActiveState);
            Assert.IsNotNull(_vectorVisualPrefab);
        }

        protected virtual void Start()
        {
            var featureConfigs = _transformRecognizerActiveState.FeatureConfigs;
            foreach (var featureConfig in featureConfigs)
            {
                var feature = featureConfig.Feature;
                CreateVectorDebugView(feature, false);
                CreateVectorDebugView(feature, true);
            }
        }

        private void CreateVectorDebugView(TransformFeature feature, bool trackingHandVector)
        {
            var featureDebugVis = Instantiate(_vectorVisualPrefab, this.transform);
            var debugVisComp = featureDebugVis.GetComponent<TransformFeatureVectorDebugVisual>();

            debugVisComp.Initialize(feature, trackingHandVector, this, trackingHandVector ?
                Color.blue : Color.black);
            var debugVisTransform = debugVisComp.transform;
            debugVisTransform.localRotation = Quaternion.identity;
            debugVisTransform.localPosition = Vector3.zero;
        }
    }
}
