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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

