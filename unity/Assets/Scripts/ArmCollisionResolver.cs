using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ArmCollisionResolver : CollisionEventResolver {

    public Collider bodyColliderIgnore;
    public GameObject bodyCollidersParent;

    // TODO: Abstract arm api so that this class doesn't need to be duplicated for ik arm
    protected Stretch_Robot_Arm_Controller arm;

    new protected void Start() {
        base.Start();
        arm = this.GetComponent<Stretch_Robot_Arm_Controller>();
        var collisionListener = this.GetComponentInParent<CollisionListener>();
    }

    public override StaticCollision resolveToStaticCollision(Collider externalCollider, HashSet<Collider> internalColliders) {
        
        if (externalCollider.transform.parent != null) {
            if (externalCollider.transform.parent.gameObject.Equals(bodyCollidersParent)) {
                // Collision with body
                Debug.Log("-------- RESOLVED COLLISION WITH BODY");
                return new StaticCollision() {
                    gameObject = externalCollider.transform.parent.gameObject
                };
            }
        }
        
        if (externalCollider.GetComponentInParent<Stretch_Robot_Arm_Controller>() != null) {
            
            if (internalColliders.Count == 1 && internalColliders.First() == bodyColliderIgnore) {
                return null;
            }
            else {
                foreach (var objectColliderSet in arm.heldObjects.Values) {

                    if (objectColliderSet.Contains(externalCollider)) {
                        // Held-Object collision with aram
                        Debug.Log("-------- RESOLVED COLLISION WITH ARm");
                        return new StaticCollision() {
                            gameObject = externalCollider.gameObject
                        };
                    }
                }
            }
        }
        return null;
    }

}