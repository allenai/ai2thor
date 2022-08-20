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
    [Serializable]
    public class TransformFeatureConfigList
    {
        [SerializeField]
        private List<TransformFeatureConfig> _values;

        public List<TransformFeatureConfig> Values => _values;
    }

    [Serializable]
    public class TransformFeatureConfig : FeatureConfigBase<TransformFeature>
    {
    }

    public class TransformRecognizerActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private TransformFeatureConfigList _transformFeatureConfigs;

        [SerializeField]
        [Tooltip("State provider uses this to determine the state of features during real time, so" +
            " edit at runtime at your own risk.")]
        private TransformConfig _transformConfig;

        public IReadOnlyList<TransformFeatureConfig> FeatureConfigs => _transformFeatureConfigs.Values;

        public TransformConfig TransformConfig => _transformConfig;

        private TransformFeatureStateProvider FeatureStateProvider { get; set; }

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);

            bool foundAspect = Hand.GetHandAspect(out TransformFeatureStateProvider aspect);
            Assert.IsTrue(foundAspect);
            FeatureStateProvider = aspect;

            Assert.IsNotNull(_transformFeatureConfigs);
            Assert.IsNotNull(_transformConfig);

            _transformConfig.InstanceId = GetInstanceID();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                FeatureStateProvider.RegisterNewConfig(_transformConfig);

                // Warm up the proactive evaluation
                InitStateProvider();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                FeatureStateProvider.UnRegisterConfig(_transformConfig);
            }
        }

        private void InitStateProvider()
        {
            foreach(var featureConfig in FeatureConfigs)
            {
                FeatureStateProvider.GetCurrentState(_transformConfig, featureConfig.Feature, out _);
            }
        }

        public void GetFeatureVectorAndWristPos(TransformFeature feature, bool isHandVector,
            ref Vector3? featureVec, ref Vector3? wristPos)
        {
            FeatureStateProvider.GetFeatureVectorAndWristPos(
                TransformConfig, feature, isHandVector, ref featureVec, ref wristPos);
        }

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }
                foreach(var featureConfig in FeatureConfigs)
                {
                    if (!FeatureStateProvider.IsStateActive(
                        _transformConfig,
                        featureConfig.Feature,
                        featureConfig.Mode,
                        featureConfig.State))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #region Inject

        public void InjectAllTransformRecognizerActiveState(IHand hand, TransformFeatureConfigList transformFeatureList,
            TransformConfig transformConfig)
        {
            InjectHand(hand);
            InjectTransformFeatureList(transformFeatureList);
            InjectTransformConfig(transformConfig);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectTransformFeatureList(TransformFeatureConfigList transformFeatureList)
        {
            _transformFeatureConfigs = transformFeatureList;
        }

        public void InjectTransformConfig(TransformConfig transformConfig)
        {
            _transformConfig = transformConfig;
        }
        #endregion
    }
}
