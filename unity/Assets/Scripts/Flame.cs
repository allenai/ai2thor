using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this is used specifically for parts of an object that can be lit on fire (like a candle wick). This object should be untagged and SimObjInvisible

//ok so hear me out, the logic here is this box always exists and will toggle the flame particle effect on or off based on environmental triggers from Liquid or Fire tagged boxes.
//this means fire sources (the candle flame particle itself, stove top fire etc) must have a trigger box on it tagged fire so that this component can take care of itself.

//for the cancle specifically, this box will control whether or not to turn the flame particle on or off, the flame particle itself has a triggerbox called tagged "flame" on it, so it can independantly be used to light another candle on fire
public class Flame : MonoBehaviour
{
    //my parent object to find the CanToggleOnOff component
    public GameObject MyObject;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //if this touches water and it's on, it is put out. Fire safety is important!
    public void OnTriggerStay(Collider MagiciansRed)
    {
        //check if the fire zone is touching Liquid(running water effects) or StandingLiquid(filled water effects)
        if(MagiciansRed.CompareTag("Liquid") || MagiciansRed.CompareTag("StandingLiquid"))
        {
            if(MyObject.GetComponent<CanToggleOnOff>().isOn)
            {
                MyObject.GetComponent<CanToggleOnOff>().Toggle();
            }
        }

        if(MagiciansRed.CompareTag("Fire"))
        {
            if(!MyObject.GetComponent<CanToggleOnOff>().isOn)
            {
                MyObject.GetComponent<CanToggleOnOff>().Toggle();
            }
        }
    }
}
