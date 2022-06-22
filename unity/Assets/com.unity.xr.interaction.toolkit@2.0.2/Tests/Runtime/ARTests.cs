// See comment in LocomotionInputTests.cs about ENABLE_INPUT_SYSTEM_TESTFRAMEWORK_TESTS
#if AR_FOUNDATION_PRESENT && ENABLE_INPUT_SYSTEM_TESTFRAMEWORK_TESTS

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.AR;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class ARTests : InputTestFixture
    {
        const int k_TestPlaneLayer = 9;

        XROrigin m_XROrigin;

        Camera m_Camera;

        static GameObject s_TestPlane;

        static readonly List<GestureTouchesUtility.TouchInputSource> s_TouchInputSources =
            new List<GestureTouchesUtility.TouchInputSource>
            {
                GestureTouchesUtility.TouchInputSource.Mock,
                GestureTouchesUtility.TouchInputSource.Enhanced,
            };

        static GameObject CreateTestPlane()
        {
            s_TestPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            s_TestPlane.name = "Plane";
            s_TestPlane.layer = k_TestPlaneLayer;
            return s_TestPlane;
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            TestUtilities.CreateInteractionManager();

            var xrOriginGO = ObjectFactory.CreateGameObject("XR Origin",
                typeof(XROrigin), typeof(ARPlaneManager), typeof(ARRaycastManager));
            m_XROrigin = xrOriginGO.GetComponent<XROrigin>();

            var cameraGO = ObjectFactory.CreateGameObject("AR Camera", typeof(Camera), typeof(ARGestureInteractor));
            cameraGO.tag = "MainCamera";
            m_Camera = cameraGO.GetComponent<Camera>();

            m_Camera.transform.position = new Vector3(0f, 5f, -5f);
            m_Camera.transform.LookAt(Vector3.zero, Vector3.up);
            m_Camera.transform.SetParent(m_XROrigin.transform);
            m_XROrigin.Camera = m_Camera;

            GestureTouchesUtility.touchInputSource = GestureTouchesUtility.TouchInputSource.Mock;
            EnhancedTouchSupport.Enable();
        }

        [TearDown]
        public override void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();

            GestureTouchesUtility.touchInputSource = GestureTouchesUtility.defaultTouchInputSource;
            EnhancedTouchSupport.Disable();

            base.TearDown();
        }

        IEnumerator SimulateTouches(Vector2[] touchesBegan, Vector2[] touchesMoved, Vector2[] touchesEnded, GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            Debug.Assert(touchesBegan.Length >= touchesMoved.Length);
            Debug.Assert(touchesBegan.Length >= touchesEnded.Length);
            Debug.Assert(touchInputSource == GestureTouchesUtility.TouchInputSource.Mock || touchInputSource == GestureTouchesUtility.TouchInputSource.Enhanced);

            // Give gesture recognizer a chance to process gesture input
            IEnumerator DoYield()
            {
                yield return null;
            }

            if (touchesBegan.Length > 0)
            {
                GestureTouchesUtility.mockTouches.Clear();
                for (var i = 0; i < touchesBegan.Length; ++i)
                {
                    // For Input System, while a touch is ongoing, it must have a non-zero ID different from
                    // all other ongoing touches.
                    var id = i + 1;
                    if (touchInputSource == GestureTouchesUtility.TouchInputSource.Mock)
                    {
                        GestureTouchesUtility.mockTouches.Add(
                            new MockTouch
                            {
                                phase = TouchPhase.Began,
                                deltaPosition = Vector2.zero,
                                position = touchesBegan[i],
                                fingerId = id,
                            });
                    }
                    else // Enhanced
                    {
                        BeginTouch(id, touchesBegan[i], true);
                    }
                }

                yield return DoYield();
            }

            if (touchesMoved.Length > 0)
            {
                // Break move into steps to track
                const int moveSegments = 10;
                for (var iSegments = 0; iSegments < moveSegments; ++iSegments)
                {
                    // Track initial move from start location
                    GestureTouchesUtility.mockTouches.Clear();
                    for (var i = 0; i < touchesMoved.Length; ++i)
                    {
                        var deltaMove = (touchesMoved[i] - touchesBegan[i]) / moveSegments;
                        var position = touchesBegan[i] + deltaMove * (iSegments + 1);
                        var id = i + 1;
                        if (touchInputSource == GestureTouchesUtility.TouchInputSource.Mock)
                        {
                            GestureTouchesUtility.mockTouches.Add(
                                new MockTouch
                                {
                                    phase = TouchPhase.Moved,
                                    deltaPosition = deltaMove,
                                    position = position,
                                    fingerId = id,
                                });
                        }
                        else // Enhanced
                        {
                            MoveTouch(id, position, deltaMove, true);
                        }
                    }

                    yield return DoYield();
                }
            }

            if (touchesEnded.Length > 0)
            {
                GestureTouchesUtility.mockTouches.Clear();
                for (var i = 0; i < touchesEnded.Length; ++i)
                {
                    var deltaPosition = touchesEnded[i] - touchesBegan[i];
                    var id = i + 1;
                    if (touchInputSource == GestureTouchesUtility.TouchInputSource.Mock)
                    {
                        GestureTouchesUtility.mockTouches.Add(
                            new MockTouch
                            {
                                phase = TouchPhase.Ended,
                                deltaPosition = deltaPosition,
                                position = touchesEnded[i],
                                fingerId = id,
                            });
                    }
                    else // Enhanced
                    {
                        EndTouch(id, touchesEnded[i], deltaPosition, true);
                    }
                }

                yield return DoYield();
            }

            GestureTouchesUtility.mockTouches.Clear();
        }

        static void SetupTouches(GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            GestureTouchesUtility.touchInputSource = touchInputSource;
            if (touchInputSource == GestureTouchesUtility.TouchInputSource.Enhanced)
                InputSystem.InputSystem.AddDevice<Touchscreen>();
        }

        [UnityTest]
        public IEnumerator GestureInteractor_SelectPlacementInteractable_CreatesGO([ValueSource(nameof(s_TouchInputSources))] GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            SetupTouches(touchInputSource);

            var interactable = ObjectFactory.CreateGameObject("ARPlacementInteractable", typeof(ARPlacementInteractable));
            var placementInteractable = interactable.GetComponent<ARPlacementInteractable>();
            placementInteractable.placementPrefab = new GameObject();
            placementInteractable.fallbackLayerMask = 1 << k_TestPlaneLayer;
            CreateTestPlane();

            // This makes sure that the simulated touch is over the plane regardless of screen size.
            Vector3 screenPos = m_Camera.WorldToScreenPoint(interactable.transform.position);

            var objectPlacedCalled = 0;
            ARObjectPlacementEventArgs objectPlacedArgs = null;
            placementInteractable.objectPlaced.AddListener(args =>
            {
                ++objectPlacedCalled;
                objectPlacedArgs = args;
            });

            yield return SimulateTouches(
                new Vector2[] { screenPos },
                new Vector2[] { },
                new Vector2[] { screenPos },
                touchInputSource);

            Assert.That(objectPlacedCalled, Is.EqualTo(1));
            Assert.That(objectPlacedArgs, Is.Not.Null);
            Assert.That(objectPlacedArgs.placementInteractable, Is.SameAs(placementInteractable));
            Assert.That(objectPlacedArgs.placementObject, Is.Not.Null);
            Assert.That(objectPlacedArgs.placementObject.transform.parent, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator GestureInteractor_SelectSelectionInteractable_Selects([ValueSource(nameof(s_TouchInputSources))] GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            SetupTouches(touchInputSource);

            var interactable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var selectionInteractable = interactable.AddComponent<ARSelectionInteractable>();

            yield return SimulateTouches(
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                new Vector2[] { },
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                touchInputSource);

            // Give interaction system a chance to process resulting interaction
            yield return null;

            Assert.That(selectionInteractable.isSelected, Is.True);
        }

        [UnityTest]
        public IEnumerator GestureInteractor_DragTranslateInteractable_TranslatesAlongPlane([ValueSource(nameof(s_TouchInputSources))] GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            SetupTouches(touchInputSource);

            var interactable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var selectionInteractable = interactable.AddComponent<ARSelectionInteractable>();
            var translationInteractable = interactable.AddComponent<ARTranslationInteractable>();
            translationInteractable.fallbackLayerMask = 1 << k_TestPlaneLayer;
            var parent = new GameObject("PlacementAnchor");
            interactable.transform.parent = parent.transform;
            CreateTestPlane();

            var originalPosition = interactable.transform.position;

            // Select
            yield return SimulateTouches(
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                new Vector2[] { },
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                touchInputSource);

            // Give interaction system a chance to process resulting interaction
            yield return null;

            Assert.That(selectionInteractable.isSelected, Is.True);

            // Drag
            const float translateDelta = 200f;
            yield return SimulateTouches(
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                new Vector2[] { new Vector2(Screen.width / 2 + translateDelta, Screen.height / 2 + translateDelta) },
                new Vector2[] { new Vector2(Screen.width / 2 + translateDelta, Screen.height / 2 + translateDelta) },
                touchInputSource);

            Assert.That(selectionInteractable.isSelected, Is.True);

            // Translates along the XZ plane
            Assert.That(interactable.transform.position.x, Is.GreaterThan(originalPosition.x));
            Assert.That(interactable.transform.position.z, Is.GreaterThan(originalPosition.z));
            Assert.That(interactable.transform.position.y, Is.EqualTo(originalPosition.y).Within(1e-6f));
        }

        [UnityTest]
        public IEnumerator GestureInteractor_PinchScaleInteractable_Scales([ValueSource(nameof(s_TouchInputSources))] GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            SetupTouches(touchInputSource);

            var interactable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interactable.AddComponent<ARSelectionInteractable>();
            interactable.AddComponent<ARScaleInteractable>();
            var originalScale = interactable.transform.localScale;

            // Select
            yield return SimulateTouches(
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                new Vector2[] { },
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                touchInputSource);

            // Pinch
            const float pinchStartOffset = 10f;
            const float pinchDelta = 200f;
            yield return SimulateTouches(
                new Vector2[] {
                    new Vector2(Screen.width / 2 - pinchStartOffset, Screen.height / 2),
                    new Vector2(Screen.width / 2 + pinchStartOffset, Screen.height / 2)
                },
                new Vector2[] {
                    new Vector2(Screen.width / 2 - pinchDelta, Screen.height / 2),
                    new Vector2(Screen.width / 2 + pinchDelta, Screen.height / 2)
                },
                new Vector2[] {
                    new Vector2(Screen.width / 2 - pinchDelta, Screen.height / 2),
                    new Vector2(Screen.width / 2 + pinchDelta, Screen.height / 2)
                },
                touchInputSource);

            Assert.That(interactable.transform.localScale.x, Is.GreaterThan(originalScale.x));
            Assert.That(interactable.transform.localScale.y, Is.GreaterThan(originalScale.y));
            Assert.That(interactable.transform.localScale.z, Is.GreaterThan(originalScale.z));
        }

        [UnityTest]
        public IEnumerator GestureInteractor_TwistRotationInteractable_Rotates([ValueSource(nameof(s_TouchInputSources))] GestureTouchesUtility.TouchInputSource touchInputSource)
        {
            SetupTouches(touchInputSource);

            var interactable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            interactable.AddComponent<ARSelectionInteractable>();
            interactable.AddComponent<ARRotationInteractable>();
            var originalRotation = interactable.transform.rotation;

            // Select
            yield return SimulateTouches(
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                new Vector2[] { },
                new Vector2[] { new Vector2(Screen.width / 2, Screen.height / 2) },
                touchInputSource);

            // Twist
            const float startOffsetDelta = 100f;
            const float rotateOffsetDelta = 200f;
            yield return SimulateTouches(
                new Vector2[] {
                    new Vector2(Screen.width / 2 - startOffsetDelta, Screen.height / 2),
                    new Vector2(Screen.width / 2 + startOffsetDelta, Screen.height / 2) },
                new Vector2[]
                {
                    new Vector2(Screen.width / 2 - startOffsetDelta + rotateOffsetDelta, Screen.height / 2 - rotateOffsetDelta),
                    new Vector2(Screen.width / 2 + startOffsetDelta - rotateOffsetDelta, Screen.height / 2 + rotateOffsetDelta)
                },
                new Vector2[]
                {
                    new Vector2(Screen.width / 2 - startOffsetDelta + rotateOffsetDelta, Screen.height / 2 - rotateOffsetDelta),
                    new Vector2(Screen.width / 2 + startOffsetDelta - rotateOffsetDelta, Screen.height / 2 + rotateOffsetDelta)
                },
                touchInputSource);

            Assert.That(interactable.transform.rotation, Is.Not.EqualTo(originalRotation).Using(QuaternionEqualityComparer.Instance));
        }
    }
}

#endif
