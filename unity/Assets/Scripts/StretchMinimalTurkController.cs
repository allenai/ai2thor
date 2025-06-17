using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using MessagePack.Unity.Extension;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UI;


namespace UnityStandardAssets.Characters.FirstPerson {

    public class StretchMinimalTurkController : MonoBehaviour {
        [SerializeField]
        private float HandMoveMagnitude = 0.1f;
        // public PhysicsRemoteFPSAgentController PhysicsController = null;
        private GameObject InputMode_Text = null;
        private ObjectHighlightController highlightController = null;
        private GameObject throwForceBar = null;
        private bool handMode = false;
        private bool visibleObject = true;
        private bool hidingPhase = false;
        private AgentManager agentManager = null;
        public string onlyPickableObjectId = null;
        public bool disableCollistionWithPickupObject = false;
        public float moveSpeed = 4.0f;
        public float moveDistance = 0.5f;
        public float rotateSpeed = 3.0f;
        public float rotateDegrees = 30.0f;


        public PhysicsRemoteFPSAgentController PhysicsController {
            get { return (PhysicsRemoteFPSAgentController)this.agentManager.GetActiveAgent(); }
        }

        void Start() {
            var Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
            agentManager = GameObject
                .Find("PhysicsSceneManager")
                .GetComponentInChildren<AgentManager>();
            agentManager.SetUpPhysicsController();
            // PhysicsController = (PhysicsRemoteFPSAgentController)agentManager.PrimaryAgent;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug_Canvas.GetComponent<Canvas>().enabled = true;

            highlightController = new ObjectHighlightController(
                PhysicsController,
                PhysicsController.maxVisibleDistance,
                true,
                false,
                0,
                0,
                true
            );
            highlightController.SetDisplayTargetText(false);

            // SpawnObjectToHide("{\"objectType\": \"Plunger\", \"objectVariation\": 1}");
        }

        public void OnEnable() {
            InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            throwForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            var camera = GetComponentInChildren<Camera>();
            camera.fieldOfView = 90.0f;
            // camera.transform.rotation = Quaternion.Euler(30, 0, 0);
            camera.transform.Rotate(30, 0, 0);
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

        public void SetMoveSpeed(string speed) {
            if ( float.TryParse(speed, out float result)) {
                this.moveSpeed = result;
            }
        }

         public void SetRotateSpeed(string speed) {
            if ( float.TryParse(speed, out float result)) {
                this.rotateSpeed = result;
            }
        }

        public void SetMoveDistance(string distance) {
            if ( float.TryParse(distance, out float result)) {
                this.moveDistance = result;
            }
        }

         public void SetRotateDegrees(string degrees) {
            if ( float.TryParse(degrees, out float result)) {
                this.rotateDegrees = result;
            }
        }

        public void Move(float ahead, float right, float speed) {
            Dictionary<string, object> action = new Dictionary<string, object>() {
                ["action"] = "MoveAgent",
                ["returnToStart"] = true,
                ["ahead"] = ahead,
                ["right"] = right,
                ["speed"] = speed,
                ["physicsSimulationParams"] = new PhysicsSimulationParams() { autoSimulation = true}
            };
            PhysicsController.ProcessControlCommand(action);
        }

         public void Rotate(float degrees, float speed) {
            Dictionary<string, object> action = new Dictionary<string, object>() {
                ["action"] = "RotateAgent",
                ["returnToStart"] = true,
                ["degrees"] = degrees,
                ["speed"] = speed,
                ["physicsSimulationParams"] = new PhysicsSimulationParams() { autoSimulation = true}
            };
            PhysicsController.ProcessControlCommand(action);
        }

        void Update() {
            highlightController.UpdateHighlightedObject(Input.mousePosition);
            highlightController.MouseControls();

            if (PhysicsController.ReadyForCommand) {
                handMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!handMode && !hidingPhase) {
                    if (Input.GetKeyDown(KeyCode.W)) {
                        Move(1.0f * moveDistance, 0.0f, moveSpeed);

                    }

                    if (Input.GetKeyDown(KeyCode.S)) {
                        Move(-1.0f * moveDistance, 0.0f, moveSpeed);
                    }

                    if (Input.GetKeyDown(KeyCode.A)) {
                         Move(0.0f, -1.0f * moveDistance, moveSpeed);
                        
                    }

                    if (Input.GetKeyDown(KeyCode.D)) {
                        Move(0.0f, 1.0f * moveDistance, moveSpeed);
                    }

                    if (Input.GetKeyDown(KeyCode.LeftArrow)) //|| Input.GetKeyDown(KeyCode.J))
                    {
                        Rotate(-rotateDegrees, rotateSpeed);
                        // Dictionary<string, object> action = new Dictionary<string, object>();
                        // action["action"] = "RotateLeftSmooth";
                        // action["timeStep"] = 0.4f;
                        // PhysicsController.ProcessControlCommand(action);
                        
                    }

                    if (Input.GetKeyDown(KeyCode.RightArrow)) //|| Input.GetKeyDown(KeyCode.L))
                    {
                        Rotate(rotateDegrees, rotateSpeed);
                        // Dictionary<string, object> action = new Dictionary<string, object>();
                        // action["action"] = "RotateRightSmooth";
                        // action["timeStep"] = 0.4f;
                        // PhysicsController.ProcessControlCommand(action);
                    }
                }
            }
        }
    }
}
