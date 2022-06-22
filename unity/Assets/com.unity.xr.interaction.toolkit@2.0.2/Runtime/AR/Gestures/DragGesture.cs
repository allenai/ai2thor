//-----------------------------------------------------------------------
// <copyright file="DragGesture.cs" company="Google">
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
    /// Gesture for when the user performs a drag motion on the touch screen.
    /// </summary>
    public partial class DragGesture : Gesture<DragGesture>
    {
        /// <summary>
        /// Initializes and returns an instance of <see cref="DragGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch">The touch that started this gesture.</param>
        public DragGesture(DragGestureRecognizer recognizer, Touch touch)
            : this(recognizer, new CommonTouch(touch))
        {
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="DragGesture"/>.
        /// </summary>
        /// <param name="recognizer">The gesture recognizer.</param>
        /// <param name="touch">The touch that started this gesture.</param>
        public DragGesture(DragGestureRecognizer recognizer, InputSystem.EnhancedTouch.Touch touch)
            : this(recognizer, new CommonTouch(touch))
        {
        }

        DragGesture(DragGestureRecognizer recognizer, CommonTouch touch) : base(recognizer)
        {
            Reinitialize(touch);
        }

        internal void Reinitialize(Touch touch) => Reinitialize(new CommonTouch(touch));
        internal void Reinitialize(InputSystem.EnhancedTouch.Touch touch) => Reinitialize(new CommonTouch(touch));

        void Reinitialize(CommonTouch touch)
        {
            Reinitialize();
            fingerId = touch.fingerId;
            startPosition = touch.position;
            position = startPosition;
            delta = Vector2.zero;
        }

        /// <summary>
        /// (Read Only) The id of the finger used in this gesture.
        /// </summary>
        public int fingerId { get; private set; }

        /// <summary>
        /// (Read Only) The screen position where the gesture started.
        /// </summary>
        public Vector2 startPosition { get; private set; }

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
        protected DragGestureRecognizer dragRecognizer => (DragGestureRecognizer)recognizer;

        /// <inheritdoc />
        protected internal override bool CanStart()
        {
            if (GestureTouchesUtility.IsFingerIdRetained(fingerId))
            {
                Cancel();
                return false;
            }

            var touches = GestureTouchesUtility.touches;
            if (touches.Count > 1)
            {
                foreach (var currentTouch in touches)
                {
                    if (currentTouch.fingerId != fingerId &&
                        !GestureTouchesUtility.IsFingerIdRetained(currentTouch.fingerId))
                    {
                        return false;
                    }
                }
            }

            if (GestureTouchesUtility.TryFindTouch(fingerId, out var touch))
            {
                var pos = touch.position;
                var diff = (pos - startPosition).magnitude;
                if (GestureTouchesUtility.PixelsToInches(diff) >= dragRecognizer.slopInches)
                {
                    return true;
                }
            }
            else
            {
                Cancel();
            }

            return false;
        }

        /// <inheritdoc />
        protected internal override void OnStart()
        {
            GestureTouchesUtility.LockFingerId(fingerId);

#pragma warning disable 618 // Using deprecated property to help with backwards compatibility.
            if (GestureTouchesUtility.RaycastFromCamera(startPosition, recognizer.xrOrigin, recognizer.arSessionOrigin, out var hit))
#pragma warning restore 618
            {
                var gameObject = hit.transform.gameObject;
                if (gameObject != null)
                {
                    var interactableObject = gameObject.GetComponentInParent<ARBaseGestureInteractable>();
                    if (interactableObject != null)
                        targetObject = interactableObject.gameObject;
                }
            }

            GestureTouchesUtility.TryFindTouch(fingerId, out var touch);
            position = touch.position;
        }

        /// <inheritdoc />
        protected internal override bool UpdateGesture()
        {
            if (GestureTouchesUtility.TryFindTouch(fingerId, out var touch))
            {
                if (touch.isPhaseMoved)
                {
                    delta = touch.position - position;
                    position = touch.position;
                    return true;
                }

                if (touch.isPhaseEnded)
                {
                    Complete();
                }
                else if (touch.isPhaseCanceled)
                {
                    Cancel();
                }
            }
            else
            {
                Cancel();
            }

            return false;
        }

        /// <inheritdoc />
        protected internal override void OnCancel()
        {
        }

        /// <inheritdoc />
        protected internal override void OnFinish() => GestureTouchesUtility.ReleaseFingerId(fingerId);
    }
}

#endif
