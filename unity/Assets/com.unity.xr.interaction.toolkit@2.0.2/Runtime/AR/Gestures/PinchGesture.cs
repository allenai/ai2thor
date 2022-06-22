//-----------------------------------------------------------------------
// <copyright file="PinchGesture.cs" company="Google">
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
    /// Gesture for when the user performs a two-finger pinch motion on the touch screen.
    /// </summary>
    public class PinchGesture : Gesture<PinchGesture>
    {
        /// <summary>
        /// Initializes and returns an instance of <see cref="PinchGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        public PinchGesture(PinchGestureRecognizer recognizer, Touch touch1, Touch touch2)
            : this(recognizer, new CommonTouch(touch1), new CommonTouch(touch2))
        {
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="PinchGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        public PinchGesture(PinchGestureRecognizer recognizer, InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
            : this(recognizer, new CommonTouch(touch1), new CommonTouch(touch2))
        {
        }

        PinchGesture(PinchGestureRecognizer recognizer, CommonTouch touch1, CommonTouch touch2) : base(recognizer)
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
            gap = 0f;
            gapDelta = 0f;
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
        /// (Read Only) The gap between then position of the first and second fingers.
        /// </summary>
        public float gap { get; private set; }

        /// <summary>
        /// (Read Only) The gap delta between then position of the first and second fingers.
        /// </summary>
        public float gapDelta { get; private set; }

        /// <summary>
        /// (Read Only) The gesture recognizer.
        /// </summary>
        protected PinchGestureRecognizer pinchRecognizer => (PinchGestureRecognizer)recognizer;

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

            // Check that at least one finger is moving.
            if (touch1.deltaPosition == Vector2.zero && touch2.deltaPosition == Vector2.zero)
            {
                return false;
            }

            Vector3 firstToSecondDirection = (startPosition1 - startPosition2).normalized;
            var dot1 = Vector3.Dot(touch1.deltaPosition.normalized, -firstToSecondDirection);
            var dot2 = Vector3.Dot(touch2.deltaPosition.normalized, firstToSecondDirection);
            var dotThreshold = Mathf.Cos(pinchRecognizer.slopMotionDirectionDegrees * Mathf.Deg2Rad);

            // Check angle of motion for the first touch.
            if (touch1.deltaPosition != Vector2.zero && Mathf.Abs(dot1) < dotThreshold)
            {
                return false;
            }

            // Check angle of motion for the second touch.
            if (touch2.deltaPosition != Vector2.zero && Mathf.Abs(dot2) < dotThreshold)
            {
                return false;
            }

            var startgap = (startPosition1 - startPosition2).magnitude;
            gap = (touch1.position - touch2.position).magnitude;
            var separation = GestureTouchesUtility.PixelsToInches(Mathf.Abs(gap - startgap));
            return separation >= pinchRecognizer.slopInches;
        }

        /// <inheritdoc />
        protected internal override void OnStart()
        {
            GestureTouchesUtility.LockFingerId(fingerId1);
            GestureTouchesUtility.LockFingerId(fingerId2);
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
                float newgap = (touch1.position - touch2.position).magnitude;
                gapDelta = newgap - gap;
                gap = newgap;
                return true;
            }

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
    }
}

#endif
