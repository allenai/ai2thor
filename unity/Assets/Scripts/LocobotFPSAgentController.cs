
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;
using RandomExtensions;
using UnityEngine.Rendering.PostProcessing;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class LocobotFPSAgentController : BaseFPSAgentController {
        protected bool applyActionNoise = true;
        protected float movementGaussianMu = 0.001f;
        protected float movementGaussianSigma = 0.005f;
        protected float rotateGaussianMu = 0.0f;
        protected float rotateGaussianSigma = 0.5f;
        protected bool allowHorizontalMovement = false;

        public LocobotFPSAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public override void InitializeBody() {
            // toggle FirstPersonCharacterCull

            VisibilityCapsule = BotVisCap;
            m_CharacterController.center = new Vector3(0, -0.45f, 0);
            m_CharacterController.radius = 0.175f;
            m_CharacterController.height = 0.9f;

            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;

            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            // camera position
            m_Camera.transform.localPosition = new Vector3(0, -0.0312f, 0);

            // camera FOV
            m_Camera.fieldOfView = 60f;

            // set camera stand/crouch local positions for Tall mode
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition + new Vector3(0, -0.2206f, 0);// smaller y offset if Bot

            // limit camera from looking too far down/up
            this.maxDownwardLookAngle = 60f;
            this.maxUpwardLookAngle = 30f;
            // this.horizonAngles = new float[] { 30.0f, 0.0f, 330.0f };
        }

        public new void Initialize(ServerAction action) {
            this.applyActionNoise = action.applyActionNoise;

            if (action.movementGaussianMu > 0.0f) {
                this.movementGaussianMu = action.movementGaussianMu;
            }

            if (action.movementGaussianSigma > 0.0f) {
                this.movementGaussianSigma = action.movementGaussianSigma;
            }

            if (action.rotateGaussianMu > 0.0f) {
                this.rotateGaussianMu = action.rotateGaussianMu;
            }

            if (action.rotateGaussianSigma > 0.0f) {
                this.rotateGaussianSigma = action.rotateGaussianSigma;
            }

#if UNITY_EDITOR
            Debug.Log("MoveNoise: " + movementGaussianMu + " mu, " + movementGaussianSigma + " sigma");
            Debug.Log("RotateNoise: " + rotateGaussianMu + " mu, " + rotateGaussianSigma + " sigma");
            Debug.Log("applynoise:" + applyActionNoise);
#endif

            base.Initialize(action);
            if (
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                == "FloorPlan_Train_Generated"
            ) {
                GenerateRoboTHOR colorChangeComponent = physicsSceneManager.GetComponent<GenerateRoboTHOR>();
                colorChangeComponent.GenerateConfig(agentTransform: transform);
            }
        }

        // reset visible objects while in editor, for debug purposes only
        private void LateUpdate() {
#if UNITY_EDITOR || UNITY_WEBGL
            VisibleSimObjPhysics = VisibleSimObjs();
#endif
        }

        public void MoveRelative(
            float? moveMagnitude = null,
            float x = 0f,
            float z = 0f,
            float noise = 0f,
            bool forceAction = false
        ) {

            if (!moveMagnitude.HasValue) {
                moveMagnitude = gridSize;
            } else if (moveMagnitude.Value <= 0f) {
                throw new InvalidOperationException("moveMagnitude must be null or >= 0.");
            }

            if (!allowHorizontalMovement && Math.Abs(x) > 0) {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }

            var moveLocal = new Vector3(x, 0, z);
            float xzMag = moveLocal.magnitude;
            if (xzMag > 1e-5f) {
                // rotate a small amount with every movement since robot doesn't always move perfectly straight
                if (this.applyActionNoise) {
                    var rotateNoise = (float)systemRandom.NextGaussian(rotateGaussianMu, rotateGaussianSigma / 2.0f);
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateNoise, 0.0f));
                }

                var moveLocalNorm = moveLocal / xzMag;
                var magnitudeWithNoise = GetMoveMagnitudeWithNoise(
                    moveMagnitude: xzMag * moveMagnitude.Value,
                    noise: noise
                );

                actionFinished(moveInDirection(
                    direction: this.transform.rotation * (moveLocalNorm * magnitudeWithNoise),
                    forceAction: forceAction
                ));
            } else {
                errorMessage = "either x or z must be != 0 for the MoveRelative action";
                actionFinished(false);
            }
        }

        // NOOP action to allow evaluation to know that the episode has finished
        public void Stop() {
            // i don't know why, but we have two no-op actions so here we go
            base.Pass();
        }

        public override void LookDown(ServerAction action) {
            // default degree increment to 30
            if (action.degrees == 0) {
                action.degrees = 30f;
            } else {
                errorMessage = "Must have degrees == 0 for now.";
                actionFinished(false);
                return;
            }

            // force the degree increment to the nearest tenths place
            // this is to prevent too small of a degree increment change that could cause float imprecision
            action.degrees = Mathf.Round(action.degrees * 10.0f) / 10.0f;

            if (!checkForUpDownAngleLimit("down", action.degrees)) {
                errorMessage = "can't look down beyond " + maxDownwardLookAngle + " degrees below the forward horizon";
                errorCode = ServerActionErrorCode.LookDownCantExceedMin;
                actionFinished(false);
                return;
            }

            base.LookDown(action);
            return;
        }

        public override void LookUp(ServerAction action) {

            // default degree increment to 30
            if (action.degrees == 0) {
                action.degrees = 30f;
            } else {
                errorMessage = "Must have degrees == 0 for now.";
                actionFinished(false);
                return;
            }

            // force the degree increment to the nearest tenths place
            // this is to prevent too small of a degree increment change that could cause float imprecision
            action.degrees = Mathf.Round(action.degrees * 10.0f) / 10.0f;

            if (!checkForUpDownAngleLimit("up", action.degrees)) {
                errorMessage = "can't look up beyond " + maxUpwardLookAngle + " degrees above the forward horizon";
                errorCode = ServerActionErrorCode.LookDownCantExceedMin;
                actionFinished(false);
                return;
            }

            base.LookUp(action);
        }

        // NOTE: This is necessary to avoid an ambiguous action between base and stochastic.
        public void Rotate(Vector3 rotation) {
            Rotate(rotation: rotation, noise: 0);
        }

        public void Rotate(Vector3 rotation, float noise, bool manualInteract = false) {
            // only default hand if not manually Interacting with things
            if (!manualInteract) {
                DefaultAgentHand();
            }

            float rotateAmountDegrees = GetRotateMagnitudeWithNoise(rotation: rotation, noise: noise);

            // multiply quaternions to apply rotation based on rotateAmountDegrees
            transform.rotation = (
                transform.rotation
                * Quaternion.Euler(new Vector3(0.0f, rotateAmountDegrees, 0.0f))
            );
            actionFinished(true);
        }

        public void RotateRight(ServerAction action) {
            float rotationAmount = this.rotateStepDegrees;

            if (action.degrees != 0.0f) {
                rotationAmount = action.degrees;
            }

            Rotate(rotation: new Vector3(0, rotationAmount, 0));
        }

        public void RotateLeft(ServerAction action) {
            float rotationAmount = this.rotateStepDegrees;

            if (action.degrees != 0.0f) {
                rotationAmount = action.degrees;
            }

            Rotate(rotation: new Vector3(0, -rotationAmount, 0));
        }

        ///////////////////////////////////////////
        //////////////// TELEPORT /////////////////
        ///////////////////////////////////////////

        [ObsoleteAttribute(message: "This action is deprecated. Call Teleport(position, ...) instead.", error: false)]
        public void Teleport(
            float x,
            float y,
            float z,
            Vector3? rotation = null,
            float? horizon = null,
            bool forceAction = false
        ) {
            Teleport(
                position: new Vector3(x, y, z), rotation: rotation, horizon: horizon, forceAction: forceAction
            );
        }

        public void Teleport(
            Vector3? position = null,
            Vector3? rotation = null,
            float? horizon = null,
            bool forceAction = false
        ) {
            base.teleport(position: position, rotation: rotation, horizon: horizon, forceAction: forceAction);
            base.assertTeleportedNearGround(targetPosition: position);
            actionFinished(success: true);
        }

        ///////////////////////////////////////////
        ////////////// TELEPORT FULL //////////////
        ///////////////////////////////////////////

        [ObsoleteAttribute(message: "This action is deprecated. Call TeleportFull(position, ...) instead.", error: false)]
        public void TeleportFull(float x, float y, float z, Vector3 rotation, float horizon, bool forceAction = false) {
            TeleportFull(
                position: new Vector3(x, y, z), rotation: rotation, horizon: horizon, forceAction: forceAction
            );
        }

        public void TeleportFull(
            Vector3 position, Vector3 rotation, float horizon, bool forceAction = false
        ) {
            base.teleportFull(position: position, rotation: rotation, horizon: horizon, forceAction: forceAction);
            base.assertTeleportedNearGround(targetPosition: position);
            actionFinished(success: true);
        }

        // TODO: This is largely copied from the PhysicsRemoteFPSAgentController. This copying was easiest
        // now as the locobot doesn't have the `standing` property which makes it a bit tricky
        // to generalize things (especially as the the `PhysicsRemoteFPSAgentController` and this controller
        // have different `Teleport` APIs. Ideally this would be refactored to be more general.
        private List<Dictionary<string, object>> getInteractablePoses(
            string objectId,
            bool markActionFinished,
            Vector3[] positions = null,
            float[] rotations = null,
            float[] horizons = null,
            float? maxDistance = null,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            if (360 % rotateStepDegrees != 0 && rotations != null) {
                throw new InvalidOperationException($"360 % rotateStepDegrees (360 % {rotateStepDegrees} != 0) must be 0, unless 'rotations: float[]' is overwritten.");
            }

            if (maxPoses <= 0) {
                throw new ArgumentOutOfRangeException("maxPoses must be > 0.");
            }

            // default "visibility" distance
            float maxDistanceFloat;
            if (maxDistance == null) {
                maxDistanceFloat = maxVisibleDistance;
            } else if ((float)maxDistance <= 0) {
                throw new ArgumentOutOfRangeException("maxDistance must be >= 0 meters from the object.");
            } else {
                maxDistanceFloat = (float)maxDistance;
            }

            SimObjPhysics theObject = getSimObjectFromId(objectId: objectId);

            // populate default horizons
            if (horizons == null) {
                horizons = new float[] { -30, 0, 30, 60 };
            } else {
                foreach (float horizon in horizons) {
                    // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
                    if (horizon > maxDownwardLookAngle || horizon < -maxUpwardLookAngle) {
                        throw new ArgumentException(
                            $"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]. You gave {horizon}."
                        );
                    }
                }
            }

            // populate the positions by those that are reachable
            if (positions == null) {
                positions = getReachablePositions();
            }

            // populate the rotations based on rotateStepDegrees
            if (rotations == null) {
                // Consider the case where one does not want to move on a perfect grid, and is currently moving
                // with an offsetted set of rotations like {10, 100, 190, 280} instead of the default {0, 90, 180, 270}.
                // This may happen if the agent starts by teleports with the rotation of 10 degrees.
                int offset = (int)Math.Round(transform.eulerAngles.y % rotateStepDegrees);

                // Examples:
                // if rotateStepDegrees=10 and offset=70, then the paths would be [70, 80, ..., 400, 410, 420].
                // if rotateStepDegrees=90 and offset=10, then the paths would be [10, 100, 190, 280]
                rotations = new float[(int)Math.Round(360 / rotateStepDegrees)];
                int i = 0;
                for (float rotation = offset; rotation < 360 + offset; rotation += rotateStepDegrees) {
                    rotations[i++] = rotation;
                }
            }

            if (horizons.Length == 0 || rotations.Length == 0 || positions.Length == 0) {
                throw new InvalidOperationException("Every degree of freedom must have at least 1 valid value.");
            }

            // save current agent pose
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            Vector3 oldHorizon = m_Camera.transform.localEulerAngles;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(false);
            }

            // Don't want to consider all positions in the scene, just those from which the object
            // is plausibly visible. The following computes a "fudgeFactor" (radius of the object)
            // which is then used to filter the set of all reachable positions to just those plausible positions.
            Bounds objectBounds = UtilityFunctions.CreateEmptyBounds();
            objectBounds.Encapsulate(theObject.transform.position);
            foreach (Transform vp in theObject.VisibilityPoints) {
                objectBounds.Encapsulate(vp.position);
            }
            float fudgeFactor = objectBounds.extents.magnitude;
            List<Vector3> filteredPositions = positions.Where(
                p => (Vector3.Distance(a: p, b: theObject.transform.position) <= maxDistanceFloat + fudgeFactor + gridSize)
            ).ToList();

            // set each key to store a list
            List<Dictionary<string, object>> validAgentPoses = new List<Dictionary<string, object>>();
            string[] keys = { "x", "y", "z", "rotation", "horizon" };

            // iterate over each reasonable agent pose
            bool stopEarly = false;
            foreach (float horizon in horizons) {
                m_Camera.transform.localEulerAngles = new Vector3(horizon, 0f, 0f);

                foreach (float rotation in rotations) {
                    Vector3 rotationVector = new Vector3(x: 0, y: rotation, z: 0);
                    transform.rotation = Quaternion.Euler(rotationVector);

                    foreach (Vector3 position in filteredPositions) {
                        transform.position = position;

                        // Each of these values is directly compatible with TeleportFull
                        // and should be used with .step(action='TeleportFull', **interactable_positions[0])
                        if (objectIsCurrentlyVisible(theObject, maxDistanceFloat)) {
                            validAgentPoses.Add(new Dictionary<string, object> {
                                ["x"] = position.x,
                                ["y"] = position.y,
                                ["z"] = position.z,
                                ["rotation"] = rotation,
                                ["horizon"] = horizon
                            });

                            if (validAgentPoses.Count >= maxPoses) {
                                stopEarly = true;
                                break;
                            }
#if UNITY_EDITOR
                            // In the editor, draw lines indicating from where the object was visible.
                            Debug.DrawLine(position, position + transform.forward * (gridSize * 0.5f), Color.red, 20f);
#endif
                        }
                    }
                    if (stopEarly) {
                        break;
                    }
                }
                if (stopEarly) {
                    break;
                }
            }

            transform.position = oldPosition;
            transform.rotation = oldRotation;
            m_Camera.transform.localEulerAngles = oldHorizon;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }

#if UNITY_EDITOR
            Debug.Log(validAgentPoses.Count);
            Debug.Log(validAgentPoses);
#endif

            if (markActionFinished) {
                actionFinishedEmit(success: true, actionReturn: validAgentPoses);
            }

            return validAgentPoses;
        }

        // Get the poses with which the agent can interact with 'objectId'
        // @rotations: if rotation is not specified, we use rotateStepDegrees, which results in [0, 90, 180, 270] by default.
        public void GetInteractablePoses(
            string objectId,
            Vector3[] positions = null,
            float[] rotations = null,
            float[] horizons = null,
            float? maxDistance = null,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            getInteractablePoses(
                objectId: objectId,
                markActionFinished: true,
                positions: positions,
                rotations: rotations,
                horizons: horizons,
                maxDistance: maxDistance,
                maxPoses: maxPoses
            );
        }

        public void MoveAhead(
            float? moveMagnitude = null,
            float noise = 0f,
            bool forceAction = false
        ) {
            MoveRelative(
                z: 1.0f,
                moveMagnitude: moveMagnitude,
                noise: noise,
                forceAction: forceAction
            );
        }

        public void MoveBack(
            float? moveMagnitude = null,
            float noise = 0f,
            bool forceAction = false
        ) {
            MoveRelative(
                z: -1.0f,
                moveMagnitude: moveMagnitude,
                noise: noise,
                forceAction: forceAction
            );
        }

        public void MoveRight(
            float? moveMagnitude = null,
            float noise = 0f,
            bool forceAction = false
        ) {
            if (!allowHorizontalMovement) {
                throw new InvalidOperationException("Controller does not support horizontal movement by default. Set AllowHorizontalMovement to true on the Controller.");
            }
            MoveRelative(
                x: 1.0f,
                moveMagnitude: moveMagnitude,
                noise: noise,
                forceAction: forceAction
            );
        }

        public void MoveLeft(
            float? moveMagnitude = null,
            float noise = 0f,
            bool forceAction = false
        ) {
            if (!allowHorizontalMovement) {
                throw new InvalidOperationException("Controller does not support horizontal movement by default. Set AllowHorizontalMovement to true on the Controller.");
            }
            MoveRelative(
                x: -1.0f,
                moveMagnitude: moveMagnitude,
                noise: noise,
                forceAction: forceAction
            );
        }

        protected float GetMoveMagnitudeWithNoise(float moveMagnitude, float noise) {
            float internalNoise = applyActionNoise ? (float)systemRandom.NextGaussian(movementGaussianMu, movementGaussianSigma) : 0;
            return moveMagnitude + noise + (float)internalNoise;
        }

        protected float GetRotateMagnitudeWithNoise(Vector3 rotation, float noise) {
            float internalNoise = applyActionNoise ? (float)systemRandom.NextGaussian(rotateGaussianMu, rotateGaussianSigma) : 0;
            return rotation.y + noise + (float)internalNoise;
        }
    }
}
