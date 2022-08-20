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
using System.Collections.Generic;

namespace Oculus.Interaction
{
    public struct InteractableStateChangeArgs
    {
        public InteractableState PreviousState;
        public InteractableState NewState;
    }

    /// <summary>
    /// An IInteractableView defines the view for an object that can be
    /// interacted with.
    /// </summary>
    public interface IInteractableView
    {
        InteractableState State { get; }
        event Action<InteractableStateChangeArgs> WhenStateChanged;

        int MaxInteractors { get; }
        int MaxSelectingInteractors { get; }

        IEnumerable<IInteractorView> InteractorViews { get; }
        IEnumerable<IInteractorView> SelectingInteractorViews { get; }

        event Action<IInteractorView> WhenInteractorViewAdded;
        event Action<IInteractorView> WhenInteractorViewRemoved;
        event Action<IInteractorView> WhenSelectingInteractorViewAdded;
        event Action<IInteractorView> WhenSelectingInteractorViewRemoved;
    }

    /// <summary>
    /// An object that can be interacted with, an IInteractable can, in addition to
    /// an IInteractableView, be enabled or disabled.
    /// </summary>
    public interface IInteractable : IInteractableView
    {
        void Enable();
        void Disable();
        new int MaxInteractors { get; set; }
        new int MaxSelectingInteractors { get; set; }
        void RemoveInteractorByIdentifier(int id);
    }
}
