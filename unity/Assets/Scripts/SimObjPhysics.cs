using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.SceneManagement;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class SimObjPhysics : MonoBehaviour, SimpleSimObj {
    [Header("Unique String ID of this Object In Scene")]
    [SerializeField]
    public string objectID = string.Empty;

    [Header("Name of Prefab this Object comes from")]
    [SerializeField]
    public string assetID = string.Empty;

    [Header("Object Type")]
    [SerializeField]
    public SimObjType Type = SimObjType.Undefined;

    [Header("Primary Property (Must Have only 1)")]
    [SerializeField]
    public SimObjPrimaryProperty PrimaryProperty;

    [Header("Additional Properties (Can Have Multiple)")]
    [SerializeField]
    public SimObjSecondaryProperty[] SecondaryProperties;

    [Header("non Axis-Aligned Box enclosing all colliders of this object")]
    // This can be used to get the "bounds" of the object, but needs to be created manually
    // we should look into a programatic way to figure this out so we don't have to set it up for EVERY object
    // for now, only CanPickup objects have a BoundingBox, although maybe every sim object needs one for
    // spawning eventually? For now make sure the Box Collider component is disabled on this, because it's literally
    // just holding values for the center and size of the box.
    public GameObject BoundingBox = null;

    [Header("Raycast to these points to determine Visible/Interactable")]
    [SerializeField]
    public Transform[] VisibilityPoints = null;

    [Header("If this object is a Receptacle, put all trigger boxes here")]
    [SerializeField]
    public GameObject[] ReceptacleTriggerBoxes = null;

    [Header("State information Bools here")]
#if UNITY_EDITOR
    public bool debugIsVisible = false;
    public bool debugIsInteractable = false;
#endif
    public bool isInAgentHand = false;

    public DroneFPSAgentController droneFPSAgent;

    // these collider references are used for switching physics materials for all colliders on this object
    [Header("Non - Trigger Colliders of this object")]
    public Collider[] MyColliders = null;

    [Header("High Friction physics values")]
    public float HFdynamicfriction;
    public float HFstaticfriction;
    public float HFbounciness;
    public float HFrbdrag;
    public float HFrbangulardrag;

    private float RBoriginalDrag;
    private float RBoriginalAngularDrag;

    [Header("Salient Materials")] // if this object is moveable or pickupable, set these up
    public SalientObjectMaterial[] salientMaterials;

    private PhysicsMaterialValues[] OriginalPhysicsMaterialValuesForAllMyColliders = null;

    public Dictionary<Collider, ContactPoint[]> contactPointsDictionary = new Dictionary<Collider, ContactPoint[]>();

    // if this object is a receptacle, get all valid spawn points from any child ReceptacleTriggerBoxes and sort them by distance to Agent
    public List<ReceptacleSpawnPoint> MySpawnPoints = new List<ReceptacleSpawnPoint>();

    // keep track of this object's current temperature (abstracted to three states, RoomTemp/Hot/Cold)
    public Temperature CurrentTemperature = Temperature.RoomTemp;

    // value for how long it should take this object to get back to room temperature from hot/cold
    public float HowManySecondsUntilRoomTemp = 10f;
    private float TimerResetValue;

    private PhysicsSceneManager sceneManager;// reference to scene manager object

    public bool inMotion = false;

    // count of number of other sim objects this object has hit, if agent is drone
    public int numSimObjHit = 0;
    public int numFloorHit = 0;
    public int numStructureHit = 0;

    // the velocity of this object from the last frame
    public float lastVelocity = 0;// start at zero assuming at rest

    // reference to this gameobject's rigidbody
    private Rigidbody myRigidbody;

    // properties initialized during Start()
    public bool IsReceptacle;
    public bool IsPickupable;
    public bool IsMoveable;
    public bool isStatic;
    public bool IsToggleable;
    public bool IsOpenable;
    public bool IsBreakable;
    public bool IsFillable;
    public bool IsDirtyable;
    public bool IsCookable;
    public bool IsSliceable;
    public bool isHeatSource;
    public bool isColdSource;
    private BoundingBoxCacheKey boundingBoxCacheKey;
    private ObjectOrientedBoundingBox cachedObjectOrientedBoundingBox;
    private AxisAlignedBoundingBox cachedAxisAlignedBoundingBox;

    private bool forceCreateObjectOrientedBoundingBox = false;
    
    private bool shouldCreateObjectOrientedBoundingBox {
        get {
            return this.forceCreateObjectOrientedBoundingBox || this.IsPickupable || this.IsMoveable;
        }
    }

    public float GetTimerResetValue() {
        return TimerResetValue;
    }

    public void SetHowManySecondsUntilRoomTemp(float f) {
        TimerResetValue = f;
        HowManySecondsUntilRoomTemp = f;
    }
    private bool StartRoomTempTimer = false;

    public void SetStartRoomTempTimer(bool b) {
        StartRoomTempTimer = b;
    }

    // used for PickupContainedObjects, this only references which objects were
    // actively contained by the sim object at the moment of pickup.
    public List<SimObjPhysics> ContainedObjectReferences;

#if UNITY_EDITOR
    // all objects currently contained by this receptacle
    public List<GameObject> CurrentlyContains;
#endif

    public class PhysicsMaterialValues {
        public float DynamicFriction;
        public float StaticFriction;
        public float Bounciness;

        public PhysicsMaterialValues(float dFriction, float sFriction, float b) {
            DynamicFriction = dFriction;
            StaticFriction = sFriction;
            Bounciness = b;
        }
    }

    public void AddToContainedObjectReferences(SimObjPhysics t) {
        ContainedObjectReferences.Add(t);
    }

    public void RemoveFromContainedObjectReferences(SimObjPhysics t) {
        ContainedObjectReferences.Remove(t);
    }

    public void syncBoundingBoxes(
        bool forceCacheReset = false,
        bool forceCreateObjectOrientedBoundingBox = false
    ) {

        if (forceCreateObjectOrientedBoundingBox) {
            bool shouldCreateBefore = this.shouldCreateObjectOrientedBoundingBox;
            this.forceCreateObjectOrientedBoundingBox = forceCreateObjectOrientedBoundingBox;

            forceCacheReset = forceCacheReset || shouldCreateBefore != this.shouldCreateObjectOrientedBoundingBox;
        }

        Vector3 position = this.gameObject.transform.position;
        Quaternion rotation = this.gameObject.transform.rotation;

        // position and rotation will vary slightly due to floating point errors
        // so we use a very small epsilon value for comparison instead of 
        // checking equality
        if (
            (!forceCacheReset)
            && this.boundingBoxCacheKey != null
            && (
                !this.IsOpenable || (
                    this.IsOpen == this.boundingBoxCacheKey.IsOpen
                    && Math.Abs(this.openness - this.boundingBoxCacheKey.openness) < .0001f
                )
               )
            && (!this.IsSliceable || this.IsSliced == this.boundingBoxCacheKey.IsSliced)
            && (!this.IsBreakable || this.IsBroken == this.boundingBoxCacheKey.IsBroken)
            && Quaternion.Angle(rotation, this.boundingBoxCacheKey.rotation) < 0.0001f
            && Vector3.Distance(position, this.boundingBoxCacheKey.position) < 0.0001f
        ) {
            return;
        }
        this.cachedAxisAlignedBoundingBox = this.axisAlignedBoundingBox();
        this.cachedObjectOrientedBoundingBox = this.objectOrientedBoundingBox();
        this.boundingBoxCacheKey = new BoundingBoxCacheKey();
        this.boundingBoxCacheKey.position = position;
        this.boundingBoxCacheKey.rotation = rotation;
        if (this.IsOpenable) {
            this.boundingBoxCacheKey.IsOpen = this.IsOpen;
            this.boundingBoxCacheKey.openness = this.openness;
        }
        if (this.IsBreakable) {
            this.boundingBoxCacheKey.IsBroken = this.IsBroken;
        }
        if (this.IsSliced) {
            this.boundingBoxCacheKey.IsSliced = this.IsSliced;
        }
    }

    private AxisAlignedBoundingBox axisAlignedBoundingBox() {

        AxisAlignedBoundingBox b = new AxisAlignedBoundingBox();

        // unparent child simobjects during bounding box generation
        List<Transform> childSimObjects = new List<Transform>();
        foreach (SimObjPhysics simObject in this.transform.GetComponentsInChildren<SimObjPhysics>()) {
            if (simObject != this) {
                childSimObjects.Add(simObject.transform);

                // TODO: fix, This line makes accessing the axisAligned bounding box of some composed object during edior fail
                // Which is needed for procedural serialization

                // HACK the bounding box function needs to be reworked to not use parenting/unparenting
                // And more information needs to be stored for the TWO cases of parenting
                // Firs: structural parenting, where objects are permanently part of each other drawer child of dresser
                // Second: another functional parenting, where objects are temporarily parented but shouldn't be treated as part of the same prefab
                // e.g. apple on a plate
#if !UNITY_EDITOR
                simObject.transform.parent = null;
#endif
            }
        }

        // get all colliders on the sop, excluding colliders if they are not enabled
        Collider[] cols = this.GetComponentsInChildren<Collider>();

        // 0 colliders mean the object is despawned, so this will cause objects broken into pieces to not generate an axis aligned box
        if (cols.Length == 0) {
            SimObjPhysics sopc = this.GetComponent<SimObjPhysics>();
            if (sopc.IsBroken || sopc.IsSliced) {
#if UNITY_EDITOR
                Debug.Log("Object is broken or sliced in pieces, no AxisAligned box generated: " + this.name);
#endif
                return b;
            } else {
#if UNITY_EDITOR
                Debug.Log("Something went wrong, no Colliders were found on" + this.name);
#endif
                return b;
            }
        }

        Bounds bounding = UtilityFunctions.CreateEmptyBounds();
        foreach (Collider c in cols) {
            if (c.enabled && !c.isTrigger) {
                bounding.Encapsulate(c.bounds);
            }
        }
        foreach (Transform visPoint in this.VisibilityPoints) {
            bounding.Encapsulate(visPoint.position);
        }

        // reparent child simobjects
        foreach (Transform childSimObject in childSimObjects) {
            childSimObject.SetParent(this.transform);
        }

        // ok now we have a bounds that encapsulates all the colliders of the object, EXCLUDING trigger colliders
        List<float[]> cornerPoints = new List<float[]>();
        float[] xs = new float[]{
            bounding.center.x + bounding.size.x / 2f,
            bounding.center.x - bounding.size.x / 2f
        };
        float[] ys = new float[]{
            bounding.center.y + bounding.size.y / 2f,
            bounding.center.y - bounding.size.y / 2f
        };
        float[] zs = new float[]{
            bounding.center.z + bounding.size.z / 2f,
            bounding.center.z - bounding.size.z / 2f
        };
        foreach (float x in xs) {
            foreach (float y in ys) {
                foreach (float z in zs) {
                    cornerPoints.Add(new float[] { x, y, z });
                }
            }
        }
        b.cornerPoints = cornerPoints.ToArray();

        b.center = bounding.center; // also return the center of this bounding box in world coordinates
        b.size = bounding.size; // also return the size in the x, y, z axes of the bounding box in world coordinates

        return b;

    }

    private ObjectOrientedBoundingBox objectOrientedBoundingBox() {
        if (this.shouldCreateObjectOrientedBoundingBox) {
            ObjectOrientedBoundingBox b = new ObjectOrientedBoundingBox();

            bool shouldSetupBBox = false;
            try {
                shouldSetupBBox = this.BoundingBox == null;
            } catch (UnassignedReferenceException e) {
                shouldSetupBBox = true;
            }

            if (shouldSetupBBox) {
                GameObject bb = new GameObject("BoundingBox");
                bb.transform.position = gameObject.transform.position;
                bb.transform.SetParent(gameObject.transform);
                bb.AddComponent<BoxCollider>();
                bb.GetComponent<BoxCollider>().enabled = false;
                bb.tag = "Untagged";
                bb.layer = 0;
                this.BoundingBox = bb;
                ContextSetUpBoundingBox();
            }

            // unparent child simobjects during bounding box generation
            List<Transform> childSimObjects = new List<Transform>();
            foreach (SimObjPhysics simObject in this.transform.GetComponentsInChildren<SimObjPhysics>()) {
                if (simObject != this) {
                    childSimObjects.Add(simObject.transform);
                    simObject.transform.parent = null;
                }

            }

            // The GameObject is cloned instead of teleporting/rotating to avoid
            // waking up the rigidbody, which can cause the SimObj to shift by tiny amounts
            // since it will never sleep.
            GameObject clone = Instantiate(this.gameObject, Vector3.zero, Quaternion.identity);


            // Get all colliders on the sop, excluding colliders if they are not enabled
            List<(Collider, LayerMask)> cols = new List<(Collider, LayerMask)>();
            var nonInteractiveLayer = LayerMask.NameToLayer("NonInteractive");
            foreach (Collider c in clone.transform.GetComponentsInChildren<Collider>()) {
                if (c.enabled) {
                    // save the state of all the layers prior to modifying
                    cols.Add((c, c.transform.gameObject.layer));

                    // move these colliders to the NonInteractive layer so upon teleporting to the origin, nothing is disturbed
                    c.transform.gameObject.layer = nonInteractiveLayer;
                }
            }

            // Initialize the new bounds
            Bounds newBB = UtilityFunctions.CreateEmptyBounds();

            // Encapsulate all active colliders in SimObject's array
            foreach ((Collider, LayerMask) colAndLayerMask in cols) {
                Collider c = colAndLayerMask.Item1;
                if (!c.isTrigger && c.gameObject != clone.GetComponent<SimObjPhysics>().BoundingBox) {
                    newBB.Encapsulate(c.bounds);
                }
            }
            // Encapsulate all visilibity points of the SimObject array
            foreach (Transform visPoint in clone.GetComponent<SimObjPhysics>().VisibilityPoints) {
                newBB.Encapsulate(visPoint.position);
            }

            // Update SimObject's BoundingBox collider to match new bounds
            this.BoundingBox.transform.localPosition = Vector3.zero;
            this.BoundingBox.transform.localRotation = Quaternion.identity;
            this.BoundingBox.GetComponent<BoxCollider>().center = newBB.center;
            this.BoundingBox.GetComponent<BoxCollider>().size = newBB.extents * 2.0f;

            // Re-enable colliders, moving them back to their original layer
            foreach ((Collider, LayerMask) colAndLayerMask in cols) {
                colAndLayerMask.Item1.transform.gameObject.layer = colAndLayerMask.Item2;
            }

            // reparent child simobjects
            foreach (Transform childSimObject in childSimObjects) {
                childSimObject.SetParent(this.transform);
            }

            DestroyImmediate(clone);

            // iTween adds references to the iTween.tweens List
            for (int i = 0; i < iTween.tweens.Count; i++) {
                if (((GameObject)iTween.tweens[i]["target"]) == null) {
                   iTween.tweens.RemoveAt(i);
                }
            }


            // Get corner points of SimObject's new BoundingBox, in its correct transformation
            List<Vector3> points = new List<Vector3>();
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(newBB.size.x, -newBB.size.y, newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(-newBB.size.x, -newBB.size.y, newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(-newBB.size.x, -newBB.size.y, -newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(newBB.size.x, -newBB.size.y, -newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(newBB.size.x, newBB.size.y, newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(-newBB.size.x, newBB.size.y, newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(-newBB.size.x, +newBB.size.y, -newBB.size.z) * 0.5f));
            points.Add(this.transform.TransformPoint(newBB.center + new Vector3(newBB.size.x, newBB.size.y, -newBB.size.z) * 0.5f));

            List<float[]> cornerPoints = new List<float[]>();
            foreach (Vector3 p in points) {
                cornerPoints.Add(new float[] { p.x, p.y, p.z });
            }

            b.cornerPoints = cornerPoints.ToArray();

            return b;
        }

        return null;
    }
    public void DropContainedObjectsStationary() {
        this.DropContainedObjects(reparentContainedObjects: false, forceKinematic: true);
    }

    public void DropContainedObjects(
        bool reparentContainedObjects,
        bool forceKinematic
    ) {
        if (this.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
            GameObject topObject = null;

            foreach (SimObjPhysics sop in this.ContainedObjectReferences) {
                // for every object that is contained by this object turn off
                // the colliders, leaving Trigger Colliders active (this is important to maintain visibility!)
                sop.transform.Find("Colliders").gameObject.SetActive(true);
                sop.isInAgentHand = false; // Agent hand flag

                if (reparentContainedObjects) {
                    if (topObject == null) {
                        topObject = GameObject.Find("Objects");
                    }
                    sop.transform.SetParent(topObject.transform);
                }

                Rigidbody rb = sop.GetComponent<Rigidbody>();
                rb.isKinematic = forceKinematic;
                if (!forceKinematic) {
                    rb.useGravity = true;
                    rb.constraints = RigidbodyConstraints.None;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }

            }
            this.ClearContainedObjectReferences();
        }
    }

    public AxisAlignedBoundingBox AxisAlignedBoundingBox {
        get {
            this.syncBoundingBoxes();
            return this.cachedAxisAlignedBoundingBox;
        }
    }

    public ObjectOrientedBoundingBox ObjectOrientedBoundingBox {
        get {
            this.syncBoundingBoxes();
            return this.cachedObjectOrientedBoundingBox;
        }
    }

    public void ClearContainedObjectReferences() {
        ContainedObjectReferences.Clear();
    }

    public string ObjectID {
        get {
            return objectID;
        }

        set {
            objectID = value;
        }
    }

    public SimObjType ObjType {
        get {
            return Type;
        }
        set {
            Type = value;
        }
    }

    public int ReceptacleCount {
        get {
            return 0;
        }
    }

    public List<string> ReceptacleObjectIds {
        get {
            return this.GetAllSimObjectsInReceptacleTriggersByObjectID();
        }
    }

    // get all objects contained by this receptacle object as a list of SimObjPhysics
    public List<SimObjPhysics> SimObjectsContainedByReceptacle {
        get {
            return this.GetAllSimObjectsInReceptacleTriggers();
        }

    }

    // return mass of object
    public float Mass {
        get {
            return this.GetComponent<Rigidbody>().mass;
        }
    }


    // if this pickupable object is being held by the agent right
    public bool isPickedUp {
        get {
            return this.isInAgentHand;
        }
    }


    // note some objects are not toggleable, but can still return the IsToggled meta value (ex: stove burners)
    // stove burners are not toggleable directly, a stove knob controls them.
    private bool isToggleable {
        get {
            if (this.GetComponent<CanToggleOnOff>()) {
                // if this object is self controlled, it is toggleable
                if (this.GetComponent<CanToggleOnOff>().ReturnSelfControlled()) {
                    return true;
                }

                // if it is not self controlled (meaning controlled by another sim object) return not toggleable
                // although if this object is not toggleable, it may still return isToggled as a state (see IsToggled below)
                else {
                    return false;
                }
            } else {
                return false;
            }
            // return this.GetComponent<CanToggleOnOff>(); 
        }
    }

    public bool IsToggled {
        get {
            // note: this can return "toggled on or off" info about objects that are controlled by other sim objects
            // for example, a stove burner will return if it is on/off even though the burner itself cannot be interacted with
            // to toggle the on/off state. Stove burners and objects like it can only have their state toggled by a sim object
            // that controls it (in this case stove knob -controls-> stove burner)
            CanToggleOnOff ctoo = this.GetComponent<CanToggleOnOff>();

            if (ctoo != null) {
                return ctoo.isOn;
            } else {
                return false;
            }
        }
    }


    public float openness {
        get { return this.GetComponent<CanOpen_Object>().currentOpenness; }
    }

    public bool IsOpen {
        get {
            CanOpen_Object coo = this.GetComponent<CanOpen_Object>();

            if (coo != null) {
                return coo.isOpen;
            } else {
                return false;
            }
        }
    }

    public bool IsBroken {
        get {
            Break b = this.GetComponentInChildren<Break>();
            if (b != null) {
                return b.isBroken();
            } else {
                return false;
            }
        }
    }

    public bool IsFilled {
        get {
            Fill f = this.GetComponent<Fill>();
            if (f != null) {
                return f.IsFilled();
            } else {
                return false;
            }
        }
    }

    public string FillLiquid {
        get {
            Fill f = this.GetComponent<Fill>();
            return f == null ? null : f.FilledLiquid();
        }
    }

    public bool IsDirty {
        get {
            Dirty deedsdonedirtcheap = this.GetComponent<Dirty>();
            if (deedsdonedirtcheap != null) {
                return deedsdonedirtcheap.IsDirty();
            } else {
                return false;
            }
        }
    }

    public bool IsCooked {
        get {
            CookObject tasty = this.GetComponent<CookObject>();
            if (tasty != null) {
                return tasty.IsCooked();
            } else {
                return false;
            }
        }
    }

    // if the object has been sliced, the rest of it has been disabled so it can't be seen or interacted with, but the metadata
    // will still reflect it's last position at time of being sliced. This is similar to break
    public bool IsSliced {
        get {
            SliceObject kars = this.GetComponent<SliceObject>();
            if (kars != null) {
                return kars.IsSliced();
            } else {
                return false;
            }
        }
    }

    /// these aren't in yet, just placeholder
    public bool CanBeUsedUp {
        get {
            return false;
        }
    }
    public bool IsUsedUp {
        get {
            return false;
        }
    }
    /// end placeholder stuff

    // return temperature enum here
    public Temperature CurrentObjTemp {
        get {
            return CurrentTemperature;
        }
    }

    private void FindMySpawnPoints(BaseFPSAgentController agent = null) {
        List<ReceptacleSpawnPoint> temp = new List<ReceptacleSpawnPoint>();
        foreach (GameObject rtb in ReceptacleTriggerBoxes) {
            Contains containsScript = rtb.GetComponent<Contains>();
            temp.AddRange(containsScript.GetValidSpawnPoints(agent));
        }

        if (agent != null) {
            temp.Sort(delegate (ReceptacleSpawnPoint one, ReceptacleSpawnPoint two) {
                return Vector3.Distance(agent.transform.position, one.Point).CompareTo(Vector3.Distance(agent.transform.position, two.Point));
            });
        }

        MySpawnPoints = temp;
    }

    // return spawn points for this receptacle objects based on the top part of all trigger boxes
    public List<Vector3> FindMySpawnPointsFromTopOfTriggerBox(bool forceVisible = false) {
        List<Vector3> points = new List<Vector3>();
        foreach (GameObject rtb in ReceptacleTriggerBoxes) {
            points.AddRange(rtb.GetComponent<Contains>().GetValidSpawnPointsFromTopOfTriggerBox());
        }

        return points;
    }

    // set ReturnPointsCloseToAgent to true if only points near the agent are wanted
    // set to false if all potential points on the object are wanted
    public List<ReceptacleSpawnPoint> ReturnMySpawnPoints(BaseFPSAgentController agent = null) {
        FindMySpawnPoints(agent);
        return MySpawnPoints;
    }

    public void ResetContactPointsDictionary() {
        contactPointsDictionary.Clear();
    }

    void OnCollisionEnter(Collision col) {
        // this is to enable kinematics if this object hits another object that isKinematic but needs to activate
        // physics uppon being touched/collided

        if (droneFPSAgent == null) {
            return;
        }

        // GameObject agent = GameObject.Find("FPSController");
        if (col.transform.GetComponentInParent<SimObjPhysics>()) {
            // add a check for if it's for initialization
            if (droneFPSAgent.HasLaunch(this)) {
                // add a check for if this is the object caought by the drone
                if (!droneFPSAgent.isObjectCaught(this)) {
                    // emperically find the relative velocity > 1 means a "real" hit.
                    if (col.relativeVelocity.magnitude > 1) {
                        // make sure we only count hit once per time, not for all collision contact points of an object.
                        if (!contactPointsDictionary.ContainsKey(col.collider)) {
                            numSimObjHit++;
                        }
                    }
                }
            }
        }


        // add a check for if the hitting one is a structure object
        else if (col.transform.GetComponentInParent<StructureObject>()) {
            // add a check for if it's for initialization
            if (droneFPSAgent.HasLaunch(this)) {
                // add a check for if this is the object caought by the drone
                if (!droneFPSAgent.isObjectCaught(this)) {
                    // emperically find the relative velocity > 1 means a "real" hit.
                    if (col.relativeVelocity.magnitude > 1) {
                        // make sure we only count hit once per time, not for all collision contact points of an object.
                        if (!contactPointsDictionary.ContainsKey(col.collider)) {
                            numStructureHit++;
                            // check if structure hit is a floor
                            if (col.transform.GetComponentInParent<StructureObject>().WhatIsMyStructureObjectTag == StructureObjectTag.Floor) {
                                numFloorHit++;
                            }
                        }
                    }
                }
            }
        }

        contactPointsDictionary[col.collider] = col.contacts;
    }

    void OnCollisionExit(Collision col) {
        contactPointsDictionary.Remove(col.collider);
    }

#if UNITY_EDITOR

    [UnityEditor.MenuItem("AI2-THOR/Add GUID to Object Names")]
    public static void AddGUIDToSimObjPhys() {
        SimObjPhysics[] objects = GameObject.FindObjectsOfType<SimObjPhysics>();// Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        foreach (SimObjPhysics sop in objects) {
            Guid g;
            g = Guid.NewGuid();
            sop.name = sop.GetComponent<SimObjPhysics>().Type.ToString() + "_" + g.ToString("N").Substring(0, 8);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [UnityEditor.MenuItem("SimObjectPhysics/Create RB Collider")]
    public static void CreateRBCollider() {
        GameObject prefabRoot = Selection.activeGameObject;
        // print(prefabRoot.name);

        GameObject inst = Instantiate(prefabRoot, Selection.activeGameObject.transform, true);

        // inst.transform.SetParent(Selection.activeGameObject.transform);

        inst.name = "rbCol";
        inst.gameObject.AddComponent<Rigidbody>();
        inst.GetComponent<Rigidbody>().isKinematic = true;
        inst.GetComponent<Rigidbody>().useGravity = true;

        // default tag and layer so that nothing is raycast against this. The only thing this exists for is to make physics real
        inst.tag = "Untagged";
        inst.layer = 0;// default layer

        // EditorUtility.GetPrefabParent(Selection.activeGameObject);
        // PrefabUtility.InstantiatePrefab(prefabRoot);
    }

    [UnityEditor.MenuItem("SimObjectPhysics/Change All Lights to Soft")]
    public static void AllOfTheLights() {
        Light[] lights = FindObjectsOfType(typeof(Light)) as Light[];
        foreach (Light l in lights) {
            l.shadows = LightShadows.Soft;
        }
    }

    [UnityEditor.MenuItem("SimObjectPhysics/Create Sim Obj from Mesh &r")]
    public static void CreateFromMesh() {
        GameObject prefabRoot = Selection.activeGameObject;
        GameObject top = new GameObject(prefabRoot.name);
        top.transform.position = prefabRoot.transform.position;
        top.transform.rotation = prefabRoot.transform.rotation;
        prefabRoot.name = "mesh";

        prefabRoot.transform.SetParent(top.transform);

        SimObjPhysics sop = top.AddComponent<SimObjPhysics>();

        sop.ContextSetUpSimObjPhysics();
    }

    [UnityEditor.MenuItem("SimObjectPhysics/Set Transform Scale to 1 &e")]
    public static void ResetTransformScale() {
        GameObject selected = Selection.activeGameObject;

        List<Transform> selectedchildren = new List<Transform>();

        foreach (Transform t in selected.transform) {
            // print(t.name);
            selectedchildren.Add(t);
            // t.SetParent(null);
        }

        foreach (Transform yes in selectedchildren) {
            yes.SetParent(null);
        }

        selected.transform.localScale = new Vector3(1, 1, 1);

        foreach (Transform t in selectedchildren) {
            t.SetParent(selected.transform);
        }


    }

    [UnityEditor.MenuItem("SimObjectPhysics/Set All Transforms to Defaults &d")]
    public static void ResetTransform() {
        GameObject selected = Selection.activeGameObject;

        List<Transform> selectedchildren = new List<Transform>();

        foreach (Transform t in selected.transform) {
            // print(t.name);
            selectedchildren.Add(t);
            // t.SetParent(null);
        }

        foreach (Transform yes in selectedchildren) {
            yes.SetParent(null);
        }

        selected.transform.localPosition = new Vector3(0, 0, 0);
        selected.transform.localRotation = new Quaternion(0, 0, 0, 0);
        selected.transform.localScale = new Vector3(1, 1, 1);

        foreach (Transform t in selectedchildren) {
            t.SetParent(selected.transform);
        }


    }

    [UnityEditor.MenuItem("SimObjectPhysics/Rotate Box Flap 90 on Y &s")]
    public static void RotateTheBoxFlap() {
        GameObject selected = Selection.activeGameObject;

        List<Transform> selectedchildren = new List<Transform>();

        foreach (Transform t in selected.transform) {
            // print(t.name);
            selectedchildren.Add(t);
            // t.SetParent(null);
        }

        foreach (Transform yes in selectedchildren) {
            yes.SetParent(null);
        }

        float initialRot = selected.transform.localRotation.eulerAngles.x;

        Vector3 setrot = new Vector3(0, 90, 180 - initialRot); // This is weird and we don't know why

        selected.transform.localRotation = Quaternion.Euler(setrot + new Vector3(-180, 180, 180)); // This is weird and we don't know why
        selected.transform.localScale = new Vector3(1, 1, 1);

        foreach (Transform t in selectedchildren) {
            t.SetParent(selected.transform);
        }
    }

#endif

    private void initializeProperties() {
        this.IsReceptacle = Array.IndexOf(SecondaryProperties, SimObjSecondaryProperty.Receptacle) > -1 && ReceptacleTriggerBoxes != null;
        this.IsPickupable = this.PrimaryProperty == SimObjPrimaryProperty.CanPickup;
        this.IsMoveable = this.PrimaryProperty == SimObjPrimaryProperty.Moveable;
        this.isStatic = this.PrimaryProperty == SimObjPrimaryProperty.Static;
        this.IsToggleable = this.isToggleable;
        this.IsOpenable = this.GetComponent<CanOpen_Object>();
        this.IsBreakable = this.GetComponentInChildren<Break>();
        this.IsFillable = this.GetComponent<Fill>();
        this.IsDirtyable = this.GetComponent<Dirty>();
        this.IsCookable = this.GetComponent<CookObject>();
        this.IsSliceable = this.GetComponent<SliceObject>();
        this.isHeatSource = DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.isHeatSource);
        this.isColdSource = DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.isColdSource);

    }

    // Use this for initialization
    void Start() {
        // For debug in editor only
#if UNITY_EDITOR
        List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);
        if (temp.Contains(SimObjSecondaryProperty.Receptacle)) {
            if (ReceptacleTriggerBoxes.Length == 0) {
                Debug.LogError(this.name + " is missing ReceptacleTriggerBoxes please hook them up");
            }
        }

        if (temp.Contains(SimObjSecondaryProperty.ObjectSpecificReceptacle)) {
            if (!gameObject.GetComponent<ObjectSpecificReceptacle>()) {
                Debug.LogError(this.name + " is missing the ObjectSpecificReceptacle component!");
            }
        }

        if (this.tag != "SimObjPhysics") {
            Debug.LogError(this.name + " is missing SimObjPhysics tag!");
        }

        if (IsPickupable || IsMoveable) {
            if (salientMaterials.Length == 0) {
                Debug.LogError(this.name + " is missing Salient Materials array!");
            }
        }

        if (this.transform.localScale != new Vector3(1, 1, 1)) {
            Debug.LogError(this.name + " is not at uniform scale! Set scale to (1, 1, 1)!!!");
        }
#endif
        // end debug setup stuff

        OriginalPhysicsMaterialValuesForAllMyColliders = new PhysicsMaterialValues[MyColliders.Length];

        for (int i = 0; i < MyColliders.Length; i++) {
            OriginalPhysicsMaterialValuesForAllMyColliders[i] =
            new PhysicsMaterialValues(MyColliders[i].material.dynamicFriction, MyColliders[i].material.staticFriction, MyColliders[i].material.bounciness);
        }

        myRigidbody = gameObject.GetComponent<Rigidbody>(); myRigidbody = gameObject.GetComponent<Rigidbody>();
        if (myRigidbody != null) {
            Rigidbody rb = myRigidbody;

            RBoriginalAngularDrag = rb.angularDrag;
            RBoriginalDrag = rb.drag;

            //default all rigidbodies so that if their drag/angular drag is zero, it's at least nonzero
            if (myRigidbody.drag == 0) {
                myRigidbody.drag = 0.01f;
            }
            if (myRigidbody.angularDrag == 0) {
                myRigidbody.angularDrag = 0.01f;
            }
        }

        TimerResetValue = HowManySecondsUntilRoomTemp;

        sceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();

        initializeProperties();
    }

    public bool DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty prop) {
        bool result = false;
        List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);

        if (temp.Contains(prop)) {
            result = true;
        }

        return result;
    }
    // Update is called once per frame
    void Update() {
        if (sceneManager.AllowDecayTemperature)// only do this if the scene is initialized to use Temperature decay over time
        {
            // if this object is either hot or col, begin a timer that counts until the object becomes room temperature again
            if (CurrentTemperature != Temperature.RoomTemp && StartRoomTempTimer == true) {
                HowManySecondsUntilRoomTemp -= Time.deltaTime;
                if (HowManySecondsUntilRoomTemp < 0) {
                    CurrentTemperature = Temperature.RoomTemp;
                    HowManySecondsUntilRoomTemp = TimerResetValue;
                }
            }
            // if this isn't reset by a HeatZone/ColdZone
            StartRoomTempTimer = true;
        }
    }

    void LateUpdate() {
        // only update lastVelocity if physicsAutosimulation = true, otherwise let the Advance Physics function take care of it;
        if (sceneManager.physicsSimulationPaused == false && myRigidbody != null)
        // record this object's current velocity
        {
            lastVelocity = Math.Abs(myRigidbody.angularVelocity.sqrMagnitude + myRigidbody.velocity.sqrMagnitude);
        }
    }

    // used for throwing the sim object, or anything that requires adding force for some reason
    public void ApplyForce(ServerAction action) {
        Vector3 dir = new Vector3(action.x, action.y, action.z);
        Rigidbody myrb = gameObject.GetComponent<Rigidbody>();

        if (myrb.IsSleeping()) {
            myrb.WakeUp();
        }

        myrb.isKinematic = false;
        myrb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        myrb.AddForce(dir * action.moveMagnitude);
    }

    // overload that doesn't use a server action
    public void ApplyForce(Vector3 dir, float magnitude) {

        Rigidbody myrb = gameObject.GetComponent<Rigidbody>();

        if (myrb.IsSleeping()) {
            myrb.WakeUp();
        }

        myrb.isKinematic = false;
        myrb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        myrb.AddForce(dir * magnitude);
    }

    // return all sim objects contained by this object if it is a receptacle
    public List<SimObjPhysics> GetAllSimObjectsInReceptacleTriggers() {
        List<SimObjPhysics> objs = new List<SimObjPhysics>();

        if (DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
            if (ReceptacleTriggerBoxes != null) {
                foreach (GameObject go in ReceptacleTriggerBoxes) {
                    foreach (SimObjPhysics sop in go.GetComponent<Contains>().CurrentlyContainedObjects()) {
                        if (!objs.Contains(sop)) {
                            // print(sop.transform.name);
                            objs.Add(sop);
                        }
                    }
                }
            }
        }

        return objs;
    }

    public float MassIncludingSimObjectsInReceptacle() {
        float mass = myRigidbody.mass;
        foreach (SimObjPhysics sop in GetAllSimObjectsInReceptacleTriggers()) {
            mass += sop.GetComponent<Rigidbody>().mass;
        }
        return mass;
    }

    // return all sim objects by object ID contained by this object if it is a receptacle
    public List<string> GetAllSimObjectsInReceptacleTriggersByObjectID() {
        List<string> objs = new List<string>();

        if (DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
            if (ReceptacleTriggerBoxes != null) {
                foreach (GameObject go in ReceptacleTriggerBoxes) {
                    foreach (string s in go.GetComponent<Contains>().CurrentlyContainedObjectIDs()) {
                        if (!objs.Contains(s)) {
                            // print(sop.transform.name);
                            objs.Add(s);
                        }
                    }
                }
            }
        }

        return objs;
    }

    public List<GameObject> ContainedGameObjects() {
        List<GameObject> objs = new List<GameObject>();

        // get box collider dimensions of ReceptacleTriggerBox if this is a receptacle
        if (IsReceptacle) {
            foreach (GameObject rtb in ReceptacleTriggerBoxes) {
                foreach (GameObject g in rtb.GetComponent<Contains>().CurrentlyContainedGameObjects()) {
                    if (!objs.Contains(g)) {
                        objs.Add(g);
                    }
                }
            }
        }

#if UNITY_EDITOR
        CurrentlyContains = objs;
#endif

        return objs;
    }

    public void OnTriggerEnter(Collider other) {
        // is colliding only needs to be set for pickupable objects. Also drag/friction values only need to change for pickupable objects not all sim objects
        if ((PrimaryProperty == SimObjPrimaryProperty.CanPickup || PrimaryProperty == SimObjPrimaryProperty.Moveable)) {
            if (other.CompareTag("HighFriction")) //&& (PrimaryProperty == SimObjPrimaryProperty.CanPickup || PrimaryProperty == SimObjPrimaryProperty.Moveable))
            {
                Rigidbody rb = gameObject.GetComponent<Rigidbody>();

                // add something so that drag/angular drag isn't reset if we haven't set it on the object yet
                rb.drag = HFrbdrag;
                rb.angularDrag = HFrbangulardrag;

                foreach (Collider col in MyColliders) {
                    col.material.dynamicFriction = HFdynamicfriction;
                    col.material.staticFriction = HFstaticfriction;
                    col.material.bounciness = HFbounciness;
                }
            }
        }
    }

    public void OnTriggerExit(Collider other) {
        if (other.CompareTag("HighFriction") && (PrimaryProperty == SimObjPrimaryProperty.CanPickup || PrimaryProperty == SimObjPrimaryProperty.Moveable)) {
            // print( "resetting to default trigger exit");

            Rigidbody rb = gameObject.GetComponent<Rigidbody>();

            rb.drag = RBoriginalDrag;
            rb.angularDrag = RBoriginalAngularDrag;

            for (int i = 0; i < MyColliders.Length; i++) {
                MyColliders[i].material.dynamicFriction = OriginalPhysicsMaterialValuesForAllMyColliders[i].DynamicFriction;
                MyColliders[i].material.staticFriction = OriginalPhysicsMaterialValuesForAllMyColliders[i].StaticFriction;
                MyColliders[i].material.bounciness = OriginalPhysicsMaterialValuesForAllMyColliders[i].Bounciness;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos() {
        Gizmos.color = Color.white;

        // if this object is in visibile range and not blocked by any other object, it is visible
        // visible drawn in yellow
        if (debugIsVisible == true && gameObject.GetComponentInChildren<MeshFilter>()) {
            MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
        }

        // interactable drawn in magenta
        if (debugIsInteractable && gameObject.GetComponentInChildren<MeshFilter>()) {
            MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
        }

        // draw visibility points for editor
        Gizmos.color = Color.yellow;
        if (VisibilityPoints != null && VisibilityPoints.Length > 0) {
            foreach (Transform t in VisibilityPoints) {
                // if (t != null)
                //{
                Gizmos.DrawSphere(t.position, 0.01f);
                //}
            }
        }

        // foreach(Collider col in MyColliders)
        // {
        //     DebugExtension.DrawBounds(col.bounds, Color.green);
        // }
    }

    // CONTEXT MENU STUFF FOR SETTING UP SIM OBJECTS
    // RIGHT CLICK this script in the inspector to reveal these options

    //[ContextMenu("Cabinet")]
    void SetUpCabinet() {
        Type = SimObjType.Cabinet;
        PrimaryProperty = SimObjPrimaryProperty.Static;

        SecondaryProperties = new SimObjSecondaryProperty[2];
        SecondaryProperties[0] = SimObjSecondaryProperty.CanOpen;
        SecondaryProperties[1] = SimObjSecondaryProperty.Receptacle;

        gameObject.transform.tag = "SimObjPhysics";
        gameObject.layer = 8;

        if (!gameObject.GetComponent<Rigidbody>()) {
            gameObject.AddComponent<Rigidbody>();
        }

        this.GetComponent<Rigidbody>().isKinematic = true;

        if (!gameObject.GetComponent<CanOpen_Object>()) {
            gameObject.AddComponent<CanOpen_Object>();
            gameObject.GetComponent<CanOpen_Object>().SetMovementToRotate();
        }

        // if (!gameObject.GetComponent<MovingPart>())
        // gameObject.AddComponent<MovingPart>();

        List<GameObject> cols = new List<GameObject>();
        // List<GameObject> tcols = new List<GameObject>();
        List<Transform> vpoints = new List<Transform>();
        List<GameObject> recepboxes = new List<GameObject>();

        List<GameObject> movparts = new List<GameObject>();

        List<Vector3> openPositions = new List<Vector3>();

        Transform door = transform.Find("CabinetDoor");


        if (!gameObject.transform.Find("StaticVisPoints")) {
            GameObject svp = new GameObject("StaticVisPoints");
            svp.transform.position = gameObject.transform.position;
            svp.transform.SetParent(gameObject.transform);

            GameObject vp = new GameObject("vPoint");
            vp.transform.position = svp.transform.position;
            vp.transform.SetParent(svp.transform);
        }

        if (!door.Find("Colliders")) {
            GameObject col = new GameObject("Colliders");
            col.transform.position = door.position;
            col.transform.SetParent(door);

            GameObject coll = new GameObject("Col");
            coll.AddComponent<BoxCollider>();
            coll.transform.tag = "SimObjPhysics";
            coll.layer = 8;
            coll.transform.position = col.transform.position;
            coll.transform.SetParent(col.transform);
        }

        if (!door.Find("VisibilityPoints")) {
            GameObject VisPoints = new GameObject("VisibilityPoints");
            VisPoints.transform.position = door.position;
            VisPoints.transform.SetParent(door);

            GameObject vp = new GameObject("VisPoint");
            vp.transform.position = VisPoints.transform.position;
            vp.transform.SetParent(VisPoints.transform);
        } else {
            Transform VisPoints = door.Find("VisibilityPoints");
            foreach (Transform child in VisPoints) {
                vpoints.Add(child);
                child.gameObject.tag = "Untagged";
                child.gameObject.layer = 8;
            }
        }
        ////////////////////////

        foreach (Transform child in gameObject.transform) {
            if (child.name == "StaticVisPoints") {
                foreach (Transform svp in child) {
                    if (!vpoints.Contains(svp)) {
                        vpoints.Add(svp);
                    }
                }
            }

            if (child.name == "ReceptacleTriggerBox") {
                // print("check");
                if (!recepboxes.Contains(child.gameObject)) {
                    recepboxes.Add(child.gameObject);
                }
            }

            // found the cabinet door, go into it and populate triggerboxes, colliders, t colliders, and vis points
            if (child.name == "CabinetDoor") {
                if (child.GetComponent<Rigidbody>()) {
                    DestroyImmediate(child.GetComponent<Rigidbody>(), true);
                }

                if (child.GetComponent<SimObjPhysics>()) {
                    DestroyImmediate(child.GetComponent<SimObjPhysics>(), true);
                }

                if (!movparts.Contains(child.gameObject)) {
                    movparts.Add(child.gameObject);
                }

                foreach (Transform c in child) {
                    if (c.name == "Colliders") {
                        foreach (Transform col in c) {
                            if (!cols.Contains(col.gameObject)) {
                                cols.Add(col.gameObject);
                            }

                            if (col.childCount == 0) {
                                GameObject prefabRoot = col.gameObject;

                                GameObject inst = Instantiate(prefabRoot, col.gameObject.transform, true);

                                // inst.transform.SetParent(Selection.activeGameObject.transform);

                                inst.name = "rbCol";
                                inst.gameObject.AddComponent<Rigidbody>();
                                inst.GetComponent<Rigidbody>().isKinematic = true;
                                inst.GetComponent<Rigidbody>().useGravity = true;

                                // default tag and layer so that nothing is raycast against this. The only thing this exists for is to make physics real
                                inst.tag = "Untagged";
                                inst.layer = 0;// default layer
                            }
                        }
                    }

                    // if (c.name == "TriggerColliders")
                    // {
                    // 	foreach (Transform col in c)
                    // 	{
                    // 		if (!tcols.Contains(col.gameObject))
                    // 			tcols.Add(col.gameObject);
                    // 	}
                    // }

                    if (c.name == "VisibilityPoints") {
                        foreach (Transform col in c) {
                            if (!vpoints.Contains(col.transform)) {
                                vpoints.Add(col.transform);
                            }
                        }
                    }
                }
            }
        }

        VisibilityPoints = vpoints.ToArray();
        // MyColliders = cols.ToArray();
        // MyTriggerColliders = tcols.ToArray();
        ReceptacleTriggerBoxes = recepboxes.ToArray();

        gameObject.GetComponent<CanOpen_Object>().MovingParts = movparts.ToArray();
        gameObject.GetComponent<CanOpen_Object>().openPositions = new Vector3[movparts.Count];
        gameObject.GetComponent<CanOpen_Object>().closedPositions = new Vector3[movparts.Count];

        if (openPositions.Count != 0) {
            gameObject.GetComponent<CanOpen_Object>().openPositions = openPositions.ToArray();
        }

        // this.GetComponent<CanOpen>().SetMovementToRotate();
    }
    //[ContextMenu("Table")]
    void SetUpTable() {
        this.Type = SimObjType.DiningTable;
        this.PrimaryProperty = SimObjPrimaryProperty.Static;
        this.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };

        ContextSetUpSimObjPhysics();

        // GameObject inst = Instantiate(new GameObject(), gameObject.transform, true);
        // inst.AddComponent<BoxCollider>();
        if (!gameObject.transform.Find("BoundingBox")) {
            GameObject bb = new GameObject("BoundingBox");
            bb.transform.position = gameObject.transform.position;
            bb.transform.SetParent(gameObject.transform);
            bb.AddComponent<BoxCollider>();
            bb.GetComponent<BoxCollider>().enabled = false;
            bb.tag = "Untagged";
            bb.layer = 0;

            BoundingBox = bb;
        }

        List<GameObject> recepboxes = new List<GameObject>();

        foreach (Transform t in gameObject.transform) {
            if (t.GetComponent<Contains>()) {
                recepboxes.Add(t.gameObject);
            }
        }

        ReceptacleTriggerBoxes = recepboxes.ToArray();
    }

    [ContextMenu("Setup Floor")]
    void FloorSetupContext() {
        this.Type = SimObjType.Floor;
        this.PrimaryProperty = SimObjPrimaryProperty.Static;

        this.SecondaryProperties = new SimObjSecondaryProperty[]
        {SimObjSecondaryProperty.Receptacle};

        if (!gameObject.GetComponent<Rigidbody>()) {
            gameObject.AddComponent<Rigidbody>();
        }

        this.GetComponent<Rigidbody>().isKinematic = true;

        ContextSetUpSimObjPhysics();

        BoxCollider col = MyColliders[0].GetComponent<BoxCollider>();
        BoxCollider meshbox = gameObject.transform.Find("mesh").GetComponent<BoxCollider>();
        col.center = meshbox.center;
        col.size = meshbox.size;

        MeshRenderer r = meshbox.GetComponent<MeshRenderer>();
        BoxCollider bb = BoundingBox.GetComponent<BoxCollider>();
        bb.center = r.bounds.center;
        bb.size = r.bounds.size * 1.1f;

        meshbox.enabled = false;

    }

    //[ContextMenu("Drawer")]
    void SetUpDrawer() {
        this.Type = SimObjType.Drawer;
        this.PrimaryProperty = SimObjPrimaryProperty.Static;
        this.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle, SimObjSecondaryProperty.CanOpen };

        ContextSetUpSimObjPhysics();

        // delete the trigger colliders and bounding box here
        // also probably edit ContextSetupSimObjPhysics to not create trigger colliders anymore

        if (!gameObject.GetComponent<CanOpen_Object>()) {
            gameObject.AddComponent<CanOpen_Object>();
        }
        CanOpen_Object coo = gameObject.GetComponent<CanOpen_Object>();

        GameObject[] myobject = new GameObject[] { gameObject };
        coo.MovingParts = myobject;
        coo.closedPositions = new Vector3[coo.MovingParts.Length];
        coo.openPositions = new Vector3[coo.MovingParts.Length];

        coo.closedPositions[0] = gameObject.transform.localPosition;
        coo.openPositions[0] = gameObject.transform.localPosition;

        List<GameObject> recepboxes = new List<GameObject>();

        foreach (Transform t in gameObject.transform) {
            if (t.name == "BoundingBox") {
                DestroyImmediate(t.gameObject, true);
                break;
            }

            if (t.name == "ReceptacleTriggerBox") {
                if (!recepboxes.Contains(t.gameObject)) {
                    recepboxes.Add(t.gameObject);
                }
            }

            // if(t.name == "Colliders")
            // {
            // 	if(!gameObject.transform.Find("TriggerColliders"))
            // 	{
            // 		GameObject inst = Instantiate(t.gameObject, gameObject.transform, true);
            // 		inst.name = "TriggerColliders";
            // 		foreach(Transform yes in inst.transform)
            // 		{
            // 			yes.GetComponent<BoxCollider>().isTrigger = true;
            // 		}
            // 	}

            // 	else
            // 	{
            // 		DestroyImmediate(gameObject.transform.Find("TriggerColliders").gameObject);
            // 		GameObject inst = Instantiate(t.gameObject, gameObject.transform, true);
            // 		inst.name = "TriggerColliders";
            // 		foreach(Transform yes in inst.transform)
            // 		{
            // 			yes.GetComponent<BoxCollider>().isTrigger = true;
            // 		}
            // 	}

            // }
        }

        ReceptacleTriggerBoxes = recepboxes.ToArray();

        // make trigger colliders, if there is "Colliders" already, duplicate them, parant to this object
        // then go through and set them all to istrigger.

    }

    //[ContextMenu("Find BoundingBox")]
    void ContextFindBoundingBox() {
        BoundingBox = gameObject.transform.Find("BoundingBox").gameObject;

    }

    //[ContextMenu("Set Up Microwave")]
    void ContextSetUpMicrowave() {
        this.Type = SimObjType.Microwave;
        this.PrimaryProperty = SimObjPrimaryProperty.Static;

        this.SecondaryProperties = new SimObjSecondaryProperty[2];
        this.SecondaryProperties[0] = SimObjSecondaryProperty.Receptacle;
        this.SecondaryProperties[1] = SimObjSecondaryProperty.CanOpen;

        if (!gameObject.transform.Find("BoundingBox")) {
            GameObject bb = new GameObject("BoundingBox");
            bb.transform.position = gameObject.transform.position;
            bb.transform.SetParent(gameObject.transform);
            bb.AddComponent<BoxCollider>();
            bb.GetComponent<BoxCollider>().enabled = false;
            bb.tag = "Untagged";
            bb.layer = 0;

            BoundingBox = bb;
        } else {
            BoundingBox = gameObject.transform.Find("BoundingBox").gameObject;
        }

        if (!gameObject.GetComponent<CanOpen_Object>()) {
            gameObject.AddComponent<CanOpen_Object>();
        }

        List<Transform> vplist = new List<Transform>();

        if (!gameObject.transform.Find("StaticVisPoints")) {
            GameObject svp = new GameObject("StaticVisPoints");
            svp.transform.position = gameObject.transform.position;
            svp.transform.SetParent(gameObject.transform);

            GameObject vp = new GameObject("vPoint");
            vp.transform.position = svp.transform.position;
            vp.transform.SetParent(svp.transform);
        } else {
            Transform vp = gameObject.transform.Find("StaticVisPoints");
            foreach (Transform child in vp) {
                vplist.Add(child);

                // set correct tag and layer for each object
                child.gameObject.tag = "Untagged";
                child.gameObject.layer = 8;
            }
        }

        Transform door = gameObject.transform.Find("Door");
        if (!door.Find("Col")) {
            GameObject col = new GameObject("Col");
            col.transform.position = door.transform.position;
            col.transform.SetParent(door.transform);

            col.AddComponent<BoxCollider>();

            col.transform.tag = "SimObjPhysics";
            col.layer = 8;

        }

        if (!door.Find("VisPoints")) {
            // empty to hold all visibility points
            GameObject vp = new GameObject("VisPoints");
            vp.transform.position = door.transform.position;
            vp.transform.SetParent(door.transform);

            // create first Visibility Point to work with
            GameObject vpc = new GameObject("vPoint");
            vpc.transform.position = vp.transform.position;
            vpc.transform.SetParent(vp.transform);
        } else {
            Transform vp = door.Find("VisPoints");
            foreach (Transform child in vp) {
                vplist.Add(child);

                // set correct tag and layer for each object
                child.gameObject.tag = "Untagged";
                child.gameObject.layer = 8;
            }
        }

        VisibilityPoints = vplist.ToArray();

        CanOpen_Object coo = gameObject.GetComponent<CanOpen_Object>();
        coo.MovingParts = new GameObject[] { door.transform.gameObject };
        coo.openPositions = new Vector3[] { new Vector3(0, 90, 0) };
        coo.closedPositions = new Vector3[] { Vector3.zero };
        coo.SetMovementToRotate();

        if (gameObject.transform.Find("ReceptacleTriggerBox")) {
            GameObject[] rtb = new GameObject[] { gameObject.transform.Find("ReceptacleTriggerBox").transform.gameObject };
            ReceptacleTriggerBoxes = rtb;
        }
    }

    //[ContextMenu("Static Mesh Collider with Receptacle")]
    void SetUpSimObjWithStaticMeshCollider() {
        if (this.Type == SimObjType.Undefined || this.PrimaryProperty == SimObjPrimaryProperty.Undefined) {
            Debug.Log("Type / Primary Property is missing");
            return;
        }
        // set up this object ot have the right tag and layer
        gameObject.tag = "SimObjPhysics";
        gameObject.layer = 8;

        if (!gameObject.GetComponent<Rigidbody>()) {
            gameObject.AddComponent<Rigidbody>();
        }

        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        if (!gameObject.transform.Find("VisibilityPoints")) {
            // empty to hold all visibility points
            GameObject vp = new GameObject("VisibilityPoints");
            vp.transform.position = gameObject.transform.position;
            vp.transform.SetParent(gameObject.transform);

            // create first Visibility Point to work with
            GameObject vpc = new GameObject("vPoint");
            vpc.transform.position = vp.transform.position;
            vpc.transform.SetParent(vp.transform);
        }

        if (!gameObject.transform.Find("BoundingBox")) {
            GameObject rac = new GameObject("BoundingBox");
            rac.transform.position = gameObject.transform.position;
            rac.transform.SetParent(gameObject.transform);
            rac.AddComponent<BoxCollider>();
            rac.GetComponent<BoxCollider>().enabled = false;
        }

        List<GameObject> rtbList = new List<GameObject>();

        foreach (Transform t in gameObject.transform) {
            if (t.GetComponent<MeshCollider>()) {
                t.gameObject.tag = "SimObjPhysics";
                t.gameObject.layer = 8;

                // now check if it has pillows or something?
                foreach (Transform yes in t) {
                    if (yes.GetComponent<MeshCollider>()) {
                        yes.gameObject.tag = "SimObjPhysics";
                        yes.gameObject.layer = 8;
                    }
                }
            }

            if (t.GetComponent<Contains>()) {
                rtbList.Add(t.gameObject);
            }
        }

        ReceptacleTriggerBoxes = rtbList.ToArray();

        ContextSetUpVisibilityPoints();
        ContextSetUpBoundingBox();
    }

    //[UnityEditor.MenuItem("SimObjectPhysics/AppleSlice")]
    public static void ContextSetupAppleSlice() {
        GameObject prefabRoot = Selection.activeGameObject;
        GameObject c = new GameObject("AppleSlice");
        c.transform.position = prefabRoot.transform.position;
        // prefabRoot.transform.position = c.transform.position;
        prefabRoot.transform.SetParent(c.transform);
        prefabRoot.name = "Mesh";

        if (!c.GetComponent<SimObjPhysics>()) {
            c.AddComponent<SimObjPhysics>();
        }

        if (c.GetComponent<SimObjPhysics>()) {
            SimObjPhysics sop = c.GetComponent<SimObjPhysics>();
            sop.PrimaryProperty = SimObjPrimaryProperty.CanPickup;
            sop.Type = SimObjType.AppleSliced;
            // sop.SecondaryProperties = new SimObjSecondaryProperty[] {SimObjSecondaryProperty};
        }
        c.tag = "SimObjPhysics";
        c.layer = 8;

        if (!c.GetComponent<Rigidbody>()) {
            c.AddComponent<Rigidbody>();
        }

        if (!c.transform.Find("Colliders")) {
            GameObject col = new GameObject("Colliders");
            col.transform.position = c.transform.position;
            col.transform.SetParent(c.transform);

            GameObject cc = new GameObject("Col");
            cc.transform.position = col.transform.position;
            cc.transform.SetParent(col.transform);
            cc.AddComponent<BoxCollider>();
            cc.tag = "SimObjPhysics";
            cc.layer = 8;
        }

        if (!c.transform.Find("VisibilityPoints")) {
            // empty to hold all visibility points
            GameObject vp = new GameObject("VisibilityPoints");
            vp.transform.position = c.transform.position;
            vp.transform.SetParent(c.transform);

            // create first Visibility Point to work with
            GameObject vpc = new GameObject("vPoint");
            vpc.transform.position = vp.transform.position;
            vpc.transform.SetParent(vp.transform);
        }

        if (!c.transform.Find("BoundingBox")) {
            GameObject rac = new GameObject("BoundingBox");
            rac.transform.position = c.transform.position;
            rac.transform.SetParent(c.transform);
            rac.AddComponent<BoxCollider>();
            rac.GetComponent<BoxCollider>().enabled = false;
        }

        // c.GetComponent<SimObjPhysics>().AppleSetupReferences();
    }

    //[UnityEditor.MenuItem("SimObjectPhysics/LightSwitch")]
    public static void ContextSetupLightSwitch() {
        GameObject prefabRoot = Selection.activeGameObject;
        GameObject c = new GameObject("LightSwitch");
        c.transform.position = prefabRoot.transform.position;
        prefabRoot.transform.SetParent(c.transform);
        prefabRoot.name = "Mesh";

        if (!c.GetComponent<SimObjPhysics>()) {
            c.AddComponent<SimObjPhysics>();
        }

        if (c.GetComponent<SimObjPhysics>()) {
            SimObjPhysics sop = c.GetComponent<SimObjPhysics>();
            sop.PrimaryProperty = SimObjPrimaryProperty.Static;
            sop.Type = SimObjType.LightSwitch;
            sop.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.CanToggleOnOff };
        }

        c.tag = "SimObjPhysics";
        c.layer = 8;
        c.isStatic = true;

        if (!c.GetComponent<Rigidbody>()) {
            Rigidbody rb = c.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        if (!c.transform.Find("Colliders")) {
            GameObject col = new GameObject("Colliders");
            col.transform.position = c.transform.position;
            col.transform.SetParent(c.transform);

            GameObject cc = new GameObject("Col");
            cc.transform.position = col.transform.position;
            cc.transform.SetParent(col.transform);
            cc.AddComponent<BoxCollider>();
            cc.tag = "SimObjPhysics";
            cc.layer = 8;
        }

        if (!c.transform.Find("VisibilityPoints")) {
            // empty to hold all visibility points
            GameObject vp = new GameObject("VisibilityPoints");
            vp.transform.position = c.transform.position;
            vp.transform.SetParent(c.transform);

            // create first Visibility Point to work with
            GameObject vpc = new GameObject("vPoint");
            vpc.transform.position = vp.transform.position;
            vpc.transform.SetParent(vp.transform);
        }

        c.GetComponent<SimObjPhysics>().SetupCollidersVisPoints();

        // add the CanToggleOnOff component and set it up with correct values
        if (!c.GetComponent<CanToggleOnOff>()) {
            CanToggleOnOff ctoo = c.AddComponent<CanToggleOnOff>();

            List<GameObject> childmeshes = new List<GameObject>();
            List<Vector3> childRotations = new List<Vector3>();

            foreach (Transform t in c.transform) {
                if (t.name == "Mesh") {
                    foreach (Transform tt in t) {
                        childmeshes.Add(tt.gameObject);
                        childRotations.Add(tt.transform.localEulerAngles);
                    }
                }
            }

            ctoo.MovingParts = childmeshes.ToArray();
            ctoo.OnPositions = childRotations.ToArray();
            ctoo.OffPositions = new Vector3[ctoo.MovingParts.Length];
            ctoo.SetMovementToRotate();
        }
    }


    [ContextMenu("Setup Colliders, VisPoints, and Bounding Box")]
    public void SetupCollidersVisPoints() {
        ContextSetUpColliders();
        ContextSetUpVisibilityPoints();
        ContextSetUpBoundingBox();
    }

    //[UnityEditor.MenuItem("SimObjectPhysics/Toaster")]
    public static void ContextSetupToaster() {
        GameObject prefabRoot = Selection.activeGameObject;
        GameObject c = new GameObject("Toaster_");
        c.transform.position = prefabRoot.transform.position;
        // prefabRoot.transform.position = c.transform.position;
        prefabRoot.transform.SetParent(c.transform);
        prefabRoot.name = "Mesh";

        if (!c.GetComponent<SimObjPhysics>()) {
            c.AddComponent<SimObjPhysics>();
        }

        if (c.GetComponent<SimObjPhysics>()) {
            SimObjPhysics sop = c.GetComponent<SimObjPhysics>();
            sop.PrimaryProperty = SimObjPrimaryProperty.Static;
            sop.Type = SimObjType.Toaster;
            // sop.SecondaryProperties = new SimObjSecondaryProperty[] {SimObjSecondaryProperty.CanBeSliced};
        }

        c.tag = "SimObjPhysics";
        c.layer = 8;

        if (!c.GetComponent<Rigidbody>()) {
            c.AddComponent<Rigidbody>();
        }

        if (!c.transform.Find("Colliders")) {
            GameObject col = new GameObject("Colliders");
            col.transform.position = c.transform.position;
            col.transform.SetParent(c.transform);

            GameObject cc = new GameObject("Col");
            cc.transform.position = col.transform.position;
            cc.transform.SetParent(col.transform);
            cc.AddComponent<BoxCollider>();
            cc.tag = "SimObjPhysics";
            cc.layer = 8;
        }

        if (!c.transform.Find("VisibilityPoints")) {
            // empty to hold all visibility points
            GameObject vp = new GameObject("VisibilityPoints");
            vp.transform.position = c.transform.position;
            vp.transform.SetParent(c.transform);

            // create first Visibility Point to work with
            GameObject vpc = new GameObject("vPoint");
            vpc.transform.position = vp.transform.position;
            vpc.transform.SetParent(vp.transform);
        }

        if (!c.transform.Find("BoundingBox")) {
            GameObject rac = new GameObject("BoundingBox");
            rac.transform.position = c.transform.position;
            rac.transform.SetParent(c.transform);
            rac.AddComponent<BoxCollider>();
            rac.GetComponent<BoxCollider>().enabled = false;
        }

        c.GetComponent<SimObjPhysics>().ToasterSetupReferences();
    }

    //[ContextMenu("Toaster Setup References")]
    void ToasterSetupReferences() {
        ContextSetUpColliders();
        ContextSetUpVisibilityPoints();
        ContextSetUpBoundingBox();

        foreach (Transform t in gameObject.transform) {
            if (t.name == "Colliders") {
                if (!gameObject.transform.Find("TriggerColliders")) {
                    GameObject inst = Instantiate(t.gameObject, gameObject.transform, true);
                    inst.name = "TriggerColliders";
                    foreach (Transform yes in inst.transform) {
                        yes.GetComponent<Collider>().isTrigger = true;
                    }
                } else {
                    DestroyImmediate(gameObject.transform.Find("TriggerColliders").gameObject);
                    GameObject inst = Instantiate(t.gameObject, gameObject.transform, true);
                    inst.name = "TriggerColliders";
                    foreach (Transform yes in inst.transform) {
                        yes.GetComponent<Collider>().isTrigger = true;
                    }
                }
            }
        }
    }

    [ContextMenu("Setup")]
    public void ContextSetUpSimObjPhysics() {
        if (this.Type == SimObjType.Undefined || this.PrimaryProperty == SimObjPrimaryProperty.Undefined) {
            Debug.Log("Type / Primary Property is missing");
            return;
        }
        // set up this object ot have the right tag and layer
        gameObject.tag = "SimObjPhysics";
        gameObject.layer = 8;

        if (!gameObject.GetComponent<Rigidbody>()) {
            gameObject.AddComponent<Rigidbody>();
        }

        gameObject.GetComponent<Rigidbody>().isKinematic = true;

        if (!gameObject.transform.Find("Colliders")) {
            GameObject c = new GameObject("Colliders");
            c.transform.position = gameObject.transform.position;
            c.transform.SetParent(gameObject.transform);
            c.transform.localEulerAngles = Vector3.zero;

            GameObject cc = new GameObject("Col");
            cc.transform.position = c.transform.position;
            cc.transform.SetParent(c.transform);
            cc.transform.localEulerAngles = Vector3.zero;
            // cc.AddComponent<CapsuleCollider>();
            cc.AddComponent<BoxCollider>();
            cc.tag = "SimObjPhysics";
            cc.layer = 8;
        }

        if (!gameObject.transform.Find("VisibilityPoints")) {
            // empty to hold all visibility points
            GameObject vp = new GameObject("VisibilityPoints");
            vp.transform.position = gameObject.transform.position;
            vp.transform.SetParent(gameObject.transform);
            vp.transform.localEulerAngles = Vector3.zero;

            // create first Visibility Point to work with
            GameObject vpc = new GameObject("vPoint");
            vpc.transform.position = vp.transform.position;
            vpc.transform.SetParent(vp.transform);
            vpc.transform.localEulerAngles = Vector3.zero;
        }

        if (!gameObject.transform.Find("BoundingBox")) {
            GameObject rac = new GameObject("BoundingBox");
            rac.transform.position = gameObject.transform.position;
            rac.transform.SetParent(gameObject.transform);
            rac.transform.localEulerAngles = Vector3.zero;
            rac.AddComponent<BoxCollider>();
            rac.GetComponent<BoxCollider>().enabled = false;
            BoundingBox = rac;
        }

        List<GameObject> recepboxes = new List<GameObject>();

        foreach (Transform t in gameObject.transform) {
            // add any receptacle trigger boxes
            if (t.GetComponent<Contains>()) {
                if (!recepboxes.Contains(t.gameObject)) {
                    recepboxes.Add(t.gameObject);
                }
            }

            if (t.name == "Colliders") {
                if (!gameObject.transform.Find("TriggerColliders")) {
                    GameObject inst = Instantiate(t.gameObject, gameObject.transform, true);
                    inst.name = "TriggerColliders";
                    foreach (Transform yes in inst.transform) {
                        yes.GetComponent<Collider>().isTrigger = true;
                    }
                } else {
                    DestroyImmediate(gameObject.transform.Find("TriggerColliders").gameObject);
                    GameObject inst = Instantiate(t.gameObject, gameObject.transform, true);
                    inst.name = "TriggerColliders";
                    foreach (Transform yes in inst.transform) {
                        yes.GetComponent<Collider>().isTrigger = true;
                    }
                }
            }

            // check if child object "t" has any objects under it called "Colliders"
            if (t.Find("Colliders")) {
                Transform childColliderObject = t.Find("Colliders");

                // if TriggerColliders dont already exist as a child under this child object t, create it by copying childColliderObject
                if (!t.Find("TriggerColliders")) {
                    GameObject inst = Instantiate(childColliderObject.gameObject, t, true);
                    inst.name = "TriggerColliders";
                    foreach (Transform thing in inst.transform) {
                        thing.GetComponent<Collider>().isTrigger = true;
                    }
                }
            }
        }

        ReceptacleTriggerBoxes = recepboxes.ToArray();

        ContextSetUpColliders();
        // ContextSetUpTriggerColliders();
        ContextSetUpVisibilityPoints();
        // ContextSetUpInteractionPoints();
        ContextSetUpBoundingBox();
    }



    //[ContextMenu("Set Up Colliders")]
    public void ContextSetUpColliders() {
        List<Collider> listColliders = new List<Collider>();

        if (transform.Find("Colliders")) {
            Transform Colliders = transform.Find("Colliders");

            foreach (Transform child in Colliders) {
                // list.toarray
                listColliders.Add(child.GetComponent<Collider>());

                // set correct tag and layer for each object
                // also ensure all colliders are NOT trigger
                child.gameObject.tag = "SimObjPhysics";
                child.gameObject.layer = 8;

                if (child.GetComponent<Collider>()) {
                    child.GetComponent<Collider>().enabled = true;
                    child.GetComponent<Collider>().isTrigger = false;
                }
            }
        }

        // loop through all child objects. For each object, check if the child itself has a child called Colliders....
        foreach (Transform child in transform) {
            if (child.Find("Colliders") && !child.GetComponent<SimObjPhysics>()) {
                Transform Colliders = child.Find("Colliders");

                foreach (Transform childschild in Colliders) {
                    // list.toarray
                    listColliders.Add(childschild.GetComponent<Collider>());

                    // set correct tag and layer for each object
                    // also ensure all colliders are NOT trigger
                    childschild.gameObject.tag = "SimObjPhysics";
                    childschild.gameObject.layer = 8;

                    if (childschild.GetComponent<Collider>()) {
                        childschild.GetComponent<Collider>().enabled = true;
                        childschild.GetComponent<Collider>().isTrigger = false;
                    }
                }
            }
        }

        MyColliders = listColliders.ToArray();

    }

    //[ContextMenu("Set Up TriggerColliders")]
    void ContextSetUpTriggerColliders() {
        if (transform.Find("TriggerColliders")) {
            Transform tc = transform.Find("TriggerColliders");

            List<GameObject> listtc = new List<GameObject>();

            foreach (Transform child in tc) {
                // list.toarray
                listtc.Add(child.gameObject);

                // set correct tag and layer for each object
                // also ensure all colliders are set to trigger
                child.gameObject.tag = "SimObjPhysics";
                child.gameObject.layer = 8;

                if (child.GetComponent<Collider>()) {
                    child.GetComponent<Collider>().enabled = true;
                    child.GetComponent<Collider>().isTrigger = true;
                }

            }

            // MyTriggerColliders = listtc.ToArray();
        }
    }

    // [ContextMenu("Set Up VisibilityPoints")]
    public void ContextSetUpVisibilityPoints() {
        List<Transform> vplist = new List<Transform>();

        if (transform.Find("VisibilityPoints")) {
            Transform vp = transform.Find("VisibilityPoints");

            foreach (Transform child in vp) {
                vplist.Add(child);

                // set correct tag and layer for each object
                child.gameObject.tag = "Untagged";
                child.gameObject.layer = 8;
            }
        }

        foreach (Transform child in transform) {
            if (child.Find("VisibilityPoints") && !child.GetComponent<SimObjPhysics>()) {
                Transform vp = child.Find("VisibilityPoints");

                foreach (Transform childschild in vp) {
                    vplist.Add(childschild);
                    childschild.gameObject.tag = "Untagged";
                    childschild.gameObject.layer = 8;
                }
            }
        }

        VisibilityPoints = vplist.ToArray();
    }
#endif

    //[ContextMenu("Set Up Bounding Box")]
    public void ContextSetUpBoundingBox(bool forceCacheReset = false) {
        Vector3[] transformSaver = new Vector3[] { transform.position, transform.eulerAngles };

        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;

        if (BoundingBox == null) {
            GameObject BoundingBox = new GameObject("BoundingBox");
            BoundingBox.transform.parent = gameObject.transform;
            BoundingBox.transform.localPosition = Vector3.zero;
            BoundingBox.transform.localEulerAngles = Vector3.zero;
            BoundingBox.transform.localScale = Vector3.one;
        }

        BoundingBox.transform.localScale = Vector3.one;// make sure to default existing BoundingBox to 1 as well

        // This collider is used as a size reference for the Agent's Rotation checking boxes, so it does not need
        // to be enabled. To ensure this doesn't interact with anything else, set the Tag to Untagged, the layer to 
        // SimObjInvisible, and disable this component. Component values can still be accessed if the component itself
        // is not enabled.
        BoundingBox.tag = "Untagged";
        BoundingBox.layer = 9; // layer 9 - SimObjInvisible

        if (!Physics.autoSyncTransforms) {
            Physics.SyncTransforms();
        }

        Collider[] colliders = transform.GetComponentsInChildren<Collider>().Where(
            c => c.enabled && !c.isTrigger
        ).ToArray();
        MeshFilter[] meshes = transform.GetComponentsInChildren<MeshFilter>();

        // SkinnedMeshRenderer[] skinnedMeshes = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
        // Transform rootBoneParentSaver;
        // Transform rootBoneSurrogateParent = new GameObject().transform;

        // Reset existing Bounding Box
        if (BoundingBox.GetComponent<BoxCollider>()) {
            BoundingBox.GetComponent<BoxCollider>().enabled = true;
            BoundingBox.GetComponent<BoxCollider>().center = colliders[0].bounds.center;
            BoundingBox.GetComponent<BoxCollider>().size = Vector3.zero;
        }

        Bounds newBoundingBox = UtilityFunctions.CreateEmptyBounds();
        Vector3 minMeshXZ = colliders[0].bounds.center;
        Vector3 maxMeshXYZ = colliders[0].bounds.center;

        // Encapsulate all colliders
        foreach (Collider collider in colliders) {
            newBoundingBox.Encapsulate(collider.gameObject.GetComponent<Collider>().bounds);
        }

        // Encapsulate all mesh filters (used instead of mesh renderers because you can sample individual vertex ids with the filters)
        // Excluded min-y because my material ID triangles are all located way below their respective main-meshes
        // newBoundingBox.Encapsulate(meshGroup.GetComponent<meshFilter>().mesh.bounds.min);
        foreach (MeshFilter meshFilter in meshes) {
            // if (meshFilter.gameObject.name != "screen_1" && meshFilter.gameObject.name != "screen_2")
            //{
            foreach (Vector3 vertex in meshFilter.sharedMesh.vertices) {
                if (minMeshXZ.x > meshFilter.gameObject.transform.TransformPoint(vertex).x) {
                    minMeshXZ.x = meshFilter.gameObject.transform.TransformPoint(vertex).x;
                }

                if (minMeshXZ.z > meshFilter.gameObject.transform.TransformPoint(vertex).z) {
                    minMeshXZ.z = meshFilter.gameObject.transform.TransformPoint(vertex).z;
                }

                if (maxMeshXYZ.x < meshFilter.gameObject.transform.TransformPoint(vertex).x) {
                    maxMeshXYZ.x = meshFilter.gameObject.transform.TransformPoint(vertex).x;
                }

                if (maxMeshXYZ.y < meshFilter.gameObject.transform.TransformPoint(vertex).y) {
                    maxMeshXYZ.y = meshFilter.gameObject.transform.TransformPoint(vertex).y;
                }

                if (maxMeshXYZ.z < meshFilter.gameObject.transform.TransformPoint(vertex).z) {
                    maxMeshXYZ.z = meshFilter.gameObject.transform.TransformPoint(vertex).z;
                }

                newBoundingBox.Encapsulate(minMeshXZ);
                newBoundingBox.Encapsulate(maxMeshXYZ);
            }
            //}
        }

        // Encapsulate all skinned mesh renderers (requires moving the bones, not the skinned mesh renderers)
        // foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
        //{
        //    skinnedMesh.updateWhenOffscreen = true;

        //    rootBoneParentSaver = skinnedMesh.rootBone.parent;

        //    rootBoneSurrogateParent.position = rootBoneParentSaver.position;
        //    rootBoneSurrogateParent.rotation = rootBoneParentSaver.rotation;
        //    skinnedMesh.rootBone.transform.SetParent(rootBoneSurrogateParent);

        //    rootBoneSurrogateParent.transform.position = Vector3.zero;
        //    rootBoneSurrogateParent.transform.rotation = Quaternion.identity;

        //    newBoundingBox.Encapsulate(skinnedMesh.bounds.min);
        //    newBoundingBox.Encapsulate(skinnedMesh.bounds.max);

        //    rootBoneSurrogateParent.position = rootBoneParentSaver.position;
        //    rootBoneSurrogateParent.rotation = rootBoneParentSaver.rotation;
        //    skinnedMesh.rootBone.SetParent(rootBoneParentSaver);
        //}

        // DestroyImmediate(rootBoneSurrogateParent.gameObject);

        // Assign new bounds to BoundingBox
        // Debug.Log("Min/max of BoundingBox: " + newBoundingBox.min + ", " + newBoundingBox.max);
        BoundingBox.GetComponent<BoxCollider>().center = newBoundingBox.center;
        // Set Bounding Box Buffer Here!!!
        BoundingBox.GetComponent<BoxCollider>().size = newBoundingBox.size + new Vector3(0.01f, 0.01f, 0.01f);
        BoundingBox.GetComponent<BoxCollider>().enabled = false;

        // var currentBoundingBox = currentGameObject.transform.Find("BoundingBox").GetComponent<BoxCollider>();
        // currentBoundingBox.size = currentGameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size + new Vector3(0.2f, 0.2f, 0.2f);
        // currentBoundingBox.center = currentGameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center

        transform.position = transformSaver[0];
        transform.eulerAngles = transformSaver[1];

        if (forceCacheReset) {
            this.syncBoundingBoxes(forceCacheReset: true);
        }
    }


    class BoundingBoxCacheKey {
        public Vector3 position;
        public Quaternion rotation;
        public bool IsOpen;
        public bool IsBroken;
        public bool IsSliced;
        public float openness;
    }
}
