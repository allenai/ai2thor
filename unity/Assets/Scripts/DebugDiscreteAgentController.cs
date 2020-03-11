using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
    public class DebugDiscreteAgentController : MonoBehaviour
    {   
        public GameObject InputFieldObj = null;
        public PhysicsRemoteFPSAgentController PhysicsController = null;
        private InputField inputField;
        public bool continuous = true;
        public bool forceAction = false;
        public float gridSize = 0.1f;
        public float visibilityDistance = 0.4f;
        public Vector3 moveOrPickupObjectDirection;
        public string moveOrPickupObjectId = "";
        public Vector3 receptacleObjectDirection;
        public string receptacleObjectId = "";
        public float rotationIncrement = 45.0f;
        public int horizonIncrement = 30;
        public float pushPullForce = 150.0f;
        public float FlyMagnitude = 1.0f;
        public float WalkMagnitude = 0.2f;

        [SerializeField] private GameObject InputMode_Text = null;
        // Start is called before the first frame update
        void Start() 
        {
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
            action.gridSize = gridSize;
            action.action = "Initialize";
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
                //     action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
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

                //if we press enter, select the input field
                if (PhysicsController.actionComplete) {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(InputFieldObj);
                    }

                
                    if(!inputField.isFocused)
                    {
                        ServerAction action = new ServerAction();
                        action.continuous = continuous;
                        action.forceAction = forceAction;
                        action.gridSize = gridSize;
                        action.visibilityDistance = visibilityDistance;

                        if(Input.GetKeyDown(KeyCode.W))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "FlyAhead";
                                action.moveMagnitude = FlyMagnitude;
                                PhysicsController.ProcessControlCommand(action);
                            }

                            else
                            {
                                action.action = "MoveAhead";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

                        if(Input.GetKeyDown(KeyCode.S))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "FlyBack";
                                action.moveMagnitude = FlyMagnitude;
                                PhysicsController.ProcessControlCommand(action);
                            }

                            else
                            {
                                action.action = "MoveBack";
                                action.moveMagnitude = WalkMagnitude;	
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

                        if(Input.GetKeyDown(KeyCode.A))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "FlyLeft";
                                action.moveMagnitude = FlyMagnitude;
                                PhysicsController.ProcessControlCommand(action);
                            }

                            else
                            {
                                action.action = "MoveLeft";
                                action.moveMagnitude = WalkMagnitude;	
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

                        if(Input.GetKeyDown(KeyCode.D))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "FlyRight";
                                action.moveMagnitude = FlyMagnitude;
                                PhysicsController.ProcessControlCommand(action);
                            }

                            else
                            {
                                action.action = "MoveRight";
                                action.moveMagnitude = WalkMagnitude;	
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

                        if(Input.GetKeyDown(KeyCode.I))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "FlyUp";
                                action.moveMagnitude = FlyMagnitude;
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

                        if(Input.GetKeyDown(KeyCode.K))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "FlyDown";
                                action.moveMagnitude = FlyMagnitude;
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

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

                        if(Input.GetKeyDown(KeyCode.Space))
                        {
                            if(PhysicsController.FlightMode)
                            {
                                action.action = "LaunchDroneObject";
                                action.moveMagnitude = 200f;
                                //action. = new Vector3(0, 1, -1);
                                action.x = 0;
                                action.y = 1;
                                action.z = -1;
                                PhysicsController.ProcessControlCommand(action);
                            }
                        }

                        if (Input.GetKeyDown(KeyCode.Escape)) {
                            action.action = "Pass";
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if (Input.GetKeyDown(KeyCode.Backspace)) {
                            action.action = "Initialize";
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if(Input.GetKeyDown(KeyCode.O))
                        {
                            action.action = "OpenObject";
                            action.moveMagnitude = 1.0f;
                            action.objectDirection = this.receptacleObjectDirection;
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

                        if(Input.GetKeyDown(KeyCode.C))
                        {
                            action.action = "CloseObject";
                            action.moveMagnitude = 1.0f;
                            action.objectDirection = this.receptacleObjectDirection;
                            action.objectId = this.receptacleObjectId;
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if(Input.GetKeyDown(KeyCode.P))
                        {
                            action.action = "PickupObject";
                            action.objectDirection = this.moveOrPickupObjectDirection;
                            action.objectId = this.moveOrPickupObjectId;
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if(Input.GetKeyDown(KeyCode.Z))
                        {
                            action.action = "PutObject";
                            action.objectDirection = this.moveOrPickupObjectDirection;
                            action.objectId = this.moveOrPickupObjectId;
                            action.receptacleObjectDirection = this.receptacleObjectDirection;
                            action.receptacleObjectId = this.receptacleObjectId;
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if(Input.GetKeyDown(KeyCode.X))
                        {
                            action.action = "DropHandObject";
                            action.objectDirection = this.moveOrPickupObjectDirection;
                            action.objectId = this.moveOrPickupObjectId;
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if(Input.GetKeyDown(KeyCode.U))
                        {
                            action.action = this.pushPullForce > 0 ? "PushObject" : "PullObject";
                            action.moveMagnitude = System.Math.Abs(this.pushPullForce);
                            action.objectDirection = this.moveOrPickupObjectDirection;
                            action.objectId = this.moveOrPickupObjectId;
                            PhysicsController.ProcessControlCommand(action);
                        }

                        if(Input.GetKeyDown(KeyCode.R))
                        {
                            action.rotation.y = rotationIncrement;
                            action.horizon = horizonIncrement;

                            action.action = "RotateLook";
                            PhysicsController.ProcessControlCommand(action);
                        }
                    }
            }
        }
    }
}
