//-----------------------------------------------------------------------
// <copyright file="DragGestureRecognizer.cs" company="Google">
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
using UnityEngine;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Gesture Recognizer for when the user performs a drag motion on the touch screen.
    /// </summary>
    /// <inheritdoc />
    public class DragGestureRecognizer : GestureRecognizer<DragGesture>
    {
        /// <summary>
        /// Distance in inches a user's touch can drift from the start position
        /// before the drag gesture is interpreted as started.
        /// </summary>
        public float slopInches { get; set; } = 0.1f;

        // Preallocate delegates to avoid GC Alloc that would happen in TryCreateGestures
        readonly Func<InputSystem.EnhancedTouch.Touch, DragGesture> m_CreateEnhancedGesture;
        readonly Func<Touch, DragGesture> m_CreateGestureFunction;
        readonly Action<DragGesture, InputSystem.EnhancedTouch.Touch> m_ReinitializeEnhancedGesture;
        readonly Action<DragGesture, Touch> m_ReinitializeGestureFunction;

        /// <summary>
        /// Initializes and returns an instance of <see cref="DragGestureRecognizer"/>.
        /// </summary>
        public DragGestureRecognizer()
        {
            m_CreateEnhancedGesture = CreateEnhancedGesture;
            m_CreateGestureFunction = CreateGesture;
            m_ReinitializeEnhancedGesture = ReinitializeEnhancedGesture;
            m_ReinitializeGestureFunction = ReinitializeGesture;
        }

        /// <summary>
        /// Creates a Drag gesture with the given touch.
        /// </summary>
        /// <param name="touch">The touch that started this gesture.</param>
        /// <returns>Returns the created Drag gesture.</returns>
        internal DragGesture CreateGesture(Touch touch)
        {
            return new DragGesture(this, touch);
        }

        static void ReinitializeGesture(DragGesture gesture, Touch touch)
        {
            gesture.Reinitialize(touch);
        }

        /// <summary>
        /// Creates a Drag gesture with the given touch.
        /// </summary>
        /// <param name="touch">The touch that started this gesture.</param>
        /// <returns>Returns the created Drag gesture.</returns>
        internal DragGesture CreateEnhancedGesture(InputSystem.EnhancedTouch.Touch touch)
        {
            return new DragGesture(this, touch);
        }

        static void ReinitializeEnhancedGesture(DragGesture gesture, InputSystem.EnhancedTouch.Touch touch)
        {
            gesture.Reinitialize(touch);
        }

        /// <inheritdoc />
        protected override void TryCreateGestures()
        {
            if (GestureTouchesUtility.touchInputSource == GestureTouchesUtility.TouchInputSource.Enhanced)
                TryCreateOneFingerGestureOnTouchBegan(m_CreateEnhancedGesture, m_ReinitializeEnhancedGesture);
            else
                TryCreateOneFingerGestureOnTouchBegan(m_CreateGestureFunction, m_ReinitializeGestureFunction);
        }
    }
}

#endif
