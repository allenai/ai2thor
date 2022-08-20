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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection
{
    public enum FeatureStateActiveMode {
        Is,
        IsNot,
    }

    [Serializable]
    public abstract class FeatureConfigBase<TFeature>
    {
        [SerializeField]
        private FeatureStateActiveMode _mode;

        [SerializeField]
        private TFeature _feature;

        [SerializeField]
        private string _state;

        public FeatureStateActiveMode Mode
        {
            get => _mode;
            set { _mode = value; }
        }

        public TFeature Feature
        {
            get => _feature;
            set { _feature = value; }
        }

        public string State
        {
            get => _state;
            set { _state = value; }
        }
    }

    /// <summary>
    /// A helper class that keeps track of the current state of features, quantized into
    /// corresponding FeatureStates.
    /// </summary>
    /// <typeparam name="TFeature">
    /// An enum containing all features that can be tracked.
    /// </typeparam>
    /// <typeparam name="TFeatureState">
    /// An enum of all the possible states of each member of the <see cref="TFeature"/> param.
    /// The name of each member of this enum must be prefixed with one of the values of TFeature.
    /// </typeparam>
    public class FeatureStateProvider<TFeature, TFeatureState>
        where TFeature : unmanaged, Enum
        where TFeatureState : IEquatable<TFeatureState>
    {
        /// <summary>
        /// This should be updated with current value of the input data frameId. It is used to
        /// determine if values need to be recalculated.
        /// </summary>
        public int LastUpdatedFrameId { get; set; }

        private struct FeatureStateSnapshot
        {
            public bool HasCurrentState;
            public TFeatureState State;
            public TFeatureState DesiredState;
            public int LastUpdatedFrameId;
            public double DesiredStateEntryTime;
        }

        // A map of Map of (int)Feature => current state
        private FeatureStateSnapshot[] _featureToCurrentState;

        // A map of Map of (int)Feature => threshold configuration
        private IFeatureStateThresholds<TFeature, TFeatureState>[] _featureToThresholds;

        private readonly Func<TFeature, float?> _valueReader;
        private readonly Func<TFeature, int> _featureToInt;
        private readonly Func<float> _timeProvider;

        #region Lookup Helpers
        private int EnumToInt(TFeature value) => _featureToInt(value);

        private static readonly TFeature[] FeatureEnumValues = (TFeature[])Enum.GetValues(typeof(TFeature));

        private IFeatureThresholds<TFeature,TFeatureState> _featureThresholds;

        #endregion

        public FeatureStateProvider(Func<TFeature, float?> valueReader,
            Func<TFeature, int> featureToInt,
            Func<float> timeProvider)
        {
            _valueReader = valueReader;
            _featureToInt = featureToInt;
            _timeProvider = timeProvider;
        }

        public void InitializeThresholds(IFeatureThresholds<TFeature, TFeatureState> featureThresholds)
        {
            _featureThresholds = featureThresholds;
            _featureToThresholds = ValidateFeatureThresholds(featureThresholds.FeatureStateThresholds);

            InitializeStates();
        }

        public IFeatureStateThresholds<TFeature, TFeatureState>[] ValidateFeatureThresholds(
            IReadOnlyList<IFeatureStateThresholds<TFeature, TFeatureState>> featureStateThresholdsList)
        {
            var featureToFeatureStateThresholds =
                new IFeatureStateThresholds<TFeature, TFeatureState>[Enum.GetNames(typeof(TFeature)).Length];
            foreach (var featureStateThresholds in featureStateThresholdsList)
            {
                var featureIdx = EnumToInt(featureStateThresholds.Feature);
                featureToFeatureStateThresholds[featureIdx] = featureStateThresholds;

                // Just check that the thresholds are set correctly.
                for (var index = 0; index < featureStateThresholds.Thresholds.Count; index++)
                {
                    var featureStateThreshold = featureStateThresholds.Thresholds[index];
                    if (featureStateThreshold.ToFirstWhenBelow >
                        featureStateThreshold.ToSecondWhenAbove)
                    {
                        Assert.IsTrue(false,
                            $"Feature {featureStateThresholds.Feature} threshold at index {index}: ToFirstWhenBelow should be less than ToSecondWhenAbove.");
                    }
                }
            }

            for (int i = 0; i < featureToFeatureStateThresholds.Length; i++)
            {
                if (featureToFeatureStateThresholds[i] == null)
                {
                    Assert.IsNotNull(featureToFeatureStateThresholds[i],
                        $"StateThresholds does not contain an entry for feature with value {i}");
                }
            }

            return featureToFeatureStateThresholds;
        }

        private void InitializeStates()
        {
            // Set up current state
            _featureToCurrentState = new FeatureStateSnapshot[FeatureEnumValues.Length];
            foreach (TFeature feature in FeatureEnumValues)
            {
                int featureIdx = EnumToInt(feature);

                // Set default state.
                ref var currentState = ref _featureToCurrentState[featureIdx];
                currentState.State = default;
                currentState.DesiredState = default;
                currentState.DesiredStateEntryTime = 0;
            }
        }

        private ref IFeatureStateThresholds<TFeature, TFeatureState> GetFeatureThresholds(TFeature feature)
        {
            Assert.IsNotNull(_featureToThresholds, "Must call InitializeThresholds() before querying state");
            return ref _featureToThresholds[EnumToInt(feature)];
        }

        public TFeatureState GetCurrentFeatureState(TFeature feature)
        {
            Assert.IsNotNull(_featureToThresholds, "Must call InitializeThresholds() before querying state");

            ref var currentState = ref _featureToCurrentState[EnumToInt(feature)];
            if (currentState.LastUpdatedFrameId == LastUpdatedFrameId)
            {
                return currentState.State;
            }

            // Reads the raw value
            float? value = _valueReader(feature);
            if (!value.HasValue)
            {
                return currentState.State;
            }

            // Hand data changed since this was last queried.
            currentState.LastUpdatedFrameId = LastUpdatedFrameId;

            // Determine which state we should transition to based on the thresholds, and previous state.
            var featureStateThresholds = GetFeatureThresholds(feature).Thresholds;

            TFeatureState desiredState;
            if (!currentState.HasCurrentState)
            {
                desiredState = ReadDesiredState(value.Value, featureStateThresholds);
            }
            else
            {
                desiredState = ReadDesiredState(value.Value, featureStateThresholds,
                    currentState.State);
            }

            // If this is the same as the current state, do nothing.
            if (desiredState.Equals(currentState.State))
            {
                return currentState.State;
            }

            // If the desired state is different from the previous frame, reset the timer
            var currentTime = _timeProvider();
            if (!desiredState.Equals(currentState.DesiredState))
            {
                currentState.DesiredStateEntryTime = currentTime;
                currentState.DesiredState = desiredState;
            }

            // If the time in the desired state has exceeded the threshold, update the actual
            // state.
            if (currentState.DesiredStateEntryTime + _featureThresholds.MinTimeInState <= currentTime)
            {
                currentState.HasCurrentState = true;
                currentState.State = desiredState;
            }
            return currentState.State;
        }

        private TFeatureState ReadDesiredState(float value,
            IReadOnlyList<IFeatureStateThreshold<TFeatureState>> featureStateThresholds,
            TFeatureState previousState)
        {
            // Run it through the threshold calculation.
            var currentFeatureState = previousState;
            for (int i = 0; i < featureStateThresholds.Count; ++i)
            {
                var featureStateThreshold = featureStateThresholds[i];
                if (currentFeatureState.Equals(featureStateThreshold.FirstState) &&
                    value > featureStateThreshold.ToSecondWhenAbove)
                {
                    // In the first state and exceeded the threshold to enter the second state.
                    return featureStateThreshold.SecondState;
                }
                if (currentFeatureState.Equals(featureStateThreshold.SecondState) &&
                    value < featureStateThreshold.ToFirstWhenBelow)
                {
                    // In the second state and exceeded the threshold to enter the first state.
                    return featureStateThreshold.FirstState;
                }
            }

            return previousState;
        }

        private TFeatureState ReadDesiredState(float value,
            IReadOnlyList<IFeatureStateThreshold<TFeatureState>> featureStateThresholds)
        {
            // Run it through the threshold calculation.
            TFeatureState currentFeatureState = default;
            for (int i = 0; i < featureStateThresholds.Count; ++i)
            {
                var featureStateThreshold = featureStateThresholds[i];
                if (value <= featureStateThreshold.ToSecondWhenAbove)
                {
                    currentFeatureState = featureStateThreshold.FirstState;
                    break;
                }

                currentFeatureState = featureStateThreshold.SecondState;
            }

            return currentFeatureState;
        }

        public void ReadTouchedFeatureStates()
        {
            Assert.IsNotNull(_featureToThresholds, "Must call InitializeThresholds() before querying state");

            for (var featureIdx = 0;
                featureIdx < _featureToCurrentState.Length;
                featureIdx++)
            {
                ref FeatureStateSnapshot stateSnapshot =
                    ref _featureToCurrentState[featureIdx];
                if (stateSnapshot.LastUpdatedFrameId == 0)
                {
                    // This state has never been queried via IsStateActive, so don't
                    // bother updating it.
                    continue;
                }

                // Force evaluation with this new frame Id.
                GetCurrentFeatureState(FeatureEnumValues[featureIdx]);
            }
        }
    }
}
