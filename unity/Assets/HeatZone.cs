using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatZone : MonoBehaviour
{
    //in preparation for different kinds of cooking rather than a single abstracted cooked/uncooked state....
	//this is to make sure that Microwaves don't switch bread slices into their toasted version, because microwaves can't do that
	public bool CanToastBread = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerStay(Collider other)
    {
        if(other.GetComponentInParent<SimObjPhysics>())
        {
            //Set temperature of object to HOT
            SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();
            sop.CurrentTemperature = ObjectMetadata.Temperature.Hot;

            if(sop.HowManySecondsUntilRoomTemp != sop.GetTimerResetValue())
            sop.HowManySecondsUntilRoomTemp = sop.GetTimerResetValue();

            sop.SetStartRoomTempTimer(false);
            //

            //now if the object is able to be cooked, automatically cook it
            if(sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeCooked))
            {
                CookObject sopcook = sop.GetComponent<CookObject>();

                //if the object is bread... check if CanToastBread is true, and if so proceed to cook
                if(sop.Type == SimObjType.BreadSliced && CanToastBread == true)
                {
                    if(!sopcook.IsCooked())
                    {
                        sopcook.Cook();
                    }
                }

                //oh it's not bread, no worries just cook it now
                else if(sop.Type != SimObjType.BreadSliced)
                {
                    if(!sopcook.IsCooked())
                    {
                        sopcook.Cook();
                    }
                }
            }


        }
    }
}
