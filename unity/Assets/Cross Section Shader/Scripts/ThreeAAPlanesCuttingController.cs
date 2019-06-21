using UnityEngine;
using System.Collections;

public class ThreeAAPlanesCuttingController : MonoBehaviour {

    public GameObject planeYZ;
    public GameObject planeXZ;
    public GameObject planeXY;
    Material mat;
    public Vector3 positionYZ;
    public Vector3 positionXZ;
    public Vector3 positionXY;
    public Renderer rend;
    // Use this for initialization
    void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateShaderProperties();
    }
    void Update()
    {
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        positionYZ = planeYZ.transform.position;
        positionXZ = planeXZ.transform.position;
        positionXY = planeXY.transform.position;
        rend.material.SetVector("_Plane1Position", positionYZ);
        rend.material.SetVector("_Plane2Position", positionXZ);
        rend.material.SetVector("_Plane3Position", positionXY);
    }
}
