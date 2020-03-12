using UnityEngine;
using System.Collections;

public class crosshairRaycast : MonoBehaviour 
{
	public Texture2D crosshair;
	public Rect position;


	void OnGUI() {
		GUI.DrawTexture (position, crosshair);
		}

	void Update () 
	{
		    position = new Rect((Screen.width - crosshair.width) / 2, (Screen.height - crosshair.height) /2, crosshair.width, crosshair.height);
			if(Input.GetMouseButton(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if(Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity))
			{
				InteractiveObject obj = hit.collider.GetComponent<InteractiveObject>();
				if(obj)
				{
					obj.TrigegrInteraction();
					}
			}
		}
	}
}