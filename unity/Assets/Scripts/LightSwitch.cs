// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class LightSwitch : MonoBehaviour {

	public SimObj ParentObj;
	public bool OnByDefault = true;
	public bool EditorOn = false;
	public Light [] Lights;
	public Renderer[] SourceRenderers;
	public int[] SourceMatIndexes;
	public Material OnMaterial;
	public Material OffMaterial;
	Color equatorColor;
	Color groundColor;
	Color skyColor;

	void OnEnable () {
		equatorColor = RenderSettings.ambientEquatorColor;
		groundColor = RenderSettings.ambientGroundColor;
		skyColor = RenderSettings.ambientSkyColor;

		ParentObj = gameObject.GetComponent <SimObj> ();
		if (ParentObj == null) {
			ParentObj = gameObject.AddComponent <SimObj> ();
		}

		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		} else {
			if (OnByDefault) {
				ParentObj.Animator.SetBool ("AnimState1", true);
			}
		}
	}
	
	void Update () 
    {
        //print(SourceRenderers.Length);

		bool on = EditorOn;
		if (Application.isPlaying) 
        {
			on = ParentObj.Animator.GetBool ("AnimState1");
		}

		for (int i = 0; i < Lights.Length; i++) 
        {
			Lights [i].enabled = on;
		}

		for (int i = 0; i < SourceRenderers.Length; i++) 
        {
          //  print(i);
			Material[] sharedMats = SourceRenderers [i].sharedMaterials;
			sharedMats [SourceMatIndexes [i]] = on ? OnMaterial : OffMaterial;
			SourceRenderers [i].sharedMaterials = sharedMats;
		}

		RenderSettings.ambientEquatorColor = on ? equatorColor : Color.Lerp (equatorColor, Color.black, 0.5f);
		RenderSettings.ambientSkyColor = on ? skyColor : Color.Lerp (skyColor, Color.black, 0.5f);
		RenderSettings.ambientGroundColor = on ? groundColor : Color.Lerp (groundColor, Color.black, 0.5f);
	}
}
