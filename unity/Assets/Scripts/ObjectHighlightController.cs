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

    [Serializable]
    public class HighlightConfig {
        public Color TextStrongColor;
        public Color TextFaintColor;
        public Color SoftOutlineColor;
        public float SoftOutlineThickness;
        public Color WithinReachOutlineColor;
        public float WithinReachOutlineThickness;
    }

    public class ObjectHighlightController
	{
        [SerializeField] private Text TargetText = null;
        [SerializeField] private Slider ThrowForceBarSlider = null;
        [SerializeField] private float MinHighlightDistance = 5f;
        [SerializeField] private float MaxChargeThrowSeconds = 1.4f;
        [SerializeField] private float MaxThrowForce = 1000.0f;
        [SerializeField] private bool DisplayTargetText = true;
        [SerializeField] private HighlightConfig HighlightParams = new HighlightConfig {
            TextStrongColor = new Color(1.0f, 1.0f, 1.0f, 1.0f),
            TextFaintColor = new Color(197.0f / 255, 197.0f / 255, 197.0f / 255, 228.0f / 255),
            SoftOutlineColor = new Color(0.66f, 0.66f, 0.66f, 0.1f),
            SoftOutlineThickness = 0.001f,
            WithinReachOutlineColor = new Color(1, 1, 1, 0.3f),
            WithinReachOutlineThickness = 0.005f,
        };
        public Text CrosshairText = null;
        private Camera m_Camera;
        private float timerAtPress;
        private SimObjPhysics highlightedObject;
        private Shader previousShader;
        private Shader highlightShader;
        private int previousRenderQueueValue = -1;
        private bool pickupState;
        private bool mouseDownThrow;
        private PhysicsRemoteFPSAgentController PhysicsController;
        private bool throwEnabled;
        // Optimization
        private bool softHighlight = true;

        private bool highlightWhileHolding = false;

        private string onlyPickableObjectId = null;

        private bool disableHighlightShaderForObject = false;


        public ObjectHighlightController(
            PhysicsRemoteFPSAgentController physicsController,
            float minHighlightDistance,
            bool throwEnabled = true,
            float maxThrowForce = 1000.0f,
            float maxChargeThrowSeconds = 1.4f,
            bool highlightWhileHolding = false,
            HighlightConfig highlightConfig = null
        )   
        {
            this.PhysicsController = physicsController;
            this.MinHighlightDistance = minHighlightDistance;
            this.MaxThrowForce = maxThrowForce;
            this.MaxChargeThrowSeconds = maxChargeThrowSeconds;
            this.highlightWhileHolding = highlightWhileHolding;
            if (highlightConfig != null) {
                this.HighlightParams = highlightConfig;
            }
            m_Camera = Camera.main;
            TargetText = GameObject.Find("DebugCanvasPhysics/TargetText").GetComponent<Text>();
            CrosshairText = GameObject.Find("DebugCanvasPhysics/Crosshair").GetComponent<Text>();
            var throwForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            if (throwForceBar) {
                ThrowForceBarSlider = throwForceBar.GetComponent<Slider>();
            }
            this.throwEnabled = throwEnabled && throwForceBar != null;
            this.highlightShader = Shader.Find("Custom/TransparentOutline");
        }
        
        public void SetDisplayTargetText(bool display)
        {
            this.DisplayTargetText = display;
        }

        public void SetOnlyPickableId(string objectId, bool disableHighlightShaderForObject = false) {
            this.onlyPickableObjectId = objectId;
            this.disableHighlightShaderForObject = disableHighlightShaderForObject;
        }

        public void MouseControls()
		{
            // Interact action for mouse left-click when nothing is picked up
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (this.PhysicsController.WhatAmIHolding() == null && this.PhysicsController.actionComplete)
                {
                    var closestObj = this.highlightedObject;
                    if (closestObj != null)
                    {
                        var actionName = "";
                        if (closestObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup && (onlyPickableObjectId == null || onlyPickableObjectId == closestObj.uniqueID))
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

                if (highlightWhileHolding && this.highlightedObject != null && this.PhysicsController.WhatAmIHolding() != this.highlightedObject.gameObject && this.PhysicsController.actionComplete) {
                     var closestObj = this.highlightedObject;
                    if (closestObj != null)
                    {
                        var actionName = "";
                        if (closestObj.GetComponent<CanOpen_Object>())
                        {
                            actionName = closestObj.GetComponent<CanOpen_Object>().isOpen ? "CloseObject" : "OpenObject";
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
                    // else if (highlightWhileHolding && this.PhysicsController.WhatAmIHolding() == this.highlightedObject) {

                    // }
                }
            }

            // Sets throw bar value
            if (throwEnabled) {
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
            }
            
            // Throw action on left click release
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                if (!pickupState)
                {
                  
                    if (this.PhysicsController.WhatAmIHolding() != null)
                    {
                        var diff = Time.time - this.timerAtPress;
                        var clampedForceTime = Mathf.Min(diff * diff, MaxChargeThrowSeconds);
                        var force = clampedForceTime * MaxThrowForce / MaxChargeThrowSeconds;

                        if (this.PhysicsController.actionComplete && (!this.highlightWhileHolding || (highlightedObject != null && this.PhysicsController.WhatAmIHolding() == highlightedObject.gameObject)))
                        {
                            ServerAction action;
                            if (throwEnabled) {
                                action = new ServerAction
                                {
                                    action = "ThrowObject",
                                    moveMagnitude = force,
                                    forceAction = true
                                };
                            }
                            else {
                                action = new ServerAction
                                {
                                    action = "DropHandObject",
                                    forceAction = true
                                };
                            }
                            this.PhysicsController.ProcessControlCommand(action);
                            this.mouseDownThrow = false;
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
            RaycastHit hit = new RaycastHit();
            var ray = m_Camera.GetComponent<Camera>().ScreenPointToRay(screenPosition);
            int layerMask = LayerMask.GetMask("SimObjVisible"); 
            Physics.Raycast(ray, out hit, this.MinHighlightDistance, layerMask);
            Debug.DrawLine(ray.origin, hit.point, Color.red);

            SimObjPhysics newHighlightedObject = null;
            Shader newPreviousShader = null;

            if (hit.transform != null 
                && hit.transform.tag == "SimObjPhysics"
                && (this.PhysicsController.WhatAmIHolding() == null || this.highlightWhileHolding)
               )
            {
                softHighlight = true;
                var simObj = hit.transform.GetComponent<SimObjPhysics>();
                Func<bool> validObjectLazy = () => { 
                    return (simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup && (this.onlyPickableObjectId == null || this.onlyPickableObjectId == simObj.uniqueID)) ||
                                  simObj.GetComponent<CanOpen_Object>() ||
                                  simObj.GetComponent<CanToggleOnOff>();
                };
                if (simObj != null && validObjectLazy())
                {
                    var withinReach = PhysicsController.FindObjectInVisibleSimObjPhysics(simObj.uniqueID) != null;
                    setTargetText(simObj.name, withinReach);
                    newHighlightedObject = simObj;
                    var mRenderer = newHighlightedObject.GetComponentInChildren<MeshRenderer>();

                    var useHighlightShader = !(disableHighlightShaderForObject && simObj.uniqueID == this.onlyPickableObjectId);
                    
                    if (mRenderer != null && useHighlightShader)
                    {
                        if (this.highlightedObject != newHighlightedObject) {
                            newPreviousShader = mRenderer.material.shader;
                            this.previousRenderQueueValue = mRenderer.material.renderQueue;
                            mRenderer.material.renderQueue = -1;
                            mRenderer.material.shader = this.highlightShader;
                        }  

                        if (withinReach)
                        {
                            softHighlight = true;
                            mRenderer.sharedMaterial.SetFloat("_Outline", this.HighlightParams.WithinReachOutlineThickness);
                            mRenderer.sharedMaterial.SetColor("_OutlineColor", this.HighlightParams.WithinReachOutlineColor);
                        }
                        else if (softHighlight)
                        {
                            softHighlight = false;
                            mRenderer.sharedMaterial.SetFloat("_Outline", this.HighlightParams.SoftOutlineThickness);
                            mRenderer.sharedMaterial.SetColor("_OutlineColor", this.HighlightParams.SoftOutlineColor);
                        }
                    }
                }
            }
            else
            {
                    newHighlightedObject = null;
            }

            if (this.highlightedObject != newHighlightedObject && this.highlightedObject != null) {
                    var mRenderer = this.highlightedObject.GetComponentInChildren<MeshRenderer>();

                    setTargetText("");
                    var useHighlightShader = !(disableHighlightShaderForObject && highlightedObject.uniqueID == this.onlyPickableObjectId);

                    if (mRenderer != null && useHighlightShader)
                    {
                        mRenderer.material.shader = this.previousShader;
                        // TODO unity has a bug for transparent objects they disappear when shader swapping, so we reset the previous shader's render queue value to render it appropiately.
                        mRenderer.material.renderQueue = this.previousRenderQueueValue;
                    }
            }
            
            if (newPreviousShader != null) {
                this.previousShader = newPreviousShader;
            }
           
           
            this.highlightedObject = newHighlightedObject;
        }

        private void setTargetText(string text, bool withinReach = false)
        {
            var eps = 1e-5;
            if (withinReach)
            {
                this.TargetText.color = this.HighlightParams.TextStrongColor;
                this.CrosshairText.text = "( + )";
            }
            else if (Math.Abs(this.TargetText.color.a - this.HighlightParams.TextStrongColor.a) < eps)
            {
                this.TargetText.color = this.HighlightParams.TextFaintColor;
                this.CrosshairText.text = "+";
            }

            if (DisplayTargetText && TargetText != null)
            {
                this.TargetText.text = text;
            }

        }
  
	}

}