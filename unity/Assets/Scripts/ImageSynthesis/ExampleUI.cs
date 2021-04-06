using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

[RequireComponent (typeof(ImageSynthesis))]
public class ExampleUI : MonoBehaviour {

	public int width = 320;
	public int height = 200;
	private int imageCounter = 1;

	void OnGUI ()
	{
		if (GUILayout.Button("Captcha!!! (" + imageCounter + ")"))
		{
			var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			// NOTE: due to per-camera / per-object motion being calculated late in the frame and after Update()
			// capturing is moved into LateUpdate (see ImageSynthesis.cs Known Issues)
			GetComponent<ImageSynthesis>().Save(sceneName + "_" + imageCounter++, width, height);
		}
	}
}
