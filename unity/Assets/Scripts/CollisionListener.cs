using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityStandardAssets.Characters.FirstPerson;

public class CollisionListener : MonoBehaviour {
    public Dictionary<Collider, HashSet<Collider>> activeColliders = new Dictionary<Collider, HashSet<Collider>>();
    public List<GameObject> doNotRegisterChildrenWithin = new List<GameObject>();

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

    public void RegisterCollision(Collider us, Collider them) {
        if (!activeColliders.ContainsKey(them)) {
            activeColliders[them] = new HashSet<Collider>();
        }
        activeColliders[them].Add(us);
    }

    public void DeregisterCollision(Collider us, Collider them) {
        if (activeColliders.ContainsKey(them)) {
            activeColliders[them].Remove(us);

            if (activeColliders[them].Count == 0) {
                activeColliders.Remove(them);
            }
        }
    }

    public void Reset() {
        activeColliders.Clear();
    }

    private static bool debugCheckIfCollisionIsNonTrivial(Collider us, Collider them) {
        Vector3 direction = new Vector3();
        float distance = 0f;
        Physics.ComputePenetration(
            us,
            us.transform.position,
            us.transform.rotation,
            them,
            them.transform.position,
            them.transform.rotation,
            out direction,
            out distance
        );
        Debug.Log($"Us: {us.transform.gameObject}, Them: {them.gameObject}");
        Debug.Log($"Distance: {distance}, direction: {direction.ToString("F4")}");
        return distance > 0.001f;
    }

    private static StaticCollision ColliderToStaticCollision(Collider us, Collider them) {
        StaticCollision sc = null;
        if (!them.isTrigger) { // only detect collisions with non-trigger colliders detected
            if (them.GetComponentInParent<SimObjPhysics>()) {

                // how does this handle nested sim objects? maybe it's fine?
                SimObjPhysics sop = them.GetComponentInParent<SimObjPhysics>();
                if (sop.PrimaryProperty == SimObjPrimaryProperty.Static) {
                    // #if UNITY_EDITOR
                    // Debug.Log("Collided with static sim obj " + sop.name);
                    // #endif
                    sc = new StaticCollision();
                    sc.simObjPhysics = sop;
                    sc.gameObject = them.gameObject;

                } else if (useMassThreshold) {
                    // if a moveable or pickupable object is too heavy for the arm to move
                    // flag it as a static collision so the arm will stop
                    if (sop.Mass > massThreshold) {
                        sc = new StaticCollision();
                        sc.simObjPhysics = sop;
                        sc.gameObject = them.gameObject;
                    }
                }
            } else if (them.gameObject.CompareTag("Structure")) {
                sc = new StaticCollision();
                sc.gameObject = them.gameObject;
            }
        }
        return sc;
    }

    public static IEnumerable<StaticCollision> StaticCollisions(
        Dictionary<Collider, HashSet<Collider>> themToUsSet
    ) {
        foreach (var themToUsKvp in themToUsSet) {
            foreach (Collider us in themToUsKvp.Value) {
                var staticCollision = ColliderToStaticCollision(us: us, them: themToUsKvp.Key);
                if (staticCollision != null) {
                    yield return staticCollision;
                }
            }
        }
    }

    public IEnumerable<StaticCollision> StaticCollisions() {
        return StaticCollisions(this.activeColliders);
    }

    public bool ShouldHalt() {
        return StaticCollisions().GetEnumerator().MoveNext();
    }

}
