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

namespace Oculus.Interaction.PoseDetection
{
    [CreateAssetMenu(menuName = "Oculus/Interaction/SDK/Pose Detection/Shape")]
    public class ShapeRecognizer : ScriptableObject
    {
        [Serializable]
        public class FingerFeatureConfigList
        {
            [SerializeField]
            private List<FingerFeatureConfig> _value;

            public IReadOnlyList<FingerFeatureConfig> Value => _value;

            public FingerFeatureConfigList() { }

            public FingerFeatureConfigList(List<FingerFeatureConfig> value)
            {
                _value = value;
            }
        }

        [Serializable]
        public class FingerFeatureConfig : FeatureConfigBase<FingerFeature>
        {
        }

        [SerializeField]
        private string _shapeName;

        [SerializeField]
        private FingerFeatureConfigList _thumbFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _indexFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _middleFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _ringFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _pinkyFeatureConfigs = new FingerFeatureConfigList();

        public IReadOnlyList<FingerFeatureConfig> ThumbFeatureConfigs => _thumbFeatureConfigs.Value;
        public IReadOnlyList<FingerFeatureConfig> IndexFeatureConfigs => _indexFeatureConfigs.Value;
        public IReadOnlyList<FingerFeatureConfig> MiddleFeatureConfigs => _middleFeatureConfigs.Value;
        public IReadOnlyList<FingerFeatureConfig> RingFeatureConfigs => _ringFeatureConfigs.Value;
        public IReadOnlyList<FingerFeatureConfig> PinkyFeatureConfigs => _pinkyFeatureConfigs.Value;

        public string ShapeName => _shapeName;

        public IReadOnlyList<FingerFeatureConfig> GetFingerFeatureConfigs(HandFinger finger)
        {
            switch (finger)
            {
                case HandFinger.Thumb:
                    return ThumbFeatureConfigs;
                case HandFinger.Index:
                    return IndexFeatureConfigs;
                case HandFinger.Middle:
                    return MiddleFeatureConfigs;
                case HandFinger.Ring:
                    return RingFeatureConfigs;
                case HandFinger.Pinky:
                    return PinkyFeatureConfigs;
                default:
                    throw new ArgumentException("must be a HandFinger enum value",
                        nameof(finger));
            }
        }

        public IEnumerable<ValueTuple<HandFinger, IReadOnlyList<FingerFeatureConfig>>>
            GetFingerFeatureConfigs()
        {
            for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
            {
                HandFinger finger = (HandFinger)fingerIdx;
                var configs = GetFingerFeatureConfigs(finger);
                if (configs.Count == 0)
                {
                    continue;
                }

                yield return new ValueTuple<HandFinger, IReadOnlyList<FingerFeatureConfig>>(finger,
                    configs);
            }
        }

#region Inject
        public void InjectAllShapeRecognizer(IDictionary<HandFinger, FingerFeatureConfig[]> fingerFeatureConfigs)
        {
            FingerFeatureConfigList ReadFeatureConfigs(HandFinger finger)
            {
                if (!fingerFeatureConfigs.TryGetValue(finger, out FingerFeatureConfig[] configs))
                {
                    configs = Array.Empty<FingerFeatureConfig>();
                }

                return new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
            }

            _thumbFeatureConfigs = ReadFeatureConfigs(HandFinger.Thumb);
            _indexFeatureConfigs = ReadFeatureConfigs(HandFinger.Index);
            _middleFeatureConfigs = ReadFeatureConfigs(HandFinger.Middle);
            _ringFeatureConfigs = ReadFeatureConfigs(HandFinger.Ring);
            _pinkyFeatureConfigs = ReadFeatureConfigs(HandFinger.Pinky);
        }

        public void InjectThumbFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _thumbFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        public void InjectIndexFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _indexFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        public void InjectMiddleFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _middleFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        public void InjectRingFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _ringFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        public void InjectPinkyFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _pinkyFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        public void InjectShapeName(string shapeName)
        {
            _shapeName = shapeName;
        }
#endregion
    }
}
