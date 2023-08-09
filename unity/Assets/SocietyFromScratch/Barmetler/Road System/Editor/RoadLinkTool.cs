using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace Barmetler.RoadSystem
{
    [EditorTool("RoadSystem/Road Link Tool")]
    public class RoadLinkTool : EditorTool
    {
        GUIContent m_IconContent;

        public override GUIContent toolbarIcon => m_IconContent;

        public static RoadLinkTool ActiveInstance { get; private set; } = null;

        public override void OnActivated()
        {
            m_IconContent ??= new GUIContent(EditorGUIUtility.IconContent("Linked@2x"))
            {
                text = "Road Link Tool",
                tooltip = "Used to link and unlink roads from anchor points.",
            };

            ActiveInstance = this;
            Undo.undoRedoPerformed += OnUndoRedo;
            UnityEditor.Selection.activeObject = null;
        }

        public override void OnWillBeDeactivated()
        {
            if (activePoint is AnchorPoint pt)
                UnityEditor.Selection.activeObject = pt.anchor.GetConnectedRoad();
            else
                UnityEditor.Selection.activeObject = activePoint?.gameObject;

            ActiveInstance = null;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        enum ToolState
        {
            SELECTING, LINKING, UNLINKING
        }

        ToolState toolState = ToolState.SELECTING;

        public interface IPoint : System.IEquatable<IPoint>
        {
            Vector3 position { get; }
            Quaternion rotation { get; }
            GameObject gameObject { get; }
            bool IsConnected { get; }
        }

        public sealed class RoadPoint : IPoint
        {
            public Road road;
            public bool isStart;
            public Vector3 position =>
                road.transform.TransformPoint(isStart ? road[0] : road[-1]);
            public Quaternion rotation =>
                RoadUtilities.GetRotationAtWorldSpace(road, isStart ? 0 : -1) * (isStart ? Quaternion.AngleAxis(180, Vector3.up) : Quaternion.identity);

            public GameObject gameObject => road ? road.gameObject : null;

            public bool IsConnected => isStart ? road.start : road.end;

            public bool Equals(IPoint other)
            {
                return (other is RoadPoint otherRoad) && road == otherRoad.road && isStart == otherRoad.isStart;
            }
        }

        public sealed class AnchorPoint : IPoint
        {
            public RoadAnchor anchor;
            public Vector3 position =>
                anchor.transform.position;
            public Quaternion rotation =>
                anchor.transform.rotation;

            public GameObject gameObject => anchor ? anchor.gameObject : null;

            public bool IsConnected => anchor.GetConnectedRoad();

            public bool Equals(IPoint other)
            {
                return (other is AnchorPoint otherAnchor) && anchor == otherAnchor.anchor;
            }
        }

        static IPoint activePoint = null;

        public static IPoint ActivePoint => activePoint;
        public static GameObject Selection => activePoint?.gameObject;

        public static void Select(Road road, bool isStart)
        {
            if (isStart ? road.start : road.end)
                activePoint = new AnchorPoint { anchor = isStart ? road.start : road.end };
            else
                activePoint = new RoadPoint { road = road, isStart = isStart };
        }

        public static void Select(RoadAnchor anchor)
        {
            if (anchor)
                activePoint = new AnchorPoint { anchor = anchor };
            else
                activePoint = null;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var e = Event.current;

            if (activePoint != null && !activePoint.gameObject) activePoint = null;

            if (activePoint != null && !activePoint.IsConnected && e.shift)
                toolState = ToolState.LINKING;
            else if (e.control && !e.shift)
                toolState = ToolState.UNLINKING;
            else
                toolState = ToolState.SELECTING;

            var buttons = new List<IPoint>();

            foreach (var intersection in FindObjectsOfType<Intersection>())
            {
                foreach (var anchor in intersection.AnchorPoints)
                {
                    buttons.Add(new AnchorPoint { anchor = anchor });
                }
            }

            foreach (var road in FindObjectsOfType<Road>())
            {
                if (!road.start)
                    buttons.Add(new RoadPoint { road = road, isStart = true });
                if (!road.end)
                    buttons.Add(new RoadPoint { road = road, isStart = false });
            }

            bool activeIsRoad = activePoint is RoadPoint activeRoadPoint;

            bool filter(IPoint point)
            {
                switch (toolState)
                {
                    case ToolState.SELECTING:
                        if (point.Equals(activePoint))
                            return false;
                        break;

                    case ToolState.LINKING:
                        if (point.Equals(activePoint))
                            return false;
                        if (activeIsRoad)
                        {
                            if (point is RoadPoint)
                                return false;
                            if (point is AnchorPoint anchorPoint && anchorPoint.anchor.GetConnectedRoad())
                                return false;
                        }
                        else
                        {
                            if (point is AnchorPoint) return false;
                            if ((activePoint as AnchorPoint).anchor.GetConnectedRoad())
                                return false;
                        }
                        break;

                    case ToolState.UNLINKING:
                        if (point is RoadPoint)
                            return false;
                        if (!(point as AnchorPoint).anchor.GetConnectedRoad())
                            return false;
                        break;
                }

                var viewPos = Camera.current.WorldToViewportPoint(point.position);
                if (Mathf.Abs(viewPos.x - 0.5f) * 2 > 1f) return false;
                if (Mathf.Abs(viewPos.y - 0.5f) * 2 > 1f) return false;
                if (viewPos.z < Camera.current.nearClipPlane + 0.5f) return false;

                return true;
            }

            buttons = buttons
                .Where(filter)
                .OrderByDescending(e => Vector3.Dot(Camera.current.transform.forward, e.position - Camera.current.transform.position))
                .ToList();

            float size = 1.5f;
            foreach (var point in buttons)
            {
                var position = point.position - point.rotation * (Vector3.forward * size / 2);
                Handles.color = Color.red + 0.7f * Color.white;
                if (point is RoadPoint) Handles.color = Color.cyan;
                else if (point is AnchorPoint _anchor1 && !_anchor1.anchor.GetConnectedRoad()) Handles.color = Color.blue;
                else if (point is AnchorPoint _anchor2 && _anchor2.anchor.GetConnectedRoad()) position = point.position;

                if (toolState == ToolState.UNLINKING)
                    Handles.color = Color.red * .5f + Color.yellow * .5f;

                if (Handles.Button(position, point.rotation, size, size * 1.5f, Handles.CubeHandleCap))
                {
                    switch (toolState)
                    {
                        case ToolState.SELECTING:
                            activePoint = point;
                            break;

                        case ToolState.LINKING:
                            Link(activePoint, point, e.control);
                            break;

                        case ToolState.UNLINKING:
                            Unlink(point);
                            break;
                    }
                }
            }

            switch (toolState)
            {
                case ToolState.SELECTING:
                case ToolState.LINKING:
                    if (activePoint != null)
                    {
                        var position = activePoint.position - activePoint.rotation * (Vector3.forward * size / 2);
                        if (activePoint is AnchorPoint _anchor2 && _anchor2.anchor.GetConnectedRoad()) position = activePoint.position;
                        Handles.color = Color.black;
                        Handles.CubeHandleCap(0, position, activePoint.rotation, -1.1f * size, EventType.Repaint);
                        Handles.color = Color.red;
                        Handles.CubeHandleCap(0, position, activePoint.rotation, size, EventType.Repaint);
                    }
                    break;
            }

            PrintToolTip();
        }

        void PrintToolTip()
        {
            string text = null;
            switch (toolState)
            {
                case ToolState.LINKING:
                    text = $"Click to link ({(Event.current.control ? "extend road" : "move endpoint")})";
                    break;

                case ToolState.UNLINKING:
                    text = "Click to unlink";
                    break;
            }

            if (text != null)
            {
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                var pos = ray.GetPoint(10) + Camera.current.transform.right * 0.2f;
                Handles.Label(pos, text);
                HandleUtility.Repaint();
            }
        }

        static void Link(IPoint a, IPoint b, bool extend)
        {
            if (a is RoadPoint && b is AnchorPoint)
            {
                Link(b, a, extend);
                return;
            }

            if (a is AnchorPoint anchor && b is RoadPoint road)
            {
                Undo.SetCurrentGroupName("Link Road");
                int group = Undo.GetCurrentGroup();
                Undo.RecordObject(road.road, "Link Road - road");
                Undo.RecordObject(road.road.GetComponent<MeshFilter>(), "Link Road - mesh");
                Undo.RecordObject(anchor.anchor, "Link Road - anchor");
                if (extend)
                {
                    road.road.AppendSegment(road.road.transform.InverseTransformPoint(anchor.position), road.isStart);
                }
                anchor.anchor.SetRoad(road.road, road.isStart);
                road.road.RefreshEndPoints();
                activePoint = anchor;
                Undo.CollapseUndoOperations(group);
            }
            else
            {
                Debug.LogWarning("A road point and an anchor point need to be selected!");
            }
        }

        public static void UnlinkSelected()
        {
            Unlink(activePoint);
        }

        static void Unlink(IPoint point)
        {
            if (ActiveInstance)
            {
                if (point is AnchorPoint anchorPoint && anchorPoint.anchor.GetConnectedRoad())
                {
                    Undo.SetCurrentGroupName("UnLink Road");
                    int group = Undo.GetCurrentGroup();
                    Undo.RecordObject(anchorPoint.anchor.GetConnectedRoad(), "UnLink Road - road");
                    Undo.RecordObject(anchorPoint.anchor.GetConnectedRoad().GetComponent<MeshFilter>(), "UnLink Road - mesh");
                    Undo.RecordObject(anchorPoint.anchor, "UnLink Road - anchor");
                    anchorPoint.anchor.Disconnect();
                    Undo.CollapseUndoOperations(group);
                }
                else
                {
                    Debug.LogWarning("No connected Point selected!");
                }
            }
            else
            {
                Debug.LogWarning("Road Link Tool not active!");
            }
        }

        static void OnUndoRedo()
        {
            if (activePoint is RoadPoint roadPoint && roadPoint.IsConnected)
            {
                activePoint = new AnchorPoint { anchor = roadPoint.isStart ? roadPoint.road.start : roadPoint.road.end };
            }
        }
    }
}
