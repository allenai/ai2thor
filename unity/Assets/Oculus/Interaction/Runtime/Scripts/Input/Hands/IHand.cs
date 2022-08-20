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
using System;

namespace Oculus.Interaction.Input
{
    public interface IHand
    {
        Handedness Handedness { get; }

        bool IsConnected { get; }

        /// <summary>
        /// The hand is connected and tracked, and the root pose's tracking data is marked as
        /// high confidence.
        /// If this is true, then it implies that IsConnected and IsRootPoseValid are also true,
        /// so they don't need to be checked in addition to this.
        /// </summary>
        bool IsHighConfidence { get; }

        bool IsDominantHand { get; }
        float Scale { get; }
        bool GetFingerIsPinching(HandFinger finger);
        bool GetIndexFingerIsPinching();

        /// <summary>
        /// Will return true if a pointer pose is available, that can be retrieved via
        /// <see cref="GetPointerPose"/>
        /// </summary>
        bool IsPointerPoseValid { get; }

        /// <summary>
        /// Attempts to calculate the pose that can be used as a root for raycasting, in world space
        /// Returns false if there is no valid tracking data.
        /// </summary>
        bool GetPointerPose(out Pose pose);

        /// <summary>
        /// Attempts to calculate the pose of the requested hand joint, in world space.
        /// Returns false if the skeleton is not yet initialized, or there is no valid
        /// tracking data.
        /// </summary>
        bool GetJointPose(HandJointId handJointId, out Pose pose);

        /// <summary>
        /// Attempts to calculate the pose of the requested hand joint, in local space.
        /// Returns false if the skeleton is not yet initialized, or there is no valid
        /// tracking data.
        /// </summary>
        bool GetJointPoseLocal(HandJointId handJointId, out Pose pose);

        /// <summary>
        /// Returns an array containing the local pose of each joint. The poses
        /// do not have the root pose applied, nor the hand scale. It is in the same coordinate
        /// system as the hand skeleton.
        /// </summary>
        /// <param name="localJointPoses">The array with the local joint poses.
        /// It will be empty if no poses where found</param>
        /// <returns>
        /// True if the poses collection was correctly populated. False otherwise.
        /// </returns>
        bool GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses);

        /// <summary>
        /// Attempts to calculate the pose of the requested hand joint relative to the wrist.
        /// Returns false if the skeleton is not yet initialized, or there is no valid
        /// tracking data.
        /// </summary>
        bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose);

        /// <summary>
        /// Returns an array containing the pose of each joint relative to the wrist. The poses
        /// do not have the root pose applied, nor the hand scale. It is in the same coordinate
        /// system as the hand skeleton.
        /// </summary>
        /// <param name="jointPosesFromWrist">The array with the joint poses from the wrist.
        /// It will be empty if no poses where found</param>
        /// <returns>
        /// True if the poses collection was correctly populated. False otherwise.
        /// </returns>
        bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist);

        /// <summary>
        /// Obtains palm pose in local space.
        /// </summary>
        /// <param name="pose">The pose to populate</param>
        /// <returns>
        /// True if pose was obtained.
        /// </returns>
        bool GetPalmPoseLocal(out Pose pose);

        bool GetFingerIsHighConfidence(HandFinger finger);
        float GetFingerPinchStrength(HandFinger finger);

        /// <summary>
        /// True if the hand is currently tracked, thus tracking poses are available for the hand
        /// root and finger joints.
        /// This property does not indicate pointing pose validity, which has its own property:
        /// <see cref="IsPointerPoseValid"/>.
        /// </summary>
        bool IsTrackedDataValid { get; }

        /// <summary>
        /// Gets the root pose of the wrist, in world space.
        /// Will return true if a pose was available; false otherwise.
        /// Confidence level of the pose is exposed via <see cref="IsHighConfidence"/>.
        /// </summary>
        bool GetRootPose(out Pose pose);

        /// <summary>
        /// Will return true if an HMD Center Eye pose available, that can be retrieved via
        /// <see cref="GetCenterEyePose"/>
        /// </summary>
        bool IsCenterEyePoseValid { get; }

        /// <summary>
        /// Gets the pose of the center eye (HMD), in world space.
        /// Will return true if a pose was available; false otherwise.
        /// </summary>
        bool GetCenterEyePose(out Pose pose);

        /// <summary>
        /// The transform that was applied to all tracking space poses to convert them to world
        /// space.
        /// </summary>
        Transform TrackingToWorldSpace { get; }

        /// <summary>
        /// Incremented every time the source tracking or state data changes.
        /// </summary>
        int CurrentDataVersion { get; }

        /// <summary>
        /// An Aspect provides additional functionality on top of what the HandState provides.
        /// The underlying hand is responsible for finding the most appropriate component.
        /// It is usually, but not necessarily, located within the same GameObject as the
        /// underlying hand.
        /// For example, this method can be used to source the SkinnedMeshRenderer representing the
        /// hand, if one exists.
        /// <returns>true if an aspect of the requested type was found, false otherwise</returns>
        /// </summary>
        bool GetHandAspect<TComponent>(out TComponent foundComponent) where TComponent : class;

        event Action WhenHandUpdated;
    }
}
