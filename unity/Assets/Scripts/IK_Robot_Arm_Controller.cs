using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class IK_Robot_Arm_Controller : MonoBehaviour {
    private Transform armTarget;
    private Transform handCameraTransform;

    [SerializeField]
    private SphereCollider magnetSphere = null;

    [SerializeField]
    private WhatIsInsideMagnetSphere magnetSphereComp = null;

    [SerializeField]
    private GameObject MagnetRenderer = null;

    private PhysicsRemoteFPSAgentController PhysicsController;

    // references to the joints of the mid level arm
    [SerializeField]
    private Transform FirstJoint = null;

    [SerializeField]
    private Transform FourthJoint = null;

    // dict to track which picked up object has which set of trigger colliders
    // which we have to parent and reparent in order for arm collision to detect
    [SerializeField]
    public Dictionary<SimObjPhysics, List<Collider>> heldObjects = new Dictionary<SimObjPhysics, List<Collider>>();

    // private bool StopMotionOnContact = false;
    // Start is called before the first frame update

    [SerializeField]
    public CapsuleCollider[] ArmCapsuleColliders;

    [SerializeField]
    public BoxCollider[] ArmBoxColliders;

    [SerializeField]
    private CapsuleCollider[] agentCapsuleCollider = null;

    private float originToShoulderLength = 0f;

    private const float extendedArmLength = 0.6325f;

    public CollisionListener collisionListener;

    void Start() {
        armTarget = this.transform
            .Find("robot_arm_FK_IK_rig")
            .Find("IK_rig")
            .Find("IK_pos_rot_manipulator");

        // FirstJoint = this.transform.Find("robot_arm_1_jnt"); this is now set via serialize field, along with the other joints
        handCameraTransform = this.transform.FirstChildOrDefault(x => x.name == "robot_arm_4_jnt");

        // calculating based on distance from origin of arm to the 2nd joint, which will always be constant
        this.originToShoulderLength = Vector3.Distance(
            this.transform.FirstChildOrDefault(
                x => x.name == "robot_arm_2_jnt"
            ).position,
            this.transform.position
        );

        this.collisionListener = this.GetComponentInParent<CollisionListener>();

        List<CapsuleCollider> armCaps = new List<CapsuleCollider>();
        List<BoxCollider> armBoxes = new List<BoxCollider>();

        // get references to all colliders in arm. Remove trigger colliders so there are no duplicates when using these as reference for
        // overlap casts since the trigger colliders are themselves duplicates of the nontrigger colliders.
        armCaps.AddRange(gameObject.GetComponentsInChildren<CapsuleCollider>());
        armBoxes.AddRange(gameObject.GetComponentsInChildren<BoxCollider>());

        // clean up arm colliders, removing triggers
        List<CapsuleCollider> cleanedCaps = new List<CapsuleCollider>();
        foreach (CapsuleCollider c in armCaps) {
            if (!c.isTrigger) {
                cleanedCaps.Add(c);
            }
        }

        ArmCapsuleColliders = cleanedCaps.ToArray();

        List<BoxCollider> cleanedBoxes = new List<BoxCollider>();
        foreach (BoxCollider b in armBoxes) {
            if (!b.isTrigger) {
                cleanedBoxes.Add(b);
            }
        }

        ArmBoxColliders = cleanedBoxes.ToArray();
    }

    // NOTE: removing this for now, will add back if functionality is required later
    // public void SetStopMotionOnContact(bool b)
    // {
    //     StopMotionOnContact = b;
    // }

    // Update is called once per frame
    void Update() {
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

        // add the AgentCapsule to the ArmCapsuleColliders for the capsule collider check
        List<CapsuleCollider> capsules = new List<CapsuleCollider>();
        capsules.AddRange(ArmCapsuleColliders);
        capsules.AddRange(agentCapsuleCollider);

        // create overlap box/capsule for each collider and check the result I guess
        foreach (CapsuleCollider c in capsules) {
            Vector3 center = c.transform.TransformPoint(c.center);
            float radius = c.radius;

            // direction of CapsuleCollider's orientation in local space
            Vector3 dir = new Vector3();

            switch (c.direction) {
                // x just in case
                case 0:
                    // get world space direction of this capsule's local right vector
                    dir = c.transform.right;
                    break;
                // y just in case
                case 1:
                    // get world space direction of this capsule's local up vector
                    dir = c.transform.up;
                    break;
                // z because all arm colliders have direction z by default
                case 2:
                    // get world space direction of this capsule's local forward vector
                    dir = c.transform.forward;

                    // this doesn't work because transform.right is in world space already,
                    // how to get transform.localRight?
                    break;
            }

            // debug draw forward of each joint
            // #if UNITY_EDITOR
            // // debug draw
            // Debug.DrawLine(center, center + dir * 2.0f, Color.red, 10.0f);
            // #endif

            // center in world space + direction with magnitude (1/2 height - radius)
            Vector3 point0 = center + dir * (c.height / 2 - radius);

            // point 1
            // center in world space - direction with magnitude (1/2 height - radius)
            Vector3 point1 = center - dir * (c.height / 2 - radius);

            // debug draw ends of each capsule of each joint
            // #if UNITY_EDITOR
            // GizmoDrawCapsule gdc = new GizmoDrawCapsule();
            // gdc.p0 = point0;
            // gdc.p1 = point1;
            // gdc.radius = radius;
            // debugCapsules.Add(gdc);
            // #endif

            // ok now finally let's make some overlap capsules
            Collider[] cols = Physics.OverlapCapsule(
                point0: point0,
                point1: point1,
                radius: radius,
                layerMask: LayerMask.GetMask("SimObjVisible"),
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );
            foreach (Collider col in cols) {
                colliders.Add(col);
            }
        }

        // also check if the couple of box colliders are colliding
        foreach (BoxCollider b in ArmBoxColliders) {
            Collider[] cols = Physics.OverlapBox(
                center: b.transform.TransformPoint(b.center),
                halfExtents: b.size / 2.0f,
                orientation: b.transform.rotation,
                layerMask: LayerMask.GetMask("SimObjVisible"),
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );
            foreach (Collider col in cols) {
                colliders.Add(col);
            }
        }
        return colliders;
    }

    public bool IsArmColliding() {
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
        Vector3 targetShoulderSpace = (
            this.transform.InverseTransformPoint(targetWorldPosition)
            - new Vector3(0, 0, originToShoulderLength)
        );

        // check if not behind, check if not hyper extended
        return targetShoulderSpace.z >= 0.0f && targetShoulderSpace.magnitude <= extendedArmLength;
    }

    public void moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target,
        float unitsPerSecond,
        float fixedDeltaTime = 0.02f,
        bool returnToStart = false,
        string coordinateSpace = "arm",
        bool restrictTargetPosition = false,
        bool disableRendering = false
    ) {
        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();

        IK_Robot_Arm_Controller arm = this;

        // Move arm based on hand space or arm origin space
        // Vector3 targetWorldPos = handCameraSpace ? handCameraTransform.TransformPoint(target) : arm.transform.TransformPoint(target);
        Vector3 targetWorldPos = Vector3.zero;

        switch (coordinateSpace) {
            case "world":
                // world space, can be used to move directly toward positions
                // returned by sim objects
                targetWorldPos = target;
                break;
            case "wrist":
                // space relative to base of the wrist, where the camera is
                targetWorldPos = handCameraTransform.TransformPoint(target);
                break;
            case "armBase":
                // space relative to the root of the arm, joint 1
                targetWorldPos = arm.transform.TransformPoint(target);
                break;
            default:
                throw new ArgumentException("Invalid coordinateSpace: " + coordinateSpace);
        }

        // TODO Remove this after restrict movement is finalized
        Vector3 targetShoulderSpace = (this.transform.InverseTransformPoint(targetWorldPos) - new Vector3(0, 0, originToShoulderLength));

#if UNITY_EDITOR
        Debug.Log(
            $"pos target {target} world {targetWorldPos} remaining {targetShoulderSpace.z}\n" +
            $"magnitude {targetShoulderSpace.magnitude} extendedArmLength {extendedArmLength}"
        );
#endif

        if (restrictTargetPosition && !validArmTargetPosition(targetWorldPos)) {
            targetShoulderSpace = (
                this.transform.InverseTransformPoint(targetWorldPos)
                - new Vector3(0, 0, originToShoulderLength)
            );
            throw new InvalidOperationException(
                $"Invalid target: Position '{target}' in space '{coordinateSpace}' is behind shoulder."
            );
        }

        Vector3 originalPos = armTarget.position;
        Vector3 targetDirectionWorld = (targetWorldPos - originalPos).normalized;

        IEnumerator moveCall = ContinuousMovement.move(
            controller,
            collisionListener,
            armTarget,
            targetWorldPos,
            disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
            unitsPerSecond,
            returnToStart,
            false
        );

        if (disableRendering) {
            controller.unrollSimulatePhysics(
                moveCall,
                fixedDeltaTime
            );
        } else {
            StartCoroutine(
                moveCall
            );
        }
    }

    public void moveArmBase(
        PhysicsRemoteFPSAgentController controller,
        float height,
        float unitsPerSecond,
        float fixedDeltaTime = 0.02f,
        bool returnToStartPositionIfFailed = false,
        bool disableRendering = false
    ) {
        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();

        // first check if the target position is within bounds of the agent's capsule center/height extents
        // if not, actionFinished false with error message listing valid range defined by extents
        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 cc_center = cc.center;
        Vector3 cc_maxY = cc.center + new Vector3(0, cc.height / 2f, 0);
        Vector3 cc_minY = cc.center + new Vector3(0, (-cc.height / 2f) / 2f, 0); // this is halved to prevent arm clipping into floor

        // linear function that take height and adjusts targetY relative to min/max extents
        float targetY = ((cc_maxY.y - cc_minY.y) * (height)) + cc_minY.y;

        Vector3 target = new Vector3(this.transform.localPosition.x, targetY, 0);

        IEnumerator moveCall = ContinuousMovement.move(
            controller: controller,
            collisionListener: collisionListener,
            moveTransform: this.transform,
            targetPosition: target,
            fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
            unitsPerSecond: unitsPerSecond,
            returnToStartPropIfFailed: returnToStartPositionIfFailed,
            localPosition: true
        );

        if (disableRendering) {
            controller.unrollSimulatePhysics(
                enumerator: moveCall,
                fixedDeltaTime: fixedDeltaTime
            );
        } else {
            StartCoroutine(moveCall);
        }
    }

    public void rotateHand(
        PhysicsRemoteFPSAgentController controller,
        Quaternion targetQuat,
        float degreesPerSecond,
        bool disableRendering = false,
        float fixedDeltaTime = 0.02f,
        bool returnToStartPositionIfFailed = false
    ) {
        collisionListener.Reset();
        IEnumerator rotate = ContinuousMovement.rotate(
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
        } else {
            StartCoroutine(rotate);
        }
    }

    public List<string> WhatObjectsAreInsideMagnetSphereAsObjectID() {
        return magnetSphereComp.CurrentlyContainedSimObjectsByID();
    }

    public List<SimObjPhysics> WhatObjectsAreInsideMagnetSphereAsSOP() {
        return magnetSphereComp.CurrentlyContainedSimObjects();
    }

    public IEnumerator ReturnObjectsInMagnetAfterPhysicsUpdate(PhysicsRemoteFPSAgentController controller) {
        yield return new WaitForFixedUpdate();
        List<string> listOfSOP = new List<string>();
        foreach (string oid in this.WhatObjectsAreInsideMagnetSphereAsObjectID()) {
            listOfSOP.Add(oid);
        }
        Debug.Log("objs: " + string.Join(", ", listOfSOP));
        controller.actionFinished(true, listOfSOP);
    }

    public bool PickupObject(List<string> objectIds, ref string errorMessage) {
        // var at = this.transform.InverseTransformPoint(armTarget.position) - new Vector3(0, 0, originToShoulderLength);
        // Debug.Log("Pickup " + at.magnitude);
        bool pickedUp = false;

        // grab all sim objects that are currently colliding with magnet sphere
        foreach (SimObjPhysics sop in WhatObjectsAreInsideMagnetSphereAsSOP()) {
            if (objectIds != null) {
                if (!objectIds.Contains(sop.objectID)) {
                    continue;
                }
            }

            Rigidbody rb = sop.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            sop.transform.SetParent(magnetSphere.transform);
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            if (sop.IsOpenable) {
                CanOpen_Object coj = sop.gameObject.GetComponent<CanOpen_Object>();

                // if an openable object receives OnTriggerEnter events
                // the RigidBody can be switched to Kinematic false 
                coj.triggerEnabled = false;
            }

            // ok new plan, clone the "myColliders" of the sop and
            // then set them all to isTrigger = True
            // and parent them to the correct joint
            List<Collider> cols = new List<Collider>();

            foreach (Collider c in sop.MyColliders) {
                Collider clone = Instantiate(
                    c,
                    c.transform.position,
                    c.transform.rotation,
                    FourthJoint
                );
                clone.isTrigger = true;

                // must disable the colliders on the held object so they 
                // don't interact with anything
                c.enabled = false;
                cols.Add(clone);
            }

            pickedUp = true;
            heldObjects.Add(sop, cols);
        }

        if (!pickedUp) {
            errorMessage = (
                objectIds != null
                ? "No objects (specified by objectId) were valid to be picked up by the arm"
                : "No objects were valid to be picked up by the arm"
            );
        }

        // note: how to handle cases where object breaks if it is shoved into another object?
        // make them all unbreakable?
        return pickedUp;
    }

    public void DropObject() {
        // grab all sim objects that are currently colliding with magnet sphere
        foreach (KeyValuePair<SimObjPhysics, List<Collider>> sop in heldObjects) {
            Rigidbody rb = sop.Key.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = false;

            // delete cloned colliders
            foreach (Collider c in sop.Value) {
                Destroy(c.gameObject);
            }

            foreach (Collider c in sop.Key.MyColliders) {
                // re-enable colliders since they were disabled during pickup
                c.enabled = true;
            }

            if (sop.Key.IsOpenable) {
                CanOpen_Object coj = sop.Key.gameObject.GetComponent<CanOpen_Object>();
                coj.triggerEnabled = true;
            }

            GameObject topObject = GameObject.Find("Objects");

            if (topObject != null) {
                sop.Key.transform.parent = topObject.transform;
            } else {
                sop.Key.transform.parent = null;
            }

            rb.WakeUp();
        }

        // clear all now dropped objects
        heldObjects.Clear();
    }

    public void SetHandSphereRadius(float radius) {
        // Magnet.transform.localScale = new Vector3(radius, radius, radius);
        magnetSphere.radius = radius;
        MagnetRenderer.transform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);
        magnetSphere.transform.localPosition = new Vector3(0, 0, 0.01f + radius);
        MagnetRenderer.transform.localPosition = new Vector3(0, 0, 0.01f + radius);
    }

    public ArmMetadata GenerateMetadata() {
        ArmMetadata meta = new ArmMetadata();
        // meta.handTarget = armTarget.position;
        Transform joint = transform;
        List<JointMetadata> joints = new List<JointMetadata>();

        // Declare variables used for processing metadata
        Transform parentJoint;
        float angleRot;
        Vector3 vectorRot;
        Quaternion currentRotation;

        // Assign joint metadata to remaining joints, which all have identical hierarchies
        for (int i = 1; i <= 4; i++) {
            joint = joint.Find("robot_arm_" + i + "_jnt");

            JointMetadata jointMeta = new JointMetadata();

            // JOINT NAME
            jointMeta.name = joint.name;

            // WORLD RELATIVE POSITION
            jointMeta.position = joint.position;

            // ROOT-JOINT RELATIVE POSITION
            // Parent-relative position of joint is meaningless because it never changes relative to its parent joint, so we use rootRelative instead
            jointMeta.rootRelativePosition = FirstJoint.InverseTransformPoint(joint.position);

            // WORLD RELATIVE ROTATION
            // GetChild grabs angler since that is what actually changes the geometry angle
            currentRotation = joint.GetChild(0).rotation;

            // Check that world-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);

                // Debug.Log(joint.name + "'s euler-angles of " + joint.GetChild(0).eulerAngles + " resulted in Quaternion " + Quaternion.Euler(joint.GetChild(0).eulerAngles) + " which should either be the same as or a corrected version of " + joint.GetChild(0).rotation);
                jointMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            } else {
                // Debug.Log(joint.name + "'s world-rotation of " + currentRotation + " is EVILLLLL!");
                jointMeta.rotation = new Vector4(1, 0, 0, 0);
            }

            // ROOT-JOINT RELATIVE ROTATION
            // Root-forward and agent-forward are always the same

            // GetChild grabs angler since that is what actually changes the geometry angle
            currentRotation = Quaternion.Euler(FirstJoint.InverseTransformDirection(joint.GetChild(0).eulerAngles));

            // Check that root-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jointMeta.rootRelativeRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            } else {
                // Debug.Log(joint.name + "'s root-rotation of " + currentRotation + " is EVILLLLL!");
                jointMeta.rootRelativeRotation = new Vector4(1, 0, 0, 0);
            }

            // PARENT-JOINT RELATIVE ROTATION
            if (i != 1) {
                parentJoint = joint.parent;

                // Grab rotation of current joint's angler relative to parent joint's angler, and convert it to a quaternion
                currentRotation = Quaternion.Euler(
                    parentJoint.GetChild(0).InverseTransformDirection(
                        joint.GetChild(0).eulerAngles
                    )
                );

                // Check that parent-relative rotation is angle-axis-notation-compatible
                if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                    // Convert parent-relative rotation to angle-axis notation
                    currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                    jointMeta.localRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
                } else {
                    // Debug.Log(joint.name + "'s parent-rotation of " + currentRotation + " is EVILLLLL!");
                    jointMeta.localRotation = new Vector4(1, 0, 0, 0);
                }
            } else {
                // Special case for robot_arm_1_jnt because it has no parent-joint
                jointMeta.localRotation = jointMeta.rootRelativeRotation;
            }

            joints.Add(jointMeta);
        }

        meta.joints = joints.ToArray();

        // metadata for any objects currently held by the hand on the arm
        // note this is different from objects intersecting the hand's sphere,
        // there could be a case where an object is inside the sphere but not picked up by the hand
        List<string> heldObjectIDs = new List<string>();
        if (heldObjects != null) {
            foreach (KeyValuePair<SimObjPhysics, List<Collider>> sop in heldObjects) {
                heldObjectIDs.Add(sop.Key.objectID);
            }
        }

        meta.heldObjects = heldObjectIDs;
        meta.handSphereCenter = transform.TransformPoint(magnetSphere.center);
        meta.handSphereRadius = magnetSphere.radius;
        meta.pickupableObjects = WhatObjectsAreInsideMagnetSphereAsObjectID();
        return meta;
    }

#if UNITY_EDITOR
    public class GizmoDrawCapsule {
        public Vector3 p0;
        public Vector3 p1;
        public float radius;
    }

    List<GizmoDrawCapsule> debugCapsules = new List<GizmoDrawCapsule>();

    private void OnDrawGizmos() {
        if (debugCapsules.Count > 0) {
            foreach (GizmoDrawCapsule thing in debugCapsules) {
                Gizmos.DrawWireSphere(thing.p0, thing.radius);
                Gizmos.DrawWireSphere(thing.p1, thing.radius);
            }
        }
    }
#endif
}
