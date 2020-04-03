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
        //for use with mouse/keyboard input
		[SerializeField] protected bool m_IsWalking;
		[SerializeField] protected float m_WalkSpeed;
		[SerializeField] protected float m_RunSpeed;

		[SerializeField] protected float m_GravityMultiplier;
		[SerializeField] protected MouseLook m_MouseLook;

        [SerializeField] protected GameObject Debug_Canvas = null;
//        [SerializeField] private GameObject Inventory_Text = null;
		[SerializeField] protected GameObject InputMode_Text = null;
        [SerializeField] protected float MaxViewDistance = 5.0f;
        [SerializeField] private float MaxChargeThrowSeconds = 1.4f;
        [SerializeField] private float MaxThrowForce = 1000.0f;
        // public bool FlightMode = false;

        public bool FPSEnabled = true;
		public GameObject InputFieldObj = null;

        private ObjectHighlightController highlightController = null;

        private Camera m_Camera;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private PhysicsRemoteFPSAgentController PhysicsController;
        private bool scroll2DEnabled = true;

        protected bool enableHighlightShader = true;

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_MouseLook.Init(transform, m_Camera.transform);
            
            //find debug canvas related objects 
            Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
			InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");

            InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();

            highlightController = new ObjectHighlightController(PhysicsController, MaxViewDistance, enableHighlightShader, true, MaxThrowForce, MaxChargeThrowSeconds);

            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
				Debug_Canvas.GetComponent<Canvas>().enabled = true;            
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
          
            // FlightMode = PhysicsController.FlightMode;

            #if UNITY_WEBGL
                FPSEnabled = false;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                HideHUD();
            #endif
        }
		public Vector3 ScreenPointMoveHand(float yOffset)
		{
			RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			//shoot a ray out based on mouse position
			Physics.Raycast(ray, out hit);
            return hit.point + new Vector3(0, yOffset, 0);
		}

        public void HideHUD()
        {
            if (InputMode_Text != null) {
                InputMode_Text.SetActive(false);
            }
            InputFieldObj.SetActive(false);
            var background = GameObject.Find("DebugCanvasPhysics/InputModeText_Background");
            background.SetActive(false);
        }

        public void SetScroll2DEnabled(bool enabled)
        {
            this.scroll2DEnabled = enabled;
        }

        public void OnEnable()
        {
            
                FPSEnabled = true;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                
                InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
                InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
                if (InputMode_Text) {
                    InputMode_Text.GetComponent<Text>().text = "FPS Mode";
                }


                Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
  
                Debug_Canvas.GetComponent<Canvas>().enabled = true;
              
        }

        public void OnDisable()
        {
            DisableMouseControl();
            //  if (InputFieldObj) {
            //     InputFieldObj.SetActive(true);
            //  }
        }

        public void EnableMouseControl()
        {
            FPSEnabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void DisableMouseControl()
        {
            Debug.Log("Disabled mouse");
            FPSEnabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void DebugKeyboardControls()
		{
			//swap between text input and not
			if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Escape))
            {
				//Switch to Text Mode
                if (FPSEnabled)
                {
                    if (InputMode_Text) {
                        InputMode_Text.GetComponent<Text>().text = "FPS Mode";
                    }
                    FPSEnabled = false;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    return;
                }
                else
                 {               
                    if (InputMode_Text) {
					    InputMode_Text.GetComponent<Text>().text = "FPS Mode (mouse free)";
                    }
                    FPSEnabled = true;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    return;
                }

            }

            // 1D Scroll for hand movement
            if (!scroll2DEnabled && this.PhysicsController.WhatAmIHolding() != null)
            {
                var scrollAmount = Input.GetAxis("Mouse ScrollWheel");

                var eps = 1e-6;
                if (Mathf.Abs(scrollAmount) > eps) {
                    ServerAction action = new ServerAction
                    {
                        action = "MoveHandAhead",
                        moveMagnitude = scrollAmount
                    };
                    this.PhysicsController.ProcessControlCommand(action);
                }

            }
            

            if (Input.GetKeyDown(KeyCode.R))
            {
                var action = new ServerAction
                {
                    action = "InitialRandomSpawn",
                    randomSeed = 0,
                    forceVisible = false,
                    numPlacementAttempts = 5,
                    placeStationary = true
                };
                PhysicsController.ProcessControlCommand(action);
            }

            if(Input.GetKey(KeyCode.Space))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if(Input.GetKeyUp(KeyCode.Space))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
		}

		private void Update()	
        {
            highlightController.UpdateHighlightedObject(new Vector3(Screen.width / 2, Screen.height / 2));
            highlightController.MouseControls();

			DebugKeyboardControls();
         
            ///////////////////////////////////////////////////////////////////////////
			//we are not in focus mode, so use WASD and mouse to move around
			if(FPSEnabled)
			{
				FPSInput();
				if(Cursor.visible == false)
				{
                    //accept input to update view based on mouse input
                    MouseRotateView();
                }
            }
        }

        public void OnGUI()
        {
            if (Event.current.type == EventType.ScrollWheel && scroll2DEnabled)
            {
                if (this.PhysicsController.WhatAmIHolding() != null)
                {
                    var scrollAmount = Event.current.delta;
                    var eps = 1e-6;
                    if (Mathf.Abs(scrollAmount.x) > eps || Mathf.Abs(scrollAmount.y) > eps)
                    {
                        ServerAction action = new ServerAction
                        {
                            action = "MoveHandDelta",
                            x = scrollAmount.x * 0.05f,
                            z = scrollAmount.y * -0.05f,
                            y = 0
                        };
                        this.PhysicsController.ProcessControlCommand(action);
                    }

                }
            }
        }

        private void GetInput(out float speed)
		{
			// Read input
			float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
			float vertical = CrossPlatformInputManager.GetAxis("Vertical");

			//bool waswalking = m_IsWalking;

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
            
		}

        public MouseLook GetMouseLook() {
            return m_MouseLook;
        }

		private void MouseRotateView()
		{
   			m_MouseLook.LookRotation (transform, m_Camera.transform);         
		}

        private void FPSInput()
		{                  
            //take WASD input and do magic, turning it into movement!
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;
            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;

            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;    

			// if(!FlightMode)
            m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;   

            //added this check so that move is not called if/when the Character Controller's capsule is disabled. Right now the capsule is being disabled when open/close animations are in progress so yeah there's that
            if(m_CharacterController.enabled == true)       
            m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
		}

  
	}
}

