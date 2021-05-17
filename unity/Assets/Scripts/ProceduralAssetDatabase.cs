using UnityEngine;
using UnityEditor;
// using System;
using System.Collections.Generic;

namespace Thor.Procedural
{
	public class ProceduralAssetDatabase : MonoBehaviour
	{
		[SerializeField]
		public AssetMap<Material> materials;
		[SerializeField] public AssetMap<GameObject> prefabs;
	}
}