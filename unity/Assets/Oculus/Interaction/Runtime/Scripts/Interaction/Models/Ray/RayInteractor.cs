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
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction
{
    public class RayInteractor : PointerInteractor<RayInteractor, RayInteractable>
    {
        [SerializeField, Interface(typeof(ISelector))]
        private MonoBehaviour _selector;

        [SerializeField]
        private Transform _rayOrigin;

        [SerializeField]
        private float _maxRayLength = 5f;

        private RayCandidate _rayCandidate = null;

        private IMovement _movement;
        private SurfaceHit _movedHit;
        private Pose _movementHitDelta = Pose.identity;

        public Vector3 Origin { get; protected set; }
        public Quaternion Rotation { get; protected set; }
        public Vector3 Forward { get; protected set; }
        public Vector3 End { get; set; }

        public float MaxRayLength
        {
            get
            {
                return _maxRayLength;
            }
            set
            {
                _maxRayLength = value;
            }
        }

        public SurfaceHit? CollisionInfo { get; protected set; }
        public Ray Ray { get; protected set; }

        protected override void Awake()
        {
            base.Awake();
            Selector = _selector as ISelector;
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(Selector);
            Assert.IsNotNull(_rayOrigin);
        }

        protected override void DoPreprocess()
        {
            Origin = _rayOrigin.transform.position;
            Rotation = _rayOrigin.transform.rotation;
            Forward = Rotation * Vector3.forward;
            Ray = new Ray(Origin, Forward);
        }

        public class RayCandidate : ICandidatePosition
        {
            public RayInteractable ClosestInteractable { get; }
            public Vector3 CandidatePosition { get; }
            public RayCandidate(RayInteractable closestInteractable, Vector3 candidatePosition)
            {
                ClosestInteractable = closestInteractable;
                CandidatePosition = candidatePosition;
            }
        }

        public override object Candidate => _rayCandidate;

        protected override RayInteractable ComputeCandidate()
        {
            CollisionInfo = null;

            RayInteractable closestInteractable = null;
            float closestDist = float.MaxValue;
            Vector3 candidatePosition = Vector3.zero;
            IEnumerable<RayInteractable> interactables = RayInteractable.Registry.List(this);

            foreach (RayInteractable interactable in interactables)
            {
                if (interactable.Raycast(Ray, out SurfaceHit hit, MaxRayLength, false))
                {
                    if (hit.Distance < closestDist)
                    {
                        closestDist = hit.Distance;
                        closestInteractable = interactable;
                        CollisionInfo = hit;
                        candidatePosition = hit.Point;
                    }
                }
            }

            float rayDist = (closestInteractable != null ? closestDist : MaxRayLength);
            End = Origin + rayDist * Forward;

            _rayCandidate = new RayCandidate(closestInteractable, candidatePosition);

            return closestInteractable;
        }

        protected override void InteractableSelected(RayInteractable interactable)
        {
            if (interactable != null)
            {
                _movedHit = CollisionInfo.Value;
                Pose hitPose = new Pose(_movedHit.Point, Quaternion.LookRotation(_movedHit.Normal));
                Pose backHitPose = new Pose(_movedHit.Point, Quaternion.LookRotation(-_movedHit.Normal));
                _movement = interactable.GenerateMovement(_rayOrigin.GetPose(), backHitPose);
                if (_movement != null)
                {
                    _movementHitDelta = PoseUtils.Delta(_movement.Pose, hitPose);
                }
            }
            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(RayInteractable interactable)
        {
            if (_movement != null)
            {
                _movement.StopAndSetPose(_movement.Pose);
            }
            base.InteractableUnselected(interactable);
            _movement = null;
        }

        protected override void DoSelectUpdate()
        {
            RayInteractable interactable = _selectedInteractable;

            if (_movement != null)
            {
                _movement.UpdateTarget(_rayOrigin.GetPose());
                _movement.Tick();
                Pose hitPoint = PoseUtils.Multiply(_movement.Pose, _movementHitDelta);
                _movedHit.Point = hitPoint.position;
                _movedHit.Normal = hitPoint.forward;
                CollisionInfo = _movedHit;
                End = _movedHit.Point;
                return;
            }

            CollisionInfo = null;
            if (interactable != null &&
                interactable.Raycast(Ray, out SurfaceHit hit, MaxRayLength, true))
            {
                End = hit.Point;
                CollisionInfo = hit;
            }
            else
            {
                End = Origin + MaxRayLength * Forward;
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (_movement != null)
            {
                return _movement.Pose;
            }

            if (CollisionInfo != null)
            {
                Vector3 position = CollisionInfo.Value.Point;
                Quaternion rotation = Quaternion.LookRotation(CollisionInfo.Value.Normal);
                return new Pose(position, rotation);
            }
            return new Pose(Vector3.zero, Quaternion.identity);
        }

        #region Inject
        public void InjectAllRayInteractor(ISelector selector, Transform rayOrigin)
        {
            InjectSelector(selector);
            InjectRayOrigin(rayOrigin);
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as MonoBehaviour;
            Selector = selector;
        }

        public void InjectRayOrigin(Transform rayOrigin)
        {
            _rayOrigin = rayOrigin;
        }
        #endregion
    }
}
