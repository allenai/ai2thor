using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CollisionEventResolver : MonoBehaviour {
    protected void Start() {
        var listener = this.transform.GetComponentInParent<CollisionListener>();
        if (listener != null) {
            listener.setCollisionEventResolver(this);
        }
    }

    public abstract StaticCollision resolveToStaticCollision(
        Collider externalCollider,
        HashSet<Collider> internalColliders
    );
}

// Class to track what was hit while arm was moving
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
