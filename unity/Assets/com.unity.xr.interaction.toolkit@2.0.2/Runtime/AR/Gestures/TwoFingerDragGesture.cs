//-----------------------------------------------------------------------
// <copyright file="TwoFingerDragGesture.cs" company="Google">
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

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Gesture for when the user performs a two finger vertical swipe motion on the touch screen.
    /// </summary>
    public partial class TwoFingerDragGesture : Gesture<TwoFingerDragGesture>
    {
        /// <summary>
        /// Initializes and returns an instance of <see cref="TwoFingerDragGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        public TwoFingerDragGesture(TwoFingerDragGestureRecognizer recognizer, Touch touch1, Touch touch2)
            : this(recognizer, new CommonTouch(touch1), new CommonTouch(touch2))
        {
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="TwoFingerDragGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch1">The first touch that started this gesture.</param>
        /// <param name="touch2">The second touch that started this gesture.</param>
        public TwoFingerDragGesture(TwoFingerDragGestureRecognizer recognizer, InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2)
            : this(recognizer, new CommonTouch(touch1), new CommonTouch(touch2))
        {
        }

        TwoFingerDragGesture(TwoFingerDragGestureRecognizer recognizer, CommonTouch touch1, CommonTouch touch2)
            : base(recognizer)
        {
            Reinitialize(touch1, touch2);
        }

        internal void Reinitialize(Touch touch1, Touch touch2) => Reinitialize(new CommonTouch(touch1), new CommonTouch(touch2));
        internal void Reinitialize(InputSystem.EnhancedTouch.Touch touch1, InputSystem.EnhancedTouch.Touch touch2) => Reinitialize(new CommonTouch(touch1), new CommonTouch(touch2));

        void Reinitialize(CommonTouch touch1, CommonTouch touch2)
        {
            Reinitialize();
            fingerId1 = touch1.fingerId;
            startPosition1 = touch1.position;
            fingerId2 = touch2.fingerId;
            startPosition2 = touch2.position;
            position = (startPosition1 + startPosition2) / 2;
            delta = Vector2.zero;
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
        /// (Read Only) The current screen position of the gesture.
        /// </summary>
        public Vector2 position { get; private set; }

        /// <summary>
        /// (Read Only) The delta screen position of the gesture.
        /// </summary>
        public Vector2 delta { get; private set; }

        /// <summary>
        /// (Read Only) The gesture recognizer.
        /// </summary>
        protected TwoFingerDragGestureRecognizer dragRecognizer => (TwoFingerDragGestureRecognizer)recognizer;

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

            var pos1 = touch1.position;
            var diff1 = (pos1 - startPosition1).magnitude;
            var pos2 = touch2.position;
            var diff2 = (pos2 - startPosition2).magnitude;
            var slopInches = dragRecognizer.slopInches;
            if (GestureTouchesUtility.PixelsToInches(diff1) < slopInches ||
                GestureTouchesUtility.PixelsToInches(diff2) < slopInches)
            {
                return false;
            }

            // Check both fingers move in the same direction.
            var dot = Vector3.Dot(touch1.deltaPosition.normalized, touch2.deltaPosition.normalized);
            return dot >= Mathf.Cos(dragRecognizer.angleThresholdRadians);
        }

        /// <inheritdoc />
        protected internal override void OnStart()
        {
            GestureTouchesUtility.LockFingerId(fingerId1);
            GestureTouchesUtility.LockFingerId(fingerId2);

#pragma warning disable 618 // Using deprecated property to help with backwards compatibility.
            if (GestureTouchesUtility.RaycastFromCamera(startPosition1, recognizer.xrOrigin, recognizer.arSessionOrigin, out var hit1))
#pragma warning restore 618
            {
                var gameObject = hit1.transform.gameObject;
                var interactableObject = gameObject.GetComponentInParent<ARBaseGestureInteractable>();
                if (interactableObject != null)
                    targetObject = interactableObject.gameObject;
            }
#pragma warning disable 618 // Using deprecated property to help with backwards compatibility.
            else if (GestureTouchesUtility.RaycastFromCamera(startPosition2, recognizer.xrOrigin, recognizer.arSessionOrigin, out var hit2))
#pragma warning restore 618
            {
                var gameObject = hit2.transform.gameObject;
                var interactableObject = gameObject.GetComponentInParent<ARBaseGestureInteractable>();
                if (interactableObject != null)
                    targetObject = interactableObject.gameObject;
            }

            GestureTouchesUtility.TryFindTouch(fingerId1, out var touch1);
            GestureTouchesUtility.TryFindTouch(fingerId2, out var touch2);
            position = (touch1.position + touch2.position) / 2;
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
                delta = ((touch1.position + touch2.position) / 2) - position;
                position = (touch1.position + touch2.position) / 2;
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
