using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles collider stretching and arm extension animation
public class ArticulatedArmExtender : MonoBehaviour
{
    
    public Transform arm2, arm3, arm4;
    public Transform[] myColliders;
    public float initialZPos;

    public float scaleMultiplier = 1f;

    public void Init() {
        initialZPos = this.gameObject.transform.localPosition.z;
    }

    private void scaleColliders() {
        var currentZPos = this.gameObject.transform.localPosition.z;
        foreach (Transform go in myColliders) {
            go.localScale = new Vector3(go.localScale.x, go.localScale.y, 1 + (currentZPos - initialZPos) * scaleMultiplier);
        }
    }

    private void animate(float armExtensionLength) {
        //Extend each part of arm by one-quarter of extension length, in local z-direction
        arm2.localPosition = new Vector3 (0, 0, 1 * (armExtensionLength / 4) + 0.01300028f);
        arm3.localPosition = new Vector3 (0, 0, 2 * (armExtensionLength / 4) + 0.01300049f);
        arm4.localPosition = new Vector3 (0, 0, 3 * (armExtensionLength / 4) + 0.01300025f);
        // arm5.localPosition = new Vector3 (0, 0, 4 * (armExtensionLength / 4) + 0.0117463f);
    }
   
   

    public void Extend(float armExtensionLength) {
       scaleColliders();
       animate(armExtensionLength);
    }
}
