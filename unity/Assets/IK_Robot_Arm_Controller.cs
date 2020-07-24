using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class IK_Robot_Arm_Controller : MonoBehaviour
{

    //track what was hit while arm was moving
    public class StaticCollided
    {
        //keep track of if we hit something
        public bool collided = false;
        //track which sim object was hit
        public SimObjPhysics simObjPhysics;
        //track which structural object was hit
        public GameObject gameObject;
    }

    private Transform armTarget;
    private StaticCollided staticCollided;
    // Start is called before the first frame update
    void Start()
    {
        // What a mess clean up this hierarchy, standarize naming
        armTarget = this.transform.Find("FK_IK_rig").Find("robot_arm_IK_rig").Find("pos_rot_manipulator");
        Debug.Log("Start + " + (armTarget != null));

        staticCollided = new StaticCollided();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCollisionEnter(Collision collision)
    {
        //debug collision print
        //print(collision.collider);
        //print(collision.gameObject);
        staticCollided.collided = false;
        staticCollided.simObjPhysics = null;
        staticCollided.gameObject = null;

        if(collision.gameObject.GetComponent<SimObjPhysics>())
        {
            SimObjPhysics sop = collision.gameObject.GetComponent<SimObjPhysics>();
            if(sop.PrimaryProperty == SimObjPrimaryProperty.Static)
            {
                Debug.Log("Collided with static " + sop.name);
                staticCollided.collided = true;
                staticCollided.simObjPhysics = sop;
                //stop moving flag here
                //something like check the stop moving flag while in moveArm xyz coroutine
                //if coroutine needs to stop, then reset the stop moving flag?
                //the timing between this physics onCollisionEnter update and the coroutine update might be a problem... we'll see?
            }
        }

        //also do this if it hits a structure object that is static
        if(collision.gameObject.isStatic)
        {
            staticCollided.collided = true;
            staticCollided.gameObject = collision.gameObject;
        }
    }


    public IEnumerator moveArmTarget(PhysicsRemoteFPSAgentController controller, Vector3 target, float unitsPerSecond,  GameObject arm, bool returnToStartPositionIfFailed = false) {

        staticCollided.collided = false;
        //do we want this coordinate to be relative to the agent's coordinate space or the arm's coordinate space?
        //using the controller.transform.TransformPoint(), a coordinate of (0, 0, 1) moves the arm toward the center of the agent. 
        //I think since we can move the arm up and down, it may be more consistent to have the coordinate from the arm's perspective?
        //Vector3 targetWorldPos = controller.transform.TransformPoint(target);
        Vector3 targetWorldPos = arm.transform.TransformPoint(target);
        
        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;
        
        var eps = 1e-3;
        yield return new WaitForFixedUpdate();
        var previousArmPosition = armTarget.position;
        while (Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps) {

            if (staticCollided.collided != false) {
                
                // TODO decide if we want to return to original position or last known position before collision
                armTarget.position = returnToStartPositionIfFailed ? originalPos : previousArmPosition - (targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime);
                // armTarget.position = previousArmPosition - (targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime);

                //if we hit a sim object
                if(staticCollided.simObjPhysics && !staticCollided.gameObject)
                controller.actionFinished(false, "Arm collided with static sim object: '" + staticCollided.simObjPhysics.name + "' arm could not reach target position: '" + target + "'.");
                
                //if we hit a structural object that isn't a sim object but still has static collision
                if(!staticCollided.simObjPhysics && staticCollided.gameObject)
                controller.actionFinished(false, "Arm collided with static structure in scene: '" + staticCollided.gameObject.name + "' arm could not reach target position: '" + target + "'.");
                
                staticCollided.collided = false;

                Debug.Log("Action Failed collided with static");
                yield break;
            }

            previousArmPosition = armTarget.position;
            armTarget.position += targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime;
            // Jump the last epsilon to match exactly targetWorldPos
            
            armTarget.position = Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps ?  armTarget.position : targetWorldPos;
           
            yield return new WaitForFixedUpdate();

        }
        controller.actionFinished(true);
    }

}
