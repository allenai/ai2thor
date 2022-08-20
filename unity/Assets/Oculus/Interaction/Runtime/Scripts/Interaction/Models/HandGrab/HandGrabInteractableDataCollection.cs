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

using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// A collection of HandGrabInteractable Data, to be used to store the information of several HandGrabInteractable
    /// so it survives Play-Mode Edit-Mode cycles.
    ///
    /// Use this to store information once in Play-Mode (where Hand-tracking can be used)
    /// and then restore it forever at Edit-time.
    /// </summary>
    [CreateAssetMenu(menuName = "Oculus/Interaction/SDK/Pose Authoring/HandGrabInteractable Data Collection")]
    public class HandGrabInteractableDataCollection : ScriptableObject
    {
        /// <summary>
        /// The data-only version of the HandGrabInteractable to be restored.
        /// Do not modify this manually here unless you are sure of what you are doing, instead
        /// reload it at Edit-Mode and use the provided tools at the HandGrabInteractable.
        /// </summary>
        [SerializeField]
        [Tooltip("Do not modify this manually unless you are sure! Instead load the HandGrabInteractable and use the tools provided.")]
        private List<HandGrabInteractableData> _interactablesData;

        /// <summary>
        /// General getter for the data-only version of the HandGrabInteractable to be restored.
        /// </summary>
        public List<HandGrabInteractableData> InteractablesData => _interactablesData;

        /// <summary>
        /// Register all the data into the Asset Database so it survives the Play-Mode shutdown.
        /// </summary>
        /// <param name="interactablesData"></param>
        public void StoreInteractables(List<HandGrabInteractableData> interactablesData)
        {
            _interactablesData = interactablesData;
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}
