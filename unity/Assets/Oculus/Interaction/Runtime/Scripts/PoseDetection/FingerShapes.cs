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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    public enum FingerFeature
    {
        Curl,
        Flexion,
        Abduction,
        Opposition
    }

    public class FingerShapes
    {
        #region Joints Visualization Mappings
        private static readonly HandJointId[][] CURL_LINE_JOINTS =
        {
            new [] {HandJointId.HandThumb2, HandJointId.HandThumb3, HandJointId.HandThumbTip},
            new [] {HandJointId.HandIndex2, HandJointId.HandIndex3, HandJointId.HandIndexTip},
            new [] {HandJointId.HandMiddle2, HandJointId.HandMiddle3, HandJointId.HandMiddleTip},
            new [] {HandJointId.HandRing2, HandJointId.HandRing3, HandJointId.HandRingTip},
            new [] {HandJointId.HandPinky2, HandJointId.HandPinky3, HandJointId.HandPinkyTip}
        };
        private static readonly HandJointId[][] FLEXION_LINE_JOINTS =
        {
            new [] {HandJointId.HandThumb1, HandJointId.HandThumb2, HandJointId.HandThumb3},
            new [] {HandJointId.HandIndex1, HandJointId.HandIndex2, HandJointId.HandIndex3},
            new [] {HandJointId.HandMiddle1, HandJointId.HandMiddle2, HandJointId.HandMiddle3},
            new [] {HandJointId.HandRing1, HandJointId.HandRing2, HandJointId.HandRing3},
            new [] {HandJointId.HandPinky1, HandJointId.HandPinky2, HandJointId.HandPinky3}
        };
        private static readonly HandJointId[][] ABDUCTION_LINE_JOINTS =
        {
            new [] {HandJointId.HandThumbTip, HandJointId.HandThumb1, HandJointId.HandIndex1, HandJointId.HandIndexTip},
            new [] {HandJointId.HandIndexTip, HandJointId.HandIndex1, HandJointId.HandMiddle1, HandJointId.HandMiddleTip},
            new [] {HandJointId.HandMiddleTip, HandJointId.HandMiddle1, HandJointId.HandRing1, HandJointId.HandRingTip},
            new [] {HandJointId.HandRingTip, HandJointId.HandRing1, HandJointId.HandPinky1, HandJointId.HandPinkyTip},
            Array.Empty<HandJointId>()
        };
        private static readonly HandJointId[][] OPPOSITION_LINE_JOINTS =
        {
            Array.Empty<HandJointId>(),
            new [] {HandJointId.HandThumbTip, HandJointId.HandIndexTip},
            new [] {HandJointId.HandThumbTip, HandJointId.HandMiddleTip},
            new [] {HandJointId.HandThumbTip, HandJointId.HandRingTip},
            new [] {HandJointId.HandThumbTip, HandJointId.HandPinkyTip},
        };
        #endregion

        #region Joint Calculation Mappings
        private static readonly HandJointId[][] CURL_ANGLE_JOINTS =
        {
            new[]
            {
                HandJointId.HandThumb1, HandJointId.HandThumb2, HandJointId.HandThumb3,
                HandJointId.HandThumbTip
            },
            new[]
            {
                HandJointId.HandIndex1, HandJointId.HandIndex2, HandJointId.HandIndex3,
                HandJointId.HandIndexTip
            },
            new[]
            {
                HandJointId.HandMiddle1, HandJointId.HandMiddle2, HandJointId.HandMiddle3,
                HandJointId.HandMiddleTip
            },
            new[]
            {
                HandJointId.HandRing1, HandJointId.HandRing2, HandJointId.HandRing3,
                HandJointId.HandRingTip
            },
            new[]
            {
                HandJointId.HandPinky1, HandJointId.HandPinky2, HandJointId.HandPinky3,
                HandJointId.HandPinkyTip
            }
        };

        private static readonly HandJointId[] KNUCKLE_JOINTS =
        {
            HandJointId.HandThumb2,
            HandJointId.HandIndex1,
            HandJointId.HandMiddle1,
            HandJointId.HandRing1,
            HandJointId.HandPinky1

        };
        #endregion

        public virtual float GetValue(HandFinger finger, FingerFeature feature, IHand hand)
        {
            switch (feature)
            {
                case FingerFeature.Curl:
                    return GetCurlValue(finger, hand);
                case FingerFeature.Flexion:
                    return GetFlexionValue(finger, hand);
                case FingerFeature.Abduction:
                    return GetAbductionValue(finger, hand);
                case FingerFeature.Opposition:
                    return GetOppositionValue(finger, hand);

                default:
                    return 0.0f;
            }
        }

        private static float PosesCurlValue(Pose p0, Pose p1, Pose p2)
        {
            Vector3 bone1 = p0.position - p1.position;
            Vector3 bone2 = p2.position - p1.position;
            float angle = Vector3.SignedAngle(bone1, bone2, p1.forward * -1f);
            if (angle < 0f) angle += 360f;
            return angle;
        }

        public static float PosesListCurlValue(Pose[] poses)
        {
            float angleSum = 0;
            for (int i = 0; i < poses.Length - 2; i++)
            {
                angleSum += PosesCurlValue(poses[i], poses[i+1], poses[i+2]);
            }
            return angleSum;
        }

        protected float JointsCurlValue(HandJointId[] joints, IHand hand)
        {
            if (!hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            Pose[] jointPoses = new Pose[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                jointPoses[i] = poses[(int)joints[i]];
            }

            return PosesListCurlValue(jointPoses);
        }

        public float GetCurlValue(HandFinger finger, IHand hand)
        {
            HandJointId[] handJointIds = CURL_ANGLE_JOINTS[(int)finger];
            return JointsCurlValue(handJointIds, hand) / (handJointIds.Length - 2);
        }

        public float GetFlexionValue(HandFinger finger, IHand hand)
        {
            if (!hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            HandJointId knuckle = KNUCKLE_JOINTS[(int)finger];
            Vector3 handDir = Vector3.up;
            Vector3 fingerDir = Vector3.ProjectOnPlane(poses[knuckle].up, Vector3.forward);

            return 180f + Vector3.SignedAngle(handDir, fingerDir, Vector3.back);
        }

        public float GetAbductionValue(HandFinger finger, IHand hand)
        {
            if (finger == HandFinger.Pinky
                || !hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            HandFinger nextFinger = finger + 1;
            Vector3 fingerProximal = poses[HandJointUtils.GetHandFingerProximal(finger)].position;
            Vector3 proximalMidpoint = Vector3.Lerp(
                fingerProximal,
                poses[HandJointUtils.GetHandFingerProximal(nextFinger)].position,
                0.5f);
            Vector3 normal1;
            if (finger == HandFinger.Thumb)
            {
                normal1 = poses[HandJointUtils.GetHandFingerTip(finger)].position -
                                  fingerProximal;
            }
            else
            {
                normal1 = poses[HandJointUtils.GetHandFingerTip(finger)].position -
                                  proximalMidpoint;
            }

            Vector3 normal2 = poses[HandJointUtils.GetHandFingerTip(nextFinger)].position -
                              proximalMidpoint;
            Vector3 axis = Vector3.Cross(normal1, normal2);
            return Vector3.SignedAngle(normal1, normal2, axis);
        }

        public float GetOppositionValue(HandFinger finger, IHand hand)
        {
            if (finger == HandFinger.Thumb
                || !hand.GetJointPosesFromWrist(out ReadOnlyHandJointPoses poses))
            {
                return 0.0f;
            }

            Vector3 pos1 = poses[HandJointUtils.GetHandFingerTip(finger)].position;
            Vector3 pos2 = poses[HandJointId.HandThumbTip].position;
            return Vector3.Magnitude(pos1 - pos2);
        }

        public virtual IReadOnlyList<HandJointId> GetJointsAffected(HandFinger finger, FingerFeature feature)
        {
            switch (feature)
            {
                case FingerFeature.Curl:
                    return CURL_LINE_JOINTS[(int)finger];
                case FingerFeature.Flexion:
                    return FLEXION_LINE_JOINTS[(int)finger];
                case FingerFeature.Abduction:
                    return ABDUCTION_LINE_JOINTS[(int)finger];
                case FingerFeature.Opposition:
                    return OPPOSITION_LINE_JOINTS[(int)finger];
                default:
                    return null;
            }
        }
    }
}
