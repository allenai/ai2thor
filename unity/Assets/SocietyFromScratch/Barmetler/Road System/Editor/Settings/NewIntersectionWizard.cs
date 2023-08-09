using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Barmetler.RoadSystem
{
	public class NewIntersectionWizard : ScriptableWizard
	{
		[MenuItem("Tools/RoadSystem/Create Intersection Wizard", priority = 3)]
		public static void CreateWizard()
		{
			DisplayWizard<NewIntersectionWizard>("Create Intersection", "Create", "Apply");
		}

		private GameObject intersection = null;

		private void OnEnable()
		{
			minSize = new Vector2(350, 200);
			helpString = "Selecte a prefab for the new intersection! You can also set that prefab in [Project Settings/MB RoadSystem]";
			intersection = RoadSystemSettings.Instance.NewIntersectionPrefab;
		}

		protected override bool DrawWizardGUI()
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Prefab", GUILayout.Width(EditorGUIUtility.labelWidth));
			intersection = EditorGUILayout.ObjectField(intersection, typeof(GameObject), false) as GameObject;
			EditorGUILayout.EndHorizontal();

			return EditorGUI.EndChangeCheck();
		}

		private void OnWizardCreate()
		{
			OnWizardOtherButton();
			RoadMenu.CreateIntersection();
		}

		private void OnWizardOtherButton()
		{
			RoadSystemSettings.Instance.NewIntersectionPrefab = intersection;
		}
	}
}
