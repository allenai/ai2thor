using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateLight : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        this.transform.position += new Vector3(-1f * Time.deltaTime, 0f, -1f * Time.deltaTime);
    }
}
