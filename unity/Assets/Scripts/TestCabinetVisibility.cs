// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

public class TestCabinetVisibility : MonoBehaviour {
	#if UNITY_EDITOR
	public float MinDistance = 0.75f;
	public float MaxDistance = 2f;
	public int ChecksPerCabinet = 75;
	public int MaxSkippedPositions = 1000;
	public bool StopOnError = false;
	public List<SimObj> ProblemCabinets = new List<SimObj>();
	public List<Vector3> ProblemPositions = new List<Vector3> ();
	public List<float> ProblemhHorizonAngles = new List<float> ();
	public List<float> ProblemHeadingAngles = new List<float> ();
	public int ProblemIndex;
	private float[] headingAngles = new float[]{0.0f, 90.0f, 180.0f, 270.0f};
	private float[] horizonAngles = new float[]{30.0f, 0.0f, 330.0f};

	SimObj currentCabinet;
	List<Vector3> positions = new List<Vector3>();

	void OnDrawGizmos(){ 
		Gizmos.color = Color.white;
		foreach (Vector3 pos in positions) {
			Gizmos.DrawWireCube (pos + Vector3.down, Vector3.one * 0.05f);
		}

		Gizmos.color = Color.Lerp (Color.red, Color.clear, 0.5f);
		if (ProblemCabinets.Count > 0 && ProblemIndex < ProblemCabinets.Count) {
			Gizmos.DrawSphere (ProblemCabinets [ProblemIndex].CenterPoint, 0.2f);
		}

		Gizmos.color = Color.green;
		if (currentCabinet != null) {
			Gizmos.DrawSphere (currentCabinet.CenterPoint, 0.2f);
		}
	}

	void OnDisable() {
		EditorUtility.ClearProgressBar ();
	}

	public IEnumerator Start () {
		//wait for cabinets to set their startup positions
		yield return new WaitForSeconds (0.1f);

		ProblemCabinets.Clear ();
		ProblemPositions.Clear ();
		ProblemhHorizonAngles.Clear ();
		ProblemHeadingAngles.Clear ();
		yield return null;
		EditorUtility.DisplayProgressBar ("Testing cabinets", "Loading", 0f);
		Camera cam = gameObject.GetComponentInChildren<Camera> ();
		HashSet<SimObj> cabinets = new HashSet<SimObj> ();
		foreach (SimObj o in SceneManager.Current.ObjectsInScene) {
			if (o.Type == SimObjType.Cabinet) {
				cabinets.Add (o);
			}
		}
		int cabinetNum = 0;
		Vector3 startPos = transform.position;

		foreach (SimObj c in cabinets) {
			//bool seenAtLeastOnce = false;
			currentCabinet = c;
			//close the cabinet
			currentCabinet.Animator.SetBool ("AnimState1", false);
			//get some valid random positions around the cabinet
			Vector3 cabinetPos = currentCabinet.CenterPoint;
			cabinetPos.y = startPos.y;
			positions.Clear ();
			int numSkippedPositions = 0;
			while (positions.Count < ChecksPerCabinet && numSkippedPositions < MaxSkippedPositions) {
				//get a point inside a circle
				Vector3 randomPos = cabinetPos;
				randomPos.x += (Random.value - 0.5f) * MaxDistance;
				randomPos.z +=  (Random.value - 0.5f) * MaxDistance;
				randomPos.y = startPos.y;
				//if the circle is too close to the cabinet, move away
				if (Vector3.Distance (cabinetPos, randomPos) < MinDistance) {
					//don't include it
					continue;
				}
				RaycastHit hit;
				//check to see if the agent is above the floor
				bool aboveGround = Physics.Raycast (randomPos, Vector3.down, out hit, 1.1f, SimUtil.RaycastVisibleLayerMask, QueryTriggerInteraction.Ignore);
				if (!aboveGround || !hit.collider.CompareTag (SimUtil.StructureTag) || !hit.collider.name.Equals ("Floor")) {
					numSkippedPositions++;
				} else {
					aboveGround = true;
				}
				bool hasOverlap = false;
				Collider[] overlap = Physics.OverlapCapsule (randomPos + Vector3.up, randomPos + Vector3.down, 0.1f, SimUtil.RaycastVisibleLayerMask, QueryTriggerInteraction.Ignore);
				foreach (Collider collider in overlap) {
					if (!collider.name.Equals ("Floor")){
						hasOverlap = true;
						break;
					}
				}
				if (aboveGround && !hasOverlap) {
					positions.Add (randomPos);
				} else {
					numSkippedPositions++;
					if (numSkippedPositions >= MaxSkippedPositions) {
						Debug.Log ("Skipped too many positions, total is " + positions.Count);
					}
				}
			}
			//start moving
			float progressInterval = 1f / cabinets.Count;
			for (int i = 0; i < positions.Count; i++) {
				//move the agent to the random position
				transform.position = positions[i];
				//look around the world in 60-degree increments
				EditorUtility.DisplayProgressBar (
					"Testing " + cabinets.Count.ToString () + " cabinets",
					currentCabinet.ObjectID + " (position " + i.ToString () + ")",
					((float)cabinetNum / cabinets.Count) + (((float)i / ChecksPerCabinet) * progressInterval));
				for (int j = 0; j < horizonAngles.Length; j++) {
					float horizonAngle = horizonAngles [j];
					float headingAngle = 0f;
					for (int k = 0; k < headingAngles.Length; k++) {

						headingAngle = headingAngles [k];
						//at each step, check whether we can see the cabinet
						bool cabinetVisibleClosed = false;
						bool cabinetVisibleOpen = false;
						transform.localEulerAngles = new Vector3 (0f, headingAngle, 0f);
						cam.transform.localRotation = Quaternion.identity;
						cam.transform.Rotate (horizonAngle, 0f, 0f);
						//close the cabinet
						//currentCabinet.Animator.SetBool ("AnimState1", false);
						//yield return null;
						//get visible again
						foreach (SimObj visibleSimObj in SimUtil.GetAllVisibleSimObjs (cam, MaxDistance)) {
							if (visibleSimObj == currentCabinet) {
								cabinetVisibleClosed = true;
								//seenAtLeastOnce = true;
								break;
							}
						}
						//don't bother if we didn't see anything
						if (!cabinetVisibleClosed) {
							yield return null;
							continue;
						}

						//open the cabinet
						currentCabinet.Animator.SetBool ("AnimState1", true);
						yield return null;
						yield return new WaitForEndOfFrame ();
						//get visible objects again
						foreach (SimObj visibleSimObj in SimUtil.GetAllVisibleSimObjs (cam, MaxDistance)) {
							if (visibleSimObj == currentCabinet) {
								cabinetVisibleOpen = true;
								//seenAtLeastOnce = true;
								break;
							}
						}
						//now check to see if there were any we could see closed that we CAN'T see open
						if (cabinetVisibleClosed && !cabinetVisibleOpen) {
							//we found one we could see before, but can't see now
							if (!ProblemCabinets.Contains (currentCabinet)) {
								ProblemCabinets.Add (currentCabinet);
								ProblemPositions.Add (positions [i]);
								ProblemHeadingAngles.Add (headingAngle);
								ProblemhHorizonAngles.Add (horizonAngle);
							}
							yield return new WaitForSeconds (1f);
							if (StopOnError) {
								UnityEditor.Selection.activeGameObject = currentCabinet.gameObject;
								EditorUtility.ClearProgressBar ();
								yield break;
							}
						}
						//close the cabinet
						currentCabinet.Animator.SetBool ("AnimState1", false);
						yield return null;
					}
				}
			}
			/*if (!seenAtLeastOnce) {
				ProblemCabinets.Add (currentCabinet);
				ProblemPositions.Add (Vector3.zero);
				ProblemHeadingAngles.Add (0f);
				ProblemhHorizonAngles.Add (0f);
			}*/
			yield return null;
			cabinetNum++;
		}
		EditorUtility.ClearProgressBar ();
	}
	#endif
}
