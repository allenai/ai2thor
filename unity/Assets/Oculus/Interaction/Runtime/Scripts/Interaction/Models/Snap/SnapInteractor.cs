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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// The SnapInteractor referes to an element that can snap to a SnapInteractable.
    /// This interactor moves itself into the Pose specified by the intereactable.
    /// Additionally, it can specify a preferred SnapInteractable and a TimeOut time, and it
    /// will automatically snap there if its Pointable element has not been used (hovered, selected)
    /// for a certain time.
    /// </summary>
    public class SnapInteractor : Interactor<SnapInteractor, SnapInteractable>,
        IRigidbodyRef
    {
        [SerializeField]
        private PointableElement _pointableElement;

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField, Optional]
        [FormerlySerializedAs("_dropPoint")]
        private Transform _snapPoint;
        public Pose SnapPose => _snapPoint.GetPose();

        [Header("Time out")]
        [SerializeField, Optional]
        private SnapInteractable _timeOutInteractable;
        [SerializeField, Optional]
        private float _timeOut = 0f;

        private float _idleStarted = -1f;
        private IMovement _movement;

        #region Editor events
        private void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
            _pointableElement = this.GetComponentInParent<PointableElement>();
        }
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _pointableElement = _pointableElement as PointableElement;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(_pointableElement, "Pointable element can not be null");
            Assert.IsNotNull(Rigidbody, "Rigidbody can not be null");
            if (_snapPoint == null)
            {
                _snapPoint = this.transform;
            }

            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _pointableElement.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _pointableElement.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
            base.OnDisable();
        }

        #endregion

        #region Interactor Lifecycle

        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _pointableElement.SelectingPointsCount == 0;
            }
        }

        public override bool ShouldUnselect {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return _shouldUnselect;
            }
        }


        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            if (Interactable == null)
            {
                return;
            }

            Interactable.InteractorHoverUpdated(this);
        }

        private bool _shouldUnselect = false;

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();

            _shouldUnselect = false;

            if (_movement == null)
            {
                _shouldUnselect = true;
                return;
            }

            if (Interactable != null)
            {
                if (Interactable.PoseForInteractor(this, out Pose targetPose))
                {
                    _movement.UpdateTarget(targetPose);
                    _movement.Tick();
                    GeneratePointerEvent(PointerEventType.Move);
                }
                else
                {
                    _shouldUnselect = true;
                }
            }
            else
            {
                _shouldUnselect = true;
            }

            if (_pointableElement.SelectingPointsCount > 1)
            {
                _shouldUnselect = true;
            }
        }

        protected override void InteractableSet(SnapInteractable interactable)
        {
            base.InteractableSet(interactable);
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEventType.Hover);
            }
        }

        protected override void InteractableUnset(SnapInteractable interactable)
        {
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEventType.Unhover);
            }
            base.InteractableUnset(interactable);
        }

        protected override void InteractableSelected(SnapInteractable interactable)
        {
            base.InteractableSelected(interactable);
            if (interactable != null)
            {
                _movement = interactable.GenerateMovement(_snapPoint.GetPose(), this);
                if (_movement != null)
                {
                    GeneratePointerEvent(PointerEventType.Select);
                }
            }
        }

        protected override void InteractableUnselected(SnapInteractable interactable)
        {
            _movement?.StopAndSetPose(_movement.Pose);
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEventType.Unselect);
            }
            base.InteractableUnselected(interactable);
            _movement = null;
        }

        #endregion

        #region Pointable

        protected virtual void HandlePointerEventRaised(PointerEvent evt)
        {
            if (_pointableElement.PointsCount == 0
                && (evt.Type == PointerEventType.Cancel
                    || evt.Type == PointerEventType.Unhover
                    || evt.Type == PointerEventType.Unselect))
            {
                _idleStarted = Time.time;
            }
            else
            {
                _idleStarted = -1f;
            }

            if (evt.Identifier == Identifier
                && evt.Type == PointerEventType.Cancel
                && Interactable != null)
            {
                Interactable.RemoveInteractorByIdentifier(Identifier);
            }
        }


        public void GeneratePointerEvent(PointerEventType pointerEventType, Pose pose)
        {
            _pointableElement.ProcessPointerEvent(new PointerEvent(Identifier, pointerEventType, pose));
        }

        private void GeneratePointerEvent(PointerEventType pointerEventType)
        {
            Pose pose = ComputePointerPose();
            _pointableElement.ProcessPointerEvent(new PointerEvent(Identifier, pointerEventType, pose));
        }

        protected Pose ComputePointerPose()
        {
            if (_movement != null)
            {
                return _movement.Pose;
            }

            return SnapPose;
        }
        #endregion

        private bool TimedOut()
        {
            return _timeOut >= 0f
                && _idleStarted >= 0f
                && Time.time - _idleStarted > _timeOut;
        }

        protected override SnapInteractable ComputeCandidate()
        {
            SnapInteractable interactable = ComputeIntersectingCandidate();
            if (TimedOut())
            {
                return interactable != null ? interactable : _timeOutInteractable;
            }
            return interactable;
        }

        private SnapInteractable ComputeIntersectingCandidate()
        {
            Vector3 position = Rigidbody.transform.position;
            SnapInteractable closestInteractable = null;
            float bestScore = float.MaxValue;
            float score = bestScore;
            bool closestPointIsInside = false;

            IEnumerable<SnapInteractable> interactables = SnapInteractable.Registry.List(this);
            foreach (SnapInteractable interactable in interactables)
            {
                Collider[] colliders = interactable.Colliders;
                foreach (Collider collider in colliders)
                {
                    bool isPointInsideCollider = Collisions.IsPointWithinCollider(position, collider);
                    if (!isPointInsideCollider && closestPointIsInside)
                    {
                        continue;
                    }
                    Vector3 measuringPoint = isPointInsideCollider ? collider.bounds.center : collider.ClosestPoint(position);
                    score = (position - measuringPoint).sqrMagnitude;
                    if (score < bestScore || (isPointInsideCollider && !closestPointIsInside))
                    {
                        bestScore = score;
                        closestInteractable = interactable;
                        closestPointIsInside = isPointInsideCollider;
                    }
                }
            }
            return closestInteractable;
        }

        #region Inject

        public void InjectAllSnapInteractor(PointableElement pointableElement, Rigidbody rigidbody)
        {
            InjectPointableElement(pointableElement);
            InjectRigidbody(rigidbody);
        }

        public void InjectPointableElement(PointableElement pointableElement)
        {
            _pointableElement = pointableElement;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalSnapPoint(Transform snapPoint)
        {
            _snapPoint = snapPoint;
        }

        public void InjectOptionalTimeOutInteractable(SnapInteractable interactable)
        {
            _timeOutInteractable = interactable;
        }

        public void InjectOptionaTimeOut(float timeOut)
        {
            _timeOut = timeOut;
        }
        #endregion
    }
}
