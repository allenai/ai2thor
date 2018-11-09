using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
	public class DebugInputField : MonoBehaviour
    {
		public GameObject Agent = null;
		public PhysicsRemoteFPSAgentController PhysicsController = null;
        public AgentManager AManager = null;
		public DiscreteRemoteFPSAgentController PivotController = null;

        private InputField debugfield;
        private DebugFPSAgentController dfac;

        // Use this for initialization
        void Start()
        {
			//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
			Agent = GameObject.Find("FPSController");
			PhysicsController = Agent.GetComponent<PhysicsRemoteFPSAgentController>();
            AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
            #if UNITY_EDITOR
            PhysicsController.GetComponent<DebugFPSAgentController>().enabled = true;
            #endif
			PivotController = Agent.GetComponent<DiscreteRemoteFPSAgentController>();
            debugfield = gameObject.GetComponent<InputField>();
            dfac = Agent.GetComponent<DebugFPSAgentController>();
        }

        // Update is called once per frame
        void Update()
        {
            if(!debugfield.isFocused && dfac.TextInputMode)
            {
                if(Input.GetKeyDown(KeyCode.W))
                {
                    ServerAction action = new ServerAction();
                    action.action = "MoveAhead";
                    action.moveMagnitude = 0.25f;		
                    PhysicsController.ProcessControlCommand(action);
                }

                if(Input.GetKeyDown(KeyCode.S))
                {
                    ServerAction action = new ServerAction();
                    action.action = "MoveBack";
                    action.moveMagnitude = 0.25f;		
                    PhysicsController.ProcessControlCommand(action);  
                }

                if(Input.GetKeyDown(KeyCode.A))
                {
                    ServerAction action = new ServerAction();
                    action.action = "MoveLeft";
                    action.moveMagnitude = 0.25f;		
                    PhysicsController.ProcessControlCommand(action);  
                }

                if(Input.GetKeyDown(KeyCode.D))
                {
                    ServerAction action = new ServerAction();
                    action.action = "MoveRight";
                    action.moveMagnitude = 0.25f;		
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

                if(Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    ServerAction action = new ServerAction();
                    action.action = "RotateLeft";
                    PhysicsController.ProcessControlCommand(action); 
                }

                if(Input.GetKeyDown(KeyCode.RightArrow))
                {
                    ServerAction action = new ServerAction();
                    action.action = "RotateRight";
                    PhysicsController.ProcessControlCommand(action); 
                }
            }
        }

        public void Execute(string command)
        {
            //pass in multiple parameters separated by spaces
			string[] splitcommand = command.Split(new string[] { " " }, System.StringSplitOptions.None);

			switch(splitcommand[0])
			{            

					//turn off all pivot things, enable all physics things
                case "init":
                    {
						ServerAction action = new ServerAction();

                        //if you want to use smaller grid size step increments, initialize with a smaller/larger gridsize here
                        //by default the gridsize is 0.25, so only moving in increments of .25 will work
                        //so the MoveAhead action will only take, by default, 0.25, .5, .75 etc magnitude with the default
                        //grid size!
						if (splitcommand.Length == 2 )
                        {
							action.gridSize = float.Parse(splitcommand[1]);
                        } else if (splitcommand.Length == 3)
                        {
							action.gridSize = float.Parse(splitcommand[1]);
                            action.agentCount = int.Parse(splitcommand[2]);
                        } else if (splitcommand.Length == 4) {
                            action.gridSize = float.Parse(splitcommand[1]);
                            action.agentCount = int.Parse(splitcommand[2]);
                            action.makeAgentsVisible = int.Parse(splitcommand[3]) == 1;
                        }

                        action.renderNormalsImage = true;
                        action.renderDepthImage = true;
                        action.renderClassImage = true;
                        action.renderObjectImage = true;

						PhysicsController.actionComplete = false;
                        action.action = "Initialize";
                        AManager.Initialize(action);
                        // AgentManager am = PhysicsController.gameObject.FindObjectsOfType<AgentManager>()[0];
                        // Debug.Log("Physics scene manager = ...");
                        // Debug.Log(physicsSceneManager);
                        // AgentManager am = physicsSceneManager.GetComponent<AgentManager>();
                        // Debug.Log(am);
      			        // am.Initialize(action);
                        break;
                    }
                case "to":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "TeleportObject";
                        action.objectId = splitcommand[1];
                        action.x = float.Parse(splitcommand[2]);
                        action.y = float.Parse(splitcommand[3]);
                        action.z = float.Parse(splitcommand[4]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "daoot":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "DisableAllObjectsOfType";
                        action.objectId = splitcommand[1];
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "roco":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RandomlyOpenCloseObjects";
                        action.randomSeed = (new System.Random()).Next(1, 1000000);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "crouch":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "Crouch";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "stand":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "Stand";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

				case "spawn":
                    {
                        ServerAction action = new ServerAction();
                        Debug.Log(action.objectVariation);
                        int objectVariation = 0;
                        if (splitcommand.Length == 2)
                        {
                            action.objectType = splitcommand[1];
                        }
                        else if (splitcommand.Length == 3)
                        {
                            action.objectType = splitcommand[1];
                            objectVariation = int.Parse(splitcommand[2]);
                        }

                        else
                        {
                            action.objectType = "Tomato";//default to spawn debug tomato

                        }
                        action.action = "CreateObject";
                        action.randomizeObjectAppearance = false;//pick randomly from available or not?
                        action.objectVariation = objectVariation;//if random false, which version of the object to spawn? (there are only 3 of each type atm)

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                case "spawnat":
                    {
                        ServerAction action = new ServerAction();

                        if (splitcommand.Length > 1)
                        {
                            action.objectType = splitcommand[1];
                            action.position = new Vector3(float.Parse(splitcommand[2]), float.Parse(splitcommand[3]), float.Parse(splitcommand[4]));
                            action.rotation = new Vector3(float.Parse(splitcommand[5]), float.Parse(splitcommand[6]), float.Parse(splitcommand[7]));
                            //action.rotation?
                        }

                        //default to zeroed out rotation tomato
                        else
                        {
                            action.objectType = "Tomato";//default to spawn debug tomato
                            action.position = Vector3.zero;
                            action.rotation = Vector3.zero;
                        }
                        action.action = "CreateObjectAtLocation";

                        action.randomizeObjectAppearance = false;//pick randomly from available or not?                  
                        action.objectVariation = 1; //if random false, which version of the object to spawn? (there are only 3 of each type atm)

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "rspawnfloor": 
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RandomlyCreateAndPlaceObjectOnFloor";
                        action.objectType = splitcommand[1];
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "spawnfloor": 
                    {
                        ServerAction action = new ServerAction();
                        int objectVariation = 1;
                        action.objectType = splitcommand[1];
                        action.x = float.Parse(splitcommand[2]);
                        action.z = float.Parse(splitcommand[3]);
                        
                        action.action = "CreateObjectOnFloor";
                        action.objectVariation = objectVariation;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "rhs":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RandomizeHideSeekObjects";
                        action.removeProb = float.Parse(splitcommand[1]);
                        action.randomSeed = int.Parse(splitcommand[2]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "cts":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeTimeScale";
                        action.timeScale = float.Parse(splitcommand[1]);
                        PhysicsController.ProcessControlCommand(action);
                        Debug.Log(PhysicsController.reachablePositions.Length);
                        break;
                    }

                case "grp":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "GetReachablePositions";
                        PhysicsController.ProcessControlCommand(action);
                        Debug.Log(PhysicsController.reachablePositions.Length);
                        break;
                    }

                case "csw":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "CoverSurfacesWith";
                        // int objectVariation = 1;
						// action.objectVariation = objectVariation;

						if (splitcommand.Length == 2)
                        {
							action.objectType = splitcommand[1];
                        }
                        else if (splitcommand.Length == 3)
                        {
							action.objectType = splitcommand[1];
                            action.objectVariation = int.Parse(splitcommand[2]);
                        }
						else
						{
							action.objectType = "Tomato"; //default to spawn debug tomato

						}
                        action.x = 0.3f;
                        action.z = 0.3f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fov":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeFOV";
                        action.fov = float.Parse(splitcommand[1]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "map":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ToggleMapView";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                // Force open object
                case "foo":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "OpenObject";
                        action.forceAction = true;
                        action.objectId = splitcommand[1];
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                // Force close object
                case "fco":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "CloseObject";
                        action.forceAction = true;
                        action.objectId = splitcommand[1];
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                
                // Close visible objects
                case "cvo":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "CloseVisibleObjects";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                // Force open object at location
                case "oal":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "OpenObjectAtLocation";
                        action.x = float.Parse(splitcommand[1]);
                        action.y = float.Parse(splitcommand[2]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                // Force pickup object
                case "fpu":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PickupObject";
                        action.objectId = splitcommand[1];
                        action.forceAction = true;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                // Get objects in box
                case "oib":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ObjectsInBox";
                        action.x = float.Parse(splitcommand[1]);
                        action.z = float.Parse(splitcommand[2]);
                        PhysicsController.ProcessControlCommand(action);
                        foreach (string s in PhysicsController.objectIdsInBox) {
                            Debug.Log(s);
                        }
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

						else {
                            action.moveMagnitude = 0.25f;
                        }
						
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
							action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
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

                // force drop object
				case "fdr":
                    {
                        ServerAction action = new ServerAction();
						action.action = "DropHandObject";
                        action.forceAction = true;
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
						else if(splitcommand.Length > 2)
						{
							action.objectId = splitcommand[1];
							action.moveMagnitude = float.Parse(splitcommand[2]);
						}

						else
						{
                           action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleOpenableObject();
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
                  
                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleOpenableObject();
                        }

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }
                   
                    //pass in object id of a receptacle, and this will report any other sim objects inside of it
                    //this works for cabinets, drawers, countertops, tabletops, etc.
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


                        if (splitcommand.Length > 1)
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
                        ServerAction action = new ServerAction();
                        action.action = splitcommand[0];
                        if (splitcommand.Length == 2) {
                            action.objectId = splitcommand[1];
                        } else if (splitcommand.Length == 3) {
                            action.x = float.Parse(splitcommand[1]);
                            action.z = float.Parse(splitcommand[2]);
                        } else if (splitcommand.Length == 4) {
                            action.x = float.Parse(splitcommand[1]);
                            action.y = float.Parse(splitcommand[2]);
                            action.z = float.Parse(splitcommand[3]);
                        }
                        PhysicsController.ProcessControlCommand(action);      
                        //Debug.Log("Invalid Command");
                        break;
                    }
			}

			StartCoroutine(CheckIfactionCompleteWasSetToTrueAfterWaitingALittleBit(splitcommand[0]));

        }

		IEnumerator CheckIfactionCompleteWasSetToTrueAfterWaitingALittleBit(string s)
		{
			yield return new WaitForSeconds(0.5f);
			if (!PhysicsController.actionComplete)
            {
                Debug.LogError("Physics controller does not have actionComplete set to true after :" + s);
				yield return null;
            }
		}


    }
}

