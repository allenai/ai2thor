using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IK_Robot_Arm_Controller : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
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
                //stop moving flag here
                //something like check the stop moving flag while in moveArm xyz coroutine
                //if coroutine needs to stop, then reset the stop moving flag?
                //the timing between this physics onCollisionEnter update and the coroutine update might be a problem... we'll see?
            }
        }
    }

}
