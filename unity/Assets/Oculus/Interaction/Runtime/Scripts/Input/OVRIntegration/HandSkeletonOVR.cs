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
    public class HandSkeletonOVR : MonoBehaviour, IHandSkeletonProvider
    {
        private readonly HandSkeleton[] _skeletons = { new HandSkeleton(), new HandSkeleton() };

        public HandSkeleton this[Handedness handedness] => _skeletons[(int)handedness];

        protected void Awake()
        {
            ApplyToSkeleton(OVRSkeletonData.LeftSkeleton, _skeletons[0]);
            ApplyToSkeleton(OVRSkeletonData.RightSkeleton, _skeletons[1]);
        }

        public static HandSkeleton CreateSkeletonData(Handedness handedness)
        {
            HandSkeleton handSkeleton = new HandSkeleton();

            // When running in the editor, the call to load the skeleton from OVRPlugin may fail. Use baked skeleton
            // data.
            if (handedness == Handedness.Left)
            {
                ApplyToSkeleton(OVRSkeletonData.LeftSkeleton, handSkeleton);
            }
            else
            {
                ApplyToSkeleton(OVRSkeletonData.RightSkeleton, handSkeleton);
            }

            return handSkeleton;
        }

        private static void ApplyToSkeleton(in OVRPlugin.Skeleton2 ovrSkeleton, HandSkeleton handSkeleton)
        {
            int numJoints = handSkeleton.joints.Length;
            Assert.AreEqual(ovrSkeleton.NumBones, numJoints);

            for (int i = 0; i < numJoints; ++i)
            {
                ref var srcPose = ref ovrSkeleton.Bones[i].Pose;
                handSkeleton.joints[i] = new HandSkeletonJoint()
                {
                    pose = new Pose()
                    {
                        position = srcPose.Position.FromFlippedXVector3f(),
                        rotation = srcPose.Orientation.FromFlippedXQuatf()
                    },
                    parent = ovrSkeleton.Bones[i].ParentBoneIndex
                };
            }
        }
    }
}
