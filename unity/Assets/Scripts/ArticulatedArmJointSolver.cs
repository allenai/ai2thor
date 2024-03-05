using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;

public enum ArmLiftState { Idle = 0, MovingDown = -1, MovingUp = 1 };
public enum ArmExtendState { Idle = 0, MovingBackward = -1, MovingForward = 1 };
public enum ArmRotateState { Idle = 0, Negative = -1, Positive = 1 };
public enum JointAxisType { Unassigned, Extend, Lift, Rotate };

public class ArmMoveParams : ABAgentPhysicsParams {
    public ArticulatedAgentController controller;
    public bool useLimits;
    public ArticulatedArmExtender armExtender;
    public float initialJointPosition;
}

public class ArticulatedArmJointSolver : MonoBehaviour {
    [Header("What kind of joint is this?")]
    public JointAxisType jointAxisType = JointAxisType.Unassigned;
    [Header("State of this joint's movements")]
    [SerializeField]
    public ArmRotateState rotateState = ArmRotateState.Idle;
    [SerializeField]
    public ArmLiftState liftState = ArmLiftState.Idle;
    [SerializeField]
    public ArmExtendState extendState = ArmExtendState.Idle;

    //pass in arm move parameters for Action based movement
    public ArmMoveParams currentArmMoveParams;

    //reference for this joint's articulation body
    public ArticulationBody myAB;

    public float distanceTransformedSoFar, prevStepTransformation;

    //these limits are from the Articulation Body drive's lower and uppper limit
    public float lowerArmBaseLimit = -0.1832155f;
    public float upperArmBaseLimit = 0.9177839f;
    public float lowerArmExtendLimit = 0.0f;
    public float upperArmExtendLimit = 0.516f;


    void Start() {
        myAB = this.GetComponent<ArticulationBody>();
    }

    public void PrepToControlJointFromAction(ArmMoveParams armMoveParams) {
        Debug.Log($"preparing joint {this.transform.name} to move");
        if (Mathf.Approximately(armMoveParams.distance, 0.0f)) {
            Debug.Log("Error! distance to move must be nonzero");
            return;
        }

        // Zero out distance delta tracking
        distanceTransformedSoFar = 0.0f;

        // Set current arm move params to prep for movement in fixed update
        currentArmMoveParams = armMoveParams;

        //initialize the buffer to cache positions to check for later
        currentArmMoveParams.cachedPositions = new List<double>();
        currentArmMoveParams.cachedFixedDeltaTimes = new List<double>();

        //snapshot the initial joint position to compare with later during movement
        currentArmMoveParams.initialJointPosition = myAB.jointPosition[0];
        prevStepTransformation = myAB.jointPosition[0];

        //we are a lift type joint, moving along the local y axis
        if (jointAxisType == JointAxisType.Lift) {
            if (liftState == ArmLiftState.Idle) {

                //set if we are moving up or down based on sign of distance from input
                if (armMoveParams.direction < 0) {
                    Debug.Log("setting lift state to move down");
                    liftState = ArmLiftState.MovingDown;
                } else if (armMoveParams.direction > 0) {
                    Debug.Log("setting lift state to move up");
                    liftState = ArmLiftState.MovingUp;
                }
            }
        }

        //we are an extending joint, moving along the local z axis
        else if (jointAxisType == JointAxisType.Extend) {
            if (extendState == ArmExtendState.Idle) {

                currentArmMoveParams.armExtender = this.GetComponent<ArticulatedArmExtender>();
                //currentArmMoveParams.armExtender.Init();

                //set if we are extending or retracting
                if (armMoveParams.direction < 0) {
                    extendState = ArmExtendState.MovingBackward;
                } else if (armMoveParams.direction > 0) {
                    extendState = ArmExtendState.MovingForward;
                }
            }
        
        //we are a rotating joint, rotating around the local y axis
        } else if (jointAxisType == JointAxisType.Rotate) {
            if (rotateState == ArmRotateState.Idle) {

                //set if we are rotating left or right
                if (armMoveParams.direction < 0) {
                    rotateState = ArmRotateState.Negative;
                } else if (armMoveParams.direction > 0) {
                    rotateState = ArmRotateState.Positive;
                }
            }
        }
    }

    public void ControlJointFromAction(float fixedDeltaTime) {
        
        if (currentArmMoveParams == null) {
            return;
        }

        double distanceTransformedThisFixedUpdate = 0f;

        // Debug.Log($"Type: {jointAxisType.ToString()} pos");
        //we are a lift type joint
        if (jointAxisType == JointAxisType.Lift) {
            //if instead we are moving up or down actively
            if (liftState != ArmLiftState.Idle) {
                //Debug.Log("start ControlJointFromAction for axis type LIFT");
                var drive = myAB.yDrive;
                float currentPosition = myAB.jointPosition[0];
                float targetPosition = currentPosition + (float)liftState * fixedDeltaTime * currentArmMoveParams.speed;
                drive.target = targetPosition;
                myAB.yDrive = drive;

                // Begin checks to see if we have stopped moving or if we need to stop moving

                // Determine (positive) distance covered
                distanceTransformedSoFar = Mathf.Abs(currentPosition - currentArmMoveParams.initialJointPosition);
                distanceTransformedThisFixedUpdate = Mathf.Abs(currentPosition - prevStepTransformation);

                // Store current values for comparing with next FixedUpdate
                prevStepTransformation = currentPosition;

                if (currentArmMoveParams.useLimits) {
                    Debug.Log("extending/retracting arm with limits");
                    float distanceRemaining = currentArmMoveParams.distance - distanceTransformedSoFar;

                    // New version of up-down drive
                    float forceAppliedFromRest = currentArmMoveParams.maxForce;
                    float slowDownTime = 5 * fixedDeltaTime;
                    drive.forceLimit = forceAppliedFromRest;

                    float direction = (float)liftState;
                    targetPosition = currentPosition + direction * distanceRemaining;

                    float offset = 1e-2f;
                    if (liftState == ArmLiftState.MovingUp) {
                        drive.upperLimit = Mathf.Min(upperArmBaseLimit, targetPosition + offset);
                        drive.lowerLimit = Mathf.Min(Mathf.Max(lowerArmBaseLimit, currentPosition), targetPosition);
                    } else if (liftState == ArmLiftState.MovingDown) {
                        drive.lowerLimit = Mathf.Max(lowerArmBaseLimit, targetPosition);
                        drive.upperLimit = Mathf.Max(Mathf.Min(upperArmBaseLimit, currentPosition + offset), targetPosition + offset);
                    }

                    drive.damping = Mathf.Min(forceAppliedFromRest / currentArmMoveParams.speed, 10000f);

                    float signedDistanceRemaining = direction * distanceRemaining;

                    float maxSpeed = Mathf.Max(distanceRemaining / fixedDeltaTime, 0f); // Never move so fast we'll overshoot in 1 step
                    currentArmMoveParams.speed = Mathf.Min(maxSpeed, currentArmMoveParams.speed);

                    float curVelocity = myAB.velocity.y;
                    float curSpeed = Mathf.Abs(curVelocity);

                    float switchWhenThisClose = 0.01f;
                    bool willReachTargetSoon = (
                        distanceRemaining < switchWhenThisClose || (
                            direction * curVelocity > 0f
                            && distanceRemaining / Mathf.Max(curSpeed, 1e-7f) <= slowDownTime
                        )
                    );

                    drive.target = targetPosition;

                    if (willReachTargetSoon) {
                        drive.stiffness = 10000f;
                        drive.targetVelocity = -direction * (distanceRemaining / slowDownTime);
                    } else {
                        drive.stiffness = 0f;
                        drive.targetVelocity = -direction * currentArmMoveParams.speed;
                    }

                    float curForceApplied = drive.stiffness * (targetPosition - currentPosition) + drive.damping * (drive.targetVelocity - curVelocity);

                    Debug.Log($"position: {currentPosition} ({targetPosition} target) ({willReachTargetSoon} near target)");
                    Debug.Log($"drive limits: {drive.lowerLimit}, {drive.upperLimit}");
                    Debug.Log($"distance moved: {distanceTransformedSoFar} ({currentArmMoveParams.distance} target)");
                    Debug.Log($"velocity: {myAB.velocity.y} ({drive.targetVelocity} target)");
                    Debug.Log($"current force applied: {curForceApplied}");

                    //this sets the drive to begin moving to the new target position
                    myAB.yDrive = drive;
                }
            }
        }

        //for extending arm joints, assume all extending joints will be in some state of movement based on input distance
        //the shouldHalt function in ArticulatedAgentController will wait for all individual extending joints to go back to
        //idle before actionFinished is called
        else if (jointAxisType == JointAxisType.Extend) {
            if (extendState != ArmExtendState.Idle) {
                //Debug.Log("start ControlJointFromAction for axis type EXTEND");
                var drive = myAB.zDrive;
                float currentPosition = myAB.jointPosition[0];
                // Begin checks to see if we have stopped moving or if we need to stop moving

                // Determine (positive) distance covered
                distanceTransformedSoFar = Mathf.Abs(currentPosition - currentArmMoveParams.initialJointPosition);
                distanceTransformedThisFixedUpdate = Mathf.Abs(currentPosition - prevStepTransformation);

                // Store current values for comparing with next FixedUpdate
                prevStepTransformation = currentPosition;


                if (!currentArmMoveParams.useLimits) {
                    float targetPosition = currentPosition + (float)extendState * fixedDeltaTime * currentArmMoveParams.speed;
                    drive.target = targetPosition;
                    myAB.zDrive = drive;

                    Debug.Log($"currentPosition: {currentPosition}");
                    Debug.Log($"targetPosition: {targetPosition}");
                } else {
                    float distanceRemaining = currentArmMoveParams.distance - distanceTransformedSoFar;
                    Debug.Log("DISTANCE REMAINING: " + distanceRemaining);

                    float forceAppliedFromRest = currentArmMoveParams.maxForce;
                    float slowDownTime = 5 * fixedDeltaTime;
                    drive.forceLimit = forceAppliedFromRest;

                    float direction = (float)extendState;
                    float targetPosition = currentPosition + direction * distanceRemaining;

                    float offset = 1e-2f;
                    if (extendState == ArmExtendState.MovingForward) {
                        drive.upperLimit = Mathf.Min(upperArmExtendLimit, targetPosition + offset);
                        drive.lowerLimit = Mathf.Min(Mathf.Max(lowerArmExtendLimit, currentPosition), targetPosition);
                    } else if (extendState == ArmExtendState.MovingBackward) {
                        drive.lowerLimit = Mathf.Max(lowerArmExtendLimit, targetPosition);
                        drive.upperLimit = Mathf.Max(Mathf.Min(upperArmExtendLimit, currentPosition + offset), targetPosition + offset);
                    }

                    drive.damping = Mathf.Min(forceAppliedFromRest / currentArmMoveParams.speed, 10000f);

                    float signedDistanceRemaining = direction * distanceRemaining;

                    float maxSpeed = Mathf.Max(distanceRemaining / fixedDeltaTime, 0f); // Never move so fast we'll overshoot in 1 step
                    currentArmMoveParams.speed = Mathf.Min(maxSpeed, currentArmMoveParams.speed);

                    float curVelocity = myAB.velocity.z;
                    float curSpeed = Mathf.Abs(curVelocity);

                    float switchWhenThisClose = 0.01f;
                    bool willReachTargetSoon = (
                        distanceRemaining < switchWhenThisClose || (
                            direction * curVelocity > 0f
                            && distanceRemaining / Mathf.Max(curSpeed, 1e-7f) <= slowDownTime
                        )
                    );

                    drive.target = targetPosition;

                    if (willReachTargetSoon) {
                        drive.stiffness = 100000f;
                        drive.targetVelocity = -direction * (distanceRemaining / slowDownTime);
                    } else {
                        drive.stiffness = 0f;
                        Debug.Log($"stiffness should be 0 but it is: {drive.stiffness}");
                        drive.targetVelocity = -direction * currentArmMoveParams.speed;
                    }

                    float curForceApplied = drive.stiffness * (targetPosition - currentPosition) + drive.damping * (drive.targetVelocity - curVelocity);

                    Debug.Log($"position: {currentPosition} ({targetPosition} target) ({willReachTargetSoon} near target)");
                    Debug.Log($"drive limits: {drive.lowerLimit}, {drive.upperLimit}");
                    Debug.Log($"distance moved: {distanceTransformedSoFar} ({currentArmMoveParams.distance} target)");
                    Debug.Log($"velocity: {myAB.velocity.y} ({drive.targetVelocity} target)");
                    Debug.Log($"current force applied: {curForceApplied}");

                    //this sets the drive to begin moving to the new target position
                    myAB.zDrive = drive;
                }

                //update colliders and arm extend sleeves animating
                currentArmMoveParams.armExtender.Extend();
            }
        }

        //if we are a revolute joint
        else if (jointAxisType == JointAxisType.Rotate) {
            if (rotateState != ArmRotateState.Idle) {
                //seems like revolute joints always only have an xDrive, and to change where its rotating
                //you just rotate the anchor itself, but always access the xDrive
                var drive = myAB.xDrive;
                //somehow this is already in radians, so we are converting to degrees here
                float currentRotation = Mathf.Rad2Deg * myAB.jointPosition[0];
                // i think this speed is in rads per second?????
                float targetRotation = currentRotation + (float)rotateState * currentArmMoveParams.speed * fixedDeltaTime;
                drive.target = targetRotation;
                myAB.xDrive = drive;

                // Begin checks to see if we have stopped moving or if we need to stop moving
                
                // Determine (positive) angular distance covered
                distanceTransformedSoFar = Mathf.Abs(currentRotation - currentArmMoveParams.initialJointPosition);
                distanceTransformedThisFixedUpdate = Mathf.Abs(currentRotation - prevStepTransformation);

                distanceTransformedSoFar = Mathf.Abs(currentRotation - Mathf.Rad2Deg * currentArmMoveParams.initialJointPosition);
            }
        }

        currentArmMoveParams.cachedPositions.Add(distanceTransformedThisFixedUpdate);
        currentArmMoveParams.cachedFixedDeltaTimes.Add(fixedDeltaTime);

        // Otherwise we have a hard timer to stop movement so we don't move forever and crash unity
        currentArmMoveParams.timePassed += fixedDeltaTime;
        // Debug.Log($"time passed: {currentArmMoveParams.timePassed}");

        return;
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

        //if we are already in an idle, state, immeidately return true
        if (jointAxisType == JointAxisType.Lift && liftState == ArmLiftState.Idle) {
            return true;
        }

        if (jointAxisType == JointAxisType.Extend && extendState == ArmExtendState.Idle) {
            return true;
        }

        if (jointAxisType == JointAxisType.Rotate && rotateState == ArmRotateState.Idle) {
            return true;
        }

        bool shouldHalt = false;

        //halt if positions/rotations are within tolerance and effectively not changing
        if (
            ArticulatedAgentSolver.ApproximatelyNoChange(
                positionDeltas: cachedPositions,
                timeDeltas: cachedFixedTimeDeltas,
                minMovementPerSecond: minMovementPerSecond,
                haltCheckTimeWindow: haltCheckTimeWindow
            )
        ) {
            shouldHalt = true;
            IdleAllStates();
            Debug.Log("halt due to position delta within tolerance");
            return shouldHalt;
        }
        //check if the amount moved/rotated exceeds this joints target
        else if (distanceTransformedSoFar >= currentArmMoveParams.distance) {
            shouldHalt = true;
            IdleAllStates();
            Debug.Log("halt due to distance reached/exceeded");
            return shouldHalt;
        }


        //hard check for time limit
        else if (currentArmMoveParams.timePassed >= currentArmMoveParams.maxTimePassed) {
            shouldHalt = true;
            IdleAllStates();
            Debug.Log("halt from timeout");
            return shouldHalt;
        }

        return shouldHalt;
    }

    private void IdleAllStates() {
        if (jointAxisType == JointAxisType.Lift) {
            liftState = ArmLiftState.Idle;
        } if (jointAxisType == JointAxisType.Extend) {
            extendState = ArmExtendState.Idle;
        } if (jointAxisType == JointAxisType.Rotate) {
            rotateState = ArmRotateState.Idle;
        }

        //reset current movement params
        this.currentArmMoveParams = null;
    }

}
