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
    public class PointableDebugVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private MonoBehaviour _pointable;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _hoverColor = Color.blue;

        [SerializeField]
        private Color _selectColor = Color.green;

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

        private IPointable Pointable;
        private Material _material;
        private bool _hover = false;
        private bool _select = false;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Pointable = _pointable as IPointable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Pointable);
            Assert.IsNotNull(_renderer);

            _material = _renderer.material;
            _material.color = _normalColor;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised += HandlePointerEventRaised;
                UpdateMaterialColor();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Hover:
                    _hover = true;
                    UpdateMaterialColor();
                    break;
                case PointerEventType.Select:
                    _select = true;
                    UpdateMaterialColor();
                    break;
                case PointerEventType.Move:
                    break;
                case PointerEventType.Unselect:
                    _select = false;
                    UpdateMaterialColor();
                    break;
                case PointerEventType.Unhover:
                    _hover = false;
                    UpdateMaterialColor();
                    break;
            }
        }

        private void UpdateMaterialColor()
        {
            _material.color = _select ? _selectColor : (_hover ? _hoverColor : _normalColor);
        }

        #region Inject

        public void InjectAllPointableDebugVisual(IPointable pointable, Renderer renderer)
        {
            InjectPointable(pointable);
            InjectRenderer(renderer);
        }

        public void InjectPointable(IPointable pointable)
        {
            _pointable = pointable as MonoBehaviour;
            Pointable = pointable;
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}
