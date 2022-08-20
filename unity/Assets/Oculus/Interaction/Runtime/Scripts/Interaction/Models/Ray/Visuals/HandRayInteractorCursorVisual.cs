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
    public class HandRayInteractorCursorVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand;

        [SerializeField]
        private RayInteractor _rayInteractor;

        [SerializeField]
        private GameObject _cursor;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Color _outlineColor = Color.black;

        [SerializeField]
        private float _offsetAlongNormal = 0.005f;

        #region Properties

        public Color OutlineColor
        {
            get
            {
                return _outlineColor;
            }
            set
            {
                _outlineColor = value;
            }
        }

        public float OffsetAlongNormal
        {
            get
            {
                return _offsetAlongNormal;
            }
            set
            {
                _offsetAlongNormal = value;
            }
        }

        #endregion

        private int _shaderRadialGradientScale = Shader.PropertyToID("_RadialGradientScale");
        private int _shaderRadialGradientIntensity = Shader.PropertyToID("_RadialGradientIntensity");
        private int _shaderRadialGradientBackgroundOpacity = Shader.PropertyToID("_RadialGradientBackgroundOpacity");
        private int _shaderOutlineColor = Shader.PropertyToID("_OutlineColor");

        [SerializeField]
        private GameObject _selectObject;

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Hand = _hand as IHand;
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_rayInteractor);
            Assert.IsNotNull(_renderer);
            Assert.IsNotNull(_cursor);
            Assert.IsNotNull(_selectObject);
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
            if (_rayInteractor.State == InteractorState.Disabled)
            {
                _cursor.SetActive(false);
                return;
            }

            if (_rayInteractor.CollisionInfo == null)
            {
                _cursor.SetActive(false);
                return;
            }

            if (!_cursor.activeSelf)
            {
                _cursor.SetActive(true);
            }

            Vector3 collisionNormal = _rayInteractor.CollisionInfo.Value.Normal;
            this.transform.position = _rayInteractor.End + collisionNormal * _offsetAlongNormal;
            this.transform.rotation = Quaternion.LookRotation(_rayInteractor.CollisionInfo.Value.Normal, Vector3.up);

            if (_rayInteractor.State == InteractorState.Select)
            {
                _selectObject.SetActive(true);
                _renderer.material.SetFloat(_shaderRadialGradientScale, 0.25f);
                _renderer.material.SetFloat(_shaderRadialGradientIntensity, 1f);
                _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, 1f);
                _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
            }
            else
            {
                _selectObject.SetActive(false);
                var mappedPinchStrength = Hand.GetFingerPinchStrength(HandFinger.Index);
                var radialScale = 1f - mappedPinchStrength;
                radialScale = Mathf.Max(radialScale, .11f);
                _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
                _renderer.material.SetFloat(_shaderRadialGradientIntensity, mappedPinchStrength);
                _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchStrength));
                _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
            }
        }

        private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

        #region Inject

        public void InjectAllHandRayInteractorCursorVisual(IHand hand,
            RayInteractor rayInteractor,
            GameObject cursor,
            Renderer renderer)
        {
            InjectHand(hand);
            InjectRayInteractor(rayInteractor);
            InjectCursor(cursor);
            InjectRenderer(renderer);
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

        public void InjectCursor(GameObject cursor)
        {
            _cursor = cursor;
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}
