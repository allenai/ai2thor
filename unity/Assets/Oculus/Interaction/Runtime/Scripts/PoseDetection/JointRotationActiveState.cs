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
    public class JointRotationActiveState : MonoBehaviour, IActiveState
    {
        public enum RelativeTo
        {
            Hand = 0,
            World = 1,
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

        public enum HandAxis
        {
            Pronation = 0,
            Supination = 1,
            RadialDeviation = 2,
            UlnarDeviation = 3,
            Extension = 4,
            Flexion = 5,
        }

        [Serializable]
        public struct JointRotationFeatureState
        {
            /// <summary>
            /// The world target euler angles for a
            /// <see cref="JointRotationFeatureConfig"/>
            /// </summary>
            public readonly Vector3 TargetAxis;

            /// <summary>
            /// The normalized joint rotation along the target
            /// axis relative to <see cref="_degreesPerSecond"/>
            /// </summary>
            public readonly float Amount;

            public JointRotationFeatureState(Vector3 targetAxis, float amount)
            {
                TargetAxis = targetAxis;
                Amount = amount;
            }
        }

        [Serializable]
        public class JointRotationFeatureConfigList
        {
            [SerializeField]
            private List<JointRotationFeatureConfig> _values;

            public List<JointRotationFeatureConfig> Values => _values;
        }

        [Serializable]
        public class JointRotationFeatureConfig : FeatureConfigBase<HandJointId>
        {
            [SerializeField]
            private RelativeTo _relativeTo = RelativeTo.Hand;

            [SerializeField]
            private WorldAxis _worldAxis = WorldAxis.PositiveZ;

            [SerializeField]
            private HandAxis _handAxis = HandAxis.RadialDeviation;

            public RelativeTo RelativeTo => _relativeTo;
            public WorldAxis WorldAxis => _worldAxis;
            public HandAxis HandAxis => _handAxis;
        }

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private JointRotationFeatureConfigList _featureConfigs;

        [SerializeField, Min(0)]
        private float _degreesPerSecond = 120f;

        [SerializeField, Min(0)]
        private float _thresholdWidth = 30f;

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

        public IReadOnlyList<JointRotationFeatureConfig> FeatureConfigs =>
            _featureConfigs.Values;

        public IReadOnlyDictionary<JointRotationFeatureConfig, JointRotationFeatureState> FeatureStates =>
             _featureStates;

        private Dictionary<JointRotationFeatureConfig, JointRotationFeatureState> _featureStates =
            new Dictionary<JointRotationFeatureConfig, JointRotationFeatureState>();


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
                _featureStates.Add(config, new JointRotationFeatureState());
            }
            _jointDeltaConfig = new JointDeltaConfig(GetInstanceID(), allTrackedJoints);

            bool foundAspect = Hand.GetHandAspect(out JointDeltaProvider aspect);
            Assert.IsTrue(foundAspect);
            JointDeltaProvider = aspect;

            _lastUpdateTime = _timeProvider();
            this.EndStart(ref _started);
        }

        private bool CheckAllJointRotations()
        {
            bool result = true;

            float deltaTime = _timeProvider() - _lastUpdateTime;
            float threshold = _internalState ?
                  _degreesPerSecond + _thresholdWidth * 0.5f :
                  _degreesPerSecond - _thresholdWidth * 0.5f;

            threshold *= deltaTime;

            foreach (var config in FeatureConfigs)
            {
                if (Hand.GetRootPose(out Pose rootPose) &&
                    Hand.GetJointPose(config.Feature, out Pose curPose) &&
                    JointDeltaProvider.GetRotationDelta(
                        config.Feature, out Quaternion worldDeltaRotation))
                {
                    Vector3 rotDeltaEuler = worldDeltaRotation.eulerAngles;

                    for (int i = 0; i < 3; ++i)
                    {
                        while (rotDeltaEuler[i] > 180)
                        {
                            rotDeltaEuler[i] -= 360;
                        }
                        while (rotDeltaEuler[i] < -180)
                        {
                            rotDeltaEuler[i] += 360;
                        }
                    }

                    Vector3 worldTargetRotation =
                        GetWorldTargetRotation(rootPose, config);
                    float rotationOnTargetAxis =
                        Vector3.Dot(rotDeltaEuler, worldTargetRotation);

                    _featureStates[config] = new JointRotationFeatureState(
                                             worldTargetRotation,
                                             threshold > 0 ?
                                             Mathf.Clamp01(rotationOnTargetAxis / threshold) :
                                             1);

                    bool rotationExceedsThreshold = rotationOnTargetAxis > threshold;
                    result &= rotationExceedsThreshold;
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

            bool newState = CheckAllJointRotations();

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

        private Vector3 GetWorldTargetRotation(Pose rootPose, JointRotationFeatureConfig config)
        {
            switch (config.RelativeTo)
            {
                default:
                case RelativeTo.Hand:
                    return GetHandAxisVector(config.HandAxis, rootPose);
                case RelativeTo.World:
                    return GetWorldAxisVector(config.WorldAxis);
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
            switch (axis)
            {
                case HandAxis.Pronation:
                    return rootPose.rotation * Vector3.left;
                case HandAxis.Supination:
                    return rootPose.rotation * Vector3.right;
                case HandAxis.RadialDeviation:
                    return rootPose.rotation * Vector3.down;
                case HandAxis.UlnarDeviation:
                    return rootPose.rotation * Vector3.up;
                case HandAxis.Extension:
                    return rootPose.rotation * Vector3.back;
                case HandAxis.Flexion:
                    return rootPose.rotation * Vector3.forward;
                default:
                    return Vector3.zero;
            }
        }

        #region Inject

        public void InjectAllJointRotationActiveState(JointRotationFeatureConfigList featureConfigs,
                                                      IHand hand)
        {
            InjectFeatureConfigList(featureConfigs);
            InjectHand(hand);
        }

        public void InjectFeatureConfigList(JointRotationFeatureConfigList featureConfigs)
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

        #endregion

    }
}
