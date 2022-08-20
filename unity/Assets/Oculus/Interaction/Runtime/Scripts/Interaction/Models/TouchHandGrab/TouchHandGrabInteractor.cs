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
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;

namespace Oculus.Interaction
{
    /// <summary>
    /// TouchHandGrabInteractor provides a hand-specific grab interaction model
    /// where selection begins when finger tips overlap with an associated interactable.
    /// Upon selection, the distance between the fingers and thumb is cached and is used for
    /// determining the point of release: when fingers are outside of the cached distance.
    /// </summary>
    public class
        TouchHandGrabInteractor : PointerInteractor<TouchHandGrabInteractor, TouchHandGrabInteractable>
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand { get; set; }

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _openHand;
        private IHand OpenHand { get; set; }

        [SerializeField, Interface(typeof(IHandSphereMap))]
        private MonoBehaviour _handSphereMap;
        protected IHandSphereMap HandSphereMap;

        [SerializeField]
        private Transform _hoverLocation;

        [SerializeField]
        private Transform _grabLocation;

        [SerializeField]
        private float _minHoverDistance = 0.05f;

        [SerializeField]
        private float _curlDeltaThreshold = 3f;

        [SerializeField]
        private float _curlTimeThreshold = 0.05f;

        [SerializeField]
        private int _iterations = 10;

        public event Action WhenFingerLocked = delegate() { };

        private Vector3 _saveOffset = Vector3.zero;

        private Vector3 GrabOffset = Vector3.zero;
        private Vector3 GrabPosition => _grabLocation.position;
        private Quaternion GrabRotation => _grabLocation.rotation;

        private class FingerStatus
        {
            public bool Locked = false;
            public bool Selecting = false;
            public HandJointId[] Joints;
            public Pose[] LocalJoints;
            public float CurlValueAtLock = 0f;
            public float Timer = 0f;
        }

        private FingerStatus[] _fingerStatuses;
        private TouchShadowHand _touchShadowHand;
        private ShadowHand _fromShadow;
        private ShadowHand _toShadow;
        private ShadowHand _openShadow;
        private Func<float> _timeProvider;
        private bool _firstSelect = false;
        private float _previousTime;
        private float _deltaTime;

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            OpenHand = _openHand as IHand;
            HandSphereMap = _handSphereMap as IHandSphereMap;

            _fingerStatuses = new FingerStatus[Constants.NUM_FINGERS];
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                int[] jointIndices = FingersMetadata.FINGER_TO_JOINT_INDEX[i];
                HandJointId[] joints = new HandJointId[jointIndices.Length];
                for (int j = 0; j < jointIndices.Length; j++)
                {
                    joints[j] = FingersMetadata.HAND_JOINT_IDS[jointIndices[j]];
                }

                _fingerStatuses[i] = new FingerStatus()
                {
                    Joints = joints,
                    LocalJoints = new Pose[joints.Length]
                };
            }

            _timeProvider = () => Time.time;
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_hoverLocation);
            Assert.IsNotNull(_grabLocation);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(OpenHand);
            Assert.IsNotNull(HandSphereMap);
            Assert.IsTrue(_iterations > 0);

            _touchShadowHand = new TouchShadowHand(HandSphereMap, Hand.Handedness, _iterations);
            _fromShadow = new ShadowHand();
            _toShadow = new ShadowHand();
            _openShadow = new ShadowHand();
            _fromShadow.FromHand(Hand);
            _toShadow.FromHand(Hand);
            _previousTime = _timeProvider();
            _deltaTime = 0;
        }

        public bool IsFingerLocked(HandFinger finger)
        {
            if (State == InteractorState.Select && _selectedInteractable == null)
            {
                return false;
            }
            return _fingerStatuses[(int)finger].Locked;
        }

        public Pose[] GetFingerJoints(HandFinger finger)
        {
            return _fingerStatuses[(int)finger].LocalJoints;
        }

        protected override void DoPreprocess()
        {
            base.DoPreprocess();
            _toShadow.FromHand(Hand);

            float currentTime = _timeProvider();
            _deltaTime = _timeProvider() - _previousTime;
            _previousTime = currentTime;
        }

        protected override void DoPostprocess()
        {
            if(State != InteractorState.Select && _interactable != null)
            {
                _fromShadow.FromHand(Hand);
            }
            else
            {
                _fromShadow.FromHandRoot(Hand);
                for (int j = 0; j < Constants.NUM_FINGERS; j++)
                {
                    FingerStatus fingerStatus = _fingerStatuses[j];
                    if (!fingerStatus.Locked)
                    {
                        for (int i = 0; i < fingerStatus.Joints.Length; i++)
                        {
                            HandJointId jointId = fingerStatus.Joints[i];
                            if (Hand.GetJointPoseLocal(jointId, out Pose localPose))
                            {
                                _fromShadow.SetLocalPose(jointId, localPose);
                            }
                        }
                    }
                }
            }
            base.DoPostprocess();
        }

        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _candidate == _interactable && HandStatusSelecting();
            }
        }

        public override bool ShouldUnselect {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return !HandStatusSelecting();
            }
        }

        protected override void DoHoverUpdate()
        {
            TouchHandGrabInteractable closestInteractable = _interactable;
            if (closestInteractable == null) return;

            TouchShadowHand.GrabTouchInfo output = new TouchShadowHand.GrabTouchInfo();

            _touchShadowHand.GrabTouch(_fromShadow, _toShadow, closestInteractable.ColliderGroup,
                false, output);
            if (!output.grabbing)
            {
                _touchShadowHand.GrabTouch(_fromShadow, _toShadow, closestInteractable.ColliderGroup,
                    true, output);
            }

            if (!output.grabbing)
            {
                return;
            }

            _touchShadowHand.SetShadowRootFromHands(_fromShadow, _toShadow, output.grabT);
            for (int i = 0; i < _fingerStatuses.Length; i++)
            {
                FingerStatus fingerStatus = _fingerStatuses[i];
                ComputeNewTouching(i, _interactable.ColliderGroup, output.offset);

                // We are overlapping at start, try pushout
                if (output.grabbingFingers[i] && !_fingerStatuses[i].Locked)
                {
                    _openShadow.FromHand(OpenHand, OpenHand.Handedness != Hand.Handedness);
                    if(!_touchShadowHand.PushoutFinger(i, _fromShadow, _openShadow,
                        _interactable.ColliderGroup, output.offset))
                    {
                        continue;
                    }

                    // Save the pre-touching locations
                    for (int j = 0; j < fingerStatus.Joints.Length; j++)
                    {
                        HandJointId jointId = fingerStatus.Joints[j];
                        _fromShadow.SetLocalPose(jointId, _touchShadowHand.ShadowHand.GetLocalPose(jointId));
                    }

                    ComputeNewTouching(i, _interactable.ColliderGroup, output.offset);
                }
            }

            if (!HandStatusSelecting())
            {
                for (int i = 0; i < _fingerStatuses.Length; i++)
                {
                    _fingerStatuses[i].Locked = false;
                    _fingerStatuses[i].Selecting = false;
                }
            }
            else
            {
                GrabOffset = Vector3.zero;
                _saveOffset = Quaternion.Inverse(GrabRotation) * output.offset;
                _firstSelect = true;
            }

            WhenFingerLocked();
        }

        private bool HandStatusSelecting()
        {
            return _fingerStatuses[0].Selecting &&
                   (_fingerStatuses[1].Selecting ||
                    _fingerStatuses[2].Selecting ||
                    _fingerStatuses[3].Selecting ||
                    _fingerStatuses[4].Selecting);
        }

        // Given a touch hand root, conform fingers from _fromShadow to _toShadow until touching
        private void ComputeNewTouching(int idx, ColliderGroup colliderGroup, Vector3 offset)
        {
            FingerStatus fingerStatus = _fingerStatuses[idx];

            // Ignore locked
            if (fingerStatus.Locked)
            {
                return;
            }

            _touchShadowHand.SetShadowFingerFrom(idx, _fromShadow);

            // Check if finger is starting within
            if (_touchShadowHand.CheckFingerTouch(idx, 0, colliderGroup, offset, null))
            {
                return;
            }

            if(!_touchShadowHand.GrabConformFinger(idx, _fromShadow, _toShadow, colliderGroup, offset))
            {
                return;
            }

            // We are touching, lock the finger
            fingerStatus.Locked = true;
            fingerStatus.Selecting = true;
            fingerStatus.Timer = 0f;

            // Save the locked joints
            _touchShadowHand.GetJointsFromShadow(fingerStatus.Joints, fingerStatus.LocalJoints, true);

            // Save the curl value of the conformed finger
            Pose[] worldPoses = new Pose[fingerStatus.Joints.Length];
            for (int i = 0; i < fingerStatus.Joints.Length; i++)
            {
                worldPoses[i] = _touchShadowHand.ShadowHand.GetWorldPose(fingerStatus.Joints[i]);
            }
            fingerStatus.CurlValueAtLock = FingerShapes.PosesListCurlValue(worldPoses);

            // Save the touching locations
            for (int i = 0; i < fingerStatus.Joints.Length; i++)
            {
                HandJointId jointId = fingerStatus.Joints[i];
                _fromShadow.SetLocalPose(jointId, _touchShadowHand.ShadowHand.GetLocalPose(jointId));
            }
        }

        private void ComputeNewRelease(int idx, ColliderGroup colliderGroup, Vector3 offset)
        {
            FingerStatus fingerStatus = _fingerStatuses[idx];
            // Ignore unlocked
            if (!fingerStatus.Locked)
            {
                return;
            }

            Pose[] worldPoses = new Pose[fingerStatus.Joints.Length];
            for (int i = 0; i < fingerStatus.Joints.Length; i++)
            {
                worldPoses[i] = _toShadow.GetWorldPose(fingerStatus.Joints[i]);
            }

            // If not curling out, return
            float toCurl = FingerShapes.PosesListCurlValue(worldPoses);
            if (toCurl >= fingerStatus.CurlValueAtLock - _curlDeltaThreshold)
            {
                fingerStatus.Timer = 0f;
                return;
            }

            // Check if finger releases
            if(!_touchShadowHand.GrabReleaseFinger(idx, _fromShadow, _toShadow, colliderGroup, offset))
            {
                fingerStatus.Timer = 0f;
                return;
            }

            fingerStatus.Timer += _deltaTime;
            if (fingerStatus.Timer < _curlTimeThreshold)
            {
                return;
            }

            // If so, unlock
            fingerStatus.Locked = false;
            fingerStatus.Selecting = false;
        }

        protected override void DoSelectUpdate()
        {
            if (_firstSelect)
            {
                GrabOffset = _saveOffset;
                _saveOffset = Vector3.zero;
                _firstSelect = false;
                return;
            }

            TouchHandGrabInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                for (int i = 0; i < _fingerStatuses.Length; i++)
                {
                    FingerStatus fingerStatus = _fingerStatuses[i];
                    if (!fingerStatus.Locked)
                    {
                        continue;
                    }

                    fingerStatus.Selecting = true;
                    fingerStatus.Locked = false;
                    fingerStatus.Timer = 0f;

                    Pose[] worldPoses = new Pose[fingerStatus.Joints.Length];
                    for (int j = 0; j < fingerStatus.Joints.Length; j++)
                    {
                        worldPoses[j] = _toShadow.GetWorldPose(fingerStatus.Joints[j]);
                    }
                    fingerStatus.CurlValueAtLock = FingerShapes.PosesListCurlValue(worldPoses);
                }

                for (int i = 0; i < _fingerStatuses.Length; i++)
                {
                    FingerStatus fingerStatus = _fingerStatuses[i];
                    if (!fingerStatus.Selecting)
                    {
                        continue;
                    }

                    Pose[] worldPoses = new Pose[fingerStatus.Joints.Length];
                    for (int j = 0; j < fingerStatus.Joints.Length; j++)
                    {
                        worldPoses[j] = _toShadow.GetWorldPose(fingerStatus.Joints[j]);
                    }

                    float curlValue = FingerShapes.PosesListCurlValue(worldPoses);
                    if (curlValue >= fingerStatus.CurlValueAtLock - _curlDeltaThreshold)
                    {
                        fingerStatus.Timer = 0f;
                        continue;
                    }

                    fingerStatus.Timer += _deltaTime;
                    if (fingerStatus.Timer < _curlTimeThreshold)
                    {
                        return;
                    }

                    fingerStatus.Selecting = false;
                }

                return;
            }

            _touchShadowHand.ShadowHand.Copy(_fromShadow);

            _touchShadowHand.SetShadowRootFromHand(_fromShadow);
            for (int i = 0; i < _fingerStatuses.Length; i++)
            {
                if (_fingerStatuses[i].Locked)
                {
                    ComputeNewRelease(i, interactable.ColliderGroup, Vector3.zero);
                }
                else
                {
                    ComputeNewTouching(i, interactable.ColliderGroup, Vector3.zero);
                }
            }

            WhenFingerLocked();
        }

        public override void Unselect()
        {
            if (!ShouldUnselect)
            {
                base.Unselect();
                return;
            }

            for (int i = 0; i < _fingerStatuses.Length; i++)
            {
                _fingerStatuses[i].Locked = false;
                _fingerStatuses[i].Selecting = false;
            }

            GrabOffset = Vector3.zero;

            WhenFingerLocked();

            base.Unselect();
        }

        protected override TouchHandGrabInteractable ComputeCandidate()
        {
            TouchHandGrabInteractable closest = null;
            float minSqrDist = float.MaxValue;
            foreach (TouchHandGrabInteractable interactable in TouchHandGrabInteractable.Registry
                .List())
            {
                foreach (Collider collider in interactable.ColliderGroup.Colliders)
                {
                    Vector3 closestPoint = collider.ClosestPoint(_hoverLocation.position);
                    float sqrDist = (closestPoint - _hoverLocation.position).sqrMagnitude;
                    if (sqrDist < minSqrDist && sqrDist < _minHoverDistance * _minHoverDistance)
                    {
                        minSqrDist = sqrDist;
                        closest = interactable;
                    }
                }
            }

            return closest;
        }

        protected override Pose ComputePointerPose()
        {
            return new Pose(GrabPosition + GrabRotation*GrabOffset, GrabRotation);
        }

        #region Inject

        public void InjectAllTouchHandGrabInteractor(
            IHand hand,
            IHand openHand,
            IHandSphereMap handSphereMap,
            Transform hoverLocation,
            Transform grabLocation)
        {
            InjectHand(hand);
            InjectOpenHand(openHand);
            InjectHandSphereMap(handSphereMap);
            InjectHoverLocation(hoverLocation);
            InjectGrabLocation(grabLocation);
        }

        public void InjectHand(IHand hand)
        {
            Hand = hand;
            _hand = hand as MonoBehaviour;
        }

        public void InjectOpenHand(IHand openHand)
        {
            OpenHand = openHand;
            _openHand = openHand as MonoBehaviour;
        }

        public void InjectHandSphereMap(IHandSphereMap handSphereMap)
        {
            HandSphereMap = handSphereMap;
            _handSphereMap = handSphereMap as MonoBehaviour;
        }

        public void InjectHoverLocation(Transform hoverLocation)
        {
            _hoverLocation = hoverLocation;
        }

        public void InjectGrabLocation(Transform grabLocation)
        {
            _grabLocation = grabLocation;
        }

        public void InjectOptionalMinHoverDistance(float minHoverDistance)
        {
            _minHoverDistance = minHoverDistance;
        }

        public void InjectOptionalCurlDeltaThreshold(float threshold)
        {
            _curlDeltaThreshold = threshold;
        }

        public void InjectOptionalCurlTimeThreshold(float seconds)
        {
            _curlTimeThreshold = seconds;
        }

        public void InjectOptionalIterations(int iterations)
        {
            _iterations = iterations;
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        #endregion
    }
}
