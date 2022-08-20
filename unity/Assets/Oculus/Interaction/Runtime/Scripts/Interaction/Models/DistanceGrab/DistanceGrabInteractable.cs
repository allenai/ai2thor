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

using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// This interactable is used for grabbing items at a distance.
    /// Upon selection the Movement Provider specifies how the grabber and the grabbable will be aligned, by
    /// default this can be moving the object towards a controller, but it could also enable other scenarios such as
    /// moving it with deltas in its own place or allowing a pull motion, etc.
    /// </summary>
    public class DistanceGrabInteractable : PointerInteractable<DistanceGrabInteractor, DistanceGrabInteractable>,
        IRigidbodyRef, IDistanceInteractable
    {
        private Collider[] _colliders;
        public Collider[] Colliders => _colliders;

        [SerializeField]
        Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField, Optional]
        private Transform _grabSource;

        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;

        [SerializeField, Optional]
        private PhysicsGrabbable _physicsGrabbable = null;

        /// <summary>
        /// The movement provider specifies how the selected interactable will
        /// align with the grabber.
        /// </summary>
        [Header("Snap")]
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private MonoBehaviour _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        #region Properties
        public bool ResetGrabOnGrabsUpdated
        {
            get
            {
                return _resetGrabOnGrabsUpdated;
            }
            set
            {
                _resetGrabOnGrabsUpdated = value;
            }
        }

        public Transform RelativeTo => _grabSource;

        #endregion

        #region Editor Events

        protected virtual void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
            _physicsGrabbable = this.GetComponentInParent<PhysicsGrabbable>();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            MovementProvider = _movementProvider as IMovementProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(Rigidbody);
            _colliders = Rigidbody.GetComponentsInChildren<Collider>();
            if (MovementProvider == null)
            {
                MoveTowardsTargetProvider movementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            if (_grabSource == null)
            {
                _grabSource = Rigidbody.transform;
            }
            this.EndStart(ref _started);
        }

        public IMovement GenerateMovement(in Pose to)
        {
            Pose source = RelativeTo.GetPose();
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(source);
            movement.MoveTo(to);
            return movement;
        }

        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_physicsGrabbable == null)
            {
                return;
            }
            _physicsGrabbable.ApplyVelocities(linearVelocity, angularVelocity);
        }

        #region Inject

        public void InjectAllGrabInteractable(Rigidbody rigidbody)
        {
            InjectRigidbody(rigidbody);
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalGrabSource(Transform grabSource)
        {
            _grabSource = grabSource;
        }

        public void InjectOptionalPhysicsGrabbable(PhysicsGrabbable physicsGrabbable)
        {
            _physicsGrabbable = physicsGrabbable;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as MonoBehaviour;
            MovementProvider = provider;
        }
        #endregion
    }
}
