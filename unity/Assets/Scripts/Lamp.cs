// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Lamp : MonoBehaviour {

	public SimObj ParentObj;
	public bool EditorOn = false;
	public bool OnByDefault = true;
	public Renderer LampshadeRenderer;
	public int LampshadeMatIndex = 0;
	public Material OnMaterial;
	public Material OffMaterial;
	public Light [] Lights;

	void OnEnable () {
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
	
	void Update () {
		bool on = EditorOn;
		if (Application.isPlaying) {
			on = ParentObj.Animator.GetBool ("AnimState1");
		}

        //lights.length was == null
		if (LampshadeRenderer == null || OnMaterial == null || OffMaterial == null || Lights.Length == 0) {
			Debug.LogError ("Required item null in lamp " + name);
			return;
		}

		Material[] sharedMats = LampshadeRenderer.sharedMaterials;
		sharedMats [LampshadeMatIndex] = on ? OnMaterial : OffMaterial;
		LampshadeRenderer.sharedMaterials = sharedMats;
		foreach (Light l in Lights) {
			l.enabled = on;
		}
	}
}
