using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncTransform : MonoBehaviour
{
    protected enum WhatToTrack {Rotation, Position};

    [SerializeField]
    protected WhatToTrack WhichTransformPropertyAmITracking;

    [SerializeField]
    GameObject ThingIamTracking;

    //used to stop syncing the upper body when the ToggleMapView function is called
    public bool StopSyncingForASecond = false;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(!StopSyncingForASecond)
        {
            if(WhichTransformPropertyAmITracking == WhatToTrack.Rotation)
            {
                gameObject.transform.rotation = ThingIamTracking.transform.rotation;
            }

            else if(WhichTransformPropertyAmITracking == WhatToTrack.Position)
            {
                gameObject.transform.localPosition = ThingIamTracking.transform.localPosition;
            }
        }
    }
}
