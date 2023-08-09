using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Barmetler.RoadSystem
{
	public class NewRoadWizard : ScriptableWizard
	{
		[MenuItem("Tools/RoadSystem/Create Road Wizard", priority = 3)]
		public static void CreateWizard()
		{
			DisplayWizard<NewRoadWizard>("Create Road", "Create", "Apply");
		}

		private GameObject road = null;

		private void OnEnable()
		{
			minSize = new Vector2(350, 200);
			helpString = "Selecte a prefab for the new road! You can also set that prefab in [Project Settings/MB RoadSystem]";
			road = RoadSystemSettings.Instance.NewRoadPrefab;
		}

		protected override bool DrawWizardGUI()
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Prefab", GUILayout.Width(EditorGUIUtility.labelWidth));
			road = EditorGUILayout.ObjectField(road, typeof(GameObject), false) as GameObject;
			EditorGUILayout.EndHorizontal();

			return EditorGUI.EndChangeCheck();
		}

		private void OnWizardCreate()
		{
			OnWizardOtherButton();
			RoadMenu.CreateRoad();
		}

		private void OnWizardOtherButton()
		{
			RoadSystemSettings.Instance.NewRoadPrefab = road;
		}
	}
}
