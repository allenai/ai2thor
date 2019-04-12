using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Candle : MonoBehaviour
{
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
        if(MagiciansRed.tag == "Liquid")
        {
            if(this.GetComponent<CanToggleOnOff>().isOn)
            {
                this.GetComponent<CanToggleOnOff>().Toggle();
            }
        }
    }
}
