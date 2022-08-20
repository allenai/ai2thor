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

namespace Oculus.Interaction.DistanceReticles
{
    public abstract class InteractorReticle<TReticleData> : MonoBehaviour
        where TReticleData : IReticleData
    {
        [SerializeField]
        private bool _visibleDuringSelect = false;
        private bool VisibleDuringSelect
        {
            get
            {
                return _visibleDuringSelect;
            }
            set
            {
                _visibleDuringSelect = value;
            }
        }

        protected abstract IInteractorView Interactor { get; }

        private TReticleData _targetData;
        private bool _drawing;
        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Interactor);
            Hide();
            this.EndStart(ref _started);
        }
        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged += HandleStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            if (args.NewState == InteractorState.Normal
                && args.PreviousState != InteractorState.Disabled)
            {
                InteractableUnset();
            }
            else if(args.NewState == InteractorState.Select)
            {
                if (!_visibleDuringSelect)
                {
                    InteractableUnset();
                }
            }
            else if (args.NewState == InteractorState.Hover)
            {
                InteractableSet(Interactor.Candidate as MonoBehaviour);
            }
        }

        #region Drawing
        protected abstract void Draw(TReticleData data);
        protected abstract void Hide();
        protected abstract void Align(TReticleData data);
        #endregion

        private void InteractableSet(MonoBehaviour interactableComponent)
        {
            if (interactableComponent != null
                && interactableComponent.TryGetComponent(out TReticleData reticleData))
            {
                _targetData = reticleData;
                Draw(reticleData);
                Align(reticleData);
                _drawing = true;
            }
        }

        private void InteractableUnset()
        {
            if (_drawing)
            {
                Hide();
                _targetData = default(TReticleData);
                _drawing = false;
            }
        }

        protected virtual void LateUpdate()
        {
            if (_drawing)
            {
                Align(_targetData);
            }
        }
    }
}
