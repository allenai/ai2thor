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
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Interactor provides a base template for any kind of interaction.
    /// Interactions can be wholly defined by three things: the concrete Interactor,
    /// the concrete Interactable, and the logic governing their coordination.
    ///
    /// Subclasses are responsible for implementing that coordination logic via template
    /// methods that operate on the concrete interactor and interactable classes.
    /// </summary>
    public abstract class Interactor<TInteractor, TInteractable> : MonoBehaviour, IInteractor
                                    where TInteractor : Interactor<TInteractor, TInteractable>
                                    where TInteractable : Interactable<TInteractor, TInteractable>
    {
        [SerializeField, Interface(typeof(IActiveState)), Optional]
        private MonoBehaviour _activeState;
        private IActiveState ActiveState = null;

        [SerializeField, Interface(typeof(IGameObjectFilter)), Optional]
        private List<MonoBehaviour> _interactableFilters = new List<MonoBehaviour>();
        private List<IGameObjectFilter> InteractableFilters = null;

        protected virtual void DoPreprocess() { }
        protected virtual void DoNormalUpdate() { }
        protected virtual void DoHoverUpdate() { }
        protected virtual void DoSelectUpdate() { }

        public virtual bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal)
                {
                    return false;
                }

                return HasCandidate || QueuedSelect;
            }
        }

        public virtual bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _interactable != _candidate || _candidate == null;
            }
        }

        public virtual bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _candidate == _interactable && QueuedSelect;
            }
        }

        public virtual bool ShouldUnselect {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return QueuedUnselect;
            }
        }

        private InteractorState _state = InteractorState.Normal;
        public event Action<InteractorStateChangeArgs> WhenStateChanged = delegate { };
        public event Action WhenPreprocessed = delegate { };
        public event Action WhenProcessed = delegate { };
        public event Action WhenPostprocessed = delegate { };

        private ISelector _selector = null;

        private int _maxIterationsPerFrame = 10;
        public int MaxIterationsPerFrame
        {
            get
            {
                return _maxIterationsPerFrame;
            }
            set
            {
                _maxIterationsPerFrame = value;
            }
        }

        protected ISelector Selector
        {
            get
            {
                return _selector;
            }
            set
            {
                if (value != _selector)
                {
                    if (_selector != null && _started)
                    {
                        _selector.WhenSelected -= HandleSelected;
                        _selector.WhenUnselected -= HandleUnselected;
                    }
                }

                _selector = value;
                if (_selector != null && _started)
                {
                    _selector.WhenSelected += HandleSelected;
                    _selector.WhenUnselected += HandleUnselected;
                }
            }
        }

        private Queue<bool> _selectorQueue = new Queue<bool>();
        private bool QueuedSelect => _selectorQueue.Count > 0 && _selectorQueue.Peek();
        private bool QueuedUnselect => _selectorQueue.Count > 0 && !_selectorQueue.Peek();

        public InteractorState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state == value)
                {
                    return;
                }
                InteractorState previousState = _state;
                _state = value;

                WhenStateChanged(new InteractorStateChangeArgs
                {
                    PreviousState = previousState,
                    NewState = _state
                });
            }
        }

        protected TInteractable _candidate;
        protected TInteractable _interactable;
        protected TInteractable _selectedInteractable;

        public virtual object Candidate => _candidate;
        public TInteractable Interactable => _interactable;
        public TInteractable SelectedInteractable => _selectedInteractable;

        public bool HasCandidate => _candidate != null;
        public bool HasInteractable => _interactable != null;
        public bool HasSelectedInteractable => _selectedInteractable != null;

        private MultiAction<TInteractable> _whenInteractableSet = new MultiAction<TInteractable>();
        private MultiAction<TInteractable> _whenInteractableUnset = new MultiAction<TInteractable>();
        private MultiAction<TInteractable> _whenInteractableSelected = new MultiAction<TInteractable>();
        private MultiAction<TInteractable> _whenInteractableUnselected = new MultiAction<TInteractable>();
        public MAction<TInteractable> WhenInteractableSet => _whenInteractableSet;
        public MAction<TInteractable> WhenInteractableUnset => _whenInteractableUnset;
        public MAction<TInteractable> WhenInteractableSelected => _whenInteractableSelected;
        public MAction<TInteractable> WhenInteractableUnselected => _whenInteractableUnselected;

        protected virtual void InteractableSet(TInteractable interactable)
        {
            _whenInteractableSet.Invoke(interactable);
        }

        protected virtual void InteractableUnset(TInteractable interactable)
        {
            _whenInteractableUnset.Invoke(interactable);
        }

        protected virtual void InteractableSelected(TInteractable interactable)
        {
            _whenInteractableSelected.Invoke(interactable);
        }

        protected virtual void InteractableUnselected(TInteractable interactable)
        {
            _whenInteractableUnselected.Invoke(interactable);
        }

        protected virtual void DoPostprocess() { }

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate();
            ActiveState = _activeState as IActiveState;
            InteractableFilters =
                _interactableFilters.ConvertAll(mono => mono as IGameObjectFilter);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            foreach (IGameObjectFilter filter in InteractableFilters)
            {
                Assert.IsNotNull(filter);
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (_selector != null)
                {
                    _selectorQueue.Clear();
                    _selector.WhenSelected += HandleSelected;
                    _selector.WhenUnselected += HandleUnselected;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_selector != null)
                {
                    _selector.WhenSelected -= HandleSelected;
                    _selector.WhenUnselected -= HandleUnselected;
                }
                Disable();
            }
        }

        protected virtual void OnDestroy()
        {
            UniqueIdentifier.Release(_identifier);
        }

        public void Preprocess()
        {
            DoPreprocess();
            if (!UpdateActiveState())
            {
                Disable();
            }
            WhenPreprocessed();
        }

        public void Process()
        {
            switch (State)
            {
                case InteractorState.Normal:
                    DoNormalUpdate();
                    break;
                case InteractorState.Hover:
                    DoHoverUpdate();
                    break;
                case InteractorState.Select:
                    DoSelectUpdate();
                    break;
            }
            WhenProcessed();
        }

        public void Postprocess()
        {
            _selectorQueue.Clear();
            DoPostprocess();
            WhenPostprocessed();
        }

        public virtual void ProcessCandidate()
        {
            _candidate = null;
            if (!UpdateActiveState())
            {
                return;
            }
            _candidate = ComputeCandidate();
        }

        public void InteractableChangesUpdate()
        {
            if (_selectedInteractable != null &&
                !_selectedInteractable.HasSelectingInteractor(this as TInteractor))
            {
                UnselectInteractable();
            }

            if (_interactable != null &&
                !_interactable.HasInteractor(this as TInteractor))
            {
                UnsetInteractable();
            }
        }

        public void Hover()
        {
            if (State != InteractorState.Normal)
            {
                return;
            }

            SetInteractable(_candidate);
            State = InteractorState.Hover;
        }

        public void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            UnsetInteractable();
            State = InteractorState.Normal;
        }


        public virtual void Select()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            while (QueuedSelect)
            {
                _selectorQueue.Dequeue();
            }

            if (Interactable != null)
            {
                SelectInteractable(Interactable);
            }

            State = InteractorState.Select;
        }

        public virtual void Unselect()
        {
            if (State != InteractorState.Select)
            {
                return;
            }

            while (QueuedUnselect)
            {
                _selectorQueue.Dequeue();
            }

            UnselectInteractable();

            State = InteractorState.Hover;
        }

        // Returns the best interactable for selection or null
        protected abstract TInteractable ComputeCandidate();

        public virtual bool CanSelect(TInteractable interactable)
        {
            if (InteractableFilters == null)
            {
                return true;
            }

            foreach (IGameObjectFilter interactableFilter in InteractableFilters)
            {
                if (!interactableFilter.Filter(interactable.gameObject))
                {
                    return false;
                }
            }

            return true;
        }

        private void SetInteractable(TInteractable interactable)
        {
            if (_interactable == interactable)
            {
                return;
            }
            UnsetInteractable();
            _interactable = interactable;
            interactable.AddInteractor(this as TInteractor);
            InteractableSet(interactable);
        }

        private void UnsetInteractable()
        {
            TInteractable interactable = _interactable;
            if (interactable == null)
            {
                return;
            }
            _interactable = null;
            interactable.RemoveInteractor(this as TInteractor);
            InteractableUnset(interactable);
        }

        private void SelectInteractable(TInteractable interactable)
        {
            Unselect();
            _selectedInteractable = interactable;
            interactable.AddSelectingInteractor(this as TInteractor);
            InteractableSelected(interactable);
        }

        private void UnselectInteractable()
        {
            TInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                return;
            }
            _selectedInteractable = null;
            interactable.RemoveSelectingInteractor(this as TInteractor);
            InteractableUnselected(interactable);
        }

        public void Enable()
        {
            if (!UpdateActiveState())
            {
                return;
            }

            if (State == InteractorState.Disabled)
            {
                State = InteractorState.Normal;
            }
        }

        public void Disable()
        {
            if (State == InteractorState.Disabled)
            {
                return;
            }

            if (State == InteractorState.Select)
            {
                UnselectInteractable();
                State = InteractorState.Hover;
            }

            if (State == InteractorState.Hover)
            {
                UnsetInteractable();
                State = InteractorState.Normal;
            }

            if (State == InteractorState.Normal)
            {
                State = InteractorState.Disabled;
            }
        }

        protected virtual void HandleSelected()
        {
            _selectorQueue.Enqueue(true);
        }

        protected virtual void HandleUnselected()
        {
            _selectorQueue.Enqueue(false);
        }

        private bool UpdateActiveState()
        {
            if (ActiveState == null || ActiveState.Active)
            {
                return true;
            }
            return false;
        }

        public bool IsRootDriver { get; set; } = true;

        protected virtual void Update()
        {
            if (!IsRootDriver)
            {
                return;
            }

            Preprocess();

            if (!UpdateActiveState())
            {
                Disable();
                Postprocess();
                return;
            }

            Enable();

            InteractorState previousState = State;
            for (int i = 0; i < MaxIterationsPerFrame; i++)
            {
                if (State == InteractorState.Normal ||
                    (State == InteractorState.Hover && previousState != InteractorState.Normal))
                {
                    ProcessCandidate();
                }
                previousState = State;

                Process();

                if (State == InteractorState.Disabled)
                {
                    break;
                }

                if (State == InteractorState.Normal)
                {
                    if (ShouldHover)
                    {
                        Hover();
                        continue;
                    }
                    break;
                }

                if (State == InteractorState.Hover)
                {
                    if (ShouldSelect)
                    {
                        Select();
                        continue;
                    }
                    if (ShouldUnhover)
                    {
                        Unhover();
                        continue;
                    }
                    break;
                }

                if(State == InteractorState.Select)
                {
                    if (ShouldUnselect)
                    {
                        Unselect();
                        continue;
                    }
                    break;
                }
            }

            Postprocess();
        }

        #region Inject
        public void InjectOptionalActiveState(IActiveState activeState)
        {
            _activeState = activeState as MonoBehaviour;
            ActiveState = activeState;
        }

        public void InjectOptionalInteractableFilters(List<IGameObjectFilter> interactableFilters)
        {
            InteractableFilters = interactableFilters;
            _interactableFilters = interactableFilters.ConvertAll(interactableFilter =>
                                    interactableFilter as MonoBehaviour);
        }
        #endregion
    }
}
