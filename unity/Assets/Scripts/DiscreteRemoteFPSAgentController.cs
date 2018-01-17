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

	public class DiscreteRemoteFPSAgentController : BaseFPSAgentController
	{

		protected string robosimsClientToken = "";
		protected int robosimsPort = 8200;
		protected string robosimsHost = "127.0.0.1";
		protected bool serverSideScreenshot;
		protected int actionCounter;
		protected int frameCounter;
		protected float moveMagnitude;
		protected Vector3 targetTeleport;
		protected Vector3 startingHandPosition;
		private Dictionary<int, Material[]> currentMaskMaterials;
		private SimObj currentMaskObj;
		private SimObj currentHandSimObj;
		private static float gridSize = 0.25f;


		private enum emitStates {Send, Wait, Received};
		private emitStates emitState;

	

		private Dictionary<string, SimObj> inventory = new Dictionary<string, SimObj>();

		// Initialize parameters from environment variables
		protected override void Awake() {
			// load config parameters from the server side
			robosimsPort = LoadIntVariable (robosimsPort, "PORT");
			robosimsHost = LoadStringVariable(robosimsHost, "HOST");

			serverSideScreenshot = LoadBoolVariable (serverSideScreenshot, "SERVER_SIDE_SCREENSHOT");
			robosimsClientToken = LoadStringVariable (robosimsClientToken, "CLIENT_TOKEN");

			base.Awake ();

		}

		protected override void actionFinished(bool success) {
			lastActionSuccess = success;
			emitState = emitStates.Send;
			actionCounter = 0;
			targetTeleport = Vector3.zero;
		}

		protected override void Start() {
			frameCounter = actionCounter = 0;
			emitState = emitStates.Send;


			base.Start ();
			// always zero out the rotation

			transform.rotation = Quaternion.Euler (new Vector3 (0.0f, 0.0f, 0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (0.0f, 0.0f, 0.0f);
			startingHandPosition = getHand ().transform.localPosition;

		}


		public void Initialize(ServerAction action)
		{
			if (action.gridSize <= 0 || action.gridSize > 5)
			{
				errorMessage = "grid size must be in the range (0,5]";
				Debug.Log(errorMessage);
				actionFinished(false);
			}
			else
			{
				gridSize = action.gridSize;
				StartCoroutine(checkInitializeAgentLocationAction());
			}
		}

		public IEnumerator checkInitializeAgentLocationAction() {
			yield return null;

			Vector3 startingPosition = this.transform.position;
			// move ahead
			// move back

			Debug.Log("trying to find nearest location on the grid");
			float mult = 1 / gridSize;
			float grid_x1 = Convert.ToSingle(Math.Floor(this.transform.position.x * mult) / mult);
			float grid_z1 = Convert.ToSingle(Math.Floor(this.transform.position.z * mult) / mult);

			float[] xs = new float[]{ grid_x1, grid_x1 + gridSize };
			float[] zs = new float[]{ grid_z1, grid_z1 + gridSize };
			List<Vector3> validMovements = new List<Vector3> (); 
			foreach (float x in xs ) {
				foreach (float z in zs) {
					this.transform.position = startingPosition;
					yield return null;

					Vector3 target = new Vector3 (x, this.transform.position.y, z);
					Vector3 dir = target - this.transform.position;
					Vector3 movement = dir.normalized * 100.0f;
					if (movement.magnitude > dir.magnitude) {
						movement = dir;
					}

					m_CharacterController.Move (movement);

					for (int i = 0; i < actionDuration; i++) {
						yield return null;
						Vector3 diff = this.transform.position - target;
	

						if ((Math.Abs (diff.x) < 0.005) && (Math.Abs (diff.z) < 0.005)) {
							Debug.Log ("initialize move succeeded");
							validMovements.Add (movement);
							break;
						}
					}

				}
			}
			this.transform.position = startingPosition;
			yield return null;
			if (validMovements.Count > 0) {
				Debug.Log ("got total valid targets: " + validMovements.Count);
				Vector3 firstMove = validMovements [0];
				m_CharacterController.Move (firstMove);
				snapToGrid ();
				actionFinished (true);
			} else {
				Debug.Log ("no valid starting positions found");
				actionFinished (false);
			}
		}

		protected override byte[] captureScreen() {
			int width = Screen.width;
			int height = Screen.height;
			Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

			// read screen contents into the texture
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			// encode texture into JPG - XXX SHOULD SET QUALITY
			byte[] bytes = tex.EncodeToPNG();
			Destroy(tex);
			return bytes;
		}


		// Check if agent is moving
		private bool IsMoving() {
			bool moving = m_Input.sqrMagnitude > Mathf.Epsilon;
			bool turningVertical = Mathf.Abs (m_XRotation) > Mathf.Epsilon;
			bool turningHorizontal = Mathf.Abs (m_ZRotation) > Mathf.Epsilon;
			return moving || turningHorizontal || turningVertical;
		}

		private void snapToGrid() {
			float mult = 1 / gridSize;
			float gridX = Convert.ToSingle (Math.Round (this.transform.position.x * mult) / mult);
			float gridZ = Convert.ToSingle (Math.Round (this.transform.position.z * mult) / mult);
			this.transform.position = new Vector3 (gridX, transform.position.y, gridZ);

		}

		virtual protected IEnumerator checkMoveAction() {
			yield return null;

			bool result = false;

			for (int i = 0; i < actionDuration; i++) {
				Vector3 currentPosition = this.transform.position;
				Vector3 diff = currentPosition - lastPosition;
				if (
					((moveMagnitude - Math.Abs (diff.x) < 0.005) && (Math.Abs (diff.z) < 0.005)) ||
					((moveMagnitude - Math.Abs (diff.z) < 0.005) && (Math.Abs (diff.x) < 0.005))

				) {
					this.snapToGrid ();
					if (this.IsCollided())
					{

						currentPosition = this.transform.position;
						for (int j = 0; j < actionDuration; j++)
						{
							yield return null;
						}
						if ((currentPosition - this.transform.position).magnitude <= 0.001f)
						{
							result = true;

						}
					}
					else {
						result = true;
					}


					break;
				} else {
					yield return null;
				}
			}


			if (!result) {
				Debug.Log ("check move failed");
				transform.position = lastPosition;
			}

			actionFinished (result);
		}

		private IEnumerator checkDropHandObjectAction() {
			yield return null; // wait for one frame to pass


			bool result = false;
			for (int i = 0; i < 50; i++) {
				Rigidbody rb = currentHandSimObj.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
				Debug.Log("checking speed");
				Debug.Log (rb.velocity.sqrMagnitude);
				if (Math.Abs (rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude) < 0.00001) {
					Debug.Log ("object is now at rest");
					result = true;
					break;
				} else {
					Debug.Log ("object is still moving");
					yield return null;
				}

			}

			actionFinished (result);
		}

		private IEnumerator checkMoveHandAction() {

			yield return null;


			if (currentHandSimObj != null && currentHandSimObj.hasCollision) {
				Debug.Log ("hand moved object into a collision");
				errorMessage = "hand object had a collision";
				resetHand ();
				actionFinished (false);
			} else {
				actionFinished (true);
				Debug.Log ("hand move with object success");

			}
		}

		protected IEnumerator checkTeleportAction() {
			yield return null;
			bool result = false;

			for (int i = 0; i < actionDuration; i++) {
				Vector3 currentPosition = this.transform.position;
				Vector3 diff = currentPosition - targetTeleport;
				if (Math.Abs (diff.x) < 0.005 && Math.Abs (diff.z) < 0.005) {
					Debug.Log ("Teleport succeeded");
					result = true;
					break;
				} else {
					yield return null;
				}
			}

			if (!result) {
				Debug.Log ("Teleport failed");
				transform.position = lastPosition;
			}

			actionFinished (result);

		}


		// Decide whether agent has stopped actions
		// And if we need to capture a new frame
		private void LateUpdate() {
			if (emitState == emitStates.Send) {
                emitState = emitStates.Wait;
				StartCoroutine (EmitFrame ());
			}
		}



		public void RandomInitialize(ServerAction response) {
			bool success = true;
			this.excludeObjectIds = response.excludeObjectIds;
			System.Random rnd = new System.Random (response.randomSeed);
			SimObj[] simObjects = GameObject.FindObjectsOfType (typeof(SimObj)) as SimObj[];
			int pickupableCount = 0;
			for (int i = 0; i < simObjects.Length; i++) {
				SimObj so = simObjects [i];
				if (IsPickupable (so)) {
					pickupableCount++;
					SimUtil.TakeItem (so);

				}


				if (IsOpenable (so) && response.randomizeOpen) {
					if (rnd.NextDouble () < 0.5) {
						openSimObj (so);	
					} else {
						closeSimObj (so);
					}
				}
			}

			//shuffle objects
			for (int i = 0; i < simObjects.Length; i++) {
				SimObj so = simObjects [i];
				int randomIndex = rnd.Next (i, simObjects.Length);
				simObjects [i] = simObjects [randomIndex];
				simObjects [randomIndex] = so;
			}


			Dictionary<SimObjType, HashSet<SimObjType>> receptacleObjects = new Dictionary<SimObjType, HashSet<SimObjType>> ();
			foreach (ReceptacleObjectList rol in response.receptacleObjects) {
				HashSet<SimObjType> objectTypes = new HashSet<SimObjType> ();
				SimObjType receptacleType = (SimObjType)Enum.Parse (typeof(SimObjType), rol.receptacleObjectType);
				foreach (string itemObjectType in rol.itemObjectTypes) {
					objectTypes.Add ((SimObjType)Enum.Parse (typeof(SimObjType), itemObjectType));
				}
				receptacleObjects.Add (receptacleType, objectTypes);	
			}

			bool[] consumedObjects = new bool[simObjects.Length];
			int randomTries = 0;
			HashSet<SimObjType> seenObjTypes = new HashSet<SimObjType> ();
			while (pickupableCount > 0) {
				if (randomTries > 5) {
					Debug.Log ("Pickupable count still at, but couldn't place all objects: " + pickupableCount);
					success = false;
					break;
				}
				randomTries++;
				foreach (SimObj so in simObjects) {
					if (so.IsReceptacle && !excludeObject(so)) {
						int totalRandomObjects = rnd.Next (1, so.Receptacle.Pivots.Length + 1);
						for (int i = 0; i < totalRandomObjects; i++) {
							for (int j = 0; j < simObjects.Length; j++) {

								if (Array.Exists (response.excludeReceptacleObjectPairs, e => e.objectId == simObjects [j].UniqueID && e.receptacleObjectId == so.UniqueID)) {
									Debug.Log ("skipping object id receptacle id pair, " + simObjects [j].UniqueID + " " + so.UniqueID);
									continue;
								}

								if (!consumedObjects[j] && IsPickupable(simObjects[j]) && 
									receptacleObjects[so.Type].Contains(simObjects[j].Type) && 
									(!response.uniquePickupableObjectTypes || !seenObjTypes.Contains(simObjects[j].Type)) &&
									SimUtil.AddItemToReceptacle (simObjects [j], so.Receptacle)) {
									consumedObjects [j] = true;
									seenObjTypes.Add (simObjects [j].Type);
									pickupableCount--;
									break;
								}
							}

						}
					}
				}
			}
			actionFinished(success);
		}

		private IEnumerator EmitFrame() {

			frameCounter += 1;

			// we should only read the screen buffer after rendering is complete
			yield return new WaitForEndOfFrame();

			MetadataWrapper metaMessage = generateMetadataWrapper();
			metaMessage.lastAction = lastAction;
			metaMessage.lastActionSuccess = lastActionSuccess;
			metaMessage.errorMessage = errorMessage;
            metaMessage.sequenceId = this.currentSequenceId;
			List<InventoryObject> ios = new List<InventoryObject>();

			foreach (string objectId in inventory.Keys) {
				SimObj so = inventory [objectId];
				InventoryObject io = new InventoryObject();
				io.objectId = so.UniqueID;
				io.objectType = Enum.GetName (typeof(SimObjType), so.Type);
				ios.Add(io);

			}

			metaMessage.inventoryObjects = ios.ToArray();

			WWWForm form = new WWWForm();

			if (!serverSideScreenshot) {
				// create a texture the size of the screen, RGB24 format
				int width = Screen.width;
				int height = Screen.height;
				Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

				// read screen contents into the texture
				tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				tex.Apply();

				// encode texture into JPG - XXX SHOULD SET QUALITY
				byte[] bytes = tex.EncodeToPNG();
				Destroy(tex);
				form.AddBinaryData("image", bytes, "frame-" + frameCounter.ToString().PadLeft(7, '0') + ".png", "image/png");
			}

			// for testing purposes, also write to a file in the project folder
			// File.WriteAllBytes(Application.dataPath + "/Screenshots/SavedScreen" + frameCounter.ToString() + ".png", bytes);
			// Debug.Log ("Frame Bytes: " + bytes.Length.ToString());
			//string img_str = System.Convert.ToBase64String (bytes);
			form.AddField("metadata", JsonUtility.ToJson(metaMessage));
			form.AddField("token", robosimsClientToken);

			WWW w = new WWW ("http://" + robosimsHost + ":" + robosimsPort + "/train", form);
			yield return w;

			if (!string.IsNullOrEmpty (w.error)) {
            	Debug.Log ("Error: " + w.error);
                yield break;
            } else {
                emitState = emitStates.Received;
                ProcessControlCommand (w.text);
            }
		}

			


		virtual protected void moveCharacter(ServerAction action, int targetOrientation) {
			resetHand ();
			moveMagnitude = gridSize;
			if (action.moveMagnitude > 0) {
				moveMagnitude = action.moveMagnitude;
			}
			Debug.Log ("move magnitude");
			Debug.Log (moveMagnitude);
			int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
			Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3> ();
			actionOrientation.Add (0, new Vector3 (0f, 0f, 1.0f * moveMagnitude));
			actionOrientation.Add (90, new Vector3 (1.0f * moveMagnitude, 0.0f, 0.0f));
			actionOrientation.Add (180, new Vector3 (0f, 0f, -1.0f * moveMagnitude));
			actionOrientation.Add (270, new Vector3 (-1.0f * moveMagnitude, 0.0f, 0.0f));
			int delta = (currentRotation + targetOrientation) % 360;

			m_CharacterController.Move (actionOrientation[delta]);
			StartCoroutine (checkMoveAction ());

		}

		public void Move(ServerAction action) {
			resetHand ();
			if (Math.Abs (action.x) > 0) {
				moveMagnitude = Math.Abs (action.x);
			} else {
				moveMagnitude = Math.Abs (action.z);
			}

			m_CharacterController.Move (new Vector3(action.x, action.y, action.z));
			StartCoroutine (checkMoveAction ());
		}

		public void MoveLeft(ServerAction action) {
			moveCharacter (action, 270);
		}

		public void MoveRight(ServerAction action) {
			moveCharacter (action, 90);
		}

		public void MoveAhead(ServerAction action) {
			moveCharacter (action, 0);
		}

		public void MaskObject(ServerAction action) {
			
			Unmask (action);
			currentMaskMaterials = new Dictionary<int, Material[]> ();
			foreach (SimObj so in VisibleSimObjs(action)) {
				currentMaskObj = so;

				foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) {
					currentMaskMaterials [r.GetInstanceID ()] = r.materials;
					Material material = new Material(Shader.Find("Unlit/Color"));
					material.color = Color.magenta;
					Material[] newMaterials = new Material[]{ material };
					r.materials = newMaterials;
				}
				actionFinished(true);
			}	
		}

		public void Unmask(ServerAction action) {
			if (currentMaskMaterials != null) {
				foreach (SimObj so in VisibleSimObjs(true)) {
					if (so.UniqueID == currentMaskObj.UniqueID) {
						foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) {
							if (currentMaskMaterials.ContainsKey(r.GetInstanceID())) {
								r.materials = currentMaskMaterials[r.GetInstanceID()];
							}
						}

					}
				}
			}
			currentMaskMaterials = null;
			
		}

		public void MoveBack(ServerAction action) {
			moveCharacter (action, 180);
		}

		private int nearestAngleIndex(float angle, float[] array) {

			for (int i = 0; i < array.Length; i++) {
				if (Math.Abs (angle - array [i]) < 2.0f) {
					return i;
				}
			}
			return 0;
		}

		private int currentHorizonAngleIndex() {
			return nearestAngleIndex(Quaternion.LookRotation (m_Camera.transform.forward).eulerAngles.x, horizonAngles);
		}

		private int currentHeadingAngleIndex() {
			return nearestAngleIndex (Quaternion.LookRotation (transform.forward).eulerAngles.y, headingAngles);
		}

		public void RotateLeft(ServerAction controlCommand) {

		
			int index = currentHeadingAngleIndex () - 1;
			if (index < 0) {
				index = headingAngles.Length - 1;
			}
			float targetRotation = headingAngles [index];
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,targetRotation,0.0f));
			actionFinished(true);
		}



		public void RotateRight(ServerAction controlCommand) {

			int index = currentHeadingAngleIndex () + 1;
			if (index == headingAngles.Length) {
				index = 0;
			}
				
			float targetRotation = headingAngles [index];
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,targetRotation,0.0f));
			actionFinished(true);
		}


		public void addObjectInventory(SimObj simObj) {
			inventory [simObj.UniqueID] = simObj;
		}

		public SimObj removeObjectInventory(string objectId) {
			SimObj so = inventory [objectId];
			inventory.Remove (objectId);
			return so;
		}

		public bool haveTypeInventory(SimObjType objectType) {
			foreach (SimObj so in inventory.Values) {
				if (so.Type == objectType) {
					return true;
				}
			}
			return false;
		}

		public void OpenObject(ServerAction action) {
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)) {

				success = openSimObj(so);

				break;
			}
			actionFinished(success);
		}

		public void CloseObject(ServerAction action) {
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)) {
				success = closeSimObj (so);
				break;
			}
			actionFinished(success);
		}

		public SimObj[] VisibleSimObjs(ServerAction action) {
			List<SimObj> simObjs = new List<SimObj> ();

			foreach (SimObj so in VisibleSimObjs (action.forceVisible)) {

				if (!string.IsNullOrEmpty(action.objectId) && action.objectId != so.UniqueID) {
					continue;
				}

				if (!string.IsNullOrEmpty(action.objectType) && action.GetSimObjType() != so.Type) {
					continue;
				}

				simObjs.Add (so);
			}	


			return simObjs.ToArray ();

		}

		public GameObject getHand() {
			return GameObject.Find ("FirstPersonHand");
		}

		public void DropHandObject(ServerAction action) {
			if (currentHandSimObj != null) {


				Rigidbody rb = currentHandSimObj.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
				rb.constraints = RigidbodyConstraints.None;
				rb.useGravity = true;

				currentHandSimObj.transform.parent = null;
				removeObjectInventory (currentHandSimObj.UniqueID);
				StartCoroutine (checkDropHandObjectAction ());
				//currentHandSimObj = null;
				//resetHand ();
			} else {
				lastActionSuccess = false;
			}


		}

		private void resetHand() {
			GameObject hand = getHand ();
			if (currentHandSimObj != null) {
				currentHandSimObj.hasCollision = false;
			}
			hand.transform.localPosition = startingHandPosition;
			hand.transform.localRotation = Quaternion.Euler (new Vector3 ());

		}

		public void ResetHand(ServerAction action) {
			resetHand ();
		}

		private void moveHand(Vector3 target) {

			GameObject hand = getHand ();
			currentHandSimObj.hasCollision = false;
			hand.transform.position = hand.transform.position + target;
			StartCoroutine (checkMoveHandAction());

		}

		public void MoveHandForward(ServerAction action) {
			moveHand (m_CharacterController.transform.forward * action.moveMagnitude);
		}


		public void RotateHand(ServerAction action) {
			getHand().transform.localRotation = Quaternion.Euler(new Vector3(action.x, action.y, action.z));
		}

		public void MoveHandLeft(ServerAction action) {
			moveHand (m_CharacterController.transform.right * action.moveMagnitude * -1);
		}

		public void MoveHandRight(ServerAction action) {
			moveHand (m_CharacterController.transform.right * action.moveMagnitude);
		}



		public void PickupHandObject(ServerAction action) {
			GameObject hand = getHand ();
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)) {
				// XXX CHECK IF OPEN
				if (!so.IsReceptacle && !IsOpenable (so)) {
					if (inventory.Count == 0) {
						
						addObjectInventory (so);
						currentHandSimObj = so;

						Rigidbody rb = so.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
						rb.freezeRotation = true;
						rb.constraints = RigidbodyConstraints.FreezeAll;
						rb.useGravity = false;
						so.transform.position = hand.transform.position;
//						so.transform.parent = this.transform;
//						so.transform.parent = m_CharacterController.transform;
						so.transform.parent = hand.transform;
						so.ResetScale ();
						so.transform.localPosition = new Vector3 ();
						so.hasCollision = false;
						success = true;
					} 
					break;
				}
			}
			actionFinished(success);
		}

		public void PickupObject(ServerAction action)
		{
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)){
				Debug.Log(" got sim object: " + so.UniqueID);
				if (!so.IsReceptacle && (!IsOpenable (so) || so.Manipulation == SimObjManipType.Inventory)) {
					if (inventory.Count == 0) {
						Debug.Log("trying to take item: " + so.UniqueID);
						SimUtil.TakeItem (so);
						addObjectInventory (so);
						success = true;

					}
					break;
				}
			}
			actionFinished(success);
		}

		// empty target receptacle and put object into receptacle

		public void Replace(ServerAction response) {
			bool success = false;

            SimObj[] simObjs = SceneManager.Current.ObjectsInScene.ToArray ();
			foreach (SimObj rso in simObjs) {
				if (response.receptacleObjectId == rso.UniqueID) {
					foreach (SimObj so in SimUtil.GetItemsFromReceptacle(rso.Receptacle)) {
						SimUtil.TakeItem (so);
					}
					foreach (SimObj so in simObjs) {
						if (so.UniqueID == response.objectId && SimUtil.AddItemToReceptaclePivot (so, rso.Receptacle.Pivots[response.pivot])) {
							success = true;

						}
					}
				}
			}
			actionFinished(success);

		}

		public void PutObject(ServerAction response) {
			bool success = false;
			if(inventory.ContainsKey(response.objectId)) {

				foreach (SimObj rso in VisibleSimObjs(response.forceVisible)) {
					if (rso.IsReceptacle && ( rso.UniqueID == response.receptacleObjectId || rso.Type == response.ReceptableSimObjType())) {


						SimObj so = removeObjectInventory (response.objectId);
			
						if ((!IsOpenable(rso) || IsOpen(rso)) && 
							((response.forceVisible && SimUtil.AddItemToReceptacle(so, rso.Receptacle)) || 
								SimUtil.AddItemToVisibleReceptacle (so, rso.Receptacle, m_Camera))) {
							success = true;
						} else {
							addObjectInventory (so);
						}


						break;
					}
				}
			}
			actionFinished(success);
		}

		public void RotateLook(ServerAction response) {
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation,0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (response.horizon, 0.0f, 0.0f);
			actionFinished(true);

		}

		public void Rotate(ServerAction response) {
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation,0.0f));
			actionFinished(true);
		}

		public void Look(ServerAction response) {
			m_Camera.transform.localEulerAngles = new Vector3 (response.horizon, 0.0f, 0.0f);
			actionFinished(true);

		}


		public void LookDown(ServerAction response) {

			if (currentHorizonAngleIndex() > 0)
			{
				float targetHorizon = horizonAngles[currentHorizonAngleIndex() - 1];
				m_Camera.transform.localEulerAngles = new Vector3(targetHorizon, 0.0f, 0.0f);
				actionFinished(true);

			}
			else { 
				errorMessage = "can't LookDown below the min horizon angle";
				actionFinished(false);
			}
		}

		public void LookUp(ServerAction controlCommand) {

			if (currentHorizonAngleIndex() < horizonAngles.Length - 1)
			{
				float targetHorizon = horizonAngles[currentHorizonAngleIndex() + 1];
				m_Camera.transform.localEulerAngles = new Vector3(targetHorizon, 0.0f, 0.0f);
				actionFinished(true);
			}
			else {
				errorMessage = "can't LookUp beyond the max horizon angle";
				actionFinished(false);
			}
		}

		public void TeleportObject(ServerAction response) {

			foreach (SimObj so in VisibleSimObjs(true)) {
				if (so.UniqueID == response.objectId) {
					so.transform.position = new Vector3 (response.x, response.y, response.z);
				}
				
			}
			StartCoroutine (checkTeleportAction ());
		}


		public void TeleportFull(ServerAction response) {

			targetTeleport = new Vector3 (response.x, response.y, response.z);
			m_CharacterController.transform.position = targetTeleport;
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation,0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (response.horizon, 0.0f, 0.0f);

			StartCoroutine (checkTeleportAction ());
		}

		public void Teleport(ServerAction response) {
			targetTeleport = new Vector3 (response.x, response.y, response.z);
			m_CharacterController.transform.position = targetTeleport;
			if (response.rotateOnTeleport) {
				transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation,0.0f));
			}
			StartCoroutine (checkTeleportAction ());
		}


		public void Reset(ServerAction response) {
			if (string.IsNullOrEmpty(response.sceneName)){
				UnityEngine.SceneManagement.SceneManager.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name);
			} else {
				UnityEngine.SceneManagement.SceneManager.LoadScene (response.sceneName);
			}
		}

	}

}
