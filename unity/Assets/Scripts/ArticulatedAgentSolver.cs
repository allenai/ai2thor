using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;

public enum ABAgentState { Idle = 0, Moving = 1, Rotating = 2 };

public class ABAgentPhysicsParams {
    public float distance;
    public float speed;
    public float maxTimePassed;
    public float maxForce;

    public float minMovementPerSecond;
    public float haltCheckTimeWindow;
    public int direction;
    public float timePassed = 0.0f;
    public List<double> cachedPositions;
    public List<double> cachedFixedDeltaTimes;
}

public class AgentMoveParams : ABAgentPhysicsParams {
    public ABAgentState agentState;
    public float acceleration;
    public float agentMass;
    public Vector3 initialTransformation;
}

public class ArticulatedAgentSolver : MonoBehaviour, MovableContinuous {
    //pass in arm move parameters for Action based movement
    public AgentMoveParams currentAgentMoveParams = new AgentMoveParams();
    //reference for this joint's articulation body
    public ArticulationBody myAB;
    public ABAgentState agentState = ABAgentState.Idle;
    public float distanceTransformedSoFar;
    public Vector3 prevStepTransformation, prevStepForward;
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
        currentAgentMoveParams.cachedPositions = new List<double>();
        currentAgentMoveParams.cachedFixedDeltaTimes = new List<double>();
        
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
        accelerationDistance = Mathf.Pow(currentAgentMoveParams.speed, 2) / (2 * currentAgentMoveParams.acceleration);
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

        double distanceTransformedThisFixedUpdate = 0f;

        Vector3 agentOrientedVelocity = myAB.transform.InverseTransformDirection(myAB.velocity);
        // Damping force for part of velocity that is not in the direction of movement
        float dampingForceX = Mathf.Clamp(-100f * agentOrientedVelocity.x * currentAgentMoveParams.agentMass, -200f, 200f);
        myAB.AddRelativeForce(new Vector3(dampingForceX, 0f, 0f));
        Debug.Log($"Damping force X equals: {dampingForceX} == clamp(-200 * {agentOrientedVelocity.x} * {currentAgentMoveParams.agentMass}, 100, 100)");

        if (currentAgentMoveParams.agentState == ABAgentState.Moving) {

            float currentSpeed = Mathf.Abs(agentOrientedVelocity.z);
            float forceScaler = 1f / fixedDeltaTime;

            // CASE: Accelerate - Apply force calculated from difference between intended distance and actual distance after amount of time that has passed
            if (distanceTransformedSoFar < accelerationDistance) {
                float desiredDistance = 0.5f * currentAgentMoveParams.acceleration * Mathf.Pow(currentAgentMoveParams.timePassed + fixedDeltaTime, 2);
                float distanceDelta = desiredDistance - distanceTransformedSoFar;

                // This shouldn't be the same formula as correcting for a velocityDelta, like in cruise-control mode, but for some reason, it's working great
                float relativeForce = forceScaler * distanceDelta * currentAgentMoveParams.agentMass * currentAgentMoveParams.direction;

                relativeForce = Mathf.Clamp(relativeForce, -currentAgentMoveParams.maxForce, currentAgentMoveParams.maxForce);
                Debug.Log(
                    $"1. distanceDelta is {distanceDelta}. Applying force of {relativeForce} = "
                    + $"clamp({currentAgentMoveParams.maxForce}, {forceScaler} * {distanceDelta} * {currentAgentMoveParams.agentMass} * {currentAgentMoveParams.direction})."
                );

                myAB.AddRelativeForce(new Vector3(0, 0, relativeForce));

            // CASE: Decelerate - Apply force calculated from difference between intended velocity and actual velocity at given distance remaining to travel
            } else if (distanceTransformedSoFar >= currentAgentMoveParams.distance - accelerationDistance) {
                if (beginDeceleration == false) {
                    beginDecelerationSpeed = currentSpeed;
                    beginDeceleration = true;
                }

                float desiredSpeed = beginDecelerationSpeed * (remainingDistance / accelerationDistance);
                float speedDelta = desiredSpeed - currentSpeed;
            
                float relativeForce = forceScaler * speedDelta * currentAgentMoveParams.agentMass * currentAgentMoveParams.direction;

                relativeForce = Mathf.Clamp(relativeForce, -currentAgentMoveParams.maxForce, currentAgentMoveParams.maxForce);
                Debug.Log(
                    $"3. speedDelta is {speedDelta}. Applying force of {relativeForce} = "
                    + $"clamp({currentAgentMoveParams.maxForce}, {forceScaler} * {speedDelta} * {currentAgentMoveParams.agentMass} * {currentAgentMoveParams.direction})."
                );

                myAB.AddRelativeForce(new Vector3(0, 0, relativeForce));
            
            // CASE: Cruise Control - Apply force calculated from difference between intended velocity and current velocity
            } else {
                float speedDelta = currentAgentMoveParams.speed - currentSpeed;

                float relativeForce = forceScaler * speedDelta * currentAgentMoveParams.agentMass * currentAgentMoveParams.direction;
                
                relativeForce = Mathf.Clamp(relativeForce, -currentAgentMoveParams.maxForce, currentAgentMoveParams.maxForce);
                Debug.Log(
                    $"2. speedDelta is {speedDelta}. Applying force of {relativeForce} = "
                    + $"clamp({currentAgentMoveParams.maxForce}, {forceScaler} * {speedDelta} * {currentAgentMoveParams.agentMass} * {currentAgentMoveParams.direction})."
                );

                myAB.AddRelativeForce(new Vector3(0, 0, relativeForce));
            }

            // Begin checks to see if we have stopped moving or if we need to stop moving

            // Cache the position at the moment
            Vector3 currentPosition = myAB.transform.position;

            // Average current and previous forward-vectors to get an averaged forward-vector over the course of previous physics-step
            Vector3 currentForward = (myAB.transform.forward + prevStepForward) / 2;

            // Determine (positive) distance covered based on forward progress, relative to previous physics-step's averaged forward-vector
            distanceTransformedThisFixedUpdate = Mathf.Clamp(
                Vector3.Dot(currentPosition - prevStepTransformation, currentForward * currentAgentMoveParams.direction),
                0,
                Mathf.Infinity
            );

            // Store current values for comparing with next FixedUpdate
            prevStepTransformation = currentPosition;
            prevStepForward = myAB.transform.forward;

        } else if (currentAgentMoveParams.agentState == ABAgentState.Rotating) {

            // When rotating the agent shouldn't be moving forward/backwards but its wheels are moving so we use a smaller
            // damping force to counteract forward/backward movement (as opposed to the force used for lateral movement
            // above)
            float dampingForceZ = Mathf.Clamp(-100f * agentOrientedVelocity.z * currentAgentMoveParams.agentMass, -50f, 50f);
            myAB.AddRelativeForce(new Vector3(0f, 0f, dampingForceZ));
            Debug.Log($"Damping force Z equals: {dampingForceZ} == clamp(-100 * {agentOrientedVelocity.z} * {currentAgentMoveParams.agentMass}, -50, 50)");
            
            float currentAngularSpeed = Mathf.Abs(myAB.angularVelocity.y);
            float forceScaler = 1f / fixedDeltaTime;

            // CASE: Accelerate - Apply force calculated from difference between intended distance and actual distance after amount of time that has passed
            if (distanceTransformedSoFar < accelerationDistance) {
                float desiredAngularDistance = 0.5f * currentAgentMoveParams.acceleration * Mathf.Pow(currentAgentMoveParams.timePassed + fixedDeltaTime, 2);
                float angularDistanceDelta = desiredAngularDistance - distanceTransformedSoFar;

                float relativeTorque = Mathf.Clamp(
                    forceScaler * angularDistanceDelta * myAB.inertiaTensor.y * currentAgentMoveParams.direction,
                    -currentAgentMoveParams.maxForce,
                    currentAgentMoveParams.maxForce
                );

                Debug.Log(
                    $"1. distanceDelta is {angularDistanceDelta}. Applying torque of {relativeTorque} = "
                    + $"clamp(one schmillion Newton-meters, {forceScaler} * {angularDistanceDelta} * {myAB.inertiaTensor.y} * {currentAgentMoveParams.direction})."
                );
                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));

            // CASE: Decelerate - Apply force calculated from difference between intended angular velocity and actual angular velocity at given angular distance remaining to travel
            } else if (distanceTransformedSoFar >= currentAgentMoveParams.distance - accelerationDistance) {
                if (beginDeceleration == false) {
                    beginDecelerationSpeed = currentAngularSpeed;
                    beginDeceleration = true;
                }
                
                float desiredAngularSpeed = beginDecelerationSpeed * (remainingDistance / accelerationDistance);
                float angularSpeedDelta = desiredAngularSpeed - currentAngularSpeed;
                
                float relativeTorque = Mathf.Clamp(
                    forceScaler * angularSpeedDelta * myAB.inertiaTensor.y * currentAgentMoveParams.direction,
                    -currentAgentMoveParams.maxForce,
                    currentAgentMoveParams.maxForce
                );

                Debug.Log(
                    $"3. speedDelta is {angularSpeedDelta}. Applying torque of {relativeTorque} = "
                    + $"clamp(one schmillion Newton-meters, {forceScaler} * {angularSpeedDelta} * {myAB.inertiaTensor.y} * {currentAgentMoveParams.direction})."
                );
                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));
                
            // CASE: Cruise - Apply force calculated from difference between intended angular velocity and current angular velocity
            } else {
                float angularSpeedDelta = currentAgentMoveParams.speed - currentAngularSpeed;

                float relativeTorque = Mathf.Clamp(
                    forceScaler * angularSpeedDelta * myAB.inertiaTensor.y * currentAgentMoveParams.direction,
                    -currentAgentMoveParams.maxForce,
                    currentAgentMoveParams.maxForce
                );

                Debug.Log(
                    $"2. speedDelta is {angularSpeedDelta}. Applying torque of {relativeTorque} = "
                    + $"clamp(one schmillion Newton-meters, {forceScaler} * {angularSpeedDelta} * {myAB.inertiaTensor.y} * {currentAgentMoveParams.direction})."
                );
                myAB.AddRelativeTorque(new Vector3(0, relativeTorque, 0));
            }
            
            // Begin checks to see if we have stopped moving or if we need to stop moving
            
            // Cache the rotation at the moment
            Vector3 currentRotation = myAB.transform.eulerAngles;

            // Determine (positive) angular distance covered
            distanceTransformedThisFixedUpdate = Mathf.Deg2Rad * Mathf.Clamp(
                (currentRotation.y - prevStepTransformation.y) * currentAgentMoveParams.direction, 0, Mathf.Infinity
            );

            // Store current values for comparing with next FixedUpdate
            prevStepTransformation = currentRotation;
        } else if (currentAgentMoveParams.agentState == ABAgentState.Idle) {
            // Do nothing
        } else {
            throw new System.NotImplementedException($"Agent is not in a valid movement state: {currentAgentMoveParams.agentState}.");
        }

        // Cache data used to check if we have stopped rotating or if we need to stop rotating
        currentAgentMoveParams.cachedPositions.Add(distanceTransformedThisFixedUpdate);
        currentAgentMoveParams.cachedFixedDeltaTimes.Add(fixedDeltaTime);
        distanceTransformedSoFar += (float)distanceTransformedThisFixedUpdate;

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
                cachedFixedTimeDeltas: a.currentAgentMoveParams.cachedFixedDeltaTimes,
                minMovementPerSecond: a.currentAgentMoveParams.minMovementPerSecond,
                haltCheckTimeWindow: a.currentAgentMoveParams.haltCheckTimeWindow
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
        List<double> cachedPositions,
        List<double> cachedFixedTimeDeltas,
        double minMovementPerSecond,
        double haltCheckTimeWindow
    ) {
        
        bool shouldHalt = false;
        
        //halt if positions/rotations are within tolerance and effectively not changing

        // Debug.Log("Distance moved is " + distanceTransformedSoFar + ", and distance to exceed is " + currentAgentMoveParams.distance);
        if (
            ApproximatelyNoChange(
                positionDeltas: cachedPositions,
                timeDeltas: cachedFixedTimeDeltas,
                minMovementPerSecond: minMovementPerSecond,
                haltCheckTimeWindow: haltCheckTimeWindow
            )
        ) {
            shouldHalt = true;
            // IdleAllStates();
            Debug.Log("halt due to position delta within tolerance");
            return shouldHalt;
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
    public static bool ApproximatelyNoChange(
        List<double> positionDeltas, List<double> timeDeltas, double minMovementPerSecond, double haltCheckTimeWindow
    ) {
        double totalTime = 0f;
        double totalDistanceTraveled = 0f;
        for (int i = positionDeltas.Count - 1; i >= 0; i--) {
            totalTime += timeDeltas[i];
            totalDistanceTraveled += Mathf.Abs((float) positionDeltas[i]);
            if (totalTime >= haltCheckTimeWindow) {
                break;
            }
        }

        if (totalTime < haltCheckTimeWindow) {
            // Not enough time recorded to make a decision
            return false;
        }

        return totalDistanceTraveled / totalTime < minMovementPerSecond;
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
