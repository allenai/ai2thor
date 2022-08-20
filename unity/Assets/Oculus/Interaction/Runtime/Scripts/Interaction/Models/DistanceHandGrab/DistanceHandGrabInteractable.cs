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

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The DistanceHandGrabInteractable allows grabbing the marked object from far away.
    /// Internally it uses HandGrabPoses to specify not only the poses of the hands but the
    /// required gestures to perform the grab. It is possible (and recommended) to reuse the same
    /// HandGrabPoses used by the HandGrabInteractable, and even select just a few so they become
    /// the default poses when distant grabbing.
    /// </summary>
    [Serializable]
    public class DistanceHandGrabInteractable : PointerInteractable<DistanceHandGrabInteractor, DistanceHandGrabInteractable>,
        IRigidbodyRef, IHandGrabbable, IDistanceInteractable
    {
        [Header("Grab")]

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;
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

        [SerializeField, Optional]
        private PhysicsGrabbable _physicsGrabbable = null;

        [Space]
        /// <summary>
        /// The available grab types dictates the available gestures for grabbing
        /// this interactable.
        /// </summary>
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;
        [SerializeField]
        private GrabbingRule _pinchGrabRules = GrabbingRule.DefaultPinchRule;
        [SerializeField]
        private GrabbingRule _palmGrabRules = GrabbingRule.DefaultPalmRule;

        /// <summary>
        /// The movement provider specifies how the selected interactable will
        /// align with the grabber.
        /// </summary>
        [Header("Snap")]
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private MonoBehaviour _movementProvider;
        public IMovementProvider MovementProvider { get; set; }

        [SerializeField]
        private HandAlignType _handAligment = HandAlignType.AlignOnGrab;
        public HandAlignType HandAlignment
        {
            get
            {
                return _handAligment;
            }
            set
            {
                _handAligment = value;
            }
        }

        [SerializeField, Optional]
        [UnityEngine.Serialization.FormerlySerializedAs("_handGrabPoints")]
        private List<HandGrabPose> _handGrabPoses = new List<HandGrabPose>();
        /// <summary>
        /// General getter for the transform of the object this interactable refers to.
        /// </summary>
        public Transform RelativeTo => _rigidbody.transform;

        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public GrabbingRule PinchGrabRules => _pinchGrabRules;
        public GrabbingRule PalmGrabRules => _palmGrabRules;

        public List<HandGrabPose> HandGrabPoses => _handGrabPoses;
        public Collider[] Colliders { get; private set; }

        private GrabPoseFinder _grabPoseFinder;

        private readonly PoseMeasureParameters SCORE_MODIFIER = new PoseMeasureParameters(0.1f, 1f);

        #region editor events
        protected virtual void Reset()
        {
            if (this.TryGetComponent(out HandGrabInteractable handGrabInteractable))
            {
                _pinchGrabRules = handGrabInteractable.PinchGrabRules;
                _palmGrabRules = handGrabInteractable.PalmGrabRules;
                _supportedGrabTypes = handGrabInteractable.SupportedGrabTypes;
                _handGrabPoses = new List<HandGrabPose>(handGrabInteractable.HandGrabPoses);
                _rigidbody = handGrabInteractable.Rigidbody;
            }
            else
            {
                _rigidbody = this.GetComponentInParent<Rigidbody>();
                _physicsGrabbable = this.GetComponentInParent<PhysicsGrabbable>();
            }
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
            Assert.IsNotNull(Rigidbody, "The rigidbody is missing");
            Colliders = Rigidbody.GetComponentsInChildren<Collider>();
            Assert.IsTrue(Colliders.Length > 0,
                "The associated Rigidbody must have at least one Collider.");
            if (MovementProvider == null)
            {
                MoveTowardsTargetProvider movementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            _grabPoseFinder = new GrabPoseFinder(_handGrabPoses, RelativeTo, this.transform);
            this.EndStart(ref _started);
        }

        public IMovement GenerateMovement(in Pose from, in Pose to)
        {
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(from);
            movement.MoveTo(to);
            return movement;
        }

        public bool CalculateBestPose(in Pose userPose, float handScale, Handedness handedness,
            ref HandGrabResult result)
        {
            return _grabPoseFinder.FindBestPose(userPose, handScale, handedness, SCORE_MODIFIER, ref result);
        }

        public bool UsesHandPose()
        {
            return _grabPoseFinder.UsesHandPose();
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

        public void InjectAllDistanceHandGrabInteractable(Rigidbody rigidbody,
            GrabTypeFlags supportedGrabTypes,
            GrabbingRule pinchGrabRules, GrabbingRule palmGrabRules)
        {
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectPinchGrabRules(pinchGrabRules);
            InjectPalmGrabRules(palmGrabRules);
        }

        public void InjectOptionalPhysicsObject(PhysicsGrabbable physicsObject)
        {
            _physicsGrabbable = physicsObject;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectPinchGrabRules(GrabbingRule pinchGrabRules)
        {
            _pinchGrabRules = pinchGrabRules;
        }

        public void InjectPalmGrabRules(GrabbingRule palmGrabRules)
        {
            _palmGrabRules = palmGrabRules;
        }

        public void InjectOptionalHandGrabPoses(List<HandGrabPose> handGrabPoses)
        {
            _handGrabPoses = handGrabPoses;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as MonoBehaviour;
            MovementProvider = provider;
        }
        #endregion
    }
}
