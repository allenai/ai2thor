using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler.RoadSystem
{
	public class Intersection : MonoBehaviour
	{
		[SerializeField, HideInInspector]
		RoadAnchor[] anchorPoints = new RoadAnchor[0];
		[SerializeField, HideInInspector]
		float radius;

		public RoadAnchor[] AnchorPoints { get => anchorPoints; }

		private void OnValidate()
		{
			anchorPoints = GetComponentsInChildren<RoadAnchor>();
			radius = Mathf.Sqrt(anchorPoints.Length > 0 ? anchorPoints.Select(e => (e.transform.position - transform.position).sqrMagnitude).Max() : 0);
		}

		private void Awake()
		{
			OnValidate();
		}

		public void Invalidate(bool updateMesh = true)
		{
			OnValidate();
			foreach (var p in anchorPoints)
			{
				p.Invalidate();
			}
		}

		public float Radius => radius;
	}
}
