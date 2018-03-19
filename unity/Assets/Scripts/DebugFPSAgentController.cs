// Copyright Allen Institute for Artificial Intelligence 2017

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

        private Dictionary<string, SimObj> inventory = new Dictionary<string, SimObj>();



		#if UNITY_EDITOR
		//used to show what's currently visible
		void OnGUI () {
			if (currentVisibleObjects != null) {
				if (currentVisibleObjects.Length > 10) {
					int horzIndex = -1;
					GUILayout.BeginHorizontal ();
					foreach (SimObj o in currentVisibleObjects) {
						horzIndex++;
						if (horzIndex >= 3) {
							GUILayout.EndHorizontal ();
							GUILayout.BeginHorizontal ();
							horzIndex = 0;
						}
						GUILayout.Button (o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth (200f));
					}
					GUILayout.EndHorizontal ();
				} else {
					Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);
					foreach (SimObj o in currentVisibleObjects) {
						string suffix = "";
						Bounds bounds = new Bounds (o.gameObject.transform.position, new Vector3 (0.05f, 0.05f, 0.05f));
						if (GeometryUtility.TestPlanesAABB (planes, bounds)) {
							suffix += " VISIBLE";
						}
							
							
						GUILayout.Button (o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth (100f));
					}
				}
			}
		}
		#endif

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
           // Target_Text = GameObject.Find("Canvas/TargetText");;
           // Debug_Canvas = GameObject.Find("Canvas");


            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
                Debug_Canvas.SetActive(true);
                Target_Text.SetActive(true);


                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }


		}


   public void addObjectInventory(SimObj simObj)
        {
            inventory[simObj.UniqueID] = simObj;
        }

        public SimObj removeObjectInventory(string objectId)
        {
            SimObj so = inventory[objectId];
            inventory.Remove(objectId);
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

        protected void TakeItem(SimObj item)
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
        }

		protected bool openSimObj(SimObj so) 
        {
  
               // return updateAnimState(so.Animator, true);
            bool res = false;
            if (OPEN_CLOSE_STATES.ContainsKey(so.Type))
            {
                res = updateAnimState(so.Animator, OPEN_CLOSE_STATES[so.Type]["open"]);

            }

            else if (so.IsAnimated)
            {
                res = updateAnimState(so.Animator, true);
            }

            return res;
   
		}

        protected bool closeSimObj(SimObj so)
        {
      
                //res = updateAnimState (so.Animator, false);
                //return updateAnimState(so.Animator, false);

            bool res = false;
            if (OPEN_CLOSE_STATES.ContainsKey(so.Type))
            {
                res = updateAnimState(so.Animator, OPEN_CLOSE_STATES[so.Type]["close"]);
            }
            else if (so.IsAnimated)
            {
                res = updateAnimState(so.Animator, false);
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

        private void RaycastTarget()
        {
           // print("trying to raycast");
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(x, y));
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {
                //check for SimObjects that we are looking at
                if(hit.transform.tag == "SimObj")
                {
                    //update text to show what we are looking at
                    Target_Text.GetComponent<Text>().text = hit.transform.name;

                    //check if it is a receptacle
                    if(hit.transform.GetComponent<Receptacle>())
                    {
                        //print("this is a receptacle");
                        isReceptacle = true;
                    }

                    else
                    {
                        isReceptacle = false;
                    }

                    if(hit.transform.GetComponent<Convertable>())
                    {
                        //print("able to pick up");
                        isPickup = true;
                    }

                    else
                    {
                        isPickup = false;    
                    }
                }

                else
                {
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

		private void Update()	
        {
            RaycastTarget();

            //try to open on left click
            if(Input.GetMouseButtonDown(0))
            {
                //check if what we are looking at is a receptacle and can be opened
                if(isReceptacle)
                {
                    OpenReceptacle_ray();
                    //open receptacle here
                }

                if(isPickup)
                {
                    //try to pick up the thing?
                    TakeObject_ray();
                }

            }

            if(Input.GetMouseButtonDown(1))
            {
                if(isReceptacle)
                {
                    CloseReceptacle_ray();
                }
            }

			RotateView();


			//SimObj[] simObjects = GameObject.FindObjectsOfType (typeof(SimObj)) as SimObj[];


//			if (pickupObject) {
//				GameObject hand = GameObject.Find ("FirstPersonHand");
//
//				foreach (SimObj so in simObjects) {
//					if (so.UniqueID == "Lettuce|+01.36|+00.99|+00.79") {
//						Rigidbody rb = so.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
//						rb.useGravity = false;
//						so.transform.position = hand.transform.position;
//						so.transform.parent = this.transform;
//						so.transform.parent = m_CharacterController.transform;
//						
//					}
//				}
//				Vector3 target = new Vector3 (hand.transform.position.x - 0.5f, hand.transform.position.y, hand.transform.position.z);
//				hand.transform.position = Vector3.MoveTowards (hand.transform.position, target, 0.01f);
//			} else {
//				foreach (SimObj so in simObjects) {
//					if (so.UniqueID == "Lettuce|+01.36|+00.99|+00.79") {
//						Rigidbody rb = so.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
//						rb.useGravity = true;
//						so.transform.parent = null;
//
//					}
//				}
//			}
//

			if (captureScreenshot) {
				screenshotCounter++;
				StartCoroutine (EmitFrame ());
				captureScreenshot = false;
			}

			// the jump state needs to read here to make sure it is not missed
			if (!m_Jump)
			{
				m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
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

			currentVisibleObjects = SimUtil.GetAllVisibleSimObjs (m_Camera, MaxDistance);


			m_PreviouslyGrounded = m_CharacterController.isGrounded;
		}

		protected byte[] captureScreen() {
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

		private IEnumerator EmitFrame() {
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

			//m_MouseLook.UpdateCursorLock();

			//currentVisibleObjects = SimUtil.GetAllVisibleSimObjs (m_Camera, MaxDistance);

		}

        //don't need jump sound for now
		
		//private void PlayJumpSound()
		//{
	//		m_AudioSource.clip = m_JumpSound;
//			m_AudioSource.Play();
//		}




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
