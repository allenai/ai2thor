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
    private float? startFixedDeltaTime;
    private bool startUseGripper;

    // used to report back reason for failure
    public enum failState {none, collision, hyperextension};
    private failState failure = failState.none;
    private GameObject failureCollision;
    
    // used to store whether moving parts should treat non-static SimObjects as barriers
    private bool stopsAtNonStaticCol = false;

    [Header("Objects To Ignore Collision With - For Cabinets/Drawers with hinges too close together")]
    // these are objects to ignore collision with. This is in case the fridge doors touch each other or something that might
    // prevent them from closing all the way. Probably not needed but it's here if there is an edge case
    [SerializeField]
    public GameObject[] IgnoreTheseObjects;

    [Header("State information bools")]
    [SerializeField]
    public bool isOpen = false;

    [SerializeField]
    public bool isCurrentlyResetting = false;

    protected enum MovementType { Slide, Rotate, ScaleX, ScaleY, ScaleZ };

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

    void Awake() {
        if (MovingParts != null) {
            // init Itween in all doors to prep for animation
            foreach (GameObject go in MovingParts) {
                // Init is getting called in Awake() vs Start() so that cloned gameobjects can add MovingParts
                // before iTween.Awake() gets called which would throw an error if this was done in Start()
                iTween.Init(go);

                // check to make sure all doors have a Fridge_Door.cs script on them, if not throw a warning
                // if (!go.GetComponent<Fridge_Door>())
                // Debug.Log("Fridge Door is missing Fridge_Door.cs component! OH NO!");
            }
        }
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

    public void SetMovementToScaleX() {
        movementType = MovementType.ScaleX;
    }

    public void SetMovementToScaleY() {
        movementType = MovementType.ScaleY;
    }

    public void SetMovementToScaleZ() {
        movementType = MovementType.ScaleZ;
    }
#endif

    //sets the openness of a "rotation" based open/close object immediately without using tweening
    //specifically used for pre-scene setup
    public void SetOpennessImmediate(float openness = 1.0f) {
        for (int i = 0; i < MovingParts.Length; i++) {
            Vector3 newRot = new Vector3(openPositions[i].x, openPositions[i].y, openPositions[i].z) * openness;
            MovingParts[i].transform.localRotation = Quaternion.Euler(newRot);
        }

        setIsOpen(openness: openness);
    }

    public void Interact(
        float openness = 1.0f,
        float? physicsInterval = null,
        bool useGripper = false
        ) {

        // if this object is pickupable AND it's trying to open (book, box, laptop, etc)
        // before trying to open or close, these objects must have kinematic = false otherwise it might clip through other objects
        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
        if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup && sop.isInAgentHand == false) {
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
        }

        // Just in case there's a failure, we can undo it
        // (There is no need for these precautionary setups during a reset,
        // since the object has already initiated an iTween operation successfully)
        if (isCurrentlyResetting == false) {
            // set physicsInterval to default of 0.02 if no value has yet been given
            physicsInterval = physicsInterval.GetValueOrDefault(0.02f);

            startOpenness = currentOpenness;
            startFixedDeltaTime = physicsInterval;
            startUseGripper = useGripper;
        }

        // For every moving part (some doors are doubles, for example...)
        for (int i = 0; i < MovingParts.Length; i++) {

            // parameters for onUpdate and onComplete methods
            Hashtable parameters = new Hashtable() {
                {"useGripper", useGripper},
                {"physicsInterval", physicsInterval},
                {"armBase", GameObject.Find("FPSController").GetComponent<BaseAgentComponent>().IKArm.GetComponent<IK_Robot_Arm_Controller>().GetArmBase().GetComponent<FK_IK_Solver>()}
            };

            // simple parameters used as input for iTween logic;
            // local means local-space instead of world-space,
            // animationTime means the number of seconds lerping should take,
            // and linear means linear animation handles instead of, say, bezier
            Hashtable args = new Hashtable() {
                {"islocal", true},
                {"time", animationTime},
                {"easetype", "linear"},
                {"onupdate", "stepOpen"},
                {"onupdatetarget", this.gameObject},
                {"onupdateparams", parameters},
                {"oncomplete", "stepOpen"},
                {"oncompletetarget", this.gameObject},
                {"oncompleteparams", parameters}
            };
            // let's open / reset the object!
            if (movementType == MovementType.Rotate) {
                args["rotation"] = openPositions[i] * openness;
                iTween.RotateTo(MovingParts[i], args);
            } else if (movementType == MovementType.Slide) {
                // this is used to determine which components of openPosition need to be scaled
                // default to open position without percentage open modifiers
                Vector3 lerpToPosition = openPositions[i];

                // some x, y, z components don't change when sliding open
                // only apply openness modifier to components of vector3 that actually change
                if (Mathf.Abs(openPositions[i].x - closedPositions[i].x) > Mathf.Epsilon) {
                    lerpToPosition.x = ((openPositions[i].x - closedPositions[i].x) * openness) + closedPositions[i].x;
                }
                if (Mathf.Abs(openPositions[i].y - closedPositions[i].y) > Mathf.Epsilon) {
                    lerpToPosition.y = ((openPositions[i].y - closedPositions[i].y) * openness) + closedPositions[i].y;
                }
                if (Mathf.Abs(openPositions[i].z - closedPositions[i].z) > Mathf.Epsilon) {
                    lerpToPosition.z = ((openPositions[i].z - closedPositions[i].z) * openness) + closedPositions[i].z;
                }
                args["position"] = lerpToPosition;
                iTween.MoveTo(MovingParts[i], args);
            } else if (movementType == MovementType.ScaleY) {
                args["scale"] = new Vector3(openPositions[i].x, closedPositions[i].y + (openPositions[i].y - closedPositions[i].y) * openness, openPositions[i].z);
                iTween.ScaleTo(MovingParts[i], args);
            } else if (movementType == MovementType.ScaleX) {
                // we are on the last loop here
                args["scale"] = new Vector3(closedPositions[i].x + (openPositions[i].x - closedPositions[i].x) * openness, openPositions[i].y, openPositions[i].z);
                iTween.ScaleTo(MovingParts[i], args);
            } else if (movementType == MovementType.ScaleZ) {
                args["scale"] = new Vector3(openPositions[i].x, openPositions[i].y, closedPositions[i].z + (openPositions[i].z - closedPositions[i].z) * openness);
                iTween.ScaleTo(MovingParts[i], args);
            }
        }
        setIsOpen(openness: openness);
    }

    private void setIsOpen(float openness) {
        isOpen = openness != 0;
        currentOpenness = openness;
    }

    public bool GetisOpen() {
        return isOpen;
    }

    public void stepOpen(Hashtable parameters) {
        if (Physics.autoSimulation != true) {
            PhysicsSceneManager.PhysicsSimulateTHOR((float)parameters["physicsInterval"]);
            Physics.SyncTransforms();
        }

        // arm hyperextension check (unnecessary if arm is already resetting back to its start-state)
        if ((bool)parameters["useGripper"] == true && isCurrentlyResetting == false) {
            FK_IK_Solver armBase = (FK_IK_Solver)parameters["armBase"];
            if ((armBase.IKTarget.position - armBase.armShoulder.position).sqrMagnitude >= Mathf.Pow(armBase.bone2Length + armBase.bone3Length - 1e-5f, 2)) {
                failure = failState.hyperextension;
                isCurrentlyResetting = true;
                StopAndReset();
                // errorMessage = "Agent-arm hyperextended while opening object";
                // succeeded = false;
            }
        }
    }

    public void OnTriggerEnter(Collider other) {
        // If the openable object is meant to ignore trigger collisions entirely, then ignore
        if (!triggerEnabled) {
            return;
        }

        // If the openable object is not opening or closing, then ignore
        if (GetiTweenCount() == 0) {
            return;
        }

        // If the overlapping collider is in the array of gameobjects to explicitly disregard, then ignore
        if (IsInIgnoreArray(other, IgnoreTheseObjects)) {
            return;
        }

        // If the collider is a BoundingBox or ReceptacleTriggerBox, then ignore
        if (other.CompareTag("Untagged") || other.CompareTag("Receptacle")) {
            return;
        }

        // If the overlapping collider is a descendant of the openable GameObject itself, then ignore
        if (hasAncestor(other.transform.gameObject, gameObject)) {
            return;
        }

        // If the overlapping collider belongs to a non-static SimObject, then ignore
        // (Unless we explicitly tell the action to treat non-static SimObjects as barriers)
        if (stopsAtNonStaticCol == false &&
        ancestorSimObjPhysics(other.gameObject) != null &&
        ancestorSimObjPhysics(other.gameObject).PrimaryProperty != SimObjPrimaryProperty.Static) {
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
        isCurrentlyResetting = true;
        StopAndReset();
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

    public int GetiTweenCount() {
        // the number of iTween instances running on all doors managed by this fridge
        int count = 0;
        foreach (GameObject go in MovingParts) {
            count += iTween.Count(go);
        }
        return count; // iTween.Count(this.gameObject);
    }

    // note: reset can interrupt the Interact() itween call because
    // it will start a new set of tweens before onComplete is called from Interact()... it seems
    public void StopAndReset() {
        if (isCurrentlyResetting == true) {
            iTween.Stop(gameObject, true);
            Interact(openness: startOpenness, physicsInterval: startFixedDeltaTime, useGripper: startUseGripper);
            StartCoroutine(updateReset());
        }
        return;
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

    // Resets the isCurrentlyResetting boolean once the reset tween is done.
    // This checks for iTween instances, once there are none this object can be used again
    private IEnumerator updateReset() {
        yield return new WaitUntil(() => GetiTweenCount() == 0);
        isCurrentlyResetting = false;
        yield break;
    }

    public failState GetFailState() {
        return failure;
    }

    public GameObject GetFailureCollision() {
        return failureCollision;
    }

    public void setStopsAtNonStaticCol(bool stopAtNonStaticCol) {
        stopsAtNonStaticCol = stopAtNonStaticCol;
    }
}