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
    public class ActiveStateDebugVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _activeState;
        private IActiveState ActiveState { get; set; }

        [SerializeField]
        private Renderer _target;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _activeColor = Color.green;

        private Material _material;
        private bool _lastActiveValue = false;

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
            Assert.IsNotNull(ActiveState);
            Assert.IsNotNull(_target);
            _material = _target.material;

            SetMaterialColor(_lastActiveValue ? _activeColor : _normalColor);
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        protected virtual void Update()
        {
            bool isActive = ActiveState.Active;
            if (_lastActiveValue != isActive)
            {
                SetMaterialColor(isActive ? _activeColor : _normalColor);
                _lastActiveValue = isActive;
            }
        }

        private void SetMaterialColor(Color activeColor)
        {
            _material.color = activeColor;
            _target.enabled = _material.color.a > 0.0f;
        }
    }
}
