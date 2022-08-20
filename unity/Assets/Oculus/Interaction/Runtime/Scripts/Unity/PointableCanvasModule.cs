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

using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.Assertions;
using System;

namespace Oculus.Interaction
{
    public class PointableCanvasEventArgs
    {
        public readonly Canvas Canvas;
        public readonly GameObject Hovered;
        public readonly bool Dragging;

        public PointableCanvasEventArgs(Canvas canvas, GameObject hovered, bool dragging)
        {
            Canvas = canvas;
            Hovered = hovered;
            Dragging = dragging;
        }
    }

    /// <summary>
    /// IPointerInteractableModule manages all InteractableCanvas events in
    /// the scene and translates them into pointer events for Unity Canvas UIs.
    /// </summary>
    public class PointableCanvasModule : PointerInputModule
    {
        public static event Action<PointableCanvasEventArgs> WhenSelected;

        public static event Action<PointableCanvasEventArgs> WhenUnselected;

        public static event Action<PointableCanvasEventArgs> WhenSelectableHovered;

        public static event Action<PointableCanvasEventArgs> WhenSelectableUnhovered;

        [SerializeField]
        private bool _useInitialPressPositionForDrag = true;

        private Camera _pointerEventCamera;
        private static PointableCanvasModule _instance = null;
        private static PointableCanvasModule Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PointableCanvasModule>();
                }
                return _instance;
            }
        }

        public static void RegisterPointableCanvas(IPointableCanvas pointerCanvas)
        {
            Assert.IsNotNull(Instance, "A PointableCanvasModule is required in the scene.");
            Instance.AddPointerCanvas(pointerCanvas);
        }

        public static void UnregisterPointableCanvas(IPointableCanvas pointerCanvas)
        {
            Instance?.RemovePointerCanvas(pointerCanvas);
        }

        private Dictionary<int, Pointer> _pointerMap = new Dictionary<int, Pointer>();
        private List<RaycastResult> _raycastResultCache = new List<RaycastResult>();
        private List<Pointer> _pointersForDeletion = new List<Pointer>();
        private Dictionary<IPointableCanvas, Action<PointerEvent>> _pointerCanvasActionMap =
            new Dictionary<IPointableCanvas, Action<PointerEvent>>();

        private void AddPointerCanvas(IPointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = (args) => HandlePointerEvent(pointerCanvas.Canvas, args);
            _pointerCanvasActionMap.Add(pointerCanvas, pointerCanvasAction);
            pointerCanvas.WhenPointerEventRaised += pointerCanvasAction;
        }

        private void RemovePointerCanvas(IPointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = _pointerCanvasActionMap[pointerCanvas];
            _pointerCanvasActionMap.Remove(pointerCanvas);
            pointerCanvas.WhenPointerEventRaised -= pointerCanvasAction;

            List<int> pointerIDs = new List<int>(_pointerMap.Keys);
            foreach (int pointerID in pointerIDs)
            {
                Pointer pointer = _pointerMap[pointerID];
                if (pointer.Canvas != pointerCanvas.Canvas)
                {
                    continue;
                }
                ClearPointerSelection(pointer.PointerEventData);
                pointer.MarkForDeletion();
                _pointersForDeletion.Add(pointer);
                _pointerMap.Remove(pointerID);
            }
        }

        private void HandlePointerEvent(Canvas canvas, PointerEvent evt)
        {
            Pointer pointer;

            switch (evt.Type)
            {
                case PointerEventType.Hover:
                    pointer = new Pointer(canvas);
                    pointer.PointerEventData = new PointerEventData(eventSystem);
                    pointer.SetPosition(evt.Pose.position);
                    _pointerMap.Add(evt.Identifier, pointer);
                    break;
                case PointerEventType.Unhover:
                    pointer = _pointerMap[evt.Identifier];
                    _pointerMap.Remove(evt.Identifier);
                    pointer.MarkForDeletion();
                    _pointersForDeletion.Add(pointer);
                    break;
                case PointerEventType.Select:
                    pointer = _pointerMap[evt.Identifier];
                    pointer.SetPosition(evt.Pose.position);
                    pointer.Press();
                    break;
                case PointerEventType.Unselect:
                    pointer = _pointerMap[evt.Identifier];
                    pointer.SetPosition(evt.Pose.position);
                    pointer.Release();
                    break;
                case PointerEventType.Move:
                    pointer = _pointerMap[evt.Identifier];
                    pointer.SetPosition(evt.Pose.position);
                    break;
                case PointerEventType.Cancel:
                    pointer = _pointerMap[evt.Identifier];
                    _pointerMap.Remove(evt.Identifier);
                    ClearPointerSelection(pointer.PointerEventData);
                    pointer.MarkForDeletion();
                    _pointersForDeletion.Add(pointer);
                    break;
            }
        }

        /// <summary>
        /// Pointer class that is used for state associated with IPointables that are currently
        /// tracked by any IPointableCanvases in the scene.
        /// </summary>
        private class Pointer
        {
            public PointerEventData PointerEventData { get; set; }

            public bool MarkedForDeletion { get; private set; }

            private Canvas _canvas;
            public Canvas Canvas => _canvas;

            private Vector3 _position;
            public Vector3 Position => _position;

            private GameObject _hoveredSelectable;
            public GameObject HoveredSelectable => _hoveredSelectable;


            private bool _pressing = false;
            private bool _pressed;
            private bool _released;

            public Pointer(Canvas canvas)
            {
                _canvas = canvas;
                _pressed = _released = false;
            }

            public void Press()
            {
                if (_pressing) return;
                _pressing = true;
                _pressed = true;
            }
            public void Release()
            {
                if (!_pressing) return;
                _pressing = false;
                _released = true;
            }

            public void ReadAndResetPressedReleased(out bool pressed, out bool released)
            {
                pressed = _pressed;
                released = _released;
                _pressed = _released = false;
            }

            public void MarkForDeletion()
            {
                MarkedForDeletion = true;
                Release();
            }

            public void SetPosition(Vector3 position)
            {
                _position = position;
            }

            public void SetHoveredSelectable(GameObject hoveredSelectable)
            {
                _hoveredSelectable = hoveredSelectable;
            }
        }

        protected bool _started = false;

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _pointerEventCamera = gameObject.AddComponent<Camera>();
                _pointerEventCamera.nearClipPlane = 0.1f;

                // We do not need this camera to be enabled to serve this module's purposes:
                // as a dependency for Canvases and for its WorldToScreenPoint functionality
                _pointerEventCamera.enabled = false;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                Destroy(_pointerEventCamera);
                _pointerEventCamera = null;
            }
            base.OnDisable();
        }

        // Based On FindFirstRaycast
        protected static RaycastResult FindFirstRaycastWithinCanvas(List<RaycastResult> candidates, Canvas canvas)
        {
            GameObject candidateGameObject;
            Canvas candidateCanvas;
            for (var i = 0; i < candidates.Count; ++i)
            {
                candidateGameObject = candidates[i].gameObject;
                if (candidateGameObject == null) continue;

                candidateCanvas = candidateGameObject.GetComponentInParent<Canvas>();
                if (candidateCanvas == null) continue;
                if (candidateCanvas.rootCanvas != canvas) continue;

                return candidates[i];
            }
            return new RaycastResult();
        }

        private void UpdateRaycasts(Pointer pointer, out bool pressed, out bool released)
        {
            PointerEventData pointerEventData = pointer.PointerEventData;

            Vector2 prevPosition = pointerEventData.position;
            Canvas canvas = pointer.Canvas;
            canvas.worldCamera = _pointerEventCamera;

            pointerEventData.Reset();

            pointer.ReadAndResetPressedReleased(out pressed, out released);

            Vector3 position = Vector3.zero;
            var plane = new Plane(-1f * canvas.transform.forward, canvas.transform.position);
            var ray = new Ray(pointer.Position - canvas.transform.forward, canvas.transform.forward);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                position = ray.GetPoint(enter);
            }

            // We need to position our camera at an offset from the Pointer position or else
            // a graphic raycast may ignore a world canvas that's outside of our regular camera view(s)
            _pointerEventCamera.transform.position = pointer.Position - canvas.transform.forward;
            _pointerEventCamera.transform.LookAt(pointer.Position, canvas.transform.up);

            Vector2 pointerPosition = _pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition;

            // RaycastAll raycasts against with every GraphicRaycaster in the scene,
            // including nested ones like in the case of a dropdown
            eventSystem.RaycastAll(pointerEventData, _raycastResultCache);

            RaycastResult firstResult = FindFirstRaycastWithinCanvas(_raycastResultCache, canvas);
            pointer.PointerEventData.pointerCurrentRaycast = firstResult;

            _raycastResultCache.Clear();

            // We use a static translation offset from the canvas for 2D position delta tracking
            _pointerEventCamera.transform.position = canvas.transform.position - canvas.transform.forward;
            _pointerEventCamera.transform.LookAt(canvas.transform.position, canvas.transform.up);

            pointerPosition = _pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition;

            if (pressed)
            {
                pointerEventData.delta = Vector2.zero;
            }
            else
            {
                pointerEventData.delta = pointerEventData.position - prevPosition;
            }

            pointerEventData.button = PointerEventData.InputButton.Left;
        }

        public override void Process()
        {
            foreach (Pointer pointer in _pointersForDeletion)
            {
                ProcessPointer(pointer, true);
            }
            _pointersForDeletion.Clear();

            foreach (Pointer pointer in _pointerMap.Values)
            {
                ProcessPointer(pointer);
            }
        }

        private void ProcessPointer(Pointer pointer, bool forceRelease = false)
        {
            bool pressed = false;
            bool released = false;
            bool wasDragging = pointer.PointerEventData.dragging;

            UpdateRaycasts(pointer, out pressed, out released);

            PointerEventData pointerEventData = pointer.PointerEventData;
            UpdatePointerEventData(pointerEventData, pressed, released);

            released |= forceRelease;

            if (!released)
            {
                ProcessMove(pointerEventData);
                ProcessDrag(pointerEventData);
            }
            else
            {
                HandlePointerExitAndEnter(pointerEventData, null);
                RemovePointerData(pointerEventData);
            }

            HandleSelectableHover(pointer, wasDragging);
            HandleSelectablePress(pointer, pressed, released, wasDragging);
        }

        private void HandleSelectableHover(Pointer pointer, bool wasDragging)
        {
            bool dragging = pointer.PointerEventData.dragging || wasDragging;

            GameObject currentOverGo = pointer.PointerEventData.pointerCurrentRaycast.gameObject;
            GameObject prevHoveredSelectable = pointer.HoveredSelectable;
            GameObject newHoveredSelectable = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            pointer.SetHoveredSelectable(newHoveredSelectable);

            if (newHoveredSelectable != null && newHoveredSelectable != prevHoveredSelectable)
            {
                WhenSelectableHovered?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging));
            }
            else if (prevHoveredSelectable != null && newHoveredSelectable == null)
            {
                WhenSelectableUnhovered?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging));
            }
        }

        private void HandleSelectablePress(Pointer pointer, bool pressed, bool released, bool wasDragging)
        {
            bool dragging = pointer.PointerEventData.dragging || wasDragging;

            if (pressed)
            {
                WhenSelected?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging));
            }
            else if (released && !pointer.MarkedForDeletion)
            {
                // Unity handles UI selection on release, so we verify the hovered element has been selected
                bool hasSelectedHoveredObject = pointer.HoveredSelectable != null &&
                                                pointer.HoveredSelectable == pointer.PointerEventData.selectedObject;
                GameObject selectedObject = hasSelectedHoveredObject ? pointer.HoveredSelectable : null;
                WhenUnselected?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, selectedObject, dragging));
            }
        }

        /// <summary>
        /// This method is based on ProcessTouchPoint in StandaloneInputModule,
        /// but is instead used for Pointer events
        /// </summary>
        protected void UpdatePointerEventData(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            }

            // PointerUp notification
            if (released)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                }

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }
        }

        /// <summary>
        /// Override of PointerInputModule's ProcessDrag to allow using the initial press position for drag begin.
        /// Set _useInitialPressPositionForDrag to false if you prefer the default behaviour of PointerInputModule.
        /// </summary>
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
                return;

            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position,
                    eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                if (_useInitialPressPositionForDrag)
                {
                    pointerEvent.position = pointerEvent.pressPosition;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent,
                    ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ClearPointerSelection(pointerEvent);
                }

                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent,
                    ExecuteEvents.dragHandler);
            }
        }

        private void ClearPointerSelection(PointerEventData pointerEvent)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent,
                ExecuteEvents.pointerUpHandler);

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;
        }

        /// <summary>
        /// Used in PointerInputModule's ProcessDrag implementation. Brought into this subclass with a protected
        /// signature (as opposed to the parent's private signature) to be used in this subclass's overridden ProcessDrag.
        /// </summary>
        protected static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }
    }
}
