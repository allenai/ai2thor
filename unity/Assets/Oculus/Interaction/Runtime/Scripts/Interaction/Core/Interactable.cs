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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Interactable provides a base template for any kind of interactable object.
    /// An Interactable can have Hover and HandleSelected Interactor(s) acting on it.
    /// Concrete Interactables can define whether they have a One-to-One or
    /// One-to-Many relationship with their associated concrete Interactors.
    /// </summary>
    public abstract class Interactable<TInteractor, TInteractable> : MonoBehaviour, IInteractable
                                        where TInteractor : Interactor<TInteractor, TInteractable>
                                        where TInteractable : Interactable<TInteractor, TInteractable>
    {

        [SerializeField, Interface(typeof(IGameObjectFilter)), Optional]
        private List<MonoBehaviour> _interactorFilters = new List<MonoBehaviour>();
        private List<IGameObjectFilter> InteractorFilters = null;

        /// <summary>
        /// The max Interactors and max selecting Interactors that this Interactable can
        /// have acting on it.
        /// -1 signifies NO limit (can have any number of Interactors)
        /// </summary>
        [SerializeField]
        private int _maxInteractors = -1;

        [SerializeField]
        private int _maxSelectingInteractors = -1;

        #region Properties
        public int MaxInteractors
        {
            get
            {
                return _maxInteractors;
            }
            set
            {
                _maxInteractors = value;
            }
        }

        public int MaxSelectingInteractors
        {
            get
            {
                return _maxSelectingInteractors;
            }
            set
            {
                _maxSelectingInteractors = value;
            }
        }
        #endregion


        public IEnumerable<IInteractorView> InteractorViews => _interactors.Cast<IInteractorView>();
        public IEnumerable<IInteractorView> SelectingInteractorViews => _selectingInteractors.Cast<IInteractorView>();

        private HashSet<TInteractor> _interactors = new HashSet<TInteractor>();
        private HashSet<TInteractor> _selectingInteractors = new HashSet<TInteractor>();

        private InteractableState _state = InteractableState.Disabled;
        public event Action<InteractableStateChangeArgs> WhenStateChanged = delegate { };

        public event Action<IInteractorView> WhenInteractorViewAdded = delegate { };
        public event Action<IInteractorView> WhenInteractorViewRemoved = delegate { };
        public event Action<IInteractorView> WhenSelectingInteractorViewAdded = delegate { };
        public event Action<IInteractorView> WhenSelectingInteractorViewRemoved = delegate { };

        private MultiAction<TInteractor> _whenInteractorAdded = new MultiAction<TInteractor>();
        private MultiAction<TInteractor> _whenInteractorRemoved = new MultiAction<TInteractor>();
        private MultiAction<TInteractor> _whenSelectingInteractorAdded = new MultiAction<TInteractor>();
        private MultiAction<TInteractor> _whenSelectingInteractorRemoved = new MultiAction<TInteractor>();
        public MAction<TInteractor> WhenInteractorAdded => _whenInteractorAdded;
        public MAction<TInteractor> WhenInteractorRemoved => _whenInteractorRemoved;
        public MAction<TInteractor> WhenSelectingInteractorAdded => _whenSelectingInteractorAdded;
        public MAction<TInteractor> WhenSelectingInteractorRemoved => _whenSelectingInteractorRemoved;

        public InteractableState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state == value) return;
                InteractableState previousState = _state;
                _state = value;
                WhenStateChanged(new InteractableStateChangeArgs
                {
                    PreviousState = previousState,
                    NewState = _state
                });
            }
        }

        private static InteractableRegistry<TInteractor, TInteractable> _registry =
                                        new InteractableRegistry<TInteractor, TInteractable>();

        public static InteractableRegistry<TInteractor, TInteractable> Registry => _registry;

        protected virtual void InteractorAdded(TInteractor interactor)
        {
            WhenInteractorViewAdded(interactor);
            _whenInteractorAdded.Invoke(interactor);
        }
        protected virtual void InteractorRemoved(TInteractor interactor)
        {
            WhenInteractorViewRemoved(interactor);
            _whenInteractorRemoved.Invoke(interactor);
        }

        protected virtual void SelectingInteractorAdded(TInteractor interactor)
        {
            WhenSelectingInteractorViewAdded(interactor);
            _whenSelectingInteractorAdded.Invoke(interactor);
        }
        protected virtual void SelectingInteractorRemoved(TInteractor interactor)
        {
            WhenSelectingInteractorViewRemoved(interactor);
            _whenSelectingInteractorRemoved.Invoke(interactor);
        }

        public ICollection<TInteractor> Interactors => _interactors;

        public ICollection<TInteractor> SelectingInteractors => _selectingInteractors;

        public void AddInteractor(TInteractor interactor)
        {
            _interactors.Add(interactor);
            InteractorAdded(interactor);
            UpdateInteractableState();
        }

        public void RemoveInteractor(TInteractor interactor)
        {
            if (!_interactors.Remove(interactor))
            {
                return;
            }
            interactor.InteractableChangesUpdate();
            InteractorRemoved(interactor);
            UpdateInteractableState();
        }

        public void AddSelectingInteractor(TInteractor interactor)
        {
            _selectingInteractors.Add(interactor);
            SelectingInteractorAdded(interactor);
            UpdateInteractableState();
        }

        public void RemoveSelectingInteractor(TInteractor interactor)
        {
            if (!_selectingInteractors.Remove(interactor))
            {
                return;
            }
            interactor.InteractableChangesUpdate();
            SelectingInteractorRemoved(interactor);
            UpdateInteractableState();
        }

        private void UpdateInteractableState()
        {
            if (State == InteractableState.Disabled) return;

            if (_selectingInteractors.Count > 0)
            {
                State = InteractableState.Select;
            }
            else if (_interactors.Count > 0)
            {
                State = InteractableState.Hover;
            }
            else
            {
                State = InteractableState.Normal;
            }
        }

        public bool CanBeSelectedBy(TInteractor interactor)
        {
            if (State == InteractableState.Disabled)
            {
                return false;
            }

            if (MaxSelectingInteractors >= 0 &&
                _selectingInteractors.Count == MaxSelectingInteractors)
            {
                return false;
            }

            if (MaxInteractors >= 0 &&
                _interactors.Count == MaxInteractors &&
                !_interactors.Contains(interactor))
            {
                return false;
            }

            if (InteractorFilters == null)
            {
                return true;
            }

            foreach (IGameObjectFilter interactorFilter in InteractorFilters)
            {
                if (!interactorFilter.Filter(interactor.gameObject))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasInteractor(TInteractor interactor)
        {
            return _interactors.Contains(interactor);
        }

        public bool HasSelectingInteractor(TInteractor interactor)
        {
            return _selectingInteractors.Contains(interactor);
        }

        public void Enable()
        {
            if (State != InteractableState.Disabled)
            {
                return;
            }
            _registry.Register((TInteractable)this);
            State = InteractableState.Normal;
        }

        public void Disable()
        {
            if (State == InteractableState.Disabled)
            {
                return;
            }

            List<TInteractor> selectingInteractorsCopy = new List<TInteractor>(_selectingInteractors);
            foreach (TInteractor selectingInteractor in selectingInteractorsCopy)
            {
                RemoveSelectingInteractor(selectingInteractor);
            }

            List<TInteractor> interactorsCopy = new List<TInteractor>(_interactors);
            foreach (TInteractor interactor in interactorsCopy)
            {
                RemoveInteractor(interactor);
            }

            State = InteractableState.Disabled;
            _registry.Unregister((TInteractable)this);
        }

        public void RemoveInteractorByIdentifier(int id)
        {
            TInteractor foundInteractor = null;
            foreach (TInteractor selectingInteractor in _selectingInteractors)
            {
                if (selectingInteractor.Identifier == id)
                {
                    foundInteractor = selectingInteractor;
                    break;
                }
            }

            if (foundInteractor != null)
            {
                RemoveSelectingInteractor(foundInteractor);
            }

            foundInteractor = null;

            foreach (TInteractor interactor in _interactors)
            {
                if (interactor.Identifier == id)
                {
                    foundInteractor = interactor;
                    break;
                }
            }

            if (foundInteractor == null)
            {
                return;
            }

            RemoveInteractor(foundInteractor);
        }

        protected virtual void Awake()
        {
            InteractorFilters =
                _interactorFilters.ConvertAll(mono => mono as IGameObjectFilter);
        }

        protected virtual void Start()
        {
            foreach (IGameObjectFilter filter in InteractorFilters)
            {
                Assert.IsNotNull(filter);
            }
        }

        protected virtual void OnEnable()
        {
            Enable();
        }

        protected virtual void OnDisable()
        {
            Disable();
        }

        protected virtual void SetRegistry(InteractableRegistry<TInteractor, TInteractable> registry)
        {
            if (registry == _registry) return;

            IEnumerable<TInteractable> interactables = _registry.List();
            foreach (TInteractable interactable in interactables)
            {
                registry.Register(interactable);
                _registry.Unregister(interactable);
            }
            _registry = registry;
        }

        #region Inject
        public void InjectOptionalInteractorFilters(List<IGameObjectFilter> interactorFilters)
        {
            InteractorFilters = interactorFilters;
            _interactorFilters = interactorFilters.ConvertAll(interactorFilter =>
                                    interactorFilter as MonoBehaviour);
        }
        #endregion
    }
}
