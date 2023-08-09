using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler.RoadSystem
{
	/// <summary>
	/// This will be accessible during Runtime as well, not just for the Editor
	/// </summary>
	public static class RoadUtilities
	{
		public static void SetRotationAtWorldSpace(Road road, int i, Quaternion q)
		{
			if (i % 3 != 0) throw new ArgumentException("i must be divisible by 3");
			if ((i == 0 && road.start != null) || (i == road.NumPoints - 1 && road.end != null)) return;

			var forward = road.transform.InverseTransformDirection(q * Vector3.forward);
			var up = road.transform.InverseTransformDirection(q * Vector3.up);

			if (i < road.NumPoints - 1)
			{
				var l = (road[i + 1] - road[i]).magnitude;
				road.MovePoint(i + 1, road[i] + forward * l);
			}
			else
			{
				var l = (road[i - 1] - road[i]).magnitude;
				road.MovePoint(i - 1, road[i] - forward * l);
			}

			road.MoveNormal(i / 3, up);
		}

		public static Quaternion GetRotationAtWorldSpace(Road road, int i)
		{
			return GetRotationAtWorldSpace(road, i, out _, out _);
		}

		public static Quaternion GetRotationAtWorldSpace(Road road, int i, out Vector3 forwards, out Vector3 upwards)
		{
			i = road.LoopIndex(i);
			if (i % 3 != 0) throw new ArgumentException("i must be divisible by 3");
			if (i == 0)
				forwards = road[1] - road[0];
			else
				forwards = road[i] - road[i - 1];
			forwards = forwards.normalized;
			upwards = road.transform.TransformDirection(road.GetNormal(i / 3));
			forwards = road.transform.TransformDirection(forwards);
			return Quaternion.LookRotation(forwards, upwards);
		}
	}
}