using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdZone : MonoBehaviour
{
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
        //if any simobjphys are touching this zone, set their temperature values to Cold
        if(other.GetComponentInParent<SimObjPhysics>())
        {
            SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();
            sop.CurrentTemperature = ObjectMetadata.Temperature.Cold;

            if(sop.HowManySecondsUntilRoomTemp != sop.GetTimerResetValue())
            sop.HowManySecondsUntilRoomTemp = sop.GetTimerResetValue();

            sop.SetStartRoomTempTimer(false);
        }
    }
}
