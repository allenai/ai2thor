using UnityEngine;
using System.Collections;

public class ViewFPS : MonoBehaviour {
	
	
	float timeA;
	int fps;
	int lastFPS;
	
	
	// Use this for initialization
	void Start () {
		
		timeA = Time.timeSinceLevelLoad;
	
	}
	
	// Update is called once per frame
	void Update () {
		
		if(Time.timeSinceLevelLoad  - timeA <= 1)
		{
			fps++;
		}
		else
		{
			lastFPS = fps + 1;
			timeA = Time.timeSinceLevelLoad;
			fps = 0;
		}
	
	}
	
	void OnGUI () {
		
		GUI.Box( new Rect(Screen.width / 2 - 50 , Screen.height - 40, 100, 30), "FPS = "+lastFPS);
		
	}
}
