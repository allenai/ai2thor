using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler.RoadSystem
{
	using PointList = List<Bezier.OrientedPoint>;

	public class RoadSystemNavigator : MonoBehaviour
	{
		public RoadSystem currentRoadSystem;

		public Vector3 Goal = Vector3.zero;

		public float GraphStepSize = 1;
		public float MinDistanceYScale = 1;
		public float MinDistanceToRoadToConnect = 10;

		public PointList CurrentPoints { private set; get; } = new PointList();
		private AsyncUpdater<PointList> currentPoints;

		private void Awake()
		{
			currentPoints = new AsyncUpdater<PointList>(this, GetNewWayPoints, new PointList(), 1f / 144);
		}

		private void Update()
		{
			currentPoints.Update();
		}

		private void FixedUpdate()
		{
			var points = currentPoints.GetData();
			if (points != CurrentPoints)
			{
				CurrentPoints = points;
				RemovePointsAhead();
			}

			RemovePointsBehind();
		}

		public float GetMinDistance(out Road road, out Vector3 closestPoint, out float distanceAlongRoad)
		{
			if (currentRoadSystem == null)
			{
				road = null;
				closestPoint = Vector3.zero;
				distanceAlongRoad = 0;
				return float.PositiveInfinity;
			}
			return currentRoadSystem.GetMinDistance(transform.position, Mathf.Max(0.1f, GraphStepSize), MinDistanceYScale, out road, out closestPoint, out distanceAlongRoad);
		}

		public float GetMinDistance(
			out Intersection intersection, out RoadAnchor anchor, out Vector3 closestPoint, out float distanceAlongRoad)
		{
			if (currentRoadSystem == null)
			{
				intersection = null;
				anchor = null;
				closestPoint = Vector3.zero;
				distanceAlongRoad = 0;
				return float.PositiveInfinity;
			}
			return currentRoadSystem.GetMinDistance(
				transform.position, MinDistanceYScale, out intersection, out anchor, out closestPoint, out distanceAlongRoad);
		}

		void RemovePointsBehind()
		{
			var pos = transform.position;
			int count = 0;
			for (; count < CurrentPoints.Count - 1; ++count)
			{
				// if next point is further away, stop (but don't stop if current point is really close)
				float sqrDst = (CurrentPoints[count].position - pos).sqrMagnitude;
				if (
					sqrDst < (CurrentPoints[count + 1].position - pos).sqrMagnitude &&
					sqrDst > GraphStepSize / 2 * GraphStepSize / 2
					) break;
			}

			if (count > 0)
			{
				CurrentPoints.RemoveRange(0, count);
			}
		}

		void RemovePointsAhead()
		{
			var pos = Goal;
			int count = 0;
			for (; count < CurrentPoints.Count - 1; ++count)
			{
				// if next point is further away, stop (but don't stop if current point is really close)
				float sqrDst = (CurrentPoints[CurrentPoints.Count - 1 - count].position - pos).sqrMagnitude;
				if (
					sqrDst < (CurrentPoints[CurrentPoints.Count - 1 - count - 1].position - pos).sqrMagnitude &&
					sqrDst > GraphStepSize / 2 * GraphStepSize / 2
					) break;
			}

			if (count > 0)
			{
				CurrentPoints.RemoveRange(CurrentPoints.Count - count, count);
			}
		}

		public void CalculateWayPointsSync()
		{
			CurrentPoints = GetNewWayPoints();
		}

		PointList GetNewWayPoints()
		{
			return currentRoadSystem.FindPath(
				transform.position, Goal, MinDistanceYScale,
				Mathf.Max(0.1f, GraphStepSize), MinDistanceToRoadToConnect);
		}
	}
}
