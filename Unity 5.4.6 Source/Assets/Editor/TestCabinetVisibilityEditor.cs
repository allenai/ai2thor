using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(TestCabinetVisibility))]
public class TestCabinetVisibilityEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector ();

		TestCabinetVisibility t = (TestCabinetVisibility)target;

		if (!Application.isPlaying) {
			if (GUILayout.Button ("Next position")) {
				t.ProblemIndex++;
				if (t.ProblemIndex >= t.ProblemCabinets.Count) {
					t.ProblemIndex = 0;
				}
				float headingAngle = t.ProblemHeadingAngles [t.ProblemIndex];
				float horizonAngle = t.ProblemhHorizonAngles [t.ProblemIndex];
				Vector3 position = t.ProblemPositions [t.ProblemIndex];
				Camera cam = t.GetComponentInChildren<Camera> ();

				t.transform.position = position;
				t.transform.localEulerAngles = new Vector3 (0f, headingAngle, 0f);
				cam.transform.localRotation = Quaternion.identity;
				cam.transform.Rotate (horizonAngle, 0f, 0f);
			}
		}
	}
}
