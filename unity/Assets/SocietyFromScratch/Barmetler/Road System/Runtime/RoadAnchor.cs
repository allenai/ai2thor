using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler.RoadSystem
{
	public class RoadAnchor : MonoBehaviour
	{
		[SerializeField]
		private Road road;
		[SerializeField, HideInInspector]
		bool isStart;

		public Intersection Intersection => GetComponentInParent<Intersection>();

		public void SetRoad(Road road, bool isStart = true)
		{
			if (road == this.road) return;
			if (this.road != null && (this.isStart ? this.road.start : this.road.end) == this)
				throw new System.Exception("Already connected to different road!");
			this.road = road;
			this.isStart = isStart;
			if (isStart) road.start = this;
			else road.end = this;
		}

		public void Disconnect()
		{
			OnValidate();
			if (road)
			{
				if (isStart)
					road.start = null;
				else
					road.end = null;
				OnValidate();
			}
		}

		public Road GetConnectedRoad()
		{
			return road;
		}

		public Road GetConnectedRoad(out bool isStart)
		{
			isStart = this.isStart;
			return road;
		}

		public void Invalidate()
		{
			OnValidate();
			if (road) road.RefreshEndPoints();
		}

		private void OnValidate()
		{
			if (road == null || ((isStart ? road.start : road.end) != this)) road = null;
		}
	}
}
