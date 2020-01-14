using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var color = gameObject.GetComponent<Renderer>().material.color;
        var newColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f);
        gameObject.GetComponent<Renderer>().material.SetColor("_Color", newColor);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
