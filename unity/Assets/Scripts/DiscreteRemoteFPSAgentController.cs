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
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]

	public class DiscreteRemoteFPSAgentController : BaseFPSAgentController
	{
		protected Vector3 startingHandPosition;
		private Dictionary<int, Material[]> currentMaskMaterials;
		private SimObj currentMaskObj;
		private SimObj currentHandSimObj;
		//private static float gridSize = 0.25f;

              
		private Dictionary<string, SimObj> inventory = new Dictionary<string, SimObj>();

        //protected DebugFPSAgentController DebugComponent = null;
        //protected Canvas DebugCanvas = null;

		// Initialize parameters from environment variables
		protected override void Awake() 
		{
			// load config parameters from the server side

			base.Awake ();

            //DebugCanvas = GameObject.Find("DebugCanvas").GetComponent<Canvas>();
            //DebugComponent = this.GetComponent<DebugFPSAgentController>();

            //DebugCanvas.enabled = false;
            //DebugComponent.enabled = false;
		}


		protected override void Start() 
		{

			base.Start ();
			this.actionComplete = true;
			// always zero out the rotation

			transform.rotation = Quaternion.Euler (new Vector3 (0.0f, 0.0f, 0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (0.0f, 0.0f, 0.0f);
			//startingHandPosition = getHand ().transform.localPosition;
			snapToGrid ();

            //On start, activate gravity
			Vector3 movement = Vector3.zero;
            movement.y = Physics.gravity.y * m_GravityMultiplier;
            m_CharacterController.Move(movement);
        }


		public override MetadataWrapper generateMetadataWrapper() 
        {


			MetadataWrapper metaMessage = base.generateMetadataWrapper ();
			metaMessage.lastAction = lastAction;
			metaMessage.lastActionSuccess = lastActionSuccess;
			metaMessage.errorMessage = errorMessage;

			if (errorCode != ServerActionErrorCode.Undefined) 
			{
				metaMessage.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
			}

			List<InventoryObject> ios = new List<InventoryObject>();

			foreach (string objectId in inventory.Keys) 
	        {
				SimObj so = inventory [objectId];
				InventoryObject io = new InventoryObject();
				io.objectId = so.UniqueID;
				io.objectType = Enum.GetName (typeof(SimObjType), so.Type);
				ios.Add(io);
	
			}

			metaMessage.inventoryObjects = ios.ToArray();

			return metaMessage;
		}




		// Check if agent is moving
		private bool IsMoving() 
		{
			bool moving = m_Input.sqrMagnitude > Mathf.Epsilon;
			bool turningVertical = Mathf.Abs (m_XRotation) > Mathf.Epsilon;
			bool turningHorizontal = Mathf.Abs (m_ZRotation) > Mathf.Epsilon;
			return moving || turningHorizontal || turningVertical;
		}



		virtual protected IEnumerator checkWaitAction(bool success) 
		{
			yield return null;
			actionFinished(success);
		}



		private string formatPos(Vector3 pos) 
		{
			return "x=" + pos.x.ToString("F3") + " y=" + pos.y.ToString("F3") + " z=" + pos.z.ToString("F3");
		}


		virtual protected IEnumerator checkOpenAction(SimObj so) {
			yield return null;

			bool result = false;
			//Debug.Log ("checkOpenAction");
			for (int i = 0; i < actionDuration; i++) {
				//Debug.Log ("checkOpenAction action duration " + i);
				//Debug.Log ("Action duration " + i);
				Vector3 currentPosition = this.transform.position;
				//Debug.Log ("collided in open");

				currentPosition = this.transform.position;
				for (int j = 0; j < actionDuration; j++) {
					yield return null;
				}
				Vector3 snapDiff = currentPosition - this.transform.position;
				snapDiff.y = Mathf.Min (Math.Abs(snapDiff.y), 0.05f);
//				Debug.Log ("currentY " + currentPosition.y);
//				Debug.Log ("positionY " + this.transform.position.y);
//				Debug.Log ("snapDiff " + snapDiff.y);
				if (snapDiff.magnitude >= 0.005f) {
					result = false;
					break;
				} else {
					result = true;
				}
			}


			if (!result) {
				Debug.Log ("check open failed");
				closeSimObj (so);
				transform.position = lastPosition;
				for (int j = 0; j < actionDuration; j++) {
					Debug.Log ("open yield return null");
					yield return null;
				}
			}

			actionFinished (result);
		}


		private IEnumerator checkDropHandObjectAction() 
        {
			yield return null; // wait for one frame to pass


			bool result = false;
			for (int i = 0; i < 30; i++) 
			{
				Debug.Log (currentHandSimObj.transform.position.ToString());
				Rigidbody rb = currentHandSimObj.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;

				if (Math.Abs (rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude) < 0.00001) 
				{
					Debug.Log ("object is now at rest");
					result = true;
					break;
				} 

				else 
				{
					Debug.Log ("object is still moving");
					yield return null;
				}

			}

			actionFinished (result);
		}

		private IEnumerator checkMoveHandAction() 
		{

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
              //if ( Input.GetKeyDown(KeyCode.BackQuote))
              //{
              //    DebugCanvas.enabled = true;
              //    DebugComponent.enabled = true;
              //}
            }
		#endif

		public void RandomInitialize(ServerAction response) {
			bool success = true;
			this.excludeObjectIds = response.excludeObjectIds;

			Dictionary<SimObjType, HashSet<SimObjType>> receptacleObjects = new Dictionary<SimObjType, HashSet<SimObjType>> ();
			HashSet<SimObjType> pickupable = new HashSet<SimObjType> ();
			foreach (ReceptacleObjectList rol in response.receptacleObjects) {
				HashSet<SimObjType> objectTypes = new HashSet<SimObjType> ();
				SimObjType receptacleType = (SimObjType)Enum.Parse (typeof(SimObjType), rol.receptacleObjectType);
				foreach (string itemObjectType in rol.itemObjectTypes) {
					objectTypes.Add ((SimObjType)Enum.Parse (typeof(SimObjType), itemObjectType));
					pickupable.Add ((SimObjType)Enum.Parse (typeof(SimObjType), itemObjectType));
				}
				receptacleObjects.Add (receptacleType, objectTypes);
			}
			Debug.Log ("random seed:Z " + response.randomSeed);
			System.Random rnd = new System.Random (response.randomSeed);


			SimObj[] simObjects = GameObject.FindObjectsOfType (typeof(SimObj)) as SimObj[];
			// Sorting to ensure that our randomization is deterministic when using a seed
			// without sorting, there is no guarantee how the objects will get returned from from FindObjectsOfType
			// so the shuffle becomes non-deterministic
			Array.Sort (simObjects, delegate(SimObj a, SimObj b) {
				return a.UniqueID.CompareTo (b.UniqueID);
			});

			int pickupableCount = 0;
			for (int i = 0; i < simObjects.Length; i++) {
				SimObj so = simObjects [i];
				if (so.IsPickupable) {
					pickupableCount++;
					SimUtil.TakeItem (so);

				}


				if (so.IsOpenable && response.randomizeOpen) {
					if (rnd.NextDouble () < 0.5) {
						openSimObj (so);	
					} else {
						closeSimObj (so);
					}
				}
			}

			//shuffle objects
			rnd = new System.Random (response.randomSeed);

			for (int i = 0; i < simObjects.Length; i++) {
				SimObj so = simObjects [i];
				int randomIndex = rnd.Next (i, simObjects.Length);
				simObjects [i] = simObjects [randomIndex];
				simObjects [randomIndex] = so;
			}



			rnd = new System.Random (response.randomSeed);
			List<SimObj> simObjectsFiltered = new List<SimObj> ();
			for (int i = 0; i < simObjects.Length; i++) {
				SimObj so = simObjects [i];

				if (so.IsPickupable && pickupable.Contains (so.Type)) {
					double val = rnd.NextDouble ();
	

					if (val > response.removeProb) {
						// Keep the item
						int numRepeats = 1;
						if (response.maxNumRepeats > 1) {
							numRepeats = rnd.Next(1, response.maxNumRepeats);
						}
						for (int j = 0; j < numRepeats; j++) {
							// Add a copy of the item.
							SimObj copy = Instantiate(so);
							copy.name += "" + j;
							copy.UniqueID = so.UniqueID + "_copy_" + j;
							simObjectsFiltered.Add (copy);
						}
					} else {
						pickupableCount--;
					}
				} else {
					simObjectsFiltered.Add (simObjects [i]);
				}
			}


			simObjects = simObjectsFiltered.ToArray ();
			int randomTries = 0;
			HashSet<SimObjType> seenObjTypes = new HashSet<SimObjType> ();
			rnd = new System.Random (response.randomSeed);
			while (pickupableCount > 0) {
				if (randomTries > 5) {
					Debug.Log ("Pickupable count still at, but couldn't place all objects: " + pickupableCount);
					success = false;
					break;
				}
				randomTries++;

				int[] randomOrder = new int[simObjects.Length];
				for (int rr = simObjects.Length - 1; rr >= 0; rr--) {
					int randomLoc = simObjects.Length - 1 - rnd.Next(0, (simObjects.Length - rr));
					randomOrder [rr] = randomOrder [randomLoc];
					randomOrder [randomLoc] = rr;
				}
				for (int ss = 0; ss < simObjects.Length; ss++) {
					int j = randomOrder [ss];
					foreach (SimObj so in simObjects) {
						if (so.IsReceptacle && !excludeObject (so)) {
							if (response.excludeReceptacleObjectPairs != null &&
								Array.Exists (response.excludeReceptacleObjectPairs, e => e.objectId == simObjects [j].UniqueID && e.receptacleObjectId == so.UniqueID)) {
								//Debug.Log ("skipping object id receptacle id pair, " + simObjects [j].UniqueID + " " + so.UniqueID);
								continue;
							}

							if (simObjects [j].IsPickupable &&
								receptacleObjects.ContainsKey (so.Type) &&
								receptacleObjects [so.Type].Contains (simObjects [j].Type) &&
								(!response.uniquePickupableObjectTypes || !seenObjTypes.Contains (simObjects [j].Type)) &&
								SimUtil.AddItemToReceptacle (simObjects [j], so.Receptacle)) {
								//Debug.Log ("Put " + simObjects [j].Type + " " + simObjects[j].name + " in " + so.Type);
								seenObjTypes.Add (simObjects [j].Type);
								pickupableCount--;
								break;
							}

						}
					}
				}
			}

			if (response.randomizeObjectAppearance) {
				// Use a random texture for each object individually.
				rnd = new System.Random (response.randomSeed);
				for (int i = 0; i < simObjects.Length; i++) {
					SimObj so = simObjects [i];
					if (so.gameObject.activeSelf) {
						Randomizer randomizer = (so.gameObject.GetComponentInChildren<Randomizer> () as Randomizer);
						if (randomizer != null) {
							randomizer.Randomize (rnd.Next (0, 2147483647));
						}
					}
				}
			}


			if (imageSynthesis != null) {
				imageSynthesis.OnSceneChange ();
			}

			actionFinished(success);
		}







			
		public void MaskObject(ServerAction action) 
		{
			unmaskCurrent ();
			currentMaskMaterials = new Dictionary<int, Material[]> ();
			bool success = false;

			foreach (SimObj so in VisibleSimObjs(action)) 
			{
				currentMaskObj = so;

				foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) 
				{
					currentMaskMaterials [r.GetInstanceID ()] = r.materials;
					Material material = new Material(Shader.Find("Unlit/Color"));
					material.color = Color.magenta;
					Material[] newMaterials = new Material[]{ material };
					r.materials = newMaterials;
				}

				success = true;
			}

			actionFinished (success);
		}

		private void unmaskCurrent() 
		{
			if (currentMaskMaterials != null) 
			{
				foreach (SimObj so in VisibleSimObjs(true)) 
				{
					if (so.UniqueID == currentMaskObj.UniqueID) 
					{
						foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) 
						{
							if (currentMaskMaterials.ContainsKey(r.GetInstanceID())) 
							{
								r.materials = currentMaskMaterials[r.GetInstanceID()];
							}
						}

					}
				}
			}
			currentMaskMaterials = null;
		}

		public void Unmask(ServerAction action) 
		{
			unmaskCurrent ();
			actionFinished (true);
		}


      
		public void addObjectInventory(SimObj simObj) 
		{
			inventory [simObj.UniqueID] = simObj;
		}

		public SimObj removeObjectInventory(string objectId) 
		{
			SimObj so = inventory [objectId];
			inventory.Remove (objectId);
			return so;
		}

		public bool haveTypeInventory(SimObjType objectType) 
		{
			foreach (SimObj so in inventory.Values) 
			{
				if (so.Type == objectType) 
				{
					return true;
				}
			}
			return false;
		}
      
		public void OpenObject(ServerAction action) 
        {
			bool success = false;
			SimObj openedSimObj = null;
			foreach (SimObj so in VisibleSimObjs(action)) 
            {

				success = openSimObj(so);
				openedSimObj = so;
				break;
			}

			if (success) {
				StartCoroutine (checkOpenAction (openedSimObj));
			} else {
				StartCoroutine(checkWaitAction(success));
			}
		}

		public void CloseObject(ServerAction action) 
		{
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action)) 
			{
				success = closeSimObj (so);
				break;
			}
		   StartCoroutine(checkWaitAction(success));
		}

		override public SimpleSimObj[] VisibleSimObjs()
		{
			return SimUtil.GetAllVisibleSimObjs(m_Camera, maxVisibleDistance) as SimpleSimObj[];
		}


		public SimpleSimObj[] VisibleSimObjs(bool forceVisible)
		{
			if (forceVisible)
			{
				return GameObject.FindObjectsOfType(typeof(SimObj)) as SimpleSimObj[];
			}
			else
			{
				return VisibleSimObjs();

			}
		}


		public SimpleSimObj[] VisibleSimObjs(ServerAction action) 
		{
			List<SimpleSimObj> simObjs = new List<SimpleSimObj> ();

			foreach (SimpleSimObj so in VisibleSimObjs (action.forceVisible)) 
			{

				if (!string.IsNullOrEmpty(action.objectId) && action.objectId != so.UniqueID) 
				{
					continue;
				}

				if (!string.IsNullOrEmpty(action.objectType) && action.GetSimObjType() != so.ObjType) 
				{
					continue;
				}

				simObjs.Add (so);
			}	


			return simObjs.ToArray ();

		}

		public GameObject getHand() 
		{
			return GameObject.Find ("FirstPersonHand");
		}

		public void DropHandObject(ServerAction action) 
		{
			if (currentHandSimObj != null) 
			{


				Rigidbody rb = currentHandSimObj.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
				rb.constraints = RigidbodyConstraints.None;
				rb.useGravity = true;

				currentHandSimObj.transform.parent = null;
				removeObjectInventory (currentHandSimObj.UniqueID);
				StartCoroutine (checkDropHandObjectAction ());
				//currentHandSimObj = null;
				//resetHand ();
			} 
			else 
			{
				lastActionSuccess = false;
			}
         
		}

		private void resetHand() 
		{
			//GameObject hand = getHand ();
			//if (currentHandSimObj != null) {
			//	currentHandSimObj.hasCollision = false;
			//}
			//hand.transform.localPosition = startingHandPosition;
			//hand.transform.localRotation = Quaternion.Euler (new Vector3 ());

		}

		public void ResetHand(ServerAction action) 
		{
			resetHand ();
		}

		private void moveHand(Vector3 target) 
		{

			GameObject hand = getHand ();
			currentHandSimObj.hasCollision = false;
			hand.transform.position = hand.transform.position + target;
			StartCoroutine (checkMoveHandAction());

		}

		public void MoveHandForward(ServerAction action) 
		{
			moveHand (m_CharacterController.transform.forward * action.moveMagnitude);
		}


		public void RotateHand(ServerAction action) 
		{
			getHand().transform.localRotation = Quaternion.Euler(new Vector3(action.x, action.y, action.z));
            actionFinished(true);
		}

		public void MoveHandLeft(ServerAction action) 
		{
			moveHand (m_CharacterController.transform.right * action.moveMagnitude * -1);
		}

		public void MoveHandRight(ServerAction action) 
		{
			moveHand (m_CharacterController.transform.right * action.moveMagnitude);
		}



		public void PickupHandObject(ServerAction action) 
		{
			GameObject hand = getHand ();
			bool success = false;
			foreach (SimObj so in VisibleSimObjs(action) as SimObj[]) 
			{
				// XXX CHECK IF OPEN
				if (!so.IsReceptacle && !so.IsOpenable) 
				{
					if (inventory.Count == 0) 
					{
						
						addObjectInventory(so);
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
			foreach (SimObj so in VisibleSimObjs(action))
			{
				objectVisible = true;
				Debug.Log(" got sim object: " + so.UniqueID);
				if (!so.IsReceptacle && (!so.IsOpenable || so.Manipulation == SimObjManipType.Inventory))
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
				else 
				{
					errorCode = ServerActionErrorCode.ObjectNotPickupable;
				}
			}

			if (success) 
			{
				errorCode = ServerActionErrorCode.Undefined;
			}
			else 
			{ 
				if (!objectVisible) 
				{
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
					foreach (SimObj so in simObjs) 
					{
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
						if (!rso.IsOpenable || rso.IsOpen)
						{
							Transform emptyPivot = null;
							if (!SimUtil.GetFirstEmptyReceptaclePivot(rso.Receptacle, out emptyPivot))
							{
								errorCode = ServerActionErrorCode.ReceptacleFull;
							}
							else 
							{ 
							
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
									else 
									{ 
										SimUtil.AddItemToReceptaclePivot(so, emptyPivot);
										success = true;
									}
								}
							}                     
						}
						else 
						{
							errorCode = ServerActionErrorCode.ReceptacleNotOpen;
						}



						if (!success) 
						{ 
                            addObjectInventory(so);
						}

						break;
					}
				}
			}
			else 
			{
				errorCode = ServerActionErrorCode.ObjectNotInInventory;
			}

			if (success)
			{
				errorCode = ServerActionErrorCode.Undefined;
			} else 
			{
				if (!receptacleVisible && errorCode == ServerActionErrorCode.Undefined) 
				{
					errorCode = ServerActionErrorCode.ReceptacleNotVisible;
				}
			}
            StartCoroutine(checkWaitAction(success));
		}


		public void TeleportObject(ServerAction response) 
		{

			foreach (SimObj so in VisibleSimObjs(true)) 
			{
				if (so.UniqueID == response.objectId) {
					so.transform.position = new Vector3 (response.x, response.y, response.z);
				}
				
			}
			actionFinished(true);
		}


		public void TeleportFull(ServerAction response) 
		{

			targetTeleport = new Vector3 (response.x, response.y, response.z);
			m_CharacterController.transform.position = targetTeleport;
			transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation.y,0.0f));
			m_Camera.transform.localEulerAngles = new Vector3 (response.horizon, 0.0f, 0.0f);

			Vector3 m = new Vector3 ();
			m.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move (m);

			StartCoroutine (checkTeleportAction ());
		}

		public void Teleport(ServerAction response) 
		{
			targetTeleport = new Vector3 (response.x, response.y, response.z);
			m_CharacterController.transform.position = targetTeleport;
			if (response.rotateOnTeleport) 
			{
				transform.rotation = Quaternion.Euler(new Vector3(0.0f,response.rotation.y,0.0f));
			}

			Vector3 m = new Vector3 ();
			m.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move (m);

			StartCoroutine (checkTeleportAction ());
		}


		public void Reset(ServerAction response) 
		{
			if (string.IsNullOrEmpty(response.sceneName))
			{
				UnityEngine.SceneManagement.SceneManager.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name);
			} 

			else {
				UnityEngine.SceneManagement.SceneManager.LoadScene (response.sceneName);
			}
		}      
	}


}
