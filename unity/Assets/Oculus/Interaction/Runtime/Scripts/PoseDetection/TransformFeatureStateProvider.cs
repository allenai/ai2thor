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

using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.PoseDetection
{
    public enum UpVectorType
    {
        Head,
        Tracking,
        World
    }

    [Serializable]
    public class TransformConfig
    {
        public TransformConfig()
        {
            PositionOffset = Vector3.zero;
            RotationOffset = Vector3.zero;
            UpVectorType = UpVectorType.Head;
            FeatureThresholds = null;
            InstanceId = 0;
        }

        // Position offset relative to the reference transform.
        public Vector3 PositionOffset;

        // Rotational offset relative to the reference transform.
        public Vector3 RotationOffset;

        public UpVectorType UpVectorType;

        public TransformFeatureStateThresholds FeatureThresholds;

        // set via component that uses this class
        public int InstanceId { get; set; }
    }

    public class TransformJointData
    {
        public bool IsValid;
        public Handedness Handedness;
        public Pose CenterEyePose, WristPose;
        public Vector3 TrackingSystemUp;
        public Vector3 TrackingSystemForward;
    }

    internal class TransformFeatureStateCollection
    {
        public class TransformStateInfo
        {
            public TransformStateInfo(TransformConfig transformConfig,
                FeatureStateProvider<TransformFeature, string> stateProvider)
            {
                Config = transformConfig;
                StateProvider = stateProvider;
            }

            public TransformConfig Config;
            public FeatureStateProvider<TransformFeature, string> StateProvider;
        }

        private Dictionary<int, TransformStateInfo> _idToTransformStateInfo =
            new Dictionary<int, TransformStateInfo>();

        public void RegisterConfig(TransformConfig transformConfig, TransformJointData jointData,
            Func<float> timeProvider)
        {
            bool containsKeyAlready = _idToTransformStateInfo.ContainsKey(transformConfig.InstanceId);
            Assert.IsFalse(containsKeyAlready,
                "Trying to register multiple configs with the same id into " +
                "TransformFeatureStateCollection.");

            var featureStateProvider = new FeatureStateProvider<TransformFeature, string>
                // note that jointData and transformConfig are reference types (classes), because they can change
                // during run time
                ((feature) => TransformFeatureValueProvider.GetValue(feature, jointData, transformConfig),
                    feature => (int)feature,
                    timeProvider);
            TransformStateInfo newTransfState = new TransformStateInfo(transformConfig, featureStateProvider);
            featureStateProvider.InitializeThresholds(transformConfig.FeatureThresholds);
            _idToTransformStateInfo.Add(transformConfig.InstanceId, newTransfState);
        }

        public void UnRegisterConfig(TransformConfig transformConfig)
        {
            _idToTransformStateInfo.Remove(transformConfig.InstanceId);
        }

        public FeatureStateProvider<TransformFeature, string> GetStateProvider(
            TransformConfig transformConfig)
        {
            return _idToTransformStateInfo[transformConfig.InstanceId].StateProvider;
        }

        public void SetConfig(int configId, TransformConfig config)
        {
            _idToTransformStateInfo[configId].Config = config;
        }

        public TransformConfig GetConfig(int configId)
        {
            return _idToTransformStateInfo[configId].Config;
        }

        public void UpdateFeatureStates(int lastUpdatedFrameId,
            bool disableProactiveEvaluation)
        {
            foreach (var transformStateInfo in _idToTransformStateInfo.Values)
            {
                var featureStateProvider = transformStateInfo.StateProvider;
                if (!disableProactiveEvaluation)
                {
                    featureStateProvider.LastUpdatedFrameId = lastUpdatedFrameId;
                    featureStateProvider.ReadTouchedFeatureStates();
                }
                else
                {
                    featureStateProvider.LastUpdatedFrameId = lastUpdatedFrameId;
                }
            }
        }
    }

    /// <summary>
    /// Interprets transform feature values from a <see cref="TransformFeatureValueProvider"/>
    /// and uses the given <see cref="TransformFeatureStateThresholds"/> to quantize
    /// these values into states. To avoid rapid fluctuations at the edges
    /// of two states, this classes uses the calculated feature states from the previous
    /// frame and the given state thresholds to apply a buffer between
    /// state transition edges.
    /// </summary>
    public class TransformFeatureStateProvider : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private MonoBehaviour _trackingToWorldTransformer;

        public ITrackingToWorldTransformer TrackingToWorldTransformer { get; private set; }

        [Header("Advanced Settings")]
        [SerializeField]
        [Tooltip("If true, disables proactive evaluation of any TransformFeature that has been " +
                 "queried at least once. This will force lazy-evaluation of state within calls " +
                 "to IsStateActive, which means you must do so each frame to avoid missing " +
                 "transitions between states.")]
        private bool _disableProactiveEvaluation;

        private TransformJointData _jointData = new TransformJointData();
        private TransformFeatureStateCollection _transformFeatureStateCollection;
        private Func<float> _timeProvider;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            _transformFeatureStateCollection = new TransformFeatureStateCollection();
            _timeProvider = () => Time.time;
        }

        public void RegisterNewConfig(TransformConfig transformConfig)
        {
            //Register time provider indirectly in case reference changes
            Func<float> getTime = () => _timeProvider();
            _transformFeatureStateCollection.RegisterConfig(transformConfig, _jointData, getTime);
        }

        public void UnRegisterConfig(TransformConfig transformConfig)
        {
            _transformFeatureStateCollection.UnRegisterConfig(transformConfig);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_timeProvider);
            Assert.IsNotNull(TrackingToWorldTransformer);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandDataAvailable;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandDataAvailable;
            }
        }

        private void HandDataAvailable()
        {
            UpdateJointData();
            UpdateStateForHand();
        }

        private void UpdateJointData()
        {
            _jointData.IsValid = Hand.GetRootPose(out _jointData.WristPose) &&
                                 Hand.GetCenterEyePose(out _jointData.CenterEyePose);
            if (!_jointData.IsValid)
            {
                return;
            }

            _jointData.Handedness = Hand.Handedness;
            _jointData.TrackingSystemUp = TrackingToWorldTransformer.Transform.up;
            _jointData.TrackingSystemForward = TrackingToWorldTransformer.Transform.forward;
        }

        private void UpdateStateForHand()
        {
            // Update the frameId of all state providers to mark data as dirty. If
            // proactiveEvaluation is enabled, also read the state of any feature that has been
            // touched, which will force it to evaluate.
            _transformFeatureStateCollection.UpdateFeatureStates(
                Hand.CurrentDataVersion,
                _disableProactiveEvaluation);
        }

        public bool IsHandDataValid()
        {
            return _jointData.IsValid;
        }

        public bool IsStateActive(TransformConfig config, TransformFeature feature, FeatureStateActiveMode mode, string stateId)
        {
            var currentState = GetCurrentFeatureState(config, feature);
            switch (mode)
            {
                case FeatureStateActiveMode.Is:
                    return currentState == stateId;
                case FeatureStateActiveMode.IsNot:
                    return currentState != stateId;
                default:
                    return false;
            }
        }

        private string GetCurrentFeatureState(TransformConfig config,
            TransformFeature feature)
        {
            return _transformFeatureStateCollection.GetStateProvider(config).
                GetCurrentFeatureState(feature);
        }

        public bool GetCurrentState(TransformConfig config, TransformFeature transformFeature,
            out string currentState)
        {
            if (!IsHandDataValid())
            {
                currentState = default;
                return false;
            }

            currentState = GetCurrentFeatureState(config, transformFeature);
            return currentState != default;
        }

        /// <summary>
        /// Returns the current value of the feature. If the hand joints are not populated with
        /// valid data (for instance, due to a disconnected hand), the method will return null;
        /// </summary>
        public float? GetFeatureValue(TransformConfig config,
            TransformFeature transformFeature)
        {
            if (!IsHandDataValid())
            {
                return null;
            }

            return TransformFeatureValueProvider.GetValue(transformFeature,
                _jointData, config);
        }

        public void GetFeatureVectorAndWristPos(TransformConfig config,
            TransformFeature transformFeature, bool isHandVector, ref Vector3? featureVec,
            ref Vector3? wristPos)
        {
            featureVec = null;
            wristPos = null;
            if (!IsHandDataValid())
            {
                return;
            }

            featureVec = isHandVector ?
                         TransformFeatureValueProvider.GetHandVectorForFeature(transformFeature,
                            _jointData, in config) :
                         TransformFeatureValueProvider.GetTargetVectorForFeature(transformFeature,
                            _jointData, in config);
            wristPos = _jointData.WristPose.position;
        }

        #region Inject
        public void InjectAllTransformFeatureStateProvider(IHand hand, bool disableProactiveEvaluation)
        {
            Hand = hand;
            _disableProactiveEvaluation = disableProactiveEvaluation;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectDisableProactiveEvaluation(bool disabled)
        {
            _disableProactiveEvaluation = disabled;
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }
        #endregion
    }
}
