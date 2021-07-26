using UnityEngine;
using System.Collections.Generic;

namespace Parabox.CSG
{
    sealed class CSG_Node
    {
        public List<CSG_Polygon> polygons;

        public CSG_Node front;  /// Reference to front node.
        public CSG_Node back;   /// Reference to front node.

        public CSG_Plane plane;

        public CSG_Node()
        {
            this.front = null;
            this.back = null;
        }

        public CSG_Node(List<CSG_Polygon> list)
        {
            Build(list);

            // this.front = null;
            // this.back = null;
        }

        public CSG_Node(List<CSG_Polygon> list, CSG_Plane plane, CSG_Node front, CSG_Node back)
        {
            this.polygons = list;
            this.plane = plane;
            this.front = front;
            this.back = back;
        }

        public CSG_Node Clone()
        {
            CSG_Node clone = new CSG_Node(this.polygons, this.plane, this.front, this.back);

            return clone;
        }

        // Remove all polygons in this BSP tree that are inside the other BSP tree
        // `bsp`.
        public void ClipTo(CSG_Node other)
        {
            this.polygons = other.ClipPolygons(this.polygons);

            if (this.front != null)
            {
                this.front.ClipTo(other);
            }

            if (this.back != null)
            {
                this.back.ClipTo(other);
            }
        }

        // Convert solid space to empty space and empty space to solid space.
        public void Invert()
        {
            for (int i = 0; i < this.polygons.Count; i++)
                this.polygons[i].Flip();

            this.plane.Flip();

            if (this.front != null)
            {
                this.front.Invert();
            }

            if (this.back != null)
            {
                this.back.Invert();
            }

            CSG_Node tmp = this.front;
            this.front = this.back;
            this.back = tmp;
        }

        // Build a BSP tree out of `polygons`. When called on an existing tree, the
        // new polygons are filtered down to the bottom of the tree and become new
        // nodes there. Each set of polygons is partitioned using the first polygon
        // (no heuristic is used to pick a good split).
        public void Build(List<CSG_Polygon> list)
        {
            if (list.Count < 1)
                return;

            if (this.plane == null || !this.plane.Valid())
            {
                this.plane = new CSG_Plane();
                this.plane.normal = list[0].plane.normal;
                this.plane.w = list[0].plane.w;
            }


            if (this.polygons == null)
                this.polygons = new List<CSG_Polygon>();

            List<CSG_Polygon> list_front = new List<CSG_Polygon>();
            List<CSG_Polygon> list_back = new List<CSG_Polygon>();

            for (int i = 0; i < list.Count; i++)
            {
                this.plane.SplitPolygon(list[i], this.polygons, this.polygons, list_front, list_back);
            }

            if (list_front.Count > 0)
            {
                if (this.front == null)
                    this.front = new CSG_Node();

                this.front.Build(list_front);
            }

            if (list_back.Count > 0)
            {
                if (this.back == null)
                    this.back = new CSG_Node();

                this.back.Build(list_back);
            }
        }

        // Recursively remove all polygons in `polygons` that are inside this BSP
        // tree.
        public List<CSG_Polygon> ClipPolygons(List<CSG_Polygon> list)
        {
            if (!this.plane.Valid())
            {
                return list;
            }

            List<CSG_Polygon> list_front = new List<CSG_Polygon>();
            List<CSG_Polygon> list_back = new List<CSG_Polygon>();

            for (int i = 0; i < list.Count; i++)
            {
                this.plane.SplitPolygon(list[i], list_front, list_back, list_front, list_back);
            }

            if (this.front != null)
            {
                list_front = this.front.ClipPolygons(list_front);
            }

            if (this.back != null)
            {
                list_back = this.back.ClipPolygons(list_back);
            }
            else
            {
                list_back.Clear();
            }

            // Position [First, Last]
            // list_front.insert(list_front.end(), list_back.begin(), list_back.end());
            list_front.AddRange(list_back);

            return list_front;
        }

        // Return a list of all polygons in this BSP tree.
        public List<CSG_Polygon> AllPolygons()
        {
            List<CSG_Polygon> list = this.polygons;
            List<CSG_Polygon> list_front = new List<CSG_Polygon>(), list_back = new List<CSG_Polygon>();

            if (this.front != null)
            {
                list_front = this.front.AllPolygons();
            }

            if (this.back != null)
            {
                list_back = this.back.AllPolygons();
            }

            list.AddRange(list_front);
            list.AddRange(list_back);

            return list;
        }

        #region STATIC OPERATIONS

        // Return a new CSG solid representing space in either this solid or in the
        // solid `csg`. Neither this solid nor the solid `csg` are modified.
        public static CSG_Node Union(CSG_Node a1, CSG_Node b1)
        {
            CSG_Node a = a1.Clone();
            CSG_Node b = b1.Clone();

            a.ClipTo(b);
            b.ClipTo(a);
            b.Invert();
            b.ClipTo(a);
            b.Invert();

            a.Build(b.AllPolygons());

            CSG_Node ret = new CSG_Node(a.AllPolygons());

            return ret;
        }

        // Return a new CSG solid representing space in this solid but not in the
        // solid `csg`. Neither this solid nor the solid `csg` are modified.
        public static CSG_Node Subtract(CSG_Node a1, CSG_Node b1)
        {
            CSG_Node a = a1.Clone();
            CSG_Node b = b1.Clone();

            a.Invert();
            a.ClipTo(b);
            b.ClipTo(a);
            b.Invert();
            b.ClipTo(a);
            b.Invert();
            a.Build(b.AllPolygons());
            a.Invert();

            CSG_Node ret = new CSG_Node(a.AllPolygons());

            return ret;
        }

        // Return a new CSG solid representing space both this solid and in the
        // solid `csg`. Neither this solid nor the solid `csg` are modified.
        public static CSG_Node Intersect(CSG_Node a1, CSG_Node b1)
        {
            CSG_Node a = a1.Clone();
            CSG_Node b = b1.Clone();

            a.Invert();
            b.ClipTo(a);
            b.Invert();
            a.ClipTo(b);
            b.ClipTo(a);
            a.Build(b.AllPolygons());
            a.Invert();

            CSG_Node ret = new CSG_Node(a.AllPolygons());

            return ret;
        }

        #endregion
    }
}
