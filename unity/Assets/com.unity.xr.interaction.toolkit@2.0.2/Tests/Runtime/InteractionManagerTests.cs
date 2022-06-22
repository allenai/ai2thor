using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class InteractionManagerTests
    {
        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        [Test]
        public void InteractorRegisteredOnEnable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            IXRInteractor registeredInteractor = null;
            manager.interactorRegistered += args => registeredInteractor = args.interactorObject;
            var interactor = TestUtilities.CreateDirectInteractor();

            var interactors = new List<IXRInteractor>();
            manager.GetRegisteredInteractors(interactors);
            Assert.That(interactors, Is.EqualTo(new[] { interactor }));
            Assert.That(registeredInteractor, Is.SameAs(interactor));
            Assert.That(manager.IsRegistered((IXRInteractor)interactor), Is.True);
        }

        [Test]
        public void InteractorUnregisteredOnDisable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            IXRInteractor unregisteredInteractor = null;
            manager.interactorUnregistered += args => unregisteredInteractor = args.interactorObject;
            var interactor = TestUtilities.CreateDirectInteractor();
            interactor.enabled = false;

            var interactors = new List<IXRInteractor>();
            manager.GetRegisteredInteractors(interactors);
            Assert.That(interactors, Is.Empty);
            Assert.That(unregisteredInteractor, Is.SameAs(interactor));
            Assert.That(manager.IsRegistered((IXRInteractor)interactor), Is.False);
        }

        [Test]
        public void InteractorRegistrationEventsInvoked()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();
            IXRInteractor registeredInteractor = null;
            IXRInteractor unregisteredInteractor = null;
            interactor.registered += args => registeredInteractor = args.interactorObject;
            interactor.unregistered += args => unregisteredInteractor = args.interactorObject;
            interactor.enabled = false;

            var interactors = new List<IXRInteractor>();
            manager.GetRegisteredInteractors(interactors);
            Assert.That(interactors, Is.Empty);
            Assert.That(unregisteredInteractor, Is.SameAs(interactor));

            interactor.enabled = true;

            manager.GetRegisteredInteractors(interactors);
            Assert.That(interactors, Is.EqualTo(new[] { interactor }));
            Assert.That(registeredInteractor, Is.SameAs(interactor));
        }

        [Test]
        public void InteractableRegisteredOnEnable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            IXRInteractable registeredInteractable = null;
            manager.interactableRegistered += args => registeredInteractable = args.interactableObject;
            var interactable = TestUtilities.CreateGrabInteractable();

            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));
            Assert.That(registeredInteractable, Is.SameAs(interactable));
            Assert.That(manager.IsRegistered((IXRInteractable)interactable), Is.True);
        }

        [Test]
        public void InteractableUnregisteredOnDisable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            IXRInteractable unregisteredInteractable = null;
            manager.interactableUnregistered += args => unregisteredInteractable = args.interactableObject;
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.enabled = false;

            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactables, Is.Empty);
            Assert.That(unregisteredInteractable, Is.SameAs(interactable));
            Assert.That(manager.IsRegistered((IXRInteractable)interactable), Is.False);
        }

        [Test]
        public void InteractableRegistrationEventsInvoked()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            IXRInteractable registeredInteractable = null;
            IXRInteractable unregisteredInteractable = null;
            interactable.registered += args => registeredInteractable = args.interactableObject;
            interactable.unregistered += args => unregisteredInteractable = args.interactableObject;
            interactable.enabled = false;

            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactables, Is.Empty);
            Assert.That(unregisteredInteractable, Is.SameAs(interactable));

            interactable.enabled = true;

            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));
            Assert.That(registeredInteractable, Is.SameAs(interactable));
        }

        [Test]
        public void InteractableRegisteredOnEnableWithColliders()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();

            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));
            Assert.That(interactable.colliders, Has.Count.EqualTo(1));
            Assert.That(manager.TryGetInteractableForCollider(interactable.colliders.First(), out var associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(interactable));
        }

        [Test]
        public void InteractableUnregistersAssociatedColliders()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var firstInteractable = TestUtilities.CreateGrabInteractable();
            var secondInteractable = TestUtilities.CreateGrabInteractable();

            manager.UnregisterInteractable((IXRInteractable)firstInteractable);
            manager.UnregisterInteractable((IXRInteractable)secondInteractable);

            // Setup so the first Interactable has both colliders, and the second Interactable has a conflicting reference
            secondInteractable.transform.SetParent(firstInteractable.transform);
            firstInteractable.colliders.Clear();
            firstInteractable.colliders.AddRange(firstInteractable.GetComponentsInChildren<Collider>());
            secondInteractable.colliders.Clear();
            secondInteractable.colliders.AddRange(secondInteractable.GetComponentsInChildren<Collider>());

            Assert.That(firstInteractable.colliders, Has.Count.EqualTo(2));
            Assert.That(secondInteractable.colliders, Has.Count.EqualTo(1));

            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[0], out var associatedInteractable), Is.False);
            Assert.That(associatedInteractable, Is.Null);
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[1], out associatedInteractable), Is.False);
            Assert.That(associatedInteractable, Is.Null);

            manager.RegisterInteractable((IXRInteractable)firstInteractable);

            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[0], out associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(firstInteractable));
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[1], out associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(firstInteractable));

            LogAssert.Expect(LogType.Warning, new Regex("A Collider used by an Interactable object is already registered with another Interactable object*"));
            manager.RegisterInteractable((IXRInteractable)secondInteractable);

            // Interactables registered afterward do not replace the existing Collider association
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[0], out associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(firstInteractable));
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[1], out associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(firstInteractable));

            manager.UnregisterInteractable((IXRInteractable)secondInteractable);

            // Interactables registered afterward should not cause the registered Collider association to be removed
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[0], out associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(firstInteractable));
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[1], out associatedInteractable), Is.True);
            Assert.That(associatedInteractable, Is.SameAs(firstInteractable));

            manager.UnregisterInteractable((IXRInteractable)firstInteractable);

            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[0], out associatedInteractable), Is.False);
            Assert.That(associatedInteractable, Is.Null);
            Assert.That(manager.TryGetInteractableForCollider(firstInteractable.colliders[1], out associatedInteractable), Is.False);
            Assert.That(associatedInteractable, Is.Null);
        }

        // Tests that Interactors and Interactables can register or unregister
        // while the Interaction Manager is iterating over the list of Interactors to process events in Update.
        [UnityTest]
        public IEnumerator ObjectsCanRegisterDuringEvents()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            var interactable = TestUtilities.CreateSimpleInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5f;

            var otherInteractor = TestUtilities.CreateDirectInteractor();
            var otherInteractable = TestUtilities.CreateSimpleInteractable();
            otherInteractor.enabled = false;
            otherInteractable.enabled = false;
            // Don't let them get in the way, both are only used to test registration
            otherInteractor.interactionLayers = 0;
            otherInteractable.interactionLayers = 0;

            // Upon Select, enable the other Interactor to have it register with the Interaction Manager during the update loop.
            // Upon Deselect, disable the other Interactor to have it unregister from the Interaction Manager during the update loop.
            interactor.selectEntered.AddListener(args =>
            {
                otherInteractor.enabled = true;
                otherInteractable.enabled = true;
            });
            interactor.selectExited.AddListener(args =>
            {
                otherInteractor.enabled = false;
                otherInteractable.enabled = false;
            });

            // Prepare controller state which will be used to cause a Select during the Interaction Manager update loop
            var controller = interactor.GetComponent<XRBaseController>();
            var controllerState = new XRControllerState(0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,false, false, false);
            controller.currentControllerState = controllerState;

            var interactors = new List<IXRInteractor>();
            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new[] { interactor }));
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));

            // Wait for Physics update to ensure the Interactable can be detected by the Interactor
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);

            // Press Grip
            controllerState.selectInteractionState = new InteractionState { active = true, activatedThisFrame = true };

            yield return null;

            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.True);

            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new IXRInteractor[] { interactor, otherInteractor }));
            Assert.That(interactables, Is.EqualTo(new IXRInteractable[] { interactable, otherInteractable }));

            // Release Grip
            controllerState.selectInteractionState = new InteractionState { active = false, deactivatedThisFrame = true };

            yield return null;

            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);

            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new[] { interactor }));
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));
        }

        // Tests that Interactors and Interactables can register or unregister
        // while the Interaction Manager is iterating over the list of Interactors to process in ProcessInteractors.
        [UnityTest]
        public IEnumerator ObjectsCanRegisterDuringProcessInteractors()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = EnablerInteractor.CreateInteractor();

            var otherInteractor = TestUtilities.CreateDirectInteractor();
            var otherInteractable = TestUtilities.CreateSimpleInteractable();
            otherInteractor.enabled = false;
            otherInteractable.enabled = false;
            // Don't let them get in the way, both are only used to test registration
            otherInteractor.interactionLayers = 0;
            otherInteractable.interactionLayers = 0;

            interactor.interactor = otherInteractor;
            interactor.interactable = otherInteractable;

            yield return null;

            var interactors = new List<IXRInteractor>();
            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new[] { interactor }));
            Assert.That(interactables, Is.Empty);

            interactor.enableBehaviors = true;

            yield return null;

            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new IXRInteractor[] { interactor, otherInteractor }));
            Assert.That(interactables, Is.EqualTo(new IXRInteractable[] { otherInteractable }));

            interactor.enableBehaviors = false;

            yield return null;

            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new[] { interactor }));
            Assert.That(interactables, Is.Empty);
        }

        // Tests that Interactors and Interactables can register or unregister
        // while the Interaction Manager is iterating over the list of Interactables to process in ProcessInteractables.
        [UnityTest]
        public IEnumerator ObjectsCanRegisterDuringProcessInteractables()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = EnablerInteractable.CreateInteractable();

            var otherInteractor = TestUtilities.CreateDirectInteractor();
            var otherInteractable = TestUtilities.CreateSimpleInteractable();
            otherInteractor.enabled = false;
            otherInteractable.enabled = false;
            // Don't let them get in the way, both are only used to test registration
            otherInteractor.interactionLayers = 0;
            otherInteractable.interactionLayers = 0;

            interactable.interactor = otherInteractor;
            interactable.interactable = otherInteractable;

            yield return null;

            var interactors = new List<IXRInteractor>();
            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.Empty);
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));

            interactable.enableBehaviors = true;

            yield return null;

            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.EqualTo(new IXRInteractor[] { otherInteractor }));
            Assert.That(interactables, Is.EqualTo(new IXRInteractable[] { interactable, otherInteractable }));

            interactable.enableBehaviors = false;

            yield return null;

            manager.GetRegisteredInteractors(interactors);
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactors, Is.Empty);
            Assert.That(interactables, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator InteractorRegistrationEventsOccurSameFrame()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();
            interactor.enabled = false;

            var numRegistered = 0;
            var numUnregistered = 0;

            interactor.registered += args =>
            {
                ++numRegistered;
            };
            interactor.unregistered += args =>
            {
                ++numUnregistered;
            };

            interactor.enabled = true;

            Assert.That(numRegistered, Is.EqualTo(1));
            Assert.That(numUnregistered, Is.EqualTo(0));

            interactor.enabled = false;

            Assert.That(numRegistered, Is.EqualTo(1));
            Assert.That(numUnregistered, Is.EqualTo(1));

            interactor.enabled = true;

            Assert.That(numRegistered, Is.EqualTo(2));
            Assert.That(numUnregistered, Is.EqualTo(1));

            yield return null;

            Assert.That(numRegistered, Is.EqualTo(2));
            Assert.That(numUnregistered, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InteractableRegistrationEventsOccurSameFrame()
        {
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateSimpleInteractable();
            interactable.enabled = false;

            var numRegistered = 0;
            var numUnregistered = 0;

            interactable.registered += args =>
            {
                ++numRegistered;
            };
            interactable.unregistered += args =>
            {
                ++numUnregistered;
            };

            interactable.enabled = true;

            Assert.That(numRegistered, Is.EqualTo(1));
            Assert.That(numUnregistered, Is.EqualTo(0));

            interactable.enabled = false;

            Assert.That(numRegistered, Is.EqualTo(1));
            Assert.That(numUnregistered, Is.EqualTo(1));

            interactable.enabled = true;

            Assert.That(numRegistered, Is.EqualTo(2));
            Assert.That(numUnregistered, Is.EqualTo(1));

            yield return null;

            Assert.That(numRegistered, Is.EqualTo(2));
            Assert.That(numUnregistered, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InteractorCanDestroy()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateDirectInteractor();

            Object.Destroy(interactor);

            yield return null;

            var interactors = new List<IXRInteractor>();
            manager.GetRegisteredInteractors(interactors);
            Assert.That(interactors, Is.Empty);
            Assert.That(manager.IsRegistered((IXRInteractor)interactor), Is.False);
        }

        [UnityTest]
        public IEnumerator InteractableCanDestroy()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();

            Object.Destroy(interactable);

            yield return null;

            var interactables = new List<IXRInteractable>();
            manager.GetRegisteredInteractables(interactables);
            Assert.That(interactables, Is.Empty);
            Assert.That(manager.IsRegistered((IXRInteractable)interactable), Is.False);
        }

        [UnityTest]
        public IEnumerator InteractionManagersInteractWithCorrectObjects()
        {
            var managerA = TestUtilities.CreateInteractionManager();
            var interactorA = TestUtilities.CreateDirectInteractor();
            interactorA.interactionManager = managerA;
            var interactableA = TestUtilities.CreateGrabInteractable();
            interactableA.interactionManager = managerA;

            var managerB = TestUtilities.CreateInteractionManager();
            var interactorB = TestUtilities.CreateDirectInteractor();
            interactorB.interactionManager = managerB;
            var interactableB = TestUtilities.CreateGrabInteractable();
            interactableB.interactionManager = managerB;

            yield return new WaitForSeconds(0.1f);

            var validTargets = new List<IXRInteractable>();
            managerA.GetValidTargets(interactorA, validTargets);
            Assert.That(validTargets, Has.Exactly(1).EqualTo(interactableA));
            managerB.GetValidTargets(interactorA, validTargets);
            Assert.That(validTargets, Is.Empty);

            Assert.That(interactorA.interactablesHovered, Has.Exactly(1).EqualTo(interactableA));
            Assert.That(interactorB.interactablesHovered, Has.Exactly(1).EqualTo(interactableB));
        }

        [Test]
        public void RegistrationListRegisterReturnsStatusChange()
        {
            var registrationList = new XRInteractionManager.RegistrationList<string>();
            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            Assert.That(registrationList.Register("A"), Is.False);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);

            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            registrationList.Flush();

            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);

            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            registrationList.Flush();

            Assert.That(registrationList.Register("A"), Is.False);
            Assert.That(registrationList.IsRegistered("A"), Is.True);
        }

        [Test]
        public void RegistrationListUnregisterReturnsStatusChange()
        {
            var registrationList = new XRInteractionManager.RegistrationList<string>();
            Assert.That(registrationList.Unregister("A"), Is.False);
            Assert.That(registrationList.IsRegistered("A"), Is.False);

            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);

            registrationList.Flush();

            Assert.That(registrationList.IsRegistered("A"), Is.False);
            Assert.That(registrationList.Unregister("A"), Is.False);

            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            registrationList.Flush();

            Assert.That(registrationList.IsRegistered("A"), Is.True);
            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);
            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);
            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);

            registrationList.Flush();

            Assert.That(registrationList.IsRegistered("A"), Is.False);
        }

        [Test]
        public void RegistrationListSnapshotUnaffectedUntilFlush()
        {
            var registrationList = new XRInteractionManager.RegistrationList<string>();
            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);
            Assert.That(registrationList.registeredSnapshot, Is.Empty);

            registrationList.Flush();

            Assert.That(registrationList.IsRegistered("A"), Is.True);
            Assert.That(registrationList.registeredSnapshot, Is.EqualTo(new[] { "A" }));

            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);
            Assert.That(registrationList.Register("B"), Is.True);
            Assert.That(registrationList.IsRegistered("B"), Is.True);
            Assert.That(registrationList.registeredSnapshot, Is.EqualTo(new[] { "A" }));

            registrationList.Flush();

            Assert.That(registrationList.IsRegistered("A"), Is.False);
            Assert.That(registrationList.IsRegistered("B"), Is.True);
            Assert.That(registrationList.registeredSnapshot, Is.EqualTo(new[] { "B" }));
        }

        [Test]
        public void RegistrationListGetRegisteredItemsIncludesAll()
        {
            var registrationList = new XRInteractionManager.RegistrationList<string>();
            var registeredItems = new List<string>();
            registrationList.GetRegisteredItems(registeredItems);
            Assert.That(registeredItems, Is.Empty);

            // Should include pending adds
            Assert.That(registrationList.Register("A"), Is.True);
            registrationList.GetRegisteredItems(registeredItems);
            Assert.That(registeredItems, Is.EqualTo(new[] { "A" }));

            registrationList.Flush();

            // Should still be equal after flush
            registrationList.GetRegisteredItems(registeredItems);
            Assert.That(registeredItems, Is.EqualTo(new[] { "A" }));

            // Should include all in the order they were registered
            Assert.That(registrationList.Register("B"), Is.True);
            registrationList.GetRegisteredItems(registeredItems);
            Assert.That(registeredItems, Is.EqualTo(new[] { "A", "B" }));

            // Should filter out pending removes from the snapshot
            Assert.That(registrationList.Unregister("A"), Is.True);
            registrationList.GetRegisteredItems(registeredItems);
            Assert.That(registeredItems, Is.EqualTo(new[] { "B" }));
        }

        [Test]
        public void RegistrationListFastPathMatches()
        {
            var registrationList = new XRInteractionManager.RegistrationList<string>();
            Assert.That(registrationList.Register("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.True);

            registrationList.Flush();

            Assert.That(registrationList.IsRegistered("A"), Is.True);
            Assert.That(registrationList.IsStillRegistered("A"), Is.True);

            Assert.That(registrationList.Unregister("A"), Is.True);
            Assert.That(registrationList.IsRegistered("A"), Is.False);
            Assert.That(registrationList.IsStillRegistered("A"), Is.False);
        }

        [Test]
        public void NestedSelectEnterEventHasCorrectEventArgs()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var firstInteractor = TestUtilities.CreateRayInteractor();
            var secondInteractor = TestUtilities.CreateDirectInteractor();
            var firstInteractable = TestUtilities.CreateSimpleInteractable();
            var secondInteractable = TestUtilities.CreateGrabInteractable();

            var interactors = new IXRSelectInteractor[4];
            var interactables = new IXRSelectInteractable[4];

            // The sequence of notifying the interactor and interactable occurs like:
            // IXRSelectInteractor.OnSelectEntering
            // IXRSelectInteractable.OnSelectEntering
            // IXRSelectInteractor.OnSelectEntered <-- During this call is when another select is triggered
            // IXRSelectInteractable.OnSelectEntered
            // This test will trigger another select event during this sequence.
            // Verify that the references to the interactor and interactable in the event args
            // does not get polluted by the second nested event that executes immediately
            // during the first event.
            firstInteractor.selectEntered.AddListener(args =>
            {
                interactors[0] = args.interactorObject;
                interactables[0] = args.interactableObject;

                // Trigger the nested event
                manager.SelectEnter((IXRSelectInteractor)secondInteractor, secondInteractable);
            });

            firstInteractable.selectEntered.AddListener(args =>
            {
                interactors[1] = args.interactorObject;
                interactables[1] = args.interactableObject;
            });

            secondInteractor.selectEntered.AddListener(args =>
            {
                interactors[2] = args.interactorObject;
                interactables[2] = args.interactableObject;
            });

            secondInteractable.selectEntered.AddListener(args =>
            {
                interactors[3] = args.interactorObject;
                interactables[3] = args.interactableObject;
            });

            // Trigger the first event
            manager.SelectEnter((IXRSelectInteractor)firstInteractor, firstInteractable);

            Assert.That(interactors[0], Is.SameAs(firstInteractor));
            Assert.That(interactables[0], Is.SameAs(firstInteractable));
            Assert.That(interactors[1], Is.SameAs(firstInteractor));
            Assert.That(interactables[1], Is.SameAs(firstInteractable));
            Assert.That(interactors[2], Is.SameAs(secondInteractor));
            Assert.That(interactables[2], Is.SameAs(secondInteractable));
            Assert.That(interactors[3], Is.SameAs(secondInteractor));
            Assert.That(interactables[3], Is.SameAs(secondInteractable));
        }

        [Test]
        public void NestedSelectExitEventHasCorrectEventArgs()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var firstInteractor = TestUtilities.CreateRayInteractor();
            var secondInteractor = TestUtilities.CreateDirectInteractor();
            var firstInteractable = TestUtilities.CreateSimpleInteractable();
            var secondInteractable = TestUtilities.CreateGrabInteractable();

            // Start selected
            manager.SelectEnter((IXRSelectInteractor)firstInteractor, firstInteractable);
            manager.SelectEnter((IXRSelectInteractor)secondInteractor, secondInteractable);

            var interactors = new IXRSelectInteractor[4];
            var interactables = new IXRSelectInteractable[4];

            // The sequence of notifying the interactor and interactable occurs like:
            // IXRSelectInteractor.OnSelectExiting
            // IXRSelectInteractable.OnSelectExiting
            // IXRSelectInteractor.OnSelectExited <-- During this call is when another select exit is triggered
            // IXRSelectInteractable.OnSelectExited
            // This test will trigger another select event during this sequence.
            // Verify that the references to the interactor and interactable in the event args
            // does not get polluted by the second nested event that executes immediately
            // during the first event.
            firstInteractor.selectExited.AddListener(args =>
            {
                interactors[0] = args.interactorObject;
                interactables[0] = args.interactableObject;

                // Trigger the nested event
                manager.SelectExit((IXRSelectInteractor)secondInteractor, secondInteractable);
            });

            firstInteractable.selectExited.AddListener(args =>
            {
                interactors[1] = args.interactorObject;
                interactables[1] = args.interactableObject;
            });

            secondInteractor.selectExited.AddListener(args =>
            {
                interactors[2] = args.interactorObject;
                interactables[2] = args.interactableObject;
            });

            secondInteractable.selectExited.AddListener(args =>
            {
                interactors[3] = args.interactorObject;
                interactables[3] = args.interactableObject;
            });

            // Trigger the first event
            manager.SelectExit((IXRSelectInteractor)firstInteractor, firstInteractable);

            Assert.That(interactors[0], Is.SameAs(firstInteractor));
            Assert.That(interactables[0], Is.SameAs(firstInteractable));
            Assert.That(interactors[1], Is.SameAs(firstInteractor));
            Assert.That(interactables[1], Is.SameAs(firstInteractable));
            Assert.That(interactors[2], Is.SameAs(secondInteractor));
            Assert.That(interactables[2], Is.SameAs(secondInteractable));
            Assert.That(interactors[3], Is.SameAs(secondInteractor));
            Assert.That(interactables[3], Is.SameAs(secondInteractable));
        }

        [Test]
        public void NestedHoverEnterEventHasCorrectEventArgs()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var firstInteractor = TestUtilities.CreateRayInteractor();
            var secondInteractor = TestUtilities.CreateDirectInteractor();
            var firstInteractable = TestUtilities.CreateSimpleInteractable();
            var secondInteractable = TestUtilities.CreateGrabInteractable();

            var interactors = new IXRHoverInteractor[4];
            var interactables = new IXRHoverInteractable[4];

            // The sequence of notifying the interactor and interactable occurs like:
            // IXRHoverInteractor.OnHoverEntering
            // IXRHoverInteractable.OnHoverEntering
            // IXRHoverInteractor.OnHoverEntered <-- During this call is when another hover is triggered
            // IXRHoverInteractable.OnHoverEntered
            // This test will trigger another hover event during this sequence.
            // Verify that the references to the interactor and interactable in the event args
            // does not get polluted by the second nested event that executes immediately
            // during the first event.
            firstInteractor.hoverEntered.AddListener(args =>
            {
                interactors[0] = args.interactorObject;
                interactables[0] = args.interactableObject;

                // Trigger the nested event
                manager.HoverEnter((IXRHoverInteractor)secondInteractor, secondInteractable);
            });

            firstInteractable.hoverEntered.AddListener(args =>
            {
                interactors[1] = args.interactorObject;
                interactables[1] = args.interactableObject;
            });

            secondInteractor.hoverEntered.AddListener(args =>
            {
                interactors[2] = args.interactorObject;
                interactables[2] = args.interactableObject;
            });

            secondInteractable.hoverEntered.AddListener(args =>
            {
                interactors[3] = args.interactorObject;
                interactables[3] = args.interactableObject;
            });

            // Trigger the first event
            manager.HoverEnter((IXRHoverInteractor)firstInteractor, firstInteractable);

            Assert.That(interactors[0], Is.SameAs(firstInteractor));
            Assert.That(interactables[0], Is.SameAs(firstInteractable));
            Assert.That(interactors[1], Is.SameAs(firstInteractor));
            Assert.That(interactables[1], Is.SameAs(firstInteractable));
            Assert.That(interactors[2], Is.SameAs(secondInteractor));
            Assert.That(interactables[2], Is.SameAs(secondInteractable));
            Assert.That(interactors[3], Is.SameAs(secondInteractor));
            Assert.That(interactables[3], Is.SameAs(secondInteractable));
        }

        [Test]
        public void NestedHoverExitEventHasCorrectEventArgs()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var firstInteractor = TestUtilities.CreateRayInteractor();
            var secondInteractor = TestUtilities.CreateDirectInteractor();
            var firstInteractable = TestUtilities.CreateSimpleInteractable();
            var secondInteractable = TestUtilities.CreateGrabInteractable();

            // Start hovered
            manager.HoverEnter((IXRHoverInteractor)firstInteractor, firstInteractable);
            manager.HoverEnter((IXRHoverInteractor)secondInteractor, secondInteractable);

            var interactors = new IXRHoverInteractor[4];
            var interactables = new IXRHoverInteractable[4];

            // The sequence of notifying the interactor and interactable occurs like:
            // IXRHoverInteractor.OnHoverExiting
            // IXRHoverInteractable.OnHoverExiting
            // IXRHoverInteractor.OnHoverExited <-- During this call is when another hover is triggered
            // IXRHoverInteractable.OnHoverExited
            // This test will trigger another hover event during this sequence.
            // Verify that the references to the interactor and interactable in the event args
            // does not get polluted by the second nested event that executes immediately
            // during the first event.
            firstInteractor.hoverExited.AddListener(args =>
            {
                interactors[0] = args.interactorObject;
                interactables[0] = args.interactableObject;

                // Trigger the nested event
                manager.HoverExit((IXRHoverInteractor)secondInteractor, secondInteractable);
            });

            firstInteractable.hoverExited.AddListener(args =>
            {
                interactors[1] = args.interactorObject;
                interactables[1] = args.interactableObject;
            });

            secondInteractor.hoverExited.AddListener(args =>
            {
                interactors[2] = args.interactorObject;
                interactables[2] = args.interactableObject;
            });

            secondInteractable.hoverExited.AddListener(args =>
            {
                interactors[3] = args.interactorObject;
                interactables[3] = args.interactableObject;
            });

            // Trigger the first event
            manager.HoverExit((IXRHoverInteractor)firstInteractor, firstInteractable);

            Assert.That(interactors[0], Is.SameAs(firstInteractor));
            Assert.That(interactables[0], Is.SameAs(firstInteractable));
            Assert.That(interactors[1], Is.SameAs(firstInteractor));
            Assert.That(interactables[1], Is.SameAs(firstInteractable));
            Assert.That(interactors[2], Is.SameAs(secondInteractor));
            Assert.That(interactables[2], Is.SameAs(secondInteractable));
            Assert.That(interactors[3], Is.SameAs(secondInteractor));
            Assert.That(interactables[3], Is.SameAs(secondInteractable));
        }

        /// <summary>
        /// Interactor that enables another Interactor and Interactable during <see cref="ProcessInteractor"/>.
        /// </summary>
        class EnablerInteractor : XRBaseInteractor
        {
            public XRBaseInteractor interactor { get; set; }

            public XRBaseInteractable interactable { get; set; }

            public bool enableBehaviors { get; set; }

            /// <inheritdoc />
            public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
            {
                base.ProcessInteractor(updatePhase);

                if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                {
                    if (interactor != null)
                        interactor.enabled = enableBehaviors;

                    if (interactable != null)
                        interactable.enabled = enableBehaviors;
                }
            }

            /// <inheritdoc />
            public override void GetValidTargets(List<IXRInteractable> targets)
            {
                targets.Clear();
            }

            public static EnablerInteractor CreateInteractor()
            {
                var interactorGO = new GameObject { name = "Enabler Interactor" };
                var interactor = interactorGO.AddComponent<EnablerInteractor>();
                return interactor;
            }
        }

        /// <summary>
        /// Interactable that enables another Interactor and Interactable during <see cref="ProcessInteractable"/>.
        /// </summary>
        class EnablerInteractable : XRBaseInteractable
        {
            public XRBaseInteractor interactor { get; set; }

            public XRBaseInteractable interactable { get; set; }

            public bool enableBehaviors { get; set; }

            /// <inheritdoc />
            public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
            {
                base.ProcessInteractable(updatePhase);

                if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                {
                    if (interactor != null)
                        interactor.enabled = enableBehaviors;

                    if (interactable != null)
                        interactable.enabled = enableBehaviors;
                }
            }

            public static EnablerInteractable CreateInteractable()
            {
                var interactableGO = new GameObject { name = "Enabler Interactable" };
                var interactable = interactableGO.AddComponent<EnablerInteractable>();
                return interactable;
            }
        }
    }
}
