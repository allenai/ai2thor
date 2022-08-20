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
using UnityEngine.Events;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Samples
{
    /// <summary>
    /// Raises events when an object is scaled up or down. Events are raised in steps,
    /// meaning scale changes are only responded to when the scale magnitude delta since
    /// last step exceeds a provided amount.
    /// </summary>
    public class ScaleAudioEvents : MonoBehaviour
    {
        private enum Direction
        {
            None,
            ScaleUp,
            ScaleDown,
        }

        [SerializeField, Interface(typeof(IInteractableView))]
        private MonoBehaviour _interactableView;

        [Tooltip("Transform to track scale of. If not provided, transform of this component is used.")]
        [SerializeField, Optional]
        private Transform _trackedTransform;

        [Tooltip("The increase in scale magnitude that will fire the step event")]
        [SerializeField]
        private float _stepSize = 0.4f;

        [Tooltip("Events will not be fired more frequently than this many times per second")]
        [SerializeField]
        private int _maxEventFreq = 20;

        [SerializeField]
        private UnityEvent _whenScalingStarted = new UnityEvent();

        [SerializeField]
        private UnityEvent _whenScalingEnded = new UnityEvent();

        [SerializeField]
        private UnityEvent _whenScaledUp = new UnityEvent();

        [SerializeField]
        private UnityEvent _whenScaledDown = new UnityEvent();

        public UnityEvent WhenScalingStarted => _whenScalingStarted;

        public UnityEvent WhenScalingEnded => _whenScalingEnded;

        public UnityEvent WhenScaledUp => _whenScaledUp;

        public UnityEvent WhenScaledDown => _whenScaledDown;

        private IInteractableView InteractableView;

        private Transform TrackedTransform
        {
            get => _trackedTransform == null ? transform : _trackedTransform;
        }

        private bool _isScaling;
        private Vector3 _lastStep;
        private float _lastEventTime;
        private Direction _direction = Direction.None;

        protected bool _started;

        private void ScalingStarted()
        {
            _lastStep = TrackedTransform.localScale;
            _whenScalingStarted.Invoke();
        }

        private void ScalingEnded()
        {
            _whenScalingEnded.Invoke();
        }

        private float GetTotalDelta(out Direction direction)
        {
            float prevMagnitude = _lastStep.magnitude;
            float newMagnitude = TrackedTransform.localScale.magnitude;
            if (newMagnitude == prevMagnitude)
            {
                direction = Direction.None;
            }
            else
            {
                direction = newMagnitude > prevMagnitude ? Direction.ScaleUp : Direction.ScaleDown;
            }

            return direction == Direction.ScaleUp ?
                   newMagnitude - prevMagnitude :
                   prevMagnitude - newMagnitude;
        }

        private void UpdateScaling()
        {
            if (_stepSize <= 0 || _maxEventFreq <= 0)
            {
                return;
            }

            float effectiveStepSize = _stepSize;
            float totalDelta = GetTotalDelta(out _direction);
            if (totalDelta > effectiveStepSize)
            {
                _lastStep = TrackedTransform.localScale;
                float timeSince = Time.time - _lastEventTime;
                if (timeSince >= 1f / _maxEventFreq)
                {
                    _lastEventTime = Time.time;
                    if (_direction == Direction.ScaleUp)
                    {
                        _whenScaledUp.Invoke();
                    }
                    else
                    {
                        _whenScaledDown.Invoke();
                    }
                }
            }
        }

        protected virtual void Awake()
        {
            InteractableView = _interactableView as IInteractableView;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(InteractableView);
            Assert.IsNotNull(TrackedTransform);
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            bool wasScaling = _isScaling;
            _isScaling = InteractableView.State == InteractableState.Select;

            if (!_isScaling)
            {
                if (wasScaling)
                {
                    ScalingEnded();
                }
            }
            else
            {
                if (!wasScaling)
                {
                    ScalingStarted();
                }
                UpdateScaling();
            }
        }
    }
}
