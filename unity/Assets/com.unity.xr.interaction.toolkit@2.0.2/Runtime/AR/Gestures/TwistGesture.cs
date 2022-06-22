//-----------------------------------------------------------------------
// <copyright file="TwistGesture.cs" company="Google">
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

using UnityEngine;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Gesture for when the user performs a two-finger twist motion on the touch screen.
    /// </summary>
    public class TwistGesture : Gesture<TwistGesture>
    {
        Vector2 m_PreviousPosition1;
        Vector2 m_PreviousPosition2;

        /// <summary>
        /// Initializes and returns an instance of <see cref="TwistGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        public TwistGesture(TwistGestureRecognizer recognizer, Touch touch1, Touch touch2)
            : this(recognizer, new CommonTouch(touch1), new CommonTouch(touch2))
        {
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="TwistGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        public TwistGesture(TwistGestureRecognizer recognizer, InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
            : this(recognizer, new CommonTouch(touch1), new CommonTouch(touch2))
        {
        }

        TwistGesture(TwistGestureRecognizer recognizer, CommonTouch touch1, CommonTouch touch2) : base(recognizer)
        {
            Reinitialize(touch1, touch2);
        }

        internal void Reinitialize(Touch touch1, Touch touch2) => Reinitialize(new CommonTouch(touch1), new CommonTouch(touch2));
        internal void Reinitialize(InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2) => Reinitialize(new CommonTouch(touch1), new CommonTouch(touch2));

        void Reinitialize(CommonTouch touch1, CommonTouch touch2)
        {
            Reinitialize();
            fingerId1 = touch1.fingerId;
            fingerId2 = touch2.fingerId;
            startPosition1 = touch1.position;
            startPosition2 = touch2.position;
            m_PreviousPosition1 = Vector2.zero;
            m_PreviousPosition2 = Vector2.zero;
            deltaRotation = 0f;
        }

        /// <summary>
        /// (Read Only) The id of the first finger used in this gesture.
        /// </summary>
        public int fingerId1 { get; private set; }

        /// <summary>
        /// (Read Only) The id of the second finger used in this gesture.
        /// </summary>
        public int fingerId2 { get; private set; }

        /// <summary>
        /// (Read Only) The screen position of the first finger where the gesture started.
        /// </summary>
        public Vector2 startPosition1 { get; private set; }

        /// <summary>
        /// (Read Only) The screen position of the second finger where the gesture started.
        /// </summary>
        public Vector2 startPosition2 { get; private set; }

        /// <summary>
        /// (Read Only) The delta rotation of the gesture.
        /// </summary>
        public float deltaRotation { get; private set; }

        /// <summary>
        /// (Read Only) The gesture recognizer.
        /// </summary>
        protected TwistGestureRecognizer twistRecognizer => (TwistGestureRecognizer)recognizer;

        /// <inheritdoc />
        protected internal override bool CanStart()
        {
            if (GestureTouchesUtility.IsFingerIdRetained(fingerId1) ||
                GestureTouchesUtility.IsFingerIdRetained(fingerId2))
            {
                Cancel();
                return false;
            }

            var foundTouches = GestureTouchesUtility.TryFindTouch(fingerId1, out var touch1);
            foundTouches =
                GestureTouchesUtility.TryFindTouch(fingerId2, out var touch2) && foundTouches;

            if (!foundTouches)
            {
                Cancel();
                return false;
            }

            // Check that both fingers are moving.
            if (touch1.deltaPosition == Vector2.zero || touch2.deltaPosition == Vector2.zero)
            {
                return false;
            }

            var rotation = CalculateDeltaRotation(
                touch1.position, touch2.position, startPosition1, startPosition2);
            return Mathf.Abs(rotation) >= twistRecognizer.slopRotation;
        }

        /// <inheritdoc />
        protected internal override void OnStart()
        {
            GestureTouchesUtility.LockFingerId(fingerId1);
            GestureTouchesUtility.LockFingerId(fingerId2);

            GestureTouchesUtility.TryFindTouch(fingerId1, out var touch1);
            GestureTouchesUtility.TryFindTouch(fingerId2, out var touch2);
            m_PreviousPosition1 = touch1.position;
            m_PreviousPosition2 = touch2.position;
        }

        /// <inheritdoc />
        protected internal override bool UpdateGesture()
        {
            var foundTouches = GestureTouchesUtility.TryFindTouch(fingerId1, out var touch1);
            foundTouches =
                GestureTouchesUtility.TryFindTouch(fingerId2, out var touch2) && foundTouches;

            if (!foundTouches)
            {
                Cancel();
                return false;
            }

            if (touch1.isPhaseCanceled || touch2.isPhaseCanceled)
            {
                Cancel();
                return false;
            }

            if (touch1.isPhaseEnded || touch2.isPhaseEnded)
            {
                Complete();
                return false;
            }

            if (touch1.isPhaseMoved || touch2.isPhaseMoved)
            {
                float rotation = CalculateDeltaRotation(
                                     touch1.position,
                                     touch2.position,
                                     m_PreviousPosition1,
                                     m_PreviousPosition2);

                deltaRotation = rotation;
                m_PreviousPosition1 = touch1.position;
                m_PreviousPosition2 = touch2.position;
                return true;
            }

            m_PreviousPosition1 = touch1.position;
            m_PreviousPosition2 = touch2.position;
            deltaRotation = 0f;
            return false;
        }

        /// <inheritdoc />
        protected internal override void OnCancel()
        {
        }

        /// <inheritdoc />
        protected internal override void OnFinish()
        {
            GestureTouchesUtility.ReleaseFingerId(fingerId1);
            GestureTouchesUtility.ReleaseFingerId(fingerId2);
        }

        /// <summary>
        /// Calculates a signed angle for how much to twist according to the movement of two touch positions.
        /// </summary>
        /// <param name="currentPosition1">Current position of the first touch.</param>
        /// <param name="currentPosition2">Current position of the second touch.</param>
        /// <param name="previousPosition1">Previous position of the first touch.</param>
        /// <param name="previousPosition2">Previous position of the second touch.</param>
        /// <returns>A signed angle, in degrees, representing how much to rotate an interactable by according to the changes in the two positions passed in.</returns>
        protected static float CalculateDeltaRotation(
            Vector2 currentPosition1,
            Vector2 currentPosition2,
            Vector2 previousPosition1,
            Vector2 previousPosition2)
        {
            var currentDirection = (currentPosition1 - currentPosition2).normalized;
            var previousDirection = (previousPosition1 - previousPosition2).normalized;

            var sign = Mathf.Sign((previousDirection.x * currentDirection.y) -
                                    (previousDirection.y * currentDirection.x));
            return Vector2.Angle(currentDirection, previousDirection) * sign;
        }
    }
}

#endif
