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

        [SerializeField] private GameObject AgentHand = null;

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

        public bool looking = false;
		//private AudioSource m_AudioSource;

		public SimObjPhysics[] VisibleObjects;

        //public Collider[] testcolliders_in_view;

        //public Transform[] SweepResults;

        public SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;

            //sphere from the camera center with radius max distance * downward range extension
            //check what objects are in range by looking for their colliders

            //testcolliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance,
                                                    //1 << 8, QueryTriggerInteraction.Collide);

            //get all sim objects in range around us
            Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance, 
                                                         1 << 8 , QueryTriggerInteraction.Collide); //layermask is 8

            if(colliders_in_view != null)
            {
                foreach (Collider item in colliders_in_view)
                {
                    //get the SimObjPhysics component from the collider that was found
                    SimObjPhysics sop = item.GetComponent<SimObjPhysics>();

                    //is the sim object in range, check if it is bounds of the camera's viewport
                    if (CheckIfInViewport(sop, agentCamera, maxDistance - 0.01f))
                    {
                        sop.isVisible = true;
                        currentlyVisibleItems.Add(sop);

                        //draw a debug line to the object's transform
#if UNITY_EDITOR
                        Debug.DrawLine(agentCameraPos, sop.transform.position, Color.yellow);
#endif
                    }

                    else
                    {

                        print("out of range");
                        sop.isVisible = false;
                        currentlyVisibleItems.Remove(sop);
                    }
                }

                //now that we have a list of currently visible items, let's see which ones are interactable!
                Rigidbody HandRB = AgentHand.GetComponent<Rigidbody>();
                RaycastHit hit = new RaycastHit();

                foreach (SimObjPhysics visibleSimObjP in currentlyVisibleItems)
                {
                    //sweeptest from the agent's hand to the interaction point on the sim object
                    //sweeptest returns true if intersects any collider, otherwise false
                    //print(HandRB.SweepTest(visibleSimObjP.transform.position - AgentHand.transform.position, out hit, maxDistance));
                    //print(hit.collider);

                    //get all interaction points on the visible sim object we are checking here
                    Transform[] InteractionPoints = visibleSimObjP.InteractionPoints;

                    //cast ray from the agent's hand to each of the sim object's interaction points

                    int AccessibleInteractionPointCount = 0;
                    foreach (Transform ip in InteractionPoints)
                    {
                        float DistanceToInteractionPoint = Vector3.Distance(AgentHand.transform.position, ip.position);

                        Physics.Raycast(AgentHand.transform.position, ip.position - AgentHand.transform.position,
                                        out hit, DistanceToInteractionPoint);

                        //is the object that was hit the same as the visible sim object? great!
                        if (hit.transform.name == visibleSimObjP.transform.name)
                        {
                            //show succesfull raycast from hand to the interaction point
#if UNITY_EDITOR
                            Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
#endif

                            AccessibleInteractionPointCount++;
                            //visibleSimObjP.isInteractable = true;
                            //now set the game object as interactable somehow....
                        }

                        //sweeptest stuff
                        RaycastHit[] SweptObjects = AgentHand.GetComponent<Rigidbody>().SweepTestAll(ip.position - AgentHand.transform.position);

                        //for each thing hit by this sweep, check if it's hit.transform.name == the interactive point's parent's name
                        //print(SweptObjects.ToString());

                    }

                    if (AccessibleInteractionPointCount != 0)
                        visibleSimObjP.isInteractable = true;

                    else
                    {
                        visibleSimObjP.isInteractable = false;
                    }
                }
            }
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            //we're done!
            return currentlyVisibleItems.ToArray();
        }

        //check if the visibility point on the physics sim object in question is in the viewport at all
        //static bool CheckVisibilityPoint(SimObjPhysics item, Vector3 itemVisibilityPointLocation, Camera agentCamera)
        //{
        //    SimObjManipTypePhysics[] itemManipType = item.GetComponent<SimObjPhysics>().ManipType;

        //    bool DoWeCareABoutThisObjectsVisibility = false;

        //    foreach(SimObjManipTypePhysics type in itemManipType)
        //    {
        //        if (type == SimObjManipTypePhysics.CanPickup || type == SimObjManipTypePhysics.Receptacle)
        //        {
        //            DoWeCareABoutThisObjectsVisibility = true;
        //        }
        //    }
        //    //is this sim object one that doesn't care about being visible?
        //    if (DoWeCareABoutThisObjectsVisibility == true)
        //    {
        //        Vector3 viewPoint = agentCamera.WorldToViewportPoint(itemVisibilityPointLocation);

        //        //move these two up to variables later
        //        float ViewPointRangeHigh = 1.0f;
        //        float ViewPointRangeLow = 0.0f;

        //        if (viewPoint.z > 0 //is in front of camera
        //           && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds
        //            && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds
        //        {
        //            return true;
        //        }

        //        else
        //            return false;

        //    }

        //    else
        //        return false;

        //}

        //see if a given SimObjPhysics is within the camera's range and field of view
        static bool CheckIfInViewport(SimObjPhysics item, Camera agentCamera, float maxDistance)
        {
            SimObjManipTypePhysics[] itemManipType = item.GetComponent<SimObjPhysics>().ManipType;

            bool DoWeCareABoutThisObjectsVisibility = false;

            foreach (SimObjManipTypePhysics type in itemManipType)
            {
                if (type == SimObjManipTypePhysics.CanPickup || type == SimObjManipTypePhysics.Receptacle)
                {
                    DoWeCareABoutThisObjectsVisibility = true;
                }
            }

            if (DoWeCareABoutThisObjectsVisibility == true)
            {
                Vector3 viewPoint = agentCamera.WorldToViewportPoint(item.transform.position);

                //move these two up to variables later
                float ViewPointRangeHigh = 1.0f;
                float ViewPointRangeLow = 0.0f;

                if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                   && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds
                    && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds
                {
                    return true;
                }

                else
                    return false;
            }

            else return false;
        }
      

		#if UNITY_EDITOR
		//used to show what's currently visible
		void OnGUI () 
        {
            //Vector3 p = new Vector3();
            //Camera c = Camera.main;
            //Event e = Event.current;
            //Vector2 mousePos = new Vector2();

            //// Get the mouse position from Event.
            //// Note that the y position from Event is inverted.
            //mousePos.x = e.mousePosition.x;
            //mousePos.y = c.pixelHeight - e.mousePosition.y;

            //p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane));

            //GUILayout.BeginArea(new Rect(20, 20, 250, 120));
            //GUILayout.Label("Screen pixels: " + c.pixelWidth + ":" + c.pixelHeight);
            //GUILayout.Label("Mouse position: " + mousePos);
            //GUILayout.Label("World position: " + p.ToString("F3"));
            //GUILayout.EndArea();

			if (VisibleObjects != null) 
            {
				if (VisibleObjects.Length > 10) 
                {
					int horzIndex = -1;
					GUILayout.BeginHorizontal ();
					foreach (SimObjPhysics o in VisibleObjects) 
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

                    //int position_number = 0;
					foreach (SimObjPhysics o in VisibleObjects) 
                    {
						string suffix = "";
						Bounds bounds = new Bounds (o.gameObject.transform.position, new Vector3 (0.05f, 0.05f, 0.05f));
						if (GeometryUtility.TestPlanesAABB (planes, bounds)) 
                        {
                            //position_number += 1;

                            //if (o.GetComponent<SimObj>().Manipulation == SimObjManipType.Inventory)
                            //    suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

                            //else
                                //suffix += " VISIBLE";
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

     

		private void Update()	
        {
            VisibleObjects = GetAllVisibleSimObjPhysics(m_Camera, 2.0f);
            
            //RaycastTarget();

            //////MOUSE AND KEYBAORD INPUT///////////////////////////////////////////////////
            //on left mouse click
            if(Input.GetMouseButtonDown(0))
            {
             

            }

            //on right mouse click
            if(Input.GetMouseButtonDown(1))
            {

              

            }

            if(Input.GetKey(KeyCode.Space))
            {

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                looking = true;

                Vector3 Where = m_Camera.ScreenToWorldPoint(Input.mousePosition);
                //is there something in the inventory? if so, place it in the receptacle we are looking at
                /*if (isReceptacle && !String.IsNullOrEmpty(current_Object_In_Inventory))
                {
                    PutObject_ray(current_Object_In_Inventory);
                }

                else if(!String.IsNullOrEmpty(current_Object_In_Inventory))
                    Debug.Log("You can't put that there!");

                else if(isReceptacle && String.IsNullOrEmpty(current_Object_In_Inventory))
                {
                    Debug.Log("Nothing in your Inventory!");
                }*/
            }

            if(Input.GetKeyUp(KeyCode.Space))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                looking = false;
            }

            ///////////////////////////////////////////////////////////////////////////
			if(looking == false)
            RotateView();


			//if (captureScreenshot) {
			//	screenshotCounter++;
			//	StartCoroutine (EmitFrame ());
			//	captureScreenshot = false;
			//}

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
            //currentVisibleObjects = SimUtil.GetAllVisibleSimObjs(m_Camera, MaxDistance);


			m_PreviouslyGrounded = m_CharacterController.isGrounded;
		}


		//protected byte[] captureScreen() 
  //      {
		//	int width = Screen.width;
		//	int height = Screen.height;

		//	Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

		//	// read screen contents into the texture
		//	tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		//	tex.Apply();

		//	// encode texture into JPG - XXX SHOULD SET QUALITY
		//	byte[] bytes = tex.EncodeToPNG();
		//	Destroy(tex);
		//	return bytes;
		//}

		//private IEnumerator EmitFrame() 
  //      {
		//	yield return new WaitForEndOfFrame ();
		//	File.WriteAllBytes ("/Users/erick/Desktop/screenshots/screenshot-" + screenshotCounter.ToString () + ".png", captureScreen ());
		//}

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
			if (/*!captureScreenshot &&*/ rotateMouseLook) {
				m_MouseLook.LookRotation (transform, m_Camera.transform);
			}

		}


		/*private void OnControllerColliderHit(ControllerColliderHit hit)
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

			//body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
		}*/
	}
}

