using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEditor;
using Newtonsoft.Json.Linq;


namespace UnityStandardAssets.Characters.FirstPerson {
    public class DebugInputField : MonoBehaviour {
        public GameObject Agent = null;
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

        public IEnumerator moveArmHeightDebug(float height) {
            CapsuleCollider cc = CurrentActiveController().GetComponent<CapsuleCollider>();
            var arm = CurrentActiveController().GetComponentInChildren<IK_Robot_Arm_Controller>();
            Vector3 cc_center = cc.center;
            Vector3 cc_maxY = cc.center + new Vector3(0, cc.height / 2f, 0);
            Vector3 cc_minY = cc.center + new Vector3(0, (-cc.height / 2f) / 2f, 0); // this is halved to prevent arm clipping into floor

            // linear function that take height and adjusts targetY relative to min/max extents
            float targetY = ((cc_maxY.y - cc_minY.y) * (height)) + cc_minY.y;
            Vector3 target = new Vector3(0, targetY, 0);
            float currentDistance = Vector3.SqrMagnitude(target - arm.transform.localPosition);
            double epsilon = 1e-3;
            while (currentDistance > epsilon && !arm.collisionListener.ShouldHalt()) {
                Vector3 direction = (target - arm.transform.localPosition).normalized;
                arm.transform.localPosition += direction * 1.0f * Time.fixedDeltaTime;

                if (!Physics.autoSimulation) {
                    PhysicsSceneManager.PhysicsSimulateTHOR(Time.fixedDeltaTime);
                }

                yield return new WaitForEndOfFrame();

                currentDistance = Vector3.SqrMagnitude(target - arm.transform.localPosition);
            }

        }
        public void dumpPosition(Transform to) {
            Debug.Log("GameObject: " + to.gameObject.name);
            Debug.Log(
                to.position.x.ToString("0.####") + " " +
                to.position.y.ToString("0.####") + " " +
                to.position.z.ToString("0.####") + " "
            );

        }

        public IEnumerator moveArmDebug(Vector3 targetArmBase) {

            var arm = CurrentActiveController().GetComponentInChildren<IK_Robot_Arm_Controller>();
            // var rig = arm.transform.Find("FK_IK_rig").Find("robot_arm_IK_rig").GetComponent<UnityEngine.Animations.Rigging.Rig>();
            // var rigBuilder = arm.transform.Find("FK_IK_rig").Find("robot_arm_IK_rig").GetComponent<UnityEngine.Animations.Rigging.RigBuilder>();
            // var animator = arm.gameObject.GetComponent<Animator>();
            // animator.enabled = false;
            Debug.Log("My name is " + arm.name);
            var armTarget = arm.transform.Find("robot_arm_FK_IK_rig").Find("IK_rig").Find("IK_pos_rot_manipulator");
            var wristCol = GameObject.Find("robot_wrist_1_tcol (11)").transform;
            Vector3 target = arm.transform.TransformPoint(targetArmBase);
            float currentDistance = Vector3.SqrMagnitude(target - armTarget.transform.position);
            double epsilon = 1e-3;
            Debug.Log("Starting arm movement");
            while (currentDistance > epsilon && !arm.collisionListener.ShouldHalt()) {
                Vector3 direction = (target - armTarget.transform.position).normalized;
                armTarget.transform.position += direction * 1.0f * Time.fixedDeltaTime;

                GameObject.Find("robot_arm_FK_IK_rig").GetComponent<FK_IK_Solver>().ManipulateArm();

                dumpPosition(wristCol);

                if (!Physics.autoSimulation) {
                    PhysicsSceneManager.PhysicsSimulateTHOR(Time.fixedDeltaTime);
                }
                // animator.Update(Time.fixedDeltaTime);

                yield return new WaitForEndOfFrame();

                currentDistance = Vector3.SqrMagnitude(target - armTarget.transform.position);
            }
            yield return new WaitForEndOfFrame();
            Debug.Log("Ending arm movement");

        }

        public void setControlMode(ControlMode mode) {
            setEnabledControlComponent(controlMode, false);
            controlMode = mode;
            setEnabledControlComponent(controlMode, true);
        }

        // Use this for initialization
        void Start() {
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

        void InitializeUserControl() {
            GameObject fpsController = GameObject.FindObjectOfType<BaseAgentComponent>().gameObject;
            Agent = fpsController.gameObject;
            AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

            // StochasticController = fpsController.GetComponent<StochasticRemoteFPSAgentController>();

            SelectPlayerControl();

#if !UNITY_EDITOR
            HideHUD();
#endif
        }

        BaseFPSAgentController CurrentActiveController() {
            return AManager.PrimaryAgent;
        }

        private PhysicsRemoteFPSAgentController PhysicsController {
            get => (PhysicsRemoteFPSAgentController)this.CurrentActiveController();

        }

        // Update is called once per frame
        void Update() {
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

        public void HideHUD() {
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

        public string closestVisibleObjectId() {
            return ((PhysicsRemoteFPSAgentController)AManager.PrimaryAgent).ObjectIdOfClosestVisibleObject();
        }

        public IEnumerator ExecuteBatch(List<string> commands) {

            foreach (var command in commands) {
                while (CurrentActiveController().IsProcessing) {
                    yield return new WaitForEndOfFrame();
                }
                Debug.Log("Executing Batch command: " + command);
                Execute(command);
            }
        }


        // shortcut to execute no-parameter actions
        private void ExecuteAction(string actionName) {
            Dictionary<string, object> action = new Dictionary<string, object>();
            action["action"] = actionName;
            CurrentActiveController().ProcessControlCommand(action);
        }

        public void Execute(string command) {

            if (CurrentActiveController().IsProcessing) {
                Debug.Log("Cannot execute command while last action has not completed.");
            }

            // pass in multiple parameters separated by spaces
            string[] splitcommand = command.Split(new string[] { " " }, System.StringSplitOptions.None);

            switch (splitcommand[0]) {
                case "init": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        // if you want to use smaller grid size step increments, initialize with a smaller/larger gridsize here
                        // by default the gridsize is 0.25, so only moving in increments of .25 will work
                        // so the MoveAhead action will only take, by default, 0.25, .5, .75 etc magnitude with the default
                        // grid size!
                        if (splitcommand.Length == 2) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                        } else if (splitcommand.Length == 3) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                            action["agentCount"] = int.Parse(splitcommand[2]);
                        } else if (splitcommand.Length == 4) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                            action["agentCount"] = int.Parse(splitcommand[2]);
                            action["makeAgentsVisible"] = int.Parse(splitcommand[3]) == 1;
                        }
                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderSemanticSegmentation = true;
                        // action.renderInstanceSegmentation = true;
                        // action.renderFlowImage = true;
                        // action.rotateStepDegrees = 30;
                        // action.ssao = "default";
                        // action.snapToGrid = true;
                        // action.makeAgentsVisible = false;
                        // action.agentMode = "locobot";
                        action["fieldOfView"] = 90f;
                        // action.cameraY = 2.0f;
                        action["snapToGrid"] = true;
                        // action.rotateStepDegrees = 45;
                        action["action"] = "Initialize";
                        CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), AManager);
                        // AgentManager am = PhysicsController.gameObject.FindObjectsOfType<AgentManager>()[0];
                        // Debug.Log("Physics scene manager = ...");
                        // Debug.Log(physicsSceneManager);
                        // AgentManager am = physicsSceneManager.GetComponent<AgentManager>();
                        // Debug.Log(am);
                        // am.Initialize(action);
                        break;
                    }
                case "initb": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        // if you want to use smaller grid size step increments, initialize with a smaller/larger gridsize here
                        // by default the gridsize is 0.25, so only moving in increments of .25 will work
                        // so the MoveAhead action will only take, by default, 0.25, .5, .75 etc magnitude with the default
                        // grid size!
                        if (splitcommand.Length == 2) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                        } else if (splitcommand.Length == 3) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                            action["agentCount"] = int.Parse(splitcommand[2]);
                        } else if (splitcommand.Length == 4) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                            action["agentCount"] = int.Parse(splitcommand[2]);
                            action["makeAgentsVisible"] = int.Parse(splitcommand[3]) == 1;
                        }

                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderSemanticSegmentation = true;
                        // action.renderInstanceSegmentation = true;
                        // action.renderFlowImage = true;

                        action["action"] = "Initialize";
                        action["agentMode"] = "locobot";
                        // action["gridSize"] = 0.25f;
                        // action["visibilityDistance"] = 1.0f;
                        action["rotateStepDegrees"] = 45;
                        // action["agentControllerType"] = "stochastic";
                        // action["applyActionNoise"] = true;
                        // action["snapToGrid"] = false;
                        // action["fieldOfView"] = 90;
                        // action["gridSize"] = 0.25f;


                        action["applyActionNoise"] = true;

                        action["snapToGrid"] = false;
                        action["action"] = "Initialize";
                        action["fieldOfView"] = 90;
                        action["gridSize"] = 0.25f;
                        CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), AManager);
                        break;
                    }
                case "inita": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        // if you want to use smaller grid size step increments, initialize with a smaller/larger gridsize here
                        // by default the gridsize is 0.25, so only moving in increments of .25 will work
                        // so the MoveAhead action will only take, by default, 0.25, .5, .75 etc magnitude with the default
                        // grid size!
                        if (splitcommand.Length == 2) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                        } else if (splitcommand.Length == 3) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                            action["agentCount"] = int.Parse(splitcommand[2]);
                        } else if (splitcommand.Length == 4) {
                            action["gridSize"] = float.Parse(splitcommand[1]);
                            action["agentCount"] = int.Parse(splitcommand[2]);
                            action["makeAgentsVisible"] = int.Parse(splitcommand[3]) == 1;
                        }
                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderClassImage = true;
                        // action.renderObjectImage = true;
                        // action.renderFlowImage = true;
                        // PhysicsController.actionComplete = false;
                        // action.rotateStepDegrees = 30;
                        // action.ssao = "default";
                        // action.snapToGrid = true;
                        // action.makeAgentsVisible = false;
                        // action.agentMode = "bot";
                        // action.fieldOfView = 90f;
                        // action.cameraY = 2.0f;
                        // action.snapToGrid = true;
                        // action.rotateStepDegrees = 45;
                        action["action"] = "Initialize";

                        action["agentMode"] = "arm";
                        action["agentControllerType"] = "mid-level";
                        action["renderInstanceSegmentation"] = true;

                        // action.useMassThreshold = true;
                        // action.massThreshold = 10f;


                        CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), AManager);
                        // AgentManager am = PhysicsController.gameObject.FindObjectsOfType<AgentManager>()[0];
                        // Debug.Log("Physics scene manager = ...");
                        // Debug.Log(physicsSceneManager);
                        // AgentManager am = physicsSceneManager.GetComponent<AgentManager>();
                        // Debug.Log(am);
                        // am.Initialize(action);
                        break;
                    }

                case "inite": {
                        Dictionary<string, object> action = new Dictionary<string, object>{
                            {"action", "Initialize"},
                            {"agentMode", "arm"},
                            {"agentControllerType", "mid-level"}
                        };
                        ActionDispatcher.Dispatch(AManager, new DynamicServerAction(action));

                        var arm = CurrentActiveController().GetComponentInChildren<IK_Robot_Arm_Controller>();
                        var armTarget = GameObject.Find("IK_pos_rot_manipulator");
                        armTarget.transform.Rotate(90f, 0f, 0f);

                        var armJointToRotate = GameObject.Find("IK_pole_manipulator");
                        armJointToRotate.transform.Rotate(0f, 0f, 90f);
                        var armBase = GameObject.Find("robot_arm_rig_gripper");
                        armBase.transform.Translate(0f, 0.27f, 0f);

                        CurrentActiveController().ProcessControlCommand(
                            new Dictionary<string, object>{
                                {"action", "LookDown"}
                        });

                        break;
                    }

                case "sim": {
                        var collisionListener = this.CurrentActiveController().GetComponent<CollisionListener>();
                        Physics.Simulate(0.02f);
                        var l = collisionListener.StaticCollisions();
                        Debug.Log("total collisions: " + l.ToArray().Length);
                        break;
                    }

                case "inits": {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["action"] = "Initialize";
                        action["agentMode"] = "stretch";
                        action["agentControllerType"] = "stretch";
                        action["renderInstanceSegmentation"] = true;

                        ActionDispatcher.Dispatch(AManager, new DynamicServerAction(action));
                        //CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), AManager);

                        break;
                    }
                
                case "stretchtest1": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_1");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest2": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_2");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }
                
                case "stretchtest3": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_3");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest4": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_4");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest5": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_5");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest6": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_6");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest7": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_7");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest8": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_8");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }
                
                case "stretchtest9": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_9");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest10": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_10");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest11": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_11");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest12": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_12");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest13": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_13");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest14": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_14");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest15": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_15");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest16": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_16");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest17": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_17");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtest18": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_18");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }


                case "stretchtestu": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_u");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "stretchtestd": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_stretch_arm_d");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "iktest1": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_ik_arm_1");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "iktest2": {
                        List<string> commands = new List<string>();
                        commands.Add("run move_ik_arm_2");
                        //commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                }

                case "parent": {
                        Dictionary<string, object> action = new Dictionary<string, object>{
                        {"action", "ParentObject"},
                    };
                        action["parentId"] = splitcommand[1];
                        action["childId"] = splitcommand[2];

                        CurrentActiveController().ProcessControlCommand(
                            action
                        );
                        break;
                    }

                case "expspawn": {
                        ServerAction action = new ServerAction();

                        if (splitcommand.Length == 2) {
                            if (splitcommand[1] == "s") {
                                action.objectType = "screen";
                            }

                            if (splitcommand[1] == "r") {
                                action.objectType = "receptacle";
                            }
                        } else {
                            action.objectType = "receptacle";
                        }

                        action.action = "ReturnValidSpawnsExpRoom";
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";
                        action.objectVariation = 0;
                        action.y = 120f;// UnityEngine.Random.Range(0, 360);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Reads and executes each action from a JSON file.
                // Example: 'run', where a local file explorer will open, and you'll select a json file.
                // Example: 'run simple', where simple.json exists in unity/debug/.
                // This works best with Unity's Debugger for vscode (or other supported Unity IDEs).
                case "run":
                    // parse the file path
                    const string BASE_PATH = "./debug/";
                    string file = "";
                    string path;
                    if (splitcommand.Length == 1) {
                        // opens up a file explorer in the background
                        path = EditorUtility.OpenFilePanel(title: "Open JSON actions file.", directory: "debug", extension: "json");
                    } else if (splitcommand.Length == 2) {
                        // uses ./debug/{splitcommand[1]}[.json]
                        file = splitcommand[1].Trim();
                        if (!file.EndsWith(".json")) {
                            file += ".json";
                        }
                        path = BASE_PATH + file;
                    } else {
                        Debug.LogError("Pass in a file name, like 'run simple' or open a file with 'run'.");
                        break;
                    }

                    // parse the json file
                    string jsonString = System.IO.File.ReadAllText(path);
                    JArray actions = JArray.Parse(jsonString);
                    Debug.Log($"Running: {file}.json. It has {actions.Count} total actions.");

                    // execute each action
                    IEnumerator executeBatch(JArray jActions) {
                        int i = 0;
                        foreach (JObject action in jActions) {
                            while (CurrentActiveController().IsProcessing) {
                                yield return new WaitForEndOfFrame();
                            }
                            Debug.Log($"{++i} Executing: {action}");
                            if (AManager.agentManagerActions.Contains((String)action["action"])) {
                                ActionDispatcher.Dispatch(
                                    AManager,
                                    new DynamicServerAction(action)
                                );
                            } else {
                                CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action));
                            }
                        }
                    }
                    StartCoroutine(executeBatch(jActions: actions));
                    break;

                case "exp": {
                        ServerAction action = new ServerAction();

                        action.action = "SpawnExperimentObjAtRandom";
                        action.objectType = "receptacle";
                        action.randomSeed = 50;// UnityEngine.Random.Range(0, 1000);
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";
                        action.objectVariation = 12;
                        action.y = 120f;// UnityEngine.Random.Range(0, 360);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "exps": {
                        ServerAction action = new ServerAction();

                        action.action = "SpawnExperimentObjAtRandom";
                        action.objectType = "screen";
                        action.randomSeed = UnityEngine.Random.Range(0, 1000);
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";

                        if (splitcommand.Length == 2) {
                            action.objectVariation = int.Parse(splitcommand[1]);
                        } else {
                            action.objectVariation = 0;
                        }

                        action.y = 0f;// UnityEngine.Random.Range(0, 360);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "expp": {
                        ServerAction action = new ServerAction();

                        action.action = "SpawnExperimentObjAtPoint";
                        action.objectType = "replacement";//"receptacle";
                        action.receptacleObjectId = "DiningTable|-00.59|+00.00|+00.33";
                        action.objectVariation = 12;
                        action.position = new Vector3(-1.4f, 0.9f, 0.1f);
                        action.y = 120f;// UnityEngine.Random.Range(0, 360);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "wallcolor": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeWallColorExpRoom";
                        action.r = 100f;
                        action.g = 100f;
                        action.b = 100f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "floorcolor": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeFloorColorExpRoom";
                        action.r = 100f;
                        action.g = 100f;
                        action.b = 100f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "wallmaterial": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeWallMaterialExpRoom";
                        action["objectVariation"] = 1;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "floormaterial": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeFloorMaterialExpRoom";
                        action.objectVariation = 1;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "lightc": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeLightColorExpRoom";
                        action.r = 20f;
                        action.g = 94f;
                        action.b = 10f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "lighti": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeLightIntensityExpRoom";
                        action["intensity"] = 3;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "tabletopc": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableTopColorExpRoom";
                        action.r = 20f;
                        action.g = 94f;
                        action.b = 10f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "tabletopm": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableTopMaterialExpRoom";
                        action.objectVariation = 3;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "tablelegc": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableLegColorExpRoom";
                        action.r = 20f;
                        action.g = 94f;
                        action.b = 10f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "tablelegm": {
                        ServerAction action = new ServerAction();

                        action.action = "ChangeTableLegMaterialExpRoom";
                        action.objectVariation = 3;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "screenm": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeScreenMaterialExpRoom";
                        action["objectVariation"] = 3;
                        action["objectId"] = "ScreenSheet|-00.18|+01.24|+00.23";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "screenc": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ChangeScreenColorExpRoom";
                        action["r"] = 20f;
                        action["g"] = 94f;
                        action["b"] = 10f;
                        action["objectId"] = "ScreenSheet|-00.18|+01.24|+00.23";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "grid": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetReceptacleCoordinatesExpRoom";
                        action["gridSize"] = 0.1f;
                        action["maxStepCount"] = 5;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // initialize drone mode
                case "initd": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        // action.renderNormalsImage = true;
                        // action.renderDepthImage = true;
                        // action.renderSemanticSegmentation = true;
                        // action.renderInstanceSegmentation = true;
                        // action.renderFlowImage = true;

                        action["action"] = "Initialize";
                        action["agentMode"] = "drone";
                        action["agentControllerType"] = "drone";
                        ActionDispatcher.Dispatch(AManager, new DynamicServerAction(action));

                        break;
                    }

                // activate cracked camera effect with random seed
                case "cc": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "CameraCrack";

                        // give me a seed
                        if (splitcommand.Length == 2) {
                            action["randomSeed"] = int.Parse(splitcommand[1]);
                        } else {
                            action["randomSeed"] = 0;
                        }

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                // move ahead stochastic
                case "mas": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveAhead";

                        action.moveMagnitude = 0.25f;

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }
                case "rad": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetAgentRadius";
                        action["agentRadius"] = 0.35f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // This is dangerous because it will modify the underlying
                // materials, and you'll have to call "git restore *.mat *maT"
                // to revert the materials.
                case "dangerouslyChangeColor":
                    CurrentActiveController().ProcessControlCommand(new Dictionary<string, object>() {
                        ["action"] = "RandomizeColors"
                    });
                    break;
                case "resetColor":
                    CurrentActiveController().ProcessControlCommand(new Dictionary<string, object>() {
                        ["action"] = "ResetColors"
                    });
                    break;

                // This is dangerous because it will modify the underlying
                // materials, and you'll have to call "git restore *.mat *maT"
                // to revert the materials.
                case "dangerouslyChangeMaterial":
                    CurrentActiveController().ProcessControlCommand(new Dictionary<string, object>() {
                        ["action"] = "RandomizeMaterials"
                    });
                    break;
                case "resetMaterial":
                    CurrentActiveController().ProcessControlCommand(new Dictionary<string, object>() {
                        ["action"] = "ResetMaterials"
                    });
                    break;

                case "light": {
                        Dictionary<string, object> action = new Dictionary<string, object>() {
                            ["action"] = "RandomizeLighting",
                            ["synchronized"] = false,
                            ["brightness"] = new float[] { 0.5f, 1.5f },
                            ["hue"] = new float[] { 0, 1 },
                            ["saturation"] = new float[] { 0, 1 }
                        };
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "resetlight": {
                        Dictionary<string, object> action = new Dictionary<string, object>() {
                            ["action"] = "ResetLighting"
                        };
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "spawnabove": {
                        ServerAction action = new ServerAction();
                        action.action = "GetSpawnCoordinatesAboveReceptacle";
                        action.objectId = "CounterTop|-01.94|+00.98|-03.67";
                        action.anywhere = false;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "crazydiamond": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MakeObjectsOfTypeUnbreakable";
                        action["objectType"] = "Egg";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "stc": {
                        ServerAction action = new ServerAction();
                        action.action = "SpawnTargetCircle";
                        if (splitcommand.Length > 1) {
                            if (int.Parse(splitcommand[1]) == 0) {
                                action.objectVariation = 0;
                            }

                            if (int.Parse(splitcommand[1]) == 1) {
                                action.objectVariation = 1;
                            }

                            if (int.Parse(splitcommand[1]) == 2) {
                                action.objectVariation = 2;
                            }
                        }

                        action.anywhere = false;// false, only recepatcle objects in viewport used
                        // action.minDistance = 1.8f;
                        // action.maxDistance = 2.5f;
                        // action.objectId = "Floor|+00.00|+00.00|+00.00";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "smp": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetMassProperties";
                        action["objectId"] = "Pot|+00.30|+00.96|+01.35";
                        action["mass"] = 100;
                        action["drag"] = 100;
                        action["angularDrag"] = 100;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "pp": {
                        ExecuteAction("PausePhysicsAutoSim");
                        break;
                    }

                case "msa": {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(-0.771f, 0.993f, 0.8023f);
                        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        SphereCollider sc = go.GetComponent<SphereCollider>();
                        sc.isTrigger = true;
                        break;
                    }
                case "msr": {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(-0.771f, 0.87f, 0.6436f);
                        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        SphereCollider sc = go.GetComponent<SphereCollider>();
                        sc.isTrigger = true;
                        break;
                    }

                case "mach": {
                        var moveCall = moveArmHeightDebug(0.4f);
                        StartCoroutine(moveCall);
                        break;
                    }

                case "mafh": {
                        var moveCall = moveArmHeightDebug(0.4f);
                        while (moveCall.MoveNext()) {
                            // physics simulate happens in  updateTransformPropertyFixedUpdate as long
                            // as autoSimulation is off
                        }
                        break;
                    }
                case "macr": {
                        var target = new Vector3(0.1f, 0.0f, 0.4f);
                        var moveCall = moveArmDebug(target);
                        StartCoroutine(moveCall);
                        break;
                    }
                case "mafr": {
                        var target = new Vector3(0.1f, 0.0f, 0.4f);
                        var moveCall = moveArmDebug(target);
                        while (moveCall.MoveNext()) {
                            // physics simulate happens in  updateTransformPropertyFixedUpdate as long
                            // as autoSimulation is off
                        }

                        break;
                    }
                case "dumpwrist": {
                        var wristCol = GameObject.Find("robot_wrist_1_tcol (11)").transform;
                        dumpPosition(wristCol);
                        break;
                    }

                case "debugarm": {
                        var arm = CurrentActiveController().GetComponentInChildren<IK_Robot_Arm_Controller>();
                        ArmMetadata armmeta = arm.GenerateMetadata();
                        Debug.Log("last joint position");
                        Vector3 rrpos = armmeta.joints[armmeta.joints.Length - 1].rootRelativePosition;
                        Debug.Log("Root Relative Arm Position - x:" + rrpos.x.ToString("0.###") + " y:" + rrpos.y.ToString("0.###") + " z:" + rrpos.z.ToString("0.###"));
                        break;
                    }

                case "debugstretcharmjoints": {
                        var arm = CurrentActiveController().GetComponentInChildren<Stretch_Robot_Arm_Controller>();
                        ArmMetadata armmeta = arm.GenerateMetadata();
                        foreach (JointMetadata jm in armmeta.joints) {
                            Debug.Log(jm.name + " position: (" + jm.position.x + ", " + jm.position.y + ", " + jm.position.z + ")");
                            Debug.Log(jm.name + " root-relative position: (" + jm.rootRelativePosition.x + ", " + jm.rootRelativePosition.y + ", " + jm.rootRelativePosition.z + ")");
                            Debug.Log(jm.name + " rotation: " + jm.rotation);
                            Debug.Log(jm.name + " root-relative rotation: " + jm.rootRelativeRotation);
                            Debug.Log(jm.name + " local rotation: " + jm.localRotation);
                        }
                        break;
                    }

                case "debugarmjoints": {
                        var arm = CurrentActiveController().GetComponentInChildren<IK_Robot_Arm_Controller>();
                        ArmMetadata armmeta = arm.GenerateMetadata();
                        foreach (JointMetadata jm in armmeta.joints) {
                            Debug.Log(jm.name + " position: (" + jm.position.x + ", " + jm.position.y + ", " + jm.position.z + ")");
                            Debug.Log(jm.name + " root-relative position: (" + jm.rootRelativePosition.x + ", " + jm.rootRelativePosition.y + ", " + jm.rootRelativePosition.z + ")");
                            Debug.Log(jm.name + " rotation: " + jm.rotation);
                            Debug.Log(jm.name + " root-relative rotation: " + jm.rootRelativeRotation);
                            Debug.Log(jm.name + " local rotation: " + jm.localRotation);
                        }
                        break;
                    }

                case "posarm1": {
                        var arm = CurrentActiveController().GetComponentInChildren<IK_Robot_Arm_Controller>();
                        var armTarget = arm.transform.Find("robot_arm_FK_IK_rig").Find("IK_rig").Find("IK_pos_rot_manipulator");
                        armTarget.transform.position = new Vector3(-0.72564f, 0.901f, 0.72564f);
                        break;
                    }

                case "slide1": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_04_2021_16_44_01");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "slide2": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_04_2021_20_51_23");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }


                case "slide3": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_04_2021_23_31_11");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "slide4": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_05_2021_01_26_52");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "slide5": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_05_2021_02_58_54");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "slide6": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_05_2021_07_28_25");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "slide7": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("run 02_05_2021_08_36_10");
                        commands.Add("debugarmjoints");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "ras1": {
                        List<string> commands = new List<string>();
                        commands.Add("pp");
                        commands.Add("rr");
                        commands.Add("inita");
                        commands.Add("telefull -1.0 0.9009995460510254 1 135");
                        commands.Add("ld 20");
                        commands.Add("posarm1");
                        commands.Add("msr");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "ras2": {
                        List<string> commands = new List<string>();
                        commands.Add("pp");
                        commands.Add("inita");
                        commands.Add("telefull -1.0 0.9009995460510254 1 135");
                        commands.Add("ld 20");
                        commands.Add("posarm1");
                        commands.Add("msa");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }
                case "reproarmrot1": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("telefull -1.0 0.9009995460510254 1.5 90");
                        commands.Add("mmla 0.0 0.0 0.2 1.0");
                        commands.Add("debugarm");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "reproarmrot2": {
                        List<string> commands = new List<string>();
                        commands.Add("inita");
                        commands.Add("telefull -1.0 0.9009995460510254 1.5 45");
                        commands.Add("mmla 0.0 0.0 0.2 1.0");
                        commands.Add("debugarm");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }
                case "reproarmsphere3": {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(-0.6648f, 1.701f, 1.421f);
                        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        go.GetComponent<SphereCollider>().enabled = false;
                        go.AddComponent(typeof(MeshCollider));
                        MeshCollider mc = go.GetComponent<MeshCollider>();
                        mc.convex = true;
                        mc.isTrigger = true;
                        List<string> commands = new List<string>();
                        commands.Add("pp");
                        commands.Add("rr");
                        commands.Add("inita");
                        commands.Add("mmlah 1 1 True True");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }
                case "reproarmsphere2": {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(-0.6648f, 1.701f, 1.421f);
                        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        go.GetComponent<SphereCollider>().enabled = false;
                        go.AddComponent(typeof(MeshCollider));
                        List<string> commands = new List<string>();
                        commands.Add("pp");
                        commands.Add("rr");
                        commands.Add("inita");
                        commands.Add("mmlah 1 1 True True");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }
                case "reproarmsphere1": {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(-0.6648f, 1.701f, 1.421f);
                        go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                        SphereCollider sc = go.GetComponent<SphereCollider>();
                        sc.isTrigger = true;
                        List<string> commands = new List<string>();
                        commands.Add("pp");
                        commands.Add("rr");
                        commands.Add("inita");
                        commands.Add("mmlah 1 1 True True");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }
                case "reproarmstuck": {
                        List<string> commands = new List<string>();
                        commands.Add("pp");
                        commands.Add("rr");
                        commands.Add("inita");
                        commands.Add("mmlah 1 1 True True");
                        commands.Add("telefull");
                        commands.Add("mmlah 0.5203709825292535 2 True True");
                        commands.Add("mmla 0.01000303 -1.63912773e-06 0.558107364 2 armBase True False True");
                        StartCoroutine(ExecuteBatch(commands));
                        break;
                    }

                case "ap": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "AdvancePhysicsStep";
                        action["timeStep"] = 0.02f; // max 0.05, min 0.01
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "up": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "UnpausePhysicsAutoSim";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "its": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "InitializeTableSetting";
                        if (splitcommand.Length > 1) {
                            action["objectVariation"] = int.Parse(splitcommand[1]);
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "potwhcb": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PointsOverTableWhereHandCanBe";
                        action["objectId"] = splitcommand[1];
                        action["x"] = float.Parse(splitcommand[2]);
                        action["z"] = float.Parse(splitcommand[3]);

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "pfrat": {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["action"] = "PlaceFixedReceptacleAtLocation";
                        if (splitcommand.Length > 1) {
                            action["objectVariation"] = int.Parse(splitcommand[1]);
                            action["x"] = float.Parse(splitcommand[2]);
                            action["y"] = float.Parse(splitcommand[3]);
                            action["z"] = float.Parse(splitcommand[4]);
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "pbwal": {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["action"] = "PlaceBookWallAtLocation";
                        if (splitcommand.Length > 1) {
                            action["objectVariation"] = int.Parse(splitcommand[1]);
                            action["x"] = float.Parse(splitcommand[2]);
                            action["y"] = float.Parse(splitcommand[3]);
                            action["z"] = float.Parse(splitcommand[4]);
                            action["rotation"] = new Vector3(0f, float.Parse(splitcommand[5]), 0f);
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // random toggle state of all objects
                case "rts": {
                        ServerAction action = new ServerAction();

                        action.randomSeed = 0;
                        action.action = "RandomToggleStateOfAllObjects";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "rtss": {
                        ServerAction action = new ServerAction();

                        action.randomSeed = 0;
                        action.StateChange = "CanOpen";
                        action.action = "RandomToggleSpecificState";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "l": {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeLightSet";
                        if (splitcommand.Length == 2) {
                            action.objectVariation = int.Parse(splitcommand[1]);
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // set state of all objects that have a state
                case "ssa": {
                        ServerAction action = new ServerAction();

                        action.StateChange = "CanBeDirty";
                        action.forceAction = true;
                        action.action = "SetStateOfAllObjects";

                        if (splitcommand.Length > 1) {
                            if (splitcommand[1] == "t") {
                                action.forceAction = true;
                            }

                            if (splitcommand[1] == "f") {
                                action.forceAction = false;
                            }
                        }
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "initsynth": {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["renderNormalsImage"] = true;
                        action["renderDepthImage"] = true;
                        action["renderSemanticSegmentation"] = true;
                        action["renderInstanceSegmentation"] = true;
                        action["renderFlowImage"] = true;

                        // action.ssao = "default";

                        action["action"] = "Initialize";
                        ActionDispatcher.Dispatch(AManager, new DynamicServerAction(action));
                        break;
                    }

                case "atpc": {
                        Dictionary<string, object> action = new Dictionary<string, object>() {
                            ["action"] = "AddThirdPartyCamera",
                            ["position"] = Vector3.zero,
                            ["rotation"] = Vector3.zero,
                            ["orthographic"] = true,
                            ["orthographicSize"] = 5,
                        };

                        CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), AManager);
                        break;
                    }

                case "to": {
                        ServerAction action = new ServerAction();
                        action.action = "TeleportObject";
                        action.objectId = splitcommand[1];
                        action.x = float.Parse(splitcommand[2]);
                        action.y = float.Parse(splitcommand[3]);
                        action.z = float.Parse(splitcommand[4]);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "daoot": {
                        ServerAction action = new ServerAction();
                        action.action = "DisableAllObjectsOfType";
                        action.objectId = splitcommand[1];
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "ctlq": {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeQuality";
                        action.quality = "Very Low";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "roco": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomlyOpenCloseObjects";
                        action["randomSeed"] = (new System.Random()).Next(1, 1000000);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "crouch": {
                        ExecuteAction("Crouch");
                        break;
                    }
                case "stand": {
                        ExecuteAction("Stand");
                        break;
                    }

                case "remove": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RemoveFromScene";

                        if (splitcommand.Length == 2) {
                            action["objectId"] = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "putr": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";
                        action["objectId"] = ((PhysicsRemoteFPSAgentController)CurrentActiveController()).ObjectIdOfClosestReceptacleObject();
                        action["randomSeed"] = int.Parse(splitcommand[1]);

                        // set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        // set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action["placeStationary"] = false;

                        // set this true to ignore Placement Restrictions
                        action["forceAction"] = true;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "put": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";

                        if (splitcommand.Length == 2) {
                            action["objectId"] = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "putxy": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";

                        if (splitcommand.Length == 2) {
                            action["putNearXY"] = bool.Parse(splitcommand[1]);
                        }
                        // set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        // action["placeStationary"] = true;
                        action["x"] = 0.5f;
                        action["y"] = 0.5f;
                        // set this true to ignore Placement Restrictions
                        // action["forceAction"] = true;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "goif": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetObjectInFrame";
                        action["x"] = 0.5f;
                        action["y"] = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // put an object down with stationary false
                case "putf": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PutObject";

                        if (splitcommand.Length == 2) {
                            action["objectId"] = splitcommand[1];
                        } else {
                            action["objectId"] = PhysicsController.ObjectIdOfClosestReceptacleObject();
                        }

                        // set this to false if we want to place it and let physics resolve by having it fall a short distance into position

                        // set true to place with kinematic = true so that it doesn't fall or roll in place - making placement more consistant and not physics engine reliant - this more closely mimics legacy pivot placement behavior
                        action["placeStationary"] = false;

                        // set this true to ignore Placement Restrictions
                        action["forceAction"] = true;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // make all pickupable objects kinematic false so that they will react to collisions. Otherwise, some objects might be defaulted to kinematic true, or
                // if they were placed with placeStationary true, then they will not interact with outside collisions immediately.
                case "maom": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MakeAllObjectsMoveable";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "echoes": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MakeAllObjectsStationary";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "gip": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetInteractablePoses";
                        action["objectId"] = "Fridge|-02.10|+00.00|+01.09";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // tests the Reset function on AgentManager.cs, adding a path to it from the PhysicsController
                case "reset": {
                        ServerAction action = new ServerAction();
                        action.action = "Reset";
                        CurrentActiveController().ProcessControlCommand(action);
                        ExecuteAction("Reset");
                        break;
                    }

                case "poap": {
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
                            // adding y offset to position so that the downward raycast used in PlaceObjectAtPoint correctly collides with surface below point
                            action["position"] = hit.point + new Vector3(0, 0.5f, 0);
                            action["objectId"] = sop.ObjectID;

                            Debug.Log(action);
                            CurrentActiveController().ProcessControlCommand(action);
                        } else {
                            agent.actionFinished(false);
                        }

                        break;
                    }

                // set forceVisible to true if you want objects to not spawn inside receptacles and only out in the open
                // set forceAction to true to spawn with kinematic = true to more closely resemble pivot functionality
                case "irs": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "InitialRandomSpawn";

                        // List<string> excludedObjectIds = new List<string>();
                        // foreach (SimObjPhysics sop in AManager.agents[0].VisibleSimObjs(false)) {
                        //     excludedObjectIds.Add(sop.ObjectID);
                        // }
                        // action["excludedObjectIds"] = excludedObjectIds.ToArray();

                        // give me a seed
                        if (splitcommand.Length == 2) {
                            action["randomSeed"] = int.Parse(splitcommand[1]);
                            action["forceVisible"] = false;
                            action["numPlacementAttempts"] = 5;
                        }

                        // should objects be spawned only in immediately visible areas?
                        else if (splitcommand.Length == 3) {
                            action["randomSeed"] = int.Parse(splitcommand[1]);

                            if (splitcommand[2] == "t") {
                                action["forceVisible"] = true;
                            }

                            if (splitcommand[2] == "f") {
                                action["forceVisible"] = false;
                            }
                        } else if (splitcommand.Length == 4) {
                            action["randomSeed"] = int.Parse(splitcommand[1]);

                            if (splitcommand[2] == "t") {
                                action["forceVisible"] = true;
                            }

                            if (splitcommand[2] == "f") {
                                action["forceVisible"] = false;
                            }

                            action["numPlacementAttempts"] = int.Parse(splitcommand[3]);
                        } else {
                            action["randomSeed"] = 0;
                            action["forceVisible"] = false;// true;
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

                        action["placeStationary"] = true;// set to false to spawn with kinematic = false, set to true to spawn everything kinematic true and they won't roll around
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }
                case "spawn": {
                        ServerAction action = new ServerAction();
                        // Debug.Log(action.objectVariation);
                        int objectVariation = 0;
                        if (splitcommand.Length == 2) {
                            action.objectType = splitcommand[1];
                        } else if (splitcommand.Length == 3) {
                            action.objectType = splitcommand[1];
                            objectVariation = int.Parse(splitcommand[2]);
                        } else {
                            action.objectType = "Tomato";// default to spawn debug tomato

                        }
                        action.action = "CreateObject";
                        action.randomizeObjectAppearance = false;// pick randomly from available or not?
                        action.objectVariation = objectVariation;// if random false, which version of the object to spawn? (there are only 3 of each type atm)

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "neutral": {
                        ExecuteAction("ChangeAgentFaceToNeutral");
                        break;
                    }
                case "happy": {
                        ExecuteAction("ChangeAgentFaceToHappy");
                        break;
                    }

                case "mad": {
                        ExecuteAction("ChangeAgentFaceToMad");
                        break;
                    }

                case "supermad": {
                        ExecuteAction("ChangeAgentFaceToSuperMad");
                        break;
                    }

                case "ruaa": {
                        ServerAction action = new ServerAction();
                        action.action = "RotateUniverseAroundAgent";

                        action.rotation = new Vector3(
                            0f, float.Parse(splitcommand[1]), 0f
                        );
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "thas": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ToggleHideAndSeekObjects";

                        if (splitcommand.Length == 2) {
                            if (splitcommand[1] == "t") {
                                action["forceVisible"] = true;
                            }

                            if (splitcommand[1] == "f") {
                                action["forceVisible"] = false;
                            }
                        } else {
                            action["forceVisible"] = false;
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "spawnat": {
                        ServerAction action = new ServerAction();

                        if (splitcommand.Length > 1) {
                            action.objectType = splitcommand[1];
                            action.position = new Vector3(float.Parse(splitcommand[2]), float.Parse(splitcommand[3]), float.Parse(splitcommand[4]));
                            action.rotation = new Vector3(float.Parse(splitcommand[5]), float.Parse(splitcommand[6]), float.Parse(splitcommand[7]));
                            // action.rotation?
                        }

                        // default to zeroed out rotation tomato
                        else {
                            action.objectType = "Tomato";// default to spawn debug tomato
                            action.position = Vector3.zero;
                            action.rotation = Vector3.zero;
                        }
                        action.action = "CreateObjectAtLocation";

                        action.randomizeObjectAppearance = false;// pick randomly from available or not?
                        action.objectVariation = 1; // if random false, which version of the object to spawn? (there are only 3 of each type atm)

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "grpfo": {
                        ServerAction action = new ServerAction();
                        action.action = "GetReachablePositionsForObject";
                        action.objectId = splitcommand[1];
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "rspawnlifted": {
                        ServerAction action = new ServerAction();
                        action.action = "RandomlyCreateLiftedFurniture";
                        action.objectType = "Television";
                        action.objectVariation = 1;
                        action.y = 1.3f;
                        action.z = 1;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "rspawnfloor": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomlyCreateAndPlaceObjectOnFloor";
                        action["objectType"] = splitcommand[1];
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "spawnfloor": {
                        ServerAction action = new ServerAction();
                        int objectVariation = 1;
                        action.objectType = splitcommand[1];
                        action.x = float.Parse(splitcommand[2]);
                        action.z = float.Parse(splitcommand[3]);

                        action.action = "CreateObjectOnFloor";
                        action.objectVariation = objectVariation;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "gusfo": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetUnreachableSilhouetteForObject";
                        action["objectId"] = splitcommand[1];
                        action["z"] = float.Parse(splitcommand[2]);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "rhs": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RandomizeHideSeekObjects";
                        action["removeProb"] = float.Parse(splitcommand[1]);
                        action["randomSeed"] = int.Parse(splitcommand[2]);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "cts": {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeTimeScale";
                        action.timeScale = float.Parse(splitcommand[1]);
                        CurrentActiveController().ProcessControlCommand(action);

                        // NOTE: reachablePositions has been removed as a public variable
                        // Debug.Log(PhysicsController.reachablePositions.Length);

                        break;
                    }

                case "grp": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetReachablePositions";
                        // action.maxStepCount = 10;
                        if (splitcommand.Length == 2) {
                            action["directionsRelativeAgent"] = bool.Parse(splitcommand[1]);
                        }
                        CurrentActiveController().ProcessControlCommand(action);

                        // NOTE: reachablePositions has been removed as a public variable
                        // Debug.Log(PhysicsController.reachablePositions.Length);

                        break;
                    }

                case "grpb": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetReachablePositions";
                        // action.maxStepCount = 10;
                        CurrentActiveController().ProcessControlCommand(action);

                        // NOTE: reachablePositions has been removed as a public variable
                        // Debug.Log("stochastic grp " + StochasticController.reachablePositions.Length);

                        break;
                    }

                case "csw": {
                        ServerAction action = new ServerAction();
                        action.action = "CoverSurfacesWith";
                        // int objectVariation = 1;
                        // action.objectVariation = objectVariation;

                        if (splitcommand.Length == 2) {
                            action.objectType = splitcommand[1];
                        } else if (splitcommand.Length == 3) {
                            action.objectType = splitcommand[1];
                            action.objectVariation = int.Parse(splitcommand[2]);
                        } else {
                            action.objectType = "Tomato"; // default to spawn debug tomato

                        }
                        action.x = 0.3f;
                        action.z = 0.3f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fov": {
                        ServerAction action = new ServerAction();
                        action.action = "ChangeFOV";
                        action.fieldOfView = float.Parse(splitcommand[1]);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "teles": {
                        ServerAction action = new ServerAction();
                        action.action = "TeleportFull";
                        action.x = 4.42f;
                        action.y = 0.9009f;
                        action.z = -1.05f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "map": {
                        ExecuteAction("ToggleMapView");
                        break;
                    }
                case "nopfwoiv": {
                        ServerAction action = new ServerAction();
                        action.action = "NumberOfPositionsObjectsOfTypeAreVisibleFrom";
                        action.objectType = splitcommand[1];
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Close visible objects
                case "cvo": {
                        ExecuteAction("CloseVisibleObjects");
                        break;
                    }

                // Force open object at location
                case "oal": {
                        ServerAction action = new ServerAction();
                        action.action = "OpenObjectAtLocation";
                        action.x = float.Parse(splitcommand[1]);
                        action.y = float.Parse(splitcommand[2]);
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Get objects in box
                case "oib": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ObjectsInBox";
                        action["x"] = float.Parse(splitcommand[1]);
                        action["z"] = float.Parse(splitcommand[2]);
                        CurrentActiveController().ProcessControlCommand(action);
                        foreach (string s in PhysicsController.objectIdsInBox) {
                            Debug.Log(s);
                        }
                        break;
                    }

                // move ahead
                case "mg": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveGlobal";
                        action["x"] = float.Parse(splitcommand[1]);
                        action["z"] = float.Parse(splitcommand[2]);

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "move": {
                        Dictionary<string, object> action = new Dictionary<string, object>() {
                            ["action"] = "Move",
                            ["ahead"] = 0.25f
                        };
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "rotate": {
                        Dictionary<string, object> action = new Dictionary<string, object>() {
                            ["action"] = "Rotate",
                            ["degrees"] = 90
                        };
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move ahead
                case "ma": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveAhead";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else { action["moveMagnitude"] = 0.25f; }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move backward
                case "mb": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveBack";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else { action["moveMagnitude"] = 0.25f; }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move left
                case "ml": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveLeft";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else { action["moveMagnitude"] = 0.25f; }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move right
                case "mr": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveRight";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else { action["moveMagnitude"] = 0.25f; }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move ahead, force action true
                case "maf": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveAhead";

                        if (splitcommand.Length > 1) {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        } else {
                            action.moveMagnitude = 0.25f;
                        }

                        action.forceAction = true;

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                // move backward, force action true
                case "mbf": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveBack";

                        if (splitcommand.Length > 1) {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        } else {
                            action.moveMagnitude = 0.25f;
                        }

                        action.forceAction = true;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move left, force action true
                case "mlf": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveLeft";

                        if (splitcommand.Length > 1) {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        } else {
                            action.moveMagnitude = 0.25f;
                        }

                        action.forceAction = true;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move right, force action true
                case "mrf": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveRight";

                        if (splitcommand.Length > 1) {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        } else {
                            action.moveMagnitude = 0.25f;
                        }

                        action.forceAction = true;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fu": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyUp";
                        action["moveMagnitude"] = 2f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fd": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyDown";
                        action["moveMagnitude"] = 2f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fa": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyAhead";
                        action["moveMagnitude"] = 2f;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "fl": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyLeft";
                        action["moveMagnitude"] = 2f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fr": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyRight";
                        action["moveMagnitude"] = 2f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fb": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "FlyBack";
                        action["moveMagnitude"] = 2f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // look up
                case "lu": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "LookUp";

                        if (splitcommand.Length > 1) {
                            action["degrees"] = float.Parse(splitcommand[1]);
                        }

                        // action.manualInteract = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // stochastic look up
                case "lus": {
                        ServerAction action = new ServerAction();
                        action.action = "LookUp";

                        if (splitcommand.Length > 1) {
                            action.degrees = float.Parse(splitcommand[1]);
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // look down
                case "ld": {
                        ServerAction action = new ServerAction();
                        action.action = "LookDown";

                        if (splitcommand.Length > 1) {
                            action.degrees = float.Parse(splitcommand[1]);
                        }

                        // action.manualInteract = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // stochastic look down
                case "lds": {
                        ServerAction action = new ServerAction();
                        action.action = "LookDown";

                        if (splitcommand.Length > 1) {
                            action.degrees = float.Parse(splitcommand[1]);
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // rotate left
                case "rl": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RotateLeft";


                        if (splitcommand.Length > 1) {
                            action["degrees"] = float.Parse(splitcommand[1]);
                        }

                        // action.manualInteract = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // rotate left stochastic
                case "rls": {
                        ServerAction action = new ServerAction();
                        action.action = "RotateLeft";

                        if (splitcommand.Length > 1) {
                            action.degrees = float.Parse(splitcommand[1]);
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // rotate right
                case "rr": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "RotateRight";


                        if (splitcommand.Length > 1) {
                            action["degrees"] = float.Parse(splitcommand[1]);
                        }

                        // action.manualInteract = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // rotate right stochastic
                case "rrs": {
                        ServerAction action = new ServerAction();
                        action.action = "RotateRight";

                        if (splitcommand.Length > 1) {
                            action.degrees = float.Parse(splitcommand[1]);
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // pickup object
                case "pu": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PickupObject";
                        if (splitcommand.Length > 1) {
                            action["objectId"] = splitcommand[1];
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Force pickup object
                case "fpu": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PickupObject";
                        if (splitcommand.Length > 1) {
                            action["objectId"] = splitcommand[1];
                        }
                        action["forceAction"] = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                // pickup using screen coordinates
                case "puxy": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PickupObject";
                        // action.forceAction = true;
                        action["x"] = 0.5f;
                        action["y"] = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // manual pickup object- test hand
                case "pum": {
                        ServerAction action = new ServerAction();
                        action.action = "PickupObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } else {
                            action.objectId = closestVisibleObjectId();
                        }

                        action.manualInteract = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "slice": {
                        ServerAction action = new ServerAction();
                        action.action = "SliceObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } 
                        action.x = 0.5f;
                        action.y = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "break": {
                        ServerAction action = new ServerAction();
                        action.action = "BreakObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } 
                        action.x = 0.5f;
                        action.y = 0.5f;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "dirtyobject": {
                        ServerAction action = new ServerAction();
                        action.action = "DirtyObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } 
                        action.x = 0.5f;
                        action.y = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "cleanobject": {
                        ServerAction action = new ServerAction();
                        action.action = "CleanObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } else {
                            action.objectId = closestVisibleObjectId();
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fillwater": {
                        ServerAction action = new ServerAction();
                        action.action = "FillObjectWithLiquid";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } else {
                            action.objectId = closestVisibleObjectId(); 
                        }

                        action.fillLiquid = "water";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fillcoffee": {
                        ServerAction action = new ServerAction();
                        action.action = "FillObjectWithLiquid";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } else {
                            action.objectId = closestVisibleObjectId();
                        }

                        action.fillLiquid = "coffee";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // map view props
                case "mvp": {
                        var action = new Dictionary<string, object>() {
                            ["action"] = "GetMapViewCameraProperties"
                        };
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "fillwine": {
                        ServerAction action = new ServerAction();
                        action.action = "FillObjectWithLiquid";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } else {
                            action.objectId = closestVisibleObjectId();
                        }

                        action.fillLiquid = "wine";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "emptyliquid": {
                        ServerAction action = new ServerAction();
                        action.action = "EmptyLiquidFromObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } else {
                            action.objectId = closestVisibleObjectId();
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "useup": {
                        ServerAction action = new ServerAction();
                        action.action = "UseUpObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        } 

                        action.x = 0.5f;
                        action.y = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // drop object
                case "dr": {
                        ServerAction action = new ServerAction();
                        action.action = "DropHandObject";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // force drop object
                case "fdr": {
                        ServerAction action = new ServerAction();
                        action.action = "DropHandObject";
                        action.forceAction = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // rotate object in hand, pass in desired x/y/z rotation
                case "ro": {
                        ServerAction action = new ServerAction();
                        action.action = "RotateHand";
                        if (splitcommand.Length > 1) {
                            action.x = float.Parse(splitcommand[1]);
                            action.y = float.Parse(splitcommand[2]);
                            action.z = float.Parse(splitcommand[3]);
                            CurrentActiveController().ProcessControlCommand(action);
                        }

                        break;
                    }

                case "ror": {
                        ServerAction action = new ServerAction();
                        action.action = "RotateHandRelative";
                        if (splitcommand.Length > 1) {
                            action.x = float.Parse(splitcommand[1]);
                            action.y = float.Parse(splitcommand[2]);
                            action.z = float.Parse(splitcommand[3]);
                            CurrentActiveController().ProcessControlCommand(action);
                        }

                        break;
                    }

                // default the Hand's position and rotation to the starting position and rotation
                case "dh": {
                        ServerAction action = new ServerAction();
                        action.action = "DefaultAgentHand";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "tta": {
                        ServerAction action = new ServerAction();
                        action.action = "TouchThenApplyForce";
                        action.x = 0.5f;
                        action.y = 0.5f;
                        action.handDistance = 2.0f;
                        action.direction = new Vector3(0, 0, 1);
                        action.moveMagnitude = 800f;

                        if (splitcommand.Length > 1) {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move hand ahead, forward relative to agent's facing
                // pass in move magnitude or default is 0.25 units
                case "mha": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandAhead";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else {
                            action["moveMagnitude"] = 0.1f;
                        }

                        // action.x = 0f;
                        // action.y = 0f;
                        // action.z = 1f;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move hand backward. relative to agent's facing
                // pass in move magnitude or default is 0.25 units
                case "mhb": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandBack";


                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else {
                            action["moveMagnitude"] = 0.1f;
                        }

                        // action.x = 0f;
                        // action.y = 0f;
                        // action.z = -1f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move hand left, relative to agent's facing
                // pass in move magnitude or default is 0.25 units
                case "mhl": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandLeft";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else {
                            action["moveMagnitude"] = 0.1f;
                        }

                        // action.x = -1f;
                        // action.y = 0f;
                        // action.z = 0f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move hand right, relative to agent's facing
                // pass in move magnitude or default is 0.25 units
                case "mhr": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandRight";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else {
                            action["moveMagnitude"] = 0.1f;
                        }

                        // action.x = 1f;
                        // action.y = 0f;
                        // action.z = 0f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // do nothing action
                case "pass":
                case "done":
                case "noop": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "NoOp";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Short circuiting exception test
                case "sc": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "OpenObject";
                        action["x"] = 1.5;
                        action["y"] = 0.5;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move hand up, relative to agent's facing
                // pass in move magnitude or default is 0.25 units
                case "mhu": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandUp";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else {
                            action["moveMagnitude"] = 0.1f;
                        }

                        // action.x = 0f;
                        // action.y = 1f;
                        // action.z = 0f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move hand down, relative to agent's facing
                // pass in move magnitude or default is 0.25 units
                case "mhd": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveHandDown";

                        if (splitcommand.Length > 1) {
                            action["moveMagnitude"] = float.Parse(splitcommand[1]);
                        } else {
                            action["moveMagnitude"] = 0.1f;
                        }

                        // action.x = 0f;
                        // action.y = -1f;
                        // action.z = 0f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // changes the time spent to decay to room temperature for all objects in this scene of given type
                case "DecayTimeForType": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetRoomTempDecayTimeForType";

                        action["TimeUntilRoomTemp"] = 20f;
                        action["objectType"] = "Bread";
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                // changes the time spent to decay to room temperature for all objects globally in the scene
                case "DecayTimeGlobal": {
                        ServerAction action = new ServerAction();
                        action.action = "SetGlobalRoomTempDecayTime";

                        action.TimeUntilRoomTemp = 20f;
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "SetTempDecayBool": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "SetDecayTemperatureBool";

                        action["allowDecayTemperature"] = false;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // throw object by dropping it and applying force.
                // default is with strength of 120, can pass in custom magnitude of throw force
                case "throw": {
                        ServerAction action = new ServerAction();
                        action.action = "ThrowObject";

                        if (splitcommand.Length > 1) {
                            action.moveMagnitude = float.Parse(splitcommand[1]);
                        } else {
                            action.moveMagnitude = 120f;
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "push": {
                        ServerAction action = new ServerAction();
                        action.action = "PushObject";

                        action.moveMagnitude = 2000f;

                        if (splitcommand.Length > 1 && splitcommand.Length < 3) {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = 200f;// 4000f;
                        } else if (splitcommand.Length > 2) {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = float.Parse(splitcommand[2]);
                        } 

                        action.x = 0.5f;
                        action.y = 0.5f;

                        action.z = 1;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "pull": {
                        ServerAction action = new ServerAction();
                        action.action = "PullObject";

                        action.moveMagnitude = 2000f;

                        if (splitcommand.Length > 1 && splitcommand.Length < 3) {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = 200f;// 4000f;
                        } else if (splitcommand.Length > 2) {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = float.Parse(splitcommand[2]);
                        } else {
                            action.objectId = ((PhysicsRemoteFPSAgentController)AManager.PrimaryAgent).ObjectIdOfClosestPickupableOrMoveableObject();
                        }

                        // action.moveMagnitude = 200f;// 4000f;
                        action.z = -1;
                        action.x = 0.5f;
                        action.y = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "dirpush": {
                        ServerAction action = new ServerAction();
                        action.action = "DirectionalPush";

                        if (splitcommand.Length > 1 && splitcommand.Length < 3) {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = 10f;// 4000f;
                        } else if (splitcommand.Length > 2) {
                            action.objectId = splitcommand[1];
                            action.moveMagnitude = float.Parse(splitcommand[2]);
                        }

                        action.pushAngle = 279f;
                        action.moveMagnitude = 159f;
                        action.x = 0.5f;
                        action.y = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "toggleon": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ToggleObjectOn";
                        if (splitcommand.Length > 1) {
                            action["objectId"] = splitcommand[1];
                        } else {
                            action["x"] = 0.5f;
                            action["y"] = 0.5f;
                        }

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "toggleoff": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ToggleObjectOff";
                        if (splitcommand.Length > 1) {
                            action["objectId"] = splitcommand[1];
                        } else {
                            action["x"] = 0.5f;
                            action["y"] = 0.5f;
                        }

                        // action.objectId = "DeskLamp|-01.32|+01.24|-00.99";
                        action["forceVisible"] = true;
                        action["forceAction"] = true;
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "cook": {
                        ServerAction action = new ServerAction();
                        action.action = "CookObject";
                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        action.x = 0.5f;
                        action.y = 0.5f;
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "sos": {
                        ServerAction action = new ServerAction();
                        action.action = "SetObjectStates";
                        action.SetObjectStates = new SetObjectStates() {
                            stateChange = "toggleable",
                            objectType = "DeskLamp",
                            isToggled = false
                        };

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "pose": {
                        ServerAction action = new ServerAction();
                        action.action = "SetObjectPoses";
                        action.objectPoses = new ObjectPose[1];

                        action.objectPoses[0] = new ObjectPose();

                        action.objectPoses[0].objectName = "Book_3d15d052";
                        action.objectPoses[0].position = new Vector3(0, 0, 0);
                        action.objectPoses[0].rotation = new Vector3(0, 0, 0);


                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                // opens given object the given percent, default is 100% open
                // open <object ID> percent
                case "open": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "OpenObject";
                        action["forceAction"] = true;

                        if (splitcommand.Length == 1) {
                            // try opening object in front of the agent
                            action["openness"] = 0.5f;
                            action["x"] = 0.5f;
                            action["y"] = 0.5f;
                        } else if (splitcommand.Length == 2) {
                            // default open 100%
                            action["objectId"] = splitcommand[1];
                        } else if (splitcommand.Length == 3) {
                            // give the open percentage as 3rd param, from 0.0 to 1.0
                            action["objectId"] = splitcommand[1];
                            action["openness"] = float.Parse(splitcommand[2]);
                        } 

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "openim": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "OpenObjectImmediate";

                        action["objectId"] = "Cabinet|-00.73|+02.02|-02.46";

                        action["openness"] = 1f;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "closeim": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "OpenObjectImmediate";

                        action["objectId"] = "Cabinet|-00.73|+02.02|-02.46";

                        action["openness"] = 0f;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "close": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "CloseObject";

                        if (splitcommand.Length > 1) {
                            action["objectId"] = splitcommand[1];
                        } else {
                            action["x"] = 0.5f;
                            action["y"] = 0.5f;
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // pass in object id of a receptacle, and this will report any other sim objects inside of it
                // this works for cabinets, drawers, countertops, tabletops, etc.
                case "contains": {
                        ServerAction action = new ServerAction();
                        action.action = "Contains";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }


                //*****************************************************************************
                // MASS SCALE ACTIONS HERE
                //*****************************************************************************

                // get total mass in right scale of MassScale sim obj
                case "rscalemass": {
                        ServerAction action = new ServerAction();
                        action.action = "MassInRightScale";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // get total mass in left scale of MassScale sim obj
                case "lscalemass": {
                        ServerAction action = new ServerAction();
                        action.action = "MassInLeftScale";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // get total count of objects in right scale of MassScale sim obj
                case "rscalecount": {
                        ServerAction action = new ServerAction();
                        action.action = "CountInRightScale";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // get total count of objects in the left scale of MassScale sim obj
                case "lscalecount": {
                        ServerAction action = new ServerAction();
                        action.action = "CountInLeftScale";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // get list of all sim objects in the right scale of MassScale sim obj
                case "rscaleobjs": {
                        ServerAction action = new ServerAction();
                        action.action = "ObjectsInRightScale";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // get list of all sim objects in the Left scale of MassScale sim obj
                case "lscaleobjs": {
                        ServerAction action = new ServerAction();
                        action.action = "ObjectsInLeftScale";

                        if (splitcommand.Length > 1) {
                            action.objectId = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Will fail if navmeshes are not setup
                case "expact": {
                        ServerAction action = new ServerAction();
                        action.action = "ObjectNavExpertAction";

                        if (splitcommand.Length == 2) {
                            // ID of spawner
                            action.objectType = splitcommand[1];
                        }
                        else if (splitcommand.Length >= 4) {
                            // Target position
                            action.position = new Vector3(float.Parse(splitcommand[1]), float.Parse(splitcommand[2]), float.Parse(splitcommand[3]));
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // Will fail if navmeshes are not setup
                case "shortest_path": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetShortestPath";

                        // pass in a min range, max range, delay
                        if (splitcommand.Length > 1) {
                            // ID of spawner
                            action["objectId"] = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action["position"] = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]),
                                    float.Parse(splitcommand[4])
                                );
                            }
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "shortest_path_type": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetShortestPath";

                        // pass in a min range, max range, delay
                        if (splitcommand.Length > 1) {
                            // ID of spawner
                            action["objectType"] = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action["position"] = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]),
                                    float.Parse(splitcommand[4])
                                );
                            }
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "shortest_path_point": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetShortestPathToPoint";

                        // pass in a min range, max range, delay
                        if (splitcommand.Length > 1) {
                            // ID of spawner
                            // action.objectId = splitcommand[1];

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
                        } else {
                            throw new ArgumentException("need to provide at least 3 floats for target position");
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "visualize_path": {
                        ServerAction action = new ServerAction();
                        action.action = "VisualizePath";
                        action.objectId = "0";

                        // pass in a min range, max range, delay
                        if (splitcommand.Length > 1) {
                            // ID of spawner
                            action.objectId = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action.position = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]),
                                    float.Parse(splitcommand[4])
                                );
                            } else {
                                action.positions = new List<Vector3>() {
                                    new Vector3( 4.258f, 1.0f, -1.69f),
                                    new Vector3(6.3f, 1.0f, -3.452f)
                                };
                            }
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "vp": {
                        ServerAction action = new ServerAction();
                        action.action = "VisualizeShortestPaths";

                        // pass in a min range, max range, delay
                        if (splitcommand.Length > 1) {
                            // ID of spawner
                            action.objectType = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action.position = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]),
                                    float.Parse(splitcommand[4])
                                );
                            } else {
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

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "visualize_shortest_path": {
                        ServerAction action = new ServerAction();
                        action.action = "VisualizeShortestPaths";

                        // pass in a min range, max range, delay
                        if (splitcommand.Length > 1) {
                            // ID of spawner
                            action.objectType = splitcommand[1];

                            if (splitcommand.Length == 5) {
                                action.position = new Vector3(
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3]),
                                    float.Parse(splitcommand[4])
                                );
                            } else {
                                // var pos = PhysicsController.getReachablePositions().Shuffle();
                                action.positions = new List<Vector3>() { PhysicsController.transform.position };
                                action.grid = true;
                            }
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "get_object_type_ids": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ObjectTypeToObjectIds";
                        if (splitcommand.Length > 1) {
                            action["objectType"] = splitcommand[1];
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "move_mid_arm":
                case "mmla": {
                        // Limit around -0.084
                        //"mmla 0 0 0.08 0.1 false wrist true"
                        ServerAction action = new ServerAction();
                        action.action = "MoveMidLevelArm";
                        action.speed = 1.0f;
                        action.disableRendering = false;
                        // action.returnToStart = true;
                        if (splitcommand.Length > 4) {
                            action.position = new Vector3(
                                    float.Parse(splitcommand[1]),
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3])
                                );

                            if (splitcommand.Length >= 5) {
                                action.speed = float.Parse(splitcommand[4]);
                            }

                            if (splitcommand.Length >= 6) {
                                action.coordinateSpace = splitcommand[5];
                            }

                            if (splitcommand.Length >= 7) {
                                action.returnToStart = bool.Parse(splitcommand[6]);
                            }

                            if (splitcommand.Length >= 8) {
                                action.restrictMovement = bool.Parse(splitcommand[7]);
                            }

                            if (splitcommand.Length >= 9) {
                                action.disableRendering = bool.Parse(splitcommand[8]);
                            }

                        } else {
                            Debug.LogError("Target x y z args needed for command");
                        }
                        // action.stopArmMovementOnContact = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                // move mid level arm stop motion
                case "mmlas": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveMidLevelArm";
                        action.speed = 1.0f;
                        // action.returnToStart = true;
                        if (splitcommand.Length > 4) {
                            action.position = new Vector3(
                                    float.Parse(splitcommand[1]),
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3])
                                );

                            if (splitcommand.Length >= 5) {
                                action.speed = float.Parse(splitcommand[4]);
                            }

                            if (splitcommand.Length >= 6) {
                                action.returnToStart = bool.Parse(splitcommand[5]);
                            }

                            if (splitcommand.Length >= 7) {
                                action.handCameraSpace = bool.Parse(splitcommand[6]);
                            }
                        } else {
                            Debug.LogError("Target x y z args needed for command");
                        }
                        action.stopArmMovementOnContact = true;
                        // action.stopArmMovementOnContact = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "pac": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "DebugMidLevelArmCollisions";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "mmlah": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "MoveArmBase";
                        action["disableRendering"] = false;

                        if (splitcommand.Length > 1) {
                            action["y"] = float.Parse(splitcommand[1]);

                            if (splitcommand.Length > 2) {
                                action["speed"] = float.Parse(splitcommand[2]);
                            }
                            if (splitcommand.Length > 3) {
                                action["returnToStart"] = bool.Parse(splitcommand[3]);
                            }

                            if (splitcommand.Length > 4) {
                                action["disableRendering"] = bool.Parse(splitcommand[4]);
                            }

                            if (splitcommand.Length > 5) {
                                action["restrictMovement"] = bool.Parse(splitcommand[5]);
                            }
                        } else {
                            action["y"] = 0.9f;
                            action["speed"] = 1.0f;
                        }
                        action["disableRendering"] = true;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "gcfr": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "GetCoordinateFromRaycast";
                        action["x"] = float.Parse(splitcommand[1]);
                        action["y"] = float.Parse(splitcommand[2]);

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "telefull": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "TeleportFull";
                        action["x"] = -1.5f;
                        action["y"] = 0.9009995460510254f;
                        action["z"] = -1.5f;
                        Vector3 rotation = new Vector3(0, 135.0f, 0);
                        int horizon = 0;
                        bool standing = true;

                        if (splitcommand.Length > 1 && splitcommand.Length < 5) {
                            action["x"] = float.Parse(splitcommand[1]);
                            action["y"] = float.Parse(splitcommand[2]);
                            action["z"] = float.Parse(splitcommand[3]);
                            // rotation = float.Parse(splitcommand[4]);
                        } else if (splitcommand.Length > 5) {
                            action["x"] = float.Parse(splitcommand[1]);
                            action["y"] = float.Parse(splitcommand[2]);
                            action["z"] = float.Parse(splitcommand[3]);
                            // rotation = float.Parse(splitcommand[4]);
                            horizon = int.Parse(splitcommand[5]);
                        }

                        action["rotation"] = rotation;
                        action["horizon"] = horizon;
                        action["standing"] = standing;

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "expfit": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "WhichContainersDoesAvailableObjectFitIn";
                        action["objectName"] = "AlarmClock_dd21c3db";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "scale": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "ScaleObject";
                        action["objectId"] = "Box|+00.00|+01.33|-00.44";
                        action["scale"] = 0.6746510624885559f;
                        action["scaleOverSeconds"] = 0f;
                        action["forceAction"] = true;

                        if (splitcommand.Length > 1) {
                            action["scale"] = float.Parse(splitcommand[1]);
                        }
                        if (splitcommand.Length > 2) {
                            action["scaleOverSeconds"] = float.Parse(splitcommand[2]);
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "closepoints": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PointOnObjectsCollidersClosestToPoint";
                        action["objectId"] = "Dumbbell|+00.00|+00.90|+00.00";
                        action["point"] = new Vector3(0f, 1000f, 0f);

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "mc": {
                        ServerAction action = new ServerAction();
                        action.action = "MoveContinuous";
                        if (splitcommand.Length > 4) {
                            action.direction = new Vector3(
                                    float.Parse(splitcommand[1]),
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3])
                                );

                            if (splitcommand.Length >= 5) {
                                action.speed = float.Parse(splitcommand[4]);
                            }
                        }

                        action.disableRendering = true;
                        action.restrictMovement = false;
                        action.returnToStart = true;
                        action.speed = 1;
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "rc": {
                        dynamic action = new JObject();
                        action.action = "RotateContinuous";
                        if (splitcommand.Length > 2) {
                            action.degrees = float.Parse(splitcommand[1]);

                            if (splitcommand.Length >= 3) {
                                action.speed = float.Parse(splitcommand[2]);
                            }

                            if (splitcommand.Length >= 4) {
                                action.disableRendering = bool.Parse(splitcommand[3]);
                            }

                            if (splitcommand.Length >= 5) {
                                action.returnToStart = bool.Parse(splitcommand[4]);
                            }
                        }

                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                default: {
                        Dictionary<string, object> action = new Dictionary<string, object>();

                        action["action"] = splitcommand[0];
                        if (splitcommand.Length == 2) {
                            action["objectId"] = splitcommand[1];
                        } else if (splitcommand.Length == 3) {
                            action["x"] = float.Parse(splitcommand[1]);
                            action["z"] = float.Parse(splitcommand[2]);
                        } else if (splitcommand.Length == 4) {
                            action["x"] = float.Parse(splitcommand[1]);
                            action["y"] = float.Parse(splitcommand[2]);
                            action["z"] = float.Parse(splitcommand[3]);
                        }
                        CurrentActiveController().ProcessControlCommand(action);
                        // Debug.Log("Invalid Command");
                        break;
                    }

                // rotate kinematic hand on arm
                case "rmlh": {
                        ServerAction action = new ServerAction();

                        action.action = "RotateMidLevelHand";

                        if (splitcommand.Length > 4) {
                            action.rotation = new Vector3(
                                    float.Parse(splitcommand[1]),
                                    float.Parse(splitcommand[2]),
                                    float.Parse(splitcommand[3])
                                );

                            if (splitcommand.Length >= 5) {
                                action.speed = float.Parse(splitcommand[4]);
                            }
                        }

                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }

                case "pumlh": {
                        Dictionary<string, object> action = new Dictionary<string, object>();
                        action["action"] = "PickUpMidLevelHand";
                        action["objectIds"] = new List<string> { "Bread|-00.52|+01.17|-00.03", "Apple|-00.54|+01.15|+00.18" };
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }

                case "dmlh": {
                        ServerAction action = new ServerAction();
                        action.action = "DropMidLevelHand";
                        CurrentActiveController().ProcessControlCommand(action);
                        break;
                    }
                case "smlhr": {
                        ServerAction action = new ServerAction();
                        action.action = "SetHandSphereRadius";

                        if (splitcommand.Length == 2) {
                            action.radius = float.Parse(splitcommand[1]);
                        }
                        CurrentActiveController().ProcessControlCommand(action);

                        break;
                    }
            }

            // StartCoroutine(CheckIfactionCompleteWasSetToTrueAfterWaitingALittleBit(splitcommand[0]));

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

        // used to show what's currently visible on the top left of the screen
        void OnGUI() {
            if (CurrentActiveController().VisibleSimObjPhysics != null && this.controlMode != ControlMode.MINIMAL_FPS) {
                if (CurrentActiveController().VisibleSimObjPhysics.Length > 10) {
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
                    // Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);

                    // int position_number = 0;
                    foreach (SimObjPhysics o in CurrentActiveController().VisibleSimObjPhysics) {
                        string suffix = "";
                        // Bounds bounds = new Bounds(o.gameObject.transform.position, new Vector3(0.05f, 0.05f, 0.05f));
                        // if (GeometryUtility.TestPlanesAABB(planes, bounds)) {
                        //     // position_number += 1;

                        //     // if (o.GetComponent<SimObj>().Manipulation == SimObjManipProperty.Inventory)
                        //     //    suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

                        //     // else
                        //     // suffix += " VISIBLE";
                        //     // if(!IgnoreInteractableFlag)
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

