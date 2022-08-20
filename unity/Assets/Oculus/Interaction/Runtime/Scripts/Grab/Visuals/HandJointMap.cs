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

namespace Oculus.Interaction.HandGrab.Visuals
{
    /// <summary>
    /// Stores the translation between hand tracked data and the represented joint.
    /// </summary>
    [System.Serializable]
    public class HandJointMap
    {
        /// <summary>
        /// The unique identifier for the joint.
        /// </summary>
        public HandJointId id;
        /// <summary>
        /// The transform that this joint drives.
        /// </summary>
        public Transform transform;
        /// <summary>
        /// The rotation offset between the hand-tracked joint, and the represented joint.
        /// </summary>
        public Vector3 rotationOffset;

        /// <summary>
        /// Get the rotationOffset as a Quaternion.
        /// </summary>
        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }

        /// <summary>
        /// Get the raw rotation of the joint, taken from the tracking data
        /// </summary>
        public Quaternion TrackedRotation
        {
            get
            {
                return Quaternion.Inverse(RotationOffset) * transform.localRotation;
            }
        }
    }

    /// <summary>
    /// A collection of joint maps to quick access the joints that are actually available in the hand rig.
    /// Stores an internal array of indices so it can transform from positions in the HandPose.HAND_JOINTIDS collection
    /// to the JointMap List without having to search for the (maybe unavailable) index every time.
    /// </summary>
    [System.Serializable]
    public class JointCollection
    {
        /// <summary>
        /// List of indices of the joints in the actual rig for quick access
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private int[] _jointIndices = new int[FingersMetadata.HAND_JOINT_IDS.Length];

        /// <summary>
        /// List of joints in the actual rig
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private List<HandJointMap> _jointMaps;

        public JointCollection(List<HandJointMap> joints)
        {
            _jointMaps = joints;
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                HandJointId boneId = FingersMetadata.HAND_JOINT_IDS[i];
                _jointIndices[i] = joints.FindIndex(bone => bone.id == boneId);
            }
        }

        public HandJointMap this[int jointIndex]
        {
            get
            {
                int joint = _jointIndices[jointIndex];
                if (joint >= 0)
                {
                    return _jointMaps[joint];
                }
                return null;
            }
        }
    }
}
