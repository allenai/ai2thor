using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public Dictionary <string, GameObject> Liquids = new Dictionary<string, GameObject>();

    public bool IsFilled() {
        return isFilled;
    }

    void Start() {
        #if UNITY_EDITOR
            if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeFilled)) {
                Debug.LogError(gameObject.name + " is missing the CanBeFilled secondary property!");
            }
        #endif 

        Liquids.Add("water", WaterObject);
        Liquids.Add("coffee", CoffeeObject);
        Liquids.Add("wine", WineObject);
    }

    void Update() {
        // check if the object is rotated too much, if so it should spill out
        if (Vector3.Angle(gameObject.transform.up, Vector3.up) > 90) {
            if(isFilled) {
                EmptyObject();
            }
        }
    }

    // fill the object with a random liquid
    public void FillObjectRandomLiquid() {
        int randN = Random.Range(1, 3);
        switch (randN) {
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

    public void FillObject(string liquidType) {
        if (whichLiquid == null) {
            throw new ArgumentNullException();
        }

        if (!Liquids.ContainsKey(liquidType)) {
            throw new ArgumentException($"liquidType: {whichLiquid} not a valid Liquid!");
        }

        // check if this object has whichLiquid setup as fillable:
        // If the object has a null reference this object is not setup for that liquid
        if (Liquids[liquidType] == null) {
            throw new InvalidOperationException("Object not compatible with this liquidType!");
        }

        if (isFilled) {
            throw new InvalidOperationException("Object is already filled!");
        }

        Liquids[liquidType].transform.gameObject.SetActive(true);

        // coffee is hot so change the object's temperature if whichLiquid was coffee
        if (liquidType == "coffee") {
            // coffee is hot!
            SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
            sop.CurrentTemperature = ObjectMetadata.Temperature.Hot;
            if (sop.HowManySecondsUntilRoomTemp != sop.GetTimerResetValue()) {
                sop.HowManySecondsUntilRoomTemp = sop.GetTimerResetValue();
            }
            sop.SetStartRoomTempTimer(false);
        }

        isFilled = true;
        currentlyFilledWith = liquidType;
    }

    public void EmptyObject() {
        if (!isFilled) {
            throw new InvalidOperationException("Object is already empty!");
        }

        // for each thing in Liquids, if it exists set it to false and then set bools appropriately
        foreach (KeyValuePair<string, GameObject> liquid in Liquids) {
            // if the value field is not null and has a reference to a liquid object 
            if (liquid.Value != null) {
                liquid.Value.SetActive(false);
            }
        }
        // Liquids[currentlyFilledWith].transform.gameObject.SetActive(false);
        currentlyFilledWith = null;
        isFilled = false;
    }

    public void OnTriggerStay(Collider other) {
        // if touching running water, automatically fill with water.
        if (other.tag == "Liquid") {
            if (!isFilled) {
                FillObject("water");
            }
        }
    }
}
