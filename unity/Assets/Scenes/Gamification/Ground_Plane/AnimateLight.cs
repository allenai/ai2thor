using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateLight : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        this.transform.position += new Vector3(-0.5f * Time.deltaTime, 0f, -0.5f * Time.deltaTime);
    }
}
