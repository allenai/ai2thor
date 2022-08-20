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

namespace Oculus.Interaction
{
    public class RayInteractorDebugGizmos : MonoBehaviour
    {
        [SerializeField]
        private RayInteractor _rayInteractor;

        [SerializeField]
        private float _rayWidth = 0.01f;

        [SerializeField]
        private Color _normalColor = Color.red;

        [SerializeField]
        private Color _hoverColor = Color.blue;

        [SerializeField]
        private Color _selectColor = Color.green;

        public float RayWidth
        {
            get
            {
                return _rayWidth;
            }
            set
            {
                _rayWidth = value;
            }
        }

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

        protected virtual void Start()
        {
            Assert.IsNotNull(_rayInteractor);
        }

        private void LateUpdate()
        {
            if (_rayInteractor.State == InteractorState.Disabled)
            {
                return;
            }

            switch (_rayInteractor.State)
            {
                case InteractorState.Normal:
                    DebugGizmos.Color = _normalColor;
                    break;
                case InteractorState.Hover:
                    DebugGizmos.Color = _hoverColor;
                    break;
                case InteractorState.Select:
                    DebugGizmos.Color = _selectColor;
                    break;
                case InteractorState.Disabled:
                    return;
            }

            DebugGizmos.LineWidth = _rayWidth;
            DebugGizmos.DrawLine(_rayInteractor.Origin, _rayInteractor.End);
        }

        #region Inject

        public void InjectAllRayInteractorDebugGizmos(RayInteractor rayInteractor)
        {
            InjectRayInteractor(rayInteractor);
        }

        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        #endregion
    }
}
