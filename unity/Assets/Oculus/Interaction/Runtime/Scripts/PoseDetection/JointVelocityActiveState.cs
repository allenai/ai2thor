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
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace Oculus.Interaction.PoseDetection
{
    public class JointVelocityActiveState : MonoBehaviour, IActiveState
    {
        public enum RelativeTo
        {
            Hand = 0,
            World = 1,
            Head = 2,
        }

        public enum WorldAxis
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
            PositiveZ = 4,
            NegativeZ = 5,
        }

        public enum HeadAxis
        {
            HeadForward = 0,
            HeadBackward = 1,
            HeadUp = 2,
            HeadDown = 3,
            HeadLeft = 4,
            HeadRight = 5,
        }

        public enum HandAxis
        {
            PalmForward = 0,
            PalmBackward = 1,
            WristUp = 2,
            WristDown = 3,
            WristForward = 4,
            WristBackward = 5,
        }

        [Serializable]
        public struct JointVelocityFeatureState
        {
            /// <summary>
            /// The world target vector for a
            /// <see cref="JointVelocityFeatureConfig"/>
            /// </summary>
            public readonly Vector3 TargetVector;

            /// <summary>
            /// The normalized joint velocity along the target
            /// vector relative to <see cref="_minVelocity"/>
            /// </summary>
            public readonly float Amount;

            public JointVelocityFeatureState(Vector3 targetVector, float velocity)
            {
                TargetVector = targetVector;
                Amount = velocity;
            }
        }

        [Serializable]
        public class JointVelocityFeatureConfigList
        {
            [SerializeField]
            private List<JointVelocityFeatureConfig> _values;

            public List<JointVelocityFeatureConfig> Values => _values;
        }

        [Serializable]
        public class JointVelocityFeatureConfig : FeatureConfigBase<HandJointId>
        {
            [SerializeField]
            private RelativeTo _relativeTo = RelativeTo.Hand;

            [SerializeField]
            private WorldAxis _worldAxis = WorldAxis.PositiveZ;

            [SerializeField]
            private HandAxis _handAxis = HandAxis.WristForward;

            [SerializeField]
            private HeadAxis _headAxis = HeadAxis.HeadForward;

            public RelativeTo RelativeTo => _relativeTo;
            public WorldAxis WorldAxis => _worldAxis;
            public HandAxis HandAxis => _handAxis;
            public HeadAxis HeadAxis => _headAxis;

        }

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField, Optional, Interface(typeof(IHmd))]
        private MonoBehaviour _hmd;
        public IHmd Hmd { get; private set; }

        [SerializeField]
        private JointVelocityFeatureConfigList _featureConfigs;

        [SerializeField, Min(0)]
        private float _minVelocity = 0.5f;

        [SerializeField, Min(0)]
        private float _thresholdWidth = 0.02f;

        [SerializeField, Min(0)]
        private float _minTimeInState = 0.05f;

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }

                UpdateActiveState();
                return _activeState;
            }
        }

        public IReadOnlyList<JointVelocityFeatureConfig> FeatureConfigs =>
            _featureConfigs.Values;

        public IReadOnlyDictionary<JointVelocityFeatureConfig, JointVelocityFeatureState> FeatureStates =>
            _featureStates;

        private Dictionary<JointVelocityFeatureConfig, JointVelocityFeatureState> _featureStates =
            new Dictionary<JointVelocityFeatureConfig, JointVelocityFeatureState>();

        private JointDeltaConfig _jointDeltaConfig;
        private JointDeltaProvider JointDeltaProvider { get; set; }

        private Func<float> _timeProvider;
        private int _lastStateUpdateFrame;
        private float _lastStateChangeTime;
        private float _lastUpdateTime;
        private bool _internalState;
        private bool _activeState;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            _timeProvider = () => Time.time;

            if (_hmd != null)
            {
                Hmd = _hmd as IHmd;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            Assert.IsNotNull(Hand);
            Assert.IsNotNull(FeatureConfigs);
            Assert.IsNotNull(_timeProvider);

            IList<HandJointId> allTrackedJoints = new List<HandJointId>();
            foreach (var config in FeatureConfigs)
            {
                allTrackedJoints.Add(config.Feature);
                _featureStates.Add(config, new JointVelocityFeatureState());

                Assert.IsTrue(config.RelativeTo != RelativeTo.Head || Hmd != null);
            }
            _jointDeltaConfig = new JointDeltaConfig(GetInstanceID(), allTrackedJoints);

            bool foundAspect = Hand.GetHandAspect(out JointDeltaProvider aspect);
            Assert.IsTrue(foundAspect);
            JointDeltaProvider = aspect;

            _lastUpdateTime = _timeProvider();
            this.EndStart(ref _started);
        }

        private bool CheckAllJointVelocities()
        {
            bool result = true;

            float deltaTime = _timeProvider() - _lastUpdateTime;
            float threshold = _internalState ?
                  _minVelocity + _thresholdWidth * 0.5f :
                  _minVelocity - _thresholdWidth * 0.5f;

            threshold *= deltaTime;

            foreach (var config in FeatureConfigs)
            {
                if (Hand.GetRootPose(out Pose rootPose) &&
                    Hand.GetJointPose(config.Feature, out Pose curPose) &&
                    JointDeltaProvider.GetPositionDelta(
                        config.Feature, out Vector3 worldDeltaDirection))
                {
                    Vector3 worldTargetDirection = GetWorldTargetVector(rootPose, config);
                    float velocityAlongTargetAxis =
                        Vector3.Dot(worldDeltaDirection, worldTargetDirection);

                    _featureStates[config] = new JointVelocityFeatureState(
                                             worldTargetDirection,
                                             threshold > 0 ?
                                             Mathf.Clamp01(velocityAlongTargetAxis / threshold) :
                                             1);

                    bool velocityExceedsThreshold = velocityAlongTargetAxis > threshold;
                    result &= velocityExceedsThreshold;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        protected virtual void Update()
        {
            UpdateActiveState();
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                JointDeltaProvider.RegisterConfig(_jointDeltaConfig);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                JointDeltaProvider.UnRegisterConfig(_jointDeltaConfig);
            }
        }

        private void UpdateActiveState()
        {
            if (Time.frameCount <= _lastStateUpdateFrame)
            {
                return;
            }
            _lastStateUpdateFrame = Time.frameCount;

            bool newState = CheckAllJointVelocities();

            if (newState != _internalState)
            {
                _internalState = newState;
                _lastStateChangeTime = _timeProvider();
            }

            if (_timeProvider() - _lastStateChangeTime >= _minTimeInState)
            {
                _activeState = _internalState;
            }
            _lastUpdateTime = _timeProvider();
        }

        private Vector3 GetWorldTargetVector(Pose rootPose, JointVelocityFeatureConfig config)
        {
            switch (config.RelativeTo)
            {
                default:
                case RelativeTo.Hand:
                    return GetHandAxisVector(config.HandAxis, rootPose);
                case RelativeTo.World:
                    return GetWorldAxisVector(config.WorldAxis);
                case RelativeTo.Head:
                    return GetHeadAxisVector(config.HeadAxis);
            }
        }

        private Vector3 GetWorldAxisVector(WorldAxis axis)
        {
            switch (axis)
            {
                default:
                case WorldAxis.PositiveX:
                    return Vector3.right;
                case WorldAxis.NegativeX:
                    return Vector3.left;
                case WorldAxis.PositiveY:
                    return Vector3.up;
                case WorldAxis.NegativeY:
                    return Vector3.down;
                case WorldAxis.PositiveZ:
                    return Vector3.forward;
                case WorldAxis.NegativeZ:
                    return Vector3.back;
            }
        }

        private Vector3 GetHandAxisVector(HandAxis axis, Pose rootPose)
        {
            Vector3 result;
            switch (axis)
            {
                case HandAxis.PalmForward:
                    result = Hand.Handedness == Handedness.Left ?
                        rootPose.up : -1.0f * rootPose.up;
                    break;
                case HandAxis.PalmBackward:
                    result = Hand.Handedness == Handedness.Left ?
                        -1.0f * rootPose.up : rootPose.up;
                    break;
                case HandAxis.WristUp:
                    result = Hand.Handedness == Handedness.Left ?
                        rootPose.forward : -1.0f * rootPose.forward;
                    break;
                case HandAxis.WristDown:
                    result = Hand.Handedness == Handedness.Left ?
                        -1.0f * rootPose.forward : rootPose.forward;
                    break;
                case HandAxis.WristForward:
                    result = Hand.Handedness == Handedness.Left ?
                        rootPose.right : -1.0f * rootPose.right;
                    break;
                case HandAxis.WristBackward:
                    result = Hand.Handedness == Handedness.Left ?
                        -1.0f * rootPose.right : rootPose.right;
                    break;
                default:
                    result = Vector3.zero;
                    break;
            }
            return result;
        }

        private Vector3 GetHeadAxisVector(HeadAxis axis)
        {
            Hmd.GetRootPose(out Pose headPose);

            Vector3 result;
            switch (axis)
            {
                case HeadAxis.HeadForward:
                    result = headPose.forward;
                    break;
                case HeadAxis.HeadBackward:
                    result = -headPose.forward;
                    break;
                case HeadAxis.HeadUp:
                    result = headPose.up;
                    break;
                case HeadAxis.HeadDown:
                    result = -headPose.up;
                    break;
                case HeadAxis.HeadRight:
                    result = headPose.right;
                    break;
                case HeadAxis.HeadLeft:
                    result = -headPose.right;
                    break;
                default:
                    result = Vector3.zero;
                    break;
            }
            return result;
        }

        #region Inject

        public void InjectAllJointVelocityActiveState(JointVelocityFeatureConfigList featureConfigs,
                                                      IHand hand)
        {
            InjectFeatureConfigList(featureConfigs);
            InjectHand(hand);
        }

        public void InjectFeatureConfigList(JointVelocityFeatureConfigList featureConfigs)
        {
            _featureConfigs = featureConfigs;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }


        public void InjectOptionalHmd(IHmd hmd)
        {
            _hmd = hmd as MonoBehaviour;
            Hmd = hmd;
        }

        #endregion

    }
}
