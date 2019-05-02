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

    public void FillObject(string whichLiquid)
    {

        if(whichLiquid == "water")
        {
            if(WaterObject != null)
            WaterObject.transform.gameObject.SetActive(true);

            isFilled = true;
            currentlyFilledWith = "water";
        }

        else if(whichLiquid == "coffee")
        {
            if(CoffeeObject != null)
            CoffeeObject.transform.gameObject.SetActive(true);

            isFilled = true;
            currentlyFilledWith = "coffee";
        }

        else if(whichLiquid == "wine")
        {
            if(WineObject != null)
            CoffeeObject.transform.gameObject.SetActive(true);

            isFilled = true;
            currentlyFilledWith = "wine";
        }
    }

    public void EmptyObject()
    {
        if(currentlyFilledWith == "water")
        {
            WaterObject.transform.gameObject.SetActive(false);
            currentlyFilledWith = null;
            isFilled= false;
        }

        else if(currentlyFilledWith == "coffee")
        {
            CoffeeObject.transform.gameObject.SetActive(false);
            currentlyFilledWith = null;
            isFilled= false;
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if(other.tag == "Liquid")
        {
            if(!isFilled)
            {
                FillObject("water");
            }
        }
    }
}
