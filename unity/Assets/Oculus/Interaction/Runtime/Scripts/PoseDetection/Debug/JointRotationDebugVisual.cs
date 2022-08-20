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
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class JointRotationDebugVisual : MonoBehaviour
    {
        [SerializeField]
        private JointRotationActiveState _jointRotation;

        [SerializeField]
        private Material _lineRendererMaterial;

        [SerializeField]
        private float _rendererLineWidth = 0.005f;

        [SerializeField]
        private float _rendererLineLength = 0.1f;

        private List<LineRenderer> _lineRenderers;
        private int _enabledRendererCount;

        protected bool _started = false;

        protected virtual void Awake()
        {
            _lineRenderers = new List<LineRenderer>();
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_jointRotation);
            Assert.IsNotNull(_lineRendererMaterial);
            this.EndStart(ref _started);
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                ResetLines();
            }
        }

        protected virtual void Update()
        {
            ResetLines();
            foreach (var config in _jointRotation.FeatureConfigs)
            {
                if (_jointRotation.Hand.GetJointPose(config.Feature, out Pose jointPose) &&
                    _jointRotation.FeatureStates.TryGetValue(config, out var state))
                {
                    DrawDebugLine(jointPose.position, state.TargetAxis, state.Amount);
                }
            }
        }

        private void DrawDebugLine(Vector3 jointPos, Vector3 direction, float amount)
        {
            Vector3 fullLength = direction.normalized * _rendererLineLength;
            bool metThreshold = amount >= 1f;

            if (metThreshold)
            {
                AddLine(jointPos, jointPos + fullLength, Color.green);
            }
            else
            {
                Vector3 breakpoint = Vector3.Lerp(jointPos, jointPos + fullLength, amount);
                AddLine(jointPos, breakpoint, Color.yellow);
                AddLine(breakpoint, jointPos + fullLength, Color.red);
            }
        }

        private void ResetLines()
        {
            foreach (var lineRenderer in _lineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
            }
            _enabledRendererCount = 0;
        }

        private void AddLine(Vector3 start, Vector3 end, Color color)
        {
            LineRenderer lineRenderer;
            if (_enabledRendererCount == _lineRenderers.Count)
            {
                lineRenderer = new GameObject().AddComponent<LineRenderer>();
                lineRenderer.startWidth = _rendererLineWidth;
                lineRenderer.endWidth = _rendererLineWidth;
                lineRenderer.positionCount = 2;
                lineRenderer.material = _lineRendererMaterial;
                _lineRenderers.Add(lineRenderer);
            }
            else
            {
                lineRenderer = _lineRenderers[_enabledRendererCount];
            }

            _enabledRendererCount++;

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
