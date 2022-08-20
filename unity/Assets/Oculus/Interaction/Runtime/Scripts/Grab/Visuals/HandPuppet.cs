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
    /// This class is a visual representation of a rigged hand (typically a skin-mesh renderer)
    /// that can move its position/rotation and the rotations of the joints that compose it.
    /// It also can offset the rotations of the individual joints, adapting the provided
    /// data to any rig.
    /// </summary>
    public class HandPuppet : MonoBehaviour
    {
        /// <summary>
        /// Joints of the hand and their relative rotations compared to hand-tracking.
        /// </summary>
        [SerializeField]
        private List<HandJointMap> _jointMaps = new List<HandJointMap>(FingersMetadata.HAND_JOINT_IDS.Length);

        /// <summary>
        /// General getter for the joints of the hand.
        /// </summary>
        public List<HandJointMap> JointMaps
        {
            get
            {
                return _jointMaps;
            }
        }

        /// <summary>
        /// Current scale of the represented hand.
        /// </summary>
        public float Scale
        {
            get
            {
                return this.transform.localScale.x;
            }
            set
            {
                this.transform.localScale = Vector3.one * value;
            }
        }

        private JointCollection _jointsCache;
        private JointCollection JointsCache
        {
            get
            {
                if (_jointsCache == null)
                {
                    _jointsCache = new JointCollection(_jointMaps);
                }
                return _jointsCache;
            }
        }

        /// <summary>
        /// Rotates all the joints in this puppet to the desired pose.
        /// </summary>
        /// <param name="jointRotations">
        /// Array of rotations to use for the fingers. It must follow the FingersMetaData.HAND_JOINT_IDS order.
        /// </param>
        public void SetJointRotations(in Quaternion[] jointRotations)
        {
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                HandJointMap jointMap = JointsCache[i];
                if (jointMap != null)
                {
                    Transform jointTransform = jointMap.transform;
                    Quaternion targetRot = jointMap.RotationOffset * jointRotations[i];
                    jointTransform.localRotation = targetRot;
                }
            }
        }

        /// <summary>
        /// Rotates and Translate the hand Wrist so it aligns with the given pose.
        /// It can apply an offset for when using controllers.
        /// </summary>
        /// <param name="rootPose">The Wrist Pose to set this puppet to.</param>
        /// </param>
        public void SetRootPose(in Pose rootPose)
        {
            this.transform.SetPose(rootPose, Space.World);
        }

        /// <summary>
        /// Copies the rotations of all the joints available in the puppet
        /// as they are visually presented.
        /// Note that any missing joints are skipped.
        /// </summary>
        /// <param name="result">Structure to copy the joints to</param>
        public void CopyCachedJoints(ref HandPose result)
        {
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                HandJointMap jointMap = JointsCache[i];
                if (jointMap != null)
                {
                    result.JointRotations[i] = jointMap.TrackedRotation;
                }
            }
        }
    }
}
