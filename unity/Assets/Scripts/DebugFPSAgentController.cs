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
		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;


		[SerializeField] private float m_GravityMultiplier;
		[SerializeField] private MouseLook m_MouseLook;

        [SerializeField] private GameObject Debug_Canvas = null;
//        [SerializeField] private GameObject Inventory_Text = null;
		[SerializeField] private GameObject InputMode_Text = null;

        [SerializeField] private Text TargetText = null;
        // [SerializeField] private GameObject ThrowForceBar = null;
        [SerializeField] private Slider ThrowForceBarSlider = null;
//        [SerializeField] private GameObject AgentHand = null;
//        [SerializeField] private GameObject ItemInHand = null;
        [SerializeField] private float MaxChargeThrowSeconds = 1.4f;
        [SerializeField] private float MaxThrowForce = 1000.0f;
        [SerializeField] private bool DisplayTargetText = true;

        public bool FlightMode = false;

		
        //this is true if FPScontrol mode using Mouse and Keyboard is active
        public bool TextInputMode = false;
		public GameObject InputFieldObj = null;
        public Text CrosshairText = null;

        private Camera m_Camera;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;

        private float timerAtPress;

        private SimObjPhysics highlightedObject;
        private Shader previousShader;
        private Shader highlightShader;
        private bool pickupState;
        private bool mouseDownThrow;
        private PhysicsRemoteFPSAgentController PhysicsController;
        private bool scroll2DEnabled = true;
        // Optimization
        private bool strongHighlight = true;

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_MouseLook.Init(transform, m_Camera.transform);
            
            //find debug canvas related objects 
            Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
			InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");

            TargetText = GameObject.Find("DebugCanvasPhysics/TargetText").GetComponent<Text>();

            InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            CrosshairText = GameObject.Find("DebugCanvasPhysics/Crosshair").GetComponent<Text>();
            var throwForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            ThrowForceBarSlider = throwForceBar.GetComponent<Slider>();

            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
				Debug_Canvas.GetComponent<Canvas>().enabled = true;            
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();
            FlightMode = PhysicsController.FlightMode;

            this.highlightShader = Shader.Find("Custom/TransparentOutline");

            #if UNITY_WEBGL
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

				//TestBall.transform.position = hit.point + new Vector3(0, 0.3f, 0);
				return hit.point + new Vector3(0, yOffset, 0);
		}

        public void HideHUD()
        {
            InputMode_Text.SetActive(false);
            InputFieldObj.SetActive(false);
            var background = GameObject.Find("DebugCanvasPhysics/InputModeText_Background");
            background.SetActive(false);
        }

        public void SetScroll2DEnabled(bool enabled)
        {
            this.scroll2DEnabled = enabled;
        }

        public void ToggleDisplayTargetText(bool display)
        {
            this.DisplayTargetText = display;
        }

        public void EnableMouseControl()
        {
            TextInputMode = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void DisableMouseControl()
        {
            Debug.Log("Disabled mouse");
            TextInputMode = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void DebugKeyboardControls()
		{
			//swap between text input and not
			if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Escape))
            {
				//Switch to Text Mode
                if (TextInputMode == false)
                {
					InputMode_Text.GetComponent<Text>().text = "Text Input Mode";
                    TextInputMode = true;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    return;
                }

                //Switch to Mouse and Keyboard Mode
                if (TextInputMode == true)
                {               
					InputMode_Text.GetComponent<Text>().text = "Free Mode";
                    TextInputMode = false;
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    return;
                }

            }

            //test slicing object
            if(Input.GetKeyDown(KeyCode.Mouse1))
            {
                if(TextInputMode == false && this.PhysicsController.actionComplete)
                {
                    var closestObj = this.highlightedObject;

                    if (closestObj != null)
                    {
                        var actionName ="";

                        if(closestObj.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeSliced))
                        {
                            actionName = "SliceObject";
                        }

                        if(actionName != "")
                        {
                            ServerAction action = new ServerAction
                            {
                                action = actionName,
                                objectId = closestObj.uniqueID
                            };

                            this.PhysicsController.ProcessControlCommand(action);
                        }
                    }
                }
            }

            //try and put held object on/in something
            if(Input.GetKeyDown(KeyCode.P))
            {
                if(TextInputMode == false && this.PhysicsController.actionComplete)
                {
                        ServerAction action = new ServerAction();
                        action.action = "PutObject";
                        action.receptacleObjectId = PhysicsController.UniqueIDOfClosestReceptacleObject();

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action.placeStationary = true; 

                        //set this true to ignore Placement Restrictions
                        action.forceAction = true;

                        this.PhysicsController.ProcessControlCommand(action);
                }
            }

            // Interact action for mouse left-click
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {

                if (TextInputMode == false)
                {
                    if (this.PhysicsController.WhatAmIHolding() == null && this.PhysicsController.actionComplete)
                    {
                        var closestObj = this.highlightedObject;
                        if (closestObj != null)
                        {
                            var actionName = "";
                            if (closestObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
                            {
                                pickupState = true;
                                actionName = "PickupObject";
                            }
                            else if (closestObj.GetComponent<CanOpen_Object>())
                            {
                                actionName = closestObj.GetComponent<CanOpen_Object>().isOpen ? "CloseObject" : "OpenObject";
                            }
                            else if (closestObj.GetComponent<CanToggleOnOff>())
                            {
                                actionName = closestObj.GetComponent<CanToggleOnOff>().isOn ? "ToggleObjectOff" : "ToggleObjectOn";
                            }

                            if (actionName != "")
                            {
                                ServerAction action = new ServerAction
                                {
                                    action = actionName,
                                    objectId = closestObj.uniqueID
                                };
                                this.PhysicsController.ProcessControlCommand(action);
                            }
                        }
                    }
                    else if (this.PhysicsController.actionComplete)
                    {
                        this.mouseDownThrow = true;
                        this.timerAtPress = Time.time;
                    }
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

            // Sets throw bar value
            if (this.mouseDownThrow)
            {
                var diff = Time.time - this.timerAtPress;
                var clampedForceTime = Mathf.Min(diff * diff, MaxChargeThrowSeconds);
                ThrowForceBarSlider.value = clampedForceTime / MaxChargeThrowSeconds;

            }
            else
            {
                ThrowForceBarSlider.value -= ThrowForceBarSlider.value > 0.0f ?
                     Time.deltaTime / MaxChargeThrowSeconds :
                     0.0f;
            }

            if (Input.GetKeyDown(KeyCode.R) && TextInputMode == false)
            {
                var action = new ServerAction
                {
                    action = "InitialRandomSpawn",
                    randomSeed = 0,
                    forceVisible = false,
                    maxNumRepeats = 5,
                    placeStationary = true
                };
                PhysicsController.ProcessControlCommand(action);
            }

            // Throw action on left clock release
            if (!TextInputMode && Input.GetKeyUp(KeyCode.Mouse0))
            {
                // Debug.Log("Pickup state " + pickupState + " obj " + this.PhysicsController.WhatAmIHolding());
                if (!pickupState)
                {
                  
                    if (this.PhysicsController.WhatAmIHolding() != null)
                    {
                        var diff = Time.time - this.timerAtPress;
                        var clampedForceTime = Mathf.Min(diff * diff, MaxChargeThrowSeconds);
                        var force = clampedForceTime * MaxThrowForce / MaxChargeThrowSeconds;

                        if (this.PhysicsController.actionComplete)
                        {
                            ServerAction action = new ServerAction
                            {
                                action = "ThrowObject",
                                moveMagnitude = force,
                                forceAction = true
                            };
                            this.PhysicsController.ProcessControlCommand(action);
                            this.mouseDownThrow = false;
                            //ThrowForceBar.GetComponent<Slider>().value = 0.0f;
                        }
                    }
                }
                else
                {
                    pickupState = false;
                }
            }

            // allow actions only via text input
            if (TextInputMode == true)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                //if we press enter, select the input field
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(InputFieldObj);

                }
            }

            // no text input, we are in fps mode
             if (TextInputMode == false)
             {
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
		}

		private void Update()	
        {
			DebugKeyboardControls();
         
            ///////////////////////////////////////////////////////////////////////////
			//we are not in focus mode, so use WASD and mouse to move around
			if(TextInputMode == false)
			{
				//this is the mouselook in first person mode
				FPSInput();
				if(Cursor.visible == false)
				{
                    //accept input to update view based on mouse input
                    MouseRotateView();
                }
			}

            var ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("SimObjVisible"); 
            Physics.Raycast(ray, out hit, 5f, layerMask);
            Debug.DrawLine(ray.origin, hit.point, Color.red);

            

            if (this.highlightedObject != null)
            {

                var meshRenderer = this.highlightedObject.GetComponentInChildren<MeshRenderer>();

                setTargetText("");
                strongHighlight = true;
                if (meshRenderer != null)
                {
                    meshRenderer.material.shader = this.previousShader;
                }
            }

            if (hit.transform != null 
                && hit.transform.tag == "SimObjPhysics" 
                && this.PhysicsController.WhatAmIHolding() == null
               )
            {
                var simObj = hit.transform.GetComponent<SimObjPhysics>();
                Func<bool> validObjectLazy = () => { 
                    return simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
                                  simObj.GetComponent<CanOpen_Object>() ||
                                  simObj.GetComponent<CanToggleOnOff>();
                };
                if (simObj != null && validObjectLazy())
                {
                    var d = hit.point - ray.origin;
                    d.y = 0;
                    var distance = d.magnitude;
                    var withinReach = PhysicsController.FindObjectInVisibleSimObjPhysics(simObj.uniqueID) != null;
                    setTargetText(simObj.name, withinReach);
                    this.highlightedObject = simObj;
                    var mRenderer = this.highlightedObject.GetComponentInChildren<MeshRenderer>();
                    if (mRenderer != null)
                    {

                        this.previousShader = mRenderer.material.shader;
                        mRenderer.material.shader = this.highlightShader;

                        if (withinReach)
                        {
                            strongHighlight = true;
                            mRenderer.sharedMaterial.SetFloat("_Outline", 0.005f);
                            mRenderer.sharedMaterial.SetColor("_OutlineColor", new Color(1, 1, 1, 0.3f));
                        }
                        else if (strongHighlight)
                        {
                            strongHighlight = false;
                            mRenderer.sharedMaterial.SetFloat("_Outline", 0.001f);
                            mRenderer.sharedMaterial.SetColor("_OutlineColorr", new Color(0.66f, 0.66f, 0.66f, 0.1f));
                        }
                    }
                }
            }
            else
            {
                this.highlightedObject = null;
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

        private void setTargetText(string text, bool withinReach = false)
        {
            var eps = 1e-5;
            if (withinReach)
            {
                this.TargetText.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                this.CrosshairText.text = "( + )";
            }
            else if (Math.Abs(this.TargetText.color.a - 1.0f) < eps)
            {
                this.TargetText.color = new Color(197.0f / 255, 197.0f / 255, 197.0f / 255, 228.0f / 255);
                this.CrosshairText.text = "+";
            }

            if (DisplayTargetText && TargetText != null)
            {
                this.TargetText.text = text;
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

			if(!FlightMode)
            m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;   

            //added this check so that move is not called if/when the Character Controller's capsule is disabled. Right now the capsule is being disabled when open/close animations are in progress so yeah there's that
            if(m_CharacterController.enabled == true)       
            m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
		}

  
	}
}

