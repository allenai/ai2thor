
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LiquidPourRectangleEdge : LiquidPourEdge
{

    public float width = 1.0f;
    public float depth = 1.0f;

    protected override Vector3 getLowestEdgePointWorld(Vector3 up, bool withOffset = false) {
        var upXZ = new Vector3(up.x, 0, up.z);

        var parentRot = this.transform.parent.rotation;
        parentRot.x = 0;
        parentRot.z = 0;

        var verts = this.getRectVerticesWorldSpace(withOffset ? radiusRaycastOffset : 0.0f);

        var minList = new List<int>();
        var currentMin = float.MaxValue;
        for (var i = 0; i < verts.Length; i++) {
            var vert = verts[i];
            if (vert.y < currentMin ) {
                currentMin = vert.y;
            }
            
        }
        var heightThreshold = 0.01f;
        for (var i = 0; i < verts.Length; i++) {
            var vert = verts[i];
            if (Mathf.Abs(vert.y - currentMin) < heightThreshold) {
                    minList.Add(i);
                }
        }

        var indices = minList.ToArray();
        
        //Debug.Log("MIN LIST: min: " + currentMin + " count " + minList.Count + " indices " + str + " vert 0 " + verts[0] + " 2 " + verts[1] + " 3 " + verts[2] + " 4 " + verts[3]);

        if (minList.Count > 1) {
            var v1 = verts[indices[0]];
            var v2 = verts[indices[1]];

            // number between -1 and 1
            var alpha = (v1.y - v2.y) / heightThreshold;

            // linear interpolator between 0 and 1
            alpha = (alpha + 1.0f) / 2.0f; 

            return verts[indices[0]] * (1.0f - alpha) + verts[indices[1]] * (alpha);

            // Simpler model just line between vertices when they are at the same height within the threshold
            // return (verts[indices[0]] + verts[indices[1]]) / 2.0f;
        }
        else {
            return verts[indices[0]];
        }
    }

    protected override void OnDrawGizmos() {
       
        UnityEditor.Handles.color  = Color.red;

        var up =  getUpVector();
        
        // UnityEditor.Handles.RectangleHandleCap()
        var rectVerts = getRectVerticesWorldSpace();

        var rectVerts2 = getRectVerticesWorldSpace(radiusRaycastOffset);
        UnityEditor.Handles.color  = Color.yellow;

         

        // UnityEditor.Handles.DrawWireDisc(this.transform.position, up, this.radius);

        // // UnityEditor.Handles.color  = new Color(1.0f, 0.1f, 0.1f, 0.4f);

        // UnityEditor.Handles.color  = Color.yellow;
        // UnityEditor.Handles.DrawWireDisc(this.transform.position, up, this.radius + this.radiusRaycastOffset);

        var circleLowestWorld = getLowestEdgePointWorld(up);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(circleLowestWorld, (width + depth) / 40.0f);
        UnityEditor.Handles.DrawSolidRectangleWithOutline(rectVerts, new Color(1, 0, 0, 0.0f),  Color.red);


        var circleLowestWorldWithOffset = getLowestEdgePointWorld(up, true);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(circleLowestWorldWithOffset, (width + depth) / 40.0f);
        UnityEditor.Handles.DrawSolidRectangleWithOutline(rectVerts2, new Color(1, 0.92f, 0.016f, 0.0f),  Color.green);

        Gizmos.color = new Color(1f, 1f, 0.0f, 0.7f);

        if (renderDebugLevelPlane) {
            var pos = getWaterLevelPositionWorld();

            var rot = Quaternion.identity;
            if (wobbleComponent == null) {
                wobbleComponent = this.transform.GetComponentInParent<Wobble>();
            }

            //rot = Quaternion.Euler(-wobbleComponent.wobbleAmountX * 360, 0, -wobbleComponent.wobbleAmountZ * 360); 
               // Debug.Log("Wobble " + wobbleComponent.wobbleAmountX + wobbleComponent.wobbleAmountZ )

            Gizmos.DrawMesh(this.debugQuad, pos, Quaternion.Euler(90, 0, 0) * rot, new Vector3(0.5f, 0.5f, 0.5f));
        }

        // Gizmos.color = new Color(1f, 0f, 0.0f, 0.5f);
        // var bounds = this.GetComponentInParent<MeshRenderer>().bounds;
        // Gizmos.DrawCube(this.transform.parent.position, bounds.size);


    }

    private Vector3[] getRectVerticesWorldSpace(float offset = 0.0f) {
        var localPos = this.transform.localPosition;
        return new Vector3[] {
            this.transform.localToWorldMatrix.MultiplyPoint(new Vector3(localPos.x - width/2.0f - offset, 0, localPos.z - depth/2.0f - offset)),
            this.transform.localToWorldMatrix.MultiplyPoint(new Vector3(localPos.x - width/2.0f - offset, 0, localPos.z + depth/2.0f + offset)),
            this.transform.localToWorldMatrix.MultiplyPoint(new Vector3(localPos.x + width/2.0f + offset, 0, localPos.z + depth/2.0f + offset)),
            this.transform.localToWorldMatrix.MultiplyPoint(new Vector3(localPos.x + width/2.0f + offset, 0, localPos.z - depth/2.0f - offset))
        };
    }

}