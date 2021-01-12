using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace UnityStandardAssets.Characters.FirstPerson
{
	public class DebugInputField : MonoBehaviour
    {
		public GameObject Agent = null;
		public PhysicsRemoteFPSAgentController PhysicsController = null;
        public StochasticRemoteFPSAgentController StochasticController = null;
        public DroneFPSAgentController DroneController = null;
        public AgentManager AManager = null;

        private ControlMode controlMode;

        #if UNITY_EDITOR
        private Dictionary<KeyCode, ControlMode> debugKeyToController = new Dictionary<KeyCode, ControlMode>{
            {KeyCode.F1, ControlMode.DEBUG_TEXT_INPUT},
            {KeyCode.BackQuote, ControlMode.FPS},
            {KeyCode.F2, ControlMode.DISCRETE_POINT_CLICK},
            {KeyCode.F3, ControlMode.DISCRETE_HIDE_N_SEEK},
            {KeyCode.F4, ControlMode.MINIMAL_FPS}
        };
        #endif

        private bool setEnabledControlComponent(ControlMode mode, bool enabled) {
            Type componentType;
            var success = PlayerControllers.controlModeToComponent.TryGetValue(mode, out componentType);
            if (success) {
                var previousComponent = Agent.GetComponent(componentType) as MonoBehaviour;
                if (previousComponent == null) {
                    previousComponent = Agent.AddComponent(componentType) as MonoBehaviour; 
                }
                previousComponent.enabled = enabled;
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
                Debug.Log("In Unity editor, init DebugInputField");
                this.InitializeUserControl();
            #endif
        }

        void SelectPlayerControl() {
            #if UNITY_EDITOR
                Debug.Log("Player Control Set To: Editor control");
                setControlMode(ControlMode.DEBUG_TEXT_INPUT);
            #endif
            #if UNITY_WEBGL
                Debug.Log("Player Control Set To:Webgl");
                setControlMode(ControlMode.FPS);
                PhysicsController.GetComponent<JavaScriptInterface>().enabled = true;
            #endif
            #if CROWDSOURCE_TASK
                Debug.Log("CROWDSOURCE_TASK");
                setControlMode(ControlMode.DISCRETE_HIDE_N_SEEK);
            #endif
            #if TURK_TASK
                Debug.Log("Player Control Set To: TURK");
                setControlMode(ControlMode.DISCRETE_POINT_CLICK);
            #endif
        }

        void InitializeUserControl()
        {
            GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
            PhysicsController = fpsController.GetComponent<PhysicsRemoteFPSAgentController>();
            StochasticController = fpsController.GetComponent<StochasticRemoteFPSAgentController>();
            DroneController = fpsController.GetComponent<DroneFPSAgentController>();
            Agent = PhysicsController.gameObject;
            AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

            // StochasticController = fpsController.GetComponent<StochasticRemoteFPSAgentController>();
            
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

                            // GameObject.Find("DebugCanvasPhysics").GetComponentInChildren<DebugInputField>().setControlMode(entry.Value);
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

        #if UNITY_EDITOR
        // shortcut to execute no-parameter actions

        private void ExecuteAction(string actionName) {
            Dictionary<string, object> action = new Dictionary<string, object>();
            action["action"] = actionName;
            PhysicsController.ProcessControlCommand(action);
        }

        public void Execute(string command)
        {

            if ((PhysicsController.enabled && PhysicsController.IsProcessing) ||
                (StochasticController != null && StochasticController.enabled && StochasticController.IsProcessing)
            ) {
                Debug.Log("Cannot execute command while last action has not completed.");
            }

            //pass in multiple parameters separated by spaces
			string[] splitcommand = command.Split(new string[] { " " }, System.StringSplitOptions.None);

			switch(splitcommand[0])
			{            
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
                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderClassImage = true;
                        // action.renderObjectImage = true;
                        // action.renderFlowImage = true;
                        // action.rotateStepDegrees = 30;
                        //action.ssao = "default";
                        //action.snapToGrid = true;
                        //action.makeAgentsVisible = false;
                        //action.agentMode = "bot";
                        action.fieldOfView = 90f;
                        //action.cameraY = 2.0f;
                        action.snapToGrid = true;
                        // action.rotateStepDegrees = 45;
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
                case "initb":
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

                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderClassImage = true;
                        // action.renderObjectImage = true;
                        // action.renderFlowImage = true;

                        action.gridSize = 0.25f;
                        action.visibilityDistance = 1.0f;
                        action.fieldOfView = 60;
                        action.rotateStepDegrees = 45;
                        action.agentMode = "bot";
                        action.agentControllerType = "stochastic";

                        action.applyActionNoise = true;
                       
                        action.snapToGrid = false;
                        action.action = "Initialize";
                        action.fieldOfView = 90;
                        action.gridSize = 0.25f;
                        AManager.Initialize(action);
                        break;
                    }

                 case "expspawn":
                    {
                        ServerAction action = new ServerAction();

						if (splitcommand.Length == 2 )
                        {
                            if(splitcommand[1] == "s")
							action.objectType = "screen";

                            if(splitcommand[1] == "r")
							action.objectType = "receptacle";
                        }

                        else
                        {
                            action.objectType = "receptacle";
                        }

                        action.action = "ReturnValidSpawnsExpRoom";
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";
                        action.objectVariation = 0;
                        action.y = 120f;//UnityEngine.Random.Range(0, 360);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "exp":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "SpawnExperimentObjAtRandom";
                        action.objectType = "receptacle";
                        action.randomSeed = 50;//UnityEngine.Random.Range(0, 1000);
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";
                        action.objectVariation = 12;
                        action.y = 120f;//UnityEngine.Random.Range(0, 360);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "exps":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "SpawnExperimentObjAtRandom";
                        action.objectType = "screen";
                        action.randomSeed = UnityEngine.Random.Range(0, 1000);
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";

						if (splitcommand.Length == 2 )
                        {
                            action.objectVariation = int.Parse(splitcommand[1]);
                        }

                        else
                        action.objectVariation = 0;

                        action.y = 0f;//UnityEngine.Random.Range(0, 360);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "expp":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "SpawnExperimentObjAtPoint";
                        action.objectType = "replacement";//"receptacle";
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";
                        action.objectVariation = 12;
                        action.position = new Vector3(-1.4f, 0.9f, 0.1f);
                        action.y = 120f;//UnityEngine.Random.Range(0, 360);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "wallcolor":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeWallColorExpRoom";
                        action.r = 100f;
                        action.g = 100f;
                        action.b = 100f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "floorcolor":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeFloorColorExpRoom";
                        action.r = 100f;
                        action.g = 100f;
                        action.b = 100f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "wallmaterial":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeWallMaterialExpRoom";
                        action["objectVariation"] = 1;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "floormaterial":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeFloorMaterialExpRoom";
                        action.objectVariation = 1;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "lightc":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeLightColorExpRoom";
                        action.r = 20f;
                        action.g = 94f;
                        action.b = 10f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "lighti":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeLightIntensityExpRoom";
                        action["intensity"] = 3;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "tabletopc":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableTopColorExpRoom";
                        action.r = 20f;
                        action.g = 94f;
                        action.b = 10f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "tabletopm":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableTopMaterialExpRoom";
                        action.objectVariation = 3;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "tablelegc":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableLegColorExpRoom";
                        action.r = 20f;
                        action.g = 94f;
                        action.b = 10f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "tablelegm":
                    {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableLegMaterialExpRoom";
                        action.objectVariation = 3;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "screenm":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeScreenMaterialExpRoom";
                        action["objectVariation"] = 3;
                        action["objectId"] = "ScreenSheet|-00.18|+01.24|+00.23";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "screenc":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeScreenColorExpRoom";
                        action["r"] = 20f;
                        action["g"] = 94f;
                        action["b"] = 10f;
                        action["objectId"] = "ScreenSheet|-00.18|+01.24|+00.23";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                 case "grid":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetReceptacleCoordinatesExpRoom";
                        action["gridSize"] = 0.1f;
                        action["maxStepCount"] = 5;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //initialize drone mode
                 case "initd":
                    {
						ServerAction action = new ServerAction();

                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderClassImage = true;
                        // action.renderObjectImage = true;
                        // action.renderFlowImage = true;

                        action.action = "Initialize";
                        action.agentMode = "drone";
                        action.agentControllerType = "drone";
                        AManager.Initialize(action);

                        break;
                    }

                //activate cracked camera effect with random seed
                 case "cc":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "CameraCrack";

                        //give me a seed
                        if(splitcommand.Length == 2)
                        {
                            action["randomSeed"] = int.Parse(splitcommand[1]);
                        }

                        else
                        {
                            action["randomSeed"] = 0;
                        }

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                //move ahead stochastic
                 case "mas":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveAhead";

                        action.moveMagnitude = 0.25f;
						
                        StochasticController.ProcessControlCommand(action);

                        break;
                    }
                case "rad":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetAgentRadius";
                        action["agentRadius"] = 0.35f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "color":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomizeColors";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "resetcolor":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ResetColors";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "spawnabove":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "GetSpawnCoordinatesAboveReceptacle";
                        action.objectId = "CounterTop|-01.94|+00.98|-03.67";
                        action.anywhere = false;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    } 

                case "crazydiamond":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MakeObjectsOfTypeUnbreakable";
                        action["objectType"] = "Egg";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  

                case "stc":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SpawnTargetCircle";
                        if (splitcommand.Length > 1) 
                        {
                            if(int.Parse(splitcommand[1]) == 0)
                            {
                                action.objectVariation = 0;
                            }

                            if(int.Parse(splitcommand[1]) == 1)
                            {
                                action.objectVariation = 1;
                            }

                            if(int.Parse(splitcommand[1]) == 2)
                            {
                                action.objectVariation = 2;
                            }
                        }

                        action.anywhere = false;//false, only recepatcle objects in viewport used
                        //action.minDistance = 1.8f;
                        //action.maxDistance = 2.5f;
                        //action.objectId = "Floor|+00.00|+00.00|+00.00";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "smp":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetMassProperties";
                        action["objectId"] = "Pot|+00.30|+00.96|+01.35";
                        action["mass"] = 100;
                        action["drag"] = 100;
                        action["angularDrag"] = 100;
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "AdvancePhysicsStep";
                        action["timeStep"] = 0.02f; //max 0.05, min 0.01
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    
                case "up":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "UnpausePhysicsAutoSim";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    
                case "its":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "InitializeTableSetting";
                        if (splitcommand.Length > 1) {
                            action["objectVariation"] = int.Parse(splitcommand[1]);
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "potwhcb":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PointsOverTableWhereHandCanBe";
                        action["objectId"] = splitcommand[1];
                        action["x"] = float.Parse(splitcommand[2]);
                        action["z"] = float.Parse(splitcommand[3]);

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "pfrat":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["action"] = "PlaceFixedReceptacleAtLocation";
                        if (splitcommand.Length > 1) {
                            action["objectVariation"] = int.Parse(splitcommand[1]);
                            action["x"] = float.Parse(splitcommand[2]);
                            action["y"] = float.Parse(splitcommand[3]);
                            action["z"] = float.Parse(splitcommand[4]);
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "pbwal":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["action"] = "PlaceBookWallAtLocation";
                        if (splitcommand.Length > 1) {
                            action["objectVariation"] = int.Parse(splitcommand[1]);
                            action["x"] = float.Parse(splitcommand[2]);
                            action["y"] = float.Parse(splitcommand[3]);
                            action["z"] = float.Parse(splitcommand[4]);
                            action["rotation"] = new Vector3(0f, float.Parse(splitcommand[5]), 0f);
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

                        //action.ssao = "default";

                        action.action = "Initialize";
                        AManager.Initialize(action);
                        break;
                    }

                case "atpc":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "AddThirdPartyCamera";
                        AManager.AddThirdPartyCamera(action);
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
                case "ctlq":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeQuality";
                        action.quality = "Very Low";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "roco":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomlyOpenCloseObjects";
                        action["randomSeed"] = (new System.Random()).Next(1, 1000000);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "crouch":
                    {
                        ExecuteAction("Crouch");
                        break;
                    }
                case "stand":
                    {
                        ExecuteAction("Stand");
                        break;
                    }

                case "remove":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RemoveFromScene";
                        
                        if(splitcommand.Length == 2)
                        {
                            action["objectId"] = splitcommand[1];
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "putr":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";
                        action["objectId"] = PhysicsController.ObjectIdOfClosestReceptacleObject();
                        action["randomSeed"] = int.Parse(splitcommand[1]);
                            
                        //set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action["placeStationary"] = false; 

                        //set this true to ignore Placement Restrictions
                        action["forceAction"] = true;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "put":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";
                        
                        if(splitcommand.Length == 2)
                        {
                            action["objectId"] = splitcommand[1];
                        }

                        else
                            //action.receptacleObjectId = PhysicsController.ObjectIdOfClosestReceptacleObject();
                            
                        //set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action["placeStationary"] = true; 
                        action["x"] = 0.5f;
                        action["y"] = 0.5f;
                        //set this true to ignore Placement Restrictions
                        action["forceAction"] = true;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //put an object down with stationary false
                case "putf":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";
                        
                        if(splitcommand.Length == 2)
                        {
                            action["objectId"] = splitcommand[1];
                        }

                        else
                            action["objectId"] = PhysicsController.ObjectIdOfClosestReceptacleObject();
                            
                        //set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        //set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action["placeStationary"] = false; 

                        //set this true to ignore Placement Restrictions
                        action["forceAction"] = true;

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //make all pickupable objects kinematic false so that they will react to collisions. Otherwise, some objects might be defaulted to kinematic true, or
                //if they were placed with placeStationary true, then they will not interact with outside collisions immediately.
                case "maom":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MakeAllObjectsMoveable";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //tests the Reset function on AgentManager.cs, adding a path to it from the PhysicsController
                case "reset":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "Reset";
                        PhysicsController.ProcessControlCommand(action);
                        ExecuteAction("Reset");
                        break;
                    }
                    
                case "poap":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PlaceObjectAtPoint";

                        BaseFPSAgentController agent = AManager.agents[0];

                        GameObject itemInHand = AManager.agents[0].ItemInHand;
                        if (itemInHand != null) {
                            itemInHand.SetActive(false);
                        }

                        RaycastHit hit;
                        Ray ray = agent.m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
                        bool raycastDidHit = Physics.Raycast(ray, out hit, 10f, (1 << 8) | (1 << 10));

                        print("where did the ray land? " + hit.point);
                        if (itemInHand != null) {
                            itemInHand.SetActive(true);
                        }

                        if (raycastDidHit) {
                            SimObjPhysics sop = null;
                            if (itemInHand != null) {
                                sop = itemInHand.GetComponent<SimObjPhysics>();
                            } else {
                                SimObjPhysics[] allSops = GameObject.FindObjectsOfType<SimObjPhysics>();
                                sop = allSops[UnityEngine.Random.Range(0, allSops.Length)];
                            }
                            //adding y offset to position so that the downward raycast used in PlaceObjectAtPoint correctly collides with surface below point
                            action["position"] = hit.point + new Vector3(0, 0.5f, 0); 
                            action["objectId"] = sop.ObjectID;
                            
                            Debug.Log(action);
                            PhysicsController.ProcessControlCommand(action);
                        } else {
                            agent.actionFinished(false);
                        }

                        break;
                    }

                //set forceVisible to true if you want objects to not spawn inside receptacles and only out in the open
                //set forceAction to true to spawn with kinematic = true to more closely resemble pivot functionality
                case "irs":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "InitialRandomSpawn";

                        // List<string> excludedObjectIds = new List<string>();
                        // foreach (SimObjPhysics sop in AManager.agents[0].VisibleSimObjs(false)) {
                        //     excludedObjectIds.Add(sop.ObjectID);
                        // }
                        // action["excludedObjectIds"] = excludedObjectIds.ToArray();

                        //give me a seed
                        if(splitcommand.Length == 2)
                        {
                            action["randomSeed"] = int.Parse(splitcommand[1]);
                            action["forceVisible"] = false;
                            action["numPlacementAttempts"] = 5;
                        }

                        //should objects be spawned only in immediately visible areas?
                        else if(splitcommand.Length == 3)
                        {
                            action["randomSeed"] = int.Parse(splitcommand[1]);

                            if(splitcommand[2] == "t") 
                            action["forceVisible"] = true;

                            if(splitcommand[2] == "f") 
                            action["forceVisible"] = false;
                        }

                        else if(splitcommand.Length == 4)
                        {
                            action["randomSeed"] = int.Parse(splitcommand[1]);

                            if(splitcommand[2] == "t") 
                            action["forceVisible"] = true;

                            if(splitcommand[2] == "f") 
                            action["forceVisible"] = false;

                            action["numPlacementAttempts"] = int.Parse(splitcommand[3]);
                        }

                        else
                        {
                            action["randomSeed"] = 0;
                            action["forceVisible"] = false;//true;
                            action["numPlacementAttempts"] = 20;
                        }

                        // ObjectTypeCount otc = new ObjectTypeCount();
                        // otc.objectType = "Mug";
                        // otc.count = 20;
                        // ObjectTypeCount[] count = new ObjectTypeCount[1];
                        // count[0] = otc;
                        // action.numDuplicatesOfType = count;

                        String[] excludeThese = new String[1];
                        excludeThese[0] = "CounterTop";
                        action["excludedReceptacles"] = excludeThese;

                        action["placeStationary"] = true;//set to false to spawn with kinematic = false, set to true to spawn everything kinematic true and they won't roll around
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
                        ExecuteAction("ChangeAgentFaceToNeutral");
                        break;
                    }
                case "happy":
                    {
                        ExecuteAction("ChangeAgentFaceToHappy");
                        break;
                    }

                case "mad":
                    {
                        ExecuteAction("ChangeAgentFaceToMad");
                        break;
                    }

                case "supermad":
                    {
                        ExecuteAction("ChangeAgentFaceToSuperMad");
                        break;
                    }

                case "ruaa":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateUniverseAroundAgent";

                        action.rotation = new Vector3(
                            0f, float.Parse(splitcommand[1]), 0f
                        );
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                case "thas":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ToggleHideAndSeekObjects";

                        if(splitcommand.Length == 2)
                        {
                            if(splitcommand[1] == "t") 
                            action["forceVisible"] = true;

                            if(splitcommand[1] == "f") 
                            action["forceVisible"] = false;
                        }

                        else
                        {
                            action["forceVisible"] = false;
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
                case "grpfo":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "GetReachablePositionsForObject";
                        action.objectId = splitcommand[1];
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomlyCreateAndPlaceObjectOnFloor";
                        action["objectType"] = splitcommand[1];
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
                case "gusfo": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetUnreachableSilhouetteForObject";
                        action["objectId"] = splitcommand[1];
                        action["z"] = float.Parse(splitcommand[2]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "rhs":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomizeHideSeekObjects";
                        action["removeProb"] = float.Parse(splitcommand[1]);
                        action["randomSeed"] = int.Parse(splitcommand[2]);
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetReachablePositions";
                        //action.maxStepCount = 10;
                        PhysicsController.ProcessControlCommand(action);
                        Debug.Log(PhysicsController.reachablePositions.Length);
                        break;
                    }

                case "grpb":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetReachablePositions";
                        //action.maxStepCount = 10;
                        StochasticController.ProcessControlCommand(action);
                        Debug.Log("stochastic grp " + StochasticController.reachablePositions.Length);
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
                        action.fieldOfView = float.Parse(splitcommand[1]);
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "map":
                    {
                        ExecuteAction("ToggleMapView");
                        break;
                    }
                case "nopfwoiv":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "NumberOfPositionsObjectsOfTypeAreVisibleFrom";
                        action.objectType = splitcommand[1];
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
                        ExecuteAction("CloseVisibleObjects");
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ObjectsInBox";
                        action["x"] = float.Parse(splitcommand[1]);
                        action["z"] = float.Parse(splitcommand[2]);
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
						
                        //action.manualInteract = true;

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

                    //move ahead, force action true
                case "maf":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveAhead";

						if (splitcommand.Length > 1)
						{
							action.moveMagnitude = float.Parse(splitcommand[1]);
						}
						else
                        action.moveMagnitude = 0.25f;

                        action.forceAction = true;
						
                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                    //move backward, force action true
                case "mbf":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveBack";     

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

						else
                        action.moveMagnitude = 0.25f;

                        action.forceAction = true;
						
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move left, force action true
                case "mlf":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveLeft";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

						else
                        action.moveMagnitude = 0.25f;

                        action.forceAction = true;
						
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move right, force action true
                case "mrf":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "MoveRight";

                        if (splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }

						else
                        action.moveMagnitude = 0.25f;

                        action.forceAction = true;
						
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                case "fu":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyUp";
                        action["moveMagnitude"] = 2f;
                        DroneController.ProcessControlCommand(action);
                        break;
                    }

                case "fd":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyDown";
                        action["moveMagnitude"] = 2f;
                        DroneController.ProcessControlCommand(action);
                        break;
                    }

                case "fa":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyAhead";
                        action["moveMagnitude"] = 2f;

                        DroneController.ProcessControlCommand(action);
                        break;
                    }
                case "fl":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyLeft";
                        action["moveMagnitude"] = 2f;
                        DroneController.ProcessControlCommand(action);
                        break;
                    }

                case "fr":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyRight";
                        action["moveMagnitude"] = 2f;
                        DroneController.ProcessControlCommand(action);
                        break;
                    }

                case "fb":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyBack";
                        action["moveMagnitude"] = 2f;
                        DroneController.ProcessControlCommand(action);
                        break;
                    }

                    //look up
                case "lu":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "LookUp";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}

                        //action.manualInteract = true;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //stochastic look up
                case "lus":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "LookUp";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}

                        StochasticController.ProcessControlCommand(action);
                        break;
                    }

                    //look down
                case "ld":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "LookDown";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}

                        //action.manualInteract = true;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //stochastic look down
                case "lds":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "LookDown";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}

                        StochasticController.ProcessControlCommand(action);
                        break;
                    }

                    //rotate left
                case "rl":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateLeft";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}

                        //action.manualInteract = true;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //rotate left stochastic
                case "rls":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateLeft";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}

                        StochasticController.ProcessControlCommand(action);
                        break;
                    }
                
                    //rotate right
                case "rr":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateRight";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}
                        
                        //action.manualInteract = true;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }   

                    //rotate right stochastic
                case "rrs":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateRight";

						if(splitcommand.Length > 1)
						{
							action.degrees = float.Parse(splitcommand[1]);
						}
                        
                        StochasticController.ProcessControlCommand(action);
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
							//action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
						}

                        action.forceAction = true;
                        action.x = 0.5f; 
                        action.y = 0.5f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                //manual pickup object- test hand                
				case "pum":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "PickupObject";
						if(splitcommand.Length > 1)
						{
							action.objectId = splitcommand[1];
						}

						else
						{
							action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
						}

                        action.manualInteract = true;
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
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
                        }
                        action.x = 0.5f;
                        action.y = 0.5f;
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
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
                        }
                        action.x = 0.5f;
                        action.y = 0.5f;

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
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
                        }
                        action.x = 0.5f;
                        action.y = 0.5f;
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
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
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
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
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
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
                        }

                        action.fillLiquid = "coffee";
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

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
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
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
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
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
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleObject();
                        }

                        action.x =0.5f;
                        action.y = 0.5f;
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

                case "ror":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "RotateHandRelative";
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
                        action.handDistance = 2.0f;
                        action.direction = new Vector3(0, 0, 1);
                        action.moveMagnitude = 800f;

                        if(splitcommand.Length > 1)
                        {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    //move hand ahead, forward relative to agent's facing
                    //pass in move magnitude or default is 0.25 units
				case "mha":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandAhead";

						if(splitcommand.Length > 1)
						{
							action["moveMagnitude"] = float.Parse(splitcommand[1]);
						}

						else
						    action["moveMagnitude"] = 0.1f;
						
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandBack";


                        if (splitcommand.Length > 1)
                        {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        }

                        else
                            action["moveMagnitude"] = 0.1f;
						
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandLeft";

                        if (splitcommand.Length > 1)
                        {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        }

                        else
                            action["moveMagnitude"] = 0.1f;
						
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandRight";

                        if (splitcommand.Length > 1)
                        {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        }

                        else
                            action["moveMagnitude"] = 0.1f;
						
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandUp";

                        if (splitcommand.Length > 1)
                        {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        }

                        else
                            action["moveMagnitude"] = 0.1f;
						
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandDown";
                        
                        if (splitcommand.Length > 1)
                        {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        }

                        else
                            action["moveMagnitude"] = 0.1f;
						
                        // action.x = 0f;
                        // action.y = -1f;
                        // action.z = 0f;
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }  
                
                //changes the time spent to decay to room temperature for all objects in this scene of given type
                case "DecayTimeForType":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetRoomTempDecayTimeForType";

                        action["TimeUntilRoomTemp"] = 20f;
                        action["objectType"] = "Bread";
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
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetDecayTemperatureBool";

                        action["allowDecayTemperature"] = false;
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

                        action.moveMagnitude = 2000f;

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
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestPickupableOrMoveableObject();
                            //action.moveMagnitude = 200f;//4000f;
                        }
						
                        action.x = 0.5f;
                        action.y = 0.5f;

                        action.z = 1;
						PhysicsController.ProcessControlCommand(action);                  
						break;
					}

                case "pull":
					{
						ServerAction action = new ServerAction();
						action.action = "PullObject";

                        action.moveMagnitude = 2000f;

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
                            action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestPickupableOrMoveableObject();
                            //action.moveMagnitude = 200f;//4000f;
                        }
							
                        //action.moveMagnitude = 200f;//4000f;
                        action.z = -1;
                        action.x = 0.5f;
                        action.y = 0.5f;
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
                        action.moveMagnitude = 159f;
                        action.x = 0.5f;
                        action.y = 0.5f;
						PhysicsController.ProcessControlCommand(action);                  
						break;
					}

                case "toggleon":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ToggleObjectOn";
                        if (splitcommand.Length > 1)
                        {
                            action["objectId"] = splitcommand[1];
                        }

                        else
                        {
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestToggleObject();
                            action["x"] = 0.5f;
                            action["y"] = 0.5f;
                        }

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                case "toggleoff":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ToggleObjectOff";
                        if (splitcommand.Length > 1)
                        {
                            action["objectId"] = splitcommand[1];
                        }
                        else
                        {
                            //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestToggleObject();
                            action["x"] = 0.5f;
                            action["y"] = 0.5f;
                        }

                        //action.objectId = "DeskLamp|-01.32|+01.24|-00.99";
                        action["forceVisible"] = true;
                        action["forceAction"] = true;
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

                        action.x = 0.5f;
                        action.y = 0.5f;
                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                case "sos":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SetObjectStates";
                        action.SetObjectStates = new SetObjectStates()
                        {
                            stateChange = "toggleable",
                            objectType = "DeskLamp",
                            isToggled = false
                        };

                        PhysicsController.ProcessControlCommand(action);

                        break;
                    }

                case "pose":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "SetObjectPoses";
                        action.objectPoses = new ObjectPose[1];

                        action.objectPoses[0] = new ObjectPose();

                        action.objectPoses[0].objectName = "Book_3d15d052";
                        action.objectPoses[0].position = new Vector3(0, 0, 0);
                        action.objectPoses[0].rotation = new Vector3(0, 0, 0);


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
                           //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleOpenableObject();
						}

                        action.moveMagnitude = 0.5f;
                        action.x = 0.5f;
                        action.y = 0.5f;
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
                           //action.objectId = Agent.GetComponent<PhysicsRemoteFPSAgentController>().ObjectIdOfClosestVisibleOpenableObject();
						}

                        action.x = 0.5f;
                        action.y = 0.5f;
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

                    // Will fail if navmeshes are not setup
                    case "shortest_path":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetShortestPath";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
                            //ID of spawner
                            action["objectId"] = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action["position"] = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]), 
                                    float.Parse(splitcommand[4])
                                );
                            }
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                     case "shortest_path_type":
                    {

                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetShortestPath";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
                            //ID of spawner
                            action["objectType"] = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action["position"] = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]), 
                                    float.Parse(splitcommand[4])
                                );
                            }
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    case "shortest_path_point":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetShortestPathToPoint";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
                            //ID of spawner
                            //action.objectId = splitcommand[1];

                            if (splitcommand.Length == 4) {
                                action["x"] = float.Parse(splitcommand[1]);
                                action["y"] = float.Parse(splitcommand[2]);
                                action["z"] = float.Parse(splitcommand[3]);
                            }
                            if (splitcommand.Length >= 7) {
                                action["position"] = new Vector3(
                                    float.Parse(splitcommand[1]),
                                    float.Parse(splitcommand[2]), 
                                    float.Parse(splitcommand[3])
                                );
                                action["x"] = float.Parse(splitcommand[4]);
                                action["y"] = float.Parse(splitcommand[5]);
                                action["z"] = float.Parse(splitcommand[6]);
                            }
                            if (splitcommand.Length >= 8) {
                                action["allowedError"] = float.Parse(splitcommand[7]);
                            }


                             if (splitcommand.Length < 4) {
                                throw new ArgumentException("need to provide 6 floats, first 3 source position second 3 target position");
                            }
                        }
                        else {
                             throw new ArgumentException("need to provide at least 3 floats for target position");
                        }
                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    case "visualize_path":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "VisualizePath";
                        action.objectId = "0";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
                            //ID of spawner
                            action.objectId = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action.position = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]), 
                                    float.Parse(splitcommand[4])
                                );
                            }
                            else {
                                action.positions = new List<Vector3>() {
                                    new Vector3( 4.258f, 1.0f, -1.69f),
                                    new Vector3(6.3f, 1.0f, -3.452f)
                                };
                            }
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    case "vp":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "VisualizeShortestPaths";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
                            //ID of spawner
                            action.objectType = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action.position = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]), 
                                    float.Parse(splitcommand[4])
                                );
                            }
                            else {
                                var pos = PhysicsController.getReachablePositions().Shuffle();
                                action.positions = pos.Take(20).ToList();
                                action.grid = true;
                                // action.pathGradient = new Gradient() {
                                //     colorKeys = new GradientColorKey[]{
                                //          new GradientColorKey(Color.white, 0.0f),
                                //          new GradientColorKey(Color.blue, 1.0f)
                                //         },
                                //     alphaKeys =  new GradientAlphaKey[]{
                                //         new GradientAlphaKey(1.0f, 0.0f),
                                //         new GradientAlphaKey(1.0f, 1.0f)
                                //     },
                                //     mode = GradientMode.Blend
                                // };
                                // action.gridColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                                // action.positions = new List<Vector3>() {
                                //     new Vector3( 4.258f, 1.0f, -2.69f),
                                //     new Vector3(4.3f, 1.0f, -3.452f)
                                // };
                            }
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    case "visualize_shortest_path":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "VisualizeShortestPaths";

                        //pass in a min range, max range, delay
                        if (splitcommand.Length > 1)
                        {
                            //ID of spawner
                            action.objectType = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action.position = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]), 
                                    float.Parse(splitcommand[4])
                                );
                            }
                            else {
                                // var pos = PhysicsController.getReachablePositions().Shuffle();
                                action.positions = new List<Vector3>() { PhysicsController.transform.position };
                                action.grid = true;
                            }
                        }

                        PhysicsController.ProcessControlCommand(action);
                        break;
                    }
                    case "get_object_type_ids":
                    {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ObjectTypeToObjectIds";
                        if (splitcommand.Length > 1)
                        {
                            action["objectType"] = splitcommand[1];
                        }

                         PhysicsController.ProcessControlCommand(action);
                        break;
                    }

                    case "scale":
                    {
                        ServerAction action = new ServerAction();
                        action.action = "ScaleObject";
                        action.objectId = "Cup|-01.36|+00.78|+00.71";
                        action.scale = 2.0f;

                        if (splitcommand.Length > 1)
                        {
                            action.scale = float.Parse(splitcommand[1]);
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
        #endif

#if UNITY_EDITOR

        // Taken from https://answers.unity.com/questions/1144378/copy-to-clipboard-with-a-button-unity-53-solution.html
        public static void CopyToClipboard(string s) {
            TextEditor te = new TextEditor();
            te.text = s;
            te.SelectAll();
            te.Copy();
        }

        //used to show what's currently visible on the top left of the screen
        void OnGUI() {
            if (PhysicsController.VisibleSimObjPhysics != null && this.controlMode != ControlMode.MINIMAL_FPS) {
                if (PhysicsController.VisibleSimObjPhysics.Length > 10) {
                    int horzIndex = -1;
                    GUILayout.BeginHorizontal();
                    foreach (SimObjPhysics o in PhysicsController.VisibleSimObjPhysics) {
                        horzIndex++;
                        if (horzIndex >= 3) {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            horzIndex = 0;
                        }
                        GUILayout.Button(o.ObjectID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth(200f));
                    }

                    GUILayout.EndHorizontal();
                } else {
                    //Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);

                    //int position_number = 0;
                    foreach (SimObjPhysics o in PhysicsController.VisibleSimObjPhysics) {
                        string suffix = "";
                        // Bounds bounds = new Bounds(o.gameObject.transform.position, new Vector3(0.05f, 0.05f, 0.05f));
                        // if (GeometryUtility.TestPlanesAABB(planes, bounds)) {
                        //     //position_number += 1;

                        //     //if (o.GetComponent<SimObj>().Manipulation == SimObjManipProperty.Inventory)
                        //     //    suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

                        //     //else
                        //     //suffix += " VISIBLE";
                        //     //if(!IgnoreInteractableFlag)
                        //     //{
                        //     // if (o.isInteractable == true)
                        //     // {
                        //     //     suffix += " INTERACTABLE";
                        //     // }
                        //     //}

                        // }

                        if (GUILayout.Button(o.ObjectID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth(100f))) {
                            CopyToClipboard(o.ObjectID);
                        }
                    }
                }
            }
        }
#endif

    }
}

