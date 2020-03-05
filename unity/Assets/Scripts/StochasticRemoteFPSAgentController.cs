
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
        protected bool applyActionNoise = true;
        protected float movementGaussianMu = 0.001f;
        protected float movementGaussianSigma = 0.005f;
        protected float rotateGaussianMu = 0.0f;
        protected float rotateGaussianSigma = 0.5f;
        protected bool allowHorizontalMovement = false;
        //determines rotation increment of stochastic rotate functions
        protected float rotateStepDegrees = 90.0f;

        public new void Initialize(ServerAction action)
        {
            this.applyActionNoise = action.applyActionNoise;

            if (action.movementGaussianMu > 0.0f)
            {
                this.movementGaussianMu = action.movementGaussianMu;
            }

            if (action.movementGaussianSigma > 0.0f)
            {
                this.movementGaussianSigma = action.movementGaussianSigma;
            }

            if (action.rotateGaussianMu > 0.0f)
            {
                this.rotateGaussianMu = action.rotateGaussianMu;
            }

            if (action.rotateGaussianSigma > 0.0f)
            {
                this.rotateGaussianSigma = action.rotateGaussianSigma;
            }

            if (action.rotateStepDegrees > 0.0) {
                this.rotateStepDegrees = action.rotateStepDegrees;
            }

            #if UNITY_EDITOR
            Debug.Log("MoveNoise: " + movementGaussianMu + " mu, " + movementGaussianSigma + " sigma");
            Debug.Log("RotateNoise: " + rotateGaussianMu + " mu, " + rotateGaussianSigma + " sigma");
            Debug.Log("applynoise:" + applyActionNoise);
            #endif

            base.Initialize(action);
        }

        public override void MoveRelative(ServerAction action)
        {
            if (!allowHorizontalMovement && Math.Abs(action.x) > 0)
            {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }
            var moveLocal = new Vector3(action.x, 0, action.z);
            var moveMagnitude = moveLocal.magnitude;
            if (moveMagnitude > 0.00001)
            {
                //random.NextGaussian(RotateGaussianMu, RotateGaussianSigma);
                var random = new System.Random();
                

                // rotate a small amount with every movement since robot doesn't always move perfectly straight
                if (this.applyActionNoise) {
                    var rotateNoise = (float)random.NextGaussian(rotateGaussianMu, rotateGaussianSigma / 2.0f);
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateNoise, 0.0f));
                }
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
                    action.maxAgentsDistance,
                    action.forceAction
                ));
            }
            else
            {
                actionFinished(false);
            }
        }


    // NOOP action to allow evaluation to know that the episode has finished
    public void Stop(ServerAction action) {
        actionFinished(true);
    }

    public override void Rotate(ServerAction action)
    {
        DefaultAgentHand(action);
        var rotateAmountDegrees = GetRotateMagnitudeWithNoise(action);

        //multiply quaternions to apply rotation based on rotateAmountDegrees
        transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateAmountDegrees, 0.0f));
        actionFinished(true);
    }

    public override void RotateRight(ServerAction action)
    {
        Rotate(new ServerAction() { rotation = new Vector3(0, this.rotateStepDegrees, 0) });
    }

    public override void RotateLeft(ServerAction action)
    {
        Rotate(new ServerAction() { rotation = new Vector3(0, -1.0f * this.rotateStepDegrees, 0) });
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
        if (!allowHorizontalMovement)
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
        if (!allowHorizontalMovement)
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
        var noise = applyActionNoise ? random.NextGaussian(movementGaussianMu, movementGaussianSigma) : 0;
        return action.moveMagnitude + action.noise + (float)noise;
    }

    private float GetRotateMagnitudeWithNoise(ServerAction action)
    {
        var random = new System.Random();
        var noise = applyActionNoise ? random.NextGaussian(rotateGaussianMu, rotateGaussianSigma) : 0;
        return action.rotation.y + action.noise + (float)noise;
    }
}
}
