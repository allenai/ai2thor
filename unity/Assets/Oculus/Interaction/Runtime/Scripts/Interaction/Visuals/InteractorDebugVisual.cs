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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class InteractorDebugVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractorView))]
        private MonoBehaviour _interactorView;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _hoverColor = Color.blue;

        [SerializeField]
        private Color _selectColor = Color.green;

        [SerializeField]
        private Color _disabledColor = Color.black;

        public Color NormalColor
        {
            get
            {
                return _normalColor;
            }
            set
            {
                _normalColor = value;
            }
        }

        public Color HoverColor
        {
            get
            {
                return _hoverColor;
            }
            set
            {
                _hoverColor = value;
            }
        }

        public Color SelectColor
        {
            get
            {
                return _selectColor;
            }
            set
            {
                _selectColor = value;
            }
        }

        public Color DisabledColor
        {
            get
            {
                return _disabledColor;
            }
            set
            {
                _disabledColor = value;
            }
        }


        private IInteractorView InteractorView;
        private Material _material;

        protected bool _started = false;

        protected virtual void Awake()
        {
            InteractorView = _interactorView as IInteractorView;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(InteractorView);
            Assert.IsNotNull(_renderer);

            _material = _renderer.material;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                InteractorView.WhenStateChanged += UpdateVisualState;
                UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractorView.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisual()
        {
            switch (InteractorView.State)
            {
                case InteractorState.Select:
                    _material.color = _selectColor;
                    break;
                case InteractorState.Hover:
                    _material.color = _hoverColor;
                    break;
                case InteractorState.Normal:
                    _material.color = _normalColor;
                    break;
                case InteractorState.Disabled:
                    _material.color = _disabledColor;
                    break;
            }
        }

        private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

        private void OnDestroy()
        {
            Destroy(_material);
        }

        #region Inject

        public void InjectAllInteractorDebugVisual(IInteractorView interactorView, Renderer renderer)
        {
            InjectInteractorView(interactorView);
            InjectRenderer(renderer);
        }

        public void InjectInteractorView(IInteractorView interactorView)
        {
            _interactorView = interactorView as MonoBehaviour;
            InteractorView = interactorView;
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}
