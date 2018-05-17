// Copyright Allen Institute for Artificial Intelligence 2017
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using System.Globalization;
using UnityEngine.SceneManagement;

using UnityEngine;


namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]

	public class DiscreteRemoteFPSAgentController : BaseFPSAgentController
	{

		protected int actionCounter;
		protected float moveMagnitude;
		protected Vector3 targetTeleport;
		protected Vector3 startingHandPosition;
		private Dictionary<int, Material[]> currentMaskMaterials;
		private SimObj currentMaskObj;
		private SimObj currentHandSimObj;
		private static float gridSize = 0.25f;

		private bool continuousMode;




	

		private Dictionary<string, SimObj> inventory = new Dictionary<string, SimObj>();

        protected DebugFPSAgentController DebugComponent = null;
        protected Canvas DebugCanvas = null;

		// Initialize parameters from environment variables
		protected override void Awake() {
			// load config parameters from the server side

			base.Awake ();

            DebugCanvas = GameObject.Find("DebugCanvas").GetComponent<Canvas>();
            DebugComponent = this.GetComponent<DebugFPSAgentController>();

            DebugCanvas.enabled = false;
            DebugComponent.enabled = false;
		}

		protected override void actionFinished(bool success) {
			lastActionSuccess = success;
			actionComplete = true;
			actionCounter = 0;
			targetTeleport = Vector3.zero;
		}

		protected override void Start() {
			actionComplete = true;

			base.Start ();
			// always zero out the rotation

			transform.rotation = Quaternion.Euler (new Vector3 (0.0f, 0.0f, 0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (0.0f, 0.0f, 0.0f);
			//startingHandPosition = getHand ().transform.localPosition;
			snapToGrid ();

		}


		public void Initialize(ServerAction action)
		{

			if (action.continuous) {
				continuousMode = true;
				if (action.gridSize == 0) {
					action.gridSize = 0.25f;
				}
			} 

			if (action.visibilityDistance > 0.0f) {
				this.maxVisibleDistance = action.visibilityDistance;
			}

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

		public override MetadataWrapper generateMetadataWrapper() {


			MetadataWrapper metaMessage = base.generateMetadataWrapper ();
			metaMessage.lastAction = lastAction;
			metaMessage.lastActionSuccess = lastActionSuccess;
			metaMessage.errorMessage = errorMessage;

			if (errorCode != ServerActionErrorCode.Undefined) {
				metaMessage.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
			}

			List<InventoryObject> ios = new List<InventoryObject>();

					foreach (string objectId in inventory.Keys) {
						SimObj so = inventory [objectId];
						InventoryObject io = new InventoryObject();
						io.objectId = so.UniqueID;
						io.objectType = Enum.GetName (typeof(SimObjType), so.Type);
						ios.Add(io);
			
					}

			metaMessage.inventoryObjects = ios.ToArray();

			return metaMessage;
		}

		public IEnumerator checkInitializeAgentLocationAction() {
			yield return null;

			Vector3 startingPosition = this.transform.position;
			// move ahead
			// move back

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
					movement.y = Physics.gravity.y * this.m_GravityMultiplier;

					m_CharacterController.Move (movement);

					for (int i = 0; i < actionDuration; i++) {
						yield return null;
						Vector3 diff = this.transform.position - target;
	

						if ((Math.Abs (diff.x) < 0.005) && (Math.Abs (diff.z) < 0.005)) {
							validMovements.Add (movement);
							break;
						}
					}

				}
			}
			this.transform.position = startingPosition;
			yield return null;
			if (validMovements.Count > 0) {
				Debug.Log ("Initialize: got total valid initial targets: " + validMovements.Count);
				Vector3 firstMove = validMovements [0];
				firstMove.y = Physics.gravity.y* this.m_GravityMultiplier;

				m_CharacterController.Move (firstMove);
				snapToGrid ();
				actionFinished (true);
			} else {
				Debug.Log ("Initialize: no valid starting positions found");
				actionFinished (false);
			}
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

		virtual protected IEnumerator checkWaitAction(bool success) {
			yield return null;
			actionFinished(success);
		}



		private string formatPos(Vector3 pos) {
			return "x=" + pos.x.ToString("F3") + " y=" + pos.y.ToString("F3") + " z=" + pos.z.ToString("F3");
		}

		virtual protected IEnumerator checkMoveAction(ServerAction action) {

			yield return null;

			if (continuousMode) {
				actionFinished(true);
				yield break;
			}

			bool result = false;


			for (int i = 0; i < actionDuration; i++) {
				Vector3 currentPosition = this.transform.position;
				Vector3 zeroY = new Vector3 (1.0f, 0.0f, 1.0f);

				float distance = Vector3.Distance (Vector3.Scale(lastPosition, zeroY), Vector3.Scale(currentPosition, zeroY));
				if (Math.Abs(moveMagnitude - distance) < 0.005)			
				{
					currentPosition = this.transform.position;

					if (action.snapToGrid){
						this.snapToGrid();
					}


					yield return null;
					if (this.IsCollided())
					{
						for (int j = 0; j < actionDuration; j++)
						{
							yield return null;
						}
		
					}

					if ((currentPosition - this.transform.position).magnitude <= 0.001f){
						result = true;
					}

					break;
				} else {
					yield return null;
				}
			}


			// Debug.Log(this.transform.position.z.ToString("F3", CultureInfo.InvariantCulture));

			// if for some reason we moved in the Y space too much, then we assume that something is wrong
			// In FloorPlan 223 @ x=-1, z=2.0 its possible to move through the wall using move=0.5

			if (Math.Abs((this.transform.position - lastPosition).y) > 0.2) {
				result = false;
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
			for (int i = 0; i < 30; i++) {
				Debug.Log (currentHandSimObj.transform.position.ToString());
				Rigidbody rb = currentHandSimObj.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
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


		#if UNITY_EDITOR
            public void Update() 
            {
              if ( Input.GetKeyDown(KeyCode.BackQuote))
              {
                  DebugCanvas.enabled = true;
                  DebugComponent.enabled = true;
              }
            }
		#endif



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

		virtual protected void moveCharacter(ServerAction action, int targetOrientation) {
			resetHand ();
			moveMagnitude = gridSize;
			if (action.moveMagnitude > 0) {
				moveMagnitude = action.moveMagnitude;
			}
			int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
			Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3> ();
			actionOrientation.Add (0, new Vector3 (0f, 0f, 1.0f));
			actionOrientation.Add (90, new Vector3 (1.0f, 0.0f, 0.0f));
			actionOrientation.Add (180, new Vector3 (0f, 0f, -1.0f));
			actionOrientation.Add (270, new Vector3 (-1.0f, 0.0f, 0.0f));
			int delta = (currentRotation + targetOrientation) % 360;

			Vector3 m;
			if (actionOrientation.ContainsKey (delta)) {
				m = actionOrientation [delta];
				
			} else {
				actionOrientation = new Dictionary<int, Vector3> ();
				actionOrientation.Add (0, transform.forward);
				actionOrientation.Add (90, transform.right);
				actionOrientation.Add (180, transform.forward * -1);
				actionOrientation.Add (270, transform.right * -1);
				m = actionOrientation[targetOrientation];
			}

			m *= moveMagnitude;

            m.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move (m);
			StartCoroutine (checkMoveAction (action));

		}

		public void Move(ServerAction action) {
			resetHand ();
			if (Math.Abs (action.x) > 0) {
				moveMagnitude = Math.Abs (action.x);
			} else {
				moveMagnitude = Math.Abs (action.z);
			}

            action.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move (new Vector3(action.x, action.y, action.z));
			StartCoroutine (checkMoveAction (action));
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



		public void OpenObject(ServerAction action) 
        {
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)) 
            {

				success = openSimObj(so);

				break;
			}

			StartCoroutine(checkWaitAction(success));
		}

		public void CloseObject(ServerAction action) {
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)) {
				success = closeSimObj (so);
				break;
			}
		   StartCoroutine(checkWaitAction(success));
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
			//GameObject hand = getHand ();
			//if (currentHandSimObj != null) {
			//	currentHandSimObj.hasCollision = false;
			//}
			//hand.transform.localPosition = startingHandPosition;
			//hand.transform.localRotation = Quaternion.Euler (new Vector3 ());

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
            actionFinished(true);
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

			bool objectVisible = false;
			foreach (SimObj so in VisibleSimObjs(action)){
				objectVisible = true;
				Debug.Log(" got sim object: " + so.UniqueID);
				if (!so.IsReceptacle && (!IsOpenable(so) || so.Manipulation == SimObjManipType.Inventory))
				{
					if (inventory.Count == 0)
					{
						Debug.Log("trying to take item: " + so.UniqueID);
						SimUtil.TakeItem(so);
						addObjectInventory(so);
						success = true;
					}
					else
					{
						errorCode = ServerActionErrorCode.InventoryFull;
					}
					break;
				}
				else {
					errorCode = ServerActionErrorCode.ObjectNotPickupable;
				}
			}

			if (success) {
				errorCode = ServerActionErrorCode.Undefined;
			}
			else { 
				if (!objectVisible) {
					errorCode = ServerActionErrorCode.ObjectNotVisible;
				}
			}
			Debug.Log("error code: " + errorCode);

            StartCoroutine(checkWaitAction(success));
		}

		// empty target receptacle and put object into receptacle

		public void Replace(ServerAction response) 
        {
			bool success = false;

            SimObj[] simObjs = SceneManager.Current.ObjectsInScene.ToArray ();
			foreach (SimObj rso in simObjs) 
            {
				if (response.receptacleObjectId == rso.UniqueID) 
                {
					foreach (SimObj so in SimUtil.GetItemsFromReceptacle(rso.Receptacle)) 
                    {
						SimUtil.TakeItem (so);
					}
					foreach (SimObj so in simObjs) {
						if (so.UniqueID == response.objectId && SimUtil.AddItemToReceptaclePivot (so, rso.Receptacle.Pivots[response.pivot])) 
                        {
							success = true;

						}
					}
				}
			}
			actionFinished(success);

		}

		public void PutObject(ServerAction response) 
        {
			bool success = false;
			bool receptacleVisible = false;
			if (inventory.ContainsKey(response.objectId))
			{

				foreach (SimObj rso in VisibleSimObjs(response.forceVisible))
				{

					if (rso.IsReceptacle && (rso.UniqueID == response.receptacleObjectId || rso.Type == response.ReceptableSimObjType()))
					{
						receptacleVisible = true;
						SimObj so = removeObjectInventory(response.objectId);
						if (!IsOpenable(rso) || IsOpen(rso))
						{
							Transform emptyPivot = null;
							if (!SimUtil.GetFirstEmptyReceptaclePivot(rso.Receptacle, out emptyPivot))
							{
								errorCode = ServerActionErrorCode.ReceptacleFull;
							}
							else { 
							
								if (response.forceVisible)
								{
									SimUtil.AddItemToReceptaclePivot(so, emptyPivot);
									success = true;
								}
								else
								{
									emptyPivot = null;
									if (!SimUtil.GetFirstEmptyVisibleReceptaclePivot(rso.Receptacle, m_Camera, out emptyPivot))
									{
										errorCode = ServerActionErrorCode.ReceptaclePivotNotVisible;
									}
									else { 
										SimUtil.AddItemToReceptaclePivot(so, emptyPivot);
										success = true;
									}
								}
							}

						}
						else {
							errorCode = ServerActionErrorCode.ReceptacleNotOpen;
						}



						if (!success) { 
                            addObjectInventory(so);
						}

						break;
					}
				}
			}
			else {
				errorCode = ServerActionErrorCode.ObjectNotInInventory;
			}

			if (success)
			{
				errorCode = ServerActionErrorCode.Undefined;
			} else {
				if (!receptacleVisible && errorCode == ServerActionErrorCode.Undefined) {
					errorCode = ServerActionErrorCode.ReceptacleNotVisible;
				}
			}
            StartCoroutine(checkWaitAction(success));
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
				errorCode = ServerActionErrorCode.LookDownCantExceedMin;
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
				errorCode = ServerActionErrorCode.LookUpCantExceedMax;
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
