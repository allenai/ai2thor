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
    [SerializeField]
    private WhatIsInsideMagnetSphere magnetSphereComp = null;
    [SerializeField]
    private GameObject MagnetRenderer = null;

    private PhysicsRemoteFPSAgentController PhysicsController; 
    private HashSet<Collider> activeColliders = new HashSet<Collider>();

    //references to the joints of the mid level arm
    [SerializeField]
    private Transform FirstJoint = null;
    [SerializeField]
    private Transform SecondJoint = null;
    [SerializeField]
    private Transform ThirdJoint = null;
    [SerializeField]
    private Transform FourthJoint = null;

    //dict to track which picked up object has which set of trigger colliders
    //which we have to parent and reparent in order for arm collision to detect
    [SerializeField]
    private Dictionary<SimObjPhysics, Transform> HeldObjects = new Dictionary<SimObjPhysics, Transform>();

    private bool StopMotionOnContact = false;
    // Start is called before the first frame update

    [SerializeField]
    private CapsuleCollider[] ArmCapsuleColliders;
    [SerializeField]
    private BoxCollider[] ArmBoxColliders;

    void Start()
    {
        // What a mess clean up this hierarchy, standarize naming
        armTarget = this.transform.Find("FK_IK_rig").Find("robot_arm_IK_rig").Find("pos_rot_manipulator");
        // handCameraTransform = this.GetComponentInChildren<Camera>().transform;
        //FirstJoint = this.transform.Find("robot_arm_1_jnt"); this is now set via serialize field, along with the other joints
        handCameraTransform = this.transform.FirstChildOrDefault(x => x.name == "robot_arm_4_jnt");
        staticCollided = new StaticCollided();

        //MagnetRenderer = handCameraTransform.FirstChildOrDefault(x => x.name == "Magnet").gameObject;
        //magnetSphereComp = magnetSphere.GetComponent<WhatIsInsideMagnetSphere>();
        // PhysicsController = GetComponentInParent<PhysicsRemoteFPSAgentController>();
    }

    //NOTE: removing this for now, will add back if functionality is required later
    // public void SetStopMotionOnContact(bool b)
    // {
    //     StopMotionOnContact = b;
    // }

    // Update is called once per frame
    void Update()
    {
        // if(Input.GetKeyDown(KeyCode.Space))
        // {
        //     IsArmColliding();
        // }
    }

    //debug for gizmo draw
    #if UNITY_EDITOR
    Vector3 gizmo_p1;
    Vector3 gizmo_p2;
    float gizmo_radius;
    #endif


    private void moveTargetSimulatePhisics(
        PhysicsRemoteFPSAgentController controller,
        float fixedDeltaTime,
        Transform moveTransform,
        Vector3 targetPosition,
        float unitsPerSecond,
        bool returnToStartPositionIfFailed = false,
        bool localPosition = true
    )
        {
            const double eps = 1e-3;
            staticCollided.collided = false;
            staticCollided.simObjPhysics = null;
            staticCollided.gameObject = null;
            Physics.autoSimulation = false;

            var actionSuccess = true;
            var debugMessage = "";

            System.Func<Transform, Vector3> getPosition = (t) => (localPosition ? t.localPosition : t.position);
             System.Action<Transform, Vector3> setPosition = (t, pos) => {
                if (localPosition) {
                    t.localPosition = pos;
                }
                else {
                    t.position = pos;
                }
            };
            
            System.Action<Transform, Vector3> addPosition = (t, pos) => {
                if (localPosition) {
                    t.localPosition += pos;
                }
                else {
                    t.position += pos;
                }
            };

            var originalPosition = getPosition(moveTransform);

            var previousArmPosition = originalPosition;

            Vector3 targetDirection = (targetPosition - previousArmPosition).normalized;
            float currentDistance = Vector3.SqrMagnitude(targetPosition - getPosition(moveTransform));
            float startingDistance = currentDistance;
            // running simulate once before we begin our movement loop to
            // ensure that the arm is not in contact with any other object
            // that should prevent it from moving
            Physics.Simulate(fixedDeltaTime);

            while ( currentDistance > eps && !shouldHalt() && currentDistance <= startingDistance) {

                previousArmPosition = getPosition(moveTransform);

                addPosition(moveTransform, targetDirection * unitsPerSecond * fixedDeltaTime);

                Physics.Simulate(fixedDeltaTime);

                currentDistance = Vector3.SqrMagnitude(targetPosition - getPosition(moveTransform));
            }

            /*
            DISABLING JUMP TO FINAL POSITION as it can lead to clipping
            if (currentDistance <= eps && !staticCollided.collided) {
                setPosition(moveTransform, targetPosition);
                // must run Simulate() one more time to ensure colliders are triggered
                Physics.Simulate(fixedDeltaTime);
            }
            */

            var staticCollisions = StaticCollisions();


            if (staticCollisions.Count > 0) {
                var sc = staticCollisions[0];
                
                //decide if we want to return to original position or last known position before collision
                setPosition(moveTransform, returnToStartPositionIfFailed ? originalPosition :previousArmPosition - (targetDirection * unitsPerSecond * fixedDeltaTime));

                //if we hit a sim object
                if(sc.simObjPhysics && !sc.gameObject)
                {
                    debugMessage = "Arm collided with static sim object: '" + sc.simObjPhysics.name + "' arm could not reach target position: '" + targetPosition + "'.";
                }

                //if we hit a structural object that isn't a sim object but still has static collision
                if(!sc.simObjPhysics && sc.gameObject)
                {
                    debugMessage = "Arm collided with static structure in scene: '" + sc.gameObject.name + "' arm could not reach target position: '" + targetPosition + "'.";
                }
                actionSuccess = false;
        } else if (currentDistance > startingDistance) {
            Debug.Log("stopping arm height - target was overshot");
            debugMessage =  "arm height has overshot the target position";
            actionSuccess = false;
        }
        Physics.autoSimulation = true;
        controller.actionFinished(actionSuccess, debugMessage);
    }

    public bool IsArmColliding()
    {
        bool result = false;

        //create overlap box/capsule for each collider and check the result I guess
        foreach (CapsuleCollider c in ArmCapsuleColliders)
        {
            Vector3 center = c.transform.TransformPoint(c.center);
            float radius = c.radius;
            //direction of CapsuleCollider's orientation in local space
            Vector3 dir = new Vector3();
            //x just in case
            if(c.direction == 0)
            {
                //get world space direction of this capsule's local right vector
                dir = c.transform.right;
            }

            //y just in case
            if(c.direction == 1)
            {
                //get world space direction of this capsule's local up vector
                dir = c.transform.up;
            }

            //z because all arm colliders have direction z by default
            if(c.direction == 2)
            {
                //get world space direction of this capsul's local forward vector
                dir = c.transform.forward;
                //this doesn't work because transform.right is in world space already,
                //how to get transform.localRight?
            }

            //debug draw
            Debug.DrawLine(center, center + dir * 2.0f, Color.red, 10.0f);

            //point 0
            //center in world space + direction with magnitude (1/2 height - radius)
            var point0 = center + dir * (c.height/2 - radius);

            //point 1
            //center in world space - direction with magnitude (1/2 height - radius)
            var point1 = center - dir * (c.height/2 - radius);

            #if UNITY_EDITOR
            // print("p0 " + point0);
            // print("p1 " + point1);
            gizmo_p1 = point0;
            gizmo_p2 = point1;
            gizmo_radius = radius;
            #endif
            
            //ok now finally let's make some overlap capsuuuules
            if(Physics.OverlapCapsule(point0, point1, radius, 1 << 8, QueryTriggerInteraction.Ignore).Length > 0)
            {
                result = true;
            }

        }

        foreach (BoxCollider b in ArmBoxColliders)
        {

        }

        return result;
    }
    public void OnTriggerExit(Collider col)
    {
        activeColliders.Remove(col);
    }

    public void OnTriggerStay(Collider col)
    {
        activeColliders.Add(col);
    }

    public List<StaticCollided> StaticCollisions() {
        var staticCols = new List<StaticCollided>();
        foreach(var col in activeColliders) {
            if(col.GetComponentInParent<SimObjPhysics>())
            {
                //how does this handle nested sim objects? maybe it's fine?
                SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();
                if(sop.PrimaryProperty == SimObjPrimaryProperty.Static)
                {

                    if(!col.isTrigger)
                    {
                        // #if UNITY_EDITOR
                        // Debug.Log("Collided with static sim obj " + sop.name);
                        // #endif
                        var sc = new StaticCollided();
                        sc.collided = true;
                        sc.simObjPhysics = sop;
                        staticCols.Add(sc);
                    }
                }
            }

            //also check if the collider hit was a structure?
            else if(col.gameObject.tag == "Structure")
            {                
                if(!col.isTrigger)
                {
                    var sc = new StaticCollided();
                    sc.collided = true;
                    sc.gameObject = col.gameObject;
                    staticCols.Add(sc);
                }
            }
        }
        return staticCols;
    }

    private bool shouldHalt() {
        return StaticCollisions().Count > 0;
    }

    public void OnTriggerEnter(Collider col)
    {
        activeColliders.Add(col);
        if(col.GetComponentInParent<SimObjPhysics>())
        {
            //how does this handle nested sim objects? maybe it's fine?
            SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();
            if(sop.PrimaryProperty == SimObjPrimaryProperty.Static)
            {

                if(!col.isTrigger)
                {
                    // #if UNITY_EDITOR
                    // Debug.Log("Collided with static sim obj " + sop.name);
                    // #endif
                    staticCollided.collided = true;
                    staticCollided.simObjPhysics = sop;
                }
            }
        }

        //also check if the collider hit was a structure?
        else if(col.gameObject.tag == "Structure")
        {                
            if(!col.isTrigger)
            {
                staticCollided.collided = true;
                staticCollided.gameObject = col.gameObject;
            }
        }
    }

    //axis and angle
    public IEnumerator rotateHand(PhysicsRemoteFPSAgentController controller, Quaternion targetQuat, float time, bool returnToStartPositionIfFailed = false)
    {

        staticCollided.collided = false;
        staticCollided.simObjPhysics = null;
        staticCollided.gameObject = null;

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
        staticCollided.simObjPhysics = null;
        staticCollided.gameObject = null;

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

        while ((Vector3.SqrMagnitude(targetLocalPos - arm.transform.localPosition) > eps) && !staticCollided.collided) {
            previousArmPosition = arm.transform.localPosition;
            arm.transform.localPosition += targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime;
            // Jump the last epsilon to match exactly targetWorldPos
            
            arm.transform.localPosition = Vector3.SqrMagnitude(targetLocalPos - arm.transform.localPosition) > eps ?  arm.transform.localPosition : targetLocalPos;
           
            yield return new WaitForFixedUpdate();
        }

        if (staticCollided.collided) {
        
            //decide if we want to return to original position or last known position before collision
            arm.transform.localPosition = returnToStartPositionIfFailed ? originalPos : previousArmPosition - (targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime);

            string debugMessage = "";

            //if we hit a sim object
            if(staticCollided.simObjPhysics && !staticCollided.gameObject)
            {
                debugMessage = "Arm collided with static sim object: '" + staticCollided.simObjPhysics.name + "' arm could not reach target position: '" + target + "'.";
            }

            //if we hit a structural object that isn't a sim object but still has static collision
            if(!staticCollided.simObjPhysics && staticCollided.gameObject)
            {
                debugMessage = "Arm collided with static structure in scene: '" + staticCollided.gameObject.name + "' arm could not reach target position: '" + target + "'.";
            }

            staticCollided.collided = false;

            controller.actionFinished(false, debugMessage);
            yield break;
        }

        //if the option to stop moving when the sphere touches any sim object is wanted

        // Removed StopMotionOnContact until someone needs it, also should be renamed to StopMotionOn(Magnet|Hand)Contact
        // if(magnetSphereComp.isColliding && StopMotionOnContact)
        // {
        //     string debugMessage = "Some object was hit by the arm's hand";
        //     controller.actionFinished(false, debugMessage);
        //     yield break;
        // }

        

        controller.actionFinished(true);
    }

    public IEnumerator moveArmTarget(PhysicsRemoteFPSAgentController controller, Vector3 target, float unitsPerSecond,  GameObject arm, bool returnToStartPositionIfFailed = false, string whichSpace = "arm") {

        staticCollided.collided = false;
        staticCollided.simObjPhysics = null;
        staticCollided.gameObject = null;
        // Move arm based on hand space or arm origin space
        //Vector3 targetWorldPos = handCameraSpace ? handCameraTransform.TransformPoint(target) : arm.transform.TransformPoint(target);

        Vector3 targetWorldPos = Vector3.zero;

        //world space, can be used to move directly toward positions returned by sim objects
        if(whichSpace == "world")
        {
            targetWorldPos = target;
        }

        //space relative to base of the wrist, where the camera is
        else if(whichSpace == "wrist")
        {
            targetWorldPos = handCameraTransform.TransformPoint(target);
        }

        //space relative to the root of the arm, joint 1
        else if(whichSpace == "armBase")
        {
            targetWorldPos = arm.transform.TransformPoint(target);
        }
        
        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;
        
        var eps = 1e-3;
        yield return new WaitForFixedUpdate();
        var previousArmPosition = armTarget.position;

        while ((Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps) && !staticCollided.collided) {


            previousArmPosition = armTarget.position;
            armTarget.position += targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime;
            // Jump the last epsilon to match exactly targetWorldPos
            
            armTarget.position = Vector3.SqrMagnitude(targetWorldPos - armTarget.position) > eps ?  armTarget.position : targetWorldPos;
           
            yield return new WaitForFixedUpdate();

        }

        if (staticCollided.collided) {
            
            //decide if we want to return to original position or last known position before collision
            armTarget.position = returnToStartPositionIfFailed ? originalPos : previousArmPosition - (targetDirectionWorld * unitsPerSecond * Time.fixedDeltaTime);

            string debugMessage = "";

            //if we hit a sim object
            if(staticCollided.simObjPhysics && !staticCollided.gameObject) {
                debugMessage = "Arm collided with static sim object: '" + staticCollided.simObjPhysics.name + "' arm could not reach target position: '" + target + "'.";
            }

            //if we hit a structural object that isn't a sim object but still has static collision
            if(!staticCollided.simObjPhysics && staticCollided.gameObject)
            {
                debugMessage = "Arm collided with static structure in scene: '" + staticCollided.gameObject.name + "' arm could not reach target position: '" + target + "'.";
            }
                            
            staticCollided.collided = false;

            controller.actionFinished(false, debugMessage);
            yield break;
        }

        //if the option to stop moving when the sphere touches any sim object is wanted

        // Removed StopMotionOnContact until someone needs it, also should be renamed to StopMotionOn(Magnet|Hand)Contact
        // if(magnetSphereComp.isColliding && StopMotionOnContact)
        // {
        //     string debugMessage = "Some object was hit by the arm's hand";
        //     controller.actionFinished(false, debugMessage);
        //     yield break;
        // }

        controller.actionFinished(true);
    }


    public void moveArmHeightNoRender(PhysicsRemoteFPSAgentController controller, float height, float unitsPerSecond, GameObject arm, float fixedDeltaTime = 0.02f, bool returnToStartPositionIfFailed = false) {
        //first check if the target position is within bounds of the agent's capsule center/height extents
        //if not, actionFinished false with error message listing valid range defined by extents

        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 cc_center = cc.center;
        Vector3 cc_maxY = cc.center + new Vector3(0, cc.height/2f, 0);
        Vector3 cc_minY = cc.center + new Vector3(0, (-cc.height/2f)/2f, 0); //this is halved to prevent arm clipping into floor

        //linear function that take height and adjusts targetY relative to min/max extents
        //I think this does that... I think... probably...
        float targetY = ((cc_maxY.y - cc_minY.y)*(height)) + cc_minY.y;


        Vector3 target = new Vector3(0, targetY, 0);

        moveTargetSimulatePhisics(controller, fixedDeltaTime, arm.transform, target, unitsPerSecond, returnToStartPositionIfFailed,true);
    } 


    public void moveArmTargetNoRender(PhysicsRemoteFPSAgentController controller, Vector3 target, float unitsPerSecond,  GameObject arm, float fixedDeltaTime = 0.02f, bool returnToStartPositionIfFailed = false, string whichSpace = "arm") {
        // Move arm based on hand space or arm origin space
        //Vector3 targetWorldPos = handCameraSpace ? handCameraTransform.TransformPoint(target) : arm.transform.TransformPoint(target);

        Vector3 targetWorldPos = Vector3.zero;

        //world space, can be used to move directly toward positions returned by sim objects
        if(whichSpace == "world")
        {
            targetWorldPos = target;
        }

        //space relative to base of the wrist, where the camera is
        else if(whichSpace == "wrist")
        {
            targetWorldPos = handCameraTransform.TransformPoint(target);
        }

        //space relative to the root of the arm, joint 1
        else if(whichSpace == "armBase")
        {
            targetWorldPos = arm.transform.TransformPoint(target);
        }
        
        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;

        moveTargetSimulatePhisics(controller, fixedDeltaTime, armTarget, targetWorldPos, unitsPerSecond, returnToStartPositionIfFailed, false);
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
            Transform cols = sop.transform.Find("TriggerColliders"); 
            cols.SetParent(FourthJoint);
            pickedUp = true;

            HeldObjects.Add(sop, cols);
        }

        //note: how to handle cases where object breaks if it is shoved into another object?
        //make them all unbreakable?
        return pickedUp;
    }

    public void DropObject()
    {
        //grab all sim objects that are currently colliding with magnet sphere
        foreach(KeyValuePair<SimObjPhysics, Transform> sop in HeldObjects) 
        {
            Rigidbody rb = sop.Key.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = false;

            //move colliders back to the sop
            //magnetSphere.transform.Find("TriggerColliders").transform.SetParent(sop.Key.transform);
            sop.Value.transform.SetParent(sop.Key.transform);

            GameObject topObject = GameObject.Find("Objects");

            if(topObject != null)
            {
                sop.Key.transform.parent = topObject.transform;
            }

            else
            {
                sop.Key.transform.parent = null;
            }
            //rb.angularVelocity = UnityEngine.Random.insideUnitSphere;
        }

        //clear all now dropped objects
        HeldObjects.Clear();
    }

    public void SetHandMagnetRadius(float radius) {
        //Magnet.transform.localScale = new Vector3(radius, radius, radius);
        magnetSphere.radius = radius;
        MagnetRenderer.transform.localScale = new Vector3(2*radius, 2*radius, 2*radius);
        magnetSphere.transform.localPosition = new Vector3(0, 0, 0.01f + radius);
        MagnetRenderer.transform.localPosition = new Vector3(0, 0, 0.01f + radius);
    }


    public ArmMetadata GenerateMetadata() {
        var meta = new ArmMetadata();
        //meta.handTarget = armTarget.position;
        var joint = FirstJoint;
        var joints = new List<JointMetadata>();
        for (var i = 2; i <= 5; i++) {
            var jointMeta = new JointMetadata();
            jointMeta.name = joint.name;
            jointMeta.position = joint.position;
            //local position of joint is meaningless because it never changes relative to its parent joint, we use rootRelative instead
            jointMeta.rootRelativePosition = FirstJoint.InverseTransformPoint(joint.position);

            float angleRot;
            Vector3 vectorRot;

            //local rotation currently relative to immediate parent joint
            joint.localRotation.ToAngleAxis(out angleRot, out vectorRot);
            jointMeta.localRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

            //world relative rotation
            joint.rotation.ToAngleAxis(out angleRot, out vectorRot);
            jointMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

            //rotation relative to root joint/agent
            //root forward and agent forward are always the same
            Quaternion.Euler(FirstJoint.InverseTransformDirection(joint.eulerAngles)).ToAngleAxis(out angleRot, out vectorRot);
            jointMeta.rootRelativeRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

            joints.Add(jointMeta);
            joint = joint.Find("robot_arm_" + i + "_jnt");
        }
        meta.joints = joints.ToArray();

        //metadata for any objects currently held by the hand on the arm
        //note this is different from objects intersecting the hand's sphere,
        //there could be a case where an object is inside the sphere but not picked up by the hand
        List<string> HeldObjectIDs = new List<string>();

        if(HeldObjects != null)
        {
            foreach(KeyValuePair<SimObjPhysics, Transform> sop in HeldObjects) 
            {
                HeldObjectIDs.Add(sop.Key.objectID);
            }
        }

        meta.HeldObjects = HeldObjectIDs;

        meta.HandSphereCenter = transform.TransformPoint(magnetSphere.center);

        meta.HandSphereRadius = magnetSphere.radius;

        return meta;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(gizmo_radius > 0.0f)
        {
            Gizmos.DrawWireSphere (gizmo_p1, gizmo_radius);
            Gizmos.DrawWireSphere( gizmo_p2, gizmo_radius);
        }

    }
    #endif
}
