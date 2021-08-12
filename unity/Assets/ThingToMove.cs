using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThingToMove : MonoBehaviour
{
    [SerializeField] GameObject thingToMove;
    /// USE THESE FOR POSITION CHANGE
    [SerializeField] Vector3 openPosition;
    [SerializeField] Vector3 closePosition;
    //USE THESE FOR ROTATION DONT WORRY ABOUT IT
    [SerializeField] Vector3 closeRotation;
    [SerializeField] Vector3 openRotation;
    public bool isOpen;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
