using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThingToMove : MonoBehaviour {
    public GameObject thingToMove;

    // USE THESE FOR POSITION CHANGE
    public Vector3 openPosition;
    public Vector3 closePosition;
    public bool shouldMove;

    // USE THESE FOR ROTATION DON'T WORRY ABOUT IT
    public Vector3 openRotation;
    public Vector3 closeRotation;
    public bool shouldRotate;

    public bool isOpen;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }
}
