using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Barmetler.RoadSystem
{
	[CustomEditor(typeof(RoadMeshGenerator))]
	public class RoadMeshEditor : Editor
	{
		RoadMeshGenerator roadMeshGenerator;

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			base.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck() && roadMeshGenerator.AutoGenerate)
				roadMeshGenerator.GenerateRoadMesh();

			GUILayout.Space(10);
			Rect rect = EditorGUILayout.GetControlRect(false, 1);
			rect.height = 1;
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
			GUILayout.Space(10);

			var preset = roadMeshGenerator.settings.SourceOrientation.Preset;
			var options = MeshConversion.MeshOrientation.Presets.Keys.Append("CUSTOM").ToList();
			var selected = options.IndexOf(preset);
			if (selected != options.Count - 1) options.RemoveAt(options.Count - 1);
			var index = EditorGUILayout.Popup(
				new GUIContent(
					"Source Orientation Preset",
					"Coordinate space of the model.\n" +
					"\n" +
					"If right-handed, like in blender, face orientation is automatically swapped, so that it looks correct."
				),
				selected, options.ToArray()
			);
			if (index != selected)
			{
				Undo.RecordObject(roadMeshGenerator, "Change Source Orientation Preset");
				roadMeshGenerator.settings.SourceOrientation.Preset = options[index];
				if (roadMeshGenerator.AutoGenerate) roadMeshGenerator.GenerateRoadMesh();
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label(
				new GUIContent(
					"Auto Generate",
					"Automatically generate road mesh when something changes."
				),
				GUILayout.Width(EditorGUIUtility.labelWidth)
			);
			bool autoGenerate = GUILayout.Toggle(roadMeshGenerator.AutoGenerate, "");
			if (autoGenerate != roadMeshGenerator.AutoGenerate)
			{
				Undo.RecordObject(roadMeshGenerator, "Toggle Auto Generate");
				roadMeshGenerator.AutoGenerate = autoGenerate;
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			if (GUILayout.Button(new GUIContent("Generate Mesh", ""), GUILayout.Height(50)))
			{
				Undo.RecordObject(roadMeshGenerator, "Generate Mesh");
				roadMeshGenerator.GenerateRoadMesh();
			}
		}

		private void OnEnable()
		{
			roadMeshGenerator = (RoadMeshGenerator)target;
		}
	}
}
