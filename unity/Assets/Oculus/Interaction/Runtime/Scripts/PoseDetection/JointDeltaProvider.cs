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

namespace Oculus.Interaction.PoseDetection
{
    public class JointDeltaConfig
    {
        public readonly int InstanceID;
        public readonly IEnumerable<HandJointId> JointIDs;

        public JointDeltaConfig(int instanceID, IEnumerable<HandJointId> jointIDs)
        {
            InstanceID = instanceID;
            JointIDs = jointIDs;
        }
    }

    public class JointDeltaProvider : MonoBehaviour
    {
        private class PoseData
        {
            public bool IsValid = false;

            public Pose Pose = Pose.identity;
        }

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand;

        private Dictionary<HandJointId, PoseData[]> _poseDataCache =
            new Dictionary<HandJointId, PoseData[]>();

        private HashSet<HandJointId> _trackedJoints =
            new HashSet<HandJointId>();

        private Dictionary<int, List<HandJointId>> _requestors =
            new Dictionary<int, List<HandJointId>>();

        private int PrevDataIndex => 1 - CurDataIndex;
        private int CurDataIndex = 0;

        private int _lastUpdateDataVersion;

        protected bool _started = false;

        /// <summary>
        /// Get the delta position between the previous pose and current pose
        /// </summary>
        /// <param name="joint">The joint for which to retrieve data</param>
        /// <param name="delta">The position delta between poses in world space</param>
        /// <returns>True if data available</returns>
        public bool GetPositionDelta(HandJointId joint, out Vector3 delta)
        {
            UpdateData();

            PoseData prevPose = _poseDataCache[joint][PrevDataIndex];
            PoseData curPose = _poseDataCache[joint][CurDataIndex];

            if (!prevPose.IsValid || !curPose.IsValid)
            {
                delta = Vector3.zero;
                return false;
            }

            delta = curPose.Pose.position - prevPose.Pose.position;
            return true;
        }

        /// <summary>
        /// Get the delta rotation between the previous pose and current pose
        /// </summary>
        /// <param name="joint">The joint for which to retrieve data</param>
        /// <param name="delta">The rotation delta between poses in world space</param>
        /// <returns>True if data available</returns>
        public bool GetRotationDelta(HandJointId joint, out Quaternion delta)
        {
            UpdateData();

            PoseData prevPose = _poseDataCache[joint][PrevDataIndex];
            PoseData curPose = _poseDataCache[joint][CurDataIndex];

            if (!prevPose.IsValid || !curPose.IsValid)
            {
                delta = Quaternion.identity;
                return false;
            }

            delta = curPose.Pose.rotation * Quaternion.Inverse(prevPose.Pose.rotation);
            return true;
        }

        /// <summary>
        /// Get the previous frame's pose
        /// </summary>
        /// <param name="joint">The joint for which to retrieve data</param>
        /// <param name="pose">The previous pose</param>
        /// <returns>True if data available</returns>
        public bool GetPrevJointPose(HandJointId joint, out Pose pose)
        {
            UpdateData();

            PoseData poseData = _poseDataCache[joint][PrevDataIndex];
            pose = poseData.Pose;
            return poseData.IsValid;
        }

        public void RegisterConfig(JointDeltaConfig config)
        {
            bool containsKeyAlready = _requestors.ContainsKey(config.InstanceID);
            Assert.IsFalse(containsKeyAlready,
                "Trying to register multiple configs with the same id into " +
                "JointDeltaProvider.");

            _requestors.Add(config.InstanceID, new List<HandJointId>(config.JointIDs));

            // Check if any new joints added, if so then add to cache
            foreach (var joint in config.JointIDs)
            {
                if (!_poseDataCache.ContainsKey(joint))
                {
                    _poseDataCache.Add(joint, new PoseData[2]
                        { new PoseData(), new PoseData() });

                    // New joint tracked, so write current data
                    PoseData toWrite = _poseDataCache[joint][CurDataIndex];
                    toWrite.IsValid = Hand.GetJointPose(joint, out toWrite.Pose);
                }
            }
        }

        public void UnRegisterConfig(JointDeltaConfig config)
        {
            _requestors.Remove(config.InstanceID);
        }

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += UpdateData;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= UpdateData;
            }
        }

        private void UpdateData()
        {
            if (Hand.CurrentDataVersion <= _lastUpdateDataVersion)
            {
                return;
            }
            _lastUpdateDataVersion = Hand.CurrentDataVersion;

            // Swap read and write indices each data version
            CurDataIndex = 1 - CurDataIndex;

            // Only fetch pose data for currently tracked joints
            _trackedJoints.Clear();
            foreach (var key in _requestors.Keys)
            {
                IList<HandJointId> joints = _requestors[key];
                _trackedJoints.UnionWithNonAlloc(joints);
            }

            // Fetch pose data for tracked joints, and
            // invalidate data for untracked joints
            foreach (var joint in _poseDataCache.Keys)
            {
                PoseData toWrite = _poseDataCache[joint][CurDataIndex];
                toWrite.IsValid = _trackedJoints.Contains(joint) &&
                                  Hand.GetJointPose(joint, out toWrite.Pose);
            }
        }
    }
}
