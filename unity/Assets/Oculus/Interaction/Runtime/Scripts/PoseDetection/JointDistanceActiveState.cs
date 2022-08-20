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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// This component tracks the distance between two hand joints and reports
    /// <see cref="IActiveState.Active"/> when distance is under a provided threshold.
    /// </summary>
    public class JointDistanceActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _handA;
        private IHand HandA;

        [SerializeField]
        private HandJointId _jointIdA;

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _handB;
        private IHand HandB;

        [SerializeField]
        private HandJointId _jointIdB;

        [SerializeField]
        private float _distance = 0.05f;

        [SerializeField]
        private float _thresholdWidth = 0.02f;

        [SerializeField]
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

        private bool _activeState = false;
        private bool _internalState = false;
        private float _lastStateChangeTime = 0f;
        private int _lastStateUpdateFrame = 0;

        protected virtual void Awake()
        {
            HandA = _handA as IHand;
            HandB = _handB as IHand;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(HandA);
            Assert.IsNotNull(HandB);
        }

        protected virtual void Update()
        {
            UpdateActiveState();
        }

        private void UpdateActiveState()
        {
            if (Time.frameCount <= _lastStateUpdateFrame)
            {
                return;
            }
            _lastStateUpdateFrame = Time.frameCount;

            bool newState = JointDistanceWithinThreshold();
            if (newState != _internalState)
            {
                _internalState = newState;
                _lastStateChangeTime = Time.unscaledTime;
            }

            if (Time.unscaledTime - _lastStateChangeTime >= _minTimeInState)
            {
                _activeState = _internalState;
            }
        }

        private bool JointDistanceWithinThreshold()
        {
            if (HandA.GetJointPose(_jointIdA, out Pose poseA) &&
                HandB.GetJointPose(_jointIdB, out Pose poseB))
            {
                float threshold = _internalState ?
                                  _distance + _thresholdWidth * 0.5f :
                                  _distance - _thresholdWidth * 0.5f;

                return Vector3.Distance(poseA.position, poseB.position) <= threshold;
            }
            else
            {
                return false;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _distance = Mathf.Max(_distance, 0f);
            _minTimeInState = Mathf.Max(_minTimeInState, 0f);
            _thresholdWidth = Mathf.Max(_thresholdWidth, 0f);
        }
#endif

        #region Inject
        public void InjectAllJointDistanceActiveState(IHand handA, HandJointId jointIdA, IHand handB, HandJointId jointIdB)
        {
            InjectHandA(handA);
            InjectJointIdA(jointIdA);
            InjectHandB(handB);
            InjectJointIdB(jointIdB);
        }

        public void InjectHandA(IHand handA)
        {
            _handA = handA as MonoBehaviour;
            HandA = handA;
        }

        public void InjectJointIdA(HandJointId jointIdA)
        {
            _jointIdA = jointIdA;
        }

        public void InjectHandB(IHand handB)
        {
            _handB = handB as MonoBehaviour;
            HandB = handB;
        }

        public void InjectJointIdB(HandJointId jointIdB)
        {
            _jointIdB = jointIdB;
        }

        public void InjectOptionalDistance(float val)
        {
            _distance = val;
        }

        public void InjectOptionalThresholdWidth(float val)
        {
            _thresholdWidth = val;
        }

        public void InjectOptionalMinTimeInState(float val)
        {
            _minTimeInState = val;
        }
        #endregion
    }
}
