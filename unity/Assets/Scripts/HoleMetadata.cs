using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class HoleMetadata : MonoBehaviour
{   
    [DraggablePoint] public Vector3 Min;
    [DraggablePoint] public Vector3 Max;
    public Vector3 Margin;

    
    
void OnDrawGizmos() {

    // Box collider green
     //Gizmos.color = new Color(145.0f/255.0f, 245.0f/255.0f, 139/255.0f, 1.0f);

     // Red
     Gizmos.color = new Color(243.0f/255.0f, 77.0f/255.0f, 44/255.0f, 1.0f);

     var halfSize = (Max-Min)/2.0f;
     var halfSizeLocal = transform.TransformPoint(halfSize);

    var bottomLeft = Min;
    var size = Max-Min;

    var k = new List<Vector3>(){ 
        Vector3.zero,
        new Vector3(0, 0, size.z)
    };

    var corners = new List<List<Vector3>> {
            new List<Vector3>(){ 
                Vector3.zero,
                new Vector3(size.x, 0, 0)
            },
            new List<Vector3>(){ 
                Vector3.zero,
                new Vector3(0, size.y, 0)
            },
            new List<Vector3>(){ 
                Vector3.zero,
                new Vector3(0, 0, size.z)
            }
        }.CartesianProduct().Select(x => x.Aggregate(bottomLeft, (acc, v) => acc + v)).ToList();

    var tmp = corners[3];
    corners[3] = corners[2];
    corners[2] = tmp;

    tmp = corners[7];
    corners[7] = corners[6];
    corners[6] = tmp;

    corners = corners.Select(c => this.transform.TransformPoint(c)).ToList();

    var sides = new List<List<Vector3>>() { corners.Take(4).ToList(), corners.Skip(4).ToList() };

    foreach (var q in sides) {

        foreach (var (first, second) in q.Zip(q.Skip(3).Concat(q.Take(3)), (first, second) => (first, second)).Concat(sides[0].Zip(sides[1], (first, second)=> (first, second)))) {
            Gizmos.DrawLine(first, second);
        }
     }
     
      //Gizmos.DrawCube (Min+halfSize, Max-Min);
 }
}
