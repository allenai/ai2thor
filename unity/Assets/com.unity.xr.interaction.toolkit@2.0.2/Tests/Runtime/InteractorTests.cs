using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class InteractorTests
    {
        static readonly Type[] s_ContactInteractors =
        {
            typeof(XRDirectInteractor),
            typeof(XRSocketInteractor),
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        [UnityTest]
        public IEnumerator ContactInteractorTargetStaysValidWhenTouchingAnyCollider([ValueSource(nameof(s_ContactInteractors))] Type interactorType)
        {
            // This tests that an Interactable will stay as a valid target as long as
            // the Direct and Socket Interactor is touching any Collider associated with the Interactable,
            // and remains so if only some (but not all) of the Interactable colliders leaves.
            var manager = TestUtilities.CreateInteractionManager();
            XRBaseInteractor interactor = null;
            if (interactorType == typeof(XRDirectInteractor))
                interactor = TestUtilities.CreateDirectInteractor();
            else if (interactorType == typeof(XRSocketInteractor))
                interactor = TestUtilities.CreateSocketInteractor();

            Assert.That(interactor, Is.Not.Null);

            var triggerCollider = interactor.GetComponent<SphereCollider>();
            Assert.That(triggerCollider, Is.Not.Null);
            Assert.That(triggerCollider.isTrigger, Is.True);

            var interactable = TestUtilities.CreateGrabInteractable();
            // Prevent the Interactable from being selected to allow the object to be moved freely
            interactable.interactionLayers = 0;
            var sphereCollider = interactable.GetComponent<SphereCollider>();
            sphereCollider.center = Vector3.zero;
            sphereCollider.radius = 0.5f;
            Assert.That(sphereCollider, Is.Not.Null);
            interactable.transform.position = Vector3.forward * 10f;

            // Create another Collider to have as part of the Interactable
            var boxColliderTransform = new GameObject("Box Collider", typeof(BoxCollider)).transform;
            boxColliderTransform.SetParent(interactable.transform);
            boxColliderTransform.localPosition = Vector3.right;
            boxColliderTransform.localRotation = Quaternion.identity;
            var boxCollider = boxColliderTransform.GetComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;

            interactable.colliders.Clear();
            interactable.colliders.Add(sphereCollider);
            interactable.colliders.Add(boxCollider);

            interactable.enabled = false;
            interactable.enabled = true;

            Assert.That(manager.TryGetInteractableForCollider(sphereCollider, out var sphereColliderInteractable), Is.True);
            Assert.That(sphereColliderInteractable, Is.EqualTo(interactable));
            Assert.That(manager.TryGetInteractableForCollider(boxCollider, out var boxColliderInteractable), Is.True);
            Assert.That(boxColliderInteractable, Is.EqualTo(interactable));

            yield return null;
            yield return new WaitForFixedUpdate();

            var directOverlaps = Physics.OverlapSphere(triggerCollider.transform.position, triggerCollider.radius, -1, QueryTriggerInteraction.Ignore);
            Assert.That(directOverlaps, Is.Empty);

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);

            // Move the Interactable to the Direct Interactor so that it overlaps both colliders
            interactable.transform.position = Vector3.left * 0.5f;

            yield return new WaitForFixedUpdate();
            yield return null;

            directOverlaps = Physics.OverlapSphere(triggerCollider.transform.position, triggerCollider.radius, -1, QueryTriggerInteraction.Ignore);
            Assert.That(directOverlaps, Is.EquivalentTo(new Collider[] { sphereCollider, boxCollider }));

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            // Move the Interactable some so one of the colliders leaves
            interactable.transform.position = Vector3.left * 2f;

            yield return new WaitForFixedUpdate();
            yield return null;

            directOverlaps = Physics.OverlapSphere(triggerCollider.transform.position, triggerCollider.radius, -1, QueryTriggerInteraction.Ignore);
            Assert.That(directOverlaps, Is.EquivalentTo(new Collider[] { boxCollider }));

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            // Move the Interactable some so the other collider is the one being hovered
            // to test that colliders can re-enter after previously exiting
            interactable.transform.position = Vector3.right * 1f;

            yield return new WaitForFixedUpdate();
            yield return null;

            directOverlaps = Physics.OverlapSphere(triggerCollider.transform.position, triggerCollider.radius, -1, QueryTriggerInteraction.Ignore);
            Assert.That(directOverlaps, Is.EquivalentTo(new Collider[] { sphereCollider }));

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            // Move the Interactable so all colliders exits the Direct Interactor
            interactable.transform.position = Vector3.forward * 10f;

            yield return new WaitForFixedUpdate();
            yield return null;

            directOverlaps = Physics.OverlapSphere(triggerCollider.transform.position, triggerCollider.radius, -1, QueryTriggerInteraction.Ignore);
            Assert.That(directOverlaps, Is.Empty);

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ContactInteractorCullsValidTargetsWhenInteractableUnregistered([ValueSource(nameof(s_ContactInteractors))] Type interactorType)
        {
            // This will test that the Direct and Socket Interactor will remove an unregistered Interactable
            // from its valid targets list.
            TestUtilities.CreateInteractionManager();
            XRBaseInteractor interactor = null;
            if (interactorType == typeof(XRDirectInteractor))
                interactor = TestUtilities.CreateDirectInteractor();
            else if (interactorType == typeof(XRSocketInteractor))
                interactor = TestUtilities.CreateSocketInteractor();

            Assert.That(interactor, Is.Not.Null);

            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            interactable.enabled = false;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ContactInteractorCullsValidTargetsUponRegistering([ValueSource(nameof(s_ContactInteractors))] Type interactorType)
        {
            // This will test that the Direct and Socket Interactor will update the list of valid targets
            // to exclude those that have been unregistered during the time when the Interactor
            // was not subscribed to the unregister event.
            TestUtilities.CreateInteractionManager();
            XRBaseInteractor interactor = null;
            if (interactorType == typeof(XRDirectInteractor))
                interactor = TestUtilities.CreateDirectInteractor();
            else if (interactorType == typeof(XRSocketInteractor))
                interactor = TestUtilities.CreateSocketInteractor();

            Assert.That(interactor, Is.Not.Null);

            var interactable = TestUtilities.CreateGrabInteractable();

            yield return new WaitForFixedUpdate();

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            interactor.enabled = false;
            interactable.enabled = false;
            interactor.enabled = true;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ContactInteractorUpdatesValidTargetsForPreviouslyUnassociatedCollidersWhenInteractableRegistered([ValueSource(nameof(s_ContactInteractors))] Type interactorType)
        {
            // This will test that the Direct and Socket Interactor will maintain the list of all entered Colliders
            // so that if any of them later become associated with a registered Interactable,
            // that Interactable will become a valid target.
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.transform.position = Vector3.forward * 10f;
            interactable.enabled = false;

            yield return new WaitForFixedUpdate();

            XRBaseInteractor interactor = null;
            if (interactorType == typeof(XRDirectInteractor))
                interactor = TestUtilities.CreateDirectInteractor();
            else if (interactorType == typeof(XRSocketInteractor))
                interactor = TestUtilities.CreateSocketInteractor();

            Assert.That(interactor, Is.Not.Null);

            interactable.transform.position = Vector3.zero;

            yield return new WaitForFixedUpdate();

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);

            interactable.enabled = true;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator ContactInteractorUpdatesValidTargetsForPreviouslyUnassociatedCollidersUponRegistering([ValueSource(nameof(s_ContactInteractors))] Type interactorType)
        {
            // This will test that the Direct and Socket Interactor will later associate the collider when
            // the Interactable is registered during the time when the Interactor
            // was not subscribed to the register event.
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.transform.position = Vector3.forward * 10f;
            interactable.enabled = false;

            yield return new WaitForFixedUpdate();

            XRBaseInteractor interactor = null;
            if (interactorType == typeof(XRDirectInteractor))
                interactor = TestUtilities.CreateDirectInteractor();
            else if (interactorType == typeof(XRSocketInteractor))
                interactor = TestUtilities.CreateSocketInteractor();

            Assert.That(interactor, Is.Not.Null);

            interactor.enabled = false;
            interactable.transform.position = Vector3.zero;

            yield return new WaitForFixedUpdate();

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty);

            interactable.enabled = true;
            interactor.enabled = true;

            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator ContactInteractorIgnoresDisabledCollidersWhenSortingValidTargets([ValueSource(nameof(s_ContactInteractors))] Type interactorType)
        {
            // This will test that the Direct and Socket Interactor will ignore disabled colliders
            // when sorting to find the closest interactable to select.

            // Create Interaction Manager
            TestUtilities.CreateInteractionManager();

            // Interactable 1 has a single sphere collider centered on its local origin.
            // The sphere collider has a radius of 1.
            var interactable1 = TestUtilities.CreateGrabInteractable();
            interactable1.transform.position = new Vector3(-1.1f, 0, 0);
            interactable1.enabled = false;
            interactable1.name = "interactable1";

            // Interactable 1 has a single sphere collider centered on its local origin.
            // The sphere collider has a radius of 1. It is also disabled.
            var interactable2 = TestUtilities.CreateGrabInteractable();
            interactable2.GetComponent<SphereCollider>().enabled = false;
            interactable2.transform.position = new Vector3(1, 0, 0);
            interactable2.enabled = false;
            interactable2.name = "interactable2";

            yield return new WaitForFixedUpdate();

            XRBaseInteractor interactor = null;
            if (interactorType == typeof(XRDirectInteractor))
                interactor = TestUtilities.CreateDirectInteractor();
            else if (interactorType == typeof(XRSocketInteractor))
                interactor = TestUtilities.CreateSocketInteractor();

            Assert.That(interactor, Is.Not.Null);

            interactor.enabled = false;

            yield return new WaitForFixedUpdate();

            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.Empty, $"All interactors and interactables are disabled, so there should be no valid targets.");

            interactor.enabled = true;
            interactable1.enabled = true;
            interactable2.enabled = true;

            yield return new WaitForFixedUpdate();

            // Since interactable2's collider is disabled, it should not show up in the list of valid targets.
            interactor.GetValidTargets(validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable1 }));
        }
    }
}
