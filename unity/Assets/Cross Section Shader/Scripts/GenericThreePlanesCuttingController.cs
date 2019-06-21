using UnityEngine;
using System.Collections;

public class GenericThreePlanesCuttingController : MonoBehaviour {

    public GameObject plane1;
    public GameObject plane2;
    public GameObject plane3;
    Material mat;
    public Vector3 normal1;
    public Vector3 position1;
    public Vector3 normal2;
    public Vector3 position2;
    public Vector3 normal3;
    public Vector3 position3;
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
        normal1 = plane1.transform.TransformVector(new Vector3(0, 0, -1));
        normal2 = plane2.transform.TransformVector(new Vector3(0, 0, -1));
        normal3 = plane3.transform.TransformVector(new Vector3(0, 0, -1));
        position1 = plane1.transform.position;
        position2 = plane2.transform.position;
        position3 = plane3.transform.position;
        rend.material.SetVector("_Plane1Normal", normal1);
        rend.material.SetVector("_Plane1Position", position1);
        rend.material.SetVector("_Plane2Normal", normal2);
        rend.material.SetVector("_Plane2Position", position2);
        rend.material.SetVector("_Plane3Normal", normal3);
        rend.material.SetVector("_Plane3Position", position3);
    }
}
