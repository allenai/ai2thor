//-----------------------------------------------------------------------
// <copyright file="TwoFingerDragGestureRecognizer.cs" company="Google">
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
    /// Gesture Recognizer for when the user performs a two finger drag motion on the touch screen.
    /// </summary>
    /// <inheritdoc />
    public class TwoFingerDragGestureRecognizer : GestureRecognizer<TwoFingerDragGesture>
    {
        /// <summary>
        /// Distance in inches a user's touches can drift from the start position
        /// before the drag gesture is interpreted as started.
        /// </summary>
        public float slopInches { get; set; } = 0.1f;

        /// <summary>
        /// Maximum angle of the divergence between the paths of both fingers
        /// for a two-finger drag gesture to be interpreted as started.
        /// </summary>
        public float angleThresholdRadians { get; set; } = Mathf.PI / 6;

        // Preallocate delegates to avoid GC Alloc that would happen in TryCreateGestures
        readonly Func<InputSystem.EnhancedTouch.Touch, InputSystem.EnhancedTouch.Touch, TwoFingerDragGesture> m_CreateEnhancedGesture;
        readonly Func<Touch, Touch, TwoFingerDragGesture> m_CreateGestureFunction;
        readonly Action<TwoFingerDragGesture, InputSystem.EnhancedTouch.Touch, InputSystem.EnhancedTouch.Touch> m_ReinitializeEnhancedGesture;
        readonly Action<TwoFingerDragGesture, Touch, Touch> m_ReinitializeGestureFunction;

        /// <summary>
        /// Initializes and returns an instance of <see cref="TwoFingerDragGestureRecognizer"/>.
        /// </summary>
        public TwoFingerDragGestureRecognizer()
        {
            m_CreateEnhancedGesture = CreateEnhancedGesture;
            m_CreateGestureFunction = CreateGesture;
            m_ReinitializeEnhancedGesture = ReinitializeEnhancedGesture;
            m_ReinitializeGestureFunction = ReinitializeGesture;
        }

        /// <summary>
        /// Creates a two finger drag gesture with the given touches.
        /// </summary>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        /// <returns>The created Two Finger Drag gesture.</returns>
        internal TwoFingerDragGesture CreateGesture(Touch touch1, Touch touch2)
        {
            return new TwoFingerDragGesture(this, touch1, touch2);
        }

        static void ReinitializeGesture(TwoFingerDragGesture gesture, Touch touch1, Touch touch2)
        {
            gesture.Reinitialize(touch1, touch2);
        }

        /// <summary>
        /// Creates a two finger drag gesture with the given touches.
        /// </summary>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        /// <returns>The created Two Finger Drag gesture.</returns>
        internal TwoFingerDragGesture CreateEnhancedGesture(InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
        {
            return new TwoFingerDragGesture(this, touch1, touch2);
        }

        static void ReinitializeEnhancedGesture(TwoFingerDragGesture gesture, InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
        {
            gesture.Reinitialize(touch1, touch2);
        }

        /// <inheritdoc />
        protected override void TryCreateGestures()
        {
            if (GestureTouchesUtility.touchInputSource == GestureTouchesUtility.TouchInputSource.Enhanced)
                TryCreateTwoFingerGestureOnTouchBegan(m_CreateEnhancedGesture, m_ReinitializeEnhancedGesture);
            else
                TryCreateTwoFingerGestureOnTouchBegan(m_CreateGestureFunction, m_ReinitializeGestureFunction);
        }
    }
}

#endif
