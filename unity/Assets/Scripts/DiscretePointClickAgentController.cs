using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class DiscretePointClickAgentController : MonoBehaviour {
        [SerializeField] private float HandMoveMagnitude = 0.1f;
        public PhysicsRemoteFPSAgentController PhysicsController = null;
        private GameObject InputMode_Text = null;
        private ObjectHighlightController highlightController = null;
        private GameObject throwForceBar = null;
        private bool handMode = false;
        void Start() {
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

        private void executeAction(string actionName, float moveMagnitude) {
            Dictionary<string, object> action = new Dictionary<string, object>();
            action["action"] = actionName;
            action["moveMagnitude"] = moveMagnitude;
            PhysicsController.ProcessControlCommand(action);
        }

        private void executeAction(string actionName) {
            Dictionary<string, object> action = new Dictionary<string, object>();
            action["action"] = actionName;
            PhysicsController.ProcessControlCommand(action);
        }

        void Update() {
            highlightController.UpdateHighlightedObject(Input.mousePosition);
            highlightController.MouseControls();

            if (PhysicsController.ReadyForCommand) {
                float WalkMagnitude = 0.25f;
                if (!handMode) {
                    if (Input.GetKeyDown(KeyCode.W)) {
                        executeAction("MoveAhead", WalkMagnitude);
                    }

                    if (Input.GetKeyDown(KeyCode.S)) {
                        executeAction("MoveBack", WalkMagnitude);
                    }

                    if (Input.GetKeyDown(KeyCode.A)) {
                        executeAction("MoveLeft", WalkMagnitude);
                    }

                    if (Input.GetKeyDown(KeyCode.D)) {
                        executeAction("MoveRight", WalkMagnitude);
                    }

                    if (Input.GetKeyDown(KeyCode.UpArrow)) {
                        executeAction("LookUp");
                    }

                    if (Input.GetKeyDown(KeyCode.DownArrow)) {
                        executeAction("LookDown");
                    }

                    if (Input.GetKeyDown(KeyCode.LeftArrow))//|| Input.GetKeyDown(KeyCode.J))
                    {
                        executeAction("RotateLeft");
                    }

                    if (Input.GetKeyDown(KeyCode.RightArrow))//|| Input.GetKeyDown(KeyCode.L))
                    {
                        executeAction("RotateRight");
                    }
                }

                if (Input.GetKeyDown(KeyCode.LeftShift)) {
                    handMode = true;
                } else if (Input.GetKeyUp(KeyCode.LeftShift)) {
                    handMode = false;
                }

                if (this.PhysicsController.WhatAmIHolding() != null && handMode) {
                    var actionName = "MoveHandForce";
                    var localPos = new Vector3(0, 0, 0);
                    // Debug.Log(" Key down shift ? " + Input.GetKey(KeyCode.LeftAlt) + " up " + Input.GetKeyDown(KeyCode.UpArrow));
                    if (Input.GetKeyDown(KeyCode.W)) {
                        localPos.z += HandMoveMagnitude;
                    } else if (Input.GetKeyDown(KeyCode.S)) {
                        localPos.z -= HandMoveMagnitude;
                    } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                        localPos.y += HandMoveMagnitude;
                    } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                        localPos.y -= HandMoveMagnitude;
                    } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                        localPos.x -= HandMoveMagnitude;
                    } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                        localPos.x += HandMoveMagnitude;
                    }
                    if (actionName != "") {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = actionName;
                        action["x"] = localPos.x;
                        action["y"] = localPos.y;
                        action["z"] = localPos.z;

                        this.PhysicsController.ProcessControlCommand(action);
                    }
                }
            }
        }
    }
}
