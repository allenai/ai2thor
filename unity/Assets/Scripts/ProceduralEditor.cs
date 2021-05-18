// // Copyright Allen Institute for Artificial Intelligence 2017
// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
// using UnityStandardAssets.Characters.FirstPerson;
// using System;
// using MessagePack.Resolvers;
// using MessagePack.Formatters;
// using MessagePack;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
// using Newtonsoft.Json.Serialization;

// #if UNITY_EDITOR
// using UnityEditor.SceneManagement;
// using UnityEditor;
// #endif

// namespace Thor.Procedural.Editor
// {
// 	public List<T> FindAssetsByType<T>() where T : UnityEngine.Object {
// 		List<T> assets = new List<T>();
// 		string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).ToString().Replace("UnityEngine.", "")));
// 		for (int i = 0; i < guids.Length; i++) {
// 			string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
// 			T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
// 			if (asset != null) {
// 				assets.Add(asset);
// 			}
// 		}
// 		return assets;
// 	}

// 	public List<GameObject> FindPrefabsInAssets() {
// 		var assets = new List<GameObject>();
// 		string[] guids = AssetDatabase.FindAssets("t:prefab");
// 		for (int i = 0; i < guids.Length; i++) {
// 			string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
// 			GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
// 			if (asset != null) {
// 				assets.Add(asset);
// 			}
// 		}
// 		return assets;
// 	}
// }