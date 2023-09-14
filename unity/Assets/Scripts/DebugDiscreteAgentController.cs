using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class DebugDiscreteAgentController : MonoBehaviour {
        public GameObject InputFieldObj = null;
        public AgentManager AManager = null;
        private InputField inputField;

        [SerializeField] private GameObject InputMode_Text = null;
        // Start is called before the first frame update
        void Start() {
            InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            var Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
            inputField = InputFieldObj.GetComponent<InputField>();

            AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (this.isActiveAndEnabled) {
                Debug_Canvas.GetComponent<Canvas>().enabled = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

        }

        BaseFPSAgentController CurrentActiveController() {
            return AManager.PrimaryAgent;
        }

        public void OnEnable() {
            InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            if (InputMode_Text) {
                InputMode_Text.GetComponent<Text>().text = "Text Input Mode";
            }
        }

        public void OnDisable() {

        }

        void Update() {
            // use these for the Breakable Window demo video
            // if(Input.GetKeyDown(KeyCode.P))
            // {
            //    // print("pickup");
            //     ServerAction action = new ServerAction();
            //     action.action = "PickupObject";
            //     action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
            //     PhysicsController.ProcessControlCommand(action);

            // }

            // if(Input.GetKeyDown(KeyCode.T))
            // {
            //     ServerAction action = new ServerAction();
            //     action.action = "ThrowObject";
            //     action.moveMagnitude = 600f;
            //     PhysicsController.ProcessControlCommand(action);
            // }

            // if(Input.GetKeyDown(KeyCode.U))
            // {
            //     ServerAction action = new ServerAction();
            //     action.action = "MoveHandMagnitude";

            //     action.moveMagnitude = 0.1f;

            //     action.x = 0f;
            //     action.y = 1f;
            //     action.z = 0f;
            //     PhysicsController.ProcessControlCommand(action);
            // }

            // if we press enter, select the input field
            if (CurrentActiveController().ReadyForCommand) {
                if (Input.GetKeyDown(KeyCode.Return)) {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(InputFieldObj);
                }

                if (!inputField.isFocused) {
                    bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                    bool noModifier = (!shiftHeld) && (!altHeld);
                    bool armMoveMode = shiftHeld && !altHeld;
                    bool armRotateMode = shiftHeld && altHeld;

                    bool isArticulated = CurrentActiveController().GetType() == typeof(ArticulatedAgentController);
                    Dictionary<string, object> action = new Dictionary<string, object>();
                    var physicsSimulationParams = new PhysicsSimulationParams() {
                        autoSimulation = false
                    };
                    bool useLimits = true;

                    if (noModifier) {
                        float WalkMagnitude = 0.25f;

                        action["action"] = "";
                        action["physicsSimulationParams"] = physicsSimulationParams;

                        if (isArticulated) {
                            if (Input.GetKeyDown(KeyCode.W)) {
                                action["action"] = "MoveAgent";
                                action["moveMagnitude"] = WalkMagnitude;
                            }

                            if (Input.GetKeyDown(KeyCode.S)) {
                                action["action"] = "MoveAgent";
                                action["moveMagnitude"] = -WalkMagnitude;
                            }

                            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                                action["action"] = "RotateLeft";
                                action["degrees"] = 30f;
                                action["speed"] = 22.5f;
                                action["acceleration"] = 22.5f;
                            }

                            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                                action["action"] = "RotateRight";
                                action["degrees"] = 30f;
                                action["speed"] = 22.5f;
                                action["acceleration"] = 22.5f;
                            }
                        } else {

                            if (Input.GetKeyDown(KeyCode.W)) {
                                action["action"] = "MoveAhead";
                                action["moveMagnitude"] = WalkMagnitude;
                            }

                            if (Input.GetKeyDown(KeyCode.S)) {
                                action["action"] = "MoveBack";
                                action["moveMagnitude"] = WalkMagnitude;
                            }

                            if (Input.GetKeyDown(KeyCode.A)) {
                                action["action"] = "MoveLeft";
                                action["moveMagnitude"] = WalkMagnitude;
                            }

                            if (Input.GetKeyDown(KeyCode.D)) {
                                action["action"] = "MoveRight";
                                action["moveMagnitude"] = WalkMagnitude;
                            }

                            if (Input.GetKeyDown(KeyCode.UpArrow)) {
                                action["action"] = "LookUp";
                            }

                            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                                action["action"] = "LookDown";
                            }

                            if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                                action["action"] = "RotateLeft";
                            }

                            if (Input.GetKeyDown(KeyCode.RightArrow)) {
                                action["action"] = "RotateRight";
                            }
                        }

                        if ((string)action["action"] != "") {
                            if (
                                ((string)action["action"]).Contains("Move")
                                && CurrentActiveController().GetType() == typeof(KinovaArmAgentController)
                            ) {
                                action["returnToStart"] = false;
                            }
                            this.CurrentActiveController().ProcessControlCommand(action);
                        }

                    } else if (armMoveMode) {
                        var actionName = "MoveArmRelative";
                        var localPos = new Vector3(0, 0, 0);
                        float ArmMoveMagnitude = 0.05f;

                        if (isArticulated) {
                            ArmMoveMagnitude = 0.1f;
                            float speed = 0.5f;
                            action["physicsSimulationParams"] = physicsSimulationParams;

                            if (Input.GetKeyDown(KeyCode.W)) {
                                action["action"] = "MoveArm";
                                action["position"] = new Vector3(0f, 0f, ArmMoveMagnitude);
                                action["speed"] = speed;
                            } else if (Input.GetKeyDown(KeyCode.S)) {
                                action["action"] = "MoveArm";
                                action["position"] = new Vector3(0f, 0f, -ArmMoveMagnitude);
                                action["speed"] = speed;
                            } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                                action["action"] = "MoveArmBaseUp";
                                action["distance"] = 0.2f;
                                action["speed"] = speed;
                            } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                                action["action"] = "MoveArmBaseDown";
                                action["distance"] = 0.2f;
                                action["speed"] = speed;
                            } else if (Input.GetKeyDown(KeyCode.Q)) {
                                action["action"] = "RotateWristRelative";
                                action["yaw"] = -30f;
                                action["speed"] = 1000f;
                            } else if (Input.GetKeyDown(KeyCode.E)) {
                                action["action"] = "RotateWristRelative";
                                action["yaw"] = 30f;
                                action["speed"] = 1000f;
                            } else if (Input.GetKeyDown(KeyCode.P)) {
                                action["action"] = "PickupObject";
                                action.Remove("physicsSimulationParams");
                            } else if (Input.GetKeyDown(KeyCode.D)) {
                                action["action"] = "ReleaseObject";
                                action.Remove("physicsSimulationParams");
                            } else {
                                actionName = "";
                            }

                            if (actionName != "") {
                                if (((string) action["action"]).StartsWith("MoveArm")) {
                                    action["useLimits"] = useLimits;
                                }
                                this.CurrentActiveController().ProcessControlCommand(action);
                            }
                        } else {
                            if (Input.GetKeyDown(KeyCode.W)) {
                                localPos.y += ArmMoveMagnitude;
                            } else if (Input.GetKeyDown(KeyCode.S)) {
                                localPos.y -= ArmMoveMagnitude;
                            } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                                localPos.z += ArmMoveMagnitude;
                            } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                                localPos.z -= ArmMoveMagnitude;
                            } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                                localPos.x -= ArmMoveMagnitude;
                            } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                                localPos.x += ArmMoveMagnitude;
                            } else if (Input.GetKeyDown(KeyCode.P)) {
                                actionName = "PickupObject";
                            } else if (Input.GetKeyDown(KeyCode.D)) {
                                actionName = "ReleaseObject";
                            } else {
                                actionName = "";
                            }
                            if (actionName != "") {
                                action["action"] = actionName;
                                if (localPos.magnitude != 0) {
                                    action["offset"] = localPos;
                                    //action["fixedDeltaTime"] = fixedDeltaTime;
                                    action["speed"] = 0.1;
                                    action["returnToStart"] = false;
                                }
                                this.CurrentActiveController().ProcessControlCommand(action);
                            }
                        }
                    } else if (armRotateMode) {
                        var actionName = "RotateWristRelative";
                        float rotateMag = 30f;
                        float pitch = 0f;
                        float yaw = 0f;
                        float roll = 0f;
                        float degrees = 0f;

                        if (Input.GetKeyDown(KeyCode.W)) {
                            roll += rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.S)) {
                            roll -= rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                            pitch += rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                            pitch -= rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
                            yaw -= rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
                            yaw += rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.E)) {
                            actionName = "RotateElbowRelative";
                            degrees += rotateMag;
                        } else if (Input.GetKeyDown(KeyCode.Q)) {
                            // Why Q/E rather than A/D? Because apparently
                            // shift+alt+A is a Unity shortcut and Unity
                            // doesn't provide the ability to disable shortcuts in
                            // play mode despite this being a feature request since 2013.
                            actionName = "RotateElbowRelative";
                            degrees -= rotateMag;
                        } else {
                            actionName = "";
                        }

                        if (actionName != "") {
                            action["action"] = actionName;
                            if (actionName == "RotateWristRelative") {
                                action["pitch"] = pitch;
                                action["yaw"] = yaw;
                                action["roll"] = roll;
                            } else if (actionName == "RotateElbowRelative") {
                                action["degrees"] = degrees;
                            }
                            this.CurrentActiveController().ProcessControlCommand(action);
                        }
                    }
                }
            }
        }
    }
}
