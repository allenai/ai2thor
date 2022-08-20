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
    public class SelectorDebugVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(ISelector))]
        private MonoBehaviour _selector;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Color _normalColor = Color.red;

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

        private ISelector Selector;
        private Material _material;
        private bool _selected = false;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Selector = _selector as ISelector;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Selector);

            Assert.IsNotNull(_renderer);
            _material = _renderer.material;
            _material.color = _normalColor;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Selector.WhenSelected += HandleSelected;
                Selector.WhenUnselected += HandleUnselected;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                HandleUnselected();
                Selector.WhenSelected -= HandleSelected;
                Selector.WhenUnselected -= HandleUnselected;
            }
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        private void HandleSelected()
        {
            if (_selected) return;
            _selected = true;

            _material.color = _selectColor;

        }
        private void HandleUnselected()
        {
            if (!_selected) return;
            _selected = false;

            _material.color = _normalColor;
        }

        #region Inject

        public void InjectAllSelectorDebugVisual(ISelector selector, Renderer renderer)
        {
            InjectSelector(selector);
            InjectRenderer(renderer);
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as MonoBehaviour;
            Selector = selector;
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}
