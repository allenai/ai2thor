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
    private Transform handCameraTransform;
    [SerializeField]
    private SphereCollider magnetSphere = null;
    private WhatIsInsideMagnetSphere magnetSphereComp = null;
    private GameObject Magnet = null;

    private PhysicsRemoteFPSAgentController PhysicsController; 

    private Transform FirstJoint;

    [SerializeField]
    private List<SimObjPhysics> HeldObjects = null;

    private bool StopMotionOnContact = false;
    // Start is called before the first frame update
    void Start()
    {
        // What a mess clean up this hierarchy, standarize naming
        armTarget = this.transform.Find("FK_IK_rig").Find("robot_arm_IK_rig").Find("pos_rot_manipulator");
        // handCameraTransform = this.GetComponentInChildren<Camera>().transform;
        FirstJoint = this.transform.Find("robot_arm_1_jnt");
        handCameraTransform = this.transform.FirstChildOrDefault(x => x.name == "robot_arm_4_jnt");
        staticCollided = new StaticCollided();

        Magnet = handCameraTransform.FirstChildOrDefault(x => x.name == "Magnet").gameObject;
        magnetSphereComp = magnetSphere.GetComponent<WhatIsInsideMagnetSphere>();
        // PhysicsController = GetComponentInParent<PhysicsRemoteFPSAgentController>();
    }

    public void SetStopMotionOnContact(bool b)
    {
        StopMotionOnContact = b;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCollisionEnter(Collision collision)
    {
        //debug collision print
        // print(collision.collider);
        // print(collision.gameObject);

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
            }
        }

        //also do this if it hits a structure object that is static
        if(collision.gameObject.isStatic)
        {
            staticCollided.collided = true;
            staticCollided.gameObject = collision.gameObject;
        }
    }

    //axis and angle
    public IEnumerator rotateHand(PhysicsRemoteFPSAgentController controller, Quaternion targetQuat, float time, bool returnToStartPositionIfFailed = false)
    {

        staticCollided.collided=false;
        float currentTime = 0.0f;

        yield return new WaitForFixedUpdate();

        var originalRot = armTarget.transform.rotation;

        while (currentTime < time)
        {
            currentTime += Time.fixedDeltaTime;

            //increment
            var interp = currentTime/time;

            if (staticCollided.collided != false) {
            
                //decide if we want to return to original rot or last known rot before collision
                armTarget.transform.rotation = returnToStartPositionIfFailed ? originalRot : Quaternion.Slerp(armTarget.transform.rotation, targetQuat, (currentTime-Time.fixedDeltaTime)/time);

                string debugMessage = "";

                //if we hit a sim object
                if(staticCollided.simObjPhysics && !staticCollided.gameObject)
                debugMessage = "Arm collided with static sim object: '" + staticCollided.simObjPhysics.name + "' arm could not reach target rotation: '" + targetQuat.eulerAngles + "'.";

                //if we hit a structural object that isn't a sim object but still has static collision
                if(!staticCollided.simObjPhysics && staticCollided.gameObject)
                debugMessage = "Arm collided with static structure in scene: '" + staticCollided.gameObject.name + "' arm could not reach target rotation: '" + targetQuat.eulerAngles + "'.";

                staticCollided.collided = false;

                controller.actionFinished(false, debugMessage);
                yield break;
            }

            //this currently shouldn't work for rotating an object because it will always be colliding with the held object....
            // //if the option to stop moving when the sphere touches any sim object is wanted
            // if(magnetSphereComp.isColliding && StopMotionOnContact)
            // {
            //     string debugMessage = "Some object was hit by the arm's hand";
            //     controller.actionFinished(false, debugMessage);
            //     yield break;
            // }

            armTarget.transform.rotation = Quaternion.Slerp(armTarget.transform.rotation, targetQuat, interp);
            yield return new WaitForFixedUpdate();

        }
        controller.actionFinished(true);

    }

    public IEnumerator moveArmHeight(PhysicsRemoteFPSAgentController controller, float height, float unitsPerSecond, GameObject arm, bool returnToStartPositionIfFailed = false)
    {
        //first check if the target position is within bounds of the agent's capsule center/height extents
        //if not, actionFinished false with error message listing valid range defined by extents
        staticCollided.collided = false;

        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 cc_center = cc.center;
        Vector3 cc_maxY = cc.center + new Vector3(0, cc.height/2f, 0);
        Vector3 cc_minY = cc.center + new Vector3(0, (-cc.height/2f)/2f, 0); //this is halved to prevent arm clipping into floor

        //linear function that take height and adjusts targetY relative to min/max extents
        //I think this does that... I think... probably...
        float targetY = ((cc_maxY.y - cc_minY.y)*(height)) + cc_minY.y;


        Vector3 target = new Vector3(0, targetY, 0);
        Vector3 targetLocalPos = target;//arm.transform.TransformPoint(target);

        Vector3 originalPos = arm.transform.localPosition;
        Vector3 targetDirectionWorld = (targetLocalPos - originalPos).normalized;
        var eps = 1e-3;
        yield return new WaitForFixedUpdate();
        var previousArmPosition = arm.transform.localPosition;

        while (Vector3.SqrMagnitude(targetLocalPos - arm.transform.localPosition) > eps) {
            if (staticCollided.collided != false) {
            
                //decide if we want to return to original position or last known position before collision
                arm.transform.localPosition = returnToStartPositionIfFailed ? originalPos : previousArmPosition - (targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime);

                string debugMessage = "";

                //if we hit a sim object
                if(staticCollided.simObjPhysics && !staticCollided.gameObject)
                debugMessage = "Arm collided with static sim object: '" + staticCollided.simObjPhysics.name + "' arm could not reach target position: '" + target + "'.";

                //if we hit a structural object that isn't a sim object but still has static collision
                if(!staticCollided.simObjPhysics && staticCollided.gameObject)
                debugMessage = "Arm collided with static structure in scene: '" + staticCollided.gameObject.name + "' arm could not reach target position: '" + target + "'.";

                staticCollided.collided = false;

                controller.actionFinished(false, debugMessage);
                yield break;
            }

            //if the option to stop moving when the sphere touches any sim object is wanted
            if(magnetSphereComp.isColliding && StopMotionOnContact)
            {
                string debugMessage = "Some object was hit by the arm's hand";
                controller.actionFinished(false, debugMessage);
                yield break;
            }

            previousArmPosition = arm.transform.localPosition;
            arm.transform.localPosition += targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime;
            // Jump the last epsilon to match exactly targetWorldPos
            
            arm.transform.localPosition = Vector3.SqrMagnitude(targetLocalPos - arm.transform.localPosition) > eps ?  arm.transform.localPosition : targetLocalPos;
           
            yield return new WaitForFixedUpdate();

        }
        controller.actionFinished(true);
    }

    public IEnumerator moveArmTarget(PhysicsRemoteFPSAgentController controller, Vector3 target, float unitsPerSecond,  GameObject arm, bool returnToStartPositionIfFailed = false, bool handCameraSpace = false) {

        staticCollided.collided = false;
        // Move arm based on hand space or arm origin space
        Vector3 targetWorldPos = handCameraSpace ? handCameraTransform.TransformPoint(target) : arm.transform.TransformPoint(target);
        
        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;
        
        var eps = 1e-3;
        yield return new WaitForFixedUpdate();
        var previousArmPosition = armTarget.position;

        while (Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps) {

            if (staticCollided.collided != false) {
                
                //decide if we want to return to original position or last known position before collision
                armTarget.position = returnToStartPositionIfFailed ? originalPos : previousArmPosition - (targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime);

                string debugMessage = "";

                //if we hit a sim object
                if(staticCollided.simObjPhysics && !staticCollided.gameObject)
                debugMessage = "Arm collided with static sim object: '" + staticCollided.simObjPhysics.name + "' arm could not reach target position: '" + target + "'.";

                //if we hit a structural object that isn't a sim object but still has static collision
                if(!staticCollided.simObjPhysics && staticCollided.gameObject)
                debugMessage = "Arm collided with static structure in scene: '" + staticCollided.gameObject.name + "' arm could not reach target position: '" + target + "'.";
                                
                staticCollided.collided = false;

                controller.actionFinished(false, debugMessage);
                yield break;
            }

            //if the option to stop moving when the sphere touches any sim object is wanted
            if(magnetSphereComp.isColliding && StopMotionOnContact)
            {
                string debugMessage = "Some object was hit by the arm's hand";
                controller.actionFinished(false, debugMessage);
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

    public List<string> WhatObjectsAreInsideMagnetSphereAsObjectID()
    {
        return magnetSphereComp.CurrentlyContainedSimObjectsByID();
    }

    public List<SimObjPhysics> WhatObjectsAreInsideMagnetSphereAsSOP()
    {
        return magnetSphereComp.CurrentlyContainedSimObjects();
    }

    public IEnumerator ReturnObjectsInMagnetAfterPhysicsUpdate(PhysicsRemoteFPSAgentController controller)
    {
        yield return new WaitForFixedUpdate();
        List<string> listOfSOP = new List<string>();
        foreach (string oid in this.WhatObjectsAreInsideMagnetSphereAsObjectID())
        {
            listOfSOP.Add(oid);
        }
        Debug.Log("objs: " + string.Join(", ", listOfSOP));
        controller.actionFinished(true, listOfSOP);
    }

    public bool PickupObject()
    {
        bool pickedUp = false;
        //grab all sim objects that are currently colliding with magnet sphere
        foreach(SimObjPhysics sop in WhatObjectsAreInsideMagnetSphereAsSOP())
        {
            Rigidbody rb = sop.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            sop.transform.SetParent(magnetSphere.transform);
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            //move colliders to be children of arm? stop arm from moving?
            sop.transform.Find("Colliders").transform.SetParent(magnetSphere.transform);
            pickedUp = true;

            HeldObjects.Add(sop);
        }

        //note: how to handle cases where object breaks if it is shoved into another object?
        //make them all unbreakable?
        return pickedUp;
    }

    public void DropObject()
    {
        //grab all sim objects that are currently colliding with magnet sphere
        foreach(SimObjPhysics sop in HeldObjects) 
        {
            Rigidbody rb = sop.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = false;

            //move colliders back to the sop
            magnetSphere.transform.Find("Colliders").transform.SetParent(sop.transform);
            GameObject topObject = GameObject.Find("Objects");

            if(topObject != null)
            {
                sop.transform.parent = topObject.transform;
            }

            else
            {
                sop.transform.parent = null;
            }

            rb.angularVelocity = UnityEngine.Random.insideUnitSphere;
        }

        //clear all now dropped objects
        HeldObjects.Clear();
    }

    public void SetHandMagnetRadius(float radius) {
        Magnet.transform.localScale = new Vector3(radius, radius, radius);
    }


    public ArmMetadata GenerateMetadata() {
        var meta = new ArmMetadata();
        meta.handTarget = armTarget.position;
        var joint = FirstJoint;
        var joints = new List<JointMetadata>();
        for (var i = 2; i <= 5; i++) {
            var jointMeta = new JointMetadata();
            jointMeta.name = joint.name;
            jointMeta.position = joint.position;
            jointMeta.localPosition = joint.localPosition;

            float angleRot;
            Vector3 vectorRot;
            joint.localRotation.ToAngleAxis(out angleRot, out vectorRot);
            jointMeta.localRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

            joint.rotation.ToAngleAxis(out angleRot, out vectorRot);
            jointMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

            joints.Add(jointMeta);
            joint = joint.Find("robot_arm_" + i + "_jnt");
        }
        meta.joints = joints.ToArray();
        return meta;
    }
}
