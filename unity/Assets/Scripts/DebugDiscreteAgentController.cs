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
                        // float FlyMagnitude = 1.0f;
                        float WalkMagnitude = 0.25f;
                        if(Input.GetKeyDown(KeyCode.W))
                        {
                            ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyAhead";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveAhead";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        if(Input.GetKeyDown(KeyCode.S))
                        {
                            ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyBack";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveBack";
                                action.moveMagnitude = WalkMagnitude;	
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        if(Input.GetKeyDown(KeyCode.A))
                        {
                            ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyLeft";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveLeft";
                                action.moveMagnitude = WalkMagnitude;	
                                PhysicsController.ProcessControlCommand(action);
                            // }
                        }

                        if(Input.GetKeyDown(KeyCode.D))
                        {
                            ServerAction action = new ServerAction();
                            // if(PhysicsController.FlightMode)
                            // {
                            //     action.action = "FlyRight";
                            //     action.moveMagnitude = FlyMagnitude;
                            //     PhysicsController.ProcessControlCommand(action);
                            // }

                            // else
                            // {
                                action.action = "MoveRight";
                                action.moveMagnitude = WalkMagnitude;	
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
                    }
            }
        }
    }
}