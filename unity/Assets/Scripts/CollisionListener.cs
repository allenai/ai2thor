using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityStandardAssets.Characters.FirstPerson;

public class CollisionListener : MonoBehaviour {
    public Dictionary<Collider, HashSet<Collider>> externalColliderToInternalCollisions = new Dictionary<Collider, HashSet<Collider>>();
    public List<GameObject> doNotRegisterChildrenWithin = new List<GameObject>();
    public bool deadZoneCheck;
    public static bool useMassThreshold = false;
    public static float massThreshold;


    private HashSet<CollisionListenerChild> collisionListenerChildren = new HashSet<CollisionListenerChild>();

    void Start() {
        registerAllChildColliders();
        foreach (CollisionListener cl in this.GetComponentsInChildren<CollisionListener>()) {
            if (cl.gameObject != this.gameObject) {
#if UNITY_EDITOR
                Debug.Log($"Offending CollisionListener: {cl.gameObject} a descendent of {this.gameObject}");
#endif
                throw new InvalidOperationException(
                    "A CollisionListener should not be included as a component on a descendent of a GameObject that already has a CollisionListener component."
                );
            }
        }
    }

    public void registerAllChildColliders() {
        HashSet<Collider> ignoredColliders = new HashSet<Collider>();
        foreach (GameObject go in doNotRegisterChildrenWithin) {
            foreach (Collider c in go.GetComponentsInChildren<Collider>()) {
                ignoredColliders.Add(c);
            }
        }

        foreach (CollisionListenerChild clc in collisionListenerChildren) {
            clc.parent = null;
        }
        collisionListenerChildren.Clear();

        foreach (Collider c in this.GetComponentsInChildren<Collider>()) {
            if (!ignoredColliders.Contains(c)) {
                registerChild(c);
            }
        }
    }

    public void registerChild(Collider c) {
        if (c.enabled && c.isTrigger) {
            CollisionListenerChild clc = c.gameObject.GetComponent<CollisionListenerChild>();
            if (clc == null) {
                clc = c.gameObject.AddComponent<CollisionListenerChild>();
            }
            registerChild(clc);
        }
    }

    public void registerChild(CollisionListenerChild clc) {
        clc.parent = this;
        collisionListenerChildren.Add(clc);
    }

    public void deregisterChild(Collider c) {
        deregisterChild(c.gameObject.GetComponent<CollisionListenerChild>());
    }

    public void deregisterChild(CollisionListenerChild clc) {
        if (clc != null) {
            clc.parent = null;
            collisionListenerChildren.Remove(clc);
        }
    }

    // track what was hit while arm was moving
    public class StaticCollision {
        public GameObject gameObject;

        public SimObjPhysics simObjPhysics;

        // indicates if gameObject a simObject
        public bool isSimObj {
            get { return simObjPhysics != null; }
        }

        public string name {
            get {
                if (this.isSimObj) {
                    return this.simObjPhysics.name;
                } else {
                    return this.gameObject.name;
                }
            }
        }
    }

    public void RegisterCollision(Collider internalCollider, Collider externalCollider) {
        if (!externalColliderToInternalCollisions.ContainsKey(externalCollider)) {
            externalColliderToInternalCollisions[externalCollider] = new HashSet<Collider>();
        }
        externalColliderToInternalCollisions[externalCollider].Add(internalCollider);
    }

    public void DeregisterCollision(Collider internalCollider, Collider externalCollider) {
        if (externalColliderToInternalCollisions.ContainsKey(externalCollider)) {
            externalColliderToInternalCollisions[externalCollider].Remove(internalCollider);

            if (externalColliderToInternalCollisions[externalCollider].Count == 0) {
                externalColliderToInternalCollisions.Remove(externalCollider);
            }
        }
    }

    public void Reset() {
        externalColliderToInternalCollisions.Clear();
        deadZoneCheck = false;
    }

    private static bool debugCheckIfCollisionIsNonTrivial(
        Collider internalCollider, Collider externalCollider
    ) {
        Vector3 direction;
        float distance = 0f;
        Physics.ComputePenetration(
            internalCollider,
            internalCollider.transform.position,
            internalCollider.transform.rotation,
            externalCollider,
            externalCollider.transform.position,
            externalCollider.transform.rotation,
            out direction,
            out distance
        );
        Debug.Log($"Us: {internalCollider.transform.gameObject}, Them: {externalCollider.gameObject}");
        Debug.Log($"Distance: {distance}, direction: {direction.ToString("F4")}");
        return distance > 0.001f;
    }

    private static StaticCollision ColliderToStaticCollision(Collider collider) {
        StaticCollision sc = null;
        if (!collider.isTrigger) { // only detect collisions with non-trigger colliders detected
            if (collider.GetComponentInParent<SimObjPhysics>()) {

                // how does this handle nested sim objects? maybe it's fine?
                SimObjPhysics sop = collider.GetComponentInParent<SimObjPhysics>();
                if (sop.PrimaryProperty == SimObjPrimaryProperty.Static || sop.GetComponent<Rigidbody>().isKinematic == true) {
                    // #if UNITY_EDITOR
                    // Debug.Log("Collided with static sim obj " + sop.name);
                    // #endif
                    sc = new StaticCollision();
                    sc.simObjPhysics = sop;
                    sc.gameObject = collider.gameObject;

                } else if (useMassThreshold) {
                    // if a moveable or pickupable object is too heavy for the arm to move
                    // flag it as a static collision so the arm will stop
                    if (sop.Mass > massThreshold) {
                        sc = new StaticCollision();
                        sc.simObjPhysics = sop;
                        sc.gameObject = collider.gameObject;
                    }
                }
            } else if (collider.gameObject.CompareTag("Structure")) {
                sc = new StaticCollision();
                sc.gameObject = collider.gameObject;
            }
        }
        return sc;
    }

    public static IEnumerable<StaticCollision> StaticCollisions(
        IEnumerable<Collider> colliders
    ) {
        foreach (Collider c in colliders) {
            var staticCollision = ColliderToStaticCollision(collider: c);
            if (staticCollision != null) {
                yield return staticCollision;
            }
        }
    }

    public IEnumerable<StaticCollision> StaticCollisions() {
        return StaticCollisions(this.externalColliderToInternalCollisions.Keys);
    }

    public void enableDeadZoneCheck() {
        deadZoneCheck = true;
    }

    public bool TransformChecks(PhysicsRemoteFPSAgentController controller, Transform objectTarget) {
        // this action is specifically for a stretch wrist-rotation with limits
        if (deadZoneCheck) {
            float currentYaw = objectTarget.rotation.eulerAngles.y;
            float cLimit = controller.gameObject.GetComponentInChildren<Stretch_Robot_Arm_Controller>().wristClockwiseLocalRotationLimit;
            float ccLimit = controller.gameObject.GetComponentInChildren<Stretch_Robot_Arm_Controller>().wristCounterClockwiseLocalRotationLimit;
            
            // Consolidate reachable euler-rotations (which are normally bounded by [0, 360)) into a continuous number line,
            // bounded instead by [continuousCounterClockwiseLocalRotationLimit, continuousClockwiseLocalRotationLimit + 360)
            if (cLimit < ccLimit) {
                cLimit += 360;
                if (currentYaw < ccLimit) {
                    currentYaw += 360;
                }
            }

            if (currentYaw < ccLimit || currentYaw > cLimit) {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    public virtual bool ShouldHalt() {
        return StaticCollisions().GetEnumerator().MoveNext();
    }

}