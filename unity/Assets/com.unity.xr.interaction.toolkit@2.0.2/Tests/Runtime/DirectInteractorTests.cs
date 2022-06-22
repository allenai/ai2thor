using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class DirectInteractorTests
    {
        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        [UnityTest]
        public IEnumerator DirectInteractorCanHoverInteractable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            var directInteractor = TestUtilities.CreateDirectInteractor();

            yield return new WaitForFixedUpdate();
            yield return null;

            var validTargets = new List<IXRInteractable>();
            manager.GetValidTargets(directInteractor, validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            Assert.That(directInteractor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(directInteractor.hasHover, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectInteractorHandlesUnregisteredInteractable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            var directInteractor = TestUtilities.CreateDirectInteractor();

            yield return new WaitForFixedUpdate();
            yield return null;

            var validTargets = new List<IXRInteractable>();
            manager.GetValidTargets(directInteractor, validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            directInteractor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            Assert.That(directInteractor.interactablesHovered, Is.EqualTo(new[] { interactable }));

            Object.Destroy(interactable);

            yield return null;
            Assert.That(interactable == null, Is.True);

            manager.GetValidTargets(directInteractor, validTargets);
            Assert.That(validTargets, Is.Empty);
            directInteractor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);

            Assert.That(directInteractor.interactablesHovered, Is.Empty);
        }

        [UnityTest]
        public IEnumerator DirectInteractorCanSelectInteractable()
        {
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            var directInteractor = TestUtilities.CreateDirectInteractor();
            var controller = directInteractor.GetComponent<XRController>();
            var controllerRecorder = TestUtilities.CreateControllerRecorder(controller, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(float.MaxValue, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });
            controllerRecorder.isPlaying = true;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(directInteractor.interactablesSelected, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator DirectInteractorHandlesCanceledInteractable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            var directInteractor = TestUtilities.CreateDirectInteractor();
            var controller = directInteractor.GetComponent<XRController>();
            var controllerRecorder = TestUtilities.CreateControllerRecorder(controller, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(float.MaxValue, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });
            controllerRecorder.isPlaying = true;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(directInteractor.interactablesSelected, Is.EqualTo(new[] { interactable }));

            IXRSelectInteractor canceledInteractor = null;
            IXRSelectInteractable canceledInteractable = null;
            interactable.selectExited.AddListener(args => canceledInteractor = args.isCanceled ? args.interactorObject : null);
            directInteractor.selectExited.AddListener(args => canceledInteractable = args.isCanceled ? args.interactableObject : null);

            Object.Destroy(interactable);

            yield return null;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse -- overloaded Object operator==
            Assert.That(interactable == null, Is.True);
            Assert.That(directInteractor.interactablesSelected, Is.Empty);

            Assert.That(canceledInteractor, Is.SameAs(directInteractor));
            Assert.That(canceledInteractable, Is.SameAs(interactable));

            var validTargets = new List<IXRInteractable>();
            manager.GetValidTargets(directInteractor, validTargets);
            Assert.That(validTargets, Is.Empty);

            Assert.That(directInteractor.interactablesHovered, Is.Empty);
        }

        [UnityTest]
        public IEnumerator DirectInteractorCanPassToAnother()
        {
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();

            var directInteractor1 = TestUtilities.CreateDirectInteractor();
            directInteractor1.name = "directInteractor1";
            var controller1 = directInteractor1.GetComponent<XRController>();
            var controllerRecorder1 = TestUtilities.CreateControllerRecorder(controller1, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    false, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.1f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.2f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.3f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });

            var directInteractor2 = TestUtilities.CreateDirectInteractor();
            directInteractor2.name = "directInteractor2";
            var controller2 = directInteractor2.GetComponent<XRController>();
            var controllerRecorder2 = TestUtilities.CreateControllerRecorder(controller2, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    false, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.1f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    false, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.2f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.3f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });

            controllerRecorder1.isPlaying = true;
            controllerRecorder2.isPlaying = true;

            yield return new WaitForSeconds(0.1f);

            // directInteractor1 grabs the interactable
            Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { directInteractor1 }), "In first frame, controller 1 should grab the interactable.");

            // Wait for the proper interaction that signifies the handoff
            yield return new WaitForSeconds(0.2f);

            // directInteractor2 grabs the interactable from directInteractor1
            Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { directInteractor2 }), "In second frame, controller 2 should grab the interactable.");
        }

        [UnityTest]
        public IEnumerator DirectInteractorReportsValidTargetWhenInteractableRegisteredAfterContact()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            interactor.selectActionTrigger = XRBaseControllerInteractor.InputTriggerType.State;
            var controller = interactor.GetComponent<XRController>();
            var controllerRecorder = TestUtilities.CreateControllerRecorder(controller, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(float.MaxValue, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });
            controllerRecorder.isPlaying = true;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(interactor.interactablesSelected, Is.EqualTo(new[] { interactable }));
            Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { interactor }));

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            // Disable the Interactable so it will be removed as a valid target.
            interactable.enabled = false;

            yield return null;

            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactable.interactorsSelecting, Is.Empty);

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);

            // Re-enable the Interactable. It should not be required to leave and enter the collider to be selected again.
            interactable.enabled = true;

            yield return null;

            Assert.That(interactor.interactablesSelected, Is.EqualTo(new[] { interactable }));
            Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { interactor }));

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator DirectInteractorUpdatesStayedCollidersOnDisablingInteractableCollider()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(interactor, Is.Not.Null);

            // Check that the interactor is hovering the interactable.
            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);

            // Disable the collider component.
            interactable.GetComponent<SphereCollider>().enabled = false;
            yield return new WaitForFixedUpdate();
            yield return null;

            // Since interactable's collider is disabled, it should not show up in the list of valid targets.
            interactor.GetValidTargets(validTargets);

            Assert.That(validTargets, Is.Empty);
            Assert.That(interactor.interactablesHovered, Is.Empty);

            yield return new WaitForFixedUpdate();

            // Additional test: re-enable the collider component, the interactor should re-hover it.
            interactable.GetComponent<SphereCollider>().enabled = true;

            yield return new WaitForFixedUpdate();
            yield return null;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectInteractorUpdatesStayedCollidersOnDeactivatingInteractableObject()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();
            yield return null;

            // Check that the interactable is a valid target of and can be hovered by the interactor.
            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);

            // De-activate the interactable's gameObject.
            interactable.gameObject.SetActive(false);

            // Test to make sure that the interactable gameObject would no longer be a valid target
            // after its gameObject is set to disable.
            yield return new WaitForFixedUpdate();
            yield return null;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);
            Assert.That(interactor.hasHover, Is.False);

            yield return new WaitForFixedUpdate();

            // Re-enabling the interactable gameObject, the interactor should re-hover it.
            interactable.gameObject.SetActive(true);

            yield return new WaitForFixedUpdate();
            yield return null;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectInteractorUpdatesStayedCollidersOnDestroyingInteractableObject()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();
            yield return null;

            // Check that the interactable is a valid target of and can be hovered by the interactor.
            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);

            // Destroy the interactable's gameObject.
            Object.Destroy(interactable.gameObject);

            // Test to make sure that the interactable gameObject would no longer be a valid target.
            yield return new WaitForFixedUpdate();
            yield return null;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);
            Assert.That(interactor.hasHover, Is.False);

            yield return new WaitForFixedUpdate();

            // Additional test: re-spawning the interactable gameObject, the interactor should re-hover it.
            interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();
            yield return null;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);
        }
    }
}