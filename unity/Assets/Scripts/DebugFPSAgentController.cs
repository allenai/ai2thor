// Copyright Allen Institute for Artificial Intelligence 2017

//Check Assets/Prefabs/DebugController for ReadMe on how to use this Debug Controller
using UnityEngine;

using Random = UnityEngine.Random;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]
    public class DebugFPSAgentController : MonoBehaviour
	{
		public float MaxDistance = 1f;

		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;
		[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
		[SerializeField] private float m_JumpSpeed;
		[SerializeField] private float m_StickToGroundForce;
		[SerializeField] private float m_GravityMultiplier;
		[SerializeField] private MouseLook m_MouseLook;
		[SerializeField] private bool m_UseFovKick;
		[SerializeField] private FOVKick m_FovKick = new FOVKick();
		[SerializeField] private bool m_UseHeadBob;
		[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
		[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
		[SerializeField] private float m_StepInterval;
		[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
		[SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
		[SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
	
        [SerializeField] private GameObject Target_Text = null;
        [SerializeField] private GameObject Debug_Canvas = null;
        [SerializeField] private bool isReceptacle = false;
        [SerializeField] private bool isPickup = false;

        [SerializeField] private string current_Object_In_Inventory = null;
        [SerializeField] private GameObject Inventory_Text = null;

		private Camera m_Camera;
		private bool m_Jump;
		private float m_YRotation;
		public bool rotateMouseLook;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;
		private CollisionFlags m_CollisionFlags;
		private bool m_PreviouslyGrounded;
		private Vector3 m_OriginalCameraPosition;
		private float m_StepCycle;
		private float m_NextStep;
		private bool m_Jumping;
		//private AudioSource m_AudioSource;

		SimObj[] currentVisibleObjects;

        protected Dictionary<SimObjType, Dictionary<string, int>> OPEN_CLOSE_STATES = new Dictionary<SimObjType, Dictionary<string, int>>{
            {SimObjType.Microwave, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
            {SimObjType.Laptop, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
            {SimObjType.Book, new Dictionary<string, int>{{"open", 1}, {"close", 2}}},
            {SimObjType.Toilet, new Dictionary<string, int>{{"open", 2}, {"close", 3}}},
            {SimObjType.Sink, new Dictionary<string, int>{{"open", 2}, {"close", 1}}}
        };

        //inventory to store picked up objects
        private Dictionary<string, SimObj> inventory = new Dictionary<string, SimObj>();

        //what things are openable or not, used to determine if pickupable
        protected SimObjType[] OpenableTypes = new SimObjType[] { SimObjType.Fridge, SimObjType.Cabinet, SimObjType.Microwave, SimObjType.LightSwitch, SimObjType.Blinds, SimObjType.Book, SimObjType.Toilet };
        protected SimObjType[] ImmobileTypes = new SimObjType[] { SimObjType.Chair, SimObjType.Toaster, SimObjType.CoffeeMachine, SimObjType.Television, SimObjType.StoveKnob };


		#if UNITY_EDITOR
		//used to show what's currently visible
		void OnGUI () 
        {
			if (currentVisibleObjects != null) 
            {
				if (currentVisibleObjects.Length > 10) 
                {
					int horzIndex = -1;
					GUILayout.BeginHorizontal ();
					foreach (SimObj o in currentVisibleObjects) 
                    {
						horzIndex++;
						if (horzIndex >= 3) 
                        {
							GUILayout.EndHorizontal ();
							GUILayout.BeginHorizontal ();
							horzIndex = 0;
						}
						GUILayout.Button (o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth (200f));
					}

					GUILayout.EndHorizontal ();
				}

                else 
                {
					Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);

                    int position_number = 0;
					foreach (SimObj o in currentVisibleObjects) 
                    {
						string suffix = "";
						Bounds bounds = new Bounds (o.gameObject.transform.position, new Vector3 (0.05f, 0.05f, 0.05f));
						if (GeometryUtility.TestPlanesAABB (planes, bounds)) 
                        {
                            position_number += 1;

                            if (o.GetComponent<SimObj>().Manipulation == SimObjManipType.Inventory)
                                suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

                            else
                                suffix += " VISIBLE";
						}
							
							
						GUILayout.Button (o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth (100f));
					}
				}
			}
		}
		#endif


        //is object pickupable?
        public bool IsPickupable(SimObj so)
        {
            return !IsOpenable(so) && !so.IsReceptacle && !(Array.IndexOf(ImmobileTypes, so.Type) >= 0);
        }

        //is object openable?
        public bool IsOpenable(SimObj so)
        {

           return Array.IndexOf(OpenableTypes, so.Type) >= 0 && so.IsAnimated;
        }


		// Use this for initialization
		private void Start()
		{
			m_CharacterController = GetComponent<CharacterController>();
			m_Camera = Camera.main;
			m_OriginalCameraPosition = m_Camera.transform.localPosition;
			m_FovKick.Setup(m_Camera);
			m_HeadBob.Setup(m_Camera, m_StepInterval);
			m_StepCycle = 0f;
			m_NextStep = m_StepCycle/2f;
			m_Jumping = false;
			//m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);

            //grab text object on canvas to update with what is currently targeted by reticle
           Target_Text = GameObject.Find("DebugCanvas/TargetText");;
           Debug_Canvas = GameObject.Find("DebugCanvas");
           Inventory_Text = GameObject.Find("DebugCanvas/InventoryText");


            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
                Debug_Canvas.SetActive(true);
                Target_Text.SetActive(true);


                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            
		}

        //add object to the inventory
        public void addObjectInventory(SimObj simObj)
        {
            inventory[simObj.UniqueID] = simObj;
            current_Object_In_Inventory = simObj.UniqueID;
        }


        //remove current object in inventory
        public SimObj removeObjectInventory(string objectId)
        {
            SimObj so = inventory[objectId];
            inventory.Remove(objectId);

            current_Object_In_Inventory = null;
            Inventory_Text.GetComponent<Text>().text = "In Inventory: Nothing!";
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


        //take a pickupable item, place in inventory if inventory is not full
        protected void TakeItem(SimObj item)
        {

           // print(item.Manipulation);

            //check so we only have one item in the inventory at a time
            if (inventory.Count == 0)
            {
                //make item visible to raycasts
                //unparent in case it's in a receptacle
                item.VisibleToRaycasts = true;
                item.transform.parent = null;
                //disable the item entirely
                item.gameObject.SetActive(false);
                //set the position to 'up' so it's rotated correctly when it comes back
                item.transform.up = Vector3.up;
                //reset the scale (to prevent floating point weirdness)
                item.ResetScale();

                //now add to inventory
                addObjectInventory(item);
                Inventory_Text.GetComponent<Text>().text = "In Inventory: " + item.UniqueID + " | Press 'Space' to put in Receptacle";
              
            }

            else
                print("inventory full!");

        }

		protected bool openSimObj(SimObj so) 
        {
            bool inrange = false;
            //check if the object we are trying to open is in visible range
            foreach(SimObj o in currentVisibleObjects) 
            {
                //check if the ID of the object we are looking at is in array of visible objects
                if(so.UniqueID == o.UniqueID)
                {
                    inrange = true;
                }
            }

            bool res = false;

            if(inrange)
            {
                
                if (OPEN_CLOSE_STATES.ContainsKey(so.Type))
                {
                    res = updateAnimState(so.Animator, OPEN_CLOSE_STATES[so.Type]["open"]);

                }

                else if (so.IsAnimated)
                {
                    res = updateAnimState(so.Animator, true);
                }

               // return res;
            }

            if(!inrange)
            {
                
                Debug.Log("This SimObj can't be opened!");
              //  return res;

            }

            return res;

   
		}

        protected bool closeSimObj(SimObj so)
        {
      
            bool inrange = false;
            //check if the object we are trying to open is in visible range
            foreach (SimObj o in currentVisibleObjects)
            {
                //check if the ID of the object we are looking at is in array of visible objects
                if (so.UniqueID == o.UniqueID)
                {
                    inrange = true;
                }
            }

            bool res = false;
            if (inrange)
            {
                
                if (OPEN_CLOSE_STATES.ContainsKey(so.Type))
                {
                    res = updateAnimState(so.Animator, OPEN_CLOSE_STATES[so.Type]["close"]);
                }
                else if (so.IsAnimated)
                {
                    res = updateAnimState(so.Animator, false);
                }
            }

            if (!inrange)
            {
                Debug.Log("Target out of range!");
            }

            return res;
        }

        ///overloaded updateAnimState
        private bool updateAnimState(Animator anim, int value)
        {
            AnimatorControllerParameter param = anim.parameters[0];

            if (anim.GetInteger(param.name) == value)
            {
                return false;
            }
            else
            {
                anim.SetInteger(param.name, value);
                return true;
            }
        }

        private bool updateAnimState(Animator anim, bool value)
        {
            AnimatorControllerParameter param = anim.parameters[0];

            if (anim.GetBool(param.name) == value)
            {
                return false;
            }
            else
            {
                anim.SetBool(param.name, value);
                return true;
            }
        }

		public bool thingdone;
		public bool captureScreenshot;
		public int screenshotCounter;
		public bool pickupObject;

        //called on update, constantly shoots rays out to identify SimObjects that can 
        //either be picked up or opened

        //pick up item in specific position in list of visible objects
        //this doesn't work great when there are so many visible objects that the list of visible objects overflows, use TryAndPickUp_All for that
        protected void TryAndPickUp(int i)
        {
            if (currentVisibleObjects.Length != 0 && currentVisibleObjects.Length > i)
            {
                //grab only pickup objects with the inventory manip type
                if (currentVisibleObjects[i].GetComponent<SimObj>().Manipulation == SimObjManipType.Inventory)//(IsPickupable(currentVisibleObjects[i]))//
                    TakeItem(currentVisibleObjects[i]);

                else
                    Debug.Log("can't pick " + currentVisibleObjects[i].name + " up!");
            }
        }

        //loops through current array of visible objects and picks up the first Convertable component found.
        //useful for when there are so many visible objects in array, it overflows and the alphanumeric pickup key inputs become wonky
        protected void TryAndPickUp_All()
        {
            for (int i = 0; i < currentVisibleObjects.Length; i++)
            {
                if (currentVisibleObjects[i].GetComponent<SimObj>().Manipulation == SimObjManipType.Inventory)
                    //(IsPickupable(currentVisibleObjects[i]))//
                    TakeItem(currentVisibleObjects[i]);

                else
                    Debug.Log("can't pick " + currentVisibleObjects[i].name + " up!");
            }
        }

        private void RaycastTarget()
        {
           // raycast from the center of the screen
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(x, y));

            //Casts raycast through all objects under reticle, sorts through them to see if
            //they are sim objects or not, if they are either a receptacle or a pickup, show the name

            RaycastHit[] hits;

            List<string> targetTextList = new List<string>();

            hits = Physics.RaycastAll(m_Camera.transform.position, m_Camera.transform.forward, 10f);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit target = hits[i];

                if(target.transform.GetComponent<SimObj>())//((target.transform.GetComponent<Receptacle>() && target.transform.GetComponent<Receptacle>().isActiveAndEnabled) || (target.transform.GetComponent<Convertable>() && target.transform.GetComponent<Convertable>().isActiveAndEnabled))
                {
                    targetTextList.Add(target.transform.name);
                }

                else
                {
                    targetTextList.Clear();
                }

            }

            string toDisplay = " ";

            foreach(string txt in targetTextList)
            {
                toDisplay = toDisplay.ToString() + txt.ToString() + "\n";
            }

            Target_Text.GetComponent<Text>().text = toDisplay;





            /////////////////RaycastHit implementation

            RaycastHit hit = new RaycastHit();
           
            //int layer = 1 << LayerMask.NameToLayer("Default");

            if (Physics.Raycast(ray, out hit))
            {
                Debug.DrawLine(m_Camera.transform.position, hit.point, Color.red);
                //check for SimObjects that we are looking at


             
                if(hit.transform.tag == "SimObj")
                {
                    //update text to show what we are looking at
                   // Target_Text.GetComponent<Text>().text = hit.transform.name;

                    //All openable items have a Receptacle component
                    if(hit.transform.GetComponent<Receptacle>() && hit.transform.GetComponent<Receptacle>() != null)
                    {
                        //print("this is a receptacle");
                        isReceptacle = true;
                    }

                    else
                    {
                        isReceptacle = false;
                    }

                    //all pickup-able items are of type inventory
                    if(hit.transform.GetComponent<SimObj>().Manipulation == SimObjManipType.Inventory && hit.transform.GetComponent<SimObj>() != null)//(hit.transform.GetComponent<Convertable>())
                    {
                       // print("able to pick up");
                        isPickup = true;
                    }

                    else
                    {
                        isPickup = false;    
                    }
                }

                else
                {
                    //if no sim objects are under the reticle, show no text
                    Target_Text.GetComponent<Text>().text = " ";
                    isReceptacle = false;
                    isPickup = false;
                }

            }


        }

        private void OpenReceptacle_ray()
        {
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(x, y));
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {

                openSimObj(hit.transform.GetComponent<SimObj>());
            }
        }

        private void CloseReceptacle_ray()
        {
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(x, y));
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {
                closeSimObj(hit.transform.GetComponent<SimObj>());
            }
        }

        //shoots ray from center of screen to pick up an object with
        private void TakeObject_ray()
        {
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(x, y));
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {
                TakeItem(hit.transform.GetComponent<SimObj>());
            }
        }

        private void PutObject_ray(string item)
        {

            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(x, y));
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {

                //check if the receptacle is in visible range
                bool inrange = false;
                //check if the object we are trying to put something in is in visible range
                foreach (SimObj o in currentVisibleObjects)
                {
                    //check if the ID of the object we are looking at is in array of visible objects
                    if (hit.transform.GetComponent<SimObj>().UniqueID == o.UniqueID)
                    {
                        inrange = true;
                    }
                }

                if (inrange)
                {
                    //if (SimUtil.AddItemToVisibleReceptacle(inventory[current_Object_In_Inventory], hit.transform.GetComponent<Receptacle>(), gameObject.GetComponentInChildren<Camera>()) == false)


                    if (SimUtil.AddItemToReceptacle(inventory[current_Object_In_Inventory], hit.transform.GetComponent<Receptacle>()) == false)
                        Debug.Log("There's no space for that!");

                    else
                        removeObjectInventory(current_Object_In_Inventory);
                }

                if(!inrange)
                {
                    Debug.Log("It's too far away to put stuff in");
                }




            }
        }

		private void Update()	
        {
            RaycastTarget();

            //////MOUSE AND KEYBAORD INPUT///////////////////////////////////////////////////
            //on left mouse click
            if(Input.GetMouseButtonDown(0))
            {
                //check if what we are looking at is a receptacle and can be opened
                if(isReceptacle && !isPickup)
                {
                    OpenReceptacle_ray();
                    //open receptacle here
                }


                if(isPickup && !isReceptacle)
                {
                    //try to pick up the thing?
                    Debug.Log("You can't open that!");
                }

                //this is to turn on items like the Sink that have both a Receptacle and a Convertable component
                if(isPickup && isReceptacle)
                {
                    Debug.Log("You can't open that!");
                }


            }

            //on right mouse click
            if(Input.GetMouseButtonDown(1))
            {

                //are we looking at a receptacle and there is something we have picked up?
                //then place it in the receptacle
                /* if (isReceptacle && !String.IsNullOrEmpty(current_Object_In_Inventory))
                 {
                     //print("place object");
                     PutObject_ray(current_Object_In_Inventory);
                 }

                 else */
                if (isReceptacle)
                {
                    //print("close recept");
                    CloseReceptacle_ray();
                }

                else
                    Debug.Log("You can't close that!");

            }

            //try to pick up an item from the currently visible objects stored in SimObj[] currentVisibleObjects;
            if(Input.GetKeyDown(KeyCode.Alpha1))
            {
                TryAndPickUp(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TryAndPickUp(1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TryAndPickUp(2);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                TryAndPickUp(3);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                TryAndPickUp(4);
            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                TryAndPickUp(5);
            }

            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                TryAndPickUp(6);
            }

            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                TryAndPickUp(7);
            }

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                TryAndPickUp(8);
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                TryAndPickUp(9);
            }

            if(Input.GetKey(KeyCode.E))
            {
                TryAndPickUp_All();
            }

            if(Input.GetKeyDown(KeyCode.Space))
            {

                //is there something in the inventory? if so, place it in the receptacle we are looking at
                if (isReceptacle && !String.IsNullOrEmpty(current_Object_In_Inventory))
                {
                    PutObject_ray(current_Object_In_Inventory);
                }

                else if(!String.IsNullOrEmpty(current_Object_In_Inventory))
                    Debug.Log("You can't put that there!");

                else if(isReceptacle && String.IsNullOrEmpty(current_Object_In_Inventory))
                {
                    Debug.Log("Nothing in your Inventory!");
                }
            }

            ///////////////////////////////////////////////////////////////////////////
			RotateView();


			if (captureScreenshot) {
				screenshotCounter++;
				StartCoroutine (EmitFrame ());
				captureScreenshot = false;
			}

			// the jump state needs to read here to make sure it is not missed
			if (!m_Jump)
			{
				//m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
			}

			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
			{
				StartCoroutine(m_JumpBob.DoBobCycle());
				m_MoveDir.y = 0f;
				m_Jumping = false;
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
			{
				m_MoveDir.y = 0f;
			}

            //populate array of what objects are visible
            currentVisibleObjects = SimUtil.GetAllVisibleSimObjs(m_Camera, MaxDistance);


			m_PreviouslyGrounded = m_CharacterController.isGrounded;
		}


		protected byte[] captureScreen() 
        {
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

		private IEnumerator EmitFrame() 
        {
			yield return new WaitForEndOfFrame ();
			File.WriteAllBytes ("/Users/erick/Desktop/screenshots/screenshot-" + screenshotCounter.ToString () + ".png", captureScreen ());
		}

		private void FixedUpdate()
		{
			float speed;
			GetInput(out speed);
			// always move along the camera forward as it is the direction that it being aimed at
			Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

			// get a normal for the surface that is being touched to move along it
			RaycastHit hitInfo;
			Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
				m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			m_MoveDir.x = desiredMove.x*speed;
			m_MoveDir.z = desiredMove.z*speed;


			if (m_CharacterController.isGrounded)
			{
				m_MoveDir.y = -m_StickToGroundForce;

				if (m_Jump)
				{
					m_MoveDir.y = m_JumpSpeed;
					//PlayJumpSound();
					m_Jump = false;
					m_Jumping = true;
				}
			}

			else
			{
				m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
			}
			m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

			ProgressStepCycle(speed);
			UpdateCameraPosition(speed);

		}




		private void ProgressStepCycle(float speed)
		{
			if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
			{
				m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
					Time.fixedDeltaTime;
			}

			if (!(m_StepCycle > m_NextStep))
			{
				return;
			}

			m_NextStep = m_StepCycle + m_StepInterval;

		}



		private void UpdateCameraPosition(float speed)
		{
			Vector3 newCameraPosition;
			if (!m_UseHeadBob)
			{
				return;
			}
			if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
			{
				m_Camera.transform.localPosition =
					m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
						(speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
			}
			else
			{
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
			}
			m_Camera.transform.localPosition = newCameraPosition;
		}


		private void GetInput(out float speed)
		{
			// Read input
			float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
			float vertical = CrossPlatformInputManager.GetAxis("Vertical");

			bool waswalking = m_IsWalking;

			#if !MOBILE_INPUT
			// On standalone builds, walk/run speed is modified by a key press.
			// keep track of whether or not the character is walking or running
			m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
			#endif
			// set the desired speed to be walking or running
			speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
			m_Input = new Vector2(horizontal, vertical);

			// normalize input if it exceeds 1 in combined length:
			if (m_Input.sqrMagnitude > 1)
			{
				m_Input.Normalize();
			}

			// handle speed change to give an fov kick
			// only if the player is going to a run, is running and the fovkick is to be used
			if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
			{
				StopAllCoroutines();
				StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
			}
		}


		private void RotateView()
		{
			if (!captureScreenshot && rotateMouseLook) {
				m_MouseLook.LookRotation (transform, m_Camera.transform);
			}

		}


		private void OnControllerColliderHit(ControllerColliderHit hit)
		{
			Rigidbody body = hit.collider.attachedRigidbody;
			//dont move the rigidbody if the character is on top of it
			if (m_CollisionFlags == CollisionFlags.Below)
			{
				return;
			}

			if (body == null || body.isKinematic || m_CharacterController == null)
			{
				return;
			}
			body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
		}
	}
}

