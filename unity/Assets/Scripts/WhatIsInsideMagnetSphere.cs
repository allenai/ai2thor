using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WhatIsInsideMagnetSphere : MonoBehaviour {

    [SerializeField] protected List<SimObjPhysics> CurrentlyContainedSOP = new List<SimObjPhysics>();
    SphereCollider sphereCol = null;

    // check if the sphere is actively colliding with anything
    // public bool isColliding;
    // Start is called before the first frame update
    void Start() {
        if (sphereCol == null) {
            sphereCol = gameObject.GetComponent<SphereCollider>();
        }
    }

    public List<SimObjPhysics> CurrentlyContainedSimObjects(bool onlyPickupable) {
        // clear lists of contained objects

        // create overlap sphere same location and dimensions as sphere collider
        var center = transform.TransformPoint(sphereCol.center);
        var radius = sphereCol.radius;

        HashSet<SimObjPhysics> currentlyContainedObjects = new HashSet<SimObjPhysics>();
        Collider[] hitColliders = Physics.OverlapSphere(
            position: center,
            radius: radius,
            layerMask: LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"),
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );
        foreach (var col in hitColliders) {
            SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();
            if (sop != null) {
                if ((!onlyPickupable) || sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                    currentlyContainedObjects.Add(sop);
                }
            }
        }
        List<SimObjPhysics> toReturn = currentlyContainedObjects.ToList();
        toReturn.Sort(
            (a, b) => a.ObjectID.CompareTo(b.ObjectID)
        );
        return toReturn;
    }


}
