using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
//using UnityEngine.InputSystem;

//this tests controlling the arm parts moving with force
public enum ABControlMode {Keyboard_Input, Actions};

public static class PretendToBeInTHOR
{
    public static void actionFinished(bool result)
    {
        Debug.Log($"Action Finished: {result}!");
    }
}

public class TestABArmController : MonoBehaviour
{
    [SerializeField]
    //public ABControlMode controlMode = ABControlMode.Keyboard_Input;

    [System.Serializable]
    public struct Joint
    {
        public TestABArmJointController joint;
        //leaving room in this struct in case we ever need to annotate more stuff per joint
    }

    public Joint[] joints;

    void Start()
    {
        //assign this controller as a reference in all joints
        foreach (Joint j in joints)
        {
            j.joint.GetComponent<TestABArmJointController>().myABArmControllerComponent = this.GetComponent<TestABArmController>();
        }
    }

    //Server Action format to move the base of the arm up
    public void MoveArmBaseUp (float distance, float speed, float tolerance, float maxTimePassed, int positionCacheSize)
    {
        MoveArmBase(                    
            distance: distance,
            speed: speed,
            tolerance: tolerance,
            maxTimePassed: maxTimePassed,
            positionCacheSize: positionCacheSize,
            direction: 1 //going up
        );
    }

    //server action format to move the base of the arm down
    public void MoveArmBaseDown (float distance, float speed, float tolerance, float maxTimePassed, int positionCacheSize)
    {
        MoveArmBase(                    
            distance: distance,
            speed: speed,
            tolerance: tolerance,
            maxTimePassed: maxTimePassed,
            positionCacheSize: positionCacheSize,
            direction: -1 //going down
        );
    }

    //actually send the arm parameters to the joint moving up/down and begin movement
    public void MoveArmBase (float distance = 0.25f, float speed = 3.0f, float tolerance = 1e-3f, float maxTimePassed = 5.0f, int positionCacheSize = 10, int direction = 1)
    {
        //create a set of movement params for how we are about to move
        ArmMoveParams amp = new ArmMoveParams{
            distance = distance,
            speed = speed,
            tolerance = tolerance,
            maxTimePassed = maxTimePassed,
            positionCacheSize = positionCacheSize,
            direction = direction 
        };

        TestABArmJointController liftJoint = joints[0].joint;
        liftJoint.PrepToControlJointFromAction(amp);
    }

    //use H and N keys to raise and lower lift
    // public void OnMoveArmLift(InputAction.CallbackContext context)
    // {
    //     if(context.started == true)
    //     {
    //         if(LiftStateFromInput(context.ReadValue<float>()) == ArmLiftState.MovingUp)
    //         {
    //             //these parameters here act as if a researcher has put them in as an action
    //             MoveArmBaseUp(
    //                 distance: 0.25f,
    //                 speed: 3.0f, //top speed or target speed
    //                 tolerance: 1e-3f,
    //                 maxTimePassed: 5.0f,
    //                 positionCacheSize: 10
    //             );
    //         }

    //         else if(LiftStateFromInput(context.ReadValue<float>()) == ArmLiftState.MovingDown)
    //         {
    //             //these parameters here act as if a researcher has put them in as an action
    //             MoveArmBaseDown(
    //                 distance: 0.25f,
    //                 speed: 3.0f,
    //                 tolerance: 1e-3f,
    //                 maxTimePassed: 5.0f,
    //                 positionCacheSize: 10
    //             );
    //         }
    //     }
    // }

    //reads input from the Player Input component to move an arm joint up and down along its local Y axis
    ArmLiftState LiftStateFromInput (float input)
    {
        if (input > 0)
        {
            return ArmLiftState.MovingUp;
        }
        else if (input < 0)
        {
            return ArmLiftState.MovingDown;
        }
        else
        {
            return ArmLiftState.Idle;
        }
    }

    public void MoveArmForward(float distance, float speed, float tolerance, float maxTimePassed, int positionCacheSize)
    {
        ExtendArm(                    
            distance: distance,
            speed: speed,
            tolerance: tolerance,
            maxTimePassed: maxTimePassed,
            positionCacheSize: positionCacheSize,
            direction: 1 //extend forward
        );
    }

    public void MoveArmBackward(float distance, float speed, float tolerance, float maxTimePassed, int positionCacheSize)
    {
        ExtendArm(                    
            distance: distance,
            speed: speed,
            tolerance: tolerance,
            maxTimePassed: maxTimePassed,
            positionCacheSize: positionCacheSize,
            direction: -1 //extend backward
        );
    }

    public void ExtendArm(float distance = 0.12f, float speed = 0.2f, float tolerance = 1e-4f, float maxTimePassed = 5.0f, int positionCacheSize = 10, int direction = 1)
    {
        Dictionary<TestABArmJointController, float> jointToArmDistanceRatios = new Dictionary<TestABArmJointController, float>();

        //get the total distance each joint can move based on the upper limits
        float totalExtendDistance = 0.0f;

        //loop through all extending joints to get the total distance each joint can move
        for(int i = 1; i <= 4; i++)
        {
            totalExtendDistance += GetDriveUpperLimit(joints[i].joint);
        }

        //loop through all extending joints and get the ratio of movement each joint is responsible for
        for(int i = 1; i <= 4; i++)
        {
            TestABArmJointController thisJoint = joints[i].joint;
            jointToArmDistanceRatios.Add(thisJoint, GetDriveUpperLimit(thisJoint)/totalExtendDistance);
        }

        List<TestABArmJointController> jointsThatAreMoving = new List<TestABArmJointController>();

        //set each joint to move its specific distance
        foreach (TestABArmJointController joint in jointToArmDistanceRatios.Keys)
        {
            //assign each joint the distance it needs to move to have the entire arm
            float myDistance = distance * jointToArmDistanceRatios[joint];

            ArmMoveParams amp = new ArmMoveParams{
                distance = myDistance,
                speed = speed,
                tolerance = tolerance,
                maxTimePassed = maxTimePassed,
                positionCacheSize = positionCacheSize,
                direction = direction 
            };

            //keep track of joints that are moving
            jointsThatAreMoving.Add(joint);

            //start moving this joint
            joint.PrepToControlJointFromAction(amp);
        }

        //start coroutine to check if all joints have become idle and the action is finished
        StartCoroutine(AreAllTheJointsBackToIdle(jointsThatAreMoving));
    }

    //helper function to return the upper limit for drives
    public float GetDriveUpperLimit(TestABArmJointController joint, JointAxisType jointAxisType = JointAxisType.Extend)
    {
        float upperLimit = 0.0f;

        if(jointAxisType == JointAxisType.Extend)
        {
            //z drive
            upperLimit = joint.myAB.zDrive.upperLimit;
        }

        if(jointAxisType == JointAxisType.Lift)
        {
            //y drive
            upperLimit = joint.myAB.yDrive.upperLimit;
        }

        return upperLimit;
    }

    private IEnumerator AreAllTheJointsBackToIdle(List<TestABArmJointController> jointsThatAreMoving)
    {
        bool hasEveryoneStoppedYet = false;

        //keep checking if things are all idle yet
        //all individual joints should have a max timeout so this won't hang infinitely (i hope)
        while(hasEveryoneStoppedYet == false)
        {
            yield return new WaitForFixedUpdate();

            foreach(TestABArmJointController joint in jointsThatAreMoving)
            {
                if(joint.extendState == ArmExtendState.Idle)
                {
                    hasEveryoneStoppedYet = true;
                }

                else
                {
                    hasEveryoneStoppedYet = false;
                }
            }
        }

        //done!
        PretendToBeInTHOR.actionFinished(true);
        yield return null;
    }

    //use J and M keys to extend and retract
    // public void OnMoveArmJoint1(InputAction.CallbackContext context) 
    // {
    //     if(context.started == true)
    //     {
    //         if(ExtendStateFromInput(context.ReadValue<float>()) == ArmExtendState.MovingForward)
    //         {
    //             //these parameters here act as if a researcher has put them in as an action
    //             MoveArmForward(
    //                 distance: 0.1f,
    //                 speed: 0.2f,
    //                 tolerance: 1e-4f,
    //                 maxTimePassed: 5.0f,
    //                 positionCacheSize: 10
    //             );
    //         }

    //         if(ExtendStateFromInput(context.ReadValue<float>()) == ArmExtendState.MovingBackward)
    //         {
    //             //these parameters here act as if a researcher has put them in as an action
    //             MoveArmBackward(
    //                 distance: 0.1f,
    //                 speed: 0.2f,
    //                 tolerance: 1e-4f,
    //                 maxTimePassed: 5.0f,
    //                 positionCacheSize: 10
    //             );
    //         }
    //     }
    // }

    //reads input from Player Input component to extend and retract arm joint
    ArmExtendState ExtendStateFromInput (float input)
    {
        if(input > 0)
        {
            return ArmExtendState.MovingForward;
        }

        else if (input < 0)
        {
            return ArmExtendState.MovingBackward;
        }
        else
        {
            return ArmExtendState.Idle;
        }
    }

    public void RotateWristRight(float distance, float speed, float tolerance, float maxTimePassed, int positionCacheSize)
    {
        RotateWrist(                    
            distance: distance,
            speed: speed,
            tolerance: tolerance,
            maxTimePassed: maxTimePassed,
            positionCacheSize: positionCacheSize,
            direction: 1 //rotate right
        );
    }

    public void RotateWristLeft(float distance, float speed, float tolerance, float maxTimePassed, int positionCacheSize)
    {
        RotateWrist(                    
            distance: distance,
            speed: speed,
            tolerance: tolerance,
            maxTimePassed: maxTimePassed,
            positionCacheSize: positionCacheSize,
            direction: -1 //rotate left
        );
    }

    public void RotateWrist(float distance = 90f, float speed = 300f, float tolerance = 1e-4f, float maxTimePassed = 10.0f, int positionCacheSize = 10, int direction = 1)
    {
        //create a set of movement params for how we are about to rotate
        ArmMoveParams amp = new ArmMoveParams{
            distance = distance,
            speed = speed,
            tolerance = tolerance,
            maxTimePassed = maxTimePassed,
            positionCacheSize = positionCacheSize,
            direction = direction 
        };

        TestABArmJointController liftJoint = joints[5].joint;
        liftJoint.PrepToControlJointFromAction(amp);
    }

    // public void OnMoveArmWrist(InputAction.CallbackContext context)
    // {
    //     if(context.started == true)
    //     {
    //         //note 1 degree is .017 ish radians, so speed is in radians/second thats why its GIGANTIC
    //         if(RotateStateFromInput(context.ReadValue<float>()) == ArmRotateState.Positive)
    //         {
    //             //these parameters here act as if a researcher has put them in as an action
    //             RotateWristRight(
    //                 distance: 90f,
    //                 speed: 300f,
    //                 tolerance: 1e-4f,
    //                 maxTimePassed: 10.0f,
    //                 positionCacheSize: 10
    //             );
    //         }

    //         if(RotateStateFromInput(context.ReadValue<float>()) == ArmRotateState.Negative)
    //         {
    //             //these parameters here act as if a researcher has put them in as an action
    //             RotateWristLeft(
    //                 distance: 90f,
    //                 speed: 300f,
    //                 tolerance: 1e-4f,
    //                 maxTimePassed: 10.0f,
    //                 positionCacheSize: 10
    //             );
    //         }
    //     }


        // if(joints[5].joint == null) {
        //     throw new ArgumentException("Yo its null, please make not null");
        // }
        // TestABArmJointController joint = joints[5].joint;
        // var input = context.ReadValue<float>();
        //joint.SetArmRotateState(RotateStateFromInput(input));
    //}

    //reads input from Player Input component to rotate arm joint
    ArmRotateState RotateStateFromInput (float input)
    {
        if(input > 0)
        {
            return ArmRotateState.Positive;
        }

        else if (input < 0)
        {
            return ArmRotateState.Negative;
        }
        else
        {
            return ArmRotateState.Idle;
        }
    }




}
