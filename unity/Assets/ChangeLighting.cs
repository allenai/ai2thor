using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeLighting : MonoBehaviour
{
    //9 elements right now
    public GameObject [] Lights;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLights(int lightset)
    {
        foreach (GameObject go in Lights)
        {
            go.SetActive(false);
        }

        Lights[lightset - 1].SetActive(true);
    }
}
