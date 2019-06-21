using UnityEngine;
using System.Collections;
//[ExecuteInEditMode]
public class OnePlaneCuttingController : MonoBehaviour {

    public GameObject plane;
    Material mat;
    public Vector3 normal;
    public Vector3 position;
    public Renderer rend;
    // Use this for initialization
    void Start () {
        rend = GetComponent<Renderer>();
        normal = plane.transform.TransformVector(new Vector3(0,0,-1));
        position = plane.transform.position;
        UpdateShaderProperties();
    }
    void Update ()
    {
        UpdateShaderProperties();
    }

    private void UpdateShaderProperties()
    {
        normal = plane.transform.TransformVector(new Vector3(0, 0, -1));
        position = plane.transform.position;
        rend.material.SetVector("_PlaneNormal", normal);
        rend.material.SetVector("_PlanePosition", position);
    }
}
