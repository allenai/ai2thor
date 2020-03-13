using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterActionController : MonoBehaviour {

    public GameObject lowPolyExample, highPolyExample;
    public Material matOFF;

	// Use this for initialization
	void Start () {
		
	}

    // Update is called once per frame
    void Update()
    {

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100.0f))
        {

            if(hit.transform.tag == "AlphaBetBlock")
            {
                lowPolyExample.GetComponent<Renderer>().material = hit.transform.GetComponent<Renderer>().material;
                highPolyExample.GetComponent<Renderer>().material = hit.transform.GetComponent<Renderer>().material;
            } else
            {
                lowPolyExample.GetComponent<Renderer>().material = matOFF;
                highPolyExample.GetComponent<Renderer>().material = matOFF;
            }
         
        }
    }
}
