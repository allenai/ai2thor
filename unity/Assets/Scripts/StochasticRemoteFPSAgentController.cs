
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

namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    public class StochasticRemoteFPSAgentController : BaseFPSAgentController {
        protected bool applyActionNoise = true;
        protected float movementGaussianMu = 0.001f;
        protected float movementGaussianSigma = 0.005f;
        protected float rotateGaussianMu = 0.0f;
        protected float rotateGaussianSigma = 0.5f;
        protected bool allowHorizontalMovement = false;

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
        }

        // reset visible objects while in editor, for debug purposes only
        private void LateUpdate() {
#if UNITY_EDITOR || UNITY_WEBGL
            VisibleSimObjPhysics = VisibleSimObjs();
#endif
        }

        public override void MoveRelative(ServerAction action) {
            if (!allowHorizontalMovement && Math.Abs(action.x) > 0) {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }
            var moveLocal = new Vector3(action.x, 0, action.z);
            var moveMagnitude = moveLocal.magnitude;
            if (moveMagnitude > 0.00001) {
                // random.NextGaussian(RotateGaussianMu, RotateGaussianSigma);
                var random = new System.Random();

                // rotate a small amount with every movement since robot doesn't always move perfectly straight
                if (this.applyActionNoise) {
                    var rotateNoise = (float)random.NextGaussian(rotateGaussianMu, rotateGaussianSigma / 2.0f);
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateNoise, 0.0f));
                }
                var moveLocalNorm = moveLocal / moveMagnitude;
                if (action.moveMagnitude > 0.0) {
                    action.moveMagnitude = moveMagnitude * action.moveMagnitude;
                } else {
                    action.moveMagnitude = moveMagnitude * gridSize;
                }

                var magnitudeWithNoise = GetMoveMagnitudeWithNoise(action);

                actionFinished(moveInDirection(
                    this.transform.rotation * (moveLocalNorm * magnitudeWithNoise),
                    action.objectId,
                    action.maxAgentsDistance,
                    action.forceAction
                ));
            } else {
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
        public override void Rotate(Vector3 rotation) {
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

        public override void RotateRight(ServerAction action) {
            float rotationAmount = this.rotateStepDegrees;

            if (action.degrees != 0.0f) {
                rotationAmount = action.degrees;
            }

            Rotate(rotation: new Vector3(0, rotationAmount, 0));
        }

        public override void RotateLeft(ServerAction action) {
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
            float x, float y, float z,
            Vector3? rotation = null, float? horizon = null, bool forceAction = false
        ) {
            Teleport(
                position: new Vector3(x, y, z), rotation: rotation, horizon: horizon, forceAction: forceAction
            );
        }

        public void Teleport(
            Vector3? position = null, Vector3? rotation = null, float? horizon = null, bool forceAction = false
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

        public override void MoveAhead(ServerAction action) {
            action.x = 0.0f;
            action.y = 0;
            action.z = 1.0f;
            MoveRelative(action);
        }

        public override void MoveBack(ServerAction action) {
            action.x = 0.0f;
            action.y = 0;
            action.z = -1.0f;
            MoveRelative(action);
        }

        public override void MoveRight(ServerAction action) {
            if (!allowHorizontalMovement) {
                throw new InvalidOperationException("Controller does not support horizontal movement by default. Set AllowHorizontalMovement to true on the Controller.");
            }
            action.x = 1.0f;
            action.y = 0;
            action.z = 0.0f;
            MoveRelative(action);
        }

        public override void MoveLeft(ServerAction action) {
            if (!allowHorizontalMovement) {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }
            action.x = -1.0f;
            action.y = 0;
            action.z = 1.0f;
            MoveRelative(action);
        }

        protected float GetMoveMagnitudeWithNoise(ServerAction action) {
            System.Random random = new System.Random();
            float internalNoise = applyActionNoise ? (float)random.NextGaussian(movementGaussianMu, movementGaussianSigma) : 0;
            return action.moveMagnitude + action.noise + (float)internalNoise;
        }

        protected float GetRotateMagnitudeWithNoise(Vector3 rotation, float noise) {
            System.Random random = new System.Random();
            float internalNoise = applyActionNoise ? (float)random.NextGaussian(rotateGaussianMu, rotateGaussianSigma) : 0;
            return rotation.y + noise + (float)internalNoise;
        }
    }
}
