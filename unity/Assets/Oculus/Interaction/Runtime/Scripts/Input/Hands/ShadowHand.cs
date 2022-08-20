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

using UnityEngine;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// A thin HandJoint skeleton implementation that can be used for computing
    /// world joints from local joints data.
    /// </summary>
    public class ShadowHand
    {
        private Pose[] _localJointMap;
        private Pose[] _worldJointMap;
        private Pose _rootPose;
        private float _rootScale;
        private int _dirtyMap;

        public ShadowHand()
        {
            _localJointMap = new Pose[(int)HandJointId.HandEnd];
            _worldJointMap = new Pose[(int)HandJointId.HandEnd];
            for (int i = 0; i < _localJointMap.Length; i++)
            {
                _localJointMap[i] = Pose.identity;
                _worldJointMap[i] = Pose.identity;
            }
            _rootPose = Pose.identity;
            _rootScale = 1f;
            _dirtyMap = 0;
        }

        public Pose GetLocalPose(HandJointId handJointId)
        {
            return _localJointMap[(int)handJointId];
        }

        public void SetLocalPose(HandJointId jointId, Pose pose)
        {
            _localJointMap[(int)jointId] = pose;
            MarkDirty(jointId);
        }

        public Pose GetWorldPose(HandJointId jointId)
        {
            UpdateDirty(jointId);
            return _worldJointMap[(int)jointId];
        }

        public Pose GetRoot() => _rootPose;

        public void SetRoot(Pose rootPose)
        {
            _rootPose = rootPose;
            MarkDirty(HandJointId.HandStart);
        }

        public float GetRootScale() => _rootScale;

        public void SetRootScale(float scale)
        {
            _rootScale = scale;
            MarkDirty(HandJointId.HandStart);
        }

        private bool CheckDirtyBit(int i) => ((_dirtyMap >> i) & 1) == 1;
        private void SetDirtyBit(int i) => _dirtyMap = _dirtyMap | (1 << i);
        private void ClearDirtyBit(int i) => _dirtyMap = _dirtyMap & ~(1 << i);

        private void MarkDirty(HandJointId jointId)
        {
            if (CheckDirtyBit((int)jointId))
            {
                return;
            }

            SetDirtyBit((int)jointId);
            foreach (HandJointId child in HandJointUtils.JointChildrenList[(int)jointId])
            {
                MarkDirty(child);
            }
        }

        private void UpdateDirty(HandJointId jointId)
        {
            if (!CheckDirtyBit((int)jointId))
            {
                return;
            }

            HandJointId parent = HandJointUtils.JointParentList[(int)jointId];
            if (parent != HandJointId.Invalid)
            {
                UpdateDirty(parent);
            }

            ClearDirtyBit((int)jointId);

            Pose parentWorldPose = (int)jointId == 0 ? _rootPose : GetWorldPose(parent);
            Pose localPose = _localJointMap[(int)jointId];
            _worldJointMap[(int)jointId] = new Pose(
                parentWorldPose.position + parentWorldPose.rotation * localPose.position * _rootScale,
                parentWorldPose.rotation * localPose.rotation
            );
        }

        public void Copy(ShadowHand hand)
        {
            SetRoot(hand.GetRoot());
            SetRootScale(hand.GetRootScale());
            for (int i = 0; i < (int)HandJointId.HandEnd; i++)
            {
                HandJointId jointId = (HandJointId)i;
                SetLocalPose(jointId, hand.GetLocalPose(jointId));
            }
        }
    }

    public static class ShadowHandExtensions
    {
        public static void FromHandRoot(this ShadowHand shadow, IHand hand)
        {
            hand.GetRootPose(out Pose root);
            shadow.SetRoot(root);
            shadow.SetRootScale(hand.Scale);
        }

        public static void FromHandFingers(this ShadowHand shadow, IHand hand, bool flipHandedness)
        {
            hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses);
            if (localJointPoses.Count != (int)HandJointId.HandEnd)
            {
                return;
            }

            for (int i = 0; i < (int)HandJointId.HandEnd; i++)
            {
                Pose pose = localJointPoses[i];
                pose.position *= flipHandedness ? -1f : 1f;
                shadow.SetLocalPose((HandJointId)i, pose);
            }
        }

        public static void FromHand(this ShadowHand shadow, IHand hand, bool flipHandedness = false)
        {
            FromHandRoot(shadow, hand);
            FromHandFingers(shadow, hand, flipHandedness);
        }
    }
}
