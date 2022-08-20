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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class RayInteractorPinchVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;

        private IHand Hand;

        [SerializeField]
        private RayInteractor _rayInteractor;

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        [SerializeField]
        AnimationCurve _remapCurve;

        [SerializeField]
        Vector2 _alphaRange = new Vector2(.1f, .4f);

        #region Properties

        public AnimationCurve RemapCurve
        {
            get
            {
                return _remapCurve;
            }
            set
            {
                _remapCurve = value;
            }
        }

        public Vector2 AlphaRange
        {
            get
            {
                return _alphaRange;
            }
            set
            {
                _alphaRange = value;
            }
        }

        #endregion

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_skinnedMeshRenderer);
            Assert.IsNotNull(_remapCurve);
            Assert.IsNotNull(_rayInteractor);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed += UpdateVisual;
                _rayInteractor.WhenStateChanged += UpdateVisualState;
                UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed -= UpdateVisual;
                _rayInteractor.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisual()
        {
            if (!Hand.IsTrackedDataValid ||
                _rayInteractor.State == InteractorState.Disabled)
            {
                if (_skinnedMeshRenderer.enabled) _skinnedMeshRenderer.enabled = false;
                return;
            }

            if (!_skinnedMeshRenderer.enabled) _skinnedMeshRenderer.enabled = true;

            if (!Hand.GetJointPose(HandJointId.HandIndex3, out var poseIndex3)) return;
            if (!Hand.GetJointPose(HandJointId.HandThumb3, out var poseThumb3)) return;

            var isPinching = _rayInteractor.State == InteractorState.Select;
            Vector3 midIndexThumb = Vector3.Lerp(poseThumb3.position, poseIndex3.position, 0.5f);

            var thisTransform = transform;
            var deltaTarget = (_rayInteractor.End - thisTransform.position).normalized;

            thisTransform.position = midIndexThumb;
            thisTransform.rotation = Quaternion.LookRotation(deltaTarget, Vector3.up);
            thisTransform.localScale = Vector3.one * Hand.Scale;

            var mappedPinchStrength = _remapCurve.Evaluate(Hand.GetFingerPinchStrength(HandFinger.Index));

            _skinnedMeshRenderer.material.color = isPinching ? Color.white : new Color(1f, 1f, 1f, Mathf.Lerp(_alphaRange.x, _alphaRange.y, mappedPinchStrength));
            _skinnedMeshRenderer.SetBlendShapeWeight(0, mappedPinchStrength * 100f);
            _skinnedMeshRenderer.SetBlendShapeWeight(1, mappedPinchStrength * 100f);
        }

        private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

        #region Inject

        public void InjectAllRayInteractorPinchVisual(IHand hand,
            RayInteractor rayInteractor,
            SkinnedMeshRenderer skinnedMeshRenderer)
        {
            InjectHand(hand);
            InjectRayInteractor(rayInteractor);
            InjectSkinnedMeshRenderer(skinnedMeshRenderer);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        public void InjectSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            _skinnedMeshRenderer = skinnedMeshRenderer;
        }

        #endregion
    }
}
