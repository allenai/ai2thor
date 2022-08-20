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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class InteractableColorVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private MonoBehaviour _interactableView;

        [SerializeField]
        private MaterialPropertyBlockEditor _editor;

        [SerializeField]
        private string _colorShaderPropertyName = "_Color";

        [Serializable]
        public class ColorState
        {
            public Color Color = Color.white;
            public AnimationCurve ColorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            public float ColorTime = 0.1f;
        }

        [SerializeField]
        private ColorState _normalColorState;

        [SerializeField]
        private ColorState _hoverColorState;

        [SerializeField]
        private ColorState _selectColorState;

        private ColorState _targetState;

        private Color _currentColor;
        private Color _startColor;

        private float _timer;

        private IInteractableView InteractableView;
        private int _colorShaderID;

        protected bool _started = false;

        protected virtual void Awake()
        {
            InteractableView = _interactableView as IInteractableView;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(InteractableView);

            Assert.IsNotNull(_editor);

            _targetState = _normalColorState;
            _startColor = _currentColor = _normalColorState.Color;
            _timer = _normalColorState.ColorTime;

            _colorShaderID = Shader.PropertyToID(_colorShaderPropertyName);

            UpdateVisual();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                UpdateVisual();
                InteractableView.WhenStateChanged += UpdateVisualState;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractableView.WhenStateChanged -= UpdateVisualState;
            }
        }

        protected virtual void UpdateVisual()
        {
            switch (InteractableView.State)
            {
                case InteractableState.Select:
                    _targetState = _selectColorState;
                    break;
                case InteractableState.Hover:
                    _targetState = _hoverColorState;
                    break;
                default:
                    _targetState = _normalColorState;
                    break;
            }
            _timer = 0.0f;
            _startColor = _currentColor;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float _normalizedTimer = Mathf.Clamp01(_timer / _targetState.ColorTime);
            float t = _targetState.ColorCurve.Evaluate(_normalizedTimer);
            _currentColor = Color.Lerp(_startColor, _targetState.Color, t);
            _editor.MaterialPropertyBlock.SetColor(_colorShaderID, _currentColor);
        }

        private void UpdateVisualState(InteractableStateChangeArgs args) => UpdateVisual();

        #region Inject

        public void InjectAllInteractableColorVisual(IInteractableView interactableView,
                                                     MaterialPropertyBlockEditor editor)
        {
            InjectInteractableView(interactableView);
            InjectMaterialPropertyBlockEditor(editor);
        }

        public void InjectInteractableView(IInteractableView interactableview)
        {
            _interactableView = interactableview as MonoBehaviour;
            InteractableView = interactableview;
        }

        public void InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor editor)
        {
            _editor = editor;
        }

        public void InjectOptionalColorShaderPropertyName(string colorShaderPropertyName)
        {
            _colorShaderPropertyName = colorShaderPropertyName;
        }

        public void InjectOptionalNormalColorState(ColorState normalColorState)
        {
            _normalColorState = normalColorState;
        }

        public void InjectOptionalHoverColorState(ColorState hoverColorState)
        {
            _hoverColorState = hoverColorState;
        }

        public void InjectOptionalSelectColorState(ColorState selectColorState)
        {
            _selectColorState = selectColorState;
        }

        #endregion
    }
}
