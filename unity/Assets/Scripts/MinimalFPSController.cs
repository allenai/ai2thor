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
using System.Linq;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]
    public class MinimalFPSController : DebugFPSAgentController
	{
        private GameObject BackgroundUI;
        MinimalFPSController() {
            this.m_MouseLook = new MouseLook {
                XSensitivity = 2,
                YSensitivity = 2,
                clampVerticalRotation = true,
                MinimumX = -30,
                MaximumX = 60,
                smooth = false,
                smoothTime = 5,
                lockCursor = false
            };
            this.m_WalkSpeed = 1;
            this.m_RunSpeed = 2;
            this.m_GravityMultiplier = 2;
        }
 
// 		[SerializeField] private bool m_IsWalking;
// 		[SerializeField] private float m_WalkSpeed;
// 		[SerializeField] private float m_RunSpeed;

// 		[SerializeField] private float m_GravityMultiplier;
// 		[SerializeField] private MouseLook m_MouseLook;

//         [SerializeField] private GameObject Debug_Canvas = null;
// //        [SerializeField] private GameObject Inventory_Text = null;
// 		[SerializeField] private GameObject InputMode_Text = null;
//         [SerializeField] private float MaxViewDistance = 5.0f;
//         [SerializeField] private float MaxChargeThrowSeconds = 1.4f;
//         [SerializeField] private float MaxThrowForce = 1000.0f;
//         public bool FlightMode = false;

//         public bool FPSEnabled = true;
// 		public GameObject InputFieldObj = null;

//         private ObjectHighlightController highlightController = null;

//         private Camera m_Camera;
//         private Vector2 m_Input;
//         private Vector3 m_MoveDir = Vector3.zero;
//         private CharacterController m_CharacterController;
//         private PhysicsRemoteFPSAgentController PhysicsController;
//         private bool scroll2DEnabled = true;

//         private void Start()
//         {
//             m_CharacterController = GetComponent<CharacterController>();
//             m_Camera = Camera.main;
//             m_MouseLook.Init(transform, m_Camera.transform);
            
//             //find debug canvas related objects 
//             Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
// 			InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");

//             InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
//             PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();

//             highlightController = new ObjectHighlightController(
//                 PhysicsController,
//                 MaxViewDistance,
//                 false, 
//                 true, 
//                 MaxThrowForce, 
//                 MaxChargeThrowSeconds
//             );

//             //if this component is enabled, turn on the targeting reticle and target text
//             if (this.isActiveAndEnabled)
//             {
// 				Debug_Canvas.GetComponent<Canvas>().enabled = true;            
//                 Cursor.visible = false;
//                 Cursor.lockState = CursorLockMode.Locked;
//             }
          
//             FlightMode = PhysicsController.FlightMode;

//             #if UNITY_WEBGL
//                 FPSEnabled = false;
//                 Cursor.visible = true;
//                 Cursor.lockState = CursorLockMode.None;
//                 HideHUD();
//             #endif
//         }

        public void HideHUD()
        {
            InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            if (InputMode_Text != null) {
                InputMode_Text.SetActive(false);
            }
            // InputFieldObj.SetActive(false);

            InputFieldObj.GetComponent<Image>().enabled = false;
            InputFieldObj.GetComponent<InputField>().enabled = false;
            InputFieldObj.GetComponentsInChildren<Text>().ToList().ForEach(x => x.enabled = false);

            BackgroundUI = GameObject.Find("DebugCanvasPhysics/InputModeText_Background");
            BackgroundUI.SetActive(false);

            Crosshair =  GameObject.Find("DebugCanvasPhysics/Crosshair");
            TargetText = GameObject.Find("DebugCanvasPhysics/TargetText");
            ThrowForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            Crosshair.GetComponent<Text>().enabled = false;
            ThrowForceBar.SetActive(false);
            TargetText.GetComponent<Text>().enabled = false;
        }

          public void ShowHUD()
        {
            if (InputMode_Text != null) {
                InputMode_Text.SetActive(true);
            }
            if (InputFieldObj != null) {
                InputFieldObj.SetActive(true);
            }
            if (BackgroundUI != null) {
                BackgroundUI.SetActive(true);
            }


            InputFieldObj.GetComponent<Image>().enabled = true;
            InputFieldObj.GetComponent<InputField>().enabled = true;
            InputFieldObj.GetComponentsInChildren<Text>().ToList().ForEach(x => x.enabled = true);
            Crosshair.GetComponent<Text>().enabled = true;
            ThrowForceBar.SetActive(true);
            TargetText.GetComponent<Text>().enabled = true;
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
                HideHUD();
              
        }

        public void OnDisable()
        {
            DisableMouseControl();
            ShowHUD();
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
	}
}

