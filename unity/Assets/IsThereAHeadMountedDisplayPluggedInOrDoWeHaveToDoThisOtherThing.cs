using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class IsThereAHeadMountedDisplayPluggedInOrDoWeHaveToDoThisOtherThing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(!XRSettings.isDeviceActive)
        {
            Debug.Log("Oh No, no Headset found so uhhh hang on, wait, wait...");
        }

        else if(XRSettings.isDeviceActive && XRSettings.loadedDeviceName == "Mock HMD" ||
        (XRSettings.loadedDeviceName == "MockHMDDisplay"))
        {
            Debug.Log("Using Virtual Mock HMD");
        }

        else
        {
        Debug.Log("Active Headset found, Device Name is: " + XRSettings.loadedDeviceName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
