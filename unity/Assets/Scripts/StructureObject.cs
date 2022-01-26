// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//this is used to tag Structural objects in the scene. Structural objects are objects with physical collision and are rendered, but are not SimObjects themselves.
//these objects are all located under the "Structure" object in the Heirarchy, and are always static and purely environmental.
public class StructureObject : MonoBehaviour
{
    [SerializeField]
    public StructureObjectTag WhatIsMyStructureObjectTag;

    public static float PLATFORM_LIP_WIDTH = 0.1f;
    public static float PLATFORM_LIP_HEIGHT = 0.3f;

    // Start is called before the first frame update
    void Start()
    {
        MCSController agent = FindObjectOfType<MCSController>();
        if(agent!=null)
            Physics.IgnoreCollision(agent.groundObjectsCollider, gameObject.GetComponentInChildren<Collider>(), true);
    }

    public void AddPlatformLips(float scaleX=1, float scaleY=1, float scaleZ=1, bool addFront=false, bool addBack=false, bool addLeft=false, bool addRight=false)
    {
        float placementOffsetXWithScale = 0.5f - (PLATFORM_LIP_WIDTH / scaleX / 2); 
        float placementOffsetYWithScale = 0.5f + (PLATFORM_LIP_HEIGHT / scaleY / 2);
        float placementOffsetZWithScale = 0.5f - (PLATFORM_LIP_WIDTH / scaleZ / 2);

        string clone = " (Clone)";
        GameObject thisPlatform = this.gameObject;
        GameObject front = null;
        GameObject back = null;
        GameObject left = null;
        GameObject right = null;

        //instantiate identical lips
        if(addFront) {
            front = Instantiate(thisPlatform, transform.position, Quaternion.identity);
            front.name = front.name.Substring(0, name.Length - clone.Length) + "-front";
            front.GetComponent<SimObjPhysics>().objectID = front.name;
        }
        
        if(addBack) {
            back = Instantiate(thisPlatform, transform.position, Quaternion.identity);
            back.name = back.name.Substring(0, name.Length - clone.Length) + "-back";
            front.GetComponent<SimObjPhysics>().objectID = back.name;
        }
        
        if(addLeft) {
            left = Instantiate(thisPlatform, transform.position, Quaternion.identity);
            left.name = left.name.Substring(0, name.Length - clone.Length) + "-left";
            front.GetComponent<SimObjPhysics>().objectID = left.name;
        }
        
        if(addRight) {
            right = Instantiate(thisPlatform, transform.position, Quaternion.identity);
            right.name = right.name.Substring(0, name.Length - clone.Length) + "-right";
            front.GetComponent<SimObjPhysics>().objectID = right.name;
        }
        
        //after all lips are instantiated, then their parent can be set to this platform and have
        //their position and scale adjusted
        if(addFront) {
            front.transform.parent = this.transform;
            front.transform.localPosition = new Vector3(0, placementOffsetYWithScale, -placementOffsetZWithScale);
            front.transform.localScale = new Vector3(1, PLATFORM_LIP_HEIGHT / scaleY, PLATFORM_LIP_WIDTH / scaleZ);
        }

        if(addBack) {
            back.transform.parent = this.transform;
            back.transform.localPosition = new Vector3(0, placementOffsetYWithScale, placementOffsetZWithScale);
            back.transform.localScale = new Vector3(1, PLATFORM_LIP_HEIGHT / scaleY, PLATFORM_LIP_WIDTH / scaleZ);
        }
        
        if(addLeft) {
            left.transform.parent = this.transform;
            left.transform.localPosition = new Vector3(-placementOffsetXWithScale, placementOffsetYWithScale, 0);
            left.transform.localScale = new Vector3(PLATFORM_LIP_WIDTH / scaleX, PLATFORM_LIP_HEIGHT / scaleY, 1);
        }

        if(addRight) {
            right.transform.parent = this.transform;
            right.transform.localPosition = new Vector3(placementOffsetXWithScale, placementOffsetYWithScale, 0);
            right.transform.localScale = new Vector3(PLATFORM_LIP_WIDTH / scaleX, PLATFORM_LIP_HEIGHT / scaleY, 1);
        }
    }
}

[Serializable]
public enum StructureObjectTag : int
{
    Undefined = 0,
    Wall = 1,
    Floor = 2,
    Ceiling = 3,
    LightFixture = 4,//for all hanging lights or other lights protruding out of something
    CeilingLight = 5,//for embedded lights in the ceiling
    Stove = 6,//for the uninteractable body of the stove
    DishWasher = 7,
    KitchenIsland = 8,
    Door = 9,
    WallCabinetBody = 10,
    OvenHood = 11,
    PaperClutter = 12,
    SkyLightWindow = 13,
    Clock = 14,
    Rug = 15,
    FirePlace = 16,
    DecorativeSticks = 17,
    
    

}

