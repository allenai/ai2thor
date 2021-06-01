using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhatIsInsideMagnetSphere : MonoBehaviour {

    [SerializeField] protected List<string> CurrentlyContainedObjectIds = new List<string>();
    [SerializeField] protected List<SimObjPhysics> CurrentlyContainedSOP = new List<SimObjPhysics>();
    SphereCollider sphereCol = null;

    private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
        SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Static}); // should we ignore SimObjPrimaryProperty.Static?

    // check if the sphere is actively colliding with anything
    // public bool isColliding;
    // Start is called before the first frame update
    void Start() {
        if (sphereCol == null) {
            sphereCol = gameObject.GetComponent<SphereCollider>();
        }
    }

    // Update is called once per frame
    void Update() {

    }

    public void GenerateCurrentlyContained() {
        // clear lists of contained objects
        CurrentlyContainedObjectIds.Clear();
        CurrentlyContainedSOP.Clear();

        // create overlap sphere same location and dimensions as sphere collider
        var center = transform.TransformPoint(sphereCol.center);
        var radius = sphereCol.radius;

        Collider[] hitColliders = Physics.OverlapSphere(center, radius, 1 << 8, QueryTriggerInteraction.Ignore);
        foreach (var col in hitColliders) {
            if (col.GetComponentInParent<SimObjPhysics>()) {
                SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();

                // ignore any sim objects that shouldn't be added to the CurrentlyContains list
                if (PropertiesToIgnore.Contains(sop.PrimaryProperty)) {
                    return;
                }

                // populate list of sim objects inside sphere by objectID
                if (!CurrentlyContainedObjectIds.Contains(sop.objectID)) {
                    if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                        CurrentlyContainedObjectIds.Add(sop.objectID);
                    }
                }

                // populate list of sim objects inside sphere by object reference
                if (!CurrentlyContainedSOP.Contains(sop)) {
                    if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                        CurrentlyContainedSOP.Add(sop);
                    }
                }
            }
        }
    }

    // report back what is currently inside this receptacle as a list of sim object references
    public List<SimObjPhysics> CurrentlyContainedSimObjects() {
        GenerateCurrentlyContained();
        return CurrentlyContainedSOP;
    }

    // report back what is currently inside this receptacle as a list of object ids of sim objects
    public List<string> CurrentlyContainedSimObjectsByID() {
        GenerateCurrentlyContained();
        return CurrentlyContainedObjectIds;
    }

}
