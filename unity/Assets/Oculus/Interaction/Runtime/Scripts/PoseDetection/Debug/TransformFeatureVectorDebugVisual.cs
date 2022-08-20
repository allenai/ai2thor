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
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class TransformFeatureVectorDebugVisual : MonoBehaviour
    {
        public IHand Hand { get; private set; }

        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private float _lineWidth = 0.005f;

        [SerializeField]
        private float _lineScale = 0.1f;

        private bool _isInitialized = false;
        private TransformFeature _feature;
        private TransformFeatureVectorDebugParentVisual _parent;
        private bool _trackingHandVector = false;

        protected virtual void Awake()
        {
            Assert.IsNotNull(_lineRenderer);

            _lineRenderer.enabled = false;
        }

        public void Initialize(TransformFeature feature,
            bool trackingHandVector,
            TransformFeatureVectorDebugParentVisual parent,
            Color lineColor)
        {
            _isInitialized = true;
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;
            _feature = feature;
            _trackingHandVector = trackingHandVector;
            _parent = parent;
        }

        protected virtual void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            Vector3? featureVec = null;
            Vector3? wristPos = null;
            _parent.GetTransformFeatureVectorAndWristPos(_feature,
                _trackingHandVector, ref featureVec, ref wristPos);

            if (featureVec == null || wristPos == null)
            {
                if (_lineRenderer.enabled)
                {
                    _lineRenderer.enabled = false;
                }
                return;
            }

            if (!_lineRenderer.enabled)
            {
                _lineRenderer.enabled = true;
            }
            if (Mathf.Abs(_lineRenderer.startWidth - _lineWidth) > Mathf.Epsilon)
            {
                _lineRenderer.startWidth = _lineWidth;
                _lineRenderer.endWidth = _lineWidth;
            }
            _lineRenderer.SetPosition(0, wristPos.Value);
            _lineRenderer.SetPosition(1, wristPos.Value + _lineScale*featureVec.Value);
        }
    }
}
