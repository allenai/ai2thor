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

namespace Oculus.Interaction.PoseDetection
{
    [Serializable]
    public class TransformFeatureStateThreshold : IFeatureStateThreshold<string>
    {
        public TransformFeatureStateThreshold()
        {
        }

        public TransformFeatureStateThreshold(
            float thresholdMidpoint,
            float thresholdWidth,
            string firstState,
            string secondState)
        {
            _thresholdMidpoint = thresholdMidpoint;
            _thresholdWidth = thresholdWidth;
            _firstState = firstState;
            _secondState = secondState;
        }

        [SerializeField]
        private float _thresholdMidpoint;

        [SerializeField]
        private float _thresholdWidth;

        [SerializeField]
        private string _firstState;

        [SerializeField]
        private string _secondState;

        public float ToFirstWhenBelow => _thresholdMidpoint - _thresholdWidth * 0.5f;

        public float ToSecondWhenAbove => _thresholdMidpoint + _thresholdWidth * 0.5f;

        public string FirstState => _firstState;

        public string SecondState => _secondState;
    }

    [Serializable]
    public class TransformFeatureThresholds : IFeatureStateThresholds<TransformFeature,
        string>
    {
        public TransformFeatureThresholds() { }

        public TransformFeatureThresholds(TransformFeature featureTransform,
            IEnumerable<TransformFeatureStateThreshold> thresholds)
        {
            _feature = featureTransform;
            _thresholds = new List<TransformFeatureStateThreshold>(thresholds);
        }

        [SerializeField]
        private TransformFeature _feature;

        [SerializeField]
        private List<TransformFeatureStateThreshold> _thresholds;

        [SerializeField]
        [Tooltip("Length of time that the transform must be in the new state before the feature " +
                 "state provider will use the new value.")]
        private double _minTimeInState;

        public TransformFeature Feature => _feature;

        public IReadOnlyList<IFeatureStateThreshold<string>>
            Thresholds => _thresholds;

        public double MinTimeInState => _minTimeInState;
    }

    [CreateAssetMenu(menuName = "Oculus/Interaction/SDK/Pose Detection/Transform Thresholds")]
    public class TransformFeatureStateThresholds : ScriptableObject,
        IFeatureThresholds<TransformFeature, string>
    {
        [SerializeField]
        private List<TransformFeatureThresholds> _featureThresholds;

        [SerializeField]
        [Tooltip("Length of time that the transform must be in the new state before the feature " +
                 "state provider will use the new value.")]
        private double _minTimeInState;

        public void Construct(List<TransformFeatureThresholds> featureThresholds,
            double minTimeInState)
        {
            _featureThresholds = featureThresholds;
            _minTimeInState = minTimeInState;
        }

        public IReadOnlyList<IFeatureStateThresholds<TransformFeature, string>>
            FeatureStateThresholds => _featureThresholds;

        public double MinTimeInState => _minTimeInState;
    }
}
