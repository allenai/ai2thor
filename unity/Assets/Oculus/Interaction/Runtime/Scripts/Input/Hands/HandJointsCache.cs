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
    public class HandJointCache
    {
        private Pose[] _localPoses = new Pose[Constants.NUM_HAND_JOINTS];
        private Pose[] _posesFromWrist = new Pose[Constants.NUM_HAND_JOINTS];
        private Pose[] _worldPoses = new Pose[Constants.NUM_HAND_JOINTS];

        private ReadOnlyHandJointPoses _posesFromWristCollection;
        private ReadOnlyHandJointPoses _localPosesCollection;

        private IReadOnlyHandSkeletonJointList _originalJoints;
        private int _dirtyWorldJoints = 0;
        private int _dirtyWristJoints = 0;

        public int LocalDataVersion { get; private set; } = -1;

        public HandJointCache(IReadOnlyHandSkeleton handSkeleton)
        {
            LocalDataVersion = -1;
            _posesFromWrist[0] = Pose.identity;

            _posesFromWristCollection = new ReadOnlyHandJointPoses(_posesFromWrist);
            _localPosesCollection = new ReadOnlyHandJointPoses(_localPoses);

            _originalJoints = handSkeleton.Joints;
        }

        public void Update(HandDataAsset data, int dataVersion)
        {
            _dirtyWorldJoints = _dirtyWristJoints = (1 << (int)HandJointId.HandEnd) - 1; //set all dirty
            if (!data.IsDataValidAndConnected)
            {
                return;
            }
            LocalDataVersion = dataVersion;
            UpdateAllLocalPoses(data);
        }

        public bool GetAllLocalPoses(out ReadOnlyHandJointPoses localJointPoses)
        {
            localJointPoses = _localPosesCollection;
            return _posesFromWristCollection.Count > 0;
        }

        public bool GetAllPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
        {
            UpdateAllPosesFromWrist();
            jointPosesFromWrist = _posesFromWristCollection;
            return _posesFromWristCollection.Count > 0;
        }

        public Pose LocalJointPose(HandJointId jointid)
        {
            return _localPoses[(int)jointid];
        }

        public Pose PoseFromWrist(HandJointId jointid)
        {
            Pose pose = _posesFromWrist[(int)jointid];
            UpdateWristJoint(jointid, ref pose);
            return pose;
        }

        public Pose WorldJointPose(HandJointId jointid, Pose rootPose, float handScale)
        {
            int jointIndex = (int)jointid;
            if ((_dirtyWorldJoints & (1 << jointIndex)) != 0) //its dirty
            {
                Pose wristPose = Pose.identity;
                UpdateWristJoint(jointid, ref wristPose);
                PoseUtils.Multiply(_localPoses[0], wristPose, ref _worldPoses[jointIndex]);
                _worldPoses[jointIndex].position *= handScale;
                _worldPoses[jointIndex].Postmultiply(rootPose);

                _dirtyWorldJoints = _dirtyWorldJoints & ~(1 << jointIndex); //set clean
            }

            return _worldPoses[jointIndex];
        }

        private void UpdateAllLocalPoses(HandDataAsset data)
        {
            for (int i = 0; i < Constants.NUM_HAND_JOINTS; ++i)
            {
                HandSkeletonJoint originalJoint = _originalJoints[i];
                _localPoses[i].position = originalJoint.pose.position;
                _localPoses[i].rotation = data.Joints[i];
            }
        }

        private void UpdateAllPosesFromWrist()
        {
            if (_dirtyWristJoints == 0) //its completely clean
            {
                return;
            }

            for (int jointIndex = 0; jointIndex < Constants.NUM_HAND_JOINTS; ++jointIndex)
            {
                if ((_dirtyWristJoints & (1 << jointIndex)) == 0) //its clean
                {
                    continue;
                }

                HandSkeletonJoint originalJoint = _originalJoints[jointIndex];
                if (originalJoint.parent >= 0)
                {
                    PoseUtils.Multiply(_posesFromWrist[originalJoint.parent],
                        _localPoses[jointIndex], ref _posesFromWrist[jointIndex]);
                }
            }
            _dirtyWristJoints = 0; //set all clean
        }

        private void UpdateWristJoint(HandJointId jointid, ref Pose pose)
        {
            int jointIndex = (int)jointid;
            if ((_dirtyWristJoints & (1 << jointIndex)) != 0)// its dirty
            {
                if (jointid > HandJointId.HandWristRoot)
                {
                    UpdateWristJoint((HandJointId)_originalJoints[jointIndex].parent, ref pose);
                    PoseUtils.Multiply(pose, _localPoses[jointIndex], ref _posesFromWrist[jointIndex]);
                }
                _dirtyWristJoints = _dirtyWristJoints & ~(1 << jointIndex); //set clean
            }
            pose.CopyFrom(_posesFromWrist[jointIndex]);
        }
    }
}
