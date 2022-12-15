using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

// Allows for openable objects to be opened.
public class CanOpen_Object : MonoBehaviour {
    [Header("Moving Parts for this Object")]
    [SerializeField]
    public GameObject[] MovingParts;

    [Header("Animation Parameters")]
    [SerializeField]
    public Vector3[] openPositions;

    [SerializeField]
    public Vector3[] closedPositions;

    //[SerializeField]
    public float animationTime = 0.2f;

    public bool triggerEnabled = true;

    [SerializeField]
    public float currentOpenness = 1.0f; // 0.0 to 1.0 - percent of openPosition the object opens. 
    
    // used to reset on failure
    private float startOpenness;
    private float lastSuccessfulOpenness;
    public enum failState {none, collision, hyperextension};
    private failState failure = failState.none;
    private GameObject failureCollision;
    
    // used to store whether moving parts should treat non-static SimObjects as barriers
    private bool forceAction = false;
    private bool ignoreAgentInTransition = false;
    private bool stopAtNonStaticCol = false;

    [Header("Objects To Ignore Collision With - For Cabinets/Drawers with hinges too close together")]
    // these are objects to ignore collision with. This is in case the fridge doors touch each other or something that might
    // prevent them from closing all the way. Probably not needed but it's here if there is an edge case
    [SerializeField]
    public GameObject[] IgnoreTheseObjects;

    [Header("State information bools")]
    [SerializeField]
    public bool isOpen = false;

    private bool isCurrentlyLerping = false;
    // [SerializeField]
    //public bool isCurrentlyResetting = false;
    // private bool isCurrentlyResetting = false;

    public enum MovementType { Slide, Rotate, Scale };

    [SerializeField]
    protected MovementType movementType;

    // keep a list of all objects that, if able to turn on/off, must be in the Off state before opening (no opening microwave unless it's off!);
    private List<SimObjType> MustBeOffToOpen = new List<SimObjType>()
    {SimObjType.Microwave};

    //[Header("References for the Open or Closed bounding box for openable and pickupable objects")]
    //// the bounding box to use when this object is in the open state
    //[SerializeField]
    // protected GameObject OpenBoundingBox;
    //
    //// the bounding box to use when this object is in the closed state
    //[SerializeField]
    // protected GameObject ClosedBoundingBox;

    public List<SimObjType> WhatReceptaclesMustBeOffToOpen() {
        return MustBeOffToOpen;
    }

    // Use this for initialization
    void Start() {

#if UNITY_EDITOR
        if (!this.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen)) {
            Debug.LogError(this.name + "is missing the CanOpen Secondary Property! Please set it!");
        }
#endif

        if (!isOpen) {
            currentOpenness = 0.0f;
        }

        //// make sure correct bounding box is referenced depending on if initial state at scene start is open or closed
        //// set initial state by toggling the isOpen bool on this component, and also adjusting the rotation/position of any openable parts accordingly
        // SwitchActiveBoundingBox();
    }

    // Update is called once per frame
    void Update() {
        // test if it can open without Agent Command - Debug Purposes
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Equals)) {
            Interact();
        }
#endif
    }

    // Helper functions for setting up scenes, only for use in Editor
#if UNITY_EDITOR
    void OnEnable() {
        // debug check for missing CanOpen property
        if (!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen)) {
            Debug.LogError(gameObject.transform.name + " is missing the Secondary Property CanOpen!");
        }
    }

    public void SetMovementToSlide() {
        movementType = MovementType.Slide;
    }

    public void SetMovementToRotate() {
        movementType = MovementType.Rotate;
    }

    public void SetMovementToScale() {
        movementType = MovementType.Scale;
    }

#endif

    //sets the openness of a "rotation" based open/close object immediately without using tweening
    //specifically used for pre-scene setup
    public void SetOpennessImmediate(float openness = 1.0f) {
        for (int i = 0; i < MovingParts.Length; i++) {
            Vector3 newRot = new Vector3(openPositions[i].x, openPositions[i].y, openPositions[i].z) * openness;
            MovingParts[i].transform.localRotation = Quaternion.Euler(newRot);
        }

        setIsOpen(openness);
    }

    public void Interact(
        float targetOpenness = 1.0f,
        float? physicsInterval = null,
        bool returnToStart = true,
        bool useGripper = false,
        bool returnToStartMode = false,
        GameObject posRotManip = null,
        GameObject posRotRef = null
        ) {

        // if this object is pickupable AND it's trying to open (book, box, laptop, etc)
        // before trying to open or close, these objects must have kinematic = false otherwise it might clip through other objects
        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
        if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup && sop.isInAgentHand == false) {
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
        }

        // set physicsInterval to default of 0.02 if no value has yet been given
        physicsInterval = physicsInterval.GetValueOrDefault(Time.fixedDeltaTime);

        if (failure == failState.none) {
            // storing initial opennness-state case there's a failure, and we want to revert back to it
            startOpenness = currentOpenness;
            // storing lastSuccessfulOpenness in case opening fails on very first physics-step, and returnToStart is false
            lastSuccessfulOpenness = currentOpenness;
        }

        // okay let's open / reset the object!
        if (movementType == MovementType.Slide) {
            if (failure == failState.none || returnToStart == true) {
                 StartCoroutine(LerpPosition(
                    movingParts: MovingParts,
                    closedLocalPositions: closedPositions,
                    openLocalPositions: openPositions,
                    initialOpenness: currentOpenness,
                    desiredOpenness: targetOpenness,
                    animationTime: animationTime,
                    physicsInterval: (float)physicsInterval,
                    useGripper: useGripper,
                    returnToStartMode: returnToStartMode,
                    posRotManip: posRotManip,
                    posRotRef: posRotRef
                 ));
            } else {
                currentOpenness = lastSuccessfulOpenness;
                for (int i = 0; i < MovingParts.Length; i++) {
                    MovingParts[i].transform.localPosition = Vector3.Lerp(closedPositions[i], openPositions[i], currentOpenness);
                }
                SyncPosRot(posRotManip, posRotRef);
                
                setIsOpen(currentOpenness);
                isCurrentlyLerping = false;
            }
        } else if (movementType == MovementType.Rotate) {
            if (failure == failState.none || returnToStart == true) {
                StartCoroutine(LerpRotation(
                    movingParts: MovingParts,
                    closedLocalRotations: closedPositions,
                    openLocalRotations: openPositions,
                    initialOpenness: currentOpenness,
                    desiredOpenness: targetOpenness,
                    animationTime: animationTime,
                    physicsInterval: (float)physicsInterval,
                    useGripper: useGripper,
                    returnToStartMode: returnToStartMode,
                    posRotManip: posRotManip,
                    posRotRef: posRotRef
                ));
            } else {
                currentOpenness = lastSuccessfulOpenness;
                for (int i = 0; i < MovingParts.Length; i++) {
                    MovingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(closedPositions[i]), Quaternion.Euler(openPositions[i]), currentOpenness);
                }
                SyncPosRot(posRotManip, posRotRef);

                setIsOpen(currentOpenness);
                isCurrentlyLerping = false;
            }
        } else if (movementType == MovementType.Scale) {
            if (failure == failState.none || returnToStart == true) {
                StartCoroutine(LerpScale(
                    movingParts: MovingParts,
                    closedLocalScales: closedPositions,
                    openLocalScales: openPositions,
                    initialOpenness: currentOpenness,
                    desiredOpenness: targetOpenness,
                    animationTime: animationTime,
                    physicsInterval: (float)physicsInterval,
                    useGripper: useGripper,
                    returnToStartMode: returnToStartMode,
                    posRotManip: posRotManip,
                    posRotRef: posRotRef
                ));
            } else {
                currentOpenness = lastSuccessfulOpenness;
                for (int i = 0; i < MovingParts.Length; i++) {
                    MovingParts[i].transform.localScale = Vector3.Lerp(closedPositions[i], openPositions[i], currentOpenness);
                }
                SyncPosRot(posRotManip, posRotRef);

                setIsOpen(currentOpenness);
                isCurrentlyLerping = false;
            }
        }
    }

    private protected IEnumerator LerpPosition(
        GameObject[] movingParts,
        Vector3[] closedLocalPositions,
        Vector3[] openLocalPositions,
        float initialOpenness,
        float desiredOpenness,
        float animationTime,
        float physicsInterval,
        bool useGripper,
        bool returnToStartMode,
        GameObject posRotManip,
        GameObject posRotRef
    ) {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime && (failure == failState.none || returnToStartMode == true))
        {
            lastSuccessfulOpenness = currentOpenness;
            elapsedTime += physicsInterval;
            currentOpenness = Mathf.Clamp(
                initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime),
                Mathf.Min(initialOpenness, desiredOpenness),
                Mathf.Max(initialOpenness, desiredOpenness));

            for (int i = 0; i < movingParts.Length; i++) {
                movingParts[i].transform.localPosition = Vector3.Lerp(closedLocalPositions[i], openLocalPositions[i], currentOpenness);
            }

            if (useGripper == true) {
                SyncPosRot(posRotManip, posRotRef);
            }

            stepOpen(physicsInterval, useGripper, elapsedTime);
#if UNITY_EDITOR
            yield return null;
#endif
        }

        setIsOpen(currentOpenness);
        isCurrentlyLerping = false;
        yield break;
    }

    private protected IEnumerator LerpRotation(
        GameObject[] movingParts,
        Vector3[] closedLocalRotations,
        Vector3[] openLocalRotations,
        float initialOpenness,
        float desiredOpenness,
        float animationTime,
        float physicsInterval,
        bool useGripper,
        bool returnToStartMode,
        GameObject posRotManip,
        GameObject posRotRef
    ) {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime && (failure == failState.none || returnToStartMode == true))
        {
            lastSuccessfulOpenness = currentOpenness;
            elapsedTime += physicsInterval;
            currentOpenness = Mathf.Clamp(
                initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime),
                Mathf.Min(initialOpenness, desiredOpenness),
                Mathf.Max(initialOpenness, desiredOpenness));

            for (int i = 0; i < movingParts.Length; i++) {
                movingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(closedLocalRotations[i]), Quaternion.Euler(openLocalRotations[i]), currentOpenness);
            }

            if (useGripper == true) {
                SyncPosRot(posRotManip, posRotRef);
            }

#if UNITY_EDITOR
            yield return null;
#endif
            stepOpen(physicsInterval, useGripper, elapsedTime);
        }

        setIsOpen(currentOpenness);
        isCurrentlyLerping = false;
        yield break;
    }

    private protected IEnumerator LerpScale(
        GameObject[] movingParts,
        Vector3[] closedLocalScales,
        Vector3[] openLocalScales,
        float initialOpenness,
        float desiredOpenness,
        float animationTime,
        float physicsInterval,
        bool useGripper,
        bool returnToStartMode,
        GameObject posRotManip,
        GameObject posRotRef
    ) {
        float elapsedTime = 0f;

        while (elapsedTime < animationTime && (failure == failState.none || returnToStartMode == true))
        {
            lastSuccessfulOpenness = currentOpenness;
            elapsedTime += physicsInterval;
            currentOpenness = Mathf.Clamp(
                initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime),
                Mathf.Min(initialOpenness, desiredOpenness),
                Mathf.Max(initialOpenness, desiredOpenness));

            for (int i = 0; i < movingParts.Length; i++) {
                movingParts[i].transform.localScale = Vector3.Lerp(closedLocalScales[i], openLocalScales[i], currentOpenness);
            }

            if (useGripper == true) {
                SyncPosRot(posRotManip, posRotRef);
            }

            stepOpen(physicsInterval, useGripper, elapsedTime);
#if UNITY_EDITOR
            yield return null;
#endif
        }

        setIsOpen(currentOpenness);
        isCurrentlyLerping = false;
        yield break;
    }

    private void setIsOpen(float openness) {
        isOpen = openness != 0;
        currentOpenness = openness;
    }

    public bool GetisOpen() {
        return isOpen;
    }

    public void stepOpen(
        float physicsInterval,
        bool useGripper,
        float elapsedTime) {
        if (Physics.autoSimulation != true) {
            PhysicsSceneManager.PhysicsSimulateTHOR(physicsInterval);
            Physics.SyncTransforms();
        }

        // failure check (The OnTriggerEnter collision check is listening at all times,
        // but this hyperextension check must be called manually)
        if (useGripper == true && forceAction == false) {
            FK_IK_Solver armBase = GameObject.Find("FPSController").GetComponent<BaseAgentComponent>().IKArm.GetComponent<IK_Robot_Arm_Controller>().GetArmBase().GetComponent<FK_IK_Solver>();
            if ((armBase.IKTarget.position - armBase.armShoulder.position).magnitude + 1e-5 >= armBase.bone2Length + armBase.bone3Length) {
                failure = failState.hyperextension;
#if UNITY_EDITOR
        Debug.Log("Agent-arm hyperextended at " + elapsedTime + ". Resetting openness.");
#endif
            }
        }
    }

    public void OnTriggerEnter(Collider other) {
        // If the openable object is meant to ignore trigger collisions entirely, then ignore
        if (!triggerEnabled) {
            // Debug.Log("I'm supposed to ignore triggers!, Bye, " + other);
            return;
        }

        // If the openable object is not opening or closing, then ignore
        if (!isCurrentlyLerping) {
            // Debug.Log("I'm not currently lerping! Bye, " + other);
            return;
        }

        // If forceAction is enabled, then ignore
        if (forceAction == true) {
            // Debug.Log("All checks are off when forceAction is true!");
            return;
        }

        // If the overlapping collider is a (non-physical) trigger collider, then ignore
        if (other.isTrigger == true) {
            // Debug.Log(other + "is a trigger, so bye!");
            return;
        }

        // If the overlapping collider is a child of one of the gameobjects
        // that it's been explicitly told to disregard, then ignore
        if (IsInIgnoreArray(other, IgnoreTheseObjects)) {
            // Debug.Log(other + " is in ignore array");
            return;
        }

        // If the collider is a BoundingBox or ReceptacleTriggerBox, then ignore
        if (other.gameObject.layer ==  LayerMask.NameToLayer ("SimObjectInvisible")) {
            // Debug.Log(other + " is bounding box or receptacle trigger box");
            return;
        }

        // If the overlapping collider is a descendant of the openable GameObject itself (or its parent), then ignore
        if (hasAncestor(other.transform.gameObject, gameObject)) {
            // Debug.Log(other + " belongs to me!");
            return;
        }

        // If the overlapping collider is a descendant of the agent when ignoreAgentInTransition is true, then ignore
        if (ignoreAgentInTransition == true && hasAncestor(other.transform.gameObject, GameObject.Find("FPSController"))) {
            // Debug.Log(other + " belongs to agent, and ignoreAgentInTransition is active!");
            return;
        }

        // If the overlapping collider belongs to a non-static SimObject, then ignore
        // (Unless we explicitly tell the action to treat non-static SimObjects as barriers)
        if (ancestorSimObjPhysics(other.gameObject) != null &&
            ancestorSimObjPhysics(other.gameObject).PrimaryProperty != SimObjPrimaryProperty.Static &&
            stopAtNonStaticCol == false) {
            // Debug.Log("Ignore nonstatics " + other);
            return;
        }

        // All right, so it was a legit collision? RESET!
        failure = failState.collision;
        if (ancestorSimObjPhysics(other.gameObject) != null) {
            failureCollision = other.GetComponentInParent<SimObjPhysics>().gameObject;
        }
        else {
            failureCollision = other.gameObject;
        }
#if UNITY_EDITOR
        Debug.Log(gameObject.name + " hit " + failureCollision + ". Resetting openness.");
#endif
    }

    // for use in OnTriggerEnter ignore check
    // return true if it should ignore the object hit. Return false to cause this object to reset to the original position
    public bool IsInIgnoreArray(Collider other, GameObject[] ignoredObjects) {
        foreach (GameObject ignoredObject in ignoredObjects) {
            foreach (Collider ignoredCollider in ignoredObject.GetComponentsInChildren<Collider>())
                if (other == ignoredCollider) {
                    return true;
                }
        }
    return false;
    }

    private bool hasAncestor(GameObject child, GameObject potentialAncestor) {
        if (child == potentialAncestor) {
            return true;
        } else if (child.transform.parent != null) {
            return hasAncestor(child.transform.parent.gameObject, potentialAncestor);
        } else {
            return false;
        }
    }

    private static SimObjPhysics ancestorSimObjPhysics(GameObject go) {
        if (go == null) {
            return null;
        }
        SimObjPhysics so = go.GetComponent<SimObjPhysics>();
        if (so != null) {
            return so;
        } else if (go.transform.parent != null) {
            return ancestorSimObjPhysics(go.transform.parent.gameObject);
        } else {
            return null;
        }
    }

    public MovementType GetMovementType() {
        return movementType;
    }
    public float GetStartOpenness() {
        return startOpenness;
    }
    public void SetFailState(failState failState) {
        failure = failState;
    }
    public failState GetFailState() {
        return failure;
    }
    public void SetFailureCollision(GameObject collision) {
        failureCollision = collision;
    }
    public GameObject GetFailureCollision() {
        return failureCollision;
    }
    public void SetForceAction(bool forceAction) {
        this.forceAction = forceAction;
    }
    public void SetIgnoreAgentInTransition(bool ignoreAgentInTransition) {
        this.ignoreAgentInTransition = ignoreAgentInTransition;
    }
    public void SetStopAtNonStaticCol(bool stopAtNonStaticCol) {
        this.stopAtNonStaticCol = stopAtNonStaticCol;
    }
    public bool GetIsCurrentlyLerping() {
        if (this.isCurrentlyLerping) {
            return true;
        }
        else {
            return false;
        }
    }
    public void SetIsCurrentlyLerping(bool isCurrentlyLerping) {
        this.isCurrentlyLerping = isCurrentlyLerping;
    }
    public void SyncPosRot(GameObject child, GameObject parent) {
            child.transform.position = parent.transform.position;
            child.transform.rotation = parent.transform.rotation;
    }
}