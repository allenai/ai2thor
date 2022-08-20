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
    internal class FingerFeatureStateDictionary
    {
        struct HandFingerState
        {
            public FeatureStateProvider<FingerFeature, string> StateProvider;
        }

        private readonly HandFingerState[] _fingerState = new HandFingerState[Constants.NUM_FINGERS];

        public void InitializeFinger(HandFinger finger,
            FeatureStateProvider<FingerFeature, string> stateProvider)
        {
            _fingerState[(int)finger] = new HandFingerState
            {
                StateProvider = stateProvider
            };
        }

        public FeatureStateProvider<FingerFeature, string> GetStateProvider(HandFinger finger)
        {
            return _fingerState[(int)finger].StateProvider;
        }
    }

    public interface IFingerFeatureStateProvider
    {
        bool GetCurrentState(HandFinger finger, FingerFeature fingerFeature, out string currentState);
        bool IsStateActive(HandFinger finger, FingerFeature feature, FeatureStateActiveMode mode, string stateId);
    }

    /// <summary>
    /// Interprets finger feature values using <see cref="FingerShapes"/> and uses
    /// the given <see cref="FingerFeatureStateThresholds"/> to quantize these values into states.
    /// To avoid rapid fluctuations at the edges of two states, this class uses the calculated
    /// feature state from the previous frame and the given state thresholds to apply a buffer
    /// between state transition edges.
    /// </summary>
    public class FingerFeatureStateProvider : MonoBehaviour, IFingerFeatureStateProvider
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [Serializable]
        public struct FingerStateThresholds
        {
            public HandFinger Finger;
            public FingerFeatureStateThresholds StateThresholds;
        }

        [SerializeField]
        private List<FingerStateThresholds> _fingerStateThresholds;

        [Header("Advanced Settings")]
        [SerializeField]
        [Tooltip("If true, disables proactive evaluation of any FingerFeature that has been " +
                 "queried at least once. This will force lazy-evaluation of state within calls " +
                 "to IsStateActive, which means you must do so each frame to avoid missing " +
                 "transitions between states.")]
        private bool _disableProactiveEvaluation;

        protected bool _started = false;

        private FingerFeatureStateDictionary _state;
        Func<float> _timeProvider;

        public static FingerShapes DefaultFingerShapes { get; } = new FingerShapes();
        private FingerShapes _fingerShapes = DefaultFingerShapes;
        private ReadOnlyHandJointPoses _handJointPoses;

        #region Unity Lifecycle Methods

        protected virtual void Awake()
        {
            Hand = _hand as IHand;

            _state = new FingerFeatureStateDictionary();
            _handJointPoses = ReadOnlyHandJointPoses.Empty;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            if (_timeProvider == null)
            {
                _timeProvider = () => Time.time;
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandDataAvailable;
                ReadStateThresholds();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandDataAvailable;
                _handJointPoses = ReadOnlyHandJointPoses.Empty;
            }
        }

        #endregion

        private void ReadStateThresholds()
        {
            Assert.IsNotNull(_fingerStateThresholds);
            Assert.IsNotNull(_timeProvider);
            Assert.AreEqual(Constants.NUM_FINGERS, _fingerStateThresholds.Count);

            HandFingerFlags seenFingers = HandFingerFlags.None;
            foreach (FingerStateThresholds fingerStateThresholds in _fingerStateThresholds)
            {
                seenFingers |= HandFingerUtils.ToFlags(fingerStateThresholds.Finger);
                HandFinger finger = fingerStateThresholds.Finger;

                var featureStateProvider = _state.GetStateProvider(finger);
                if (featureStateProvider == null)
                {
                    featureStateProvider =
                        new FeatureStateProvider<FingerFeature, string>(
                            feature => GetFeatureValue(finger, feature),
                            feature => (int)feature,
                            _timeProvider);

                    _state.InitializeFinger(fingerStateThresholds.Finger,
                        featureStateProvider);
                }

                featureStateProvider.InitializeThresholds(fingerStateThresholds.StateThresholds);
            }
            Assert.AreEqual(seenFingers, HandFingerFlags.All);
        }

        private void HandDataAvailable()
        {
            int frameId = Hand.CurrentDataVersion;

            if (!Hand.GetJointPosesFromWrist(out _handJointPoses))
            {
                return;
            }

            // Update the frameId of all state providers to mark data as dirty. If
            // proactiveEvaluation is enabled, also read the state of any feature that has been
            // touched, which will force it to evaluate.
            if (!_disableProactiveEvaluation)
            {
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
                {
                    var featureStateProvider = _state.GetStateProvider((HandFinger)fingerIdx);
                    featureStateProvider.LastUpdatedFrameId = frameId;
                    featureStateProvider.ReadTouchedFeatureStates();
                }
            }
            else
            {
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
                {
                    _state.GetStateProvider((HandFinger)fingerIdx).LastUpdatedFrameId =
                        frameId;
                }
            }
        }

        public bool GetCurrentState(HandFinger finger, FingerFeature fingerFeature, out string currentState)
        {
            if (!IsDataValid())
            {
                currentState = default;
                return false;
            }
            else
            {
                currentState = GetCurrentFingerFeatureState(finger, fingerFeature);
                return currentState != default;
            }
        }

        private string GetCurrentFingerFeatureState(HandFinger finger, FingerFeature fingerFeature)
        {
            return _state.GetStateProvider(finger).GetCurrentFeatureState(fingerFeature);
        }

        /// <summary>
        /// Returns the current value of the feature. If the finger joints are not populated with
        /// valid data (for instance, due to a disconnected hand), the method will return NaN.
        /// </summary>
        public float? GetFeatureValue(HandFinger finger, FingerFeature fingerFeature)
        {
            if (!IsDataValid())
            {
                return null;
            }

            return _fingerShapes.GetValue(finger, fingerFeature, Hand);
        }

        private bool IsDataValid()
        {
            return _handJointPoses.Count > 0;
        }

        public FingerShapes GetValueProvider(HandFinger finger)
        {
            return _fingerShapes;
        }

        public bool IsStateActive(HandFinger finger, FingerFeature feature, FeatureStateActiveMode mode, string stateId)
        {
            var currentState = GetCurrentFingerFeatureState(finger, feature);
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

        #region Inject
        public void InjectAllFingerFeatureStateProvider(IHand hand, List<FingerStateThresholds> fingerStateThresholds, FingerShapes fingerShapes,
            bool disableProactiveEvaluation)
        {
            InjectHand(hand);
            InjectFingerStateThresholds(fingerStateThresholds);
            InjectFingerShapes(fingerShapes);
            InjectDisableProactiveEvaluation(disableProactiveEvaluation);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectFingerStateThresholds(List<FingerStateThresholds> fingerStateThresholds)
        {
            _fingerStateThresholds = fingerStateThresholds;
        }

        public void InjectFingerShapes(FingerShapes fingerShapes)
        {
            _fingerShapes = fingerShapes;
        }

        public void InjectDisableProactiveEvaluation(bool disableProactiveEvaluation)
        {
            _disableProactiveEvaluation = disableProactiveEvaluation;
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }
        #endregion
    }
}
