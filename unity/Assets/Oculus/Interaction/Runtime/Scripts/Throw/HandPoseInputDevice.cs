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

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Provides pose information for a hand.
    /// </summary>
    public class HandPoseInputDevice : MonoBehaviour, IPoseInputDevice
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private float _bufferLengthSeconds = 0.1f;
        [SerializeField]
        private float _sampleFrequency = 90.0f;

        public float BufferLengthSeconds
        {
            get
            {
                return _bufferLengthSeconds;
            }
            set
            {
                _bufferLengthSeconds = value;
            }
        }

        public float SampleFrequency
        {
            get
            {
                return _sampleFrequency;
            }
            set
            {
                _sampleFrequency = value;
            }
        }

        public bool IsInputValid => Hand.IsTrackedDataValid;

        public bool IsHighConfidence => Hand.IsHighConfidence;

        private int _bufferSize = -1;

        private class HandJointPoseMetaData
        {
            public HandJointPoseMetaData(HandFinger finger,
                HandJointId joint, int bufferLength)
            {
                Finger = finger;
                JointId = joint;
                Velocities = new List<Vector3>();
                _previousPosition = null;
                _lastWritePos = -1;
                _bufferLength = bufferLength;
            }

            public void BufferNewValue(Pose newPose, float delta)
            {
                Vector3 newPosition = newPose.position;
                Vector3 newVelocity = Vector3.zero;
                if (delta > Mathf.Epsilon && _previousPosition.HasValue)
                {
                    newVelocity = (newPosition - _previousPosition.Value)
                        / delta;
                }
                int nextWritePos = (_lastWritePos < 0) ? 0 :
                    (_lastWritePos + 1) % _bufferLength;
                if (Velocities.Count <= nextWritePos)
                {
                    Velocities.Add(newVelocity);
                }
                else
                {
                    Velocities[nextWritePos] = newVelocity;
                }

                _previousPosition = newPosition;
                _lastWritePos = nextWritePos;
            }

            public Vector3 GetAverageVelocityVector()
            {
                int numVelocities = Velocities.Count;
                if (numVelocities == 0)
                {
                    return Vector3.zero;
                }
                Vector3 average = Vector3.zero;
                foreach (var speed in Velocities)
                {
                    average += speed;
                }
                average /= numVelocities;
                return average;
            }

            public void ResetSpeedsBuffer()
            {
                Velocities.Clear();
                _lastWritePos = -1;
                _previousPosition = null;
            }

            public readonly HandFinger Finger;
            public readonly HandJointId JointId;
            public readonly List<Vector3> Velocities;

            private Vector3? _previousPosition;
            private int _lastWritePos;
            private int _bufferLength;
        }

        private HandJointPoseMetaData[] _jointPoseInfoArray = null;

        public bool GetRootPose(out Pose pose)
        {
            pose = Pose.identity;
            if (!IsInputValid)
            {
                return false;
            }

            if (!Hand.GetJointPose(HandJointId.HandWristRoot,
                out pose))
            {
                return false;
            }

            Pose palmOffset = Pose.identity;
            if (!Hand.GetPalmPoseLocal(out palmOffset))
            {
                return false;
            }
            palmOffset.Postmultiply(pose);
            pose = palmOffset;

            return true;
        }

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_hand);
            _bufferSize = Mathf.CeilToInt(_bufferLengthSeconds
                * _sampleFrequency);
        }

        protected virtual void LateUpdate()
        {
            BufferFingerVelocities();
        }

        private void BufferFingerVelocities()
        {
            if (!IsInputValid)
            {
                return;
            }
            AllocateFingerBonesArrayIfNecessary();
            BufferFingerBoneVelocities();
        }

        private void AllocateFingerBonesArrayIfNecessary()
        {
            if (_jointPoseInfoArray != null)
            {
                return;
            }

            _jointPoseInfoArray = new[]
            {
                new HandJointPoseMetaData(HandFinger.Thumb,
                    HandJointId.HandThumb3,
                    _bufferSize),
                new HandJointPoseMetaData(HandFinger.Index,
                    HandJointId.HandIndex3,
                    _bufferSize),
                new HandJointPoseMetaData(HandFinger.Middle,
                    HandJointId.HandMiddle3,
                    _bufferSize),
                new HandJointPoseMetaData(HandFinger.Ring,
                    HandJointId.HandRing3,
                    _bufferSize),
                new HandJointPoseMetaData(HandFinger.Pinky,
                    HandJointId.HandPinky3,
                    _bufferSize)
            };
        }

        private bool GetFingerIsHighConfidence(HandFinger handFinger)
        {
            return Hand.IsTrackedDataValid &&
                Hand.GetFingerIsHighConfidence(handFinger);
        }

        private bool GetJointPose(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!Hand.IsTrackedDataValid)
            {
                return false;
            }

            if (!Hand.GetJointPose(handJointId, out pose))
            {
                return false;
            }

            return true;
        }

        private void BufferFingerBoneVelocities()
        {
            float deltaValue = Time.deltaTime;
            foreach (var jointPoseInfo in _jointPoseInfoArray)
            {
                if (!GetFingerIsHighConfidence(jointPoseInfo.Finger))
                {
                    continue;
                }

                Pose jointPose;
                if (!GetJointPose(jointPoseInfo.JointId, out jointPose))
                {
                    continue;
                }

                jointPoseInfo.BufferNewValue(jointPose, deltaValue);
            }
        }

        public (Vector3, Vector3) GetExternalVelocities()
        {
            if (_jointPoseInfoArray == null ||
                _jointPoseInfoArray.Length == 0)
            {
                return (Vector3.zero, Vector3.zero);
            }

            Vector3 averageVelocityAllFingers = Vector3.zero;
            foreach (var fingerMetaInfo in _jointPoseInfoArray)
            {
                averageVelocityAllFingers += fingerMetaInfo.GetAverageVelocityVector();
            }
            averageVelocityAllFingers /= _jointPoseInfoArray.Length;
            foreach (var item in _jointPoseInfoArray)
            {
                item.ResetSpeedsBuffer();
            }

            return (averageVelocityAllFingers, Vector3.zero);
        }

        #region Inject

        public void InjectAllHandPoseInputDevice(
            IHand hand)
        {
            InjectHand(hand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        #endregion
    }
}
