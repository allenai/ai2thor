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
    public int positionCacheSize;
    
    //these are used during movement in fixed update (or whenever physics step executes)
    //can probably move these out of this class and into the joint solver class iself????
    public int direction;
    public float timePassed = 0.0f;
    public Vector3[] cachedPositions;
    public int oldestCachedIndex;
    public Vector3 initialTransformation;
}

public class ArticulatedAgentSolver : MonoBehaviour, MovableContinuous {
    //pass in arm move parameters for Action based movement
    public AgentMoveParams currentAgentMoveParams = new AgentMoveParams();
    //reference for this joint's articulation body
    public ArticulationBody myAB;
    public ABAgentState agentState = ABAgentState.Idle;
    public float distanceMovedSoFar;
    public bool checkStandardDev;
    //private Vector3 directionWorld;
    private float accelerationTorque;
    private float accelerationDistance, beginDecelerationSpeed, decelerationDistance, beginDecelerationTime; 
    private bool beginDeceleration, maxSpeed;
    float deceleration, startTime, speedupTime;

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
        Debug.Log("DISTANCE IS " + agentMoveParams.distance);
        
        startTime = Time.fixedTime;
        // Debug.Log("ArticulatedAgentController's inertiaTensor is (" + myAB.inertiaTensor.x + ", " + myAB.inertiaTensor.y + ", " + myAB.inertiaTensor.z + ")");
        // Debug.Log("(4) ArticulatedAgentSolver: SETTING UP EVERYTHING I NEED TO INFORM EACH FRAME OF MOVEMENT");
        //zero out distance delta tracking
        distanceMovedSoFar = 0.0f;

        //set current arm move params to prep for movement in fixed update
        currentAgentMoveParams = agentMoveParams;
        
        // THIS SHOULD BE MOVED TO AGENT INITIALIZATION
        SetCenterOfMass(myAB.transform.position - (Vector3.up * 0.9f));

        //initialize the buffer to cache positions to check for later
        currentAgentMoveParams.cachedPositions = new Vector3[currentAgentMoveParams.positionCacheSize];
        
        if (currentAgentMoveParams.agentState == ABAgentState.Moving) {
            // snapshot the initial agent position to compare with later during movement
            currentAgentMoveParams.initialTransformation = this.transform.position;

            // Vector3 directionWorld = transform.TransformDirection(new Vector3(0,0,currentAgentMoveParams.distance));
            // Vector3 targetPosition = transform.position + directionWorld;

        } else if (currentAgentMoveParams.agentState == ABAgentState.Rotating) {
            currentAgentMoveParams.initialTransformation = this.transform.eulerAngles;
            // float accelerationTorque = myAB.inertiaTensor.y * currentAgentMoveParams.acceleration * (180f / Mathf.PI);
        }
        
        // determine if agent can even accelerate to max velocity and decelerate to 0 before reaching target position
        accelerationDistance = Mathf.Pow(currentAgentMoveParams.speed,2) / (2 * currentAgentMoveParams.acceleration);
        Debug.Log("accelerationDistance by default equals " + accelerationDistance);
        // Debug.Log("accelerationDistance by default equals " + accelerationDistance * Mathf.Rad2Deg);

        if (2 * accelerationDistance > currentAgentMoveParams.distance) {
            accelerationDistance = currentAgentMoveParams.distance / 2;
            Debug.Log("accelerationDistance equals " + accelerationDistance);
            // Debug.Log("accelerationDistance equals " + accelerationDistance * Mathf.Rad2Deg);
        }
    }
    
    // bool maxedOut = false;
    public void ContinuousUpdate(float fixedDeltaTime)  {
        if (currentAgentMoveParams.agentState == ABAgentState.Moving) {
            // Debug.Log("(7) ArticulatedAgentSolver: ACTUAL LOGIC. Also, distanceMovedSoFar is " + distanceMovedSoFar + ", and decelerationDistance is " + decelerationDistance);
            if (myAB.velocity.magnitude < currentAgentMoveParams.speed && currentAgentMoveParams.distance - distanceMovedSoFar > accelerationDistance) {
                // since the acceleration is distance-based,
                    Vector3 relativeForce = ( (1.0f * currentAgentMoveParams.agentMass * currentAgentMoveParams.acceleration) + (myAB.velocity.magnitude * myAB.linearDamping)) * currentAgentMoveParams.direction * Vector3.forward;
                    myAB.AddRelativeForce(relativeForce);
                    Debug.Log("Applying acceleration force of " + relativeForce.z);
            } else if (distanceMovedSoFar >= currentAgentMoveParams.distance - accelerationDistance) {
                
                if (beginDeceleration == false) {
                    beginDeceleration = true;
                    // not necessary, since speed is kept constant, but 
                    beginDecelerationSpeed = myAB.velocity.magnitude;
                    decelerationDistance = currentAgentMoveParams.distance - distanceMovedSoFar;
                    
                    deceleration = myAB.velocity.magnitude / (currentAgentMoveParams.distance - distanceMovedSoFar);
                    Debug.Log("DECELERATION RATE IS " + deceleration + " because " + myAB.velocity.magnitude + " over " + (currentAgentMoveParams.distance - distanceMovedSoFar) );
                    // Debug.Log("Applying deceleration values, based on " + beginDecelerationSpeed + " and " + decelerationDistance);
                }

                // Vector3 relativeDecForce = currentAgentMoveParams.agentMass * ( Mathf.Pow(beginDecelerationSpeed,2) / (2 * decelerationDistance) ) * -Vector3.forward;
                // Debug.Log("Agent's traveled this far: " + distanceMovedSoFar + ". Applying relative force of " + relativeDecForce + " to delecerate " + currentAgentMoveParams.agentMass + " kg of agent! Velocity is currently " + myAB.velocity.magnitude);
                // myAB.AddRelativeForce(relativeDecForce);
                Vector3 relativeForce = ((1.0f * currentAgentMoveParams.agentMass * currentAgentMoveParams.acceleration) - (myAB.velocity.magnitude * myAB.linearDamping)) * currentAgentMoveParams.direction * -Vector3.forward;
                myAB.AddRelativeForce(relativeForce);
            } else {
                // if (maxedOut == false) {
                //     maxedOut = true;
                //     speedupTime = Time.fixedTime - startTime;
                //     Debug.Log("Reached max speed at " + speedupTime);
                // }
                myAB.AddRelativeForce( (currentAgentMoveParams.agentMass * (currentAgentMoveParams.speed - myAB.velocity.magnitude) + (myAB.velocity.magnitude * myAB.linearDamping)) * currentAgentMoveParams.direction * Vector3.forward);
            }

            //begin checks to see if we have stopped moving or if we need to stop moving
            //cache the position at the moment
            Vector3 currentPosition = myAB.transform.position;
            currentAgentMoveParams.cachedPositions[currentAgentMoveParams.oldestCachedIndex] = currentPosition;

            distanceMovedSoFar = Mathf.Abs((currentPosition - currentAgentMoveParams.initialTransformation).magnitude);

        } else if (currentAgentMoveParams.agentState == ABAgentState.Rotating) {
            
            Debug.Log("Okay, distanceMovedSoFar is " + Mathf.Rad2Deg * distanceMovedSoFar + ", and accelerationDistance is " + Mathf.Rad2Deg * accelerationDistance);
            // CASE: Accelerate
            if (myAB.angularVelocity.magnitude < currentAgentMoveParams.speed && currentAgentMoveParams.distance - distanceMovedSoFar > accelerationDistance) {
                float relativeTorque = ((1.0f * myAB.inertiaTensor.y * currentAgentMoveParams.acceleration) + (myAB.angularVelocity.magnitude * myAB.angularDamping)) * currentAgentMoveParams.direction;
                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));
                // Debug.Log("AAAAAAAAAAAAAAAAAAAAAAApplying torque force of " + relativeTorque);

            // CASE: Decelerate
            } else if (distanceMovedSoFar >= currentAgentMoveParams.distance - accelerationDistance) {
                // Debug.Log("Okay, DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD distanceMovedSoFar is " + Mathf.Rad2Deg * distanceMovedSoFar);
                if(beginDeceleration == false) {
                    beginDeceleration = true;
                    beginDecelerationSpeed = myAB.angularVelocity[1];
                    beginDecelerationTime = Time.time;
                    decelerationDistance = currentAgentMoveParams.distance - distanceMovedSoFar;
                    Debug.Log("APPLYING DECELERATION VALUES, based on " + beginDecelerationSpeed + " and " + decelerationDistance);
                }
                float relativeTorque = ((1.0f * myAB.inertiaTensor.y * currentAgentMoveParams.acceleration) - (myAB.angularVelocity.magnitude * myAB.angularDamping)) * currentAgentMoveParams.direction;
                myAB.AddRelativeTorque(new Vector3(0, -relativeTorque, 0));

                //Vector3 relativeDecTorque = currentAgentMoveParams.agentMass * (Mathf.Pow(beginDecelerationSpeed,2) / (2 * decelerationDistance)) * -Vector3.forward;
                //Debug.Log("Agent's traveled this far: " + distanceMovedSoFar + ". Applying relative force of " + relativeDecTorque + " to delecerate " + currentAgentMoveParams.agentMass + " kg of agent! Velocity is currently " + myAB.velocity.magnitude);
            // CASE: Cruise
            } else {
                // Debug.Log("CCCCCCCCCCCCCCCCCCCCCCCCCCCC");
                float relativeTorque = (((currentAgentMoveParams.speed - myAB.angularVelocity.magnitude) * myAB.inertiaTensor.y) + (myAB.angularVelocity.magnitude * myAB.angularDamping)) * currentAgentMoveParams.direction;
                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));
            }
            
            Vector3 currentRotation = myAB.transform.eulerAngles;
            currentAgentMoveParams.cachedPositions[currentAgentMoveParams.oldestCachedIndex] = currentRotation;

            distanceMovedSoFar = Mathf.Deg2Rad * Mathf.Abs(currentRotation.y - currentAgentMoveParams.initialTransformation.y);
            // Debug.Log("MOOOOOOOOOVING TOWARDS " + currentAgentMoveParams.distance + ", CURRENTLY AT " + distanceMovedSoFar);
        }

        //iterate next index in cache, loop back to index 0 as we get newer positions
        currentAgentMoveParams.oldestCachedIndex = (currentAgentMoveParams.oldestCachedIndex + 1) % currentAgentMoveParams.positionCacheSize;
        // Debug.Log($"current index: {currentAgentMoveParams.oldestCachedIndex}");

        //this is here so the first time, iterating through the [0] index we don't check, only the second time and beyond
        //every time we loop back around the cached positions, check if we effectively stopped moving
        if (currentAgentMoveParams.oldestCachedIndex == 0) {            
            //flag for shouldHalt to check if we should, in fact, halt
            checkStandardDev = true;
            // Debug.Log($"current index after looped: {currentAgentMoveParams.oldestCachedIndex}");
        } else {
            checkStandardDev = false;
        }

        // otherwise we have a hard timer to stop movement so we don't move forever and crash unity
        currentAgentMoveParams.timePassed += Time.deltaTime;

        // we are set to be in an idle state so return and do nothing
        return;
    }

    public bool ShouldHalt() {
            // Debug.Log("checking ArticulatedAgentController shouldHalt");
            bool shouldStop = false;
            ArticulatedAgentSolver a = this;
            // Debug.Log($"checking agent: {a.transform.name}");
            Debug.Log($"distance moved so far is: {a.distanceMovedSoFar}");
            Debug.Log($"current velocity is: {this.transform.GetComponent<ArticulationBody>().velocity.magnitude}");
            
            //check agent to see if it has halted or not
            if (!a.shouldHalt(
                distanceMovedSoFar: a.distanceMovedSoFar,
                cachedPositions: a.currentAgentMoveParams.cachedPositions,
                tolerance: a.currentAgentMoveParams.tolerance
            )) {
                //if any single joint is still not halting, return false
//                Debug.Log("still not done, don't halt yet");
                shouldStop = false;
                return shouldStop;
            } else {
                //this joint returns that it should stop!
                Debug.Log($"halted! Return shouldStop! Distance moved: {a.distanceMovedSoFar}");
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
        float distanceMovedSoFar,
        Vector3[] cachedPositions,
        float tolerance) {
        
        bool shouldHalt = false;

        
        //halt if positions/rotations are within tolerance and effectively not changing
        // Debug.Log($"checkStandardDev is: {checkStandardDev}");
        
        Debug.Log("Distance moved is " + distanceMovedSoFar + ", and distance to exceed is " + currentAgentMoveParams.distance);
        if (checkStandardDev) {
            if (CheckArrayWithinStandardDeviation(cachedPositions, tolerance)) {
                shouldHalt = true;
                // IdleAllStates();
                Debug.Log("halt due to position delta within tolerance");
                checkStandardDev = false;
                return shouldHalt;
            }
        }
        //check if the amount moved/rotated exceeds this agents target
        else if (distanceMovedSoFar >= currentAgentMoveParams.distance) {
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

    // private void IdleAllStates() {
        // if (jointAxisType == JointAxisType.Lift)
        //     liftState = ArmLiftState.Idle;

        // if (jointAxisType == JointAxisType.Extend)
        //     extendState = ArmExtendState.Idle;

        // if (jointAxisType == JointAxisType.Rotate)
        //     rotateState = ArmRotateState.Idle;

        // //reset current movement params
        // this.currentArmMoveParams = null;
    // }

    //check if all values in the array are within a standard deviation or not
    bool CheckArrayWithinStandardDeviation(Vector3[] values, float standardDeviation) {
        float[] mags = new float[values.Length];
        
        // Calculate the mean value of the array
        for(int i = 0; i < mags.Length; i++) {
            mags[i] = values[i].magnitude;
        }
        float mean = mags.Average();

        // Calculate the sum of squares of the differences between each value and the mean
        float sumOfSquares = 0.0f;
        foreach (float mag in mags) {
            //Debug.Log(value);
            sumOfSquares += (mag - mean) * (mag - mean);
        }

        // Calculate the standard deviation of the array
        float arrayStdDev = (float)Mathf.Sqrt(sumOfSquares / values.Length);

        // Check if the standard deviation of the array is within the specified range
        return arrayStdDev <= standardDeviation;
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
