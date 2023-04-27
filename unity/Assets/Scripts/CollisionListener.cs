using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CollisionListener : MonoBehaviour {
    public Dictionary<Collider, HashSet<Collider>> externalColliderToInternalCollisions = new Dictionary<Collider, HashSet<Collider>>();
    public List<GameObject> doNotRegisterChildrenWithin = new List<GameObject>();

    public static bool useMassThreshold = false;
    public static float massThreshold;

    public CollisionEventResolver collisionEventResolver;

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

    public void setCollisionEventResolver(CollisionEventResolver collisionEventResolver) {
        this.collisionEventResolver = collisionEventResolver;
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

    private StaticCollision ColliderToStaticCollision(Collider collider) {
        StaticCollision sc = null;
        if (!collider.isTrigger) { // only detect collisions with non-trigger colliders detected
            if (collider.GetComponentInParent<SimObjPhysics>()) {
                // how does this handle nested sim objects? maybe it's fine?
                SimObjPhysics sop = collider.GetComponentInParent<SimObjPhysics>();
                if (sop.PrimaryProperty == SimObjPrimaryProperty.Static) {
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
            else if (collisionEventResolver != null) {
                sc = collisionEventResolver.resolveToStaticCollision(collider, externalColliderToInternalCollisions[collider]);
            }


        }
        return sc;
    }

    public IEnumerable<StaticCollision> StaticCollisions(
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

    public virtual bool ShouldHalt() {
        return StaticCollisions().GetEnumerator().MoveNext();
    }

}
