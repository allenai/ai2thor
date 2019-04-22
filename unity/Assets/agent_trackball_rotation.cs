using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class agent_trackball_rotation : MonoBehaviour
{
    public GameObject myController;
    Vector3 myRotation = new Vector3(0,0,0);
    float posRotRatio = 7200 / (11 * Mathf.PI);

    void Update()
    {
        myRotation.x = myController.transform.position.z * posRotRatio;
        myRotation.z = myController.transform.position.x * -posRotRatio;

        gameObject.transform.eulerAngles = myRotation;
    }
}
