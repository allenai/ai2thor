using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
    public class DiscretePointClickAgentController : MonoBehaviour
    {
        [SerializeField] private float HandMoveMagnitude = 0.1f;
        public PhysicsRemoteFPSAgentController PhysicsController = null;
        private GameObject InputMode_Text = null;
        private ObjectHighlightController highlightController = null;
        private GameObject throwForceBar = null;
        private bool handMode = false;
        void Start() 
        {
            var Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
            PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug_Canvas.GetComponent<Canvas>().enabled = true; 

            highlightController = new ObjectHighlightController(PhysicsController, PhysicsController.maxVisibleDistance, true, false);
        }

        public void OnEnable() {
            InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            throwForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            if (InputMode_Text) {
                InputMode_Text.GetComponent<Text>().text = "Point and Click Mode";
            }
            if (throwForceBar) {
                throwForceBar.SetActive(false);
            }
            // InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            // TODO: move debug input field script from, Input Field and disable here
        }

         public void OnDisable() {
             if (throwForceBar) {
                throwForceBar.SetActive(true);
            }
             // TODO: move debug input field script from, Input Field and enable here
        }
        void Update()
        {
                highlightController.UpdateHighlightedObject(Input.mousePosition);
                highlightController.MouseControls();

                if (PhysicsController.actionComplete) {
                        float WalkMagnitude = 0.25f;
                        if (!handMode) {
                            if(Input.GetKeyDown(KeyCode.W))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveAhead";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.S))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveBack";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.A))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveLeft";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.D))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveRight";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.UpArrow))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "LookUp";
                                PhysicsController.ProcessControlCommand(action); 
                            }

                            if(Input.GetKeyDown(KeyCode.DownArrow))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "LookDown";
                                PhysicsController.ProcessControlCommand(action); 
                            }

                            if(Input.GetKeyDown(KeyCode.LeftArrow) )//|| Input.GetKeyDown(KeyCode.J))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "RotateLeft";
                                PhysicsController.ProcessControlCommand(action); 
                            }

                            if(Input.GetKeyDown(KeyCode.RightArrow) )//|| Input.GetKeyDown(KeyCode.L))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "RotateRight";
                                PhysicsController.ProcessControlCommand(action); 
                            }
                        }

                         if (Input.GetKeyDown(KeyCode.LeftShift)) {
                            handMode = true;
                         }
                         else if (Input.GetKeyUp(KeyCode.LeftShift)){
                            handMode = false;
                         }

                        if (this.PhysicsController.WhatAmIHolding() != null  && handMode)
                        {
                            var actionName = "MoveHandForce";
                            var localPos = new Vector3(0, 0, 0);
                            // Debug.Log(" Key down shift ? " + Input.GetKey(KeyCode.LeftAlt) + " up " + Input.GetKeyDown(KeyCode.UpArrow));
                            if (Input.GetKeyDown(KeyCode.W)) {
                                localPos.z += HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.S)) {
                                localPos.z -= HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                                localPos.y += HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                                localPos.y -= HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                                localPos.x -= HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                                localPos.x += HandMoveMagnitude;
                            }
                            if (actionName != "") {
                                ServerAction action = new ServerAction
                                {
                                    action = "MoveHandForce",
                                    x = localPos.x,
                                    y = localPos.y,
                                    z = localPos.z
                                };
                                this.PhysicsController.ProcessControlCommand(action);
                            }
                        }
            }
        }
    }
}