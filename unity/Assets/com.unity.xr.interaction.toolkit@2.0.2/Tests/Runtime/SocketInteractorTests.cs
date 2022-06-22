using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class SocketInteractorTests
    {
        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        [UnityTest]
        public IEnumerator SocketInteractorCanSelectInteractable()
        {
            TestUtilities.CreateInteractionManager();
            var socketInteractor = TestUtilities.CreateSocketInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(socketInteractor.interactablesSelected, Is.EqualTo(new[] { interactable }));
            Assert.That(socketInteractor.hasSelection, Is.True);
        }

        [UnityTest]
        public IEnumerator SocketInteractorHandlesUnregisteredInteractable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var socketInteractor = TestUtilities.CreateSocketInteractor();
            var selectedInteractable = TestUtilities.CreateGrabInteractable();
            var hoveredInteractable = TestUtilities.CreateGrabInteractable();
            // Move to a position so it won't be the closest to ensure selectedInteractable is the one selected
            hoveredInteractable.transform.localPosition = new Vector3(0.001f, 0f, 0f);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(socketInteractor.interactablesSelected, Is.EqualTo(new[] { selectedInteractable }));

            var validTargets = new List<IXRInteractable>();
            manager.GetValidTargets(socketInteractor, validTargets);
            Assert.That(validTargets, Is.EquivalentTo(new[] { selectedInteractable, hoveredInteractable }));
            socketInteractor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EquivalentTo(new[] { selectedInteractable, hoveredInteractable }));

            Assert.That(socketInteractor.interactablesHovered, Is.EquivalentTo(new[] { selectedInteractable, hoveredInteractable }));

            Object.Destroy(hoveredInteractable);

            yield return null;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse -- Object operator==
            Assert.That(hoveredInteractable == null, Is.True);

            manager.GetValidTargets(socketInteractor, validTargets);
            Assert.That(validTargets, Is.EquivalentTo(new[] { selectedInteractable }));
            socketInteractor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EquivalentTo(new[] { selectedInteractable }));

            Assert.That(socketInteractor.interactablesHovered, Is.EquivalentTo(new[] { selectedInteractable }));

            Object.Destroy(selectedInteractable);

            yield return null;
            Assert.That(selectedInteractable == null, Is.True);
            Assert.That(socketInteractor.interactablesSelected, Is.Empty);
        }

        [UnityTest]
        public IEnumerator SocketInteractorCanDirectInteractorStealSingleModeInteractableFromSocket()
        {
            TestUtilities.CreateInteractionManager();
            var socketInteractor = TestUtilities.CreateSocketInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();
            Assert.That(interactable.selectMode, Is.EqualTo(InteractableSelectMode.Single));

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

            // Only need to wait one frame since the single select mode should cause the socket selection to first exit
            // before being selected by the direct interactor.
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(socketInteractor.interactablesSelected, Is.Empty);
            Assert.That(directInteractor.interactablesSelected, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator SocketInteractorCanDirectInteractorStealMultipleModeInteractableFromSocket()
        {
            TestUtilities.CreateInteractionManager();
            var socketInteractor = TestUtilities.CreateSocketInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.selectMode = InteractableSelectMode.Multiple;

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

            // May need to wait two frames.
            // One for XRInteractionManager to enter the selection of the socket and direct interactors.
            // Two for XRInteractionManager.ClearInteractorSelection to exit the selection of the socket
            // since the CanSelect should now want to clear the selection due to no longer being the exclusive
            // interactor selecting.
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;

            Assert.That(socketInteractor.interactablesSelected, Is.Empty);
            Assert.That(directInteractor.interactablesSelected, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator SocketInteractorReportsValidTargetWhenInteractableRegisteredAfterContact()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateSocketInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(interactor.interactablesSelected, Is.EqualTo(new[] { interactable }));
            Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { interactor }));

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EquivalentTo(new[] { interactable }));

            // Disable the Interactable so it will be removed as a valid target
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
            Assert.That(validTargets, Is.EquivalentTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator SocketInteractorUpdatesStayedCollidersOnDisablingInteractableCollider()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateSocketInteractor();
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
        public IEnumerator SocketInteractorUpdatesStayedCollidersOnDeactivatingInteractableObject()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateSocketInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            // Set the socket's recycleDelayTime to 0 instead of the default 1s.
            interactor.recycleDelayTime = 0f;

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

            // Additional test: re-enabling the interactable gameObject, the interactor should re-hover it.
            interactable.gameObject.SetActive(true);

            yield return new WaitForFixedUpdate();
            yield return null;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
            Assert.That(interactor.hasHover, Is.True);
        }

        [UnityTest]
        public IEnumerator SocketInteractorUpdatesStayedCollidersOnDestroyingInteractableObject()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateSocketInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            // Set the socket's recycleDelayTime to 0 instead of the default 1s.
            interactor.recycleDelayTime = 0f;

            yield return new WaitForFixedUpdate();
            yield return null;

            // Check that the interactable is a ValidTarget of and can be hovered by the interactor.
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