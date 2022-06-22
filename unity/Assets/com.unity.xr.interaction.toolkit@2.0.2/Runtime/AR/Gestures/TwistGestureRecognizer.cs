//-----------------------------------------------------------------------
// <copyright file="TwistGestureRecognizer.cs" company="Google">
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
    /// Gesture Recognizer for when the user performs a two-finger twist motion on the touch screen.
    /// </summary>
    /// <inheritdoc />
    public class TwistGestureRecognizer : GestureRecognizer<TwistGesture>
    {
        /// <summary>
        /// Rotation in degrees a user's touches must twist from the start positions
        /// before the twist gesture is interpreted as started.
        /// </summary>
        public float slopRotation { get; set; } = 10f;

        // Preallocate delegates to avoid GC Alloc that would happen in TryCreateGestures
        readonly Func<InputSystem.EnhancedTouch.Touch, InputSystem.EnhancedTouch.Touch, TwistGesture> m_CreateEnhancedGesture;
        readonly Func<Touch, Touch, TwistGesture> m_CreateGestureFunction;
        readonly Action<TwistGesture, InputSystem.EnhancedTouch.Touch, InputSystem.EnhancedTouch.Touch> m_ReinitializeEnhancedGesture;
        readonly Action<TwistGesture, Touch, Touch> m_ReinitializeGestureFunction;

        /// <summary>
        /// Initializes and returns an instance of <see cref="TwistGestureRecognizer"/>.
        /// </summary>
        public TwistGestureRecognizer()
        {
            m_CreateEnhancedGesture = CreateEnhancedGesture;
            m_CreateGestureFunction = CreateGesture;
            m_ReinitializeEnhancedGesture = ReinitializeEnhancedGesture;
            m_ReinitializeGestureFunction = ReinitializeGesture;
        }

        /// <summary>
        /// Creates a Twist gesture with the given touches.
        /// </summary>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        /// <returns>The created Twist gesture.</returns>
        internal TwistGesture CreateGesture(Touch touch1, Touch touch2)
        {
            return new TwistGesture(this, touch1, touch2);
        }

        static void ReinitializeGesture(TwistGesture gesture, Touch touch1, Touch touch2)
        {
            gesture.Reinitialize(touch1, touch2);
        }

        /// <summary>
        /// Creates a Twist gesture with the given touches.
        /// </summary>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        /// <returns>The created Twist gesture.</returns>
        internal TwistGesture CreateEnhancedGesture(InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
        {
            return new TwistGesture(this, touch1, touch2);
        }

        static void ReinitializeEnhancedGesture(TwistGesture gesture, InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
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
