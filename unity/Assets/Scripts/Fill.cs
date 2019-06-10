using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fill : MonoBehaviour
{
    [SerializeField]
    protected GameObject WaterObject = null;

    [SerializeField]
    protected GameObject CoffeeObject = null;

    [SerializeField]
    protected GameObject WineObject = null;


    [SerializeField]
    protected bool isFilled = false; //false - empty, true - currently filled with

    protected string currentlyFilledWith = null;

    public Dictionary <string, GameObject> Liquids = new Dictionary<string, GameObject>();

    public bool IsFilled()
    {
        return isFilled;
    }

    void Start()
    {
        #if UNITY_EDITOR
        if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeFilled))
        {
            Debug.LogError(gameObject.name + " is missing the CanBeFilled secondary property!");
        }
        #endif 

        Liquids.Add("water", WaterObject);
        Liquids.Add("coffee", CoffeeObject);
        Liquids.Add("wine", WineObject);

    }

    // Update is called once per frame
    void Update()
    {
        //check if the object is rotated too much, if so it should spill out
        if(Vector3.Angle(gameObject.transform.up, Vector3.up) > 90)
        {
            //print("spilling!");
            if(isFilled)
            {
                EmptyObject();
            }
        }

        // //debug stuff
        // if(Input.GetKeyDown(KeyCode.G))
        // {
        //     FillObject("water");
        // }
    }

    //fill the object with a random liquid
    public void FillObjectRandomLiquid()
    {
        int whichone = Random.Range(1, 3);
        if(whichone == 1)
        {
            FillObject("water");
        }

        if(whichone == 2)
        {
            FillObject("wine");
        }

        if(whichone == 3)
        {
            FillObject("coffee");
        }
    }

    public bool FillObject(string whichLiquid)
    {
        if(Liquids.ContainsKey(whichLiquid))
        {
            //check if this object has whichLiquid setup as fillable: If the object has a null reference this object
            //is not setup for that liquid
            if(Liquids[whichLiquid] == null)
            {
                return false;
            }

            Liquids[whichLiquid].transform.gameObject.SetActive(true);

            //coffee is hot so change the object's temperature if whichLiquid was coffee
            if(whichLiquid == "coffee")
            {
                //coffee is hot!
                SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
                sop.CurrentTemperature = ObjectMetadata.Temperature.Hot;
                if(sop.HowManySecondsUntilRoomTemp != sop.GetTimerResetValue())
                sop.HowManySecondsUntilRoomTemp = sop.GetTimerResetValue();
                sop.SetStartRoomTempTimer(false);
            }

            isFilled = true;
            currentlyFilledWith = whichLiquid;
            return true;
        }

        //whichLiquid is not in the dictionary
        else
        return false;


        //if the dict doesn't contain this key pair uuuuuh

        // if(whichLiquid == "water")
        // {
        //     if(WaterObject != null)
        //     WaterObject.transform.gameObject.SetActive(true);

        //     isFilled = true;
        //     currentlyFilledWith = "water";
        // }

        // else if(whichLiquid == "coffee")
        // {
        //     if(CoffeeObject != null)
        //     CoffeeObject.transform.gameObject.SetActive(true);

        //     //coffee is hot!
        //     SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
        //     sop.CurrentTemperature = ObjectMetadata.Temperature.Hot;
        //     if(sop.HowManySecondsUntilRoomTemp != sop.GetTimerResetValue())
        //     sop.HowManySecondsUntilRoomTemp = sop.GetTimerResetValue();
        //     sop.SetStartRoomTempTimer(false);


        //     isFilled = true;
        //     currentlyFilledWith = "coffee";
        // }

        // else if(whichLiquid == "wine")
        // {
        //     if(WineObject != null)
        //     CoffeeObject.transform.gameObject.SetActive(true);

        //     isFilled = true;
        //     currentlyFilledWith = "wine";
        // }
    }

    public void EmptyObject()
    {
        //for each thing in Liquids, if it exists set it to false and then set bools appropriately

        foreach (KeyValuePair<string, GameObject> gogogo in Liquids)
        {
            //if the value field is not null and has a reference to a liquid object 
            if(gogogo.Value != null)
            {
                gogogo.Value.SetActive(false);
            }
        }
        //Liquids[currentlyFilledWith].transform.gameObject.SetActive(false);
        currentlyFilledWith = null;
        isFilled = false;

        // if(currentlyFilledWith == "water")
        // {
        //     WaterObject.transform.gameObject.SetActive(false);
        //     currentlyFilledWith = null;
        //     isFilled= false;
        // }

        // else if(currentlyFilledWith == "coffee")
        // {
        //     CoffeeObject.transform.gameObject.SetActive(false);
        //     currentlyFilledWith = null;
        //     isFilled= false;
        // }
    }

    public void OnTriggerStay(Collider other)
    {
        //if touching running water, automatically fill with water.
        if(other.tag == "Liquid")
        {
            if(!isFilled)
            {
                FillObject("water");
            }
        }
    }
}
