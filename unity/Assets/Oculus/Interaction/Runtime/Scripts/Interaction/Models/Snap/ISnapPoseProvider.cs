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

namespace Oculus.Interaction
{
    /// <summary>
    /// This interface is used to specify how a series of slots (e.g. an inventory)
    /// will be laid out and how the objects will snap to them within a SnapInteractable.
    /// </summary>
    public interface ISnapPoseProvider
    {
        /// <summary>
        /// Indicates that a new element is hovering the slots.
        /// </summary>
        /// <param name="interactor">The element nearby</param>
        void TrackInteractor(SnapInteractor interactor);
        /// <summary>
        /// Indicates that an element is no longer part of the snap interaction
        /// </summary>
        /// <param name="interactor">The element that exited</param>
        void UntrackInteractor(SnapInteractor interactor);
        /// <summary>
        /// Indicates that the tracked element has been snapped by
        /// the interactable.
        /// </summary>
        /// <param name="interactor">The selected element</param>
        void SnapInteractor(SnapInteractor interactor);
        /// <summary>
        /// Indicates that the element is not snapping anymore
        /// to the interactable.
        /// </summary>
        /// <param name="interactor">the unselected element</param>
        void UnsnapInteractor(SnapInteractor interactor);
        /// <summary>
        /// Called frequently when a non-placed element moves near the slots.
        /// Use this callback to reorganize the placed elements.
        /// </summary>
        /// <param name="interactor">The element nearby</param>
        void UpdateTrackedInteractor(SnapInteractor interactor);
        /// <summary>
        /// This method returns the desired Pose for a queried element
        /// within the interactable.
        /// </summary>
        /// <param name="interactor">Queried element</param>
        /// <param name="pose">The desired pose in the interactable</param>
        /// <returns>True if the element has a valid pose in the zone</returns>
        bool PoseForInteractor(SnapInteractor interactor, out Pose pose);
    }
}
