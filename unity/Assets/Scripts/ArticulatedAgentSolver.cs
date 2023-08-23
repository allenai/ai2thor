using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;

public enum ABAgentState { Idle = 0, Moving = 1, Rotating = 2 };

public class AgentMoveParams {
    public ABAgentState agentState;
    public float distance;
    public float speed;
    public float acceleration;
    public float agentMass;
    public float tolerance;
    public float maxTimePassed;
    public float maxForce;
    public int positionCacheSize;
    
    //these are used during movement in fixed update (or whenever physics step executes)
    //can probably move these out of this class and into the joint solver class iself????
    public int direction;
    public float timePassed = 0.0f;
    public double[] cachedPositions;
    public int oldestCachedIndex;
    public Vector3 initialTransformation;
}

public class ArticulatedAgentSolver : MonoBehaviour, MovableContinuous {
    //pass in arm move parameters for Action based movement
    public AgentMoveParams currentAgentMoveParams = new AgentMoveParams();
    //reference for this joint's articulation body
    public ArticulationBody myAB;
    public ABAgentState agentState = ABAgentState.Idle;
    public float distanceTransformedSoFar;
    public double distanceTransformedThisFixedUpdate;
    public Vector3 prevStepTransformation, prevStepForward;
    public bool checkForMotion;
    //private Vector3 directionWorld;
    private float accelerationTorque;
    private float accelerationDistance, beginDecelerationSpeed, decelerationDistance, beginDecelerationTime; 
    private bool beginDeceleration, maxSpeed;
    float deceleration, speedupTime;

    void Start() {
        myAB = this.GetComponent<ArticulationBody>();
        currentAgentMoveParams.agentState = ABAgentState.Idle;
    }

    public void PrepToControlAgentFromAction(AgentMoveParams agentMoveParams) {
        Debug.Log($"preparing {this.transform.name} to move");
        if (Mathf.Approximately(agentMoveParams.distance, 0.0f)) {
            Debug.Log("Error! distance to move must be nonzero");
            return;
        }

        // THIS SHOULD BE MOVED TO AGENT INITIALIZATION
        SetCenterOfMass(myAB.transform.position - (Vector3.up * 0.9f));

        // Zero out distance delta tracking
        distanceTransformedSoFar = 0.0f;

        // Set current arm move params to prep for movement in fixed update
        currentAgentMoveParams = agentMoveParams;

        //initialize the buffer to cache positions to check for later
        currentAgentMoveParams.cachedPositions = new double[currentAgentMoveParams.positionCacheSize];
        
        if (currentAgentMoveParams.agentState == ABAgentState.Moving) {
            // snapshot the initial agent position to compare with later during movement
            currentAgentMoveParams.initialTransformation = myAB.transform.position;
            prevStepTransformation = myAB.transform.position;
        } else if (currentAgentMoveParams.agentState == ABAgentState.Rotating) {
            currentAgentMoveParams.initialTransformation = myAB.transform.eulerAngles;
            prevStepTransformation = myAB.transform.eulerAngles;
        }

        prevStepForward = myAB.transform.forward;
        
        // determine if agent can even accelerate to max velocity and decelerate to 0 before reaching target position in best-case scenario
        accelerationDistance = Mathf.Pow(currentAgentMoveParams.speed,2) / (2 * currentAgentMoveParams.acceleration);
        Debug.Log("accelerationDistance by default equals " + accelerationDistance);

        if (2 * accelerationDistance > currentAgentMoveParams.distance) {
            accelerationDistance = currentAgentMoveParams.distance / 2;
            Debug.Log("accelerationDistance now equals " + accelerationDistance);
        }

        beginDeceleration = false;
    }
    
    public void ContinuousUpdate(float fixedDeltaTime)  {
        // Vector3 maxForce = currentAgentMoveParams.maxForce * currentAgentMoveParams.direction * Vector3.forward;
        float remainingDistance = currentAgentMoveParams.distance - distanceTransformedSoFar;

        if (currentAgentMoveParams.agentState == ABAgentState.Moving) {

            float currentSpeed = Mathf.Abs(myAB.transform.InverseTransformDirection(myAB.velocity).z);
            
            // CASE: Accelerate - Apply force calculated from difference between intended distance and actual distance after amount of time that has passed
            if (distanceTransformedSoFar < accelerationDistance) {
                float desiredDistance = 0.5f * currentAgentMoveParams.acceleration * Mathf.Pow(currentAgentMoveParams.timePassed,2);
                float distanceDelta = desiredDistance - distanceTransformedSoFar;
                
                // This shouldn't be the same formula as correcting for a velocityDelta, like in cruise-control mode, but for some reason, it's working great
                float relativeForce = (distanceDelta / fixedDeltaTime) * currentAgentMoveParams.agentMass
                    * currentAgentMoveParams.direction;

                Debug.Log("1. distanceDelta is " + distanceDelta + ". Applying force of " + relativeForce);

                // Use motor's max force in edge case where progress is halted, such as an obstacle in the way
                // UGH, NEED TO ACCOUNT FOR SIGN CHANGE
                // if (maxForce.sqrMagnitude > Mathf.Abs(relativeForce.sqrMagnitude)) {
                //     relativeForce = maxForce;
                // }
                
                myAB.AddRelativeForce(new Vector3(0, 0, relativeForce));
            
            // CASE: Decelerate - Apply force calculated from difference between intended velocity and actual velocity at given distance remaining to travel
            } else if (distanceTransformedSoFar >= currentAgentMoveParams.distance - accelerationDistance) {
                if (beginDeceleration == false) {
                    beginDecelerationSpeed = currentSpeed;
                    beginDeceleration = true;
                }

                float desiredSpeed = beginDecelerationSpeed * (remainingDistance / accelerationDistance);
                float speedDelta = desiredSpeed - currentSpeed;
            
                float relativeForce = (speedDelta / Time.fixedDeltaTime) * currentAgentMoveParams.agentMass
                    * currentAgentMoveParams.direction;

                Debug.Log("3. speedDelta is " + speedDelta + ". Applying force of " + relativeForce);
                myAB.AddRelativeForce(new Vector3(0, 0, relativeForce));
            
            // CASE: Cruise Control - Apply force calculated from difference between intended velocity and current velocity
            } else {
                float speedDelta = currentAgentMoveParams.speed - currentSpeed;

                float relativeForce = (speedDelta / Time.fixedDeltaTime) * currentAgentMoveParams.agentMass
                    * currentAgentMoveParams.direction;
                
                // Use motor's max force in edge case where progress is halted, such as an obstacle in the way
                // UGH, NEED TO ACCOUNT FOR SIGN CHANGE
                // if (relativeForce.sqrMagnitude > Mathf.Abs(maxForce.sqrMagnitude)) {
                //     relativeForce = maxForce;
                // }

                Debug.Log("2. speedDelta is " + speedDelta + ". Applying force of " + relativeForce);
                myAB.AddRelativeForce(new Vector3(0, 0, relativeForce));
            }

            // Begin checks to see if we have stopped moving or if we need to stop moving

            // Cache the position at the moment
            Vector3 currentPosition = myAB.transform.position;

            // Average current and previous forward-vectors to get an averaged forward-vector over the course of previous physics-step
            Vector3 currentForward = (myAB.transform.forward + prevStepForward) / 2;

            // Determine (positive) distance covered based on forward progress, relative to previous physics-step's averaged forward-vector
            distanceTransformedThisFixedUpdate = Mathf.Clamp(Vector3.Dot(currentPosition - prevStepTransformation, currentForward * currentAgentMoveParams.direction), 0, Mathf.Infinity);
            distanceTransformedSoFar += (float)distanceTransformedThisFixedUpdate;

            // Cache data used to check if we have stopped moving or if we need to stop moving
            currentAgentMoveParams.cachedPositions[currentAgentMoveParams.oldestCachedIndex] = distanceTransformedThisFixedUpdate;

            // Store current values for comparing with next FixedUpdate
            prevStepTransformation = currentPosition;
            prevStepForward = myAB.transform.forward;

        } else if (currentAgentMoveParams.agentState == ABAgentState.Rotating) {
            
            float currentAngularSpeed = Mathf.Abs(myAB.angularVelocity.y);
            
            // CASE: Accelerate - Apply force calculated from difference between intended distance and actual distance after amount of time that has passed
            if (distanceTransformedSoFar < accelerationDistance) {
                float desiredAngularDistance = 0.5f * currentAgentMoveParams.acceleration * Mathf.Pow(currentAgentMoveParams.timePassed,2);
                float angularDistanceDelta = desiredAngularDistance - distanceTransformedSoFar;
                
                float relativeTorque = (angularDistanceDelta / Time.fixedDeltaTime) * myAB.inertiaTensor.y * currentAgentMoveParams.direction;

                Debug.Log("1. distanceDelta is " + angularDistanceDelta + ". Applying torque of " + relativeTorque);

                // Use motor's max force in edge case where progress is halted, such as an obstacle in the way
                // UGH, NEED TO ACCOUNT FOR SIGN CHANGE
                // if (maxForce.sqrMagnitude > Mathf.Abs(relativeForce.sqrMagnitude)) {
                //     relativeForce = maxForce;
                // }

                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));

            // CASE: Decelerate - Apply force calculated from difference between intended angular velocity and actual angular velocity at given angular distance remaining to travel
            } else if (distanceTransformedSoFar >= currentAgentMoveParams.distance - accelerationDistance) {
                if (beginDeceleration == false) {
                    beginDecelerationSpeed = currentAngularSpeed;
                    beginDeceleration = true;
                }
                
                float desiredAngularSpeed = beginDecelerationSpeed * (remainingDistance / accelerationDistance);
                float angularSpeedDelta = desiredAngularSpeed - currentAngularSpeed;
                
                float relativeTorque = (angularSpeedDelta / Time.fixedDeltaTime) * myAB.inertiaTensor.y
                    * currentAgentMoveParams.direction;

                Debug.Log("3. desiredAngularSpeed is " + desiredAngularSpeed + ". Applying torque of " + relativeTorque);

                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));
                
            // CASE: Cruise - Apply force calculated from difference between intended angular velocity and current angular velocity
            } else {
                float angularSpeedDelta = currentAgentMoveParams.speed - currentAngularSpeed;

                float relativeTorque = (angularSpeedDelta / Time.fixedDeltaTime) * myAB.inertiaTensor.y
                    * currentAgentMoveParams.direction;

                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));
                Debug.Log("2. angularSpeedDelta is " + angularSpeedDelta + ". Applying torque of " + relativeTorque);
            }
            
            // Begin checks to see if we have stopped moving or if we need to stop moving
            
            // Cache the rotation at the moment
            Vector3 currentRotation = myAB.transform.eulerAngles;

            // Determine (positive) angular distance covered
            distanceTransformedThisFixedUpdate = Mathf.Deg2Rad * Mathf.Clamp((currentRotation.y - prevStepTransformation.y) * currentAgentMoveParams.direction, 0, Mathf.Infinity);
            distanceTransformedSoFar += (float)distanceTransformedThisFixedUpdate;

            // Cache data used to check if we have stopped rotating or if we need to stop rotating
            currentAgentMoveParams.cachedPositions[currentAgentMoveParams.oldestCachedIndex] = distanceTransformedThisFixedUpdate;

            // Store current values for comparing with next FixedUpdate
            prevStepTransformation = currentRotation;
        }

        // Iterate next index in cache, loop back to index 0 as we get newer positions
        currentAgentMoveParams.oldestCachedIndex = (currentAgentMoveParams.oldestCachedIndex + 1) % currentAgentMoveParams.positionCacheSize;
        // Debug.Log($"current index: {currentAgentMoveParams.oldestCachedIndex}");

        // This is here so the first time, iterating through the [0] index we don't check, only the second time and beyond
        // every time we loop back around the cached positions, check if we effectively stopped moving
        if (currentAgentMoveParams.oldestCachedIndex == 0) {            
            // Flag for shouldHalt to check if we should, in fact, halt
            checkForMotion = true;
            // Debug.Log($"current index after looped: {currentAgentMoveParams.oldestCachedIndex}");
        } else {
            checkForMotion = false;
        }

        // Otherwise we have a hard timer to stop movement so we don't move forever and crash unity
        currentAgentMoveParams.timePassed += fixedDeltaTime;

        // We are set to be in an idle state so return and do nothing
        return;
    }

    public bool ShouldHalt() {
            // Debug.Log("checking ArticulatedAgentController shouldHalt");
            bool shouldStop = false;
            ArticulatedAgentSolver a = this;
            // Debug.Log($"checking agent: {a.transform.name}");
            // Debug.Log($"distance moved so far is: {a.distanceTransformedSoFar}");
            // Debug.Log($"current velocity is: {this.transform.GetComponent<ArticulationBody>().velocity.magnitude}");
            
            //check agent to see if it has halted or not
            if (!a.shouldHalt(
                distanceTransformedSoFar: a.distanceTransformedSoFar,
                cachedPositions: a.currentAgentMoveParams.cachedPositions,
                tolerance: a.currentAgentMoveParams.tolerance
            )) {
                // if any single joint is still not halting, return false
                // Debug.Log("still not done, don't halt yet");
                shouldStop = false;
                return shouldStop;
            } else {
                //this joint returns that it should stop!
                Debug.Log($"halted! Return shouldStop! Distance moved: {a.distanceTransformedSoFar}");
                shouldStop = true;
                return shouldStop;
            }
        }

    public void FinishContinuousMove(BaseFPSAgentController controller) {
        Debug.Log("starting continuousMoveFinishAB");
        controller.transform.GetComponent<ArticulationBody>().velocity = Vector3.zero;
        controller.transform.GetComponent<ArticulationBody>().angularVelocity = Vector3.zero;
        controller.transform.GetComponent<ArticulatedAgentSolver>().currentAgentMoveParams.agentState = ABAgentState.Idle;
        bool actionSuccess = true;
        string debugMessage = "I guess everything is fine?";

        //maybe needs to switch back to slippery here to prep for movement???
        //or maybe we default to high friction, and only change to no friction when moving body
        //controller.SetFloorColliderToSlippery();

        controller.errorMessage = debugMessage;
        controller.actionFinished(actionSuccess, debugMessage);
    }

    //do checks based on what sort of joint I am
    //have a bool or something to check if you should check std dev
    public bool shouldHalt(
        float distanceTransformedSoFar,
        double[] cachedPositions,
        float tolerance) {
        
        bool shouldHalt = false;

        
        //halt if positions/rotations are within tolerance and effectively not changing
        // Debug.Log($"checkForMotion is: {checkForMotion}");
        
        // Debug.Log("Distance moved is " + distanceTransformedSoFar + ", and distance to exceed is " + currentAgentMoveParams.distance);
        if (checkForMotion) {
            if (CheckArrayForMotion(cachedPositions, tolerance)) {
                shouldHalt = true;
                // IdleAllStates();
                Debug.Log("halt due to position delta within tolerance");
                checkForMotion = false;
                return shouldHalt;
            }
        }

        //check if the amount moved/rotated exceeds this agents target
        else if (distanceTransformedSoFar >= currentAgentMoveParams.distance) {
            shouldHalt = true;
            // IdleAllStates();
            Debug.Log("halt due to distance reached/exceeded");
            return shouldHalt;
        }

        //hard check for time limit
        else if (currentAgentMoveParams.timePassed >= currentAgentMoveParams.maxTimePassed) {
            shouldHalt = true;
            // IdleAllStates();
            Debug.Log("halt from timeout");
            return shouldHalt;
        }

        return shouldHalt;
    }

    //check if all values in the array are within a threshold of motion or not
    bool CheckArrayForMotion(double[] values, double tolerance) {
        // check whether any of previous n FixedUpdate deltas qualify agent for a continuation
        bool noProgress = true;
        foreach (double distanceDelta in values) {
            Debug.Log("distanceDelta is " + distanceDelta);
            if (distanceDelta >= tolerance) {
                noProgress = false;
            }
        }
        
        return noProgress;
    }

    void SetCenterOfMass(Vector3 worldCenterOfMass)
    {
        ArticulationBody[] bodies = FindObjectsOfType<ArticulationBody>();
        foreach (ArticulationBody body in bodies)
        {
            // Convert world-space center of mass to local space
            Vector3 localCenterOfMass = body.transform.InverseTransformPoint(worldCenterOfMass);
            body.centerOfMass = localCenterOfMass;
            // Debug.Log(body.gameObject.name + "'s center of mass set to (" + body.centerOfMass.x + ", " + body.centerOfMass.y + ", " + body.centerOfMass.z + ")");
        }
    }
}
