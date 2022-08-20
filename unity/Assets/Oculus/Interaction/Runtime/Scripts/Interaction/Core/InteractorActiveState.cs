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
    public class InteractorActiveState : MonoBehaviour, IActiveState
    {
        [System.Flags]
        public enum InteractorProperty
        {
            HasCandidate = 1 << 0,
            HasInteractable = 1 << 1,
            IsSelecting = 1 << 2,
            HasSelectedInteractable = 1 << 3,
        }

        [SerializeField, Interface(typeof(IInteractor))]
        private MonoBehaviour _interactor;
        private IInteractor Interactor;

        [SerializeField]
        private InteractorProperty _property;

        public InteractorProperty Property
        {
            get
            {
                return _property;
            }
            set
            {
                _property = value;
            }
        }

        public bool Active
        {
            get
            {
                if((_property & InteractorProperty.HasCandidate) != 0
                    && Interactor.HasCandidate)
                {
                    return true;
                }
                if((_property & InteractorProperty.HasInteractable) != 0
                    && Interactor.HasInteractable)
                {
                    return true;
                }
                if((_property & InteractorProperty.IsSelecting) != 0
                    && Interactor.State == InteractorState.Select)
                {
                    return true;
                }
                if((_property & InteractorProperty.HasSelectedInteractable) != 0
                    && Interactor.HasSelectedInteractable)
                {
                    return true;
                }
                return false;
            }
        }

        protected virtual void Awake()
        {
            Interactor = _interactor as IInteractor;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Interactor);
        }

        #region Inject

        public void InjectAllInteractorActiveState(IInteractor interactor)
        {
            InjectInteractor(interactor);
        }

        public void InjectInteractor(IInteractor interactor)
        {
            _interactor = interactor as MonoBehaviour;
            Interactor = interactor;
        }
        #endregion
    }
}
