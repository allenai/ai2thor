using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityStandardAssets.Characters.FirstPerson;

public class CollisionListener : MonoBehaviour {
    // references to the joints of the mid level arm
    [SerializeField]
    private bool CascadeCollisionEventsToParent = true;
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

    private HashSet<Collider> activeColliders = new HashSet<Collider>();
    public static bool useMassThreshold = false;

    public static float massThreshold;

    public void RegisterCollision(Collider col, bool notifyParent = true) {
        activeColliders.Add(col);
        if (notifyParent && this.transform.parent != null) {
            foreach (var listener in this.transform.parent.GetComponentsInParent<CollisionListener>()) {
                listener.RegisterCollision(col, notifyParent);
            }
        }
    }

    public void DeregisterCollision(Collider col, bool notifyParent = true) {
        activeColliders.Remove(col);
        if (notifyParent && this.transform.parent != null) {
            foreach (var listener in this.transform.parent.GetComponentsInParent<CollisionListener>()) {
                listener.DeregisterCollision(col, notifyParent);
            }
        }
    }

    public void Reset(bool? notifyParent = null) {
        activeColliders.Clear();
        if (notifyParent.GetValueOrDefault(this.CascadeCollisionEventsToParent) && this.transform.parent != null) {
            foreach (var listener in this.transform.parent.GetComponentsInParent<CollisionListener>()) {
                listener.Reset(notifyParent);
            }
        }
    }

    public void OnTriggerExit(Collider col) {
        DeregisterCollision(col, CascadeCollisionEventsToParent);
    }

    public void OnTriggerStay(Collider col) {
#if UNITY_EDITOR
        if (!activeColliders.Contains(col)) {
            if (col.gameObject.name == "StandardIslandHeight" || col.gameObject.name == "Sphere") {
                Debug.Log("got collision stay with " + col.gameObject.name + " this" + this.gameObject.name);
            }
        }
#endif
        this.
        RegisterCollision(col, CascadeCollisionEventsToParent);
    }

    public void OnTriggerEnter(Collider col) {
#if UNITY_EDITOR
        if (!activeColliders.Contains(col)) {
            if (col.gameObject.name == "StandardIslandHeight" || col.gameObject.name == "Sphere") {
                Debug.Log("got collision enter with " + col.gameObject.name + " this" + this.gameObject.name);
            }
        }
#endif
        // Debug.Log("got collision with " + col.gameObject.name + " this" + this.gameObject.name);
        RegisterCollision(col, CascadeCollisionEventsToParent);
    }

    private static StaticCollision ColliderToStaticCollision(Collider col) {
        StaticCollision sc = null;
        if (col.GetComponentInParent<SimObjPhysics>()) {
            // only detect collisions with non-trigger colliders detected
            if (!col.isTrigger) {
                // how does this handle nested sim objects? maybe it's fine?
                SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();
                if (sop.PrimaryProperty == SimObjPrimaryProperty.Static) {


                    // #if UNITY_EDITOR
                    // Debug.Log("Collided with static sim obj " + sop.name);
                    // #endif
                    sc = new StaticCollision();
                    sc.simObjPhysics = sop;
                    sc.gameObject = col.gameObject;

                }

                // if instead it is a moveable or pickupable sim object
                else if (useMassThreshold) {
                    // if a moveable or pickupable object is too heavy for the arm to move
                    // flag it as a static collision so the arm will stop
                    if (sop.Mass > massThreshold) {
                        sc = new StaticCollision();
                        sc.simObjPhysics = sop;
                        sc.gameObject = col.gameObject;
                    }
                }
            }
        }

            // also check if the collider hit was a structure?
            else if (col.gameObject.CompareTag("Structure")) {
            // only detect collisions with non-trigger colliders detected
            if (!col.isTrigger) {
                sc = new StaticCollision();
                sc.gameObject = col.gameObject;
            }
        }
        return sc;
    }

    public static List<StaticCollision> StaticCollisions(IEnumerable<Collider> colliders) {
        var staticCols = new List<StaticCollision>();
        foreach (var col in colliders) {
            var staticCollision = ColliderToStaticCollision(col);
            if (staticCollision != null) {
                staticCols.Add(staticCollision);
            }
        }
        return staticCols;
    }

    public List<StaticCollision> StaticCollisions() {
        return StaticCollisions(this.activeColliders);
    }

    public bool ShouldHalt() {
        return StaticCollisions().Count > 0;
    }

}
