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

namespace Oculus.Interaction.HandGrab
{
    public class AutoMoveTowardsTargetProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField]
        private PoseTravelData _travellingData = PoseTravelData.DEFAULT;
        public PoseTravelData TravellingData
        {
            get
            {
                return _travellingData;
            }
            set
            {
                _travellingData = value;
            }
        }

        [SerializeField, Interface(typeof(IPointableElement))]
        private MonoBehaviour _pointableElement;
        public IPointableElement PointableElement { get; private set; }

        private bool _started;

        public List<AutoMoveTowardsTarget> _movers = new List<AutoMoveTowardsTarget>();

        protected virtual void Awake()
        {
            PointableElement = _pointableElement as IPointableElement;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_pointableElement);
            this.EndStart(ref _started);
        }

        private void LateUpdate()
        {
            for (int i = _movers.Count - 1; i >= 0; i--)
            {
                AutoMoveTowardsTarget mover = _movers[i];
                if (mover.Aborting)
                {
                    mover.Tick();
                    if (mover.Stopped)
                    {
                        _movers.Remove(mover);
                    }
                }
            }
        }

        public IMovement CreateMovement()
        {
            AutoMoveTowardsTarget mover = new AutoMoveTowardsTarget(_travellingData, PointableElement);
            mover.WhenAborted += HandleAborted;
            return mover;
        }

        private void HandleAborted(AutoMoveTowardsTarget mover)
        {
            mover.WhenAborted -= HandleAborted;
            _movers.Add(mover);
        }

        #region Inject

        public void InjectAllAutoMoveTowardsTargetProvider(IPointableElement pointableElement)
        {
            InjectPointableElement(pointableElement);
        }

        public void InjectPointableElement(IPointableElement pointableElement)
        {
            PointableElement = pointableElement;
            _pointableElement = pointableElement as MonoBehaviour;
        }
        #endregion
    }

    /// <summary>
    /// This IMovement stores the initial Pose, and in case
    /// of an aborted movement it will finish it itself.
    /// </summary>
    public class AutoMoveTowardsTarget : IMovement
    {
        private PoseTravelData _travellingData;
        private IPointableElement _pointableElement;

        public Pose Pose => _tween.Pose;
        public bool Stopped => _tween == null || _tween.Stopped;
        public bool Aborting { get; private set; }

        public Action<AutoMoveTowardsTarget> WhenAborted = delegate { };

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        private Tween _tween;
        private Pose _target;
        private Pose _source;
        private bool _eventRegistered;

        public AutoMoveTowardsTarget(PoseTravelData travellingData, IPointableElement pointableElement)
        {
            _identifier = UniqueIdentifier.Generate();
            _travellingData = travellingData;
            _pointableElement = pointableElement;
        }

        public void MoveTo(Pose target)
        {
            AbortSelfAligment();
            _target = target;
            _tween = _travellingData.CreateTween(_source, target);
            if (!_eventRegistered)
            {
                _pointableElement.WhenPointerEventRaised += HandlePointerEventRaised;
                _eventRegistered = true;
            }
        }

        public void UpdateTarget(Pose target)
        {
            _target = target;
            _tween.UpdateTarget(_target);
        }

        public void StopAndSetPose(Pose pose)
        {
            if (_eventRegistered)
            {
                _pointableElement.WhenPointerEventRaised -= HandlePointerEventRaised;
                _eventRegistered = false;
            }

            _source = pose;
            if (_tween != null && !_tween.Stopped)
            {
                GeneratePointerEvent(PointerEventType.Hover);
                GeneratePointerEvent(PointerEventType.Select);
                Aborting = true;
                WhenAborted.Invoke(this);
            }
        }

        public void Tick()
        {
            _tween.Tick();
            if (Aborting)
            {
                GeneratePointerEvent(PointerEventType.Move);
                if (_tween.Stopped)
                {
                    AbortSelfAligment();
                }
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect)
            {
                AbortSelfAligment();
            }
        }

        private void AbortSelfAligment()
        {
            if (Aborting)
            {
                Aborting = false;

                GeneratePointerEvent(PointerEventType.Unselect);
                GeneratePointerEvent(PointerEventType.Unhover);
            }
        }

        private void GeneratePointerEvent(PointerEventType pointerEventType)
        {
            PointerEvent evt = new PointerEvent(Identifier, pointerEventType, Pose);
            _pointableElement.ProcessPointerEvent(evt);
        }
    }
}
