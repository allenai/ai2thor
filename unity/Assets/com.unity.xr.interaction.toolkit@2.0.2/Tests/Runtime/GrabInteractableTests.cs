using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class GrabInteractableTests
    {
        static readonly XRBaseInteractable.MovementType[] s_MovementTypes =
        {
            XRBaseInteractable.MovementType.VelocityTracking,
            XRBaseInteractable.MovementType.Kinematic,
            XRBaseInteractable.MovementType.Instantaneous,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        static void DisableDelayProperties(XRGrabInteractable grabInteractable)
        {
            grabInteractable.velocityDamping = 1f;
            grabInteractable.velocityScale = 1f;
            grabInteractable.angularVelocityDamping = 1f;
            grabInteractable.angularVelocityScale = 1f;
            grabInteractable.attachEaseInTime = 0f;
            var rigidbody = grabInteractable.GetComponent<Rigidbody>();
            rigidbody.maxAngularVelocity = float.PositiveInfinity;
        }

        static IEnumerator WaitForSteadyState(XRBaseInteractable.MovementType movementType)
        {
            yield return null;

            if (movementType == XRBaseInteractable.MovementType.VelocityTracking)
                yield return new WaitForFixedUpdate();

            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator CenteredObjectWithAttachTransformMovesToExpectedPosition([ValueSource(nameof(s_MovementTypes))] XRBaseInteractable.MovementType movementType)
        {
            // Create Grab Interactable at some arbitrary point
            var grabInteractableGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grabInteractableGO.name = "Grab Interactable";
            grabInteractableGO.transform.localPosition = new Vector3(1f, 2f, 3f);
            grabInteractableGO.transform.localRotation = Quaternion.identity;
            var boxCollider = grabInteractableGO.GetComponent<BoxCollider>();
            var rigidbody = grabInteractableGO.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            var grabInteractable = grabInteractableGO.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = movementType;
            DisableDelayProperties(grabInteractable);
            // Set the Attach Transform to the back upper-right corner of the cube
            // to test an attach transform different from the transform position (which is also its center).
            var grabInteractableAttach = new GameObject("Grab Interactable Attach").transform;
            var attachOffset = new Vector3(0.5f, 0.5f, -0.5f);
            grabInteractableAttach.SetParent(grabInteractable.transform);
            grabInteractableAttach.localPosition = attachOffset;
            grabInteractableAttach.localRotation = Quaternion.identity;
            grabInteractable.attachTransform = grabInteractableAttach;
            // The built-in Cube resource has its center at the center of the cube.
            var centerOffset = Vector3.zero;

            Assert.That(grabInteractable.attachPointCompatibilityMode, Is.EqualTo(XRGrabInteractable.AttachPointCompatibilityMode.Default));

            // Wait for physics update to ensure the Rigidbody is stable and center of mass has been calculated
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(new Vector3(1f, 2f, 3f) + attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(boxCollider, Is.Not.Null);
            Assert.That(boxCollider.center, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.centerOfMass, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.worldCenterOfMass, Is.EqualTo(new Vector3(1f, 2f, 3f) + centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Create Interactor at some arbitrary point away from the Interactable
            var interactor = TestUtilities.CreateMockInteractor();
            var targetPosition = Vector3.zero;

            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(interactor.hasSelection, Is.False);
            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactor.attachTransform, Is.Not.Null);
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Set valid so it will be selected next frame by the Interaction Manager
            interactor.validTargets.Add(grabInteractable);

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move the attach transform on the Interactor after being grabbed
            targetPosition = new Vector3(5f, 5f, 5f);
            interactor.attachTransform.position = targetPosition;

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move the attach transform on the Interactable to the back lower-right corner of the cube
            attachOffset = new Vector3(0.5f, -0.5f, -0.5f);
            grabInteractable.attachTransform.localPosition = attachOffset;

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator CenteredObjectWithoutAttachTransformMovesToExpectedPosition([ValueSource(nameof(s_MovementTypes))] XRBaseInteractable.MovementType movementType)
        {
            // Create Grab Interactable at some arbitrary point
            var grabInteractableGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grabInteractableGO.name = "Grab Interactable";
            grabInteractableGO.transform.localPosition = new Vector3(1f, 2f, 3f);
            grabInteractableGO.transform.localRotation = Quaternion.identity;
            var boxCollider = grabInteractableGO.GetComponent<BoxCollider>();
            var rigidbody = grabInteractableGO.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            var grabInteractable = grabInteractableGO.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = movementType;
            DisableDelayProperties(grabInteractable);
            // Keep the Attach Transform null to use the transform itself (which is also its center).
            var attachOffset = Vector3.zero;
            grabInteractable.attachTransform = null;
            // The built-in Cube resource has its center at the center of the cube.
            var centerOffset = Vector3.zero;

            Assert.That(grabInteractable.attachPointCompatibilityMode, Is.EqualTo(XRGrabInteractable.AttachPointCompatibilityMode.Default));

            // Wait for physics update to ensure the Rigidbody is stable and center of mass has been calculated
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(grabInteractable.attachTransform, Is.Null);
            Assert.That(grabInteractable.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(boxCollider, Is.Not.Null);
            Assert.That(boxCollider.center, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.centerOfMass, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.worldCenterOfMass, Is.EqualTo(new Vector3(1f, 2f, 3f) + centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Create Interactor at some arbitrary point away from the Interactable
            var interactor = TestUtilities.CreateMockInteractor();
            var targetPosition = Vector3.zero;

            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(interactor.hasSelection, Is.False);
            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactor.attachTransform, Is.Not.Null);
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Set valid so it will be selected next frame by the Interaction Manager
            interactor.validTargets.Add(grabInteractable);

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform, Is.Null);
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move the attach transform on the Interactor after being grabbed
            targetPosition = new Vector3(5f, 5f, 5f);
            interactor.attachTransform.position = targetPosition;

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform, Is.Null);
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator NonCenteredObjectMovesToExpectedPosition([ValueSource(nameof(s_MovementTypes))] XRBaseInteractable.MovementType movementType)
        {
            // Create a cube mesh with the pivot position at the bottom center
            // rather than the built-in Cube resource which has its center at the
            // center of the cube.
            var cubePrimitiveGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubePrimitiveMesh = cubePrimitiveGO.GetComponent<MeshFilter>().sharedMesh;
            var centerOffset = new Vector3(0f, 0.5f, 0f);
            var offCenterMesh = new Mesh
            {
                vertices = cubePrimitiveMesh.vertices.Select(vertex => vertex + centerOffset).ToArray(),
                triangles = cubePrimitiveMesh.triangles.ToArray(),
                normals = cubePrimitiveMesh.normals.ToArray(),
            };
            cubePrimitiveGO.SetActive(false);
            Object.Destroy(cubePrimitiveGO);

            // Create Grab Interactable at some arbitrary point
            var grabInteractableGO = new GameObject("Grab Interactable");
            grabInteractableGO.transform.localPosition = new Vector3(1f, 2f, 3f);
            grabInteractableGO.transform.localRotation = Quaternion.identity;
            var meshFilter = grabInteractableGO.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = offCenterMesh;
            grabInteractableGO.AddComponent<MeshRenderer>();
            var boxCollider = grabInteractableGO.AddComponent<BoxCollider>();
            var rigidbody = grabInteractableGO.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            var grabInteractable = grabInteractableGO.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = movementType;
            DisableDelayProperties(grabInteractable);
            // Set the Attach Transform to the back upper-right corner of the cube
            // to test an attach transform different from both the transform position and center.
            var grabInteractableAttach = new GameObject("Grab Interactable Attach").transform;
            var attachOffset = new Vector3(0.5f, 1f, -0.5f);
            grabInteractableAttach.SetParent(grabInteractable.transform);
            grabInteractableAttach.localPosition = attachOffset;
            grabInteractableAttach.localRotation = Quaternion.identity;
            grabInteractable.attachTransform = grabInteractableAttach;

            Assert.That(grabInteractable.attachPointCompatibilityMode, Is.EqualTo(XRGrabInteractable.AttachPointCompatibilityMode.Default));

            // Wait for physics update to ensure the Rigidbody is stable and center of mass has been calculated
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(new Vector3(1f, 2f, 3f) + attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(boxCollider.center, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.centerOfMass, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.worldCenterOfMass, Is.EqualTo(new Vector3(1f, 2f, 3f) + centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Create Interactor at some arbitrary point away from the Interactable
            var interactor = TestUtilities.CreateMockInteractor();
            var targetPosition = Vector3.zero;

            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(interactor.hasSelection, Is.False);
            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactor.attachTransform, Is.Not.Null);
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Set valid so it will be selected next frame by the Interaction Manager
            interactor.validTargets.Add(grabInteractable);

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move the attach transform on the Interactor after being grabbed
            targetPosition = new Vector3(5f, 5f, 5f);
            interactor.attachTransform.position = targetPosition;

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move the attach transform on the Interactable to the back lower-right corner of the cube
            attachOffset = new Vector3(0.5f, 0f, -0.5f);
            grabInteractable.attachTransform.localPosition = attachOffset;

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator NonCenteredObjectRotatesToExpectedOrientation([ValueSource(nameof(s_MovementTypes))] XRBaseInteractable.MovementType movementType)
        {
            // Create a cube mesh with the pivot position at the bottom center
            // rather than the built-in Cube resource which has its center at the
            // center of the cube.
            var cubePrimitiveGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubePrimitiveMesh = cubePrimitiveGO.GetComponent<MeshFilter>().sharedMesh;
            var centerOffset = new Vector3(0f, 0.5f, 0f);
            var offCenterMesh = new Mesh
            {
                vertices = cubePrimitiveMesh.vertices.Select(vertex => vertex + centerOffset).ToArray(),
                triangles = cubePrimitiveMesh.triangles.ToArray(),
                normals = cubePrimitiveMesh.normals.ToArray(),
            };
            cubePrimitiveGO.SetActive(false);
            Object.Destroy(cubePrimitiveGO);

            // Create Grab Interactable at some arbitrary point
            var grabInteractableGO = new GameObject("Grab Interactable");
            grabInteractableGO.transform.localPosition = new Vector3(1f, 2f, 3f);
            grabInteractableGO.transform.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            var meshFilter = grabInteractableGO.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = offCenterMesh;
            grabInteractableGO.AddComponent<MeshRenderer>();
            var boxCollider = grabInteractableGO.AddComponent<BoxCollider>();
            var rigidbody = grabInteractableGO.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            var grabInteractable = grabInteractableGO.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = movementType;
            DisableDelayProperties(grabInteractable);
            // Set the Attach Transform to the back upper-right corner of the cube
            // to test an attach transform different from both the transform position and center.
            var grabInteractableAttach = new GameObject("Grab Interactable Attach").transform;
            var attachOffset = new Vector3(0.5f, 1f, -0.5f);
            grabInteractableAttach.SetParent(grabInteractable.transform);
            grabInteractableAttach.localPosition = attachOffset;
            var attachRotation = Quaternion.LookRotation(Vector3.left, Vector3.forward);
            grabInteractableAttach.rotation = attachRotation;
            grabInteractable.attachTransform = grabInteractableAttach;

            Assert.That(grabInteractable.attachPointCompatibilityMode, Is.EqualTo(XRGrabInteractable.AttachPointCompatibilityMode.Default));

            // Wait for physics update to ensure the Rigidbody is stable and center of mass has been calculated
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            // The Grab Interactable is rotated 180 degrees around the y-axis,
            // so the Attach Transform becomes the front upper-left corner of the cube from the perspective of the world axes,
            // so the position will end up at (0.5, 3, 3.5).
            var worldAttachOffset = new Vector3(-0.5f, 1f, 0.5f);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(new Vector3(1f, 2f, 3f) + worldAttachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(boxCollider.center, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.centerOfMass, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.worldCenterOfMass, Is.EqualTo(new Vector3(1f, 2f, 3f) + centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Create Interactor at some arbitrary point away from the Interactable
            var interactor = TestUtilities.CreateMockInteractor();
            var targetPosition = Vector3.zero;
            var targetRotation = Quaternion.identity;

            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(interactor.hasSelection, Is.False);
            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactor.attachTransform, Is.Not.Null);
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(targetRotation).Using(QuaternionEqualityComparer.Instance));

            // Set valid so it will be selected next frame by the Interaction Manager
            interactor.validTargets.Add(grabInteractable);

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            // When the Grab Interactable moves to align with the Interactor's Attach Transform at the origin,
            // the cube should end up with the transform pivot on the right face from the perspective of the world axes
            // to have the Attach Transform there pointing forward.
            var expectedRotation = Quaternion.LookRotation(Vector3.down, Vector3.left);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(targetRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(new Vector3(1f, -0.5f, -0.5f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(expectedRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(targetRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(new Vector3(1f, -0.5f, -0.5f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(expectedRotation).Using(QuaternionEqualityComparer.Instance));
        }
        
        [UnityTest]
        public IEnumerator TrackRotationDisabledObjectMovesAndRotatesToExpectedPositionAndOrientation([ValueSource(nameof(s_MovementTypes))] XRBaseInteractable.MovementType movementType)
        {
            // Create a cube mesh with the pivot position at the bottom center
            // rather than the built-in Cube resource which has its center at the
            // center of the cube.
            var cubePrimitiveGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubePrimitiveMesh = cubePrimitiveGO.GetComponent<MeshFilter>().sharedMesh;
            var centerOffset = new Vector3(0f, 0.5f, 0f);
            var offCenterMesh = new Mesh
            {
                vertices = cubePrimitiveMesh.vertices.Select(vertex => vertex + centerOffset).ToArray(),
                triangles = cubePrimitiveMesh.triangles.ToArray(),
                normals = cubePrimitiveMesh.normals.ToArray(),
            };
            cubePrimitiveGO.SetActive(false);
            Object.Destroy(cubePrimitiveGO);

            // Create Grab Interactable at some arbitrary point
            var grabInteractableGO = new GameObject("Grab Interactable");
            grabInteractableGO.transform.localPosition = new Vector3(1f, 2f, 3f);
            grabInteractableGO.transform.localRotation = Quaternion.identity;
            var meshFilter = grabInteractableGO.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = offCenterMesh;
            grabInteractableGO.AddComponent<MeshRenderer>();
            var boxCollider = grabInteractableGO.AddComponent<BoxCollider>();
            var rigidbody = grabInteractableGO.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            var grabInteractable = grabInteractableGO.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = movementType;
            DisableDelayProperties(grabInteractable);
            // Set the Attach Transform to the back upper-right corner of the cube
            // to test an attach transform different from both the transform position and center.
            var grabInteractableAttach = new GameObject("Grab Interactable Attach").transform;
            var attachOffset = new Vector3(0.5f, 1f, -0.5f);
            var attachRotation = Quaternion.Euler(0f, 45f, 0f);
            grabInteractableAttach.SetParent(grabInteractable.transform);
            grabInteractableAttach.localPosition = attachOffset;
            grabInteractableAttach.localRotation = attachRotation;
            grabInteractable.attachTransform = grabInteractableAttach;
            // Disable track rotation
            grabInteractable.trackRotation = false;

            Assert.That(grabInteractable.attachPointCompatibilityMode, Is.EqualTo(XRGrabInteractable.AttachPointCompatibilityMode.Default));

            // Wait for physics update to ensure the Rigidbody is stable and center of mass has been calculated
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(new Vector3(1f, 2f, 3f) + attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(attachRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(boxCollider.center, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.centerOfMass, Is.EqualTo(centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.worldCenterOfMass, Is.EqualTo(new Vector3(1f, 2f, 3f) + centerOffset).Using(Vector3ComparerWithEqualsOperator.Instance));

            // Create Interactor at some arbitrary point away from the Interactable
            var interactor = TestUtilities.CreateMockInteractor();
            var targetPosition = Vector3.zero;
            var interactorAttachTransformRotation = Quaternion.identity;

            yield return null;

            Assert.That(grabInteractable.isSelected, Is.False);
            Assert.That(interactor.hasSelection, Is.False);
            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactor.attachTransform, Is.Not.Null);
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(interactorAttachTransformRotation).Using(QuaternionEqualityComparer.Instance));

            // Set valid so it will be selected next frame by the Interaction Manager
            interactor.validTargets.Add(grabInteractable);

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(attachRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(interactorAttachTransformRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move and rotate the attach transform on the Interactor after being grabbed
            targetPosition = new Vector3(5f, 5f, 5f);
            interactorAttachTransformRotation = Quaternion.Euler(0f, 90f, 0f);
            interactor.attachTransform.position = targetPosition;
            interactor.attachTransform.rotation = interactorAttachTransformRotation;

            yield return WaitForSteadyState(movementType);

            // The expected object and its attached transform rotation remains unchanged since track rotation is disabled
            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(attachRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(interactorAttachTransformRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));

            // Move the attach transform on the Interactable to the back lower-right corner of the cube
            attachOffset = new Vector3(0.5f, 0f, -0.5f);
            grabInteractable.attachTransform.localPosition = attachOffset;

            yield return WaitForSteadyState(movementType);

            Assert.That(grabInteractable.isSelected, Is.True);
            Assert.That(grabInteractable.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.attachTransform.rotation, Is.EqualTo(attachRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(grabInteractable.transform.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(grabInteractable.transform.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
            Assert.That(interactor.attachTransform.position, Is.EqualTo(targetPosition).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(interactor.attachTransform.rotation, Is.EqualTo(interactorAttachTransformRotation).Using(QuaternionEqualityComparer.Instance));
            Assert.That(rigidbody.position, Is.EqualTo(targetPosition - attachOffset).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(rigidbody.rotation, Is.EqualTo(Quaternion.identity).Using(QuaternionEqualityComparer.Instance));
        }
    }
}