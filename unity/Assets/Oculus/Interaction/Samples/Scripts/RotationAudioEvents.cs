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
    /// Raises events when an object is rotated relative to a provided transform. Rotated
    /// events will be raised when the rotation exceeds a provided angle threshold, in degrees.
    /// Events are raised only once per directional sweep, so if an event was fired while angle
    /// was increasing, the next must fire while angle decreases.
    /// </summary>
    public class RotationAudioEvents : MonoBehaviour
    {
        private enum Direction
        {
            None,
            Opening,
            Closing,
        }

        [SerializeField, Interface(typeof(IInteractableView))]
        private MonoBehaviour _interactableView;

        [Tooltip("Transform to track rotation of. If not provided, transform of this component is used.")]
        [SerializeField, Optional]
        private Transform _trackedTransform;

        [SerializeField]
        private Transform _relativeTo;

        [Tooltip("The angle delta at which the threshold crossed event will be fired.")]
        [SerializeField]
        private float _thresholdDeg = 20f;

        [Tooltip("Maximum rotation arc within which the crossed event will be triggered.")]
        [SerializeField, Range(1f, 150f)]
        private float _maxRangeDeg = 150f;

        [SerializeField]
        private UnityEvent _whenRotationStarted = new UnityEvent();

        [SerializeField]
        private UnityEvent _whenRotationEnded = new UnityEvent();

        [SerializeField]
        private UnityEvent _whenRotatedOpen = new UnityEvent();

        [SerializeField]
        private UnityEvent _whenRotatedClosed = new UnityEvent();

        public UnityEvent WhenRotationStarted => _whenRotationStarted;

        public UnityEvent WhenRotationEnded => _whenRotationEnded;

        public UnityEvent WhenRotatedOpen => _whenRotatedOpen;

        public UnityEvent WhenRotatedClosed => _whenRotatedClosed;

        private IInteractableView InteractableView;

        private Transform TrackedTransform
        {
            get => _trackedTransform == null ? transform : _trackedTransform;
        }

        private float _baseDelta;
        private bool _isRotating;
        private Direction _lastCrossedDirection;

        protected bool _started;

        private void RotationStarted()
        {
            _baseDelta = GetTotalDelta();
            _lastCrossedDirection = Direction.None;
            _whenRotationStarted.Invoke();
        }

        private void RotationEnded()
        {
            _whenRotationEnded.Invoke();
        }

        private Quaternion GetCurrentRotation()
        {
            return Quaternion.Inverse(_relativeTo.rotation) * TrackedTransform.rotation;
        }

        private float GetTotalDelta()
        {
            return Quaternion.Angle(_relativeTo.rotation, GetCurrentRotation());
        }

        private void UpdateRotation()
        {
            float totalDelta = GetTotalDelta();

            if (totalDelta > _maxRangeDeg)
            {
                return;
            }

            if (Mathf.Abs(totalDelta - _baseDelta) > _thresholdDeg)
            {
                var _direction = totalDelta - _baseDelta > 0 ?
                                 Direction.Opening :
                                 Direction.Closing;

                if (_direction != _lastCrossedDirection)
                {
                    _lastCrossedDirection = _direction;
                    if (_direction == Direction.Opening)
                    {
                        _whenRotatedOpen.Invoke();
                    }
                    else
                    {
                        _whenRotatedClosed.Invoke();
                    }
                }
            }

            if (_lastCrossedDirection == Direction.Opening)
            {
                _baseDelta = Mathf.Max(_baseDelta, totalDelta);
            }
            else if (_lastCrossedDirection == Direction.Closing)
            {
                _baseDelta = Mathf.Min(_baseDelta, totalDelta);
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
            Assert.IsNotNull(_relativeTo);
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            bool wasRotating = _isRotating;
            _isRotating = InteractableView.State == InteractableState.Select;

            if (!_isRotating)
            {
                if (wasRotating)
                {
                    RotationEnded();
                }
            }
            else
            {
                if (!wasRotating)
                {
                    RotationStarted();
                }
                UpdateRotation();
            }
        }
    }
}
