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
    private float startOpenness; // used to reset on failure

    [Header("Objects To Ignore Collision With - For Cabinets/Drawers with hinges too close together")]
    // these are objects to ignore collision with. This is in case the fridge doors touch each other or something that might
    // prevent them from closing all the way. Probably not needed but it's here if there is an edge case
    [SerializeField]
    public GameObject[] IgnoreTheseObjects;

    [Header("State information bools")]
    [SerializeField]
    public bool isOpen = false;

    [SerializeField]
    public bool isCurrentlyResetting = true;

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

    // Use this for initialization
    void Start() {
        // init Itween in all doors to prep for animation
        if (MovingParts != null) {
            foreach (GameObject go in MovingParts) {
                iTween.Init(go);

                // check to make sure all doors have a Fridge_Door.cs script on them, if not throw a warning
                // if (!go.GetComponent<Fridge_Door>())
                // Debug.Log("Fridge Door is missing Fridge_Door.cs component! OH NO!");
            }
        }

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

    public void Interact(float openness = 1.0f) {
        // if this object is pickupable AND it's trying to open (book, box, laptop, etc)
        // before trying to open or close, these objects must have kinematic = false otherwise it might clip through other objects
        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
        if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup && sop.isInAgentHand == false) {
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
        }

        startOpenness = currentOpenness;
        for (int i = 0; i < MovingParts.Length; i++) {
            Hashtable args = new Hashtable() {
                {"islocal", true},
                {"time", animationTime},
                {"easetype", "linear"}
            };

            // we are on the last moving part here
            if (i == MovingParts.Length - 1) {
                args["onCompleteTarget"] = gameObject;
            }

            // let's open the object!
            if (movementType == MovementType.Rotate) {
                args["rotation"] = openPositions[i] * openness;
                iTween.RotateTo(MovingParts[i], args);
            } else if (movementType == MovementType.Slide) {
                // this is used to determine which components of openPosition need to be scaled
                // default to open position without percentage open modifiers
                Vector3 lerpToPosition = openPositions[i];

                // some x, y, z components don't change when sliding open
                // only apply openness modifier to components of vector3 that actually change
                if (openPositions[i].x - closedPositions[i].x != Mathf.Epsilon) {
                    lerpToPosition.x = ((openPositions[i].x - closedPositions[i].x) * openness) + closedPositions[i].x;
                }
                if (openPositions[i].y - closedPositions[i].y != Mathf.Epsilon) {
                    lerpToPosition.y = ((openPositions[i].y - closedPositions[i].y) * openness) + closedPositions[i].y;
                }
                if (openPositions[i].z - closedPositions[i].z != Mathf.Epsilon) {
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
        // SwitchActiveBoundingBox();
    }

    //// private void SwitchActiveBoundingBox() {
    //    // some things that open and close don't need to switch bounding boxes- drawers for example, only things like
    //    // cabinets that are not self contained need to switch between open/close bounding box references (ie: books, cabinets, microwave, etc)
    //    if (OpenBoundingBox == null || ClosedBoundingBox == null) {
    //        return;
    //    }

    //    SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
    //    sop.BoundingBox = isOpen ? OpenBoundingBox : ClosedBoundingBox;
    //}

    public bool GetisOpen() {
        return isOpen;
    }

    // for use in OnTriggerEnter ignore check
    // return true if it should ignore the object hit. Return false to cause this object to reset to the original position
    public bool IsInIgnoreArray(Collider other, GameObject[] arrayOfCol) {
        for (int i = 0; i < arrayOfCol.Length; i++) {
            if (other.GetComponentInParent<CanOpen_Object>().transform) {
                if (other.GetComponentInParent<CanOpen_Object>().transform == arrayOfCol[i].transform) {
                    return true;
                }
            } else {
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
        return count; // iTween.Count(this.transform.gameObject);
    }

    // note: reset can interrupt the Interact() itween call because
    // it will start a new set of tweens before onComplete is called from Interact()... it seems
    public void Reset() {
        if (!isCurrentlyResetting) {
            Interact(openness: startOpenness);
            StartCoroutine("updateReset");
        }
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

    public void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Receptacle")) {
            return;
        }
        if (!triggerEnabled) {
            return;
        }
        // note: Normally rigidbodies set to Kinematic will never call the OnTriggerX events
        // when colliding with another rigidbody that is kinematic. For some reason, if the other object
        // has a trigger collider even though THIS object only has a kinematic rigidbody, this
        // function is still called so we'll use that here:

        // The Agent has a trigger Capsule collider, and other cabinets/drawers have
        // a trigger collider, so this is used to reset the position if the agent or another
        // cabinet or drawer is in the way of this object opening/closing

        // if hitting the Agent AND not being currently held by the Agent(so things like Laptops don't constantly reset if the agent is holding them)
        // ..., reset position and report failed action
        // NOTE: hitting the agent and resetting is now handled by the OpenAnimation coroutine in PhysicsRemote

        //// If the thing your colliding with is one of your (grand)-children then don't worry about it
        if (hasAncestor(other.transform.gameObject, gameObject)) {
            return;
        }

        // if hitting another object that has double doors, do some checks 
        if (other.GetComponentInParent<CanOpen_Object>() && isCurrentlyResetting == true) {
            if (IsInIgnoreArray(other, IgnoreTheseObjects)) {
                // don't reset, it's cool to ignore these since some cabinets literally clip into each other if they are double doors
                return;
            }

            // oh it was something else RESET! DO IT!
            else {
                // check the collider hit's parent for itween instances
                // if 0, then it is not actively animating so check against it. This is needed so openable objects don't reset unless they are the active
                // object moving. Otherwise, an open cabinet hit by a drawer would cause the Drawer AND the cabinet to try and reset.
                // this should be fine since only one cabinet/drawer will be moving at a time given the Agent's action only opening on object at a time
                if (other.transform.GetComponentInParent<CanOpen_Object>().GetiTweenCount() == 0
                    && other.GetComponentInParent<SimObjPhysics>().PrimaryProperty == SimObjPrimaryProperty.Static)// check this so that objects that are openable & pickupable don't prevent drawers/cabinets from animating
                {
#if UNITY_EDITOR
                    Debug.Log(gameObject.name + " hit " + other.name + " on " + other.GetComponentInParent<SimObjPhysics>().transform.name + " Resetting position");
#endif
                    isCurrentlyResetting = false;
                    Reset();
                }
            }
        }
    }

    // resets the isCurrentlyResetting boolean once the reset tween is done. This checks for iTween instances, once there are none this object can be used again
    private IEnumerator updateReset() {
        while (true) {
            if (GetiTweenCount() != 0) {
                yield return new WaitForEndOfFrame();
            } else {
                isCurrentlyResetting = true;
                yield break;
            }
        }
    }
}