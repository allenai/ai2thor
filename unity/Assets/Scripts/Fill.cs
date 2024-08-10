using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Fill : MonoBehaviour {
    [SerializeField]
    protected GameObject WaterObject = null;

    [SerializeField]
    protected GameObject CoffeeObject = null;

    [SerializeField]
    protected GameObject WineObject = null;

    [SerializeField]
    protected bool isFilled = false; // false - empty, true - currently filled with

    protected string currentlyFilledWith = null;

    public Dictionary<string, GameObject> Liquids = new Dictionary<string, GameObject>();

    public bool IsFilled() {
        return isFilled;
    }

    public string FilledLiquid() {
        return currentlyFilledWith;
    }

    void Awake() {
#if UNITY_EDITOR
        if (
            !gameObject
                .GetComponent<SimObjPhysics>()
                .DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeFilled)
        ) {
            Debug.LogError(gameObject.name + " is missing the CanBeFilled secondary property!");
        }
#endif

        Liquids.Add("water", WaterObject);
        Liquids.Add("coffee", CoffeeObject);
        Liquids.Add("wine", WineObject);
    }

    // Update is called once per frame
    void Update() {
        // check if the object is rotated too much, if so it should spill out
        if (Vector3.Angle(gameObject.transform.up, Vector3.up) > 90) {
            // print("spilling!");
            if (isFilled) {
                EmptyObject();
            }
        }
    }

    // fill the object with a random liquid
    public void FillObjectRandomLiquid() {
        int whichone = Random.Range(1, 3);
        switch (whichone) {
            case 1:
                FillObject("water");
                break;
            case 2:
                FillObject("wine");
                break;
            case 3:
                FillObject("coffee");
                break;
        }
    }

    public void FillObject(string whichLiquid) {
        if (!Liquids.ContainsKey(whichLiquid)) {
            throw new ArgumentException("Unknown liquid: " + whichLiquid);
        }

        // check if this object has whichLiquid setup as fillable: If the object has a null reference this object
        // is not setup for that liquid
        if (Liquids[whichLiquid] == null) {
            throw new ArgumentException($"The liquid {whichLiquid} is not setup for this object.");
        }

        Liquids[whichLiquid].transform.gameObject.SetActive(true);

        // coffee is hot so change the object's temperature if whichLiquid was coffee
        if (whichLiquid == "coffee") {
            // coffee is hot!
            SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
            sop.CurrentTemperature = Temperature.Hot;
            if (sop.HowManySecondsUntilRoomTemp != sop.GetTimerResetValue()) {
                sop.HowManySecondsUntilRoomTemp = sop.GetTimerResetValue();
            }
            sop.SetStartRoomTempTimer(false);
        }

        isFilled = true;
        currentlyFilledWith = whichLiquid;
    }

    public void EmptyObject() {
        // for each thing in Liquids, if it exists set it to false and then set bools appropriately
        foreach (KeyValuePair<string, GameObject> gogogo in Liquids) {
            // if the value field is not null and has a reference to a liquid object
            if (gogogo.Value != null) {
                gogogo.Value.SetActive(false);
            }
        }
        currentlyFilledWith = null;
        isFilled = false;
    }

    public void OnTriggerStay(Collider other) {
        // if touching running water, automatically fill with water.
        if (!isFilled && other.CompareTag("Liquid")) {
            FillObject("water");
        }
    }
}
