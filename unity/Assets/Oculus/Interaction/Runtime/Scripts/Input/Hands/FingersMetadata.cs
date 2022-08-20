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

namespace Oculus.Interaction.Input
{
    public enum JointFreedom
    {
        Free,
        Constrained,
        Locked
    }

    /// <summary>
    /// This class contains a series of useful fingers-related data structures
    /// to be used for optimal calculations without relying in dictionaries.
    ///
    /// Since we always assume the hand pose information to be sorted in
    /// the HAND_JOINT_IDS order, we can align multiple data structures
    /// that follow that convention.
    /// </summary>
    public class FingersMetadata
    {
        public static JointFreedom[] DefaultFingersFreedom()
        {
            return new JointFreedom[Constants.NUM_FINGERS]
            {
                JointFreedom.Locked,
                JointFreedom.Locked,
                JointFreedom.Constrained,
                JointFreedom.Constrained,
                JointFreedom.Free
            };
        }

        public static int HandJointIdToIndex(HandJointId id)
        {
            return (int)id - (int)HandJointId.HandThumb0;
        }

        /// <summary>
        /// Valid identifiers for the i-bone of a hand.
        /// </summary>
        public static readonly HandJointId[] HAND_JOINT_IDS = new HandJointId[]
        {
            HandJointId.HandThumb0,
            HandJointId.HandThumb1,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
            HandJointId.HandIndex1,
            HandJointId.HandIndex2,
            HandJointId.HandIndex3,
            HandJointId.HandMiddle1,
            HandJointId.HandMiddle2,
            HandJointId.HandMiddle3,
            HandJointId.HandRing1,
            HandJointId.HandRing2,
            HandJointId.HandRing3,
            HandJointId.HandPinky0,
            HandJointId.HandPinky1,
            HandJointId.HandPinky2,
            HandJointId.HandPinky3
        };

        /// <summary>
        /// This array is used to convert from Finger id to the list indices
        /// of its joint in the HAND_JOINT_IDS list.
        /// </summary>
        public static readonly int[][] FINGER_TO_JOINT_INDEX = new int[][]
        {
            new[] {0,1,2,3},
            new[] {4,5,6},
            new[] {7,8,9},
            new[] {10,11,12},
            new[] {13,14,15,16}
        };

        public static readonly HandJointId[][] FINGER_TO_JOINTS = new[]
        {
            new HandJointId[]
            {
                HandJointId.HandThumb0,
                HandJointId.HandThumb1,
                HandJointId.HandThumb2,
                HandJointId.HandThumb3
            },
            new HandJointId[]
            {
                HandJointId.HandIndex1,
                HandJointId.HandIndex2,
                HandJointId.HandIndex3
            },
            new HandJointId[]
            {
                HandJointId.HandMiddle1,
                HandJointId.HandMiddle2,
                HandJointId.HandMiddle3
            },
            new HandJointId[]
            {
                HandJointId.HandRing1,
                HandJointId.HandRing2,
                HandJointId.HandRing3
            },
            new HandJointId[]
            {
                HandJointId.HandPinky0,
                HandJointId.HandPinky1,
                HandJointId.HandPinky2,
                HandJointId.HandPinky3
            }
        };

        /// <summary>
        /// Array order following HAND_JOINT_IDS that indicates if the i joint
        /// can spread (rotate around Y). Should be true for the root of the fingers
        /// but Pink and Thumb are special cases
        /// </summary>
        public static readonly bool[] HAND_JOINT_CAN_SPREAD = new bool[]
        {
            true, //HandJointId.HandThumb0
            true, //HandJointId.HandThumb1
            false,//HandJointId.HandThumb2
            false,//HandJointId.HandThumb3
            true, //HandJointId.HandIndex1
            false,//HandJointId.HandIndex2
            false,//HandJointId.HandIndex3
            true, //HandJointId.HandMiddle1
            false,//HandJointId.HandMiddle2
            false,//HandJointId.HandMiddle3
            true, //HandJointId.HandRing1
            false,//HandJointId.HandRing2
            false,//HandJointId.HandRing3
            true, //HandJointId.HandPinky0
            true, //HandJointId.HandPinky1
            false,//HandJointId.HandPinky2
            false //HandJointId.HandPinky3
        };

        /// <summary>
        /// Map HandJointId to HandFinger
        /// </summary>
        public static readonly HandFinger[] JOINT_TO_FINGER = new HandFinger[]
        {
            HandFinger.Invalid,
            HandFinger.Invalid,
            HandFinger.Thumb,
            HandFinger.Thumb,
            HandFinger.Thumb,
            HandFinger.Thumb,
            HandFinger.Index,
            HandFinger.Index,
            HandFinger.Index,
            HandFinger.Middle,
            HandFinger.Middle,
            HandFinger.Middle,
            HandFinger.Ring,
            HandFinger.Ring,
            HandFinger.Ring,
            HandFinger.Pinky,
            HandFinger.Pinky,
            HandFinger.Pinky,
            HandFinger.Pinky,
            HandFinger.Thumb,
            HandFinger.Index,
            HandFinger.Middle,
            HandFinger.Ring,
            HandFinger.Pinky
        };

        /// <summary>
        /// Map HandJointId to HandFinger
        /// </summary>
        public static readonly int[] JOINT_TO_FINGER_INDEX = new int[]
        {
            -1, -1, 0, 1, 2, 3, 0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2, 3, 4, 3, 3, 3, 4
        };
    }
}
