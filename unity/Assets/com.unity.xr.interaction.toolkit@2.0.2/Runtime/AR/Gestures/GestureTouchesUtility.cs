//-----------------------------------------------------------------------
// <copyright file="GestureTouchesUtility.cs" company="Google">
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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    class MockTouch
    {
        public float deltaTime
        {
            get => ((Touch)m_Touch).deltaTime;
            set => s_Fields["m_TimeDelta"].SetValue(m_Touch, value);
        }

        public int tapCount
        {
            get => ((Touch)m_Touch).tapCount;
            set => s_Fields["m_TapCount"].SetValue(m_Touch, value);
        }

        public TouchPhase phase
        {
            get => ((Touch)m_Touch).phase;
            set => s_Fields["m_Phase"].SetValue(m_Touch, value);
        }

        public Vector2 deltaPosition
        {
            get => ((Touch)m_Touch).deltaPosition;
            set => s_Fields["m_PositionDelta"].SetValue(m_Touch, value);
        }

        public int fingerId
        {
            get => ((Touch)m_Touch).fingerId;
            set => s_Fields["m_FingerId"].SetValue(m_Touch, value);
        }

        public Vector2 position
        {
            get => ((Touch)m_Touch).position;
            set => s_Fields["m_Position"].SetValue(m_Touch, value);
        }

        public Vector2 rawPosition
        {
            get => ((Touch)m_Touch).rawPosition;
            set => s_Fields["m_RawPosition"].SetValue(m_Touch, value);
        }

        static readonly Dictionary<string, FieldInfo> s_Fields = new Dictionary<string, FieldInfo>();
        readonly object m_Touch = new Touch();

        static MockTouch()
        {
            foreach (var f in typeof(Touch).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                s_Fields.Add(f.Name, f);
        }

        public Touch ToTouch() => (Touch)m_Touch;
    }

    /// <summary>
    /// An adapter struct that wraps a <c>Touch</c> from either the Input System package or the Input Manager.
    /// </summary>
    /// <seealso cref="Touch"/>
    /// <seealso cref="InputSystem.EnhancedTouch.Touch"/>
    readonly struct CommonTouch
    {
        public float deltaTime => m_IsEnhancedTouch ? (float)(m_EnhancedTouch.time - m_EnhancedTouch.startTime) : m_Touch.deltaTime;

        public int tapCount => m_IsEnhancedTouch ? m_EnhancedTouch.tapCount : m_Touch.tapCount;

        public bool isPhaseBegan => m_IsEnhancedTouch ? m_EnhancedTouch.phase == InputSystem.TouchPhase.Began : m_Touch.phase == TouchPhase.Began;
        public bool isPhaseMoved => m_IsEnhancedTouch ? m_EnhancedTouch.phase == InputSystem.TouchPhase.Moved : m_Touch.phase == TouchPhase.Moved;
        public bool isPhaseStationary => m_IsEnhancedTouch ? m_EnhancedTouch.phase == InputSystem.TouchPhase.Stationary : m_Touch.phase == TouchPhase.Stationary;
        public bool isPhaseEnded => m_IsEnhancedTouch ? m_EnhancedTouch.phase == InputSystem.TouchPhase.Ended : m_Touch.phase == TouchPhase.Ended;
        public bool isPhaseCanceled => m_IsEnhancedTouch ? m_EnhancedTouch.phase == InputSystem.TouchPhase.Canceled : m_Touch.phase == TouchPhase.Canceled;

        public Vector2 deltaPosition => m_IsEnhancedTouch ? m_EnhancedTouch.delta : m_Touch.deltaPosition;

        /// <summary>
        /// Unique ID of the touch used to identify it across multiple frames.
        /// Not to be confused with <see cref="InputSystem.EnhancedTouch.Finger.index"/>.
        /// </summary>
        /// <seealso cref="Touch.fingerId"/>
        /// <seealso cref="InputSystem.EnhancedTouch.Touch.touchId"/>
        public int fingerId => m_IsEnhancedTouch ? m_EnhancedTouch.touchId : m_Touch.fingerId;

        public Vector2 position => m_IsEnhancedTouch ? m_EnhancedTouch.screenPosition : m_Touch.position;

        readonly Touch m_Touch;
        readonly InputSystem.EnhancedTouch.Touch m_EnhancedTouch;
        readonly bool m_IsEnhancedTouch;

        public CommonTouch(Touch touch)
        {
            m_Touch = touch;
            m_EnhancedTouch = default;
            m_IsEnhancedTouch = false;
        }

        public CommonTouch(InputSystem.EnhancedTouch.Touch touch)
        {
            m_Touch = default;
            m_EnhancedTouch = touch;
            m_IsEnhancedTouch = true;
        }

        /// <summary>
        /// Gets the <see cref="Touch"/> if constructed with <see cref="CommonTouch(Touch)"/>.
        /// </summary>
        /// <returns>Returns the <see cref="Touch"/> used to construct this object. Otherwise, throws an exception.</returns>
        /// <exception cref="InvalidOperationException">Throws when this object was constructed with an Input System <see cref="InputSystem.EnhancedTouch.Touch"/>.</exception>
        public Touch GetTouch() =>
            !m_IsEnhancedTouch
                ? m_Touch
                : throw new InvalidOperationException($"Cannot convert to {typeof(Touch).FullName} since this was sourced from the Input System package.");

        /// <summary>
        /// Gets the <see cref="InputSystem.EnhancedTouch.Touch"/> if constructed with <see cref="CommonTouch(InputSystem.EnhancedTouch.Touch)"/>.
        /// </summary>
        /// <returns>Returns the <see cref="InputSystem.EnhancedTouch.Touch"/> used to construct this object. Otherwise, throws an exception.</returns>
        /// <exception cref="InvalidOperationException">Throws when this object was constructed with an Input Manager <see cref="Touch"/>.</exception>
        public InputSystem.EnhancedTouch.Touch GetEnhancedTouch() =>
            m_IsEnhancedTouch
                ? m_EnhancedTouch
                : throw new InvalidOperationException($"Cannot convert to {typeof(InputSystem.EnhancedTouch.Touch).FullName} since this was sourced from Input Manager Input.");
    }

    /// <summary>
    /// Singleton used by Gestures and GestureRecognizers to interact with touch input.
    ///
    /// 1. Makes it easy to find touches by fingerId.
    /// 2. Allows Gestures to Lock/Release fingerIds.
    /// 3. Wraps Input.Touches so that it works both in editor and on device.
    /// 4. Provides helper functions for converting touch coordinates
    ///    and performing ray casts based on touches.
    /// </summary>
    static class GestureTouchesUtility
    {
        public enum TouchInputSource
        {
            Legacy,
            Enhanced,
            Mock,
        }

        const float k_EdgeThresholdInches = 0.1f;

        /// <summary>
        /// The default source of <c>Touch</c> input this class uses.
        /// </summary>
        /// <remarks>
        /// Defaults to use legacy Input Manager Touch input when the <b>Active Input Handling</b> mode of the Unity project
        /// is set to <b>Both</b> for backwards compatibility with existing projects.
        /// </remarks>
        public static readonly TouchInputSource defaultTouchInputSource =
#if ENABLE_LEGACY_INPUT_MANAGER
            TouchInputSource.Legacy;
#else
            TouchInputSource.Enhanced;
#endif

        public static TouchInputSource touchInputSource { get; set; } = defaultTouchInputSource;

        /// <summary>
        /// The list of the status of all touches.
        /// Does not return a copy in order to avoid allocation.
        /// </summary>
        public static List<CommonTouch> touches
        {
            get
            {
                s_Touches.Clear();

                switch (touchInputSource)
                {
                    case TouchInputSource.Legacy:
                        for (int index = 0, touchCount = Input.touchCount; index < touchCount; ++index)
                            s_Touches.Add(new CommonTouch(Input.GetTouch(index)));
                        break;
                    case TouchInputSource.Enhanced:
                        // ReSharper disable once ForCanBeConvertedToForeach -- Would produce garbage, ReadOnlyArray does not use a struct for the enumerator
                        for (var index = 0; index < InputSystem.EnhancedTouch.Touch.activeTouches.Count; ++index)
                        {
                            var touch = InputSystem.EnhancedTouch.Touch.activeTouches[index];
                            s_Touches.Add(new CommonTouch(touch));
                        }
                        break;
                    case TouchInputSource.Mock:
                        foreach (var touch in mockTouches)
                            s_Touches.Add(new CommonTouch(touch.ToTouch()));
                        break;
                }

                return s_Touches;
            }
        }

        static readonly List<CommonTouch> s_Touches = new List<CommonTouch>();
        internal static readonly List<MockTouch> mockTouches = new List<MockTouch>();
        static readonly HashSet<int> s_RetainedFingerIds = new HashSet<int>();

        /// <summary>
        /// Try to find a touch for a particular finger id.
        /// </summary>
        /// <param name="fingerId">The finger id to find the touch.</param>
        /// <param name="touchOut">The output touch.</param>
        /// <returns>True if a touch was found.</returns>
        public static bool TryFindTouch(int fingerId, out CommonTouch touchOut)
        {
            foreach (var touch in touches)
            {
                if (touch.fingerId == fingerId)
                {
                    touchOut = touch;
                    return true;
                }
            }

            touchOut = default;
            return false;
        }

        /// <summary>
        /// Converts Pixels to Inches.
        /// </summary>
        /// <param name="pixels">The amount to convert in pixels.</param>
        /// <returns>The converted amount in inches.</returns>
        public static float PixelsToInches(float pixels) => pixels / Screen.dpi;

        /// <summary>
        /// Converts Inches to Pixels.
        /// </summary>
        /// <param name="inches">The amount to convert in inches.</param>
        /// <returns>The converted amount in pixels.</returns>
        public static float InchesToPixels(float inches) => inches * Screen.dpi;

        /// <summary>
        /// Used to determine if a touch is off the edge of the screen based on some slop.
        /// Useful to prevent accidental touches from simply holding the device from causing
        /// confusing behavior.
        /// </summary>
        /// <param name="touch">The touch to check.</param>
        /// <returns>True if the touch is off screen edge.</returns>
        public static bool IsTouchOffScreenEdge(CommonTouch touch)
        {
            var slopPixels = InchesToPixels(k_EdgeThresholdInches);

            var result = touch.position.x <= slopPixels;
            result |= touch.position.y <= slopPixels;
            result |= touch.position.x >= Screen.width - slopPixels;
            result |= touch.position.y >= Screen.height - slopPixels;

            return result;
        }

        /// <summary>
        /// Performs a Raycast from the camera.
        /// </summary>
        /// <param name="screenPos">The screen position to perform the ray cast from.</param>
        /// <param name="sessionOrigin">The <see cref="XROrigin"/> whose Camera is used for ray casting.</param>
        /// <param name="arSessionOrigin">The fallback <see cref="ARSessionOrigin"/> whose Camera is used for ray casting.</param>
        /// <param name="result">When this method returns, contains the <see cref="RaycastHit"/> result.</param>
        /// <returns>Returns <see langword="true"/> if an object was hit. Otherwise, returns <see langword="false"/>.</returns>
        public static bool RaycastFromCamera(Vector2 screenPos, XROrigin sessionOrigin, ARSessionOrigin arSessionOrigin, out RaycastHit result)
        {
            // The ARSessionOrigin parameter will eventually be removed. This class is internal so no need for overloaded.
            var camera = sessionOrigin != null
                ? sessionOrigin.Camera
                : (arSessionOrigin != null ? arSessionOrigin.camera : Camera.main);
            if (camera == null)
            {
                result = default;
                return false;
            }

            var ray = camera.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out result);
        }

        /// <summary>
        /// Locks a finger Id.
        /// </summary>
        /// <param name="fingerId">The finger id to lock.</param>
        public static void LockFingerId(int fingerId)
        {
            s_RetainedFingerIds.Add(fingerId);
        }

        /// <summary>
        /// Releases a finger Id.
        /// </summary>
        /// <param name="fingerId">The finger id to release.</param>
        public static void ReleaseFingerId(int fingerId)
        {
            s_RetainedFingerIds.Remove(fingerId);
        }

        /// <summary>
        /// Returns true if the finger Id is retained.
        /// </summary>
        /// <param name="fingerId">The finger id to check.</param>
        /// <returns>Returns <see langword="true"/> if the finger is retained. Otherwise, returns <see langword="false"/>.</returns>
        public static bool IsFingerIdRetained(int fingerId)
        {
            return s_RetainedFingerIds.Contains(fingerId);
        }
    }
}

#endif
