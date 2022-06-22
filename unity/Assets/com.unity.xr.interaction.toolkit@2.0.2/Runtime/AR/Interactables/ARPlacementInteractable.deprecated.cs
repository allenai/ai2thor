//-----------------------------------------------------------------------
// <copyright originalFile="AndyPlacementManipulator.cs" company="Google">
// <renamed file="ARPlacementInteractable.cs">
//
// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

// Modifications copyright © 2020 Unity Technologies ApS

#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION

using System;
using UnityEngine.Events;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// (Deprecated) <see cref="UnityEvent"/> that is invoked when an object is placed.
    /// Use <see cref="ARObjectPlacementEvent" /> instead.
    /// </summary>
    [Serializable, Obsolete("ARObjectPlacedEvent has been deprecated. Use ARObjectPlacementEvent instead.")]
    public class ARObjectPlacedEvent : UnityEvent<ARPlacementInteractable, GameObject>
    {
    }

    public partial class ARPlacementInteractable
    {
#pragma warning disable 618
        [SerializeField]
        ARObjectPlacedEvent m_OnObjectPlaced = new ARObjectPlacedEvent();

        /// <summary>
        /// Gets or sets the event that is called when this Interactable places a new <see cref="GameObject"/> in the world.
        /// </summary>
        /// <remarks>
        /// <c>onObjectPlaced</c> has been deprecated. Use <see cref="objectPlaced"/> with updated signature instead.
        /// </remarks>
        [Obsolete("onObjectPlaced has been deprecated. Use objectPlaced with updated signature instead.")]
        public ARObjectPlacedEvent onObjectPlaced
        {
            get => m_OnObjectPlaced;
            set => m_OnObjectPlaced = value;
        }
#pragma warning restore 618
    }
}

#endif
