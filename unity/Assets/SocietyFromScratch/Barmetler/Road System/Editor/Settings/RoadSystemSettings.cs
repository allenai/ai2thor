using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Barmetler.RoadSystem
{
	[CreateAssetMenu(fileName = "RoadSystemSettings", menuName = "Barmetler/RoadSystemSettings")]
	public class RoadSystemSettings : ScriptableObject
	{
		[System.Serializable]
		class RoadSettings
		{
			[Tooltip("Draw bounding boxes around bezier segments?")]
			public bool drawBoundingBoxes = false;
			[Tooltip("When extending the road, whether to place it at the intersection of the mouse with the scene's geometry.")]
			public bool useRayCast = true;
			[Tooltip("If useRayCast is enabled, should the new road segment copy the surface normal of the intersection?")]
			public bool copyHitNormal = false;

			[Tooltip("The Prefab to use when creating a new road.")]
			public GameObject newRoadPrefab;
		}

		[System.Serializable]
		class IntersectionSettings
        {
			[Tooltip("The Prefab to use when creating a new intersection.")]
			public GameObject newIntersectionPrefab;
		}

		[SerializeField]
		RoadSettings roadSettings = new RoadSettings();
		[SerializeField]
		IntersectionSettings intersectionSettings = new IntersectionSettings();

		[SerializeField]
		bool drawNavigatorDebug = false;
		[SerializeField]
		bool drawNavigatorDebugPoints = false;
		[SerializeField]
		bool autoCalculateNavigator = false;

		public bool DrawBoundingBoxes => roadSettings.drawBoundingBoxes;
		public bool UseRayCast => roadSettings.useRayCast;
		public bool CopyHitNormal => roadSettings.copyHitNormal;

		public GameObject NewRoadPrefab
		{
			get => roadSettings.newRoadPrefab;
			set
			{
				roadSettings.newRoadPrefab = value;
				EditorUtility.SetDirty(this);
			}
		}

		public GameObject NewIntersectionPrefab
		{
			get => intersectionSettings.newIntersectionPrefab;
			set
			{
				intersectionSettings.newIntersectionPrefab = value;
				EditorUtility.SetDirty(this);
			}
		}

		public bool DrawNavigatorDebug
		{
			get => drawNavigatorDebug;
			set
			{
				drawNavigatorDebug = value;
				EditorUtility.SetDirty(this);
			}
		}

		public bool DrawNavigatorDebugPoints
		{
			get => drawNavigatorDebugPoints;
			set
			{
				drawNavigatorDebugPoints = value;
				EditorUtility.SetDirty(this);
			}
		}

		public bool AutoCalculateNavigator
		{
			get => autoCalculateNavigator;
			set
			{
				autoCalculateNavigator = value;
				EditorUtility.SetDirty(this);
			}
		}

		public const string settingsPath = "Assets/Settings/Editor/RoadSystemSettings.asset";

		internal static RoadSystemSettings instance = null;
		public static RoadSystemSettings Instance
		{
			get
			{
				if (instance == null)
					instance = AssetDatabase.LoadAssetAtPath<RoadSystemSettings>(settingsPath);
				if (instance == null)
				{
					instance = CreateInstance<RoadSystemSettings>();
					Directory.CreateDirectory(settingsPath);
					AssetDatabase.CreateAsset(instance, settingsPath);
					AssetDatabase.SaveAssets();
				}
				return instance;
			}
		}

		internal static SerializedObject SerializedInstance => new SerializedObject(Instance);
	}
}
