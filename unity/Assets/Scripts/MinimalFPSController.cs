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
        private GameObject Crosshair;
         private GameObject   TargetText;
        private GameObject    ThrowForceBar;
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
            this.enableHighlightShader = false;
        }

        public new void HideHUD()
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

        public new void OnEnable()
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

        public new void OnDisable()
        {
            DisableMouseControl();
            ShowHUD();
        }

        public new void EnableMouseControl()
        {
            FPSEnabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public new void DisableMouseControl()
        {
            Debug.Log("Disabled mouse");
            FPSEnabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
	}
}

