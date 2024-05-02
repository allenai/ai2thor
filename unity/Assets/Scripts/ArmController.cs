using UnityEngine;
using System;

using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;

public abstract class ArmController : MonoBehaviour, Arm, MovableContinuous {


    [SerializeField]
    public Transform FinalJoint;

    [SerializeField]
    protected Transform armTarget;

    [SerializeField]
    protected WhatIsInsideMagnetSphere magnetSphereComp;

    [SerializeField] 
    public SphereCollider magnetSphere = null;

    [SerializeField]
    protected GameObject MagnetRenderer = null;

    [SerializeField]
    public CapsuleCollider[] ArmCapsuleColliders {get; protected set; }

    [SerializeField]
    public BoxCollider[] ArmBoxColliders {get; protected set; }

    [SerializeField]
    public CapsuleCollider agentCapsuleCollider {get; protected set; } = null;

    [HideInInspector]
    public CollisionListener collisionListener;

    //
    [SerializeField]
    public Dictionary<SimObjPhysics, HashSet<Collider>> heldObjects { get; protected set; } = new Dictionary<SimObjPhysics, HashSet<Collider>>();

    protected bool ignoreHeldObjectToAgentCollisions = false;


    protected virtual bool validArmTargetPosition(Vector3 targetWorldPosition) {
        return true;
    }

    protected abstract void lastStepCallback();

    public abstract Transform pickupParent();

    public abstract Vector3 wristSpaceOffsetToWorldPos(Vector3 offset);
    public abstract Vector3 armBaseSpaceOffsetToWorldPos(Vector3 offset);

    public abstract Vector3 pointToWristSpace(Vector3 point);
    public abstract Vector3 pointToArmBaseSpace(Vector3 point);

    public abstract void ContinuousUpdate(float fixedDeltaTime);

    public virtual ActionFinished FinishContinuousMove(BaseFPSAgentController controller) {
        bool actionSuccess = !this.ShouldHalt();
        string errorMessage = this.GetHaltMessage();

        return new ActionFinished() {
            success = actionSuccess,
            errorMessage = errorMessage
        };
    }

    // bool actionSuccess = !movable.ShouldHalt();
            // string errorMessage = movable.GetHaltMessage();
            // if (!actionSuccess) {
            //      setProp(moveTransform, resetProp);
            // }

            // return new ActionFinished() {
            //     success = actionSuccess,
            //     errorMessage = errorMessage
            // };
    public abstract GameObject GetArmTarget();
    public abstract ArmMetadata GenerateMetadata();

    public virtual bool ShouldHalt() {
        return collisionListener.ShouldHalt();
    }

    public Vector3 MagnetSphereWorldCenter() {
        return magnetSphere.transform.TransformPoint(magnetSphere.center);
    }

    public virtual string GetHaltMessage() {
        var staticCollisions = collisionListener?.StaticCollisions().ToList();

            // decide if we want to return to original property or last known property before collision
            if (staticCollisions.Count > 0) {
                var sc = staticCollisions[0];

                // if we hit a sim object
                if (sc.isSimObj) {
                    return "Collided with static/kinematic sim object: '" + sc.simObjPhysics.name + "', could not reach target: '" + armTarget + "'.";
                }

                // if we hit a structural object that isn't a sim object but still has static collision
                if (!sc.isSimObj) {
                    return "Collided with static structure in scene: '" + sc.gameObject.name + "', could not reach target: '" + armTarget + "'.";
                }
            }
            return "";
    }

    // public virtual ActionFinished FinishContinuousMove(

    public bool IsArmColliding() {
        HashSet<Collider> colliders = this.currentArmCollisions();
        return colliders.Count > 0;
    }

    public IEnumerator withLastStepCallback(IEnumerator steps) {
        while (steps.MoveNext()) {
            yield return steps.Current;
        }
        lastStepCallback();
    }

     public HashSet<Collider> currentArmCollisions() {
         HashSet<Collider> colliders = new HashSet<Collider>();

         // add the AgentCapsule to the ArmCapsuleColliders for the capsule collider check
         List<CapsuleCollider> capsules = new List<CapsuleCollider>();

        capsules.AddRange(ArmCapsuleColliders);
        capsules.Add(agentCapsuleCollider);

        // create overlap box/capsule for each collider and check the result I guess
        foreach (CapsuleCollider c in capsules) {
            if (c.isTrigger || !c.gameObject.active) {
                continue;
            }

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
                layerMask: LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"),
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );
            foreach (Collider col in cols) {
                colliders.Add(col);
            }
        }

        // also check if the couple of box colliders are colliding
        foreach (BoxCollider b in ArmBoxColliders) {
            if (b.isTrigger || !b.gameObject.active) {
                continue;
            }
            Collider[] cols = Physics.OverlapBox(
                center: b.transform.TransformPoint(b.center),
                halfExtents: b.size / 2.0f,
                orientation: b.transform.rotation,
                layerMask: LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"),
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );
            foreach (Collider col in cols) {
                colliders.Add(col);
            }
        }
        return colliders;
    }


    public virtual IEnumerator moveArmRelative(
        PhysicsRemoteFPSAgentController controller,
        Vector3 offset,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition
    ) {
        Vector3 offsetWorldPos;
        switch (coordinateSpace) {
            case "world":
                // world space, can be used to move directly toward positions
                // returned by sim objects
                offsetWorldPos = offset;
                break;
            case "wrist":
                // space relative to base of the wrist, where the camera is
                offsetWorldPos = wristSpaceOffsetToWorldPos(offset);
                break;
            case "armBase":
                // space relative to the root of the arm, joint 1
                offsetWorldPos = armBaseSpaceOffsetToWorldPos(offset);
                break;
            default:
                throw new ArgumentException("Invalid coordinateSpace: " + coordinateSpace);
        }
        return moveArmTarget(
            controller: controller,
            target: armTarget.position + offsetWorldPos,
            unitsPerSecond: unitsPerSecond,
            fixedDeltaTime: fixedDeltaTime,
            returnToStart: returnToStart,
            coordinateSpace: "world",
            restrictTargetPosition: restrictTargetPosition
        );
    }

    

    public virtual IEnumerator moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition
    ) {
        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();

        var arm = this;

        // Move arm based on hand space or arm origin space
        Vector3 targetWorldPos;
        switch (coordinateSpace) {
            case "world":
                // world space, can be used to move directly toward positions
                // returned by sim objects
                targetWorldPos = target;
                break;
            case "wrist":
                // space relative to base of the wrist, where the camera is
                targetWorldPos = pointToWristSpace(target);
                break;
            case "armBase":
                // space relative to the root of the arm, joint 1
                targetWorldPos = pointToArmBaseSpace(target);
                break;
            default:
                throw new ArgumentException("Invalid coordinateSpace: " + coordinateSpace);
        }

        if (restrictTargetPosition && !validArmTargetPosition(targetWorldPos)) {
            throw new InvalidOperationException(
                $"Invalid target: Position '{target}' in space '{coordinateSpace}' is behind shoulder."
            );
        }

        return withLastStepCallback(
            ContinuousMovement.move(
                movable: this,
                controller,
                armTarget,
                targetWorldPos,
                fixedDeltaTime,
                unitsPerSecond,
                returnToStart,
                false
            )
        );
    }


    public virtual IEnumerator moveArmBase(
        PhysicsRemoteFPSAgentController controller,
        float height,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool normalizedY
    ) {
        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();

        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 capsuleWorldCenter = cc.transform.TransformPoint(cc.center);

        float minY, maxY;
        getCapsuleMinMaxY(controller, out minY, out maxY);

        if (normalizedY) {
            height = (maxY - minY) * height + minY;
        }

        if (height < minY || height > maxY) {
            throw new ArgumentOutOfRangeException($"height={height} value must be in [{minY}, {maxY}].");
        }

        Vector3 target = new Vector3(this.transform.position.x, height, this.transform.position.z);
        return withLastStepCallback(
                ContinuousMovement.move(
                movable: this,
                controller: controller,
                moveTransform: this.transform,
                targetPosition: target,
                fixedDeltaTime: fixedDeltaTime,
                unitsPerSecond: unitsPerSecond,
                returnToStartPropIfFailed: returnToStartPositionIfFailed,
                localPosition: false
            )
        );
    }

     private void getCapsuleMinMaxY(PhysicsRemoteFPSAgentController controller, out float minY, out float maxY) {
        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 capsuleWorldCenter = cc.transform.TransformPoint(cc.center);

        maxY = capsuleWorldCenter.y + cc.height / 2f;
        minY = capsuleWorldCenter.y + (-cc.height / 2f) / 2f;
    }

    public virtual IEnumerator moveArmBaseUp(
        PhysicsRemoteFPSAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed
    ) {
        // clearing out colliders here since OnTriggerExit is not consistently called in Editor
        collisionListener.Reset();

        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 capsuleWorldCenter = cc.transform.TransformPoint(cc.center);
        float minY, maxY;
        getCapsuleMinMaxY(controller, out minY, out maxY);
        float targetY = this.transform.position.y + distance;
        targetY = Mathf.Max(Mathf.Min(targetY, maxY), minY);

        return moveArmBase(
            controller: controller,
            height: targetY,
            unitsPerSecond: unitsPerSecond,
            fixedDeltaTime: fixedDeltaTime,
            returnToStartPositionIfFailed: returnToStartPositionIfFailed,
            normalizedY: false
        );
    }

    public virtual IEnumerator rotateWrist(
        PhysicsRemoteFPSAgentController controller,
        Quaternion rotation,
        float degreesPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed
    ) {
        collisionListener.Reset();
        return withLastStepCallback(
            ContinuousMovement.rotate(
                movable: this,
                controller,
                armTarget.transform,
                armTarget.transform.rotation * rotation,
                fixedDeltaTime,
                degreesPerSecond,
                returnToStartPositionIfFailed
            )
        );
    }

    public List<SimObjPhysics> WhatObjectsAreInsideMagnetSphereAsSOP(bool onlyPickupable) {
        //Debug.Log("calling WhatObjectsAreInsideMagnetSphereAsSOP");
        return magnetSphereComp.CurrentlyContainedSimObjects(onlyPickupable: onlyPickupable);
    }

    public IEnumerator ReturnObjectsInMagnetAfterPhysicsUpdate(PhysicsRemoteFPSAgentController controller) {
        yield return new WaitForFixedUpdate();
        List<string> listOfSOP = new List<string>();
        foreach (SimObjPhysics sop in this.WhatObjectsAreInsideMagnetSphereAsSOP(false)) {
            listOfSOP.Add(sop.ObjectID);
        }
        Debug.Log("objs: " + string.Join(", ", listOfSOP));
        controller.actionFinished(true, listOfSOP);
    }
    
    private Dictionary<GameObject, Vector3> getGameObjectToMultipliedScale(
        GameObject go,
        Vector3 currentScale,
        Dictionary<GameObject, Vector3> gameObjectToScale = null
    ) {
        if (gameObjectToScale == null) {
            gameObjectToScale = new Dictionary<GameObject, Vector3>();
        }

        currentScale = Vector3.Scale(currentScale, go.transform.localScale);
        gameObjectToScale[go] = currentScale;

        foreach (Transform child in go.transform) {
            getGameObjectToMultipliedScale(
                go: child.gameObject,
                currentScale: currentScale,
                gameObjectToScale
            );
        }

        return gameObjectToScale;
    }

   public virtual IEnumerator PickupObject(List<string> objectIds) {
        // var at = this.transform.InverseTransformPoint(armTarget.position) - new Vector3(0, 0, originToShoulderLength);
        // Debug.Log("Pickup " + at.magnitude);
        bool pickedUp = false;

        // grab all sim objects that are currently colliding with magnet sphere
        foreach (SimObjPhysics sop in WhatObjectsAreInsideMagnetSphereAsSOP(onlyPickupable: true)) {
            if (objectIds != null) {
                if (!objectIds.Contains(sop.objectID)) {
                    continue;
                }
            }

            Dictionary<GameObject, Vector3> gameObjectToMultipliedScale = getGameObjectToMultipliedScale(
                go: sop.gameObject,
                currentScale: new Vector3(1f, 1f, 1f)
            );
            Rigidbody rb = sop.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            sop.transform.SetParent(pickupParent());
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.detectCollisions = false; // Disable detecting of collisions

            if (sop.IsOpenable) {
                CanOpen_Object coj = sop.gameObject.GetComponent<CanOpen_Object>();

                // if an openable object receives OnTriggerEnter events
                // the RigidBody can be switched to Kinematic false 
                coj.triggerEnabled = false;
            }

            // ok new plan, clone the "myColliders" of the sop and
            // then set them all to isTrigger = True
            // and parent them to the correct joint
            HashSet<Collider> cols = new HashSet<Collider>();

            foreach (Collider c in sop.MyColliders) {
                // One set of colliders are used to check collisions
                // with kinematic objects
                Collider clone = Instantiate(
                    original: c,
                    position: c.transform.position,
                    rotation: c.transform.rotation,
                    parent: FinalJoint
                );
                clone.transform.localScale = gameObjectToMultipliedScale[c.gameObject];

                clone.isTrigger = true;
                collisionListener.registerChild(clone);
                cols.Add(clone);

                // The other set is used to interact with moveable objects
                clone = Instantiate(
                    original: c,
                    position: c.transform.position,
                    rotation: c.transform.rotation,
                    parent: FinalJoint
                );
                clone.transform.localScale = gameObjectToMultipliedScale[c.gameObject];
                cols.Add(clone);

                // OLD: must disable the colliders on the held object so they  don't interact with anything
                // PROBLEM: turning off colliders like this causes bounding boxes to be wrongly updated
                // NEW: We turn on rb.detectCollisions = false above
                // c.enabled = false;
            }

            // TODO: Ignore all collisions between arm/held object colliders (for efficiency)!
            // Removed first loop because of  wanting heldobject->arm collision events
            var colliders = this.GetComponentsInChildren<Collider>();
            Debug.Log($"------- ignoreHeldObjectToAgentCollisions {ignoreHeldObjectToAgentCollisions}");
            if (ignoreHeldObjectToAgentCollisions) {
                foreach (Collider c0 in colliders) {
                    foreach (Collider c1 in cols) {
                        Physics.IgnoreCollision(c0, c1);
                    }
                }
            }
            foreach (Collider c0 in cols) {
                foreach (Collider c1 in cols) {
                    Physics.IgnoreCollision(c0, c1);
                }
            }

            pickedUp = true;
            heldObjects.Add(sop, cols);
        }

        var errorMessage = "";
        if (!pickedUp) {
            errorMessage = (
                objectIds != null
                ? "No objects (specified by objectId) were valid to be picked up by the arm"
                : "No objects were valid to be picked up by the arm"
            );
        }

        // note: how to handle cases where object breaks if it is shoved into another object?
        // make them all unbreakable?
        yield return new ActionFinished() {
            success = pickedUp,
            errorMessage= errorMessage
        };
    }

    public virtual IEnumerator DropObject() {
        // grab all sim objects that are currently colliding with magnet sphere
        foreach (KeyValuePair<SimObjPhysics, HashSet<Collider>> sop in heldObjects) {
            Rigidbody rb = sop.Key.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = false;

            // delete cloned colliders
            foreach (Collider c in sop.Value) {
                Destroy(c.gameObject);
            }

            // Colliders are no longer disabled on pickup, instead rb.detectCollisions is set to false
            // Note that rb.detectCollisions is now set to true below.
            // foreach (Collider c in sop.Key.MyColliders) {
            //     // re-enable colliders since they were disabled during pickup
            //     c.enabled = true;
            // }

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

            rb.detectCollisions = true;
            rb.WakeUp();
        }

        // clear all now dropped objects
        heldObjects.Clear();
        yield return ActionFinished.Success;
    }

    public void SetHandSphereRadius(float radius) {
        // Magnet.transform.localScale = new Vector3(radius, radius, radius);
        magnetSphere.radius = radius;
        MagnetRenderer.transform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);
        magnetSphere.transform.localPosition = new Vector3(0, 0, 0.01f + radius);
        MagnetRenderer.transform.localPosition = new Vector3(0, 0, 0.01f + radius);
    }


}