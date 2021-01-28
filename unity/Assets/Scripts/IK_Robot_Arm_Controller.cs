using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class IK_Robot_Arm_Controller : MonoBehaviour
{
    private Transform armTarget;
    private Transform handCameraTransform;
    [SerializeField]
    private SphereCollider magnetSphere = null;
    [SerializeField]
    private WhatIsInsideMagnetSphere magnetSphereComp = null;
    [SerializeField]
    private GameObject MagnetRenderer = null;

    private PhysicsRemoteFPSAgentController PhysicsController; 

    //references to the joints of the mid level arm
    [SerializeField]
    private Transform FirstJoint = null;
    [SerializeField]
    private Transform FourthJoint = null;

    //dict to track which picked up object has which set of trigger colliders
    //which we have to parent and reparent in order for arm collision to detect
    [SerializeField]
    public Dictionary<SimObjPhysics, List<Collider>> HeldObjects = new Dictionary<SimObjPhysics, List<Collider>>();

    //private bool StopMotionOnContact = false;
    // Start is called before the first frame update

    [SerializeField]
    public CapsuleCollider[] ArmCapsuleColliders;
    [SerializeField]
    public BoxCollider[] ArmBoxColliders;
    [SerializeField]
    private CapsuleCollider[] agentCapsuleCollider = null;

    private float originToShoulderLength = 0f;

    private const float extendedArmLenth = 0.6325f;

    public CollisionListener collisionListener;

    void Start()
    {
        armTarget = this.transform.Find("robot_arm_FK_IK_rig").Find("IK_rig").Find("IK_pos_rot_manipulator");

        //FirstJoint = this.transform.Find("robot_arm_1_jnt"); this is now set via serialize field, along with the other joints
        handCameraTransform = this.transform.FirstChildOrDefault(x => x.name == "robot_arm_4_jnt");

        //calculating based on distance from origin of arm to the 2nd joint, which will always be constant
        this.originToShoulderLength = Vector3.Distance(this.transform.FirstChildOrDefault(x => x.name == "robot_arm_2_jnt").position, this.transform.position);

        this.collisionListener = this.GetComponentInParent<CollisionListener>();

        List<CapsuleCollider> armCaps = new List<CapsuleCollider>();
        List<BoxCollider> armBoxes = new List<BoxCollider>();

        //get references to all colliders in arm. Remove trigger colliders so there are no duplicates when using these as reference for
        //overlap casts since the trigger colliders are themselves duplicates of the nontrigger colliders.
        armCaps.AddRange(gameObject.GetComponentsInChildren<CapsuleCollider>());
        armBoxes.AddRange(gameObject.GetComponentsInChildren<BoxCollider>());

        //clean up arm colliders, removing triggers
        List<CapsuleCollider> cleanedCaps = new List<CapsuleCollider>();
        foreach(CapsuleCollider c in armCaps)
        {
            if(!c.isTrigger)
            cleanedCaps.Add(c);
        }

        ArmCapsuleColliders = cleanedCaps.ToArray();

        List<BoxCollider> cleanedBoxes = new List<BoxCollider>();
        foreach(BoxCollider b in armBoxes)
        {
            if(!b.isTrigger)
            cleanedBoxes.Add(b);
        }

        ArmBoxColliders = cleanedBoxes.ToArray();


    }

    //NOTE: removing this for now, will add back if functionality is required later
    // public void SetStopMotionOnContact(bool b)
    // {
    //     StopMotionOnContact = b;
    // }

    //debug for gizmo draw
    #if UNITY_EDITOR
    public class GizmoDrawCapsule
    {
        public Vector3 p0;
        public Vector3 p1;
        public float radius;
    }

    List <GizmoDrawCapsule> debugCapsules = new List<GizmoDrawCapsule>();
    #endif

    // Update is called once per frame
    void Update()
    {
        // if(Input.GetKeyDown(KeyCode.Space))
        // {
        //     #if UNITY_EDITOR
        //     debugCapsules.Clear();
        //     #endif

        //     bool result;
        //     result = IsArmColliding();
        //     print("Is the arm actively colliding RIGHT NOW?: " + result);
        // }
    }

    public HashSet<Collider> currentArmCollisions() {
        HashSet<Collider> colliders = new HashSet<Collider>();

        //add the AgentCapsule to the ArmCapsuleColliders for the capsule collider check
        List<CapsuleCollider> capsules = new List<CapsuleCollider>();
        capsules.AddRange(ArmCapsuleColliders);
        capsules.AddRange(agentCapsuleCollider);


        //create overlap box/capsule for each collider and check the result I guess
        foreach (CapsuleCollider c in capsules)
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

            //debug draw forward of each joint
            // #if UNITY_EDITOR
            // //debug draw
            // Debug.DrawLine(center, center + dir * 2.0f, Color.red, 10.0f);
            // #endif

            //center in world space + direction with magnitude (1/2 height - radius)
            var point0 = center + dir * (c.height/2 - radius);

            //point 1
            //center in world space - direction with magnitude (1/2 height - radius)
            var point1 = center - dir * (c.height/2 - radius);

            //debug draw ends of each capsule of each joint
            // #if UNITY_EDITOR
            // GizmoDrawCapsule gdc = new GizmoDrawCapsule();
            // gdc.p0 = point0;
            // gdc.p1 = point1;
            // gdc.radius = radius;
            // debugCapsules.Add(gdc);
            // #endif
            
            //ok now finally let's make some overlap capsuuuules
            foreach(var col in Physics.OverlapCapsule(point0, point1, radius, 1 << 8, QueryTriggerInteraction.Ignore))
            {
                colliders.Add(col);
            }

        }

        //also check if the couple of box colliders are colliding
        foreach (BoxCollider b in ArmBoxColliders)
        {
            foreach(var col in Physics.OverlapBox(b.transform.TransformPoint(b.center), b.size/2.0f, b.transform.rotation, 1 << 8, QueryTriggerInteraction.Ignore))
            {
                colliders.Add(col);
            }
        }
        return colliders;
    }

    public bool IsArmColliding()
    {
        // #if UNITY_EDITOR
        // debugCapsules.Clear();
        // #endif

        HashSet<Collider> colliders = currentArmCollisions();

        return colliders.Count > 0;
    }

    private bool shouldHalt() {
        return collisionListener.ShouldHalt();
    }

    // Restricts front hemisphere for arm movement
    private bool validArmTargetPosition(Vector3 targetWorldPosition) {
        var targetShoulderSpace = this.transform.InverseTransformPoint(targetWorldPosition) - new Vector3(0, 0, originToShoulderLength);
        //check if not behind, check if not hyper extended
        return targetShoulderSpace.z >= 0.0f && targetShoulderSpace.magnitude <= extendedArmLenth;
    }

    public void moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target, 
        float unitsPerSecond,
        float fixedDeltaTime = 0.02f,
        bool returnToStartPositionIfFailed = false, 
        string whichSpace = "arm", 
        bool restrictTargetPosition = false,
        bool disableRendering = false
    ) {
        

        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();

        var arm = this;
        
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


        // TODO Remove this after restrict movement is finalized
        var targetShoulderSpace = (this.transform.InverseTransformPoint(targetWorldPos) - new Vector3(0, 0, originToShoulderLength));

        #if UNITY_EDITOR
        Debug.Log("pos target  " + target + " world " + targetWorldPos + " remaining " + targetShoulderSpace.z + " magnitude " + targetShoulderSpace.magnitude + " extendedArmLength " + extendedArmLenth);
        #endif
        
        if (restrictTargetPosition && !validArmTargetPosition(targetWorldPos)) {
            var k = this.transform.position + this.transform.TransformPoint(new Vector3(0, 0, originToShoulderLength));
            targetShoulderSpace = (this.transform.InverseTransformPoint(targetWorldPos) - new Vector3(0, 0, originToShoulderLength));
            controller.actionFinished(false, $"Invalid target: Position '{target}' in space '{whichSpace}' is behind shoulder.");
            return;
        }
        
        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;

        var moveCall = ContinuousMovement
            .move(
                controller,
                collisionListener,
                armTarget,
                targetWorldPos,
                disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
                unitsPerSecond,
                returnToStartPositionIfFailed,
                false
        );

        if (disableRendering) {
            controller.unrollSimulatePhysics(
                moveCall,
                fixedDeltaTime
            );
        }
        else {
            StartCoroutine(
                moveCall
            );
        }
    }

    public void moveArmHeight(
        PhysicsRemoteFPSAgentController controller, 
        float height, 
        float unitsPerSecond, 
        float fixedDeltaTime = 0.02f, 
        bool returnToStartPositionIfFailed = false,
        bool disableRendering = false) {

        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();
            
        //first check if the target position is within bounds of the agent's capsule center/height extents
        //if not, actionFinished false with error message listing valid range defined by extents
        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 cc_center = cc.center;
        Vector3 cc_maxY = cc.center + new Vector3(0, cc.height/2f, 0);
        Vector3 cc_minY = cc.center + new Vector3(0, (-cc.height/2f)/2f, 0); //this is halved to prevent arm clipping into floor

        //linear function that take height and adjusts targetY relative to min/max extents
        float targetY = ((cc_maxY.y - cc_minY.y)*(height)) + cc_minY.y;

        Vector3 target = new Vector3(this.transform.localPosition.x, targetY, 0);

        var moveCall = ContinuousMovement
            .move(
                controller,
                collisionListener,
                this.transform,
                target,
                disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
                unitsPerSecond,
                returnToStartPositionIfFailed,
                true
        );

        if (disableRendering) {
            controller.unrollSimulatePhysics(
                moveCall,
                fixedDeltaTime
            );
        }
        else {
            StartCoroutine(
                moveCall
            );
        }
    }

    public void rotateHand(
        PhysicsRemoteFPSAgentController controller,
        Quaternion targetQuat,
        float degreesPerSecond, 
        bool disableRendering = false, 
        float fixedDeltaTime = 0.02f, 
        bool returnToStartPositionIfFailed = false
    )
    {
        collisionListener.Reset();
        var rotate = ContinuousMovement.rotate(
            controller,
            collisionListener,
            armTarget.transform,
            armTarget.transform.rotation * targetQuat,
            disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
            degreesPerSecond,
            returnToStartPositionIfFailed
        );

        if (disableRendering) {
            controller.unrollSimulatePhysics(
                rotate,
                fixedDeltaTime
            );
        }
        else {
            StartCoroutine(rotate);
        }
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
        // var at = this.transform.InverseTransformPoint(armTarget.position) - new Vector3(0, 0, originToShoulderLength);
        // Debug.Log("Pickup " + at.magnitude);
        bool pickedUp = false;
        //grab all sim objects that are currently colliding with magnet sphere
        foreach(SimObjPhysics sop in WhatObjectsAreInsideMagnetSphereAsSOP())
        {
            Rigidbody rb = sop.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            sop.transform.SetParent(magnetSphere.transform);
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            //ok new plan, clone the "myColliders" of the sop and
            //then set them all to isTrigger = True
            //and parent them to the correct joint
            List<Collider> cols = new List<Collider>();

            foreach (Collider c in sop.MyColliders)
            {
                Collider clone = Instantiate(c, c.transform.position, c.transform.rotation, FourthJoint);
                clone.isTrigger = true;
                cols.Add(clone);
            }

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
        foreach(KeyValuePair<SimObjPhysics, List<Collider>> sop in HeldObjects) 
        {
            Rigidbody rb = sop.Key.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = false;

            //delete cloned colliders
            foreach (Collider c in sop.Value)
            {
                Destroy(c.gameObject);
            }

            GameObject topObject = GameObject.Find("Objects");

            if(topObject != null)
            {
                sop.Key.transform.parent = topObject.transform;
            }

            else
            {
                sop.Key.transform.parent = null;
            }
            
            rb.WakeUp();
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

        //Declare variables used for processing metadata
        float angleRot;
        Vector3 vectorRot;
        var jointMetaRoot = new JointMetadata();

        //Assign metadata to FirstJoint joint separately from others, since its angler joint uniquely refers to the tip's orientation rather than the base's, which is not what we want in this case
        jointMetaRoot.position = joint.position;
        jointMetaRoot.name = joint.name;
        jointMetaRoot.rootRelativePosition = Vector3.zero;

        jointMetaRoot.localRotation = new Vector4 (1, 0, 0, 0);

        joint.rotation.ToAngleAxis(out angleRot, out vectorRot);
        jointMetaRoot.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

        jointMetaRoot.rootRelativeRotation = new Vector4 (1, 0, 0, 0);

        joints.Add(jointMetaRoot);

        //Assign joint metadata to remaining joints, which all have identical hierarchies
        for (var i = 2; i <= 4; i++) {
            joint = joint.Find("robot_arm_" + i + "_jnt");

            var jointMeta = new JointMetadata();

            jointMeta.name = joint.name;
            jointMeta.position = joint.position;
            //local position of joint is meaningless because it never changes relative to its parent joint, we use rootRelative instead
            jointMeta.rootRelativePosition = FirstJoint.InverseTransformPoint(joint.position);

            //local rotation currently relative to immediate parent joint
            if (joint.rotation != joint.GetChild(0).rotation)
            {
                joint.GetChild(0).localRotation.ToAngleAxis(out angleRot, out vectorRot);//getchild to grab the angler since that is what actually changes the geometry angle
                jointMeta.localRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            }

            //edge case for where angler and parent rotations are aligned, which Quaternions have trouble resolving, so we hard-code it here
            else
            {
                jointMeta.localRotation = new Vector4(1, 0, 0, 0);
            }
            
            //world relative rotation
            joint.GetChild(0).rotation.ToAngleAxis(out angleRot, out vectorRot);//getchild to grab the angler since that is what actually changes the geometry angle
            jointMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);

            //rotation relative to root joint/agent
            //root forward and agent forward are always the same
            if (FirstJoint.rotation != joint.GetChild(0).rotation)
            {
                Quaternion.Euler(FirstJoint.InverseTransformDirection(joint.GetChild(0).eulerAngles)).ToAngleAxis(out angleRot, out vectorRot);//getchild to grab the angler since that is what actually changes the geometry angle
                jointMeta.rootRelativeRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            }

            //edge case for when angler and root rotations are aligned, which Quaternions have trouble resolving, so we hard-code it here
            else
            {
                jointMeta.rootRelativeRotation = new Vector4(1, 0, 0, 0);
            }

            joints.Add(jointMeta);
        }

        meta.joints = joints.ToArray();

        //metadata for any objects currently held by the hand on the arm
        //note this is different from objects intersecting the hand's sphere,
        //there could be a case where an object is inside the sphere but not picked up by the hand
        List<string> HeldObjectIDs = new List<string>();

        if(HeldObjects != null)
        {
            foreach(KeyValuePair<SimObjPhysics, List<Collider>> sop in HeldObjects) 
            {
                HeldObjectIDs.Add(sop.Key.objectID);
            }
        }

        meta.HeldObjects = HeldObjectIDs;

        meta.HandSphereCenter = transform.TransformPoint(magnetSphere.center);

        meta.HandSphereRadius = magnetSphere.radius;

        meta.PickupableObjectsInsideHandSphere = WhatObjectsAreInsideMagnetSphereAsObjectID();

        return meta;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(debugCapsules.Count > 0)
        {
            foreach (GizmoDrawCapsule thing in debugCapsules)
            {
                Gizmos.DrawWireSphere(thing.p0, thing.radius);
                Gizmos.DrawWireSphere(thing.p1, thing.radius);
            }
        }
    }
    #endif
}
