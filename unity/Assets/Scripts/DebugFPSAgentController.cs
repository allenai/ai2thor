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
//        [SerializeField] private GameObject AgentHand = null;
//        [SerializeField] private GameObject ItemInHand = null;

		private Camera m_Camera;
		//public bool rotateMouseLook;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;

        //this is true if FPScontrol mode using Mouse and Keyboard is active
        public bool TextInputMode = false;
		public GameObject InputFieldObj = null;

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_MouseLook.Init(transform, m_Camera.transform);
            
            //find debug canvas related objects 
            Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
			//Inventory_Text = GameObject.Find("DebugCanvas/InventoryText");
			InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");

			InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");

            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
				Debug_Canvas.GetComponent<Canvas>().enabled = true;            
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
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

        private void DebugKeyboardControls()
		{
			//swap between text input and not
			if (Input.GetKeyDown(KeyCode.BackQuote))
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

            //allow actions only via text input
            if (TextInputMode == true)
            {
                //if we press enter, select the input field
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(InputFieldObj);

                }
            }

            //no text input, we are in fps mode
            // if (TextInputMode == false)
            // {
			// 	if(Input.GetKey(KeyCode.Space))
			// 	{
			// 		Cursor.visible = true;
            //         Cursor.lockState = CursorLockMode.None;
			// 	}

            //     if(Input.GetKeyUp(KeyCode.Space))
			// 	{
			// 		Cursor.visible = false;
            //         Cursor.lockState = CursorLockMode.Locked;
			// 	}
            // }
		}

		private void Update()	
        {
			//constantly check for visible objects in front of agent
			//VisibleObjects = GetAllVisibleSimObjPhysics(m_Camera, MaxDistance);

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

            //we are in focus mode, this should be the default - can toggle fps control from here
            //by default we can only use enter to execute commands in the text field
			if(TextInputMode == true)
			{
				
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
            m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;         
            m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
		}

  
	}
}

