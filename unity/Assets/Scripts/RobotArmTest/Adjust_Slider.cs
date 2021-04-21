using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Adjust_Slider : MonoBehaviour {
    public Transform robotArmRoot;
    Vector3 localStartingPoint;
    float minThreshold = -0.32f;
    float maxThreshold = 0.15f;

    void Start() {
        localStartingPoint = this.transform.localPosition;
    }

    void Update() {
        if (robotArmRoot.localPosition.y < minThreshold) {
            this.transform.localPosition = new Vector3(this.transform.localPosition.x, localStartingPoint.y - Mathf.Abs(robotArmRoot.localPosition.y - minThreshold), this.transform.localPosition.z);
        } else if (robotArmRoot.localPosition.y > maxThreshold) {
            this.transform.localPosition = new Vector3(this.transform.localPosition.x, localStartingPoint.y + Mathf.Abs(robotArmRoot.localPosition.y - maxThreshold), this.transform.localPosition.z);
        }

        this.transform.GetChild(0).position = new Vector3(this.transform.GetChild(0).position.x, robotArmRoot.position.y, this.transform.GetChild(0).position.z);
    }
}
