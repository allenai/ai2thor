using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UsedUp : MonoBehaviour {
    [SerializeField]
    protected MeshRenderer usedUpRenderer;

    [SerializeField]
    protected Collider[] usedUpColliders;

    [SerializeField]
    protected Collider[] usedUpTriggerColliders;

    [SerializeField]
    protected Collider[] alwaysActiveColliders;

    [SerializeField]
    protected Collider[] alwaysActiveTriggerColliders;

    public bool isUsedUp = false;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void UseUp() {
        usedUpRenderer.enabled = false;

        // disable all colliders that are used up
        foreach (Collider col in usedUpColliders) {
            col.enabled = false;
        }

        // disable all trigger colliders that are used up
        foreach (Collider col in usedUpTriggerColliders) {
            col.enabled = false;
        }

        // reference to SimObjPhysics component to 
        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();

        // set colliders to ones active while used up
        sop.MyColliders = alwaysActiveColliders;

        // set trigger colliders to ones active while used up

        isUsedUp = true;
    }
}
