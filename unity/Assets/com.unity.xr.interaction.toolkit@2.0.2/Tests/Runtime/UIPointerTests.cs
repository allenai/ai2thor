using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

// There is a problem with upm-ci package test builds where the Unity.InputSystem.TestFramework assembly
// is not included, which causes some of these tests to fail to compile. To allow these tests to be run,
// modify your project's Packages\manifest.json file to include com.unity.inputsystem in the testables list.
// See [Project Manifest](https://docs.unity3d.com/Manual/upm-manifestPrj.html)
// Example:
//   "testables": [
//     "com.unity.inputsystem",
//     "com.unity.xr.interaction.toolkit"
//   ]
// Then open Edit > Project Settings... > Player and edit the Scripting Define Symbols to add this.
// It is enabled in the XR Interaction Toolkit Examples project to allow these
// tests to be manually run, but skipped during some types of automated builds where the symbol is not defined.
#if ENABLE_INPUT_SYSTEM_TESTFRAMEWORK_TESTS
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    static class XRControllerRecorderExtensions
    {
        internal static void SetNextPose(this XRControllerRecorder recorder, Vector3 position, Quaternion rotation, bool selectActive, bool activateActive, bool pressActive)
        {
            XRControllerRecording currentRecording = recorder.recording;
            currentRecording.InitRecording();
            currentRecording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, position, rotation, InputTrackingState.All, selectActive, activateActive, pressActive));
            currentRecording.AddRecordingFrameNonAlloc(new XRControllerState(1000f, position, rotation, InputTrackingState.All, selectActive, activateActive, pressActive));
            recorder.recording = currentRecording;
            recorder.ResetPlayback();
            recorder.isPlaying = true;
        }
    }

    [TestFixture]
#if ENABLE_INPUT_SYSTEM_TESTFRAMEWORK_TESTS
    class UIPointerTests : InputTestFixture
#else
    class UIPointerTests
#endif
    {
        internal enum EventType
        {
            Click,
            Down,
            Up,
            Enter,
            Exit,
            Select,
            Deselect,
            PotentialDrag,
            BeginDrag,
            Dragging,
            Drop,
            EndDrag,
            Move,
            Submit,
            Cancel,
            Scroll,
            UpdateSelected,
        }

        struct TestObjects
        {
            public Camera camera;
            public TestEventSystem eventSystem;
            public XRControllerRecorder controllerRecorder;
            public XRRayInteractor interactor;
            public UICallbackReceiver leftUIReceiver;
            public UICallbackReceiver rightUIReceiver;
            public GlobalUIReceiver globalUIReceiver;
            public XRUIInputModule uiInputModule;
        }

        internal struct Event
        {
            public EventType type;
            public BaseEventData data;
            public GameObject target;

            public Event(EventType type, BaseEventData data, GameObject target = null)
            {
                this.type = type;
                this.data = data;
                this.target = target;
            }

            public override string ToString()
            {
                var dataString = data.ToString();
                dataString = dataString.Replace("\n", "\n\t");
                return $"{type} - {target}[\n\t{dataString}]";
            }
        }

        static BaseEventData CloneEventData(BaseEventData eventData)
        {
            switch (eventData)
            {
                case AxisEventData axisEventData:
                    return new AxisEventData(EventSystem.current)
                    {
                        moveVector = axisEventData.moveVector,
                        moveDir = axisEventData.moveDir,
                    };
                case TrackedDeviceEventData trackedEventData:
                    return new TrackedDeviceEventData(EventSystem.current)
                    {
                        pointerId = trackedEventData.pointerId,
                        position = trackedEventData.position,
                        button = trackedEventData.button,
                        clickCount = trackedEventData.clickCount,
                        clickTime = trackedEventData.clickTime,
                        eligibleForClick = trackedEventData.eligibleForClick,
                        delta = trackedEventData.delta,
                        scrollDelta = trackedEventData.scrollDelta,
                        dragging = trackedEventData.dragging,
                        hovered = new List<GameObject>(trackedEventData.hovered),
                        pointerDrag = trackedEventData.pointerDrag,
                        pointerEnter = trackedEventData.pointerEnter,
                        pointerPress = trackedEventData.pointerPress,
                        pressPosition = trackedEventData.pressPosition,
                        pointerCurrentRaycast = trackedEventData.pointerCurrentRaycast,
                        pointerPressRaycast = trackedEventData.pointerPressRaycast,
                        rawPointerPress = trackedEventData.rawPointerPress,
                        useDragThreshold = trackedEventData.useDragThreshold,

                        layerMask = trackedEventData.layerMask,
                        rayHitIndex = trackedEventData.rayHitIndex,
                        rayPoints = new List<Vector3>(trackedEventData.rayPoints),
                    };
                case PointerEventData pointerEventData:
                    return new PointerEventData(EventSystem.current)
                    {
                        pointerId = pointerEventData.pointerId,
                        position = pointerEventData.position,
                        button = pointerEventData.button,
                        clickCount = pointerEventData.clickCount,
                        clickTime = pointerEventData.clickTime,
                        eligibleForClick = pointerEventData.eligibleForClick,
                        delta = pointerEventData.delta,
                        scrollDelta = pointerEventData.scrollDelta,
                        dragging = pointerEventData.dragging,
                        hovered = new List<GameObject>(pointerEventData.hovered),
                        pointerDrag = pointerEventData.pointerDrag,
                        pointerEnter = pointerEventData.pointerEnter,
                        pointerPress = pointerEventData.pointerPress,
                        pressPosition = pointerEventData.pressPosition,
                        pointerCurrentRaycast = pointerEventData.pointerCurrentRaycast,
                        pointerPressRaycast = pointerEventData.pointerPressRaycast,
                        rawPointerPress = pointerEventData.rawPointerPress,
                        useDragThreshold = pointerEventData.useDragThreshold,
                    };
                default:
                    return new BaseEventData(EventSystem.current);
            }
        }

        internal class UICallbackReceiver : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler,
            IPointerExitHandler, IPointerUpHandler, IMoveHandler, ISelectHandler, IDeselectHandler, IInitializePotentialDragHandler,
            IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ISubmitHandler, ICancelHandler, IScrollHandler
        {
            public List<Event> events = new List<Event>();

            public void Reset()
            {
                events.Clear();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Click, CloneEventData(eventData)));
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Down, CloneEventData(eventData)));
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Enter, CloneEventData(eventData)));
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Exit, CloneEventData(eventData)));
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Up, CloneEventData(eventData)));
            }

            public void OnMove(AxisEventData eventData)
            {
                events.Add(new Event(EventType.Move, CloneEventData(eventData)));
            }

            public void OnSubmit(BaseEventData eventData)
            {
                events.Add(new Event(EventType.Submit, CloneEventData(eventData)));
            }

            public void OnCancel(BaseEventData eventData)
            {
                events.Add(new Event(EventType.Cancel, CloneEventData(eventData)));
            }

            public void OnSelect(BaseEventData eventData)
            {
                events.Add(new Event(EventType.Select, CloneEventData(eventData)));
            }

            public void OnDeselect(BaseEventData eventData)
            {
                events.Add(new Event(EventType.Deselect, CloneEventData(eventData)));
            }

            public void OnInitializePotentialDrag(PointerEventData eventData)
            {
                events.Add(new Event(EventType.PotentialDrag, CloneEventData(eventData)));
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                events.Add(new Event(EventType.BeginDrag, CloneEventData(eventData)));
            }

            public void OnDrag(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Dragging, CloneEventData(eventData)));
            }

            public void OnDrop(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Drop, CloneEventData(eventData)));
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                events.Add(new Event(EventType.EndDrag, CloneEventData(eventData)));
            }

            public void OnScroll(PointerEventData eventData)
            {
                events.Add(new Event(EventType.Scroll, CloneEventData(eventData)));
            }
        }

        class GlobalUIReceiver
        {
            public List<Event> events = new List<Event>();

            public GlobalUIReceiver(UIInputModule module)
            {
                // We never unsubscribe these events--Always ensure only one GlobalUIReciever is associated with one UIInputModule
                module.pointerEnter += OnPointerEnter;
                module.pointerExit += OnPointerExit;
                module.pointerDown += OnPointerDown;
                module.pointerUp += OnPointerUp;
                module.pointerClick += OnPointerClick;
                module.initializePotentialDrag += OnInitializePotentialDrag;
                module.beginDrag += OnBeginDrag;
                module.drag += OnDrag;
                module.endDrag += OnEndDrag;
                module.drop += OnDrop;
                module.scroll += OnScroll;
                module.updateSelected += OnUpdateSelected;
                module.move += OnMove;
                module.submit += OnSubmit;
                module.cancel += OnCancel;
            }

            public void Reset()
            {
                events.Clear();
            }

            void OnPointerEnter(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Enter, CloneEventData(eventData), target));
            }

            void OnPointerExit(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Exit, CloneEventData(eventData), target));
            }

            void OnPointerDown(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Down, CloneEventData(eventData), target));
            }

            void OnPointerUp(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Up, CloneEventData(eventData), target));
            }

            void OnPointerClick(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Click, CloneEventData(eventData), target));
            }

            void OnInitializePotentialDrag(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.PotentialDrag, CloneEventData(eventData), target));
            }

            void OnBeginDrag(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.BeginDrag, CloneEventData(eventData), target));
            }

            void OnDrag(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Dragging, CloneEventData(eventData), target));
            }

            void OnEndDrag(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.EndDrag, CloneEventData(eventData), target));
            }

            void OnDrop(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Drop, CloneEventData(eventData), target));
            }

            void OnScroll(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Scroll, CloneEventData(eventData), target));
            }

            void OnUpdateSelected(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.UpdateSelected, CloneEventData(eventData), target));
            }

            void OnMove(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Move, CloneEventData(eventData), target));
            }

            void OnSubmit(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Submit, CloneEventData(eventData), target));
            }

            void OnCancel(GameObject target, BaseEventData eventData)
            {
                events.Add(new Event(EventType.Cancel, CloneEventData(eventData), target));
            }
        }

        internal class TestEventSystem : EventSystem
        {
            public void InvokeUpdate()
            {
                current = this; // Needs to be current to be allowed to update.
                Update();
            }
        }

        static TestObjects SetupRig(bool setFirstSelected = false)
        {
            var testObjects = new TestObjects();

            _ = new GameObject("InteractionManager", typeof(XRInteractionManager));

            var rigGo = new GameObject("XROrigin");
            rigGo.SetActive(false);
            var rig = rigGo.AddComponent<XROrigin>();

            // Add camera offset
            var cameraOffsetGo = new GameObject();
            cameraOffsetGo.name = "CameraOffset";
            cameraOffsetGo.transform.SetParent(rig.transform, false);
            rig.CameraFloorOffsetObject = cameraOffsetGo;

            // Set up camera and canvas on which we can perform ray casts.
            var cameraGo = new GameObject("Camera");
            cameraGo.transform.parent = rigGo.transform;
            Camera camera = testObjects.camera = cameraGo.AddComponent<Camera>();
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.pixelRect = new Rect(0, 0, 640, 480);

            rig.Camera = cameraGo.GetComponent<Camera>();
            rigGo.SetActive(true);

            var eventSystemGo = new GameObject("EventSystem", typeof(TestEventSystem), typeof(XRUIInputModule));
            var inputModule = eventSystemGo.GetComponent<XRUIInputModule>();
            inputModule.uiCamera = camera;
            inputModule.enableXRInput = true;
            inputModule.enableMouseInput = false;
            inputModule.enableTouchInput = false;
            inputModule.enableGamepadInput = false;
            inputModule.enableJoystickInput = false;
            testObjects.eventSystem = eventSystemGo.GetComponent<TestEventSystem>();
            testObjects.eventSystem.UpdateModules();
            if (!setFirstSelected) // This will get called from SetupUIScene
                testObjects.eventSystem.InvokeUpdate(); // Initial update only sets current module.

            testObjects.globalUIReceiver = new GlobalUIReceiver(inputModule);

            var interactorGo = new GameObject("Interactor", typeof(XRController), typeof(XRRayInteractor), typeof(XRControllerRecorder));
            interactorGo.transform.parent = rigGo.transform;
            testObjects.controllerRecorder = interactorGo.GetComponent<XRControllerRecorder>();
            testObjects.controllerRecorder.recording = ScriptableObject.CreateInstance<XRControllerRecording>();
            testObjects.interactor = interactorGo.GetComponent<XRRayInteractor>();
            testObjects.interactor.maxRaycastDistance = int.MaxValue;
            testObjects.interactor.referenceFrame = rigGo.transform;
            testObjects.uiInputModule = inputModule;

            return testObjects;
        }

        static TestObjects SetupUIScene(bool setFirstSelected = false)
        {
            var testObjects = SetupRig(setFirstSelected);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.worldCamera = testObjects.camera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;

            // Set up a GameObject hierarchy that we send events to. In a real setup,
            // this would be a hierarchy involving UI components.
            var parentGameObject = new GameObject("Parent");
            var parentTransform = parentGameObject.AddComponent<RectTransform>();
            parentGameObject.AddComponent<UICallbackReceiver>();

            var leftChildGameObject = new GameObject("Left Child");
            var leftChildTransform = leftChildGameObject.AddComponent<RectTransform>();
            leftChildGameObject.AddComponent<Image>();
            testObjects.leftUIReceiver = leftChildGameObject.AddComponent<UICallbackReceiver>();
            leftChildGameObject.AddComponent<Selectable>();

            var rightChildGameObject = new GameObject("Right Child");
            var rightChildTransform = rightChildGameObject.AddComponent<RectTransform>();
            rightChildGameObject.AddComponent<Image>();
            testObjects.rightUIReceiver = rightChildGameObject.AddComponent<UICallbackReceiver>();
            rightChildGameObject.AddComponent<Selectable>();

            parentTransform.SetParent(canvas.transform, false);
            leftChildTransform.SetParent(parentTransform, false);
            rightChildTransform.SetParent(parentTransform, false);

            if (setFirstSelected)
            {
                testObjects.eventSystem.firstSelectedGameObject = leftChildGameObject;
                testObjects.eventSystem.InvokeUpdate();
            }

            // Parent occupies full space of canvas.
            parentTransform.sizeDelta = new Vector2(640, 480);

            // Left child occupies left half of parent.
            const int quarterSize = 640 / 4;
            leftChildTransform.anchoredPosition = new Vector2(-quarterSize, 0);
            leftChildTransform.sizeDelta = new Vector2(320, 480);

            // Right child occupies right half of parent.
            rightChildTransform.anchoredPosition = new Vector2(quarterSize, 0);
            rightChildTransform.sizeDelta = new Vector2(320, 480);

            return testObjects;
        }

        static TestObjects SetupPhysicsScene()
        {
            var testObjects = SetupRig();

            var physicsRaycaster = new GameObject("PhysicsRaycaster", typeof(TrackedDevicePhysicsRaycaster)).GetComponent<TrackedDevicePhysicsRaycaster>();
            physicsRaycaster.SetEventCamera(testObjects.camera);

            var parentGameObject = new GameObject("Interactables");
            var parentTransform = parentGameObject.transform;

            var groupGameObject = new GameObject("Group");
            var groupTransform = groupGameObject.transform;
            groupGameObject.AddComponent<UICallbackReceiver>();

            var leftGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObjects.leftUIReceiver = leftGameObject.AddComponent<UICallbackReceiver>();
            var leftTransform = leftGameObject.transform;
            leftTransform.position = new Vector3(-0.5f, 0.0f, 1.75f);
            leftGameObject.AddComponent<Selectable>();

            var rightGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObjects.rightUIReceiver = rightGameObject.AddComponent<UICallbackReceiver>();
            var rightTransform = rightGameObject.transform;
            rightTransform.position = new Vector3(0.5f, 0.0f, 1.75f);
            rightGameObject.AddComponent<Selectable>();

            groupGameObject.transform.SetParent(parentTransform, false);
            leftGameObject.transform.SetParent(groupTransform, false);
            rightGameObject.transform.SetParent(groupTransform, false);

            return testObjects;
        }

        static IEnumerator ResetTestObjects(TestObjects testObjects)
        {
            testObjects.controllerRecorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -90.0f, 0.0f), false, false, false);
            testObjects.eventSystem.SetSelectedGameObject(null);
            yield return new WaitForFixedUpdate();
            yield return null;

            testObjects.leftUIReceiver.Reset();
            testObjects.rightUIReceiver.Reset();
            testObjects.globalUIReceiver.Reset();

            Assert.That(testObjects.leftUIReceiver.events, Has.Count.EqualTo(0));
            Assert.That(testObjects.rightUIReceiver.events, Has.Count.EqualTo(0));
            Assert.That(testObjects.globalUIReceiver.events, Has.Count.EqualTo(0));
        }

        static IEnumerator CheckEvents(TestObjects testObjects)
        {
            var leftUIReceiver = testObjects.leftUIReceiver;
            var rightUIReceiver = testObjects.rightUIReceiver;
            var globalUIReceiver = testObjects.globalUIReceiver;

            var recorder = testObjects.controllerRecorder;
            var eventSystem = testObjects.eventSystem;
            Assert.That(testObjects.uiInputModule.GetTrackedDeviceModel(testObjects.interactor, out var model), Is.True);
            var primaryPointerId = model.pointerId;
            Assert.That(primaryPointerId, Is.Not.LessThan(0));
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.False);

            // Reset to Defaults
            yield return ResetTestObjects(testObjects);

            // Move over left child.
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, false);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That(leftUIReceiver.events[0].data, Is.TypeOf<TrackedDeviceEventData>());
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.True);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.True);

            var globalEvents = globalUIReceiver.events;
            var leftUIReceiverParentTransform = leftUIReceiver.transform.parent;
            Assert.That(globalEvents, Has.Count.EqualTo(3));
            Assert.That(globalEvents[0].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalEvents[0].data, Is.TypeOf<TrackedDeviceEventData>());
            Assert.That(globalEvents[0].target, Is.EqualTo(leftUIReceiver.gameObject));
            Assert.That(globalEvents[1].target, Is.EqualTo(leftUIReceiverParentTransform.gameObject));
            Assert.That(globalEvents[2].target, Is.EqualTo(leftUIReceiverParentTransform.parent.gameObject));

            var eventData = (TrackedDeviceEventData)leftUIReceiver.events[0].data;
            Assert.That(eventData.interactor, Is.EqualTo(testObjects.interactor));
            leftUIReceiver.Reset();
            globalUIReceiver.Reset();

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            // Check basic down/up
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, true);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(3));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Select));
            Assert.That(leftUIReceiver.events[2].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(leftUIReceiver.gameObject));
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));

            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, false);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Up));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Click));
            Assert.That(((PointerEventData)leftUIReceiver.events[1].data).pointerId, Is.EqualTo(primaryPointerId));
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.True);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.True);

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(3));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.UpdateSelected));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.Up));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Click));
            yield return ResetTestObjects(testObjects);
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.False);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.False);


            // Check down, off, back on, and up
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, true);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(4));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Select));
            Assert.That(leftUIReceiver.events[2].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That(leftUIReceiver.events[3].type, Is.EqualTo(EventType.Enter));
            Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(leftUIReceiver.gameObject));
            Assert.That(((PointerEventData)leftUIReceiver.events[2].data).pointerId, Is.EqualTo(primaryPointerId));
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.True);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.True);
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(5));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalUIReceiver.events[3].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalUIReceiver.events[4].type, Is.EqualTo(EventType.Enter));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));

            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, 30.0f, 0.0f), false, false, true);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(3));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Exit));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.BeginDrag));
            Assert.That(leftUIReceiver.events[2].type, Is.EqualTo(EventType.Dragging));
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That(((PointerEventData)rightUIReceiver.events[0].data).pointerId, Is.EqualTo(primaryPointerId));
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.True);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.True);
            rightUIReceiver.Reset();
            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(5));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.UpdateSelected));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.Exit));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalUIReceiver.events[3].type, Is.EqualTo(EventType.BeginDrag));
            Assert.That(globalUIReceiver.events[4].type, Is.EqualTo(EventType.Dragging));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));

            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, false);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(4));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Up));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Click));
            Assert.That(leftUIReceiver.events[2].type, Is.EqualTo(EventType.EndDrag));
            Assert.That(leftUIReceiver.events[3].type, Is.EqualTo(EventType.Enter));
            Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(leftUIReceiver.gameObject));
            Assert.That(((PointerEventData)leftUIReceiver.events[3].data).pointerId, Is.EqualTo(primaryPointerId));
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.True);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.True);
            Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(leftUIReceiver.gameObject));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Exit));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(6));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.UpdateSelected));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.Up));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Click));
            Assert.That(globalUIReceiver.events[3].type, Is.EqualTo(EventType.EndDrag));
            Assert.That(globalUIReceiver.events[4].type, Is.EqualTo(EventType.Exit));
            Assert.That(globalUIReceiver.events[5].type, Is.EqualTo(EventType.Enter));
            yield return ResetTestObjects(testObjects);
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.False);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.False);


            // Check down and drag
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, true);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(4));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Select));
            Assert.That(leftUIReceiver.events[2].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That(leftUIReceiver.events[3].type, Is.EqualTo(EventType.Enter));
            Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(leftUIReceiver.gameObject));
            Assert.That(((PointerEventData)leftUIReceiver.events[2].data).pointerId, Is.EqualTo(primaryPointerId));
            Assert.That(eventSystem.IsPointerOverGameObject(primaryPointerId), Is.True);
            Assert.That(eventSystem.IsPointerOverGameObject(-1), Is.True);
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(5));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.Down));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.PotentialDrag));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalUIReceiver.events[3].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalUIReceiver.events[4].type, Is.EqualTo(EventType.Enter));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));

            // Move to new location on left child
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -10.0f, 0.0f), false, false, true);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.BeginDrag));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(3));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.UpdateSelected));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.BeginDrag));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Dragging));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));

            // Move children
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, 30.0f, 0.0f), false, false, true);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Exit));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Dragging));
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            rightUIReceiver.Reset();
            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(4));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.UpdateSelected));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.Exit));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalUIReceiver.events[3].type, Is.EqualTo(EventType.Dragging));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));

            // Deselect
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, 30.0f, 0.0f), false, false, false);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Up));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.EndDrag));
            leftUIReceiver.Reset();
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Drop));
            rightUIReceiver.Reset();
            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(4));
            Assert.That(globalUIReceiver.events[0].type, Is.EqualTo(EventType.UpdateSelected));
            Assert.That(globalUIReceiver.events[1].type, Is.EqualTo(EventType.Up));
            Assert.That(globalUIReceiver.events[2].type, Is.EqualTo(EventType.Drop));
            Assert.That(globalUIReceiver.events[3].type, Is.EqualTo(EventType.EndDrag));
            globalUIReceiver.Reset();
            Assert.That(globalUIReceiver.events, Has.Count.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TrackedDevicesCanDriveUIGraphics()
        {
            TestObjects testObjects = SetupUIScene();

            yield return CheckEvents(testObjects);

            // This suppresses a warning that would be logged by TrackedDeviceGraphicRaycaster if the Camera is destroyed first
            Object.Destroy(testObjects.eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator TrackedDevicesCanDriveUIPhysics()
        {
            var testObjects = SetupPhysicsScene();

            yield return CheckEvents(testObjects);

            // This suppresses a warning that would be logged by TrackedDeviceGraphicRaycaster if the Camera is destroyed first
            Object.Destroy(testObjects.eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator PointerEnterBubblesUp()
        {
            var testObjects = SetupPhysicsScene();

            var leftUIReceiver = testObjects.leftUIReceiver;
            var rightUIReceiver = testObjects.rightUIReceiver;
            var globalUIReceiver = testObjects.globalUIReceiver;

            // Have the event receiver on a parent of the Collider child
            // to test that pointer enter events are bubbled up to parent objects
            // even when the hit GameObject does not have any event handlers itself.
            Object.Destroy(leftUIReceiver.GetComponent<Collider>());
            var leftUIColliderGameObject = new GameObject("Collider", typeof(BoxCollider));
            leftUIColliderGameObject.transform.SetParent(leftUIReceiver.transform, false);

            var recorder = testObjects.controllerRecorder;

            // Reset to Defaults
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -90.0f, 0.0f), false, false, false);
            yield return new WaitForFixedUpdate();
            yield return null;

            leftUIReceiver.Reset();
            rightUIReceiver.Reset();
            globalUIReceiver.Reset();

            // Move over left child.
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -30.0f, 0.0f), false, false, false);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Enter));
            Assert.That(leftUIReceiver.events[0].data, Is.TypeOf<TrackedDeviceEventData>());

            var globalEvents = globalUIReceiver.events;
            var leftUIReceiverParentTransform = leftUIReceiver.transform.parent;
            Assert.That(globalEvents, Has.Count.EqualTo(4));
            Assert.That(globalEvents[0].type, Is.EqualTo(EventType.Enter));
            Assert.That(globalEvents[0].data, Is.TypeOf<TrackedDeviceEventData>());
            Assert.That(globalEvents[0].target, Is.EqualTo(leftUIColliderGameObject));
            Assert.That(globalEvents[1].target, Is.EqualTo(leftUIReceiver.gameObject));
            Assert.That(globalEvents[2].target, Is.EqualTo(leftUIReceiverParentTransform.gameObject));
            Assert.That(globalEvents[3].target, Is.EqualTo(leftUIReceiverParentTransform.parent.gameObject));

            var eventData = (TrackedDeviceEventData)leftUIReceiver.events[0].data;
            Assert.That(eventData.interactor, Is.EqualTo(testObjects.interactor));
            leftUIReceiver.Reset();
            globalUIReceiver.Reset();

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            // Move off left child.
            recorder.SetNextPose(Vector3.zero, Quaternion.Euler(0.0f, -90.0f, 0.0f), false, false, false);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Exit));
            Assert.That(leftUIReceiver.events[0].data, Is.TypeOf<TrackedDeviceEventData>());

            Assert.That(globalEvents, Has.Count.EqualTo(4));
            Assert.That(globalEvents[0].type, Is.EqualTo(EventType.Exit));
            Assert.That(globalEvents[0].data, Is.TypeOf<TrackedDeviceEventData>());
            Assert.That(globalEvents[0].target, Is.EqualTo(leftUIColliderGameObject));
            Assert.That(globalEvents[1].target, Is.EqualTo(leftUIReceiver.gameObject));
            Assert.That(globalEvents[2].target, Is.EqualTo(leftUIReceiverParentTransform.gameObject));
            Assert.That(globalEvents[3].target, Is.EqualTo(leftUIReceiverParentTransform.parent.gameObject));

            // This suppresses a warning that would be logged by TrackedDeviceGraphicRaycaster if the Camera is destroyed first
            Object.Destroy(testObjects.eventSystem.gameObject);
        }

#if ENABLE_INPUT_SYSTEM_TESTFRAMEWORK_TESTS
        [UnityTest]
        public IEnumerator UIJoystickNavigation()
        {
            var testObjects = SetupUIScene(true);

            // Enable device input
            testObjects.uiInputModule.enableXRInput = false;
            testObjects.uiInputModule.enableGamepadInput = false;
            testObjects.uiInputModule.enableJoystickInput = true;

            var joystick = InputSystem.InputSystem.AddDevice<Joystick>();
            joystick.MakeCurrent();
            // We can pass null into the cancelButton field of this function since it is not explicitely defined in the Joystick class.
            yield return InputDeviceUINavigationChecks(testObjects, joystick, joystick.stick, joystick.trigger, null);
        }

        [UnityTest]
        public IEnumerator UIGamepadNavigation()
        {
            var testObjects = SetupUIScene(true);

            // Enable device input
            testObjects.uiInputModule.enableXRInput = false;
            testObjects.uiInputModule.enableGamepadInput = true;
            testObjects.uiInputModule.enableJoystickInput = false;

            var gamepad = InputSystem.InputSystem.AddDevice<Gamepad>();
            gamepad.MakeCurrent();
            yield return InputDeviceUINavigationChecks(testObjects, gamepad, gamepad.leftStick, gamepad.buttonSouth, gamepad.buttonEast);
        }

        private IEnumerator InputDeviceUINavigationChecks(TestObjects testObjects, InputSystem.InputDevice device, StickControl stick, ButtonControl submitButton, ButtonControl cancelButton)
        {
            // First we check inputs here to make sure we are working with populated fields
            // The cancelButton is optional since the Joystick class does not have it, so skip that check and handle below
            Assert.That(device, Is.Not.Null);
            Assert.That(stick, Is.Not.Null);
            Assert.That(submitButton, Is.Not.Null);
            
            // Setup gamepad input with new input system
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            // Create actions.
            var map = new InputActionMap("map");
            asset.AddActionMap(map);
            var moveAction = map.AddAction("move", type: InputActionType.Value);
            var submitAction = map.AddAction("submit", type: InputActionType.Button);
            var cancelAction = map.AddAction("cancel", type: InputActionType.Button);

            // Create bindings.
            moveAction.AddBinding(stick);
            submitAction.AddBinding(submitButton);
            if (cancelButton != null)
                cancelAction.AddBinding(cancelButton);

            map.Enable();

            if (device is Gamepad)
                Assert.That(Gamepad.current, Is.SameAs(device));
            else if (device is Joystick)
                Assert.That(Joystick.current, Is.SameAs(device));

            var leftUIReceiver = testObjects.leftUIReceiver;
            var rightUIReceiver = testObjects.rightUIReceiver;
            var globalUIReceiver = testObjects.globalUIReceiver;

            // Set left object as selected
            testObjects.eventSystem.SetSelectedGameObject(leftUIReceiver.gameObject);
            yield return null;

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Select));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(0));

            Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(leftUIReceiver.gameObject));

            globalUIReceiver.Reset();
            leftUIReceiver.Reset();
            rightUIReceiver.Reset();

            // Move right on gamepad.
            Set(stick, Vector2.right);
            yield return null;

            // should have moved from left to right object.
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Move));
            Assert.That(leftUIReceiver.events[1].type, Is.EqualTo(EventType.Deselect));

            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Select));

            Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(rightUIReceiver.gameObject));

            globalUIReceiver.Reset();
            leftUIReceiver.Reset();
            rightUIReceiver.Reset();

            // Check Submit button press
            Set(stick, Vector2.zero);
            Set(submitButton, 1);
            yield return null;
            Set(submitButton, 0);
            yield return null;

            // make sure only right is receiving submit
            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Submit));
            Assert.That(leftUIReceiver.events, Is.Empty);

            globalUIReceiver.Reset();
            leftUIReceiver.Reset();
            rightUIReceiver.Reset();

            if (cancelButton != null)
            {
                // Check Cancel button press
                Set(stick, Vector2.zero);
                Set(cancelButton, 1);
                yield return null;
                Set(cancelButton, 0);
                yield return null;

                // make sure only right is receiving cancel
                Assert.That(rightUIReceiver.events, Has.Count.EqualTo(1));
                Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Cancel));
                Assert.That(leftUIReceiver.events, Is.Empty);

                globalUIReceiver.Reset();
                leftUIReceiver.Reset();
                rightUIReceiver.Reset();
            }

            // Move back to left on gamepad.
            Set(stick, Vector2.left);
            yield return null;

            // should have moved from left to right object.
            Assert.That(rightUIReceiver.events, Has.Count.EqualTo(2));
            Assert.That(rightUIReceiver.events[0].type, Is.EqualTo(EventType.Move));
            Assert.That(rightUIReceiver.events[1].type, Is.EqualTo(EventType.Deselect));

            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Select));

            Assert.That(testObjects.eventSystem.currentSelectedGameObject, Is.SameAs(leftUIReceiver.gameObject));

            globalUIReceiver.Reset();
            leftUIReceiver.Reset();
            rightUIReceiver.Reset();

            // Check Submit button pressed
            Set(stick, Vector2.zero);
            Set(submitButton, 1);
            yield return null;
            Set(submitButton, 0);
            yield return null;

            // Make sure only left is receiving submit
            Assert.That(leftUIReceiver.events, Has.Count.EqualTo(1));
            Assert.That(leftUIReceiver.events[0].type, Is.EqualTo(EventType.Submit));
            Assert.That(rightUIReceiver.events, Is.Empty);

            globalUIReceiver.Reset();
            leftUIReceiver.Reset();
            rightUIReceiver.Reset();
        }

        [TearDown]
        public override void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
            base.TearDown();
        }
#else
        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }
#endif

    }
}


