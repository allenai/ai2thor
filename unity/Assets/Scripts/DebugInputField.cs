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
			Agent = GameObject.Find("FPSController");
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
						    action.moveMagnitude = 0.1f;
						
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
                            action.moveMagnitude = 0.1f;
						
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
                            action.moveMagnitude = 0.1f;
						
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
                            action.moveMagnitude = 0.1f;
						
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
                            action.moveMagnitude = 0.1f;
						
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
                            action.moveMagnitude = 0.1f;
						
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

                    //opens given object the given percent, default is 100% open
                    //open <object ID> percent
				case "open":
					{
						ServerAction action = new ServerAction();
						action.action = "OpenObject";

                        //default open 100%
						if (splitcommand.Length > 1 && splitcommand.Length < 3)
                        {
                            action.objectId = splitcommand[1];
                        }

						//give the open percentage as 3rd param, from 0.0 to 1.0
						if(splitcommand.Length > 2)
						{
							action.objectId = splitcommand[1];
							action.moveMagnitude = float.Parse(splitcommand[2]);
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
                   
				case "contains":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "Contains";

                        if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }


                //*****************************************************************************
                //MASS SCALE ACTIONS HERE
                //*****************************************************************************

                    //get total mass in right scale of MassScale sim obj
				case "rscalemass":
					{
						ServerAction action = new ServerAction();
                        action.action = "MassInRightScale";
                        
						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

						PhysicsController.ProcessControlCommand(action);
						break;
					}

                    //get total mass in left scale of MassScale sim obj
				case "lscalemass":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MassInLeftScale";

						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //get total count of objects in right scale of MassScale sim obj
				case "rscalecount":
                    {
						ServerAction action = new ServerAction();
                        action.action = "CountInRightScale";

						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //get total count of objects in the left scale of MassScale sim obj
				case "lscalecount":
                    {
						ServerAction action = new ServerAction();
                        action.action = "CountInLeftScale";

						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //get list of all sim objects in the right scale of MassScale sim obj
				case "rscaleobjs":
                    {
						ServerAction action = new ServerAction();
                        action.action = "ObjectsInRightScale";

						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //get list of all sim objects in the Left scale of MassScale sim obj
				case "lscaleobjs":
                    {
						ServerAction action = new ServerAction();
                        action.action = "ObjectsInLeftScale";

						if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //*****************************************************************************
                    //START SPAWNER ACTIONS HERE
					//*****************************************************************************

                    //spawn a single object of a single type
				case "spawner_ss":
					{
						//pass in a string name of the object you want to spawn from {bread, tomato, egg, potato, lettuce, apple}
						ServerAction action = new ServerAction();
						action.action = "SpawnerSS";

                        
						if(splitcommand.Length > 1)
						{
							//ID of spawner
							action.objectId = splitcommand[1];

							//type of object to spawn, selected from {bread, tomato, egg, potato, lettuce, apple}
							action.objectType = splitcommand[2];
						}

						PhysicsController.ProcessControlCommand(action);
						break;
					}

                    //spawn a single object of a random type
				case "spawner_sor":
                    {
						
						ServerAction action = new ServerAction();
                        action.action = "SpawnerSOR";

						if (splitcommand.Length > 1)
						{
							//ID of spawner
							action.objectId = splitcommand[1];
						}

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //spawn multiple objects, all of a single type
				case "spawner_ms":
                    {
						ServerAction action = new ServerAction();
                        action.action = "SpawnerMS";

                        //need to pass in the count of objects, the type of object, and the delay 
                        if (splitcommand.Length > 1)
                        {
							//ID of spawner
                            action.objectId = splitcommand[1];

							//count of objects to spawn in
							action.maxNumRepeats = int.Parse(splitcommand[2]);

							//type of object
                            action.objectType = splitcommand[3];

							//delay between spawns
							action.moveMagnitude = float.Parse(splitcommand[4]);
       
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //spawn multiple objects, all of one random type
				case "spawner_mor":
                    {
						ServerAction action = new ServerAction();
						action.action = "SpawnerMOR";

                        //need to pass in the count and the delay
						if (splitcommand.Length > 1)
						{
							//ID of spawner
                            action.objectId = splitcommand[1];
							//count of objects to spawn
							action.maxNumRepeats = int.Parse(splitcommand[2]);
                            //delay between spawns
							action.moveMagnitude = float.Parse(splitcommand[3]);

						}

						PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //spawn multiple obects, each of a random type
				case "spawner_mer":
                    {
					
                        ServerAction action = new ServerAction();
                        action.action = "SpawnerMER";

                        //need to pass in the count and the delay
                        if (splitcommand.Length > 1)
                        {
							//ID of spawner
                            action.objectId = splitcommand[1];
                            
                            //count of objects to spawn
                            action.maxNumRepeats = int.Parse(splitcommand[2]);

                            //delay between spawns
                            action.moveMagnitude = float.Parse(splitcommand[3]);                     
                        }

						PhysicsController.ProcessControlCommand(action);
                        break;
                    }

					//spawn a random number (range) of objects, all of a single type
				case "spawner_rs":
                    {
						ServerAction action = new ServerAction();
						action.action = "SpawnerRS";

                        //pass in a min range, max range, object type, and delay
						if (splitcommand.Length > 1)
                        {
							//ID of spawner
                            action.objectId = splitcommand[1];

							//minimum range of how many objects to spawn
							action.agentCount = int.Parse(splitcommand[2]);

                            //maximum range of how many bjects to spawn
                            action.maxNumRepeats = int.Parse(splitcommand[3]);
                            
							//type of object
                            action.objectType = splitcommand[4];

                            //delay between spawns
                            action.moveMagnitude = float.Parse(splitcommand[5]);
                        }

						PhysicsController.ProcessControlCommand(action);
                        break;
                    }

					//spawn a random number (range) of objects, all of one random type
				case "spawner_ror":
                    {
						ServerAction action = new ServerAction();
                        action.action = "SpawnerROR";

						//pass in a min range, max range, delay
						if (splitcommand.Length > 1)
						{
							//ID of spawner
                            action.objectId = splitcommand[1];

							//minimum range of how many objects to spawn
                            action.agentCount = int.Parse(splitcommand[2]);
                            
                            //maximum range of how many objects to spawn
                            action.maxNumRepeats = int.Parse(splitcommand[3]);
                            
                            //delay between spawns
                            action.moveMagnitude = float.Parse(splitcommand[4]);
						}

						PhysicsController.ProcessControlCommand(action);
                        break;
                    }

					//spawn a random number (range) of objects, each of a random type
				case "spawner_rer":
                    {
						ServerAction action = new ServerAction();
                        action.action = "SpawnerRER";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
							//ID of spawner
                            action.objectId = splitcommand[1];

                            //minimum range of how many objects to spawn
                            action.agentCount = int.Parse(splitcommand[2]);

                            //maximum range of how many objects to spawn
                            action.maxNumRepeats = int.Parse(splitcommand[3]);

                            //delay between spawns
                            action.moveMagnitude = float.Parse(splitcommand[4]);
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

