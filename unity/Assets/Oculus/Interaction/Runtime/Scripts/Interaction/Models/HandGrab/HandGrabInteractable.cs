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
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Serializable data-only version of the HandGrabInteractable so it can be stored when they
    /// are generated at Play-Mode (where Hand-tracking works).
    /// </summary>
    [Serializable]
    public struct HandGrabInteractableData
    {
        public List<HandGrabPoseData> poses;
        public GrabTypeFlags grabType;
        public HandAlignType handAlignment;

        public PoseMeasureParameters scoringModifier;
        public GrabbingRule pinchGrabRules;
        public GrabbingRule palmGrabRules;
    }

    /// <summary>
    /// A HandGrabInteractable indicates the properties about how a hand can snap to an object.
    /// The most important is the position/rotation and finger rotations for the hand,
    /// but it can also contain extra information like a valid holding surface (instead of just
    /// a single point) or a visual representation (using a hand-ghost)
    /// </summary>
    [Serializable]
    public class HandGrabInteractable : PointerInteractable<HandGrabInteractor, HandGrabInteractable>,
        IRigidbodyRef, IHandGrabbable
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

        [SerializeField]
        private PoseMeasureParameters _scoringModifier = new PoseMeasureParameters(0.1f, 0.8f);

        [Space]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;
        [SerializeField]
        private GrabbingRule _pinchGrabRules = GrabbingRule.DefaultPinchRule;
        [SerializeField]
        private GrabbingRule _palmGrabRules = GrabbingRule.DefaultPalmRule;

        [Header("Movement")]
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private MonoBehaviour _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

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

        public PoseMeasureParameters ScoreModifier => _scoringModifier;

        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public GrabbingRule PinchGrabRules => _pinchGrabRules;
        public GrabbingRule PalmGrabRules => _palmGrabRules;

        public List<HandGrabPose> HandGrabPoses => _handGrabPoses;

        public Collider[] Colliders { get; private set; }

        private GrabPoseFinder _grabPoseFinder;

        private static CollisionInteractionRegistry<HandGrabInteractor, HandGrabInteractable> _registry = null;

        #region editor events
        protected virtual void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();

            Grabbable grabbable = this.GetComponentInParent<Grabbable>();
            if (grabbable != null)
            {
                InjectOptionalPointableElement(grabbable);
            }
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            if (_rigidbody == null)
            {
                _rigidbody = this.GetComponentInParent<Rigidbody>();
            }
            Colliders = Rigidbody.GetComponentsInChildren<Collider>();
            if (_registry == null)
            {
                _registry = new CollisionInteractionRegistry<HandGrabInteractor, HandGrabInteractable>();
                SetRegistry(_registry);
            }
            MovementProvider = _movementProvider as IMovementProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(Rigidbody, "The Rigidbody can not be null");
            Assert.IsTrue(Colliders.Length > 0, "This interactable needs to have at least one collider");
            if (MovementProvider == null)
            {
                IMovementProvider movementProvider;
                if (HandGrabPoses.Count > 0)
                {
                    movementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                }
                else
                {
                    movementProvider = this.gameObject.AddComponent<MoveFromTargetProvider>();
                }
                InjectOptionalMovementProvider(movementProvider);
            }

            _grabPoseFinder = new GrabPoseFinder(_handGrabPoses, RelativeTo, this.transform);
            this.EndStart(ref _started);
        }

        #region pose moving

        public IMovement GenerateMovement(in Pose from, in Pose to)
        {
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(from);
            movement.MoveTo(to);
            return movement;
        }

        public bool CalculateBestPose(Pose userPose, float handScale, Handedness handedness,
            ref HandGrabResult result)
        {
            return _grabPoseFinder.FindBestPose(userPose, handScale, handedness, _scoringModifier, ref result);
        }

        public bool UsesHandPose()
        {
            return _grabPoseFinder.UsesHandPose();
        }

        #endregion

        #region generation
        /// <summary>
        /// Creates a new HandGrabInteractable under the given object
        /// </summary>
        /// <param name="parent">The relative object for the interactable</param>
        /// <param name="name">Name for the GameObject holding this interactable</param>
        /// <returns>An non-populated HandGrabInteractable</returns>
        public static HandGrabInteractable Create(Transform parent, string name = null)
        {
            GameObject go = new GameObject(name ?? "HandGrabInteractable");
            go.transform.SetParent(parent, false);
            HandGrabInteractable record = go.AddComponent<HandGrabInteractable>();
            return record;
        }

        public HandGrabPose CreatePoint()
        {
            GameObject go = this.gameObject;
            if (this.TryGetComponent(out HandGrabPose point))
            {
                go = new GameObject("HandGrab Point");
                go.transform.SetParent(this.transform, false);
            }
            HandGrabPose record = go.AddComponent<HandGrabPose>();
            return record;
        }
        #endregion

        #region dataSave
        /// <summary>
        /// Serializes the data of the HandGrabInteractable so it can be stored
        /// </summary>
        /// <returns>The struct data to recreate the interactable</returns>
        public HandGrabInteractableData SaveData()
        {
            return new HandGrabInteractableData()
            {
                poses = _handGrabPoses.Select(p => p.SaveData()).ToList(),
                scoringModifier = _scoringModifier,
                grabType = _supportedGrabTypes,
                handAlignment = _handAligment,
                pinchGrabRules = _pinchGrabRules,
                palmGrabRules = _palmGrabRules
            };
        }

        /// <summary>
        /// Populates the HandGrabInteractable with the serialized data version
        /// </summary>
        /// <param name="data">The serialized data for the HandGrabInteractable.</param>
        public void LoadData(HandGrabInteractableData data)
        {
            _supportedGrabTypes = data.grabType;
            _handAligment = data.handAlignment;
            _pinchGrabRules = data.pinchGrabRules;
            _palmGrabRules = data.palmGrabRules;
            _scoringModifier = data.scoringModifier;

            if (data.poses != null)
            {
                foreach (HandGrabPoseData posesData in data.poses)
                {
                    LoadHandGrabPose(posesData);
                }
            }
        }

        public HandGrabPose LoadHandGrabPose(HandGrabPoseData poseData)
        {
            HandGrabPose point = CreatePoint();
            point.LoadData(poseData, this.RelativeTo);
            _handGrabPoses.Add(point);
            return point;
        }
        #endregion

        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_physicsGrabbable == null)
            {
                return;
            }
            _physicsGrabbable.ApplyVelocities(linearVelocity, angularVelocity);
        }

        #region Inject

        public void InjectAllHandGrabInteractable(Rigidbody rigidbody,
            GrabTypeFlags supportedGrabTypes,
            GrabbingRule pinchGrabRules, GrabbingRule palmGrabRules)
        {
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectPinchGrabRules(pinchGrabRules);
            InjectPalmGrabRules(palmGrabRules);
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

        public void InjectOptionalPhysicsGrabbable(PhysicsGrabbable physicsGrabbable)
        {
            _physicsGrabbable = physicsGrabbable;
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
