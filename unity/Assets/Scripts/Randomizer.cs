// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public enum RandomizerStyle {
	MeshAndMat,
	GameObject,
	MatColorFixed,
	MatColorRandom,
}

[ExecuteInEditMode]
public class Randomizer : MonoBehaviour {
	public int SceneNumber = 0;
	public bool UseLocalSceneNumber;

	public RandomizerStyle Style = RandomizerStyle.MeshAndMat;

	public string Title = string.Empty;
	public GameObject[] TargetGameObjects;
	public Renderer[] TargetRenderers;
	public MeshFilter[] TargetMeshes;
	public int[] TargetMats;
	public Mesh[] Meshes;
	public Material[] Mats;
	public Color[] Colors;
	public Color ColorRangeLow = Color.grey;
	public Color ColorRangeHigh = Color.white;
	public float ColorSaturation = 1f;
	public System.Random rand = null;
	private int randomSeed;
	private bool seedInitialized;

	void OnEnable() {
		Randomize ();
	}

	public void Randomize() {

		if (!UseLocalSceneNumber) {
			if (SceneManager.Current != null) {
				SceneNumber = SceneManager.Current.SceneNumber;
			}
		}

		if (!seedInitialized) {
			randomSeed = SceneNumber;
			seedInitialized = true;
		}

		rand = new System.Random (randomSeed);

		if (Application.isPlaying) {
			StartCoroutine (StaggerRandomize ());
		} else {
			RandomizeNow ();
		}
	}

	public void Randomize (int seed) {
		randomSeed = seed;
		seedInitialized = true;
		Randomize ();
	}

	IEnumerator StaggerRandomize () {
		switch (Style) {
		case RandomizerStyle.GameObject:
		case RandomizerStyle.MeshAndMat:
			//no wait necessary
			break;

		case RandomizerStyle.MatColorFixed:
		case RandomizerStyle.MatColorRandom:
			//wait until gameObject & meshAndMat styles are done
			yield return new WaitForEndOfFrame();
			break;
		}

		RandomizeNow ();
		yield break;
	}

	void RandomizeNow () {
		if (SceneNumber < 0) {
			SceneNumber = 0;
		}

		switch (Style) {
		case RandomizerStyle.MeshAndMat:
			if (Meshes != null && Meshes.Length > 0 && TargetMeshes != null && TargetMeshes.Length > 0) {
				int meshNum = SceneNumber % Meshes.Length;
				for (int i = 0; i < TargetMeshes.Length; i++) {
					TargetMeshes[i].sharedMesh = Meshes [meshNum];
				}
			}
			if (Mats != null && Mats.Length > 0 && TargetRenderers != null && TargetRenderers.Length > 0) {
				int matNum = SceneNumber % Mats.Length;
				for (int i = 0; i < TargetRenderers.Length; i++) {
					if (TargetRenderers [i] != null) {
						Material[] sharedMats = TargetRenderers [i].sharedMaterials;
						sharedMats [TargetMats [i]] = Mats [matNum];
						TargetRenderers [i].sharedMaterials = sharedMats;
					}
				}
			}
			break;

		case RandomizerStyle.GameObject:
			if (TargetGameObjects != null && TargetGameObjects.Length > 0) {
				int goNum = rand.Next(0, TargetGameObjects.Length);
				for (int i = 0; i < TargetGameObjects.Length; i++) {
					TargetGameObjects [i].SetActive (i == goNum);
				}
			}
			break;

		case RandomizerStyle.MatColorFixed:
			if (Application.isPlaying) {
				if (Colors != null && Colors.Length > 0 && TargetRenderers != null && TargetRenderers.Length > 0) {
					int colorNum = SceneNumber % Colors.Length;
					for (int i = 0; i < TargetRenderers.Length; i++) {
						Material[] mats = TargetRenderers [i].materials;
						mats [TargetMats [i]].color = Colors [colorNum];
						TargetRenderers [i].materials = mats;
					}
				}
			}
			break;

		case RandomizerStyle.MatColorRandom:
			if (Application.isPlaying) {
				if (TargetRenderers != null && TargetRenderers.Length > 0) {
					Color randomColor = GetRandomColor (SceneNumber, ColorRangeLow, ColorRangeHigh, ColorSaturation);
					for (int i = 0; i < TargetRenderers.Length; i++) {
						Material[] mats = TargetRenderers [i].materials;
						mats [TargetMats [i]].color = randomColor;
						TargetRenderers [i].materials = mats;
					}
				}
			}
			break;
		}
	}

	public static Color GetRandomColor (int sceneNumber, Color low, Color high, float saturation) {
		Color randomColor = Color.white;
		System.Random rand = new System.Random (sceneNumber);
		float randR = ((float)rand.Next (0, 100) / 100);
		float randG = ((float)rand.Next (0, 100) / 100);
		float randB = ((float)rand.Next (0, 100) / 100);
		randomColor.r = Mathf.Lerp (low.r, high.r, randR);
		randomColor.g = Mathf.Lerp (low.g, high.g, randG);
		randomColor.b = Mathf.Lerp (low.b, high.b, randB);
		Color gsColor = new Color (randomColor.grayscale, randomColor.grayscale, randomColor.grayscale);
		randomColor = Color.Lerp (gsColor, randomColor, saturation);
		return randomColor;
	}

	//#if UNITY_EDITOR
	//void Update() {
	//	if (Application.isPlaying)
	//		return;

	//	if (UnityEditor.Selection.activeGameObject == gameObject) {
	//		Randomize ();
	//	}
	//}
	//#endif
}
