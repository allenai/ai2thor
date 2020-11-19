using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Adjust_Slider : MonoBehaviour
{
    public Transform robotArmRoot;
    Vector3 localStartingPoint;
    float minThreshold = -0.32f;
    float maxThreshold = 0.28f;
    
    void Start()
    {
        localStartingPoint = this.transform.localPosition;
        //Debug.Log(localStartingPoint + "Fuck you");
        //Debug.Log(localStartingPoint + "Fuck you");
    }
    
    void Update()
    {
        //Debug.Log(robotArmRoot.localPosition.x + ", " + robotArmRoot.localPosition.y + ", " + robotArmRoot.localPosition.z + " you bitch");
        if (robotArmRoot.localPosition.y < minThreshold)
        {
            this.transform.localPosition = new Vector3(this.transform.localPosition.x, localStartingPoint.y - Mathf.Abs(robotArmRoot.localPosition.y - minThreshold), this.transform.localPosition.z);
        }

        else if (robotArmRoot.localPosition.y > maxThreshold)
        {
            this.transform.localPosition = new Vector3(this.transform.localPosition.x, localStartingPoint.y + Mathf.Abs(robotArmRoot.localPosition.y - maxThreshold), this.transform.localPosition.z);
        }
    }
}
