
// These are the actions available to the LoCoBot

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
    
    // TODO: Why is this separate controller necessary, and why is it inheriting from base?
    public class StochasticRemoteFPSAgentController : BaseFPSAgentController {

        // these mu's seem like really weird defaults?
        // do we want it to basically never move?
        protected float movementGaussianMu = 0.001f;
        protected float rotateGaussianMu = 0.0f;

        protected float movementGaussianSigma = 0.005f;
        protected float rotateGaussianSigma = 0.5f;

        protected bool applyActionNoise = true;
        protected bool allowHorizontalMovement;

        public void Initialize(
            // stochastic specific
            bool applyActionNoise = true,
            bool allowHorizontalMovement = false,
            float? movementGaussianMu = null,
            float? movementGaussianSigma = null,
            float? rotateGaussianMu = null,
            float? rotateGaussianSigma = null,

            // base specific
            string agentMode = "default",
            float? fieldOfView = null,
            float gridSize = 0.25f,
            float timeScale = 1,
            float rotateStepDegrees = 90,
            bool snapToGrid = true,
            float visibilityDistance = 1.5f,
            float timeToWaitForObjectsToComeToRest = 10.0f,
            bool renderDepthImage = false,
            bool renderClassImage = false,
            bool renderObjectImage = false,
            bool renderNormalsImage = false,
            string visibilityScheme = "Collider"
        ) {
            if (movementGaussianMu != null) {
                this.movementGaussianMu = (float) movementGaussianMu;
            }
            if (rotateGaussianMu != null) {
                this.rotateGaussianMu = rotateGaussianMu;
            }

            if (movementGaussianSigma != null) {
                if ((float) movementGaussianSigma < 0) {
                    throw new ArgumentOutOfRangeException("movementGaussianSigma must be >= 0");
                }
                this.movementGaussianSigma = (float) movementGaussianSigma;
            }
            if (rotateGaussianSigma) {
                if ((float) rotateGaussianSigma < 0) {
                    throw new ArgumentOutOfRangeException("rotateGaussianSigma must be >= 0");
                }
                this.rotateGaussianSigma = rotateGaussianSigma;
            }

            #if UNITY_EDITOR
                Debug.Log("MoveNoise: " + movementGaussianMu + " mu, " + movementGaussianSigma + " sigma");
                Debug.Log("RotateNoise: " + rotateGaussianMu + " mu, " + rotateGaussianSigma + " sigma");
                Debug.Log("applynoise:" + applyActionNoise);
            #endif

            this.applyActionNoise = applyActionNoise;
            this.allowHorizontalMovement = allowHorizontalMovement;

            base.Initialize(
                agentMode: agentMode,
                fieldOfView: fieldOfView,
                gridSize: gridSize,
                timeScale: timeScale,
                rotateStepDegrees: rotateStepDegrees,
                snapToGrid: snapToGrid,
                visibilityDistance: visibilityDistance,
                timeToWaitForObjectsToComeToRest: timeToWaitForObjectsToComeToRest,
                renderDepthImage: renderDepthImage,
                renderClassImage: renderClassImage,
                renderObjectImage: renderObjectImage,
                renderNormalsImage: renderNormalsImage,
                visibilityScheme: visibilityScheme
            );
        }

        public void MoveRelative(float x, float z) {
            if (!allowHorizontalMovement && Math.Abs(x) > 0) {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set allowHorizontalMovement to true on the Controller.");
            }

            Vector3 moveLocal = new Vector3(x, 0, z);
            var random = new System.Random();
            

            // rotate a small amount with every movement since robot doesn't always move perfectly straight
            if (applyActionNoise) {
                float rotateNoise = (float) random.NextGaussian(rotateGaussianMu, rotateGaussianSigma / 2.0f);
                transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateNoise, 0.0f));
            }

            // Todo: come back to here.... :0
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
        }

        // TODO: manualInteract isn't even setable reachable...?
        public void Rotate(bool manualInteract = false) {
            // only default hand if not manually interacting with things
            if (!manualInteract) {
                DefaultAgentHand();
            }

            var rotateAmountDegrees = GetRotateMagnitudeWithNoise(action);

            // multiply quaternions to apply rotation based on rotateAmountDegrees
            transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateAmountDegrees, 0.0f));
            actionFinished(true);
        }

        public override void RotateRight(float? rotation) {
            float rotationAmount = this.rotateStepDegrees;
            if (rotation != null) {
                rotationAmount = (float) degrees;
            }
            Rotate(new ServerAction() { rotation = new Vector3(0, 1.0f * rotationAmount, 0) });
        }

        public override void RotateLeft(float? rotation) {
            float rotationAmount = this.rotateStepDegrees;
            if (rotation != null) {
                rotationAmount = (float) degrees;
            }
            Rotate(new ServerAction() { rotation = new Vector3(0, -1.0f * rotationAmount, 0) });
        }

        public void MoveAhead(ServerAction action) {
            action.x = 0.0f;
            action.y = 0;
            action.z = 1.0f;
            MoveRelative(action);
        }

        public void MoveBack(ServerAction action) {
            action.x = 0.0f;
            action.y = 0;
            action.z = -1.0f;
            MoveRelative(action);
        }

        public void MoveRight(ServerAction action) {
            // TODO: why would we not just allow horizontal movement by default?
            if (!allowHorizontalMovement) {
                throw new InvalidOperationException("Controller does not support horizontal movement by default. Set AllowHorizontalMovement to true on the Controller.");
            }
            action.x = 1.0f;
            action.y = 0;
            action.z = 0.0f;
            MoveRelative(action);
        }

        public void MoveLeft(ServerAction action) {
            // TODO: why would we not just allow horizontal movement by default?
            if (!allowHorizontalMovement) {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }
            action.x = -1.0f;
            action.y = 0;
            action.z = 1.0f;
            MoveRelative(action);
        }

        private float GetMoveMagnitudeWithNoise(ServerAction action) {
            // TODO: why are these being declared here?
            var random = new System.Random();
            var noise = applyActionNoise ? random.NextGaussian(movementGaussianMu, movementGaussianSigma) : 0;
            return action.moveMagnitude + action.noise + (float)noise;
        }

        private float GetRotateMagnitudeWithNoise(Vector3 rotation, float noise) {
            // TODO: why are these being declared here?
            var random = new System.Random();
            var noise = applyActionNoise ? random.NextGaussian(rotateGaussianMu, rotateGaussianSigma) : 0;
            return action.rotation.y + action.noise + (float) noise;
        }
    }
}
