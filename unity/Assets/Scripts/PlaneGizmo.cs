using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneGizmo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnDrawGizmos() {
        //  Gizmos.color = Color.yellow;
       
        Gizmos.color = new Color(1, 0.92f, 0.016f, 1f);

        var size = 5.0f;
        
        var p0 = new Vector3(-size, 0.0f, -size);
        var p1 = new Vector3(-size, 0.0f, size);
        var p2 = new Vector3(size, 0.0f, size);
        var p3 = new Vector3(size, 0.0f, -size);
        

        Gizmos.DrawLine(transform.TransformPoint(p0), transform.TransformPoint(p1));
        Gizmos.DrawLine(transform.TransformPoint(p1), transform.TransformPoint(p2));
        Gizmos.DrawLine(transform.TransformPoint(p2), transform.TransformPoint(p3));
        Gizmos.DrawLine(transform.TransformPoint(p3), transform.TransformPoint(p0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
