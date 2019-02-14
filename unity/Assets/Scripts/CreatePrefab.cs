#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;


public class CreatePrefab : MonoBehaviour
{

	[MenuItem("Extras/Create Prefab From Selection")]
	static void DoCreatePrefab()
	{
		Transform[] transforms = Selection.transforms;
		foreach (Transform t in transforms)
		{
			PrefabUtility.CreatePrefab("Assets/Prefabs/" + t.gameObject.name + ".prefab", t.gameObject, ReplacePrefabOptions.ReplaceNameBased);
		}
	}
}
#endif