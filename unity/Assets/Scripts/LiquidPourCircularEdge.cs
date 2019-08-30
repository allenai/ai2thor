
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LiquidPourCircularEdge : LiquidPourEdge
{
    public float radius = 1.0f;

    protected override Vector3 getEdgeLowestPointWorldSpace(Vector3 up, bool withOffset = false) {
        var upXZ = new Vector3(up.x, 0, up.z);

        var parentRot = this.transform.parent.rotation;
        parentRot.x = 0;
        parentRot.z = 0;

        // Quat
        
        // upXZ = Quaternion.AngleAxis(-this.transform.parent.eulerAngles.y, Vector3.up) * upXZ.normalized;
        upXZ = Quaternion.Inverse(parentRot).normalized * upXZ.normalized;
        // Debug.Log("up xz " + upXZ);
        // Debug.Log("Local Pos " + this.transform.localPosition);
        var calculatedRadius = withOffset ? this.radius + this.radiusRaycastOffset : this.radius;
        var circleLowestLocal = Vector3.zero + calculatedRadius * upXZ;
        // var circleLowestWorld = this.transform.TransformPoint(circleLowestLocal);
        var circleLowestWorld = this.transform.TransformPoint(circleLowestLocal);

        return circleLowestWorld;
    }

    protected override ParticleSystem.MinMaxCurve GetFlowSize(float edgeDifference) {
        return new ParticleSystem.MinMaxCurve(radius * 0.8f, radius * 1f);
    }

    void OnDrawGizmos() {
       
      
        UnityEditor.Handles.color  = Color.red;

        var up =  getUpVector();

        var outerRingColor = Color.green;//new Color(180/255f, 132/255f, 191/255f, 1.0f);
        var innerRingColor = Color.red;
        
        UnityEditor.Handles.color = innerRingColor;
        UnityEditor.Handles.DrawWireDisc(this.transform.position, up, this.radius);

        // UnityEditor.Handles.color  = new Color(1.0f, 0.1f, 0.1f, 0.4f);

        UnityEditor.Handles.color  = outerRingColor;
        UnityEditor.Handles.DrawWireDisc(this.transform.position, up, this.radius + this.radiusRaycastOffset);

        var circleLowestWorld = getEdgeLowestPointWorldSpace(up);

        Gizmos.color = innerRingColor;
        Gizmos.DrawSphere(circleLowestWorld, radius / 10.0f);


        var circleLowestWorldWithOffset = getEdgeLowestPointWorldSpace(up, true);

        Gizmos.color = outerRingColor;
        Gizmos.DrawSphere(circleLowestWorldWithOffset, radius / 10.0f);

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

    #if UNITY_EDITOR
        [UnityEditor.MenuItem("Thor/Set Circular Liquid Component")]
        public static void SetCircularLiquidComponent() {
            LiquidPourEdge.SetLiquidComponent("Assets/Prefabs/Systems/CircularLiquidPourEdge.prefab");
        }
    #endif

}