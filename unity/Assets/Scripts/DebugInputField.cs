﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace UnityStandardAssets.Characters.FirstPerson
{
	public class DebugInputField : MonoBehaviour
    {
		public GameObject Agent = null;
		public PhysicsRemoteFPSAgentController PhysicsController = null;
        public AgentManager AManager = null;

        private ControlMode controlMode;

        private Dictionary<KeyCode, ControlMode> debugKeyToController = new Dictionary<KeyCode, ControlMode>{
            {KeyCode.Alpha1, ControlMode.DEBUG_TEXT_INPUT},
            {KeyCode.BackQuote, ControlMode.FPS},
            {KeyCode.Alpha2, ControlMode.DISCRETE_POINT_CLICK}
        };

        private bool setEnabledControlComponent(ControlMode mode, bool enabled) {
            Type componentType;
            var success = PlayerControllers.controlModeToComponent.TryGetValue(mode, out componentType);
            if (success) {
                var previousComponent = Agent.GetComponent(componentType) as MonoBehaviour;
                if (previousComponent != null) {
                    previousComponent.enabled = enabled;
                }
                else {
                    success = false;
                }
            }
            return success;
        }

        public void setControlMode(ControlMode mode) {
            setEnabledControlComponent(controlMode, false);
            controlMode = mode;
            setEnabledControlComponent(controlMode, true);
        } 

        // Use this for initialization
        void Start()
        {
            #if UNITY_EDITOR || UNITY_WEBGL
                Debug.Log("Unity editor");
                this.InitializeUserControl();

            #endif
        }

        void SelectPlayerControl() {
            #if UNITY_EDITOR
                Debug.Log("Editor control");
                setControlMode(ControlMode.DEBUG_TEXT_INPUT);
            #endif
            #if UNITY_WEBGL
                Debug.Log("Webgl");
                setControlMode(ControlMode.FPS);
                PhysicsController.GetComponent<JavaScriptInterface>().enabled = true;
            #endif
            #if TURK_TASK
                Debug.Log("TURK");
                setControlMode(ControlMode.DISCRETE_POINT_CLICK);
            #endif
        }

        void InitializeUserControl()
        {
            GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
            PhysicsController = fpsController.GetComponent<PhysicsRemoteFPSAgentController>();
            Agent = PhysicsController.gameObject;
            AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
            
           SelectPlayerControl();

           #if !UNITY_EDITOR
               HideHUD();
           #endif
        }

        // Update is called once per frame
        void Update()
        {
            #if UNITY_EDITOR
                foreach (KeyValuePair<KeyCode, ControlMode> entry in debugKeyToController) {
                    if (Input.GetKeyDown(entry.Key)) {
                        if (controlMode != entry.Value) {
                            setControlMode(entry.Value);
                            break;
                        }
                    }
                }
            #endif
        }

        public void HideHUD()
        {
            var InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            if (InputMode_Text != null) {
                InputMode_Text.SetActive(false);
            }
            var InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            InputFieldObj.SetActive(false);
            var background = GameObject.Find("DebugCanvasPhysics/InputModeText_Background");
            background.SetActive(false);
        }

        public void Execute(string command)
        {
            if (!PhysicsController.actionComplete) {
                Debug.Log("Cannot execute command while last action has not completed.");
            }

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

                        action.rotateStepDegrees = 45;
                        action.agentType = "stochastic";

                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderClassImage = true;
                        // action.renderObjectImage = true;
                        // action.renderFlowImage = true;

                        //action.continuous = true;//turn on continuous to test multiple emit frames after a single action

						PhysicsController.actionComplete = false;
                        action.ssao = "default";
                        //action.snapToGrid = true;
                        action.makeAgentsVisible = false;
                
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

                case "rad":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SetAgentRadius";
                        action.agentRadius = 0.35f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }   

                case "crazydiamond":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MakeObjectsOfTypeUnbreakable";
                        action.objectType = "Egg";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }                   

                case "pp":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PausePhysicsAutoSim";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "ap":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "AdvancePhysicsStep";
                        action.timeStep = 0.02f; //max 0.05, min 0.01
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    
                case "up":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "UnpausePhysicsAutoSim";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    
                case "its":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "InitializeTableSetting";
                        if (splitcommand.Length > 1) {
                            action.objectVariation = int.Parse(splitcommand[1]);
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "pfrat":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "PlaceFixedReceptacleAtLocation";
                        if (splitcommand.Length > 1) {
                            action.objectVariation = int.Parse(splitcommand[1]);
                            action.x = float.Parse(splitcommand[2]);
                            action.y = float.Parse(splitcommand[3]);
                            action.z = float.Parse(splitcommand[4]);
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "pbwal":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "PlaceBookWallAtLocation";
                        if (splitcommand.Length > 1) {
                            action.objectVariation = int.Parse(splitcommand[1]);
                            action.x = float.Parse(splitcommand[2]);
                            action.y = float.Parse(splitcommand[3]);
                            action.z = float.Parse(splitcommand[4]);
                            action.rotation = new Vector3(0f, float.Parse(splitcommand[5]), 0f);
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //random toggle state of all objects
                case "rts":
                    {
                        ServerAction action = new ServerAction();

                        action.randomSeed = 0;
                        action.action = "RandomToggleStateOfAllObjects";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "rtss":
                    {
                        ServerAction action = new ServerAction();

                        action.randomSeed = 0;
                        action.StateChange = "CanOpen";
                        action.action = "RandomToggleSpecificState";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "l":
                {
                    ServerAction action = new ServerAction();
                    action.action = "ChangeLightSet";
                    if(splitcommand.Length == 2)
                    {
                        action.objectVariation = int.Parse(splitcommand[1]);
                    }

                    PhysicsController.ProcessControlCommand(action);
                    break;
                }

                //set state of all objects that have a state
                case "ssa":
                {
                    ServerAction action = new ServerAction();

                    action.StateChange = "CanBeDirty";
                    action.forceAction = true;
                    action.action = "SetStateOfAllObjects";

                     if (splitcommand.Length > 1) 
                     {
                        if(splitcommand[1] == "t")
                        {
                            action.forceAction = true;
                        }

                        if(splitcommand[1] == "f")
                        {
                            action.forceAction = false;
                        }
                     }
                    PhysicsController.ProcessControlCommand(action);
                        
                    break;
                }

                case "initsynth":
                    {
						ServerAction action = new ServerAction();

                        action.renderNormalsImage = true;
                        action.renderDepthImage = true;
                        action.renderClassImage = true;
                        action.renderObjectImage = true;
                        action.renderFlowImage = true;

						PhysicsController.actionComplete = false;
                        action.ssao = "default";

                        action.action = "Initialize";
                        AManager.Initialize(action);
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

                case "remove":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RemoveFromScene";
                        
                        if(splitcommand.Length == 2)
                        {
                            action.objectId = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "putr":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PutObject";
                        action.receptacleObjectId = PhysicsController.UniqueIDOfClosestReceptacleObject();
                        action.randomSeed = int.Parse(splitcommand[1]);
                            
                        //set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action.placeStationary = false; 

                        //set this true to ignore Placement Restrictions
                        action.forceAction = true;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "put":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PutObject";
                        
                        if(splitcommand.Length == 2)
                        {
                            action.receptacleObjectId = splitcommand[1];
                        }

                        else
                            action.receptacleObjectId = PhysicsController.UniqueIDOfClosestReceptacleObject();
                            
                        //set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action.placeStationary = true; 

                        //set this true to ignore Placement Restrictions
                        action.forceAction = true;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //put an object down with stationary false
                case "putf":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PutObject";
                        
                        if(splitcommand.Length == 2)
                        {
                            action.receptacleObjectId = splitcommand[1];
                        }

                        else
                            action.receptacleObjectId = PhysicsController.UniqueIDOfClosestReceptacleObject();
                            
                        //set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action.placeStationary = false; 

                        //set this true to ignore Placement Restrictions
                        action.forceAction = true;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //make all pickupable objects kinematic false so that they will react to collisions. Otherwise, some objects might be defaulted to kinematic true, or
                //if they were placed with placeStationary true, then they will not interact with outside collisions immediately.
                case "kinematicfalse":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MakeAllPickupableObjectsMoveable";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //set forceVisible to true if you want objects to not spawn inside receptacles and only out in the open
                //set forceAction to true to spawn with kinematic = true to more closely resemble pivot functionality
                case "irs":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "InitialRandomSpawn";

                        //give me a seed
                        if(splitcommand.Length == 2)
                        {
                            action.randomSeed = int.Parse(splitcommand[1]);
                            action.forceVisible = false;
                            action.numPlacementAttempts = 5;
                        }

                        //should objects be spawned only in immediately visible areas?
                        else if(splitcommand.Length == 3)
                        {
                            action.randomSeed = int.Parse(splitcommand[1]);

                            if(splitcommand[2] == "t") 
                            action.forceVisible = true;

                            if(splitcommand[2] == "f") 
                            action.forceVisible = false;
                        }

                        else if(splitcommand.Length == 4)
                        {
                            action.randomSeed = int.Parse(splitcommand[1]);

                            if(splitcommand[2] == "t") 
                            action.forceVisible = true;

                            if(splitcommand[2] == "f") 
                            action.forceVisible = false;

                            action.numPlacementAttempts = int.Parse(splitcommand[3]);
                        }

                        else
                        {
                            action.randomSeed = 0;
                            action.forceVisible = false;//true;
                            action.numPlacementAttempts = 5;
                        }

                        action.placeStationary = true;//set to false to spawn with kinematic = false, set to true to spawn everything kinematic true and they won't roll around
                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }  

				case "spawn":
                    {
                        ServerAction action = new ServerAction();
                        //Debug.Log(action.objectVariation);
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

                case "neutral":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeAgentFaceToNeutral";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "happy":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeAgentFaceToHappy";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "mad":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeAgentFaceToMad";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "supermad":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeAgentFaceToSuperMad";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "thas":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ToggleHideAndSeekObjects";

                        if(splitcommand.Length == 2)
                        {
                            if(splitcommand[1] == "t") 
                            action.forceVisible = true;

                            if(splitcommand[1] == "f") 
                            action.forceVisible = false;
                        }

                        else
                        {
                            action.forceVisible = false;
                        }

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
                case "rspawnlifted":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RandomlyCreateLiftedFurniture";
                        action.objectType = "Television";
                        action.objectVariation = 1;
                        action.y = 1.3f;
                        action.z = 1;
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

                case "fu":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FlyUp";

                        action.moveMagnitude = 2f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fd":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FlyDown";

                        action.moveMagnitude = 2f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fa":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FlyAhead";

                        action.moveMagnitude = 2f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "fl":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FlyLeft";

                        action.moveMagnitude = 2f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fr":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FlyRight";

                        action.moveMagnitude = 2f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fb":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FlyBack";

                        action.moveMagnitude = 2f;
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
                
                case "slice":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SliceObject";
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
                    
                case "break":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "BreakObject";
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
                
                case "dirtyobject":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "DirtyObject";
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

                case "cleanobject":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "CleanObject";
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

                case "fillwater":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FillObjectWithLiquid";
						if(splitcommand.Length > 1)
						{
							action.objectId = splitcommand[1];
						}

                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
                        }

                        action.fillLiquid = "water";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fillcoffee":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FillObjectWithLiquid";
						if(splitcommand.Length > 1)
						{
							action.objectId = splitcommand[1];
						}

                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
                        }

                        action.fillLiquid = "coffee";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                // case "fillsoap":
                //     {
                //         ServerAction action = new ServerAction();
                //         action.action = "FillObjectWithLiquid";
				// 		if(splitcommand.Length > 1)
				// 		{
				// 			action.objectId = splitcommand[1];
				// 		}

                //         else
                //         {
                //             action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
                //         }

                //         action.fillLiquid = "soap";
                //         PhysicsController.ProcessControlCommand(action);
                //         break;
                //     }

                case "fillwine":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "FillObjectWithLiquid";
						if(splitcommand.Length > 1)
						{
							action.objectId = splitcommand[1];
						}

                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
                        }

                        action.fillLiquid = "wine";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "emptyliquid":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "EmptyLiquidFromObject";
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

                case "useup":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "UseUpObject";
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

                case "tta":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "TouchThenApplyForce";
                        action.x = 0.5f;
                        action.y = 0.5f;
                        action.handDistance = 5.0f;
                        action.direction = new Vector3(0, 1, 0);
                        action.moveMagnitude = 200f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move hand ahead, forward relative to agent's facing
                    //pass in move magnitude or default is 0.25 units
				case "mha":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveHandAhead";

						if(splitcommand.Length > 1)
						{
							action.moveMagnitude = float.Parse(splitcommand[1]);
						}

						else
						    action.moveMagnitude = 0.1f;
						
						// action.x = 0f;
						// action.y = 0f;
						// action.z = 1f;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    } 

					//move hand backward. relative to agent's facing
					//pass in move magnitude or default is 0.25 units               
                case "mhb":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandBack";


                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.1f;
						
                        // action.x = 0f;
                        // action.y = 0f;
                        // action.z = -1f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand left, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhl":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandLeft";                  

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.1f;
						
                        // action.x = -1f;
                        // action.y = 0f;
                        // action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand right, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhr":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandRight";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.1f;
						
                        // action.x = 1f;
                        // action.y = 0f;
                        // action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand up, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhu":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandUp";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.1f;
						
                        // action.x = 0f;
                        // action.y = 1f;
                        // action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

					//move hand down, relative to agent's facing
					//pass in move magnitude or default is 0.25 units
                case "mhd":
                    {
						ServerAction action = new ServerAction();
                        action.action = "MoveHandDown";
                        
                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

                        else
                            action.moveMagnitude = 0.1f;
						
                        // action.x = 0f;
                        // action.y = -1f;
                        // action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  
                
                //changes the time spent to decay to room temperature for all objects in this scene of given type
                case "DecayTimeForType":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SetRoomTempDecayTimeForType";

                        action.TimeUntilRoomTemp = 20f;
                        action.objectType = "Bread";
                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }
                    
                //changes the time spent to decay to room temperature for all objects globally in the scene
                case "DecayTimeGlobal":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SetGlobalRoomTempDecayTime";

                        action.TimeUntilRoomTemp = 20f;
                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }
                
                case "SetTempDecayBool":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SetDecayTemperatureBool";

                        action.allowDecayTemperature = false;
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

                case "push":
					{
						ServerAction action = new ServerAction();
						action.action = "PushObject";

                        if (splitcommand.Length > 1 && splitcommand.Length < 3)
                        {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = 200f;//4000f;
                        }

                        else if(splitcommand.Length > 2)
                        {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = float.Parse(splitcommand[2]);
                        }

                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
                            //action.moveMagnitude = 200f;//4000f;
                        }
							
                        action.z = 1;
						PhysicsController.ProcessControlCommand(action);                  
						break;
					}

                case "pull":
					{
						ServerAction action = new ServerAction();
						action.action = "PullObject";

                        if (splitcommand.Length > 1 && splitcommand.Length < 3)
                        {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = 200f;//4000f;
                        }

                        else if(splitcommand.Length > 2)
                        {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = float.Parse(splitcommand[2]);
                        }

                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestVisibleObject();
                            //action.moveMagnitude = 200f;//4000f;
                        }
							
                        //action.moveMagnitude = 200f;//4000f;
                        action.z = -1;
						PhysicsController.ProcessControlCommand(action);                  
						break;
					}

                case "dirpush":
					{
						ServerAction action = new ServerAction();
						action.action = "DirectionalPush";

                        if (splitcommand.Length > 1 && splitcommand.Length < 3)
                        {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = 10f;//4000f;
                        }

                        else if(splitcommand.Length > 2)
                        {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = float.Parse(splitcommand[2]);
                        }

                        action.pushAngle = 279f;

						PhysicsController.ProcessControlCommand(action);                  
						break;
					}

                case "toggleon":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ToggleObjectOn";
                        if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestToggleObject();
                        }

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                case "toggleoff":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ToggleObjectOff";
                        if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }
                        else
                        {
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestToggleObject();
                        }

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                case "cook":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "CookObject";
                        if (splitcommand.Length > 1)
                        {
                            action.objectId = splitcommand[1];
                        }

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
                            action.numPlacementAttempts = int.Parse(splitcommand[2]);

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
                            action.numPlacementAttempts = int.Parse(splitcommand[2]);
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
                            action.numPlacementAttempts = int.Parse(splitcommand[2]);

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
                            action.numPlacementAttempts = int.Parse(splitcommand[3]);

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
                            action.numPlacementAttempts = int.Parse(splitcommand[3]);

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
                            action.numPlacementAttempts = int.Parse(splitcommand[3]);

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

			//StartCoroutine(CheckIfactionCompleteWasSetToTrueAfterWaitingALittleBit(splitcommand[0]));

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

