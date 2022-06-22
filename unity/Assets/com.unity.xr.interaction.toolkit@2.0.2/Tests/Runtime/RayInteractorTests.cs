using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class RayInteractorTests
    {
        static readonly XRRayInteractor.LineType[] s_LineTypes =
        {
            XRRayInteractor.LineType.StraightLine,
            XRRayInteractor.LineType.ProjectileCurve,
            XRRayInteractor.LineType.BezierCurve,
        };

        static readonly XRBaseControllerInteractor.InputTriggerType[] s_SelectActionTriggers =
        {
            XRBaseControllerInteractor.InputTriggerType.State,
            XRBaseControllerInteractor.InputTriggerType.StateChange,
            XRBaseControllerInteractor.InputTriggerType.Toggle,
            XRBaseControllerInteractor.InputTriggerType.Sticky,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        [UnityTest]
        public IEnumerator RayInteractorCanHoverInteractable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5.0f;

            yield return new WaitForSeconds(0.1f);

            var validTargets = new List<IXRInteractable>();
            manager.GetValidTargets(interactor, validTargets);
            Assert.That(validTargets, Is.EqualTo(new[] { interactable }));

            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator RayInteractorCanSelectInteractable()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5.0f;

            var controller = interactor.GetComponent<XRController>();
            var controllerRecorder = TestUtilities.CreateControllerRecorder(controller, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(float.MaxValue, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });
            controllerRecorder.isPlaying = true;

            yield return new WaitForSeconds(0.1f);

            Assert.That(interactor.interactablesSelected, Is.EqualTo(new[] { interactable }));
        }

        [UnityTest]
        public IEnumerator RayInteractorCanSelectAndReleaseInteractable([ValueSource(nameof(s_SelectActionTriggers))] XRBaseControllerInteractor.InputTriggerType selectActionTrigger)
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            interactor.selectActionTrigger = selectActionTrigger;
            var interactable = TestUtilities.CreateSimpleInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5.0f;

            var controller = interactor.GetComponent<XRBaseController>();
            var controllerState = new XRControllerState(0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,false, false, false);
            controller.currentControllerState = controllerState;

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);
            Assert.That(interactor.isSelectActive, Is.False);

            // Press Grip
            controllerState.selectInteractionState = new InteractionState { active = true, activatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.True);
            Assert.That(controllerState.selectInteractionState.activatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.True);
            Assert.That(interactor.isSelectActive, Is.True);

            // Release Grip
            controllerState.selectInteractionState = new InteractionState { active = false, deactivatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(controllerState.selectInteractionState.deactivatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            switch (selectActionTrigger)
            {
                case XRBaseControllerInteractor.InputTriggerType.State:
                case XRBaseControllerInteractor.InputTriggerType.StateChange:
                    Assert.That(interactable.isSelected, Is.False);
                    Assert.That(interactor.isSelectActive, Is.False);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.Toggle:
                case XRBaseControllerInteractor.InputTriggerType.Sticky:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
            }

            // Press Grip again
            controllerState.selectInteractionState = new InteractionState { active = true, activatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.True);
            Assert.That(controllerState.selectInteractionState.activatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            switch (selectActionTrigger)
            {
                case XRBaseControllerInteractor.InputTriggerType.State:
                case XRBaseControllerInteractor.InputTriggerType.StateChange:
                case XRBaseControllerInteractor.InputTriggerType.Sticky:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.Toggle:
                    Assert.That(interactable.isSelected, Is.False);
                    Assert.That(interactor.isSelectActive, Is.False);
                    break;
            }

            // Release Grip again
            controllerState.selectInteractionState = new InteractionState { active = false, deactivatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(controllerState.selectInteractionState.deactivatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);
            Assert.That(interactor.isSelectActive, Is.False);
        }

        [UnityTest]
        public IEnumerator RayInteractorCanReleaseInteractableAfterHoverToSelectWhenNotGripping([ValueSource(nameof(s_SelectActionTriggers))] XRBaseControllerInteractor.InputTriggerType selectActionTrigger)
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            interactor.hoverToSelect = true;
            interactor.hoverTimeToSelect = 0.2f;
            interactor.selectActionTrigger = selectActionTrigger;
            var interactable = TestUtilities.CreateSimpleInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5.0f;

            var controller = interactor.GetComponent<XRBaseController>();
            var controllerState = new XRControllerState(0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,false, false, false);
            controller.currentControllerState = controllerState;

            yield return new WaitForSeconds(0.1f);

            // Hasn't met duration threshold yet
            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);
            Assert.That(interactor.isSelectActive, Is.False);

            yield return new WaitForSeconds(0.2f);

            // Hovered for long enough
            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.True);
            Assert.That(interactor.isSelectActive, Is.True);

            interactor.hoverToSelect = false;

            yield return null;

            // This table summarizes what must be done by a user to drop the Interactable
            // when hoverToSelect is disabled. It depends on whether the Select (i.e. Grip)
            // is pressed after the Interactable is automatically selected from hoverToSelect.
            // |Type       |To drop when Grip is false|To drop when Grip is true after auto Select|
            // |-----------|--------------------------|-------------------------------------------|
            // |State      |Nothing (will drop)       |Release Grip                               |
            // |StateChange|Press then Release Grip   |Release Grip                               |
            // |Toggle     |Press Grip                |Nothing (will drop since already toggled)  |
            // |Sticky     |Press then Release Grip   |Release Grip                               |
            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(interactable.isHovered, Is.True);
            switch (selectActionTrigger)
            {
                case XRBaseControllerInteractor.InputTriggerType.State:
                    Assert.That(interactable.isSelected, Is.False);
                    Assert.That(interactor.isSelectActive, Is.False);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.StateChange:
                case XRBaseControllerInteractor.InputTriggerType.Toggle:
                case XRBaseControllerInteractor.InputTriggerType.Sticky:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
            }

            // Press Grip
            controllerState.selectInteractionState = new InteractionState { active = true, activatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.True);
            Assert.That(controllerState.selectInteractionState.activatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            switch (selectActionTrigger)
            {
                case XRBaseControllerInteractor.InputTriggerType.State:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.StateChange:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.Toggle:
                    Assert.That(interactable.isSelected, Is.False);
                    Assert.That(interactor.isSelectActive, Is.False);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.Sticky:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
            }

            // Release Grip
            controllerState.selectInteractionState = new InteractionState { active = false, deactivatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(controllerState.selectInteractionState.deactivatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);
            Assert.That(interactor.isSelectActive, Is.False);
        }

        [UnityTest]
        public IEnumerator RayInteractorCanReleaseInteractableAfterHoverToSelectWhenGripping([ValueSource(nameof(s_SelectActionTriggers))] XRBaseControllerInteractor.InputTriggerType selectActionTrigger)
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            interactor.hoverToSelect = true;
            interactor.hoverTimeToSelect = 0.2f;
            interactor.selectActionTrigger = selectActionTrigger;
            var interactable = TestUtilities.CreateSimpleInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5.0f;

            var controller = interactor.GetComponent<XRBaseController>();

            var controllerState = new XRControllerState(0f, Vector3.zero, Quaternion.identity, InputTrackingState.All, false, false, false);
            controller.currentControllerState = controllerState;

            yield return new WaitForSeconds(0.1f);

            // Hasn't met duration threshold yet
            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);
            Assert.That(interactor.isSelectActive, Is.False);

            yield return new WaitForSeconds(0.2f);

            // Hovered for long enough
            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.True);
            Assert.That(interactor.isSelectActive, Is.True);

            // Press Grip
            controllerState.selectInteractionState = new InteractionState { active = true, activatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.True);
            Assert.That(controllerState.selectInteractionState.activatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.True);
            Assert.That(interactor.isSelectActive, Is.True);

            interactor.hoverToSelect = false;
            controllerState.selectInteractionState = new InteractionState { active = true };

            yield return null;

            // This table summarizes what must be done by a user to drop the Interactable
            // when hoverToSelect is disabled. It depends on whether the Select (i.e. Grip)
            // is pressed after the Interactable is automatically selected from hoverToSelect.
            // |Type       |To drop when Grip is false|To drop when Grip is true after auto Select|
            // |-----------|--------------------------|-------------------------------------------|
            // |State      |Nothing (will drop)       |Release Grip                               |
            // |StateChange|Press then Release Grip   |Release Grip                               |
            // |Toggle     |Press Grip                |Nothing (will drop since already toggled)  |
            // |Sticky     |Press then Release Grip   |Release Grip                               |
            Assert.That(controllerState.selectInteractionState.active, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            switch (selectActionTrigger)
            {
                case XRBaseControllerInteractor.InputTriggerType.State:
                case XRBaseControllerInteractor.InputTriggerType.StateChange:
                case XRBaseControllerInteractor.InputTriggerType.Sticky:
                    Assert.That(interactable.isSelected, Is.True);
                    Assert.That(interactor.isSelectActive, Is.True);
                    break;
                case XRBaseControllerInteractor.InputTriggerType.Toggle:
                    Assert.That(interactable.isSelected, Is.False);
                    Assert.That(interactor.isSelectActive, Is.False);
                    break;
            }

            // Release Grip
            controllerState.selectInteractionState = new InteractionState { active = false, deactivatedThisFrame = true };

            yield return null;

            Assert.That(controllerState.selectInteractionState.active, Is.False);
            Assert.That(controllerState.selectInteractionState.deactivatedThisFrame, Is.True);
            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.isSelected, Is.False);
            Assert.That(interactor.isSelectActive, Is.False);
        }

        [UnityTest]
        public IEnumerator ManualInteractorSelection()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.right * 5.0f;

            yield return new WaitForSeconds(0.1f);

            Assert.That(interactor.interactablesSelected, Is.Empty);

            interactor.StartManualInteraction((IXRSelectInteractable)interactable);

            yield return new WaitForSeconds(0.1f);

            Assert.That(interactor.interactablesSelected, Is.EqualTo(new[] { interactable }));

            interactor.EndManualInteraction();

            yield return new WaitForSeconds(0.1f);

            Assert.That(interactor.interactablesSelected, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RayInteractorCanResetOnInteractableDestroy()
        {
            var manager = TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.transform.position = Vector3.zero;
            interactor.transform.forward = Vector3.forward;
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.transform.position = interactor.transform.position + interactor.transform.forward * 5.0f;

            var controller = interactor.GetComponent<XRController>();
            var controllerRecorder = TestUtilities.CreateControllerRecorder(controller, (recording) =>
            {
                recording.AddRecordingFrameNonAlloc(new XRControllerState(0.0f, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
                recording.AddRecordingFrameNonAlloc(new XRControllerState(float.MaxValue, Vector3.zero, Quaternion.identity, InputTrackingState.All,
                    true, false, false));
            });
            controllerRecorder.isPlaying = true;

            yield return new WaitForSeconds(0.1f);

            Assert.That(interactor.interactablesSelected, Is.EqualTo(new[] { interactable }));

            // The above part is the same test for selecting a grab interactable.

            var attachPosition = interactor.attachTransform.position;

            Object.Destroy(interactable.gameObject);

            yield return new WaitForSeconds(0.1f);

            Assert.That(interactor.interactablesSelected, Is.Empty);
            Assert.That(interactor.attachTransform.position, Is.EqualTo(attachPosition));
        }

        [UnityTest]
        public IEnumerator RayInteractorCanLimitClosestOnly()
        {
            TestUtilities.CreateInteractionManager();
            TestUtilities.CreateXROrigin();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.xrController.enabled = false;
            interactor.hitClosestOnly = false;
            interactor.lineType = XRRayInteractor.LineType.StraightLine;

            yield return null;

            // Verify that valid targets and hover targets is empty
            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);

            Assert.That(validTargets, Is.Empty);

            Assert.That(interactor.interactablesHovered, Is.Empty);

            var nearInteractable = TestUtilities.CreateGrabInteractable();
            nearInteractable.transform.localPosition = new Vector3(0f, 0f, 10f);

            var farInteractable = TestUtilities.CreateGrabInteractable();
            farInteractable.transform.localPosition = new Vector3(0f, 0f, 20f);

            // Wait for Physics update for hit
            yield return new WaitForFixedUpdate();
            yield return null;

            // Verify that both Interactables are hit
            interactor.GetValidTargets(validTargets);

            Assert.That(validTargets, Is.EqualTo(new[] { nearInteractable, farInteractable }));

            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { nearInteractable, farInteractable }));
            Assert.That(nearInteractable.interactorsHovering, Is.EqualTo(new[] { interactor }));
            Assert.That(farInteractable.interactorsHovering, Is.EqualTo(new[] { interactor }));

            interactor.hitClosestOnly = true;

            // Wait for Valid Targets list to be updated
            yield return null;

            // Verify that only the closest Interactable is considered valid
            interactor.GetValidTargets(validTargets);

            Assert.That(validTargets, Is.EqualTo(new[] { nearInteractable }));

            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { nearInteractable }));
            Assert.That(nearInteractable.interactorsHovering, Is.EqualTo(new[] { interactor }));
            Assert.That(farInteractable.interactorsHovering, Is.Empty);
        }

        [UnityTest]
        [Ignore("Ignored for known issue where only the ray casts of the first segment where a hit occurred are captured.")]
        public IEnumerator RayInteractorHitsAllAlongCurve([ValueSource(nameof(s_LineTypes))] XRRayInteractor.LineType lineType)
        {
            TestUtilities.CreateInteractionManager();
            TestUtilities.CreateXROrigin();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.xrController.enabled = false;
            const int sampleFrequency = 5;
            interactor.sampleFrequency = sampleFrequency;
            interactor.hitClosestOnly = false;
            interactor.lineType = lineType;

            yield return null;

            // Get the Sample Points to determine where to place the Interactable Planes in different segments
            Vector3[] samplePoints = null;
            var isValid = interactor.GetLinePoints(ref samplePoints, out var samplePointsCount);

            Assert.That(isValid, Is.True);
            Assert.That(samplePoints, Is.Not.Null);
            Assert.That(samplePointsCount, Is.EqualTo(lineType == XRRayInteractor.LineType.StraightLine ? 2 : sampleFrequency));
            Assert.That(samplePoints.Length, Is.GreaterThanOrEqualTo(samplePointsCount));

            // Verify that valid targets and hover targets is empty
            var validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);

            Assert.That(validTargets, Is.Empty);

            Assert.That(interactor.interactablesHovered, Is.Empty);

            // Create two Interactable Planes such that they are within two separate line segments of the curve
            // (although for Straight Line, there is only the one line segment).
            // Add Interactable Plane near the end of the curve
            var from = samplePoints[samplePointsCount - 2];
            var to = samplePoints[samplePointsCount - 1];
            var farInteractable = CreatePlaneInteractable(Vector3.Lerp(from, to, 0.75f));

            // Add Interactable Plane near the start of the curve
            from = samplePoints[0];
            to = samplePoints[1];
            var nearInteractable = CreatePlaneInteractable(Vector3.Lerp(from, to, 0.25f));

            // Wait for Physics update for hit
            yield return new WaitForFixedUpdate();
            yield return null;

            // Verify that both Interactable Planes are hit
            interactor.GetValidTargets(validTargets);

            Assert.That(validTargets, Is.EqualTo(new[] { nearInteractable, farInteractable }));

            Assert.That(interactor.interactablesHovered, Is.EqualTo(new[] { nearInteractable, farInteractable }));

            IXRInteractable CreatePlaneInteractable(Vector3 position)
            {
                var planeGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                var interactable = planeGO.AddComponent<XRSimpleInteractable>();
                planeGO.transform.localPosition = position;
                planeGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                planeGO.transform.localScale = new Vector3(100f, 1f, 100f);
                return interactable;
            }
        }

        [UnityTest]
        public IEnumerator RayInteractorSamplePointsContinuesThroughGeometry([ValueSource(nameof(s_LineTypes))] XRRayInteractor.LineType lineType)
        {
            TestUtilities.CreateInteractionManager();
            TestUtilities.CreateXROrigin();
            var interactor = TestUtilities.CreateRayInteractor();
            interactor.xrController.enabled = false;
            interactor.transform.position = Vector3.zero;
            interactor.transform.rotation = Quaternion.Euler(-45f, 0f, 0f);
            const int sampleFrequency = 5;
            interactor.sampleFrequency = sampleFrequency;
            interactor.maxRaycastDistance = 20f;
            // These produce a curve that is the rough equivalent of the default projectile curve
            // in order to keep the assertions the same between the test cases.
            interactor.controlPointHeight = 0f;
            interactor.controlPointDistance = 22.6f;
            interactor.endPointHeight = -38.9f;
            interactor.endPointDistance = 45.2f;
            interactor.lineType = lineType;

            var lineVisual = interactor.GetComponent<XRInteractorLineVisual>();
            Assert.That(lineVisual, Is.Not.Null);
            Assert.That(lineVisual.enabled, Is.True);
            lineVisual.overrideInteractorLineLength = false;
            lineVisual.stopLineAtFirstRaycastHit = false;

            var lineRenderer = interactor.GetComponent<LineRenderer>();
            Assert.That(lineRenderer, Is.Not.Null);
            Assert.That(lineRenderer.enabled, Is.True);

            yield return null;

            Vector3[] samplePoints = null;
            var isValid = interactor.GetLinePoints(ref samplePoints, out var samplePointsCount);

            Assert.That(isValid, Is.True);
            Assert.That(samplePoints, Is.Not.Null);
            Assert.That(samplePointsCount, Is.EqualTo(lineType == XRRayInteractor.LineType.StraightLine ? 2 : sampleFrequency));
            Assert.That(samplePoints.Length, Is.GreaterThanOrEqualTo(samplePointsCount));

            // Verify that the Ray Interactor is not hitting anything
            var isHitInfoValid = interactor.TryGetHitInfo(out _, out _, out var hitPositionInLine, out var isValidTarget);

            Assert.That(isHitInfoValid, Is.False);
            Assert.That(isValidTarget, Is.False);
            Assert.That(hitPositionInLine, Is.EqualTo(0));

            yield return null;

            // The Line Renderer should match
            Assert.That(lineVisual.enabled, Is.True);
            Assert.That(lineRenderer.enabled, Is.True);
            var lineRendererPositions = new Vector3[samplePointsCount];
            // LineRenderer.GetPositions always returns 0 when running with the -batchmode flag
            if (!Application.isBatchMode)
            {
                var lineRendererPositionsCount = lineRenderer.GetPositions(lineRendererPositions);
                Assert.That(lineRendererPositionsCount, Is.EqualTo(samplePointsCount));
                Assert.That(lineRendererPositions, Is.EquivalentTo(samplePoints).Using(Vector3ComparerWithEqualsOperator.Instance));
                Assert.That(lineRenderer.positionCount, Is.EqualTo(samplePointsCount));
            }

            // Create and place a plane between sample index 1 and 2
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.localPosition = new Vector3(0f, 0f, 13f);
            plane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            plane.transform.localScale = new Vector3(10f, 1f, 10f);

            // Wait for Physics update for hit and onBeforeRender callback to be invoked in XRInteractorLineVisual
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;

            // Verify that the Ray Interactor is hitting the plane
            isHitInfoValid = interactor.TryGetHitInfo(out var hitPosition, out var hitNormal, out hitPositionInLine, out isValidTarget);

            Assert.That(isHitInfoValid, Is.True);
            Assert.That(isValidTarget, Is.False);
            Assert.That(hitPositionInLine, Is.EqualTo(lineType == XRRayInteractor.LineType.StraightLine ? 1 : 2));
            Assert.That(hitPosition.z, Is.EqualTo(plane.transform.position.z).Using(FloatEqualityComparer.Instance));
            Assert.That(hitNormal, Is.EqualTo(plane.transform.up).Using(Vector3ComparerWithEqualsOperator.Instance));

            // The sample points should continue beyond the hit to allow the
            // Line Visual behavior to render them
            Vector3[] samplePointsWithPlane = null;
            isValid = interactor.GetLinePoints(ref samplePointsWithPlane, out var samplePointsCountWithPlane);

            Assert.That(isValid, Is.True);
            Assert.That(samplePointsWithPlane, Is.Not.Null);
            Assert.That(samplePointsCountWithPlane, Is.EqualTo(lineType == XRRayInteractor.LineType.StraightLine ? 2 : sampleFrequency));
            Assert.That(samplePointsWithPlane.Length, Is.GreaterThanOrEqualTo(samplePointsCountWithPlane));
            Assert.That(samplePointsWithPlane, Is.EquivalentTo(samplePoints).Using(Vector3ComparerWithEqualsOperator.Instance));

            // The Line Renderer should still match
            Assert.That(lineVisual.enabled, Is.True);
            Assert.That(lineRenderer.enabled, Is.True);
            if (!Application.isBatchMode)
            {
                var lineRendererPositionsCount = lineRenderer.GetPositions(lineRendererPositions);
                Assert.That(lineRendererPositionsCount, Is.EqualTo(samplePointsCount));
                Assert.That(lineRendererPositions, Is.EquivalentTo(samplePoints).Using(Vector3ComparerWithEqualsOperator.Instance));
                Assert.That(lineRenderer.positionCount, Is.EqualTo(samplePointsCount));
            }

            lineVisual.stopLineAtFirstRaycastHit = true;

            // Wait for onBeforeRender callback to be invoked in XRInteractorLineVisual
            yield return null;
            yield return null;

            // The Line Renderer should now stop at the first hit
            Assert.That(lineVisual.enabled, Is.True);
            Assert.That(lineRenderer.enabled, Is.True);
            if (!Application.isBatchMode)
            {
                var lineRendererPositionsCount = lineRenderer.GetPositions(lineRendererPositions);
                Assert.That(lineRendererPositionsCount, Is.EqualTo(hitPositionInLine + 1));
                var expectedLineRendererPositions = samplePoints.Take(hitPositionInLine).ToList();
                expectedLineRendererPositions.Add(hitPosition);
                Assert.That(lineRendererPositions.Take(hitPositionInLine + 1), Is.EquivalentTo(expectedLineRendererPositions).Using(Vector3ComparerWithEqualsOperator.Instance));
                Assert.That(lineRenderer.positionCount, Is.EqualTo(hitPositionInLine + 1));
            }
        }
    }
}
