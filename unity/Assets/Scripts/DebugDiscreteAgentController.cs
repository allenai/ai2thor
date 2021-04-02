using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
    public class DebugDiscreteAgentController : MonoBehaviour
    {   
        public static float MOVE_MAX = 0.1f;

        public GameObject InputFieldObj = null;
        public PhysicsRemoteFPSAgentController PhysicsController = null;
        private InputField inputField;
        public bool continuous = true;
        public bool forceAction = false;
        public float gridSize = 0.1f;
        public float visibilityDistance = 0.4f;
        public Vector2 moveOrPickupObjectImageCoords;
        public string moveOrPickupObjectId = "";
        public Vector2 receptacleObjectImageCoords;
        public string receptacleObjectId = "";
        public float rotationIncrement = 45.0f;
        public float horizonIncrement = 30.0f;
        public float pushPullForce = 150.0f;
        public float FlyMagnitude = 1.0f;
        public float WalkMagnitude = 0.2f;
        public bool consistentColors = false;
        public string newSceneFile = "";
        public bool teleportOnEndHabituation = false;
        public float teleportXPosition;
        public float teleportZPosition;
        public bool rotateOnEndHabituation = false;
        public float teleportYRotate;

        private Dictionary<string, string[]> positionByStep = new Dictionary<string, string[]>();
        private GameObject objectParent = null;

        [SerializeField] private GameObject InputMode_Text = null;
        // Start is called before the first frame update
        void Start() 
        {
            this.objectParent = GameObject.Find("Objects");
            InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            var Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
            inputField = InputFieldObj.GetComponent<InputField>();
            PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (this.isActiveAndEnabled)
            {
				Debug_Canvas.GetComponent<Canvas>().enabled = true;            
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            ServerAction action = new ServerAction();
            action.action = "Initialize";
            action.gridSize = gridSize;
            action.visibilityDistance = visibilityDistance;
            action.consistentColors = consistentColors;
            action.snapToGrid = false; // default to false for MCS
            PhysicsController.ProcessControlCommand(action);
        }

        public void OnEnable() {
            InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            if (InputMode_Text) {
                InputMode_Text.GetComponent<Text>().text = "Text Input Mode";
            }
        }

        public void OnDisable() {

        }
        
        void Update()
        {
                //use these for the Breakable Window demo video
                // if(Input.GetKeyDown(KeyCode.P))
                // {
                //    // print("pickup");
                //     ServerAction action = new ServerAction();
                //     action.action = "PickupObject";
                //     action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
                //     PhysicsController.ProcessControlCommand(action);
                            
                // }

                // MCS:
                // Left mouse click populates moveOrPickupObjectImageCoords with
                // screen point vector coordinates, left mouse click + left shift key
                // populates receptacleObjectImageCoords.
                if (Input.GetMouseButtonDown(0)) {
                    Vector2 screenPtToPixels = new Vector2(Input.mousePosition.x, (Screen.height - Input.mousePosition.y));

                    // Normally, (0,0) for pixels is the top left, but for Unity screen points, (0,0) is the
                    // bottom left.
                    Debug.Log("MCS: Screen Point Clicked: " + Input.mousePosition.ToString());
                    Debug.Log("MCS: Screen Point as Image Pixel Coords: " + screenPtToPixels.ToString());
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                        receptacleObjectImageCoords.x = Input.mousePosition.x;
                        receptacleObjectImageCoords.y = Input.mousePosition.y;
                    } else {
                        moveOrPickupObjectImageCoords.x = Input.mousePosition.x;
                        moveOrPickupObjectImageCoords.y = Input.mousePosition.y;
                    }
                }

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

                //if we press enter, select the input field
                if (PhysicsController.ReadyForCommand) {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(InputFieldObj);
                    }

                
                    if(!inputField.isFocused)
                    {
                        ServerAction action = new ServerAction();
                        //[REVIEW] was continous used anywhere? Does not seem like it.
                        //action.continuous = continuous;
                        action.forceAction = forceAction;
                        action.gridSize = gridSize;
                        action.visibilityDistance = visibilityDistance;

                        if(Input.GetKeyDown(KeyCode.W))
                        {
                            //ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyAhead";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveAhead";
                                action.moveMagnitude = Mathf.Min(WalkMagnitude, DebugDiscreteAgentController.MOVE_MAX);
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        if(Input.GetKeyDown(KeyCode.S))
                        {
                            //ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyBack";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveBack";
                                action.moveMagnitude = Mathf.Min(WalkMagnitude, DebugDiscreteAgentController.MOVE_MAX);
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        if(Input.GetKeyDown(KeyCode.A))
                        {
                            //ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyLeft";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveLeft";
                                action.moveMagnitude = Mathf.Min(WalkMagnitude, DebugDiscreteAgentController.MOVE_MAX);
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        if(Input.GetKeyDown(KeyCode.D))
                        {
                            //ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyRight";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveRight";
                                action.moveMagnitude = Mathf.Min(WalkMagnitude, DebugDiscreteAgentController.MOVE_MAX);
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        // if(Input.GetKeyDown(KeyCode.I))
                        // {
                        //     if(PhysicsController.FlightMode)
                        //     {
                        //         ServerAction action = new ServerAction();
                        //         action.action = "FlyUp";
                        //         action.moveMagnitude = FlyMagnitude;
                        //         PhysicsController.ProcessControlCommand(action);
                        //     }
                        // }

                        // if(Input.GetKeyDown(KeyCode.K))
                        // {
                        //     if(PhysicsController.FlightMode)
                        //     {
                        //         ServerAction action = new ServerAction();
                        //         action.action = "FlyDown";
                        //         action.moveMagnitude = FlyMagnitude;
                        //         PhysicsController.ProcessControlCommand(action);
                        //     }
                        // }

                        if(Input.GetKeyDown(KeyCode.UpArrow))
                        {
                            action.action = "LookUp";
                            PhysicsController.ProcessControlCommand(action); 
                        }

                        if(Input.GetKeyDown(KeyCode.DownArrow))
                        {
                            action.action = "LookDown";
                            PhysicsController.ProcessControlCommand(action); 
                        }

                        if(Input.GetKeyDown(KeyCode.LeftArrow) )//|| Input.GetKeyDown(KeyCode.J))
                        {
                            action.action = "RotateLeft";
                            PhysicsController.ProcessControlCommand(action); 
                        }

                        if(Input.GetKeyDown(KeyCode.RightArrow) )//|| Input.GetKeyDown(KeyCode.L))
                        {
                            action.action = "RotateRight";
                            PhysicsController.ProcessControlCommand(action); 
                        }

                    // if(Input.GetKeyDown(KeyCode.Space))
                    // {
                    //     if(PhysicsController.FlightMode)
                    //     {
                    //         ServerAction action = new ServerAction();
                    //         action.action = "LaunchDroneObject";
                    //         action.moveMagnitude = 200f;
                    //         //action. = new Vector3(0, 1, -1);
                    //         action.x = 0;
                    //         action.y = 1;
                    //         action.z = -1;
                    //         PhysicsController.ProcessControlCommand(action);
                    //     }
                    // }

                    // if(Input.GetKeyDown(KeyCode.O))
                    // {
                    //     if(PhysicsController.FlightMode)
                    //     {
                    //         ServerAction action = new ServerAction();
                    //         action.action = "CheckDroneCaught";
                    //         PhysicsController.ProcessControlCommand(action);
                    //     }
                    // }

                    // MCS Additions
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        action.action = "Pass";
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        action.action = "Initialize";
                        if (!this.newSceneFile.Equals(""))
                        {
                            action.sceneConfig = MCSMain.LoadCurrentSceneFromFile(this.newSceneFile);
                        }
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.H))
                    {
                        if(teleportOnEndHabituation) {
                            action.teleportPosition = new Vector3(
                                teleportXPosition,
                                MCSController.STANDING_POSITION_Y,
                                teleportZPosition);
                        }

                        if (rotateOnEndHabituation) {
                            action.teleportRotation = new Vector3(0, teleportYRotate, 0);
                        }

                        action.action = "EndHabituation";
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.O))
                    {
                        action.action = "OpenObject";
                        action.moveMagnitude = 1.0f;
                        action.objectImageCoords = this.receptacleObjectImageCoords;
                        action.objectId = this.receptacleObjectId;
                        PhysicsController.ProcessControlCommand(action);
                        /*
                        if(PhysicsController.FlightMode)
                        {
                            action.action = "CheckDroneCaught";
                            PhysicsController.ProcessControlCommand(action);
                        }
                        */
                    }

                    if (Input.GetKeyDown(KeyCode.C))
                    {
                        action.action = "CloseObject";
                        action.moveMagnitude = 1.0f;
                        action.objectImageCoords = this.receptacleObjectImageCoords;
                        action.objectId = this.receptacleObjectId;
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        action.action = "PickupObject";
                        action.objectImageCoords = this.moveOrPickupObjectImageCoords;
                        action.objectId = this.moveOrPickupObjectId;
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.Z))
                    {
                        action.action = "PutObject";
                        action.objectImageCoords = this.moveOrPickupObjectImageCoords;
                        action.objectId = this.moveOrPickupObjectId;
                        action.receptacleObjectImageCoords = this.receptacleObjectImageCoords;
                        action.receptacleObjectId = this.receptacleObjectId;
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.X))
                    {
                        action.action = "DropHandObject";
                        action.objectImageCoords = this.moveOrPickupObjectImageCoords;
                        action.objectId = this.moveOrPickupObjectId;
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.T))
                    {
                        action.objectId = moveOrPickupObjectId;

                        action.action = "ThrowObject";
                        action.objectImageCoords = moveOrPickupObjectImageCoords;
                        action.moveMagnitude = pushPullForce;
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.U))
                    {
                        action.action = this.pushPullForce > 0 ? "PushObject" : "PullObject";
                        action.moveMagnitude = System.Math.Abs(this.pushPullForce);
                        action.objectImageCoords = this.moveOrPickupObjectImageCoords;
                        action.objectId = this.moveOrPickupObjectId;
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        action.rotation.y = rotationIncrement;
                        action.horizon = horizonIncrement;

                        action.action = "RotateLook";
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        action.action = "Crawl";
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        action.action = "Stand";
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.L))
                    {
                        action.action = "LieDown";
                        PhysicsController.ProcessControlCommand(action);
                    }

                    if (Input.GetKeyDown(KeyCode.Backslash))
                    {
                        foreach (Transform child in this.objectParent.transform)
                        {
                            this.positionByStep[child.name] = new string[100];
                        }
                        action.action = "Pass";
                        StartCoroutine(PassThenRecordPosition(action, 0));
                    }
                }
            }
        }

        IEnumerator PassThenRecordPosition(ServerAction action, int i) {
            PhysicsController.ProcessControlCommand(action);
            yield return 0;
            foreach (Transform child in this.objectParent.transform) {
                if (child.gameObject.activeSelf) {
                    this.positionByStep[child.name][i] = "" + Math.Round(child.position.x, 3);
                    //this.positionByStep[child.name][i] = "" + Math.Round(child.position.x, 3) + "," + Math.Round(child.position.y, 3);
                    //this.positionByStep[child.name][i] = "" + Math.Round(child.position.x, 3) + "," + Math.Round(child.position.z, 3);
                }
            }
            if (i < 99) {
                StartCoroutine(PassThenRecordPosition(action, i + 1));
            }
            else {
                foreach (Transform child in this.objectParent.transform) {
                    Debug.Log("POSITION BY STEP " + child.name + "\n" + String.Join("\n", this.positionByStep[child.name]));
                }
            }
        }
    }
}
