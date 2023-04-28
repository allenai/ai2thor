using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;

public enum ArmLiftState { Idle = 0, MovingDown = -1, MovingUp = 1 };
public enum ArmExtendState {Idle = 0, MovingBackward = -1, MovingForward = 1};
public enum ArmRotateState {Idle = 0, Negative = -1, Positive = 1};
public enum JointAxisType {Unassigned, Extend, Lift, Rotate};

public class ArmMoveParams
{
    public ArticulatedAgentController controller;
    public float distance;
    public float speed;
    public float tolerance;
    public float maxTimePassed;
    public int positionCacheSize;

    //these are used during movement in fixed update
    public int direction;
    public float timePassed = 0.0f;
    public float[] cachedPositions;
    public int oldestCachedIndex;
    public float initialJointPosition;
}

public class ArticulatedArmJointSolver : MonoBehaviour
{
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
    private ArmMoveParams currentArmMoveParams;

    //reference for this joint's articulation body
    public ArticulationBody myAB;

    void Start() 
    {
        myAB = this.GetComponent<ArticulationBody>();
    }  

    public void PrepToControlJointFromAction(ArmMoveParams armMoveParams)
    {
        if(Mathf.Approximately(armMoveParams.distance, 0.0f))
        {
            Debug.Log("Error! distance to move must be nonzero");
            return;
        }

        //we are a lift type joint, moving along the local y axis
        if(jointAxisType == JointAxisType.Lift)
        {
            if(liftState == ArmLiftState.Idle)
            {
                //set current arm move params to prep for movement in fixed update
                currentArmMoveParams = armMoveParams;

                //initialize the buffer to cache positions to check for later
                currentArmMoveParams.cachedPositions = new float[currentArmMoveParams.positionCacheSize];
                
                //snapshot the initial joint position to compare with later during movement
                currentArmMoveParams.initialJointPosition = myAB.jointPosition[0];

                //set if we are moving up or down based on sign of distance from input
                if(armMoveParams.direction < 0)
                {
                    Debug.Log("setting lift state to move down");
                    liftState = ArmLiftState.MovingDown;
                }

                else if(armMoveParams.direction > 0)
                {
                    Debug.Log("setting lift state to move up");
                    liftState = ArmLiftState.MovingUp;
                }
            }
        }

        //we are an extending joint, moving along the local z axis
        else if(jointAxisType == JointAxisType.Extend)
        {
            if(extendState == ArmExtendState.Idle)
            {
                //set current arm move params to prep for movement in fixed update
                currentArmMoveParams = armMoveParams;

                //initialize the buffer to cache positions to check for later
                currentArmMoveParams.cachedPositions = new float[currentArmMoveParams.positionCacheSize];
                
                //snapshot the initial joint position to compare with later during movement
                currentArmMoveParams.initialJointPosition = myAB.jointPosition[0];

                //set if we are extending or retracting
                if(armMoveParams.direction < 0)
                {
                    extendState = ArmExtendState.MovingBackward;
                }

                if(armMoveParams.direction > 0)
                {
                    extendState = ArmExtendState.MovingForward;
                }
            }
        }

        else if(jointAxisType == JointAxisType.Rotate)
        {
            if(rotateState == ArmRotateState.Idle)
            {
                //set current arm move params to prep for movement in fixed update
                currentArmMoveParams = armMoveParams;

                //initialize the buffer to cache rotations to check for later
                currentArmMoveParams.cachedPositions = new float[currentArmMoveParams.positionCacheSize];
                
                //snapshot the initial joint "rotation" to compare with later during movement
                currentArmMoveParams.initialJointPosition = myAB.jointPosition[0];

                //set if we are rotating left or right
                if(armMoveParams.direction < 0)
                {
                    rotateState = ArmRotateState.Negative;
                }

                if(armMoveParams.direction > 0)
                {
                    rotateState = ArmRotateState.Positive;
                }
            }
        }
    }

    public void ControlJointFromAction()
    {
        //we are a lift type joint
        if(jointAxisType == JointAxisType.Lift)
        {
            //if instead we are moving up or down actively
            if(liftState != ArmLiftState.Idle)
            {
                var drive = myAB.yDrive;
                float currentPosition = myAB.jointPosition[0];
                float targetPosition = currentPosition + (float)liftState * Time.fixedDeltaTime * currentArmMoveParams.speed;    
                drive.target = targetPosition;
                myAB.yDrive = drive;

                //begin checks to see if we have stopped moving or if we need to stop moving
                //cache the position at the moment
                currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = currentPosition;

                var distanceMovedSoFar = Mathf.Abs(currentPosition - currentArmMoveParams.initialJointPosition);

                //iterate next index in cache, loop back to index 0 as we get newer positions
                currentArmMoveParams.oldestCachedIndex = (currentArmMoveParams.oldestCachedIndex + 1) % currentArmMoveParams.positionCacheSize;

                //every time we loop back around the cached positions, check if we effectively stopped moving
                if(currentArmMoveParams.oldestCachedIndex == 0)
                {
                    //go ahead and update index 0 super quick so we don't miss it
                    currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = currentPosition;
                    
                    //for the last {NumberOfCachedPositions} positions, and if that amount hasn't changed
                    //by the {tolerance} deviation then we have presumably stopped moving
                    if(CheckArrayWithinStandardDeviation(currentArmMoveParams.cachedPositions, currentArmMoveParams.tolerance))
                    {
                        //Debug.Log($"last {currentArmMoveParams.positionCacheSize} positions were within tolerance, stop moving now!");
                        liftState = ArmLiftState.Idle;
                        //actionFinished(true);
                        return;
                    }
                }

                if(distanceMovedSoFar >= currentArmMoveParams.distance)
                {
                    Debug.Log($"distance we were trying to move was: {currentArmMoveParams.distance}");
                    Debug.Log($"max distance exceeded, distance {myAB} moved this distance: {distanceMovedSoFar}");
                    liftState = ArmLiftState.Idle;
                    //PretendToBeInTHOR.actionFinished(true);
                    return;
                }
                
                //otherwise we have a hard timer to stop movement so we don't move forever and crash unity
                currentArmMoveParams.timePassed += Time.deltaTime;

                if(currentArmMoveParams.timePassed >= currentArmMoveParams.maxTimePassed)
                {
                    Debug.Log($"{currentArmMoveParams.timePassed} seconds have passed. Time out happening, stop moving!");
                    liftState = ArmLiftState.Idle;
                    //PretendToBeInTHOR.actionFinished(true);
                    return;
                }
            }

            //we are set to be in an idle state so return and do nothing
            return;
        }

        //for extending arm joints, don't set actionFinished here, instead we have a coroutine in the
        //TestABArmController component that will check each arm joint to see if all arm joints have either
        //finished moving their required distance, or if they have stopped moving, or if they have timed out
        else if(jointAxisType == JointAxisType.Extend)
        {
            if(extendState != ArmExtendState.Idle)
            {
                var drive = myAB.zDrive;
                float currentPosition = myAB.jointPosition[0];
                float targetPosition = currentPosition + (float)extendState * Time.fixedDeltaTime * currentArmMoveParams.speed;    
                drive.target = targetPosition;
                myAB.zDrive = drive;
            
                //begin checks to see if we have stopped moving or if we need to stop moving
                //cache the position at the moment
                currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = currentPosition;

                //Debug.Log($"initialPosition: {currentArmMoveParams.initialJointPosition}");

                var distanceMovedSoFar = Mathf.Abs(currentPosition - currentArmMoveParams.initialJointPosition);
                //Debug.Log($"distance moved so far is: {distanceMovedSoFar}");

                //iterate next index in cache, loop back to index 0 as we get newer positions
                currentArmMoveParams.oldestCachedIndex = (currentArmMoveParams.oldestCachedIndex + 1) % currentArmMoveParams.positionCacheSize;

                //every time we loop back around the cached positions, check if we effectively stopped moving
                if(currentArmMoveParams.oldestCachedIndex == 0)
                {
                    //go ahead and update index 0 super quick so we don't miss it
                    currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = currentPosition;
                    
                    //for the last {NumberOfCachedPositions} positions, and if that amount hasn't changed
                    //by the {tolerance} deviation then we have presumably stopped moving
                    if(CheckArrayWithinStandardDeviation(currentArmMoveParams.cachedPositions, currentArmMoveParams.tolerance))
                    {
                        Debug.Log($"tolerance reached, distance {myAB} moved this distance: {distanceMovedSoFar}");
                        extendState = ArmExtendState.Idle;
                        return;
                    }
                }

                if(distanceMovedSoFar >= currentArmMoveParams.distance)
                {
                    Debug.Log($"distance we were trying to move was: {currentArmMoveParams.distance}");
                    Debug.Log($"max distance exceeded, distance {myAB} moved this distance: {distanceMovedSoFar}");
                    extendState = ArmExtendState.Idle;
                    return;
                }
                
                //otherwise we have a hard timer to stop movement so we don't move forever and crash unity
                currentArmMoveParams.timePassed += Time.deltaTime;

                if(currentArmMoveParams.timePassed >= currentArmMoveParams.maxTimePassed)
                {
                    Debug.Log($"max time passed, distance {myAB} moved this distance: {distanceMovedSoFar}");
                    extendState = ArmExtendState.Idle;
                    return;
                }
            }
        }

        //if we are a revolute joint
        else if(jointAxisType == JointAxisType.Rotate)
        {
            if(rotateState != ArmRotateState.Idle)
            {
                //seems like revolute joints always only have an xDrive, and to change where its rotating
                //you just rotate the anchor itself, but always access the xDrive
                var drive = myAB.xDrive;
                //somehow this is already in either meters or radians, so we are in radians here
                float currentRotationRads = myAB.jointPosition[0];
                //convert to degrees
                float currentRotation = Mathf.Rad2Deg * currentRotationRads;
                //i think this speed is in rads per second?????
                float targetRotation = currentRotation + (float)rotateState * currentArmMoveParams.speed * Time.fixedDeltaTime;
                drive.target = targetRotation;
                myAB.xDrive = drive;

                //begin checks to see if we have stopped moving or if we need to stop moving
                //cache the rotation at the moment
                currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = currentRotation;

                var distanceMovedSoFar = Mathf.Abs(currentRotation - Mathf.Rad2Deg * currentArmMoveParams.initialJointPosition);

                //iterate next index in cache, loop back to index 0 as we get newer positions
                currentArmMoveParams.oldestCachedIndex = (currentArmMoveParams.oldestCachedIndex + 1) % currentArmMoveParams.positionCacheSize;

                //every time we loop back around the cached positions, check if we effectively stopped moving
                if(currentArmMoveParams.oldestCachedIndex == 0)
                {
                    //go ahead and update index 0 super quick so we don't miss it
                    currentArmMoveParams.cachedPositions[currentArmMoveParams.oldestCachedIndex] = currentRotation;
                    
                    //for the last {NumberOfCachedPositions} positions, and if that amount hasn't changed
                    //by the {tolerance} deviation then we have presumably stopped moving
                    if(CheckArrayWithinStandardDeviation(currentArmMoveParams.cachedPositions, currentArmMoveParams.tolerance))
                    {
                        Debug.Log($"last {currentArmMoveParams.positionCacheSize} rotations were within tolerance, stop rotating now!");
                        rotateState = ArmRotateState.Idle;
                        //PretendToBeInTHOR.actionFinished(true);
                        return;
                    }
                }

                if(distanceMovedSoFar >= currentArmMoveParams.distance)
                {
                    Debug.Log($"degrees we were trying to rotate was: {currentArmMoveParams.distance}");
                    Debug.Log($"max degrees exceeded, {myAB} rotated {distanceMovedSoFar} degrees");
                    rotateState = ArmRotateState.Idle;
                    //PretendToBeInTHOR.actionFinished(true);
                    return;
                }
                
                //otherwise we have a hard timer to stop rotation so we don't rotate forever and crash unity
                currentArmMoveParams.timePassed += Time.deltaTime;

                if(currentArmMoveParams.timePassed >= currentArmMoveParams.maxTimePassed)
                {
                    Debug.Log($"{currentArmMoveParams.timePassed} seconds have passed. Time out happening, stop moving!");
                    rotateState = ArmRotateState.Idle;
                    //PretendToBeInTHOR.actionFinished(true);
                    return;
                }            
            }
        }
    }

    //check if all values in the array are within a standard deviation or not
    bool CheckArrayWithinStandardDeviation(float[] values, float standardDeviation)
    {
        // Calculate the mean value of the array
        float mean = values.Average();

        // Calculate the sum of squares of the differences between each value and the mean
        float sumOfSquares = 0.0f;
        foreach (float value in values)
        {
            //Debug.Log(value);
            sumOfSquares += (value - mean) * (value - mean);
        }

        // Calculate the standard deviation of the array
        float arrayStdDev = (float)Mathf.Sqrt(sumOfSquares / values.Length);

        // Check if the standard deviation of the array is within the specified range
        return arrayStdDev <= standardDeviation;
    }
}
