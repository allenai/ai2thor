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
using UnityStandardAssets.Characters.FirstPerson;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class ObjectHighlightController
	{
        [SerializeField] private Text TargetText = null;
        [SerializeField] private Slider ThrowForceBarSlider = null;

        [SerializeField] private float MinHighlightDistance = 5f;
        [SerializeField] private float MaxChargeThrowSeconds = 1.4f;
        [SerializeField] private float MaxThrowForce = 1000.0f;
        [SerializeField] private bool DisplayTargetText = true;
		
        //this is true if FPScontrol mode using Mouse and Keyboard is active
        public bool TextInputMode = false;
		public GameObject InputFieldObj = null;
        public Text CrosshairText = null;

        private Camera m_Camera;

        private float timerAtPress;

        private SimObjPhysics highlightedObject;
        private Shader previousShader;
        private Shader highlightShader;
        private bool pickupState;
        private bool mouseDownThrow;
        private PhysicsRemoteFPSAgentController PhysicsController;
        // Optimization
        private bool strongHighlight = true;

        public ObjectHighlightController(PhysicsRemoteFPSAgentController physicsController, float minHighlightDistance, float maxThrowForce, float maxChargeThrowSeconds)
        {
            this.PhysicsController = physicsController;
            this.MinHighlightDistance = minHighlightDistance;
            this.MaxThrowForce = maxThrowForce;
            this.MaxChargeThrowSeconds = maxChargeThrowSeconds;
            m_Camera = Camera.main;

            TargetText = GameObject.Find("DebugCanvasPhysics/TargetText").GetComponent<Text>();

            InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            CrosshairText = GameObject.Find("DebugCanvasPhysics/Crosshair").GetComponent<Text>();
            var throwForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            ThrowForceBarSlider = throwForceBar.GetComponent<Slider>();

            this.highlightShader = Shader.Find("Custom/TransparentOutline");
            // #endif
        }
        
        public void ToggleDisplayTargetText(bool display)
        {
            this.DisplayTargetText = display;
        }

        public void MouseControls()
		{
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
            
            // Throw action on left clock release
            if (Input.GetKeyUp(KeyCode.Mouse0))
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
		}

        public void UpdateHighlightedObject(Vector3 screenPosition) {
            /* if(TextInputMode == false)
			{
                Debug.Log("FPS INPUT!!!!!!!!!");
				//this is the mouselook in first person mode
				FPSInput();
				if(Cursor.visible == false)
				{
                    //accept input to update view based on mouse input
                    MouseRotateView();
                }
			}*/
            RaycastHit hit = new RaycastHit();
            //  Ray ray = new Ray();
            // if (!allowFPSControl) {
            var ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(screenPosition);
            int layerMask = LayerMask.GetMask("SimObjVisible"); 
            Physics.Raycast(ray, out hit, this.MinHighlightDistance, layerMask);
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            // }

            

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

            // SimObjPhysics simObj = null;
            // if (!allowFPSControl) {
            //     Func<SimObjPhysics, bool> filter = (SimObjPhysics x) => { 
            //             return x.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
            //                     x.GetComponent<CanOpen_Object>() ||
            //                     x.GetComponent<CanToggleOnOff>();
            //         };
            //     simObj = PhysicsController.ClosestObject(filter);
            // }
            // else if(
            //     hit.transform != null 
            //     && hit.transform.tag == "SimObjPhysics" ) {
            //     simObj = hit.transform.GetComponent<SimObjPhysics>();

            // }

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
  
	}

}