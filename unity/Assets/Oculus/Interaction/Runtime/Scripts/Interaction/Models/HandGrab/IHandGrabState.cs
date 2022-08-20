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
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Defines the strategy for aligning the hand to the snapped object.
    /// The hand can go to the object upon selection, during hover or
    /// simply stay in its pose.
    /// </summary>
    public enum HandAlignType
    {
        None,
        AlignOnGrab,
        AttractOnHover,
        AlignFingersOnHover
    }

    /// <summary>
    /// Interface for interactors that allow aligning to an object.
    /// Contains information to drive the HandGrabVisual moving
    /// the fingers and wrist.
    /// </summary>
    public interface IHandGrabState
    {
        bool IsGrabbing { get; }
        float FingersStrength { get; }
        float WristStrength { get; }
        Pose WristToGrabPoseOffset { get; }
        HandFingerFlags GrabbingFingers();
        HandGrabTarget HandGrabTarget { get; }
    }
}
