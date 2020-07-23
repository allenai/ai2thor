using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class IK_Robot_Arm_Controller : MonoBehaviour
{
    private Transform armTarget;
    private SimObjPhysics staticCollided;
    // Start is called before the first frame update
    void Start()
    {
        // What a mess clean up this hierarchy, standarize naming
        armTarget = this.transform.Find("FK_IK_rig").Find("robot_arm_IK_rig").Find("pos_rot_manipulator");
        Debug.Log("Start + " + (armTarget != null));
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

        if(collision.gameObject.GetComponent<SimObjPhysics>())
        {
            SimObjPhysics sop = collision.gameObject.GetComponent<SimObjPhysics>();
            if(sop.PrimaryProperty == SimObjPrimaryProperty.Static)
            {
                staticCollided = sop;
                //stop moving flag here
                //something like check the stop moving flag while in moveArm xyz coroutine
                //if coroutine needs to stop, then reset the stop moving flag?
                //the timing between this physics onCollisionEnter update and the coroutine update might be a problem... we'll see?
            }
        }
    }


    public IEnumerator moveArmTarget(PhysicsRemoteFPSAgentController controller, Vector3 target, float unitsPerSecond) {

        Vector3 targetWorldPos = controller.transform.TransformPoint(target);
        
        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;
        
        var eps = 1e-3;
        yield return new WaitForFixedUpdate();
        var previousArmPosition = armTarget.position;
        while (Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps) {

            if (staticCollided != null) {
                
                // TODO decide if we want to return to original position or last known position before collision
                armTarget.position = previousArmPosition;
                controller.actionFinished(false, "Arm collided with static object: '" + staticCollided.name + "' arm could not reach target position: '" + target + "'.");
                staticCollided = null;
                Debug.Log("Action Failed collided with static");
                yield break;
            }

            armTarget.position += targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime;
            // Jump the last epsilon to match exactly targetWorldPos
            armTarget.position = Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps ?  armTarget.position : targetWorldPos;
            previousArmPosition = armTarget.position;
            yield return new WaitForFixedUpdate();

        }
        controller.actionFinished(true);
    }

}
