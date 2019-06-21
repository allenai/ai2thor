using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RotationController : MonoBehaviour {

    public Slider XRot;
    public Slider YRot;
    public Slider ZRot;
    //public Transform ControlledObject;
    public void UpdateObjectPosition(Transform ControlledObject)
    {
        Vector3 newRotation = new Vector3(XRot.value, YRot.value, ZRot.value);
        ControlledObject.rotation = Quaternion.Euler(newRotation);
    }
}
