using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler.RoadSystem
{
	using PointList = List<Bezier.OrientedPoint>;

	public class RoadSystem : MonoBehaviour
	{
		[SerializeField, HideInInspector]
		private Intersection[] intersections;
		[SerializeField, HideInInspector]
		private Road[] roads;
		[SerializeField, HideInInspector]
		private Graph graph = new Graph();

		public bool ShowDebugInfo = true;
		public bool ShowEdgeWeights = true;

		public float DistanceFactor = 1000;

		public struct Edge
		{
			public Vector3 start, end;
			public float cost;
		}

		private void OnValidate()
		{
			intersections = GetComponentsInChildren<Intersection>();
			roads = GetComponentsInChildren<Road>();
			graph.roadSystem = this;
		}

		public void RebuildAllRoads()
		{
			ConstructGraph();
			foreach (var road in roads)
			{
				road.OnCurveChanged(true);
			}
		}

		public Intersection[] Intersections => intersections;
		public Road[] Roads => roads;

		public float GetMinDistance(Vector3 worldPosition, float stepSize, float yScale,
			out Road closestRoad, out Vector3 closestPoint, out float distanceAlongRoad)
		{
			// Initiate output parameters
			closestRoad = null;
			closestPoint = Vector3.zero;
			distanceAlongRoad = 0;

			float minDst = float.PositiveInfinity;

			foreach (var road in roads)
			{
				if (road.IsMaybeCloser(worldPosition, minDst * yScale, yScale))
				{
					float dst = road.GetMinDistance(worldPosition, stepSize, yScale, out Vector3 newClosestPoint, out float newDistanceAlongRoad);
					if (dst < minDst)
					{
						minDst = dst;
						closestRoad = road;
						closestPoint = newClosestPoint;
						distanceAlongRoad = newDistanceAlongRoad;
					}
				}
			}

			return minDst;
		}

		public float GetMinDistance(Vector3 worldPosition, float yScale,
			out Intersection closestIntersection, out RoadAnchor closestAnchor, out Vector3 closestPoint, out float distanceAlongRoad)
		{
			// Initiate output parameters
			closestIntersection = null;
			closestAnchor = null;
			closestPoint = Vector3.zero;
			distanceAlongRoad = 0;

			float minDst = float.PositiveInfinity;

			foreach (var intersection in intersections)
			{
				if (!intersection) continue;
				float dst =
					Vector3.Scale(intersection.transform.position - worldPosition, new Vector3(1, yScale, 1)).magnitude -
					intersection.Radius;
				if (dst < minDst)
				{
					foreach (var anch in intersection.AnchorPoints)
					{
						var a = intersection.transform.position;
						var b = anch.transform.position;
						var l = (b - a).magnitude;
						var n = (b - a).normalized;
						var along = Vector3.Dot(worldPosition - a, n);
						float newDst;
						Vector3 closestPt;
						if (along < 0)
						{
							closestPt = a;
							along = 0;
						}
						else if (along > l)
						{
							closestPt = b;
							along = l;
						}
						else
						{
							closestPt = a + n * along;
						}
						newDst = Vector3.Scale(worldPosition - closestPt, new Vector3(1, yScale, 1)).magnitude;
						if (newDst < minDst)
						{
							minDst = newDst;
							closestPoint = closestPt;
							distanceAlongRoad = along;
							closestIntersection = intersection;
							closestAnchor = anch;
						}
					}
				}
			}

			return minDst;
		}

		public void ConstructGraph()
		{
			OnValidate();
			graph.ConstructGraph();
		}

		public List<Edge> GetGraphEdges()
		{
			return graph.GetEdges();
		}

		public List<Bezier.OrientedPoint> FindPath(
			Vector3 startPosWorld, Vector3 goalPosWorld,
			float yScale = 1, float stepSize = 1, float minDstToRoadToConnect = 10)
		{
			List<Graph.Node> nodes;

			float startDistRoad = GetMinDistance(
				startPosWorld, stepSize, yScale,
				out Road startRoad, out Vector3 startPosition1, out float startDstAlongRoad1);
			float startDistIntersection = GetMinDistance(
				startPosWorld, yScale,
				out Intersection _, out RoadAnchor startAnchor,
				out Vector3 startPosition2, out float startDstAlongRoad2);
			var startUseRoad = startDistRoad < startDistIntersection;

			float goalDistRoad = GetMinDistance(
				goalPosWorld, stepSize, yScale,
				out Road goalRoad, out Vector3 goalPosition1, out float goalDstAlongRoad1);
			float goalDistIntersection = GetMinDistance(
				goalPosWorld, yScale,
				out Intersection _, out RoadAnchor goalAnchor,
				out Vector3 goalPosition2, out float goalDstAlongRoad2);
			var goalUseRoad = goalDistRoad < goalDistIntersection;


			nodes = graph.FindPath(
				startUseRoad ? startPosition1 : startPosition2,
				startUseRoad ? startRoad : null,
				startUseRoad ? null : startAnchor,
				startUseRoad ? startDstAlongRoad1 : startDstAlongRoad2,
				goalUseRoad ? goalPosition1 : goalPosition2,
				goalUseRoad ? goalRoad : null,
				goalUseRoad ? null : goalAnchor,
				goalUseRoad ? goalDstAlongRoad1 : goalDstAlongRoad2);

			return GenerateSmoothPath(startPosWorld, goalPosWorld, nodes, stepSize, minDstToRoadToConnect);
		}

		private PointList GenerateSmoothPath(
			Vector3 startPosWorld, Vector3 goalPosWorld, List<Graph.Node> nodes,
			float stepSize = 1, float minDstToRoadToConnect = 10)
		{
			var pathPoints = new PointList();

			if (nodes.Count == 2 && nodes[0].road)
			{
				var a = Mathf.Min(nodes[0].distanceAlongRoad, nodes[1].distanceAlongRoad);
				var b = Mathf.Max(nodes[0].distanceAlongRoad, nodes[1].distanceAlongRoad);
				var pts = nodes[0].road.GetEvenlySpacedPoints(stepSize);
				float d = 0;
				for (int i = 0; i < pts.Length; ++i)
				{
					if (i > 0) d += (pts[i].position - pts[i - 1].position).magnitude;
					if (d >= a && d <= b) pathPoints.Add(pts[i].ToWorldSpace(nodes[0].road.transform));
				}
				if (nodes[0].distanceAlongRoad > nodes[1].distanceAlongRoad) pathPoints.Reverse();
			}
			else if (nodes.Count == 2 && nodes[0].anchor)
			{
				var a = nodes[0].distanceAlongRoad;
				var b = nodes[1].distanceAlongRoad;
				var intersection = nodes[0].anchor.Intersection.transform.position;
				var n = (nodes[0].anchor.transform.position - intersection).normalized;
				int nPoints = Mathf.CeilToInt(Mathf.Abs(b - a) / stepSize);
				if (nPoints == 1) ++nPoints;
				for (int i = 0; i < nPoints; ++i)
				{
					var t = Mathf.Lerp(a, b, (float)i / Mathf.Max(1, nPoints - 1));
					pathPoints.Add(new Bezier.OrientedPoint(intersection + t * n, n, nodes[0].anchor.Intersection.transform.up));
				}
			}
			else
			{
				int count = nodes.Count;
				for (int wpt = 0; wpt < count; ++wpt)
				{
					if (wpt == 0 && nodes[wpt].road != null) // Start Road Section
					{
						float dst = nodes[0].distanceAlongRoad;
						bool reverse = nodes[1].anchor == nodes[0].road.start;
						var pts = nodes[0].road.GetEvenlySpacedPoints(stepSize);
						float d = 0;
						for (int i = 0; i < pts.Length; ++i)
						{
							if (i > 0) d += (pts[i].position - pts[i - 1].position).magnitude;
							if (!reverse && d >= dst)
								pathPoints.Add(pts[i].ToWorldSpace(nodes[0].road.transform));
							else if (reverse && d <= dst)
								pathPoints.Insert(0, pts[i].ToWorldSpace(nodes[0].road.transform));
						}
						pathPoints.Insert(0, new Bezier.OrientedPoint(
							nodes[wpt].GetWorldPosition(), pathPoints[0].forward, pathPoints[0].normal));
					}
					else if (wpt == 0 && nodes[wpt].anchor != null) // Start Intersection Section
					{
						var a = nodes[wpt].position;
						var b = nodes[wpt + 1].position;
						var l = (b - a).magnitude;
						var n = (b - a).normalized;
						int amount = Mathf.Max(2, Mathf.RoundToInt(l / stepSize));
						var up1 = nodes[wpt].anchor.transform.up;
						var up2 = nodes[wpt + 1].anchor?.transform?.up ?? nodes[wpt + 1].intersection.transform.up;
						for (int i = 0; i <= amount; ++i)
						{
							float f = (float)i / amount;
							pathPoints.Add(
								new Bezier.OrientedPoint(a + f * l * n, n, Vector3.Lerp(up1, up2, f).normalized)
								.ToWorldSpace(transform));
						}
					}
					else if (wpt == count - 1 && nodes[wpt].road != null) // End Road Section
					{
						float dst = nodes[wpt].distanceAlongRoad;
						bool reverse = nodes[wpt - 1].anchor == nodes[wpt].road.end;
						var pts = nodes[wpt].road.GetEvenlySpacedPoints(stepSize);
						float d = 0;
						int insertAt = pathPoints.Count;
						for (int i = 0; i < pts.Length; ++i)
						{
							if (i > 0) d += (pts[i].position - pts[i - 1].position).magnitude;
							if (!reverse && d <= dst)
								pathPoints.Add(pts[i].ToWorldSpace(nodes[wpt].road.transform));
							else if (reverse && d >= dst)
								pathPoints.Insert(insertAt, pts[i].ToWorldSpace(nodes[wpt].road.transform));
						}
						pathPoints.Add(
							new Bezier.OrientedPoint(
								nodes[wpt].GetWorldPosition(),
								pathPoints[pathPoints.Count - 1].forward,
								pathPoints[pathPoints.Count - 1].normal));
					}
					else if (wpt == count - 1 && nodes[wpt].anchor != null) // End Intersection Section
					{
						var a = nodes[wpt - 1].position;
						var b = nodes[wpt].position;
						var l = (b - a).magnitude;
						var n = (b - a).normalized;
						int amount = Mathf.Max(2, Mathf.RoundToInt(l / stepSize));
						var up1 = nodes[wpt - 1].anchor?.transform?.up ?? nodes[wpt - 1].intersection.transform.up;
						var up2 = nodes[wpt].anchor.transform.up;
						for (int i = 1; i <= amount; ++i)
						{
							float f = (float)i / amount;
							pathPoints.Add(
								new Bezier.OrientedPoint(a + f * l * n, n, Vector3.Lerp(up1, up2, f).normalized)
								.ToWorldSpace(transform));
						}
					}
					else if (
						nodes[wpt].nodeType == Graph.Node.NodeType.ANCHOR &&
						nodes[wpt + 1].nodeType == Graph.Node.NodeType.ANCHOR)
					{
						var a = nodes[wpt].anchor;
						var b = nodes[wpt].anchor;
						var road = a.GetConnectedRoad();
						if (road)
						{
							bool reverse = road.end == a;
							int insertAt = pathPoints.Count;
							var pts = road.GetEvenlySpacedPoints(stepSize);

							for (int i = 0; i < pts.Length; ++i)
							{
								if (!reverse)
									pathPoints.Add(pts[i].ToWorldSpace(road.transform));
								else
									pathPoints.Insert(insertAt, pts[i].ToWorldSpace(road.transform));
							}
						}
						else
						{
							pathPoints.Add(new Bezier.OrientedPoint(a.transform.position, a.transform.forward, a.transform.up));
							pathPoints.Add(new Bezier.OrientedPoint(b.transform.position, -b.transform.forward, b.transform.up));
						}
					}
					else if (
						nodes[wpt].nodeType == Graph.Node.NodeType.ANCHOR &&
						nodes[wpt + 1].nodeType == Graph.Node.NodeType.INTERSECTION ||
						nodes[wpt].nodeType == Graph.Node.NodeType.INTERSECTION &&
						nodes[wpt + 1].nodeType == Graph.Node.NodeType.ANCHOR)
					{
						var a = nodes[wpt].position;
						var b = nodes[wpt + 1].position;
						var l = (b - a).magnitude;
						var n = (b - a).normalized;
						int amount = Mathf.Max(2, Mathf.RoundToInt(l / stepSize));
						var up1 = nodes[wpt].anchor?.transform?.up ?? nodes[wpt].intersection.transform.up;
						var up2 = nodes[wpt + 1].anchor?.transform?.up ?? nodes[wpt + 1].intersection.transform.up;
						for (int i = 1; i <= amount - (nodes[wpt].nodeType == Graph.Node.NodeType.INTERSECTION ? 1 : 0); ++i)
						{
							float f = (float)i / amount;
							pathPoints.Add(
								new Bezier.OrientedPoint(a + f * l * n, n, Vector3.Lerp(up1, up2, f).normalized)
								.ToWorldSpace(transform));
						}
					}
				}
			}

			// Connect Start and End Point to road
			if (pathPoints.Count > 0)
			{
				for (int end = 0; end < 2; ++end)
				{
					var pos = end == 0 ? startPosWorld : goalPosWorld;
					var point = pathPoints[end * (pathPoints.Count - 1)];
					var dst = (pos - point.position).magnitude;
					if (dst >= minDstToRoadToConnect)
					{
						int count = (int)(dst / stepSize);
						for (int i = count - 1; i >= 0; --i)
						{
							float t = (float)i / count;
							var newPoint = new Bezier.OrientedPoint(
								Vector3.Lerp(pos, point.position, t), point.forward, point.normal);
							if (end == 0)
								pathPoints.Insert(0, newPoint);
							else
								pathPoints.Add(newPoint);
						}
					}
				}
			}

			return pathPoints;
		}

		[Serializable]
		private class Graph
		{
			[Serializable]
			public class Node : AStar.NodeBase
			{
				public enum NodeType { INTERSECTION, ANCHOR, ENTRY_EXIT }

				public NodeType nodeType;
				public RoadSystem roadSystem;

				public Intersection intersection;
				public RoadAnchor anchor;
				public Road road;
				public float distanceAlongRoad;

				public Node(RoadSystem roadSystem)
				{
					this.roadSystem = roadSystem;
					nodeType = NodeType.ENTRY_EXIT;
				}

				public Node(Vector3 worldPosition, Road road, float distanceAlongRoad, RoadSystem roadSystem)
				{
					this.roadSystem = roadSystem;
					nodeType = NodeType.ENTRY_EXIT;

					position = roadSystem.transform.InverseTransformPoint(worldPosition);
					this.road = road;
					this.distanceAlongRoad = distanceAlongRoad;
				}

				public Node(Vector3 worldPosition, RoadAnchor anchor, float distanceAlongRoad, RoadSystem roadSystem)
				{
					this.roadSystem = roadSystem;
					nodeType = NodeType.ENTRY_EXIT;

					position = roadSystem.transform.InverseTransformPoint(worldPosition);
					this.anchor = anchor;
					this.distanceAlongRoad = distanceAlongRoad;
				}

				public Node(Intersection intersection, RoadSystem roadSystem)
				{
					this.roadSystem = roadSystem;
					nodeType = NodeType.INTERSECTION;

					this.intersection = intersection;
					position = roadSystem.transform.InverseTransformPoint(intersection.transform.position);
				}

				public Node(RoadAnchor anchor, RoadSystem roadSystem)
				{
					this.roadSystem = roadSystem;
					nodeType = NodeType.ANCHOR;

					this.anchor = anchor;
					position = roadSystem.transform.InverseTransformPoint(anchor.transform.position);
				}

				public Vector3 GetWorldPosition()
				{
					return roadSystem.transform.TransformPoint(position);
				}
			}

			[SerializeField]
			public RoadSystem roadSystem;
			[SerializeField]
			private List<Node> nodes = new List<Node>();
			[SerializeField]
			private TwoDimensionalArray<float> weights = new TwoDimensionalArray<float>(0, 0);

			public void ConstructGraph()
			{
				nodes = new List<Node>();
				int count = roadSystem.intersections.Select(delegate (Intersection intersection) { return 1 + intersection.AnchorPoints.Length; }).Sum();
				weights = new TwoDimensionalArray<float>(count, count);

				for (int i = 0; i < count; ++i)
					for (int j = 0; j < count; ++j)
						weights[i, j] = float.PositiveInfinity;

				foreach (var intersection in roadSystem.intersections)
				{
					intersection.Invalidate(false);
					int index = nodes.Count;
					nodes.Add(new Node(intersection, roadSystem));
					for (int i = 0; i < intersection.AnchorPoints.Length; ++i)
					{
						nodes.Add(new Node(intersection.AnchorPoints[i], roadSystem));
						weights[index, index + i + 1] = weights[index + i + 1, index] = (nodes[index].position - nodes[index + i + 1].position).magnitude;
					}
				}

				foreach (var road in roadSystem.roads)
				{
					road.OnValidate();
					if (road.start != null && road.end != null)
					{
						int startIndex = FindIndex(road.start, nodes);
						int endIndex = FindIndex(road.end, nodes);
						if (startIndex != -1 && endIndex != -1)
							weights[startIndex, endIndex] = weights[endIndex, startIndex] = road.GetLength();
					}
				}

				// Connect all nodes with 1000x their distance, so that islands can still connect, and not exception will be thrown.
				// This shouldn't really be needed, because islands shouldn't exist in a roadsystem, so it's more of a failsafe.
				for (int i = 0; i < count; ++i)
				{
					for (int j = i; j < count; ++j)
					{
						var weight = (nodes[i].position - nodes[j].position).magnitude * roadSystem.DistanceFactor;
						if (float.IsInfinity(weights[i, j])) weights[i, j] = weight;
						if (float.IsInfinity(weights[j, i])) weights[j, i] = weight;
					}
				}
			}

			public List<Edge> GetEdges()
			{
				var ret = new List<Edge>();
				for (int from = 0; from < nodes.Count; ++from)
				{
					for (int to = 0; to < nodes.Count; ++to)
					{
						if (weights[from, to] < 5e3 && weights[from, to] > 1e-3)
						{
							var edge = new Edge
							{
								start = roadSystem.transform.TransformPoint(nodes[from].position),
								end = roadSystem.transform.TransformPoint(nodes[to].position),
								cost = weights[from, to]
							};
							ret.Add(edge);
						}
					}
				}
				return ret;
			}

			private static int FindIndex(Intersection intersection, List<Node> nodes)
			{
				if (intersection == null) return -1;
				return nodes.FindIndex(0, nodes.Count, delegate (Node node)
				{
					return node.nodeType == Node.NodeType.INTERSECTION && node.intersection == intersection;
				});
			}

			private static int FindIndex(RoadAnchor anchor, List<Node> nodes)
			{
				if (anchor == null) return -1;
				return nodes.FindIndex(0, nodes.Count, delegate (Node node)
				{
					return node.nodeType == Node.NodeType.ANCHOR && node.anchor == anchor;
				});
			}

			public List<Node> FindPath(Vector3 startPosWorld, Road startRoad, float startDistanceAlongRoad,
				Vector3 goalPosWorld, Road goalRoad, float goalDistanceAlongRoad)
			{
				return FindPath(startPosWorld, startRoad, null, startDistanceAlongRoad, goalPosWorld, goalRoad, null, goalDistanceAlongRoad);
			}

			public List<Node> FindPath(Vector3 startPosWorld, RoadAnchor startAnchor, float startDistanceAlongRoad,
				Vector3 goalPosWorld, RoadAnchor goalAnchor, float goalDistanceAlongRoad)
			{
				return FindPath(startPosWorld, null, startAnchor, startDistanceAlongRoad, goalPosWorld, null, goalAnchor, goalDistanceAlongRoad);
			}

			public List<Node> FindPath(Vector3 startPosWorld, Road startRoad, RoadAnchor startAnchor, float startDistanceAlongRoad,
				Vector3 goalPosWorld, Road goalRoad, RoadAnchor goalAnchor, float goalDistanceAlongRoad)
			{
				List<Node> nodes = this.nodes.ToList();

				if (goalRoad != null)
				{
					nodes.Insert(0, new Node(goalPosWorld, goalRoad, goalDistanceAlongRoad, roadSystem));
				}
				else
				{
					nodes.Insert(0, new Node(goalPosWorld, goalAnchor, goalDistanceAlongRoad, roadSystem));
				}
				if (startRoad != null)
				{
					nodes.Insert(0, new Node(startPosWorld, startRoad, startDistanceAlongRoad, roadSystem));
				}
				else
				{
					nodes.Insert(0, new Node(startPosWorld, startAnchor, startDistanceAlongRoad, roadSystem));
				}

				var weights = new TwoDimensionalArray<float>(nodes.Count, nodes.Count);
				this.weights.CopyInto(float.PositiveInfinity, weights, new Vector2Int(2, 2), Vector2Int.zero);

				int startIndex = 0;
				int goalIndex = 1;
				var startPosLocal = nodes[startIndex].position;
				var goalPosLocal = nodes[goalIndex].position;

				for (int i = 0; i < weights.Width; ++i)
					for (int j = 0; j < 2; ++j)
						weights[i, j == 0 ? startIndex : goalIndex] = weights[j == 0 ? startIndex : goalIndex, i] =
							(nodes[j == 0 ? startIndex : goalIndex].position - nodes[i].position).magnitude * roadSystem.DistanceFactor;

				if (startRoad == goalRoad && startRoad != null)
				{
					weights[startIndex, goalIndex] = weights[goalIndex, startIndex] = Mathf.Abs(startDistanceAlongRoad - goalDistanceAlongRoad);

					int roadStartIndex = FindIndex(startRoad.start, nodes);
					int roadEndIndex = FindIndex(startRoad.end, nodes);

					if (startDistanceAlongRoad > goalDistanceAlongRoad)
					{
						// start to roadEnd, goal to roadStart
						if (roadEndIndex != -1)
							weights[startIndex, roadEndIndex] = weights[roadEndIndex, startIndex] =
								startRoad.GetLength() - startDistanceAlongRoad;

						if (roadStartIndex != -1)
							weights[goalIndex, roadStartIndex] = weights[roadStartIndex, goalIndex] =
								goalDistanceAlongRoad;
					}
					else
					{
						// goal to roadEnd, start to roadStart
						if (roadEndIndex != -1)
							weights[goalIndex, roadEndIndex] = weights[roadEndIndex, goalIndex] =
								startRoad.GetLength() - goalDistanceAlongRoad;

						if (roadStartIndex != -1)
							weights[startIndex, roadStartIndex] = weights[roadStartIndex, startIndex] =
								startDistanceAlongRoad;
					}
				}
				else if (startAnchor == goalAnchor && startAnchor != null)
				{
					weights[startIndex, goalIndex] = weights[goalIndex, startIndex] = (startPosLocal - goalPosLocal).magnitude;

					int anchorIndex = FindIndex(startAnchor, nodes);
					int intersectionIndex = FindIndex(startAnchor.Intersection, nodes);
					float length = (startAnchor.transform.position - startAnchor.Intersection.transform.position).magnitude;

					if (startDistanceAlongRoad > goalDistanceAlongRoad)
					{
						weights[startIndex, anchorIndex] = weights[anchorIndex, startIndex] = length - startDistanceAlongRoad;
						weights[goalIndex, intersectionIndex] = weights[intersectionIndex, goalIndex] = goalDistanceAlongRoad;
					}
					else
					{
						weights[startIndex, anchorIndex] = weights[anchorIndex, startIndex] = startDistanceAlongRoad;
						weights[goalIndex, intersectionIndex] = weights[intersectionIndex, goalIndex] = length - goalDistanceAlongRoad;
					}
				}
				else
				{
					// connect start and end to roadEnds
					for (int i = 0; i < 2; ++i)
					{
						Road road = (i == 0 ? startRoad : goalRoad);
						RoadAnchor anchor = (i == 0 ? startAnchor : goalAnchor);
						int pointIndex = i == 0 ? startIndex : goalIndex;
						float distanceAlongRoad = i == 0 ? startDistanceAlongRoad : goalDistanceAlongRoad;

						if (road != null)
						{
							int roadStartIndex = FindIndex(road.start, nodes);
							int roadEndIndex = FindIndex(road.end, nodes);

							if (roadEndIndex != -1)
								weights[pointIndex, roadEndIndex] = weights[roadEndIndex, pointIndex] =
									road.GetLength() - distanceAlongRoad;

							if (roadStartIndex != -1)
								weights[pointIndex, roadStartIndex] = weights[roadStartIndex, pointIndex] =
									distanceAlongRoad;
						}
						else if (anchor != null)
						{
							int intersectionIndex = FindIndex(anchor.Intersection, nodes);
							int anchorIndex = FindIndex(anchor, nodes);
							float length = (anchor.transform.position - anchor.Intersection.transform.position).magnitude;

							weights[intersectionIndex, pointIndex] = weights[pointIndex, intersectionIndex] = distanceAlongRoad;
							weights[anchorIndex, pointIndex] = weights[pointIndex, anchorIndex] = length - distanceAlongRoad;
						}
					}
				}

				// PathFinding

				// float DijkstraHeuristic(Node a, Node b) { return 0; }

				var path = AStar.FindShortestPath(nodes, weights, nodes[startIndex], nodes[goalIndex] /*, DijkstraHeuristic*/);

				return path;
			}
		}
	}
}
