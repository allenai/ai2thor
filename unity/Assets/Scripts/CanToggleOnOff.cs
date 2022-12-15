using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// serialized class so it can show up in Inspector Window
[System.Serializable]
public class SwapObjList {
    // reference to game object that needs to have materials changed
    [Header("Object That Needs Mat Swaps")]
    [SerializeField]
    public GameObject MyObject;

    // copy the Materials array on MyObject's Renderer component here
    [Header("Materials for On state")]
    [SerializeField]
    public Material[] OnMaterials;

    // swap to this array of materials when off, usually just one or two materials will change
    [Header("Materials for Off state")]
    [SerializeField]
    public Material[] OffMaterials;

}

public class CanToggleOnOff : MonoBehaviour {
    // the array of moving parts and lightsources will correspond with each other based on their 
    // position in the array

    // these are any switches, dials, levers, etc that should change position or orientation when toggle on or off
    [Header("Moving Parts (switches, dials, etc")]
    [SerializeField]
    public GameObject[] MovingParts;

    // Meshes that require different materials when in on/off state
    [Header("Objects that need Mat Swaps")]
    [SerializeField]
    public SwapObjList[] MaterialSwapObjects;

    // Light emitting objects that must be toggled enabled/disabled.
    [Header("Light Source to Enable or Disable")]
    [SerializeField]
    public Light[] LightSources;

    //stuff like particles or other rendered objects to toggle (ex: flame particles, renderers, heat trigger colliders-Microwaves)
    [Header("Effects/Objects to Enable or Disable")]
    [SerializeField]
    public GameObject[] effects;

    [Header("Animation Parameters")]

    // rotations or translations for the MovingParts when On
    [SerializeField]
    public Vector3[] OnPositions;

    // rotations or translations for the MovingParts when off
    [SerializeField]
    public Vector3[] OffPositions;

    //[SerializeField]
    public float animationTime = 0.05f;

    // use this to set the default state of this object. Lightswitches should be default on, things like
    // microwaves should be default off etc.
    [SerializeField]
    public bool isOn = true;

    private bool isCurrentlyLerping = false;

    protected enum MovementType { Slide, Rotate };

    [SerializeField]
    protected MovementType movementType;

    // keep a list of all objects that can turn on, but must be in the closed state before turning on (ie: can't microwave an object with the door open!)
    protected List<SimObjType> MustBeClosedToTurnOn = new List<SimObjType>()
    {SimObjType.Microwave};

    // if this object controls the on/off state of ONLY itself, set to true (lamps, laptops, etc.)
    // if this object's on/off state is not controlled by itself, but instead controlled by another sim object (ex: stove burner is controlled by the stove knob) set this to false
    [SerializeField]
    protected bool SelfControlled = true;

    // reference to any sim objects that this object will turn on/off by proxy (ie: stove burner knob will toggle on/off state of its stove burner)
    [SerializeField]
    protected SimObjPhysics[] ControlledSimObjects;

    // return this for metadata check to see if this object is Toggleable or not
    // specifically, stove burners should not be Toggleable, but they can return 'isToggled' because they can be Toggled on by
    // another sim object, the stove knob.
    // stove knob: returns toggleable, returns istoggled
    // stove burner: only returns istoggled
    public bool ReturnSelfControlled() {
        return SelfControlled;
    }

    // returns references to all sim objects this object toggles the on/off state of. For example all stove knobs can
    // return which burner they control with this
    public SimObjPhysics[] ReturnControlledSimObjects() {
        return ControlledSimObjects;
    }

    public List<SimObjType> ReturnMustBeClosedToTurnOn() {
        return MustBeClosedToTurnOn;
    }

    public bool isTurnedOnOrOff() {
        return isOn;
    }
    // Helper functions for setting up scenes, only for use in Editor
#if UNITY_EDITOR
    public void SetMovementToSlide() {
        movementType = MovementType.Slide;
    }

    public void SetMovementToRotate() {
        movementType = MovementType.Rotate;
    }

#endif

    // Use this for initialization
    void Start() {

        //setLightSourcesNames();

#if UNITY_EDITOR
        if (!this.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanToggleOnOff)) {
            Debug.LogError(this.name + "is missing the CanToggleOnOff Secondary Property! Please set it!");
        }
#endif
    }

    //set light source names to naming scheme: {objectID}|{Type}|{instance}
    //used to set child light source names
    // public void setLightSourcesNames () {
    //     for (int i = 0; i < LightSources.Length; i++ ) {
    //         Light actualLightCauseSometimesTheseAreNested = LightSources[i].GetComponentInChildren<Light>();
    //         actualLightCauseSometimesTheseAreNested.name = 
    //         this.GetComponent<SimObjPhysics>().objectID + "|" + LightSources[i].GetComponentInChildren<Light>().type.ToString()+ "|" + i.ToString();
    //     }
    // }

    // Update is called once per frame
    void Update() {
        // // test if it can open without Agent Command - Debug Purposes
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            Toggle();
        }
        #endif
    }

    public void Toggle() {
        // if this object is controlled by another object, do nothing and report failure?
        if (!SelfControlled) {
            return;
        }
        
        isCurrentlyLerping = true;
        // check if there are moving parts
        // check if there are lights/materials etc to swap out
        if (!isOn) {
            if (MovingParts.Length > 0) {
                for (int i = 0; i < MovingParts.Length; i++) {
                    if (movementType == MovementType.Slide) {
                        StartCoroutine(LerpPosition(
                            movingParts: MovingParts,
                            offLocalPositions: OffPositions,
                            onLocalPositions: OnPositions,
                            initialOpenness: 0,
                            desiredOpenness: 1,
                            animationTime: animationTime
                        ));
                    }

                    else if (movementType == MovementType.Rotate) { 
                        StartCoroutine(LerpRotation(
                            movingParts: MovingParts,
                            offLocalRotations: OffPositions,
                            onLocalRotations: OnPositions,
                            initialOpenness: 0,
                            desiredOpenness: 1,
                            animationTime: animationTime
                        ));
                    }
                }
            }

            setisOn();
        } else {
            if (MovingParts.Length > 0) {
                for (int i = 0; i < MovingParts.Length; i++) {
                    if (movementType == MovementType.Slide) {
                        StartCoroutine(LerpPosition(
                            movingParts: MovingParts,
                            offLocalPositions: OffPositions,
                            onLocalPositions: OnPositions,
                            initialOpenness: 1,
                            desiredOpenness: 0,
                            animationTime: animationTime
                        ));
                    }

                    else if (movementType == MovementType.Rotate) { 
                        StartCoroutine(LerpRotation(
                            movingParts: MovingParts,
                            offLocalRotations: OffPositions,
                            onLocalRotations: OnPositions,
                            initialOpenness: 1,
                            desiredOpenness: 0,
                            animationTime: animationTime
                        ));
                    }                    
                }
            }

            setisOn();
        }
        isCurrentlyLerping = false;
    }

    // toggle isOn variable, swap Materials and enable/disable Light sources
    private void setisOn() {
        // if isOn true, set it to false and also turn off all lights/deactivate materials
        if (isOn) {
            if (LightSources.Length > 0) {
                for (int i = 0; i < LightSources.Length; i++) {
                    LightSources[i].transform.gameObject.SetActive(false);
                }
            }

            if(effects.Length > 0) {
                for (int i = 0; i< effects.Length; i++) {
                    effects[i].SetActive(false);
                }
            }

            if (MaterialSwapObjects.Length > 0) {
                for (int i = 0; i < MaterialSwapObjects.Length; i++) {
                    MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials =
                    MaterialSwapObjects[i].OffMaterials;
                }
            }

            // also set any objects this object controlls to the off state
            if (ControlledSimObjects.Length > 0) {
                foreach (SimObjPhysics sop in ControlledSimObjects) {
                    sop.GetComponent<CanToggleOnOff>().isOn = false;
                }
            }

            isOn = false;
        }

        // if isOn false, set to true and then turn ON all lights and activate material swaps
        else {
            if (LightSources.Length > 0) {
                for (int i = 0; i < LightSources.Length; i++) {
                    LightSources[i].transform.gameObject.SetActive(true);
                }
            }

            if(effects.Length > 0) {
                for (int i = 0; i< effects.Length; i++) {
                    effects[i].SetActive(true);
                }
            }

            if (MaterialSwapObjects.Length > 0) {
                for (int i = 0; i < MaterialSwapObjects.Length; i++) {
                    MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials =
                    MaterialSwapObjects[i].OnMaterials;
                }
            }

            // also set any objects this object controlls to the on state
            if (ControlledSimObjects.Length > 0) {
                foreach (SimObjPhysics sop in ControlledSimObjects) {
                    sop.GetComponent<CanToggleOnOff>().isOn = true;
                }
            }

            isOn = true;
        }
    }

    private protected IEnumerator LerpPosition(
        GameObject[] movingParts,
        Vector3[] offLocalPositions,
        Vector3[] onLocalPositions,
        float initialOpenness,
        float desiredOpenness,
        float animationTime
    ) {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime) {
            elapsedTime += Time.fixedDeltaTime;
            float currentOpenness = Mathf.Clamp(
                initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime),
                Mathf.Min(initialOpenness, desiredOpenness),
                Mathf.Max(initialOpenness, desiredOpenness));
            for (int i = 0; i < movingParts.Length; i++) {
                movingParts[i].transform.localPosition = Vector3.Lerp(offLocalPositions[i], onLocalPositions[i], currentOpenness);
            }
        }
        yield break;
    }
    private protected IEnumerator LerpRotation(
        GameObject[] movingParts,
        Vector3[] offLocalRotations,
        Vector3[] onLocalRotations,
        float initialOpenness,
        float desiredOpenness,
        float animationTime
    ) {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime) {
            elapsedTime += Time.fixedDeltaTime;
            float currentOpenness = Mathf.Clamp(
                initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime),
                Mathf.Min(initialOpenness, desiredOpenness),
                Mathf.Max(initialOpenness, desiredOpenness));
            for (int i = 0; i < MovingParts.Length; i++) {
                    MovingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(offLocalRotations[i]), Quaternion.Euler(onLocalRotations[i]), currentOpenness);
                }
        }
        yield break;
    }
    public bool GetIsCurrentlyLerping() {
        if (this.isCurrentlyLerping) {
            return true;
        }
        else {
            return false;
        }
    }
    
    // [ContextMenu("Get On-Off Materials")]
    // void ContextOnOffMaterials()
    // {
    // 	foreach (SwapObjList swap in MaterialSwapObjects)
    // 	{
    // 		List<Material> list = 
    // 		new List<Material>(swap.MyObject.GetComponent<MeshRenderer>().sharedMaterials);

    // 		// print(swap.MyObject.name);
    // 		Material[] objectMats = list.ToArray();// swap.MyObject.GetComponent<MeshRenderer>().sharedMaterials;

    // 		// foreach (Material m in objectMats)
    // 		// {
    // 		// 	// print(m.name);
    // 		// }

    // 		swap.OnMaterials = objectMats;
    // 		swap.OffMaterials = objectMats;
    // 	}
    // }
}
