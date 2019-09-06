
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
    public class StochasticRemoteFPSAgentController : PhysicsRemoteFPSAgentController {
        [SerializeField]
        public bool HasMovementNoise = false;
        [SerializeField]
        public float GaussianMu = 0.0f;

        [SerializeField]
        public float GaussianSigma = 1.0f;

         public override void RotateRight(ServerAction controlCommand) {
            if (CheckIfAgentCanTurn(90)||controlCommand.forceAction) {
                DefaultAgentHand(controlCommand);
                base.RotateRight(controlCommand);
            } else {
                actionFinished(false);
            }

        }

        public override void RotateLeft(ServerAction controlCommand) {
            if (CheckIfAgentCanTurn(-90)||controlCommand.forceAction) {
                DefaultAgentHand(controlCommand);
                base.RotateLeft(controlCommand);

            } else {
                actionFinished(false);
            }
        }

        public override void MoveLeft(ServerAction action) {
            action.moveMagnitude = action.moveMagnitude > 0 ? GetActionMagnitudeWithNoise(action.moveMagnitude) : gridSize;
            actionFinished(moveInDirection(
                -1 * transform.right * action.moveMagnitude,
                action.objectId,
                action.maxAgentsDistance, action.forceAction
            ));
        }

        public override void MoveRight(ServerAction action) {
            action.moveMagnitude = action.moveMagnitude > 0 ? GetActionMagnitudeWithNoise(action.moveMagnitude) : gridSize;
            actionFinished(moveInDirection(
                transform.right * action.moveMagnitude,
                action.objectId,
                action.maxAgentsDistance, action.forceAction
            ));
        }

        public override void MoveAhead(ServerAction action) {
            action.moveMagnitude = action.moveMagnitude > 0 ? GetActionMagnitudeWithNoise(action.moveMagnitude) : gridSize;
            actionFinished(moveInDirection(
                transform.forward * action.moveMagnitude,
                action.objectId,
                action.maxAgentsDistance, action.forceAction
            ));
        }

        public override void MoveBack(ServerAction action) {
            action.moveMagnitude = action.moveMagnitude > 0 ? GetActionMagnitudeWithNoise(action.moveMagnitude) : gridSize;
            actionFinished(moveInDirection(
                -1 * transform.forward * action.moveMagnitude,
                action.objectId,
                action.maxAgentsDistance, action.forceAction
            ));
        }

        //  public override void MoveWithNoise(ServerAction action) {
        //     action.moveMagnitude = action.moveMagnitude > 0 ? GetActionMagnitudeWithNoise(action.moveMagnitude) : gridSize;
        //     actionFinished(moveInDirection(
        //         -1 * transform.forward * action.moveMagnitude,
        //         action.objectId,
        //         action.maxAgentsDistance, action.forceAction
        //     ));
        // }

        private float GetActionMagnitudeWithNoise(float magnitude) {
            var random = new System.Random();
            var noise = random.NextGaussian(GaussianMu, GaussianSigma);
            return magnitude + (float) noise;
        } 
    }
}