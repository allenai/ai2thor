//-----------------------------------------------------------------------
// <copyright file="TapGestureRecognizer.cs" company="Google">
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
    /// Gesture Recognizer for when the user performs a tap on the touch screen.
    /// </summary>
    /// <inheritdoc />
    public class TapGestureRecognizer : GestureRecognizer<TapGesture>
    {
        /// <summary>
        /// Distance in inches a user's touch can drift from the start position
        /// before the tap gesture is canceled.
        /// </summary>
        public float slopInches { get; set; } = 0.1f;

        /// <summary>
        /// Time (in seconds) within which a touch and release has to occur for it
        /// to be registered as a tap.
        /// </summary>
        public float durationSeconds { get; set; } = 0.3f;

        // Preallocate delegates to avoid GC Alloc that would happen in TryCreateGestures
        readonly Func<InputSystem.EnhancedTouch.Touch, TapGesture> m_CreateEnhancedGesture;
        readonly Func<Touch, TapGesture> m_CreateGestureFunction;
        readonly Action<TapGesture, InputSystem.EnhancedTouch.Touch> m_ReinitializeEnhancedGesture;
        readonly Action<TapGesture, Touch> m_ReinitializeGestureFunction;

        /// <summary>
        /// Initializes and returns an instance of <see cref="TapGestureRecognizer"/>.
        /// </summary>
        public TapGestureRecognizer()
        {
            m_CreateEnhancedGesture = CreateEnhancedGesture;
            m_CreateGestureFunction = CreateGesture;
            m_ReinitializeEnhancedGesture = ReinitializeEnhancedGesture;
            m_ReinitializeGestureFunction = ReinitializeGesture;
        }

        /// <summary>
        /// Creates a Tap gesture with the given touch.
        /// </summary>
        /// <param name="touch">The touch that started this gesture.</param>
        /// <returns>The created Tap gesture.</returns>
        internal TapGesture CreateGesture(Touch touch)
        {
            return new TapGesture(this, touch);
        }

        static void ReinitializeGesture(TapGesture gesture, Touch touch)
        {
            gesture.Reinitialize(touch);
        }

        /// <summary>
        /// Creates a Tap gesture with the given touch.
        /// </summary>
        /// <param name="touch">The touch that started this gesture.</param>
        /// <returns>The created Tap gesture.</returns>
        internal TapGesture CreateEnhancedGesture(InputSystem.EnhancedTouch.Touch touch)
        {
            return new TapGesture(this, touch);
        }

        static void ReinitializeEnhancedGesture(TapGesture gesture, InputSystem.EnhancedTouch.Touch touch)
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
