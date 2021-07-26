using UnityEngine;
using System.Collections.Generic;

namespace Parabox.CSG
{
    /// <summary>
    /// Represents a plane in 3d space.
    /// <remarks>Does not include position.</remarks>
    /// </summary>
    sealed class CSG_Plane
    {
        public Vector3 normal;
        public float w;

        [System.Flags]
        enum EPolygonType
        {
            Coplanar    = 0,
            Front       = 1,
            Back        = 2,
            Spanning    = 3         /// 3 is Front | Back - not a separate entry
        };

        public CSG_Plane()
        {
            normal = Vector3.zero;
            w = 0f;
        }

        public CSG_Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            normal = Vector3.Cross(b - a, c - a);//.normalized;
            w = Vector3.Dot(normal, a);
        }

        public bool Valid()
        {
            return normal.magnitude > 0f;
        }

        public void Flip()
        {
            normal *= -1f;
            w *= -1f;
        }

        // Split `polygon` by this plane if needed, then put the polygon or polygon
        // fragments in the appropriate lists. Coplanar polygons go into either
        // `coplanarFront` or `coplanarBack` depending on their orientation with
        // respect to this plane. Polygons in front or in back of this plane go into
        // either `front` or `back`.
        public void SplitPolygon(CSG_Polygon polygon, List<CSG_Polygon> coplanarFront, List<CSG_Polygon> coplanarBack, List<CSG_Polygon> front, List<CSG_Polygon> back)
        {
            // Classify each point as well as the entire polygon into one of the above
            // four classes.
            EPolygonType polygonType = 0;
            List<EPolygonType> types = new List<EPolygonType>();

            for (int i = 0; i < polygon.vertices.Count; i++)
            {
                float t = Vector3.Dot(this.normal, polygon.vertices[i].position) - this.w;
                EPolygonType type = (t < -Boolean.k_Epsilon) ? EPolygonType.Back : ((t > Boolean.k_Epsilon) ? EPolygonType.Front : EPolygonType.Coplanar);
                polygonType |= type;
                types.Add(type);
            }

            // Put the polygon in the correct list, splitting it when necessary.
            switch (polygonType)
            {
                case EPolygonType.Coplanar:
                {
                    if (Vector3.Dot(this.normal, polygon.plane.normal) > 0)
                        coplanarFront.Add(polygon);
                    else
                        coplanarBack.Add(polygon);
                }
                break;

                case EPolygonType.Front:
                {
                    front.Add(polygon);
                }
                break;

                case EPolygonType.Back:
                {
                    back.Add(polygon);
                }
                break;

                case EPolygonType.Spanning:
                {
                    List<CSG_Vertex> f = new List<CSG_Vertex>();
                    List<CSG_Vertex> b = new List<CSG_Vertex>();

                    for (int i = 0; i < polygon.vertices.Count; i++)
                    {
                        int j = (i + 1) % polygon.vertices.Count;

                        EPolygonType ti = types[i], tj = types[j];

                        CSG_Vertex vi = polygon.vertices[i], vj = polygon.vertices[j];

                        if (ti != EPolygonType.Back)
                        {
                            f.Add(vi);
                        }

                        if (ti != EPolygonType.Front)
                        {
                            b.Add(vi);
                        }

                        if ((ti | tj) == EPolygonType.Spanning)
                        {
                            float t = (this.w - Vector3.Dot(this.normal, vi.position)) / Vector3.Dot(this.normal, vj.position - vi.position);

                            CSG_Vertex v = CSG_VertexUtility.Mix(vi, vj, t);

                            f.Add(v);
                            b.Add(v);
                        }
                    }

                    if (f.Count >= 3)
                    {
                        front.Add(new CSG_Polygon(f, polygon.material));
                    }

                    if (b.Count >= 3)
                    {
                        back.Add(new CSG_Polygon(b, polygon.material));
                    }
                }
                break;
            }   // End switch(polygonType)
        }
    }
}
