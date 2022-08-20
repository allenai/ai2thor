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

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Chains together a number of IActiveStates into a sequence.
    /// The Sequence._stepsToActivate field contains an optional list of IActiveState's which must be 'activated' in
    /// order.
    /// The sequence can progress from Step N to N + 1 when: MinActiveTime <= "Time step N active for" <= MaxStepTime, and:
    ///   Step N just became inactive OR
    ///   Step N is the last step OR
    ///   Step N+1 is active
    ///
    /// Note that once the sequence has moved on to the next step, the previous step does not need to remain active.
    /// Each step has three fields:
    ///   ActiveState: The IActiveState that is used to determine if the conditions of this step are fulfilled
    ///   MinActiveTime: How long (in seconds) the IActiveState of this step must be contiguously active before moving
    ///                  on to the next step. If the ActiveState drops out of being active for even a single frame
    ///                  the countdown is reset.
    ///   MaxStepTime: If the elapsed time that the sequence is spent waiting for this step to reach its MinActiveTime
    ///                exceeds this value then the whole sequence is reset back to the beginning.
    ///
    /// Once all steps are complete the Sequence.Active becomes true. It will remain true as long as RemainActiveWhile
    /// is true. If _remainActiveCooldown > 0, Sequence.Active will remain active even after RemainActiveWhile becomes
    /// false until the cooldown timer is met. The timer is reset if RemainActiveWhile becomes true again.
    /// </summary>
    public class Sequence : MonoBehaviour, IActiveState
    {
        [Serializable]
        public class ActivationStep
        {
            [SerializeField, Interface(typeof(IActiveState))]
            private MonoBehaviour _activeState;

            public IActiveState ActiveState { get; private set; }

            [SerializeField]
            [Tooltip("This step must be consistently active for this amount of time before continuing to the next step.")]
            private float _minActiveTime;

            public float MinActiveTime => _minActiveTime;

            [SerializeField]
            [Tooltip(
                "Maximum time that can be spent waiting for this step to complete, before the whole sequence is abandoned. This value must be greater than minActiveTime, or zero. This value is ignored if zero, and for the first step in the list.")]
            private float _maxStepTime;

            public float MaxStepTime => _maxStepTime;

            public ActivationStep()
            {
            }

            public ActivationStep(IActiveState activeState, float minActiveTime, float maxStepTime)
            {
                ActiveState = activeState;
                _minActiveTime = minActiveTime;
                _maxStepTime = maxStepTime;
            }

            public void Start()
            {
                if (ActiveState == null)
                {
                    ActiveState = _activeState as IActiveState;
                }

                Assert.IsNotNull(ActiveState);
            }
        }

        [SerializeField, Optional]
        private ActivationStep[] _stepsToActivate;

        [SerializeField, Optional, Interface(typeof(IActiveState))]
        private MonoBehaviour _remainActiveWhile;

        [SerializeField, Optional]
        private float _remainActiveCooldown;

        private IActiveState RemainActiveWhile { get; set; }

        /// <summary>
        /// Returns the index of the step in <see cref="_stepsToActivate"/> whose conditions are
        /// waiting to be activated.
        /// If <see cref="Active"/> is true, this value will be set to the
        /// size of <see cref="_stepsToActivate"/>.
        /// If <see cref="_stepsToActivate"/> has no steps, this property will be 0.
        /// </summary>
        public int CurrentActivationStep { get; private set; }
        private float _currentStepActivatedTime;
        private float _stepFailedTime;
        private bool _currentStepWasActive;
        Func<float> _timeProvider;

        private float _cooldownExceededTime;
        private bool _wasRemainActive;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            RemainActiveWhile = _remainActiveWhile as IActiveState;

            ResetState();
        }

        protected virtual void Start()
        {
            if (_timeProvider == null)
            {
                _timeProvider = () => Time.time;
            }

            if (_stepsToActivate == null)
            {
                _stepsToActivate = Array.Empty<ActivationStep>();
            }

            foreach (var step in _stepsToActivate)
            {
                step.Start();
            }
        }

        protected virtual void Update()
        {
            var time = _timeProvider();
            if (Active)
            {
                // Test for active, if RemainActiveWhile is set.
                bool shouldBeActive = RemainActiveWhile != null && RemainActiveWhile.Active;
                if (!shouldBeActive)
                {
                    if (_wasRemainActive)
                    {
                        _cooldownExceededTime = time + _remainActiveCooldown;
                    }

                    if (_cooldownExceededTime <= time)
                    {
                        Active = false;
                    }
                }

                _wasRemainActive = shouldBeActive;

                // No longer active; start activation condition at the beginning
                if (!Active)
                {
                    ResetState();
                }

                return;
            }

            if (CurrentActivationStep < _stepsToActivate.Length)
            {
                var currentStep = _stepsToActivate[CurrentActivationStep];

                if (time > _stepFailedTime && CurrentActivationStep > 0 && currentStep.MaxStepTime > 0.0f)
                {
                    // Failed to activate before max time limit reached. Start from the beginning.
                    ResetState();
                }

                bool currentStepIsActive = currentStep.ActiveState.Active;
                if (currentStepIsActive)
                {
                    if (!_currentStepWasActive)
                    {
                        // Step wasn't active, but now it is! Start the timer until next step can
                        // be entered.
                        _currentStepActivatedTime = time + currentStep.MinActiveTime;
                    }
                }

                if (time >= _currentStepActivatedTime && _currentStepWasActive)
                {
                    // Time constraint met. Go to next step if either:
                    // - this step just became inactive OR
                    // - this is the last step OR
                    // - the next step is active
                    var nextStepIndex = CurrentActivationStep + 1;
                    bool thisStepCondition = !currentStepIsActive;
                    bool nextStepCondition = (nextStepIndex == _stepsToActivate.Length) ||
                                             _stepsToActivate[nextStepIndex].ActiveState.Active;

                    if (thisStepCondition || nextStepCondition)
                    {
                        EnterNextStep(time);
                    }
                }

                _currentStepWasActive = currentStepIsActive;
            }
            else if (RemainActiveWhile != null)
            {
                Active = RemainActiveWhile.Active;
            }
        }

        private void EnterNextStep(float time)
        {
            CurrentActivationStep++;
            _currentStepWasActive = false;

            if (CurrentActivationStep < _stepsToActivate.Length)
            {
                var currentStep = _stepsToActivate[CurrentActivationStep];
                _stepFailedTime = time + currentStep.MaxStepTime;
                return;
            }

            // This was the last step. Activate.
            Active = true;

            // In case there is no RemainActiveWhile condition, start the cooldown
            // timer
            _cooldownExceededTime = time + _remainActiveCooldown;
        }

        private void ResetState()
        {
            CurrentActivationStep = 0;
            _currentStepWasActive = false;
            _currentStepActivatedTime = 0.0f;
        }

        #endregion

        public bool Active { get; private set;  }

        #region Inject

        public void InjectOptionalStepsToActivate(ActivationStep[] stepsToActivate)
        {
            _stepsToActivate = stepsToActivate;
        }

        public void InjectOptionalRemainActiveWhile(IActiveState activeState)
        {
            _remainActiveWhile = activeState as MonoBehaviour;
            RemainActiveWhile = activeState;
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        #endregion

    }
}
