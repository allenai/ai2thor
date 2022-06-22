// There is a problem with upm-ci package test builds where the Unity.InputSystem.TestFramework assembly
// is not included, which causes this test to fail to compile. To allow these tests to be run,
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

using System.Collections;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Processors;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class LocomotionInputTests : InputTestFixture
    {
        enum ForwardSource
        {
            Default,
            Camera,
            Controller,
        }

        [TearDown]
        public override void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MoveInDefaultDirection()
        {
            return MoveInDirection(ForwardSource.Default);
        }

        [UnityTest]
        public IEnumerator MoveInCameraDirection()
        {
            return MoveInDirection(ForwardSource.Camera);
        }

        [UnityTest]
        public IEnumerator MoveInControllerDirection()
        {
            return MoveInDirection(ForwardSource.Controller);
        }

        IEnumerator MoveInDirection(ForwardSource forwardSource)
        {
            // Create a stick control to serve as the input action source for the move provider
            var gamepad = InputSystem.InputSystem.AddDevice<Gamepad>();

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var actionMap = asset.AddActionMap("Locomotion");
            var action = actionMap.AddAction("Move",
                InputActionType.Value,
                "<Gamepad>/leftStick");

            var xrOrigin = TestUtilities.CreateXROrigin();
            var rigTransform = xrOrigin.Origin.transform;
            var cameraTransform = xrOrigin.Camera.transform;

            // Rotate the camera to face a different direction than rig forward to test
            // that the move provider will move with respect to a selected forward object.
            cameraTransform.Rotate(0f, 45f, 0f);
            var cameraForward = cameraTransform.forward;
            Assert.That(rigTransform.forward, Is.Not.EqualTo(cameraForward).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Create a controller object to serve as another forward source
            var controllerGO = new GameObject("Controller");
            controllerGO.transform.SetParent(xrOrigin.CameraFloorOffsetObject.transform, false);
            controllerGO.transform.Rotate(0f, -45f, 0f);
            var controllerForward = controllerGO.transform.forward;
            Assert.That(rigTransform.forward, Is.Not.EqualTo(controllerForward).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Config continuous move on XR Origin
            var locoSys = xrOrigin.gameObject.AddComponent<LocomotionSystem>();
            locoSys.xrOrigin = xrOrigin;
            var moveProvider = xrOrigin.gameObject.AddComponent<ActionBasedContinuousMoveProvider>();
            moveProvider.system = locoSys;
            moveProvider.leftHandMoveAction = new InputActionProperty(action);
            moveProvider.moveSpeed = 1f;

            switch (forwardSource)
            {
                case ForwardSource.Default:
                    break;
                case ForwardSource.Camera:
                    moveProvider.forwardSource = xrOrigin.Camera.transform;
                    break;
                case ForwardSource.Controller:
                    moveProvider.forwardSource = controllerGO.transform;
                    break;
                default:
                    Assert.Fail($"Unhandled {nameof(ForwardSource)}={forwardSource}");
                    break;
            }

            // See Script Execution Order diagram https://docs.unity3d.com/Manual/ExecutionOrder.html
            // This test will begin after Update() during the yield null/yield WaitForSeconds/yield StartCoroutine stage.
            // The move provider will process input during Update() of the next frame, and scale the move based on Time.deltaTime.
            // After yielding for 1 second with the stick pushed forward, the stick will be released back to center.
            // The move provider will process the release during Update() of the next frame, and should not apply any more movement.

            // Partially push stick directly forward.
            // This tests that the move speed will be scaled by the input magnitude.
            var input = new Vector2(0f, 0.5f);
            var processedInput = new StickDeadzoneProcessor().Process(input);
            Set(gamepad.leftStick, input);
            var startTime = Time.time;

            yield return new WaitForSeconds(1f);

            var actualPosition = rigTransform.position;
            var actualDistance = Vector3.Distance(Vector3.zero, actualPosition);
            var expectedDistance = processedInput.magnitude * moveProvider.moveSpeed * (Time.time - startTime);
            Assert.That(actualDistance, Is.EqualTo(expectedDistance).Within(1e-5f));

            switch (forwardSource)
            {
                case ForwardSource.Default:
                case ForwardSource.Camera:
                    Assert.That(actualPosition, Is.EqualTo(cameraForward * expectedDistance).Using(Vector3ComparerWithEqualsOperator.Instance));
                    break;
                case ForwardSource.Controller:
                    Assert.That(actualPosition, Is.EqualTo(controllerForward * expectedDistance).Using(Vector3ComparerWithEqualsOperator.Instance));
                    break;
                default:
                    Assert.Fail($"Unhandled {nameof(ForwardSource)}={forwardSource}");
                    break;
            }

            // Stop moving
            Set(gamepad.leftStick, Vector2.zero);

            yield return new WaitForSeconds(0.1f);

            // ReSharper disable Unity.InefficientPropertyAccess -- Property value accessed after yield
            Assert.That(Vector3.Distance(actualPosition, rigTransform.position), Is.EqualTo(0f));
            // ReSharper restore Unity.InefficientPropertyAccess
        }

        [UnityTest]
        public IEnumerator SmoothTurn()
        {
            // Create a stick control to serve as the input action source for the turn provider
            var gamepad = InputSystem.InputSystem.AddDevice<Gamepad>();

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var actionMap = asset.AddActionMap("Locomotion");
            var action = actionMap.AddAction("Turn",
                InputActionType.Value,
                "<Gamepad>/rightStick");

            var xrOrigin = TestUtilities.CreateXROrigin();
            var rigTransform = xrOrigin.Origin.transform;

            // Config continuous turn on XR Origin
            var locoSys = xrOrigin.gameObject.AddComponent<LocomotionSystem>();
            locoSys.xrOrigin = xrOrigin;
            var turnProvider = xrOrigin.gameObject.AddComponent<ActionBasedContinuousTurnProvider>();
            turnProvider.system = locoSys;
            turnProvider.leftHandTurnAction = new InputActionProperty(action);
            turnProvider.turnSpeed = 60f;

            // Partially push stick directly right.
            // This tests that the turn speed will be scaled by the input magnitude.
            var input = new Vector2(0.5f, 0f);
            var processedInput = new StickDeadzoneProcessor().Process(input);
            Set(gamepad.rightStick, input);
            var startTime = Time.time;

            yield return new WaitForSeconds(1f);

            var turnAmount = processedInput.magnitude * turnProvider.turnSpeed * (Time.time - startTime);
            var actualRotation = rigTransform.rotation;
            Assert.That(actualRotation, Is.EqualTo(Quaternion.Euler(0f, turnAmount, 0f)).Using(QuaternionEqualityComparer.Instance));

            // Stop turning
            Set(gamepad.rightStick, Vector2.zero);

            yield return new WaitForSeconds(0.1f);

            // ReSharper disable Unity.InefficientPropertyAccess -- Property value accessed after yield
            Assert.That(actualRotation, Is.EqualTo(rigTransform.rotation).Using(QuaternionEqualityComparer.Instance));
            // ReSharper restore Unity.InefficientPropertyAccess
        }
    }
}

#endif
