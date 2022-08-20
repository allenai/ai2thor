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
    public class ActiveStateSelector : MonoBehaviour, ISelector
    {
        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _activeState;
        protected IActiveState ActiveState { get; private set; }
        private bool _selecting = false;

        public event Action WhenSelected = delegate { };
        public event Action WhenUnselected = delegate { };

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(ActiveState);
        }

        protected virtual void Update()
        {
            if (_selecting != ActiveState.Active)
            {
                _selecting = ActiveState.Active;
                if (_selecting)
                {
                    WhenSelected();
                }
                else
                {
                    WhenUnselected();
                }
            }
        }

        #region Inject

        public void InjectAllActiveStateSelector(IActiveState activeState)
        {
            InjectActiveState(activeState);
        }

        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as MonoBehaviour;
            ActiveState = activeState;
        }
        #endregion
    }
}
