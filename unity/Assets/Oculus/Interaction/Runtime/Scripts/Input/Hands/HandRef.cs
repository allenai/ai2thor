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
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// HandRef is a utility component that delegates all of its IHand implementation
    /// to the provided Hand object.
    /// </summary>
    public class HandRef : MonoBehaviour, IHand, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;

        public IHand Hand { get; private set; }

        public Handedness Handedness => Hand.Handedness;

        public bool IsConnected => Hand.IsConnected;

        public bool IsHighConfidence => Hand.IsHighConfidence;

        public bool IsDominantHand => Hand.IsDominantHand;

        public float Scale => Hand.Scale;

        public bool IsPointerPoseValid => Hand.IsPointerPoseValid;

        public bool IsTrackedDataValid => Hand.IsTrackedDataValid;

        public bool IsCenterEyePoseValid => Hand.IsCenterEyePoseValid;

        public Transform TrackingToWorldSpace => Hand.TrackingToWorldSpace;
        public int CurrentDataVersion => Hand.CurrentDataVersion;

        public event Action WhenHandUpdated
        {
            add => Hand.WhenHandUpdated += value;
            remove => Hand.WhenHandUpdated -= value;
        }

        public bool Active => IsConnected;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Hand);
        }

        public bool GetFingerIsPinching(HandFinger finger)
        {
            return Hand.GetFingerIsPinching(finger);
        }

        public bool GetIndexFingerIsPinching()
        {
            return Hand.GetIndexFingerIsPinching();
        }

        public bool GetPointerPose(out Pose pose)
        {
            return Hand.GetPointerPose(out pose);
        }

        public bool GetJointPose(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPose(handJointId, out pose);
        }

        public bool GetJointPoseLocal(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPoseLocal(handJointId, out pose);
        }

        public bool GetJointPosesLocal(out ReadOnlyHandJointPoses jointPosesLocal)
        {
            return Hand.GetJointPosesLocal(out jointPosesLocal);
        }

        public bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPoseFromWrist(handJointId, out pose);
        }

        public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
        {
            return Hand.GetJointPosesFromWrist(out jointPosesFromWrist);
        }

        public bool GetPalmPoseLocal(out Pose pose)
        {
            return Hand.GetPalmPoseLocal(out pose);
        }

        public bool GetFingerIsHighConfidence(HandFinger finger)
        {
            return Hand.GetFingerIsHighConfidence(finger);
        }

        public float GetFingerPinchStrength(HandFinger finger)
        {
            return Hand.GetFingerPinchStrength(finger);
        }

        public bool GetRootPose(out Pose pose)
        {
            return Hand.GetRootPose(out pose);
        }

        public bool GetCenterEyePose(out Pose pose)
        {
            return Hand.GetCenterEyePose(out pose);
        }

        public bool GetHandAspect<TComponent>(out TComponent foundComponent) where TComponent : class
        {
            return Hand.GetHandAspect(out foundComponent);
        }

        #region Inject
        public void InjectAllHandRef(IHand hand)
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
