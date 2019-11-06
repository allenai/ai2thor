
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

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    public class StochasticRemoteFPSAgentController : PhysicsRemoteFPSAgentController
    {
        [SerializeField]
        public bool ApplyActionNoise = true;
        [SerializeField]
        public float MovementGaussianMu = 0.0f;

        [SerializeField]
        public float MovementGaussianSigma = 0.05f;

        [SerializeField]
        public float RotateGaussianMu = 0.0f;

        [SerializeField]
        public float RotateGaussianSigma = 0.01f;

        [SerializeField]
        public bool AllowHorizontalMovement = false;

        public override void MoveRelative(ServerAction action)
        {
            if (!AllowHorizontalMovement && Math.Abs(action.x) > 0)
            {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }
            var moveLocal = new Vector3(action.x, 0, action.z);
            var moveMagnitude = moveLocal.magnitude;
            if (moveMagnitude > 0.00001)
            {
                //random.NextGaussian(RotateGaussianMu, RotateGaussianSigma);
                var random = new System.Random();
                var rotateNoise = (float)random.NextGaussian(RotateGaussianMu, RotateGaussianSigma / 2.0f);

                // rotate a small amount with every movement since robot doesn't always move perfectly straight
                transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateNoise, 0.0f));

                var moveLocalNorm = moveLocal / moveMagnitude;
                if (action.moveMagnitude > 0.0)
                {
                    action.moveMagnitude = moveMagnitude * action.moveMagnitude;
                }
                else
                {
                    action.moveMagnitude = moveMagnitude * gridSize;
                }

                var magnitudeWithNoise = GetMoveMagnitudeWithNoise(action);

                actionFinished(moveInDirection(
                    this.transform.rotation * (moveLocalNorm * magnitudeWithNoise),
                    action.objectId,
                    action.maxAgentsDistance, action.forceAction
                ));
            }
            else
            {
                actionFinished(false);
            }
        }

        public override void Rotate(ServerAction action)
        {
            DefaultAgentHand(action);
            var rotateAmountDegrees = GetRotateMagnitudeWithNoise(action);

            transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateAmountDegrees, 0.0f));
            actionFinished(true);
        }

        public override void RotateRight(ServerAction action)
        {
            Rotate(new ServerAction() { rotation = new Vector3(0, 90.0f, 0) });
        }

        public override void RotateLeft(ServerAction action)
        {
            Rotate(new ServerAction() { rotation = new Vector3(0, -90.0f, 0) });
        }

        public override void MoveAhead(ServerAction action)
        {
            action.x = 0.0f;
            action.y = 0;
            action.z = 1.0f;
            MoveRelative(action);
        }

        public override void MoveBack(ServerAction action)
        {
            action.x = 0.0f;
            action.y = 0;
            action.z = -1.0f;
            MoveRelative(action);
        }

        public override void MoveRight(ServerAction action)
        {
            if (!AllowHorizontalMovement)
            {
                throw new InvalidOperationException("Controller does not support horizontal movement by default. Set AllowHorizontalMovement to true on the Controller.");
            }
            action.x = 1.0f;
            action.y = 0;
            action.z = 0.0f;
            MoveRelative(action);
        }

        public override void MoveLeft(ServerAction action)
        {
            if (!AllowHorizontalMovement)
            {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }
            action.x = -1.0f;
            action.y = 0;
            action.z = 1.0f;
            MoveRelative(action);
        }

        private float GetMoveMagnitudeWithNoise(ServerAction action)
        {
            var random = new System.Random();
            var noise = ApplyActionNoise ? random.NextGaussian(MovementGaussianMu, MovementGaussianSigma) : 0;
            return action.moveMagnitude + action.noise + (float)noise;
        }

        private float GetRotateMagnitudeWithNoise(ServerAction action)
        {
            var random = new System.Random();
            var noise = ApplyActionNoise ? random.NextGaussian(RotateGaussianMu, RotateGaussianSigma) : 0;
            return action.rotation.y + action.noise + (float)noise;
        }
    }
}