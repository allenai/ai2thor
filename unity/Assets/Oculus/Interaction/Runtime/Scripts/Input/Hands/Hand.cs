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

namespace Oculus.Interaction.Input
{
    // A top level component that provides hand pose data, pinch states, and more.
    // Rather than sourcing data directly from the runtime layer, provides one
    // level of abstraction so that the aforementioned data can be injected
    // from other sources.
    public class Hand : DataModifier<HandDataAsset>, IHand
    {
        [SerializeField]
        [Tooltip(
            "Provides access to additional functionality on top of what the IHand interface provides." +
            "For example, this list can be used to provide access to the SkinnedMeshRenderer through " +
            "the IHand.GetHandAspect method.")]
        private Component[] _aspects;

        public IReadOnlyList<Component> Aspects => _aspects;

        public Handedness Handedness => GetData().Config.Handedness;

        public ITrackingToWorldTransformer TrackingToWorldTransformer =>
            GetData().Config.TrackingToWorldTransformer;

        public HandSkeleton HandSkeleton => GetData().Config.HandSkeleton;
        public IDataSource<HmdDataAsset> HmdData => GetData().Config.HmdData;

        private HandJointCache _jointPosesCache;

        public event Action WhenHandUpdated = delegate { };

        public bool IsConnected => GetData().IsDataValidAndConnected;
        public bool IsHighConfidence => GetData().IsHighConfidence;
        public bool IsDominantHand => GetData().IsDominantHand;

        public float Scale => GetData().HandScale * (TrackingToWorldTransformer != null
            ? TrackingToWorldTransformer.Transform.localScale.x
            : 1);

        private static readonly Vector3 PALM_LOCAL_OFFSET = new Vector3(0.08f, -0.01f, 0.0f);

        protected override void Apply(HandDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                InitializeJointPosesCache();
                WhenHandUpdated.Invoke();
            }
        }

        private void InitializeJointPosesCache()
        {
            if (_jointPosesCache == null && GetData().IsDataValidAndConnected)
            {
                _jointPosesCache = new HandJointCache(HandSkeleton);
            }
        }

        private void CheckJointPosesCacheUpdate()
        {
            if (_jointPosesCache != null
                && CurrentDataVersion != _jointPosesCache.LocalDataVersion)
            {
                _jointPosesCache.Update(GetData(), CurrentDataVersion);
            }
        }

        #region IHandState implementation

        public bool GetFingerIsPinching(HandFinger finger)
        {
            HandDataAsset currentData = GetData();
            return currentData.IsConnected && currentData.IsFingerPinching[(int)finger];
        }

        public bool GetIndexFingerIsPinching()
        {
            return GetFingerIsPinching(HandFinger.Index);
        }

        public bool IsPointerPoseValid => IsPoseOriginAllowed(GetData().PointerPoseOrigin);

        public bool GetPointerPose(out Pose pose)
        {
            HandDataAsset currentData = GetData();
            return ValidatePose(currentData.PointerPose, currentData.PointerPoseOrigin,
                out pose);
        }

        public bool GetJointPose(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;

            if (!IsTrackedDataValid
                || _jointPosesCache == null
                || !GetRootPose(out Pose rootPose))
            {
                return false;
            }
            CheckJointPosesCacheUpdate();
            pose = _jointPosesCache.WorldJointPose(handJointId, rootPose, Scale);
            return true;
        }

        public bool GetJointPoseLocal(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses))
            {
                return false;
            }

            pose = localJointPoses[(int)handJointId];
            return true;
        }

        public bool GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses)
        {
            if (!IsTrackedDataValid || _jointPosesCache == null)
            {
                localJointPoses = ReadOnlyHandJointPoses.Empty;
                return false;
            }
            CheckJointPosesCacheUpdate();
            return _jointPosesCache.GetAllLocalPoses(out localJointPoses);
        }

        public bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist))
            {
                return false;
            }

            pose = jointPosesFromWrist[(int)handJointId];
            return true;
        }

        public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
        {
            if (!IsTrackedDataValid || _jointPosesCache == null)
            {
                jointPosesFromWrist = ReadOnlyHandJointPoses.Empty;
                return false;
            }
            CheckJointPosesCacheUpdate();
            return _jointPosesCache.GetAllPosesFromWrist(out jointPosesFromWrist);
        }

        public bool GetPalmPoseLocal(out Pose pose)
        {
            Quaternion rotationQuat = Quaternion.identity;
            Vector3 offset = PALM_LOCAL_OFFSET;
            if (Handedness == Handedness.Left)
            {
                offset = -offset;
            }
            pose = new Pose(offset * Scale, rotationQuat);
            return true;
        }

        public bool GetFingerIsHighConfidence(HandFinger finger)
        {
            return GetData().IsFingerHighConfidence[(int)finger];
        }

        public float GetFingerPinchStrength(HandFinger finger)
        {
            return GetData().FingerPinchStrength[(int)finger];
        }

        public bool IsTrackedDataValid => IsPoseOriginAllowed(GetData().RootPoseOrigin);

        public bool GetRootPose(out Pose pose)
        {
            HandDataAsset currentData = GetData();
            return ValidatePose(currentData.Root, currentData.RootPoseOrigin, out pose);
        }

        public bool IsCenterEyePoseValid => HmdData.GetData().IsTracked;

        public bool GetCenterEyePose(out Pose pose)
        {
            HmdDataAsset hmd = HmdData.GetData();

            if (hmd == null || !hmd.IsTracked)
            {
                pose = Pose.identity;
                return false;
            }

            pose = TrackingToWorldTransformer.ToWorldPose(hmd.Root);
            return true;
        }

        #endregion


        public Transform TrackingToWorldSpace
        {
            get
            {
                if (TrackingToWorldSpace == null)
                {
                    return null;
                }
                return TrackingToWorldTransformer.Transform;
            }
        }

        private bool ValidatePose(in Pose sourcePose, PoseOrigin sourcePoseOrigin, out Pose pose)
        {
            if (IsPoseOriginDisallowed(sourcePoseOrigin))
            {
                pose = Pose.identity;
                return false;
            }

            pose = TrackingToWorldTransformer != null
                ? TrackingToWorldTransformer.ToWorldPose(sourcePose)
                : sourcePose;

            return true;
        }

        private bool IsPoseOriginAllowed(PoseOrigin poseOrigin)
        {
            return poseOrigin != PoseOrigin.None;
        }

        private bool IsPoseOriginDisallowed(PoseOrigin poseOrigin)
        {
            return poseOrigin == PoseOrigin.None;
        }

        public bool GetHandAspect<TComponent>(out TComponent foundComponent) where TComponent : class
        {
            foreach (Component aspect in _aspects)
            {
                foundComponent = aspect as TComponent;
                if (foundComponent != null)
                {
                    return true;
                }
            }

            foundComponent = null;
            return false;
        }

        #region Inject

        public void InjectAllHand(UpdateModeFlags updateMode, IDataSource updateAfter,
            DataModifier<HandDataAsset> modifyDataFromSource, bool applyModifier,
            Component[] aspects)
        {
            base.InjectAllDataModifier(updateMode, updateAfter, modifyDataFromSource, applyModifier);
            InjectAspects(aspects);
        }

        public void InjectAspects(Component[] aspects)
        {
            _aspects = aspects;
        }

        #endregion
    }
}
