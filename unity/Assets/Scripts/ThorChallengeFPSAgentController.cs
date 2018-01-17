// Copyright Allen Institute for Artificial Intelligence 2017
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine.SceneManagement;

using UnityEngine;


namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]

	public class ThorChallengeFPSAgentController : DiscreteRemoteFPSAgentController
	{

		private SceneConfigurationList sceneConfigList;
		private SceneConfiguration sceneConfig;

		override protected void Start() {
			base.Start ();

			string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name;
			string json = ThorChallengeInfo.RawSceneInfo[sceneName];
			sceneConfigList = JsonUtility.FromJson<SceneConfigurationList>(json);
		}
		private Vector3 nearestGridPoint(GridPoint[] gridPoints, Vector3 target) {
			foreach (GridPoint gp in gridPoints) {
				if (Math.Abs(target.x - gp.x) < 0.01 && Math.Abs(target.z - gp.z) < 0.01) {
					return new Vector3 (gp.x, gp.y, gp.z);
				}
			}

			return new Vector3 ();
		}
		protected MetadataWrapper generateMetadataWrapper() {
			MetadataWrapper metaMessage = base.generateMetadataWrapper ();

			if (ThorChallengeInfo.IsValidationScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name)) {
				metaMessage.agent = new ObjectMetadata ();
				metaMessage.objects = new ObjectMetadata[]{};
				metaMessage.collidedObjects = new string[]{};
			}

			return metaMessage;
		}


		public void InitializePositionRotation(ServerAction response) {
			
			GridPoint gp = sceneConfig.gridPoints [response.agentPositionIndex];
			m_CharacterController.transform.position = new Vector3 (gp.x, gp.y, gp.z) ;
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation,0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (response.horizon, 0.0f, 0.0f);

		}

		public void Initialize(ServerAction response) {
			sceneConfig = sceneConfigList.configs [response.sceneConfigIndex];
			Dictionary<string, SimObj> simobjLookup = new Dictionary<string, SimObj> ();
			SimObj[] simObjects = GameObject.FindObjectsOfType (typeof(SimObj)) as SimObj[];

			for (int i = 0; i < simObjects.Length; i++) {
				SimObj so = simObjects [i];
				simobjLookup [so.UniqueID] = so;
				// only pick everything up if we have a list of receptacle/object pairs
				if (IsPickupable (so) && sceneConfig.receptacleObjectPairs.Length > 0) {
					SimUtil.TakeItem (so);
				}
				if (IsOpenable (so)) {

					if (Array.IndexOf (sceneConfig.openReceptacles, so.UniqueID) >= 0) {
						openSimObj (so);
					} else {
						closeSimObj (so);
					}
				}

			}
			foreach (ReceptacleObjectPair rop in sceneConfig.receptacleObjectPairs) {
				SimObj o = simobjLookup [rop.objectId];
				SimObj r = simobjLookup [rop.receptacleObjectId];
				SimUtil.AddItemToReceptaclePivot (o, r.Receptacle.Pivots [rop.pivot]);
			}

			InitializePositionRotation (response);

		}




		private void moveCharacterContinuous(ServerAction action, int targetOrientation) {			
			moveMagnitude = action.moveMagnitude;
			int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
			Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3> ();
			actionOrientation.Add (0, transform.forward );
			actionOrientation.Add (90, transform.right);
			actionOrientation.Add (180, transform.forward * -1);
			actionOrientation.Add (270, transform.right * -1);
			Vector3 v = actionOrientation [targetOrientation] * moveMagnitude;
			m_CharacterController.Move (v);
		}

		private void moveCharacterGrid(ServerAction action, int targetOrientation) {			
			moveMagnitude = action.moveMagnitude;
			int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
			Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3> ();
			actionOrientation.Add (0, new Vector3 (0f, 0f, 1.0f * moveMagnitude));
			actionOrientation.Add (90, new Vector3 (1.0f * moveMagnitude, 0.0f, 0.0f));
			actionOrientation.Add (180, new Vector3 (0f, 0f, -1.0f * moveMagnitude));
			actionOrientation.Add (270, new Vector3 (-1.0f * moveMagnitude, 0.0f, 0.0f));
			int delta = (currentRotation + targetOrientation) % 360;
			GridPoint[] gridPoints = sceneConfig.gridPoints;
			Vector3 currentPoint = nearestGridPoint (gridPoints, transform.position);
			targetTeleport = nearestGridPoint (gridPoints, (new Vector3 (currentPoint.x, currentPoint.y, currentPoint.z)) + actionOrientation [delta]);


			//we don't move the agent if it is to a point that moves agent upwards
			if (targetTeleport == new Vector3 () || targetTeleport.y > 1.35) {
				lastActionSuccess = false;
			} else {
				m_CharacterController.transform.position = targetTeleport;
			}
		}

		override protected void moveCharacter(ServerAction action, int targetOrientation) {

			if (action.continuousMode) {
				moveCharacterContinuous (action, targetOrientation);
			} else {
				moveCharacterGrid (action, targetOrientation);
			}


		}

	}

}
