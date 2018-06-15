using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
	public class DebugInputField : MonoBehaviour
    {
		public GameObject Agent = null;
		public PhysicsRemoteFPSAgentController PhysicsController = null;
		public DiscreteRemoteFPSAgentController PivotController = null;

        // Use this for initialization
        void Start()
        {
			//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
			PhysicsController = Agent.GetComponent<PhysicsRemoteFPSAgentController>();
			PivotController = Agent.GetComponent<DiscreteRemoteFPSAgentController>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Execute(string command)
        {

			string[] splitcommand = command.Split(new string[] { " " }, System.StringSplitOptions.None);

			switch(splitcommand[0])
			{            
				//turn on all physics things, disable all pivot things
				case "physicsenabled":
                    {
                        PhysicsController.enabled = true;
                        PivotController.enabled = false;

                        //PhysicsController.DebugInitialize(Agent.transform.position);
                        break;
                    }
                
                    //turn on all pivot things, disable all physics things
                case "pivotenabled":
                    {
                        PhysicsController.enabled = false;
                        PivotController.enabled = true;
                        break;
                    }

					//turn on all pivot things, disable all physics things
                case "init":
                    {
						ServerAction action = new ServerAction();
      			        PhysicsController.Initialize(action);
                        break;
                    }

                    //move ahead
                case "ma":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveAhead";
                        action.moveMagnitude = 0.25f;
                        PhysicsController.ProcessControlCommand(action);

                        //PhysicsController.CheckIfAgentCanMove(5.0f, 0);
                        break;
                    }

                    //move backward
                case "mb":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveBack";
                        action.moveMagnitude = 0.25f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move left
                case "ml":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveLeft";
                        action.moveMagnitude = 0.25f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move right
                case "mr":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveRight";
                        action.moveMagnitude = 0.25f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //look up
                case "lu":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "LookUp";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //look down
                case "ld":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "LookDown";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //rotate left
                case "rl":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateLeft";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                
                    //rotate right
                case "rr":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateRight";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }   

                    //pickup object
				case "pu":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PickupObject";
                        action.objectId = splitcommand[1];
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    
                    //drop object
				case "dr":
                    {
                        ServerAction action = new ServerAction();
						action.action = "DropHandObject";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }   

                    //rotate object in hand, pass in desired x/y/z rotation
				case "ro":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateHand";
						action.x = float.Parse(splitcommand[1]);
						action.y = float.Parse(splitcommand[2]);
						action.z = float.Parse(splitcommand[3]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  
                   

				default:
                    {

                        //Debug.Log("Invalid Command");
                        break;
                    }
			}


        }
    }
}

