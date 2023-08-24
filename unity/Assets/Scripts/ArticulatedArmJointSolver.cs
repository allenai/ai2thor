using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;

public enum ArmLiftState { Idle = 0, MovingDown = -1, MovingUp = 1 };
public enum ArmExtendState { Idle = 0, MovingBackward = -1, MovingForward = 1 };
public enum ArmRotateState { Idle = 0, Negative = -1, Positive = 1 };
public enum JointAxisType { Unassigned, Extend, Lift, Rotate };

public class ArmMoveParams {
    public ArticulatedAgentController controller;
    public float distance;
    public float speed;
    public float tolerance;
    public float maxTimePassed;
    public int positionCacheSize;

    //these are used during movement in fixed update (or whenever physics step executes)
    //can probably move these out of this class and into the joint solver class iself????
    public int direction;
    public float timePassed = 0.0f;
    public double[] cachedPositions;
    public int oldestCachedIndex;
    public float initialJointPosition;

    public bool useLimits;

    public ArticulatedArmExtender armExtender;
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

    public float distanceMovedSoFar, prevStepTransformation;
    public double distanceTransformedThisFixedUpdate;
    public bool checkForMotion;

    public float lowerArmBaseLimit = -0.1832155f;
    public float upperArmBaseLimit = 0.9177839f;


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
        distanceMovedSoFar = 0.0f;

        // Set current arm move params to prep for movement in fixed update
        currentArmMoveParams = armMoveParams;

        //initialize the buffer to cache positions to check for later
        currentArmMoveParams.cachedPositions = new double[currentArmMoveParams.positionCacheSize];

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
                currentArmMoveParams.armExtender.Init();

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

    // public void AnimateArmExtend(float armExtensionLength) {
        
    //     var arm2 = this.gameObject.transform.parent.Find("stretch_robot_arm_2");
    //     var arm3 = this.gameObject.transform.parent.Find("stretch_robot_arm_3");
    //     var arm4 = this.gameObject.transform.parent.Find("stretch_robot_arm_4");
    //     var arm5 = this.gameObject.transform.parent.Find("stretch_robot_arm_5");

    //      //Extend each part of arm by one-quarter of extension length, in local z-direction
    //     arm2.localPosition = new Vector3 (0, 0, 1 * (armExtensionLength / 4) + 0.01300028f);
    //     arm3.localPosition = new Vector3 (0, 0, 2 * (armExtensionLength / 4) + 0.01300049f);
    //     arm4.localPosition = new Vector3 (0, 0, 3 * (armExtensionLength / 4) + 0.01300025f);
    //     arm5.localPosition = new Vector3 (0, 0, 4 * (armExtensionLength / 4) + 0.0117463f);


    // }

    public void ControlJointFromAction(float fixedDeltaTime) {
        
        if (currentArmMoveParams == null) {
            return;
        }

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
                distanceMovedSoFar = Mathf.Abs(currentPosition - currentArmMoveParams.initialJointPosition);
                distanceTransformedThisFixedUpdate = Mathf.Abs(currentPosition - prevStepTransformation);

                // Cache data used to check if we have stopped rotating or if we need to stop rotating
                currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = (double)distanceTransformedThisFixedUpdate;

                // Store current values for comparing with next FixedUpdate
                prevStepTransformation = currentPosition;

                if (currentArmMoveParams.useLimits) {
                    float distanceRemaining = currentArmMoveParams.distance - distanceMovedSoFar;

                    // New version of up-down drive
                    float forceAppliedFromRest = 500f;
                    float slowDownTime = 5 * fixedDeltaTime;
                    drive.forceLimit = forceAppliedFromRest * 2;

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

                    drive.damping = forceAppliedFromRest / currentArmMoveParams.speed;

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
                    Debug.Log($"distance moved: {distanceMovedSoFar} ({currentArmMoveParams.distance} target)");
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
                float targetPosition = currentPosition + (float)extendState * fixedDeltaTime * currentArmMoveParams.speed;
                drive.target = targetPosition;
                myAB.zDrive = drive;

                // Begin checks to see if we have stopped moving or if we need to stop moving

                // Determine (positive) distance covered
                distanceMovedSoFar = Mathf.Abs(currentPosition - currentArmMoveParams.initialJointPosition);
                distanceTransformedThisFixedUpdate = Mathf.Abs(currentPosition - prevStepTransformation);

                // Cache data used to check if we have stopped rotating or if we need to stop rotating
                currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = (double)distanceTransformedThisFixedUpdate;

                // Store current values for comparing with next FixedUpdate
                prevStepTransformation = currentPosition;

                currentArmMoveParams.armExtender.Extend(distanceMovedSoFar);
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
                distanceMovedSoFar = Mathf.Abs(currentRotation - currentArmMoveParams.initialJointPosition);
                distanceTransformedThisFixedUpdate = Mathf.Abs(currentRotation - prevStepTransformation);
                
                // Cache the rotation at the moment
                currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = (double)distanceTransformedThisFixedUpdate;

                distanceMovedSoFar = Mathf.Abs(currentRotation - Mathf.Rad2Deg * currentArmMoveParams.initialJointPosition);
            }
        }

        // Iterate next index in cache, loop back to index 0 as we get newer positions
        // Debug.Log("Okay, so I made it here: " + currentArmMoveParams.oldestCachedIndex);
        currentArmMoveParams.oldestCachedIndex = (currentArmMoveParams.oldestCachedIndex + 1) % currentArmMoveParams.positionCacheSize;
        // Debug.Log($"current index: {currentArmMoveParams.oldestCachedIndex}");
        
        // This is here so the first time, iterating through the [0] index we don't check, only the second time and beyond
        // every time we loop back around the cached positions, check if we effectively stopped moving
        if (currentArmMoveParams.oldestCachedIndex == 0) {            
            // Flag for shouldHalt to check if we should, in fact, halt
            checkForMotion = true;
            // Debug.Log($"current index after looped: {currentAgentMoveParams.oldestCachedIndex}");
        } else {
            checkForMotion = false;
        }
        
        // Otherwise we have a hard timer to stop movement so we don't move forever and crash unity
        currentArmMoveParams.timePassed += fixedDeltaTime;
        Debug.Log($"time passed: {currentArmMoveParams.timePassed}");

        return;
    }

    //do checks based on what sort of joint I am
    //have a bool or something to check if you should check std dev
    public bool shouldHalt(
        float distanceMovedSoFar,
        double[] cachedPositions,
        float tolerance) {

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
        //Debug.Log($"checkForMotion is: {checkForMotion}");
        if (checkForMotion) {
            if (CheckArrayForMotion(cachedPositions, tolerance)) {
                shouldHalt = true;
                IdleAllStates();
                Debug.Log("halt due to position delta within tolerance");
                checkForMotion = false;
                return shouldHalt;
            }
        }

        //check if the amount moved/rotated exceeds this joints target
        else if (distanceMovedSoFar >= currentArmMoveParams.distance) {
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
}
