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
            //pass in multiple parameters separated by spaces
			string[] splitcommand = command.Split(new string[] { " " }, System.StringSplitOptions.None);

			switch(splitcommand[0])
			{            

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

						if (splitcommand.Length > 1)
						{
							action.moveMagnitude = float.Parse(splitcommand[1]);
						}
						else
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

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

						else
                        action.moveMagnitude = 0.25f;
						
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move left
                case "ml":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveLeft";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

						else
                        action.moveMagnitude = 0.25f;
						
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move right
                case "mr":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveRight";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

						else
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

                    //pickup object, if no specific object passed in, it will pick up the closest interactable simobj in the agent's viewport
				case "pu":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PickupObject";
						if(splitcommand.Length > 1)
						{
							action.objectId = splitcommand[1];
						}

						else
						{
							action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestInteractableObject();
						}

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
						if(splitcommand.Length > 1)
						{
							action.x = float.Parse(splitcommand[1]);
                            action.y = float.Parse(splitcommand[2]);
                            action.z = float.Parse(splitcommand[3]);
                            PhysicsController.ProcessControlCommand(action);
						}

                        break;
                    }  

                    //default the Hand's position and rotation to the starting position and rotation
				case "dh": 
                    {
                        ServerAction action = new ServerAction();
                        action.action = "DefaultAgentHand";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }   

                    //move hand ahead, forward relative to agent's facing
                    //pass in move magnitude or default is 0.25 units
				case "mha":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveHandMagnitude";

						if(splitcommand.Length > 1)
						{
							action.moveMagnitude = float.Parse(splitcommand[1]);
						}

						else
						    action.moveMagnitude = 0.25f;
						
						action.x = 0f;
						action.y = 0f;
						action.z = 1f;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    } 

					//move hand backward. relative to agent's facing
					//pass in move magnitude or default is 0.25 units               
                case "mhb":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandMagnitude";


                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.25f;
						
                        action.x = 0f;
                        action.y = 0f;
                        action.z = -1f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand left, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhl":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandMagnitude";                  

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.25f;
						
                        action.x = -1f;
                        action.y = 0f;
                        action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand right, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhr":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandMagnitude";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.25f;
						
                        action.x = 1f;
                        action.y = 0f;
                        action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand up, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhu":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandMagnitude";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.25f;
						
                        action.x = 0f;
                        action.y = 1f;
                        action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand down, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhd":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandMagnitude";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.25f;
						
                        action.x = 0f;
                        action.y = -1f;
                        action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  
                    
                    //throw object by dropping it and applying force.
                    //default is with strength of 120, can pass in custom magnitude of throw force
                case "throw":
					{
						ServerAction action = new ServerAction();
						action.action = "ThrowObject";

						if (splitcommand.Length > 1)
						{
							action.moveMagnitude = float.Parse(splitcommand[1]);
						}

						else
							action.moveMagnitude = 120f;

						PhysicsController.ProcessControlCommand(action);                  
						break;
					}

				case "open":
					{
						ServerAction action = new ServerAction();
						action.action = "OpenObject";

						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

						PhysicsController.ProcessControlCommand(action);                  

						break;
					}

				case "close":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "CloseObject";

                        if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

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

