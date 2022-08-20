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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.PoseDetection
{
    public class ShapeRecognizerActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private ShapeRecognizer[] _shapes;
        public IReadOnlyList<ShapeRecognizer> Shapes => _shapes;
        private IFingerFeatureStateProvider FingerFeatureStateProvider { get; set; }
        public Handedness Handedness => Hand.Handedness;

        struct FingerFeatureStateUsage
        {
            public HandFinger handFinger;
            public ShapeRecognizer.FingerFeatureConfig config;
        }

        private List<FingerFeatureStateUsage> _allFingerStates = new List<FingerFeatureStateUsage>();

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_shapes);

            for (var index = 0; index < _shapes.Length; index++)
            {
                var sr = _shapes[index];
                if (sr == null)
                {
                    Assert.IsNotNull(sr, "_shapes[" + index + "] != null");
                }
            }

            bool foundAspect = Hand.GetHandAspect(out IFingerFeatureStateProvider state);
            Assert.IsTrue(foundAspect);
            FingerFeatureStateProvider = state;

            _allFingerStates = FlattenUsedFeatures();

            // Warm up the proactive evaluation
            InitStateProvider();
        }

        private void InitStateProvider()
        {
            foreach (FingerFeatureStateUsage state in _allFingerStates)
            {
                FingerFeatureStateProvider.GetCurrentState(state.handFinger, state.config.Feature, out _);
            }
        }

        private List<FingerFeatureStateUsage> FlattenUsedFeatures()
        {
            var fingerFeatureStateUsages = new List<FingerFeatureStateUsage>();
            foreach (var sr in _shapes)
            {
                int configCount = 0;
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
                {
                    var handFinger = (HandFinger)fingerIdx;
                    foreach (var config in sr.GetFingerFeatureConfigs(handFinger))
                    {
                        ++configCount;
                        fingerFeatureStateUsages.Add(new FingerFeatureStateUsage()
                        {
                            handFinger = handFinger, config = config
                        });
                    }
                }

                // If this assertion is hit, open the ScriptableObject in the Unity Inspector
                // and ensure that it has at least one valid condition.
                Assert.IsTrue(configCount > 0, $"Shape {sr.ShapeName} has no valid conditions.");
            }

            return fingerFeatureStateUsages;
        }

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled || _allFingerStates.Count == 0)
                {
                    return false;
                }

                foreach (FingerFeatureStateUsage stateUsage in _allFingerStates)
                {
                    if (!FingerFeatureStateProvider.IsStateActive(stateUsage.handFinger,
                        stateUsage.config.Feature, stateUsage.config.Mode, stateUsage.config.State))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #region Inject
        public void InjectAllShapeRecognizerActiveState(IHand hand, ShapeRecognizer[] shapes)
        {
            InjectHand(hand);
            InjectShapes(shapes);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectShapes(ShapeRecognizer[] shapes)
        {
            _shapes = shapes;
        }
        #endregion
    }
}
