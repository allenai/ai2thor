using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhatControlsThis : MonoBehaviour
{
    //reference to the sim object that can control this object in some way
    //this will be used for `ToggleOnOff` objects to reference which lights they directly control
    public SimObjPhysics[] SimObjsThatControlsMe;
}
