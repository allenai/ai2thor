using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System.Text;

public class MCSMain : MonoBehaviour {
    private static string PATH_PREFIX = "MCS/";
    private static int LATEST_SCENE_VERSION = 2;

    private static float CUBE_INTERNAL_GRID = 0.25f;
    private static float FLOOR_SCALE_X = 11f;
    private static float FLOOR_SCALE_Y = 0.5f;
    private static float FLOOR_SCALE_Z = 11f;
    private static float INTUITIVE_PHYSICS_FLOOR_SCALE_X = 40f;
    private static float INTUITIVE_PHYSICS_PERFORMER_START_POSITION_Y = 1.5f;
    private static float INTUITIVE_PHYSICS_PERFORMER_START_POSITION_Z = -4.5f;
    private static float INTUITIVE_PHYSICS_WALL_FRONT_POSITION_Y = 3f;
    private static float INTUITIVE_PHYSICS_WALL_FRONT_SCALE_X = 40f;
    private static float INTUITIVE_PHYSICS_WALL_FRONT_SCALE_Y = 6f;
    private static float INTUITIVE_PHYSICS_WALL_LEFT_POSITION_X = -20.0f;
    private static float INTUITIVE_PHYSICS_WALL_RIGHT_POSITION_X = 20.0f;
    private static float ISOMETRIC_FLOOR_SCALE_X = 20f;
    private static float ISOMETRIC_FLOOR_SCALE_Z = 20f;
    private static float ISOMETRIC_PERFORMER_START_POSITION_X = 4f;
    private static float ISOMETRIC_PERFORMER_START_POSITION_Y = 3f;
    private static float ISOMETRIC_PERFORMER_START_POSITION_Z = -4f;
    private static float ISOMETRIC_PERFORMER_START_ROTATION_X = 35.264f;
    private static float ISOMETRIC_PERFORMER_START_ROTATION_Y = -45f;
    private static float ISOMETRIC_WALL_BACK_FRONT_SCALE_X = 4f;
    private static float ISOMETRIC_WALL_BACK_FRONT_SCALE_Z = 0.5f;
    private static float ISOMETRIC_WALL_BACK_POSITION_X = 0f;
    private static float ISOMETRIC_WALL_BACK_POSITION_Z = -2.25f;
    private static float ISOMETRIC_WALL_FRONT_POSITION_X = 0f;
    private static float ISOMETRIC_WALL_FRONT_POSITION_Z = 2.25f;
    private static float ISOMETRIC_WALL_LEFT_POSITION_X = -2.25f;
    private static float ISOMETRIC_WALL_LEFT_POSITION_Z = 0f;
    private static float ISOMETRIC_WALL_LEFT_RIGHT_SCALE_X = 0.5f;
    private static float ISOMETRIC_WALL_LEFT_RIGHT_SCALE_Z = 5f;
    private static float ISOMETRIC_WALL_RIGHT_POSITION_X = 2.25f;
    private static float ISOMETRIC_WALL_RIGHT_POSITION_Z = 0f;
    private static float ISOMETRIC_WALL_POSITION_Y = 0.0625f;
    private static float ISOMETRIC_WALL_SCALE_Y = 0.125f;
    private static float LIGHT_RANGE = 20f;
    private static float LIGHT_RANGE_SCREENSHOT = 10f;
    private static float LIGHT_Y_POSITION = 2.95f;
    private static float LIGHT_Y_POSITION_SCREENSHOT = 0.5f;
    private static float LIGHT_Z_POSITION = 0;
    private static float LIGHT_Z_POSITION_SCREENSHOT = -2.0f;
    private static float PHYSICS_FRICTION_DYNAMIC_DEFAULT = 0.6f;
    private static float PHYSICS_FRICTION_DYNAMIC_PASSIVE = 0.1f;
    private static float PHYSICS_FRICTION_STATIC_DEFAULT = 0.6f;
    private static float PHYSICS_FRICTION_STATIC_PASSIVE = 0.1f;
    private static float PHYSICS_BOUNCINESS_DEFAULT = 0;
    private static float RIGIDBODY_DRAG_DEFAULT = 0;
    private static float RIGIDBODY_ANGULAR_DRAG_DEFAULT = 0.5f;
    private static float WALL_BACK_FRONT_POSITION_X = 0;
    private static float WALL_BACK_FRONT_SCALE_X = 11.0f;
    private static float WALL_BACK_FRONT_SCALE_Z = 0.5f;
    private static float WALL_BACK_POSITION_Z = -5.25f;
    private static float WALL_FRONT_POSITION_Z = 5.25f;
    private static float WALL_LEFT_POSITION_X = -5.25f;
    private static float WALL_LEFT_RIGHT_POSITION_Z = 0;
    private static float WALL_LEFT_RIGHT_SCALE_X = 0.5f;
    private static float WALL_LEFT_RIGHT_SCALE_Z = 11.0f;
    private static float WALL_POSITION_Y = 1.5f;
    private static float WALL_RIGHT_POSITION_X = 5.25f;
    private static float WALL_SCALE_Y = 3.0f;
    private static Vector3 DEFAULT_ROOM_DIMENSIONS = new Vector3(10, 3, 10);
    public string defaultSceneFile = "";
    public bool enableVerboseLog = false;
    public bool enableDebugLogsInEditor = true;
    public string ai2thorObjectRegistryFile = "ai2thor_object_registry";
    public string mcsObjectRegistryFile = "mcs_object_registry";
    public string primitiveObjectRegistryFile = "primitive_object_registry";
    public string defaultCeilingMaterial = "AI2-THOR/Materials/Walls/Drywall";
    public string defaultFloorMaterial = "AI2-THOR/Materials/Fabrics/CarpetWhite 3";
    public string defaultWallsMaterial = "AI2-THOR/Materials/Walls/DrywallBeige";
    public bool isPassiveScene = false;

    private MCSConfigScene currentScene;
    private int lastStep = -1;
    private Dictionary<String, MCSConfigObjectDefinition> objectDictionary =
        new Dictionary<string, MCSConfigObjectDefinition>();

    // AI2-THOR Objects and Scripts
    private MCSController agentController;
    private GameObject objectParent;
    private PhysicsSceneManager physicsSceneManager;

    // Room objects
    private GameObject ceiling;
    private GameObject floor;
    private GameObject light;
    private GameObject light2;
    private GameObject wallLeft;
    private GameObject wallRight;
    private GameObject wallFront;
    private GameObject wallBack;

    public static MCSConfigScene LoadCurrentSceneFromFile(String filePath) {
        TextAsset currentSceneFile = Resources.Load<TextAsset>(MCSMain.PATH_PREFIX + "Scenes/" + filePath);
        Debug.Log("MCS: Config file Assets/Resources/MCS/Scenes/" + filePath + ".json" + (currentSceneFile == null ?
            " is null!" : (":\n" + currentSceneFile.text)));
        return JsonUtility.FromJson<MCSConfigScene>(currentSceneFile.text);
    }

    // Unity's Start method is called before the first frame update
    void Start() {
        this.agentController = GameObject.Find("FPSController").GetComponent<MCSController>();
        this.objectParent = GameObject.Find("Objects");
        this.physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
        this.ceiling = GameObject.Find("Ceiling");
        this.floor = GameObject.Find("Floor");
        this.light = GameObject.Find("Point light");
        this.light2 = GameObject.Find("Spot light");
        this.wallLeft = GameObject.Find("Wall Left");
        this.wallRight = GameObject.Find("Wall Right");
        this.wallFront = GameObject.Find("Wall Front");
        this.wallBack = GameObject.Find("Wall Back");

        // Disable all physics simulation (we re-enable it on each step in MachineCommonSenseController).
        Physics.autoSimulation = false;
        this.physicsSceneManager.physicsSimulationPaused = true;

        // Load the configurable game objects from our custom registry files.
        List<MCSConfigObjectDefinition> ai2thorObjects = LoadObjectRegistryFromFile(
            this.ai2thorObjectRegistryFile);
        List<MCSConfigObjectDefinition> mcsObjects = LoadObjectRegistryFromFile(
            this.mcsObjectRegistryFile);
        List<MCSConfigObjectDefinition> primitiveObjects = LoadObjectRegistryFromFile(
            this.primitiveObjectRegistryFile);
        ai2thorObjects.Concat(mcsObjects).Concat(primitiveObjects).ToList().ForEach((objectDefinition) => {
            this.objectDictionary.Add(objectDefinition.id.ToUpper(), objectDefinition);
        });

        // Load the default MCS scene set in the Unity Editor.
        if (!this.defaultSceneFile.Equals("")) {
            this.currentScene = MCSMain.LoadCurrentSceneFromFile(this.defaultSceneFile);
            this.currentScene.name = ((this.currentScene.name == null || this.currentScene.name.Equals("")) ?
                this.defaultSceneFile : this.currentScene.name);
            this.currentScene.version = (this.currentScene.version > 0 ? this.currentScene.version :
                MCSMain.LATEST_SCENE_VERSION);
            ChangeCurrentScene(this.currentScene);
        }

        // We should always have debug logs enabled in debug builds.
        if (Debug.isDebugBuild) {
            Debug.unityLogger.logEnabled = true;
        }
        else {
#if ENABLE_DEBUG_LOGS
            Debug.unityLogger.logEnabled = true;
#else
            Debug.unityLogger.logEnabled = false;
#endif
        }

#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = enableDebugLogsInEditor;
#endif
    }

    // Unity's Update method is called once per frame.
    void Update() {
        // If the player made a step, update the scene based on the current configuration.
        if (this.lastStep < this.agentController.step) {
            this.lastStep++;
            Debug.Log("MCS: Run Step " + this.lastStep + " at Frame " + Time.frameCount);
            if (this.currentScene != null && this.currentScene.objects != null) {
                // update segmentation mask colors
                ImageSynthesis imageSynthesis = GameObject.Find("FPSController").GetComponentInChildren<ImageSynthesis>();
                if (imageSynthesis != null && imageSynthesis.enabled) {
                    imageSynthesis.UpdateGuidForColors(this.agentController.agentManager.consistentColors);
                    imageSynthesis.OnSceneChange();
                }
                bool objectsWereShown = false;
                List<MCSConfigGameObject> objects = this.currentScene.objects.Where(objectConfig =>
                    objectConfig.GetGameObject() != null).ToList();

                // Loop over each configuration object in the scene and update if needed.
                objects.ForEach(objectConfig => {
                    objectsWereShown = this.UpdateGameObjectOnStep(objectConfig, this.lastStep) || objectsWereShown;
                });

                this.PostUpdateGameObjectOnStep(objects, this.lastStep);

                // If new objects were added to the scene...
                if (objectsWereShown) {
                    // Notify the PhysicsSceneManager so the objects will be compatible with AI2-THOR scripts.
                    this.physicsSceneManager.SetupScene();
                    // Notify ImageSynthesis so the objects will appear in the masks.
                    if (imageSynthesis != null && imageSynthesis.enabled) {
                        imageSynthesis.OnSceneChange();
                    }
                }
            }
            
            // Objects aren't fully added in Start(), so we need to adjust the location here in case we are on a platform.
            if (this.lastStep == 0 && !this.isPassiveScene) {
                GameObject controller = GameObject.Find("FPSController");
                controller.GetComponent<MCSController>().MatchAgentHeightToStructureBelow(false);
            }
            this.agentController.SimulatePhysics();
        }
    }

    // Custom Public Methods

    public void ChangeCurrentScene(MCSConfigScene scene) {
        if (scene == null && this.currentScene == null) {
            Debug.LogError("MCS: Cannot switch the MCS scene to null... Keeping the current MCS scene.");
            return;
        }

        if (this.currentScene != null && this.currentScene.objects != null) {
            this.currentScene.objects.ForEach(objectConfig => {
                GameObject gameOrParentObject = objectConfig.GetParentObject() ?? objectConfig.GetGameObject();
                Destroy(gameOrParentObject);
            });
        }

        if (scene != null) {
            this.currentScene = scene;
            Debug.Log("MCS: Switching the current MCS scene to " + scene.name);
        } else {
            Debug.Log("MCS: Resetting the current MCS scene...");
        }

        if (this.currentScene != null && this.currentScene.objects != null) {
            Debug.Log("MCS: Initializing " + this.currentScene.objects.Count + " objects...");
            this.currentScene.objects.ForEach(InitializeGameObject);
        } else {
            Debug.Log("MCS: No objects to initialize!");
        }

        this.isPassiveScene = (this.currentScene.intuitivePhysics || this.currentScene.observation ||
                this.currentScene.isometric);
        this.AdjustRoomStructuralObjects();

        if (this.currentScene.goal != null && this.currentScene.goal.description != null) {
            Debug.Log("MCS: Goal = " + this.currentScene.goal.description);
        }

        GameObject controller = GameObject.Find("FPSController");
        if (this.currentScene.performerStart != null && this.currentScene.performerStart.position != null) {
            // Always keep the Y position on the floor.
            controller.transform.position = new Vector3(this.currentScene.performerStart.position.x,
                this.currentScene.performerStart.position.y, this.currentScene.performerStart.position.z);
        } else {
            controller.transform.position = new Vector3(0, this.currentScene.performerStart.position.y, 0);
        }

        if (this.currentScene.performerStart != null && this.currentScene.performerStart.rotation != null) {
            // Only permit rotating left or right (along the Y axis).
            controller.transform.rotation = Quaternion.Euler(0, this.currentScene.performerStart.rotation.y, 0);
            controller.GetComponent<MCSController>().m_Camera.transform.localEulerAngles = new Vector3(
                this.currentScene.performerStart.rotation.x, 0, 0);
        } else {
            controller.transform.rotation = Quaternion.Euler(0, 0, 0);
            controller.GetComponent<MCSController>().m_Camera.transform.localEulerAngles = new Vector3(
                0, 0, 0);
        }

        this.lastStep = -1;
        this.physicsSceneManager.SetupScene();
    }

    // Custom Private Methods

    private void AdjustRoomStructuralObjects() {
        String ceilingMaterial = (this.currentScene.ceilingMaterial != null &&
            !this.currentScene.ceilingMaterial.Equals("")) ? this.currentScene.ceilingMaterial :
            this.defaultCeilingMaterial;
        String floorMaterial = (this.currentScene.floorMaterial != null &&
            !this.currentScene.floorMaterial.Equals("")) ? this.currentScene.floorMaterial :
            this.defaultFloorMaterial;
        String myDefaultWallMaterial = (this.currentScene.wallMaterial != null &&
            !this.currentScene.wallMaterial.Equals("")) ? this.currentScene.wallMaterial :
            this.defaultWallsMaterial;
        // get material for each wall

        String leftWallMaterial = (this.currentScene.roomMaterials?.left != null && 
            !this.currentScene.roomMaterials.left.Equals("") ? 
            this.currentScene.roomMaterials.left : 
            myDefaultWallMaterial);
        String rightWallMaterial = (this.currentScene.roomMaterials?.right != null && 
            !this.currentScene.roomMaterials.right.Equals("") ? 
            this.currentScene.roomMaterials.right : 
            myDefaultWallMaterial);
        String frontWallMaterial = (this.currentScene.roomMaterials?.front != null && 
            !this.currentScene.roomMaterials.front.Equals("") ? 
            this.currentScene.roomMaterials.front : 
            myDefaultWallMaterial);
        String backWallMaterial = (this.currentScene.roomMaterials?.back != null && 
            !this.currentScene.roomMaterials.back.Equals("") ? 
            this.currentScene.roomMaterials.back : 
            myDefaultWallMaterial);

        // Remove the ceiling from all intuitive physics and isometric scenes.
        this.ceiling.SetActive(!(this.currentScene.intuitivePhysics || this.currentScene.observation ||
                this.currentScene.isometric));

        // Set the controller's action substeps to 1 in all intuitive physics and isometric scenes.
        if (this.currentScene.intuitivePhysics || this.currentScene.observation || this.currentScene.isometric) {
            this.agentController.substeps = 1;
        }

        // Expand the walls of the room in all intuitive physics scenes and set a specific performer start.
        if (this.currentScene.intuitivePhysics || this.currentScene.observation) {
            this.wallLeft.transform.position = new Vector3(MCSMain.INTUITIVE_PHYSICS_WALL_LEFT_POSITION_X,
                MCSMain.WALL_POSITION_Y, MCSMain.WALL_LEFT_RIGHT_POSITION_Z);
            this.wallLeft.transform.localScale = new Vector3(MCSMain.WALL_LEFT_RIGHT_SCALE_X,
                MCSMain.WALL_SCALE_Y, MCSMain.WALL_LEFT_RIGHT_SCALE_Z);

            this.wallRight.transform.position = new Vector3(MCSMain.INTUITIVE_PHYSICS_WALL_RIGHT_POSITION_X,
                MCSMain.WALL_POSITION_Y, MCSMain.WALL_LEFT_RIGHT_POSITION_Z);
            this.wallRight.transform.localScale = new Vector3(MCSMain.WALL_LEFT_RIGHT_SCALE_X,
                MCSMain.WALL_SCALE_Y, MCSMain.WALL_LEFT_RIGHT_SCALE_Z);

            this.wallFront.transform.position = new Vector3(MCSMain.WALL_BACK_FRONT_POSITION_X,
                MCSMain.INTUITIVE_PHYSICS_WALL_FRONT_POSITION_Y, MCSMain.WALL_FRONT_POSITION_Z);
            this.wallFront.transform.localScale = new Vector3(MCSMain.INTUITIVE_PHYSICS_WALL_FRONT_SCALE_X,
                MCSMain.INTUITIVE_PHYSICS_WALL_FRONT_SCALE_Y, MCSMain.WALL_BACK_FRONT_SCALE_Z);

            this.wallBack.transform.position = new Vector3(MCSMain.WALL_BACK_FRONT_POSITION_X,
                MCSMain.WALL_POSITION_Y, MCSMain.WALL_BACK_POSITION_Z);
            this.wallBack.transform.localScale = new Vector3(MCSMain.WALL_BACK_FRONT_SCALE_X,
                MCSMain.WALL_SCALE_Y, MCSMain.WALL_BACK_FRONT_SCALE_Z);

            this.floor.transform.localScale = new Vector3(MCSMain.INTUITIVE_PHYSICS_FLOOR_SCALE_X,
                MCSMain.FLOOR_SCALE_Y, MCSMain.FLOOR_SCALE_Z);

            this.currentScene.performerStart = new MCSConfigTransform();
            this.currentScene.performerStart.position = new MCSConfigVector();
            this.currentScene.performerStart.position.y = MCSMain.INTUITIVE_PHYSICS_PERFORMER_START_POSITION_Y;
            this.currentScene.performerStart.position.z = MCSMain.INTUITIVE_PHYSICS_PERFORMER_START_POSITION_Z;
            this.currentScene.performerStart.rotation = new MCSConfigVector();

            if (this.currentScene.floorProperties == null || !this.currentScene.floorProperties.enable) {
                this.currentScene.floorProperties = new MCSConfigPhysicsProperties();
                this.currentScene.floorProperties.enable = true;
                this.currentScene.floorProperties.dynamicFriction = MCSMain.PHYSICS_FRICTION_DYNAMIC_PASSIVE;
                this.currentScene.floorProperties.staticFriction = MCSMain.PHYSICS_FRICTION_STATIC_PASSIVE;
                this.currentScene.floorProperties.bounciness = MCSMain.PHYSICS_BOUNCINESS_DEFAULT;
                this.currentScene.floorProperties.drag = MCSMain.RIGIDBODY_DRAG_DEFAULT;
                this.currentScene.floorProperties.angularDrag = MCSMain.RIGIDBODY_ANGULAR_DRAG_DEFAULT;
            }
        } else if (this.currentScene.isometric) {
            this.currentScene.performerStart = new MCSConfigTransform();
            this.currentScene.performerStart.position = new MCSConfigVector();
            this.currentScene.performerStart.position.x = MCSMain.ISOMETRIC_PERFORMER_START_POSITION_X;
            this.currentScene.performerStart.position.y = MCSMain.ISOMETRIC_PERFORMER_START_POSITION_Y;
            this.currentScene.performerStart.position.z = MCSMain.ISOMETRIC_PERFORMER_START_POSITION_Z;
            this.currentScene.performerStart.rotation = new MCSConfigVector();
            this.currentScene.performerStart.rotation.x = MCSMain.ISOMETRIC_PERFORMER_START_ROTATION_X;
            this.currentScene.performerStart.rotation.y = MCSMain.ISOMETRIC_PERFORMER_START_ROTATION_Y;

            this.wallLeft.transform.position = new Vector3(MCSMain.ISOMETRIC_WALL_LEFT_POSITION_X,
                MCSMain.ISOMETRIC_WALL_POSITION_Y, MCSMain.ISOMETRIC_WALL_LEFT_POSITION_Z);
            this.wallLeft.transform.localScale = new Vector3(MCSMain.ISOMETRIC_WALL_LEFT_RIGHT_SCALE_X,
                MCSMain.ISOMETRIC_WALL_SCALE_Y, MCSMain.ISOMETRIC_WALL_LEFT_RIGHT_SCALE_Z);

            this.wallRight.transform.position = new Vector3(MCSMain.ISOMETRIC_WALL_RIGHT_POSITION_X,
                MCSMain.ISOMETRIC_WALL_POSITION_Y, MCSMain.ISOMETRIC_WALL_RIGHT_POSITION_Z);
            this.wallRight.transform.localScale = new Vector3(MCSMain.ISOMETRIC_WALL_LEFT_RIGHT_SCALE_X,
                MCSMain.ISOMETRIC_WALL_SCALE_Y, MCSMain.ISOMETRIC_WALL_LEFT_RIGHT_SCALE_Z);

            this.wallFront.transform.position = new Vector3(MCSMain.ISOMETRIC_WALL_FRONT_POSITION_X,
                MCSMain.ISOMETRIC_WALL_POSITION_Y, MCSMain.ISOMETRIC_WALL_FRONT_POSITION_Z);
            this.wallFront.transform.localScale = new Vector3(MCSMain.ISOMETRIC_WALL_BACK_FRONT_SCALE_X,
                MCSMain.ISOMETRIC_WALL_SCALE_Y, MCSMain.ISOMETRIC_WALL_BACK_FRONT_SCALE_Z);

            this.wallBack.transform.position = new Vector3(MCSMain.ISOMETRIC_WALL_BACK_POSITION_X,
                MCSMain.ISOMETRIC_WALL_POSITION_Y, MCSMain.ISOMETRIC_WALL_BACK_POSITION_Z);
            this.wallBack.transform.localScale = new Vector3(MCSMain.ISOMETRIC_WALL_BACK_FRONT_SCALE_X,
                MCSMain.ISOMETRIC_WALL_SCALE_Y, MCSMain.ISOMETRIC_WALL_BACK_FRONT_SCALE_Z);

            this.floor.transform.localScale = new Vector3(MCSMain.ISOMETRIC_FLOOR_SCALE_X,
                MCSMain.FLOOR_SCALE_Y, MCSMain.ISOMETRIC_FLOOR_SCALE_Z);
        } else {
            float wallWidth = .5f;
            SetRoomInternalSize(currentScene.roomDimensions, wallWidth);

            if (this.currentScene.performerStart == null) {
                this.currentScene.performerStart = new MCSConfigTransform();
            }
            if (this.currentScene.performerStart.position == null) {
                this.currentScene.performerStart.position = new MCSConfigVector();
            }
            if (currentScene.performerStart.position.y < MCSController.STANDING_POSITION_Y) { 
                this.currentScene.performerStart.position.y = MCSController.STANDING_POSITION_Y;
            }
        }

        SimObjPhysics ceilingSimObjPhysics = this.ceiling.GetComponent<SimObjPhysics>();
        SimObjPhysics floorSimObjPhysics = this.floor.GetComponent<SimObjPhysics>();
        SimObjPhysics wallLeftSimObjPhysics = this.wallLeft.GetComponent<SimObjPhysics>();
        SimObjPhysics wallRightSimObjPhysics = this.wallRight.GetComponent<SimObjPhysics>();
        SimObjPhysics wallFrontSimObjPhysics = this.wallFront.GetComponent<SimObjPhysics>();
        SimObjPhysics wallBackSimObjPhysics = this.wallBack.GetComponent<SimObjPhysics>();

        this.light2.SetActive(false);

        if (this.currentScene.screenshot) {
            this.floor.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallLeft.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallRight.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallFront.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallBack.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.light.GetComponent<Light>().range = MCSMain.LIGHT_RANGE_SCREENSHOT;
            this.light.transform.position = new Vector3(0, MCSMain.LIGHT_Y_POSITION_SCREENSHOT,
                MCSMain.LIGHT_Z_POSITION_SCREENSHOT);
        } else {
            // Intuitive physics and isometric scenes don't have ceilings.
            if (!(this.currentScene.intuitivePhysics || this.currentScene.observation || this.currentScene.isometric)) {
                AssignMaterial(this.ceiling, ceilingMaterial);
            }

            AssignMaterial(this.floor, floorMaterial);
            AssignMaterial(this.wallLeft, leftWallMaterial);
            AssignMaterial(this.wallRight, rightWallMaterial);
            AssignMaterial(this.wallFront, frontWallMaterial);
            AssignMaterial(this.wallBack, backWallMaterial);

            this.light.GetComponent<Light>().range = MCSMain.LIGHT_RANGE;
            this.light.transform.position = new Vector3(0, MCSMain.LIGHT_Y_POSITION,
                MCSMain.LIGHT_Z_POSITION);

            // Intuitive physics scenes have a second light source.
            if (this.currentScene.intuitivePhysics || this.currentScene.observation) {
                this.light2.SetActive(true);
            }
        }

        if (this.currentScene.wallProperties != null && this.currentScene.wallProperties.enable) {
            AssignPhysicsMaterialAndRigidBodyValues(currentScene.wallProperties, this.wallLeft, wallLeftSimObjPhysics);
            AssignPhysicsMaterialAndRigidBodyValues(currentScene.wallProperties, this.wallFront, wallFrontSimObjPhysics);
            AssignPhysicsMaterialAndRigidBodyValues(currentScene.wallProperties, this.wallRight, wallRightSimObjPhysics);
            AssignPhysicsMaterialAndRigidBodyValues(currentScene.wallProperties, this.wallBack, wallBackSimObjPhysics);
            AssignPhysicsMaterialAndRigidBodyValues(currentScene.wallProperties, this.ceiling, ceilingSimObjPhysics);
        }

        if (this.currentScene.floorProperties != null && this.currentScene.floorProperties.enable) {
            AssignPhysicsMaterialAndRigidBodyValues(this.currentScene.floorProperties, this.floor, floorSimObjPhysics);
        }

        if (this.currentScene.goal != null && this.currentScene.goal.description != null) {
            Debug.Log("MCS: Goal = " + this.currentScene.goal.description);
        }

        GameObject controller = GameObject.Find("FPSController");
        if (this.currentScene.performerStart != null && this.currentScene.performerStart.position != null) {
            // Always keep the Y position on the floor.
            controller.transform.position = new Vector3(this.currentScene.performerStart.position.x,
                MCSController.STANDING_POSITION_Y, this.currentScene.performerStart.position.z);
        } else {
            controller.transform.position = new Vector3(0, MCSController.STANDING_POSITION_Y, 0);
        }

        if (this.currentScene.performerStart != null && this.currentScene.performerStart.rotation != null) {
            // Only permit rotating left or right (along the Y axis).
            controller.transform.rotation = Quaternion.Euler(0, this.currentScene.performerStart.rotation.y, 0);
            controller.GetComponent<MCSController>().m_Camera.transform.localEulerAngles = new Vector3(
                this.currentScene.performerStart.rotation.x, 0, 0);
        } else {
            controller.transform.rotation = Quaternion.Euler(0, 0, 0);
            controller.GetComponent<MCSController>().m_Camera.transform.localEulerAngles = new Vector3(
                0, 0, 0);
        }

        this.lastStep = -1;
        this.physicsSceneManager.SetupScene();
    }

    //Sets a room to have the given dimensions between the walls, floor and ceiling.  
    //The walls, floor, and ceiling will also have a width equal to the wall width.
    private void SetRoomInternalSize(Vector3 roomDimensions, float wallWidth) {
        Vector3 wallWidths = new Vector3(wallWidth, wallWidth, wallWidth);
        if (roomDimensions == null || roomDimensions == Vector3.zero) {
            roomDimensions = DEFAULT_ROOM_DIMENSIONS;
        }
        Vector3 roomHalfDimensions = roomDimensions * .5f;
        Vector3 wallHalfWidths = wallWidths * .5f;
        this.agentController.substeps = MCSController.PHYSICS_SIMULATION_LOOPS;

        this.wallLeft.transform.position = new Vector3(-roomHalfDimensions.x - wallHalfWidths.x,
            roomHalfDimensions.y, MCSMain.WALL_LEFT_RIGHT_POSITION_Z);
        this.wallLeft.transform.localScale = new Vector3(wallWidths.x,
            roomDimensions.y, roomDimensions.z + wallWidths.z * 2);

        this.wallRight.transform.position = new Vector3(roomHalfDimensions.x + wallHalfWidths.x,
            roomHalfDimensions.y, MCSMain.WALL_LEFT_RIGHT_POSITION_Z);
        this.wallRight.transform.localScale = new Vector3(wallWidths.x,
            roomDimensions.y, roomDimensions.z + wallWidths.z * 2);

        this.wallFront.transform.position = new Vector3(MCSMain.WALL_BACK_FRONT_POSITION_X,
            roomHalfDimensions.y, roomHalfDimensions.z + wallHalfWidths.z);
        this.wallFront.transform.localScale = new Vector3(roomDimensions.x + wallWidths.x * 2,
            roomDimensions.y, wallWidths.z);

        this.wallBack.transform.position = new Vector3(MCSMain.WALL_BACK_FRONT_POSITION_X,
            roomHalfDimensions.y, -roomHalfDimensions.z - wallHalfWidths.z);
        this.wallBack.transform.localScale = new Vector3(roomDimensions.x + wallWidths.x * 2,
            roomDimensions.y, wallWidths.z);

        this.floor.transform.localScale = new Vector3(roomDimensions.x + wallWidths.x * 2,
            MCSMain.FLOOR_SCALE_Y, roomDimensions.z + wallWidths.z * 2);
        this.ceiling.transform.localScale = new Vector3(roomDimensions.x + wallWidths.x * 2,
            MCSMain.FLOOR_SCALE_Y, roomDimensions.z + wallWidths.z * 2);
        this.ceiling.transform.position = new Vector3(0, roomDimensions.y + wallHalfWidths.y, 0);
        agentController.agentManager.ResetSceneBounds();
    }

    private Collider AssignBoundingBox(
        GameObject gameObject,
        MCSConfigCollider colliderDefinition
    ) {
        // The AI2-THOR bounding box property is always a box collider.
        colliderDefinition.type = "box";
        // The AI2-THOR scripts assume the bounding box collider object is always called BoundingBox.
        GameObject boundingBoxObject = new GameObject {
            isStatic = true,
            layer = 9, // AI2-THOR Layer SimObjInvisible
            name = "BoundingBox"
        };
        boundingBoxObject.transform.parent = gameObject.transform;
        Collider boundingBox = AssignCollider(boundingBoxObject, colliderDefinition);
        // The AI2-THOR documentation says to deactive the bounding box collider.
        boundingBox.enabled = false;
        return boundingBox;
    }

    private Collider AssignCollider(
        GameObject gameObject,
        MCSConfigCollider colliderDefinition
    ) {
        this.AssignTransform(gameObject, colliderDefinition);

        if (colliderDefinition.type.Equals("box")) {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;
            // The assignToColliderItself flag is useful if you want to rotate the collider around the center.
            // We may want to do this by default, but that will need more testing across all existing objects.
            if (colliderDefinition.assignToColliderItself) {
                boxCollider.center = gameObject.transform.localPosition;
                boxCollider.size = gameObject.transform.localScale;
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localScale = Vector3.one;
            }
            LogVerbose("ASSIGN BOX COLLIDER TO GAME OBJECT " + gameObject.name);
            return boxCollider;
        }

        if (colliderDefinition.type.Equals("capsule")) {
            CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            capsuleCollider.center = Vector3.zero;
            capsuleCollider.height = colliderDefinition.height;
            capsuleCollider.radius = colliderDefinition.radius;
            LogVerbose("ASSIGN CAPSULE COLLIDER TO GAME OBJECT " + gameObject.name);
            return capsuleCollider;
        }

        if (colliderDefinition.type.Equals("mesh")) {
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = gameObject.GetComponentInParent<MeshFilter>().sharedMesh;
            return meshCollider;
        }

        if (colliderDefinition.type.Equals("sphere")) {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.center = Vector3.zero;
            sphereCollider.radius = colliderDefinition.radius;
            return sphereCollider;
        }

        return null;
    }

    private Collider[] AssignColliders(
        GameObject gameObject,
        MCSConfigObjectDefinition objectDefinition
    ) {
        // We don't care about existing trigger colliders here so just ignore them.
        Collider[] colliders = gameObject.GetComponentsInChildren<Collider>().Where((collider) =>
            !collider.isTrigger).ToArray();

        MCSConfigLegacyObjectDefinition legacy = this.RetrieveLegacyObjectDefinition(objectDefinition,
            this.currentScene.version);
        List<MCSConfigCollider> collidersFromConfig = legacy != null && legacy.colliders.Count > 0 ?
            legacy.colliders : objectDefinition.colliders;

        if (collidersFromConfig.Count > 0) {
            // Deactivate any concave MeshCollider.  We expect other collider(s) to be defined on the object.
            // Concave MeshCollider cause Unity errors with our object's ContinuousDynamic Rigidbody component.
            colliders.ToList().ForEach((collider) => {
                if (collider is MeshCollider) {
                    if (!((MeshCollider)collider).convex) {
                        // TODO Do we need to do more?
                        Debug.LogWarning("MCS: Deactivating concave MeshCollider in GameObject " + gameObject.name);
                        collider.enabled = false;
                    }
                }
            });

            // If new colliders are defined for the object, deactivate the existing colliders.
            if (!objectDefinition.keepColliders) {
                colliders.ToList().ForEach((collider) => {
                    collider.enabled = false;
                });
                colliders = new Collider[] { };
            }

            // The AI2-THOR scripts assume the colliders have a parent object with the name Colliders.
            GameObject colliderParentObject = new GameObject {
                isStatic = true,
                name = "Colliders"
            };
            colliderParentObject.transform.parent = gameObject.transform;
            colliderParentObject.transform.localPosition = Vector3.zero;
            int index = 0;
            colliders = colliders.ToList().Concat(collidersFromConfig.Select((colliderDefinition) => {
                ++index;
                GameObject colliderObject = new GameObject {
                    isStatic = true,
                    layer = 8, // AI2-THOR Layer SimObjVisible
                    name = gameObject.name + "_collider_" + index,
                    tag = "SimObjPhysics" // AI2-THOR Tag
                };
                colliderObject.transform.parent = colliderParentObject.transform;
                Collider collider = this.AssignCollider(colliderObject, colliderDefinition);
                // If this uses a mesh collider, reset the parent's scale, else the collider won't be the correct size.
                if (collider is MeshCollider) {
                    colliderParentObject.transform.localScale = Vector3.one;
                }
                return collider;
            })).ToArray();
        } else {
            // Else, add the AI2-THOR layer and tag to the existing colliders so they work with the AI2-THOR scripts.
            colliders.ToList().ForEach((collider) => {
                collider.gameObject.layer = 8; // AI2-THOR Layer SimObjVisible
                collider.gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            });
        }

        return colliders;
    }

    private Material LoadMaterial(string filename, string[] restrictions) {
        if (restrictions.Length == 1 && restrictions[0].Equals("no_material")) {
            LogVerbose("ALL CUSTOM MATERIALS ARE RESTRICTED ON THIS OBJECT");
            return null;
        }

        foreach (KeyValuePair<string, Dictionary<string, string[]>> materialType in MCSConfig.MATERIAL_REGISTRY) {
            if (materialType.Value.ContainsKey(filename)) {
                if (restrictions.Length == 0 || Array.IndexOf(restrictions, materialType.Key) >= 0) {
                    Material material = Resources.Load<Material>(MCSMain.PATH_PREFIX + filename);
                    LogVerbose("LOAD OF MATERIAL FILE Assets/Resources/MCS/" + filename +
                        (material == null ? " IS NULL" : " IS DONE"));
                    return material;
                }

                LogVerbose("MATERIAL " + filename + " TYPE " + materialType.Key + " IS RESTRICTED");
                return null;
            }
        }

        LogVerbose("MATERIAL " + filename + " NOT IN MATERIAL REGISTRY");
        return null;
    }

    private void AssignMaterial(GameObject gameObject, string configMaterialFile) {
        this.AssignMaterials(gameObject, new string[] { configMaterialFile }, new string[] { }, new string[] { });
    }

    private void AssignMaterials(
        GameObject gameObject,
        string[] configMaterialFiles,
        string[] objectMaterialNames,
        string[] objectMaterialRestrictions
    ) {
        if (configMaterialFiles.Length == 0) {
            return;
        }

        // If given objectMaterialNames, assign each objectMaterialName to its corresponding configMaterialFile.
        Dictionary<string, Material> assignments = new Dictionary<string, Material>();
        for (int i = 0; i < objectMaterialNames.Length; ++i) {
            if (configMaterialFiles.Length > i && !configMaterialFiles[i].Equals("")) {
                Material material = this.LoadMaterial(configMaterialFiles[i], objectMaterialRestrictions);
                if (material != null) {
                    assignments.Add(objectMaterialNames[i], material);
                    LogVerbose("OBJECT " + gameObject.name.ToUpper() + " SWAP MATERIAL " + objectMaterialNames[i] +
                        " WITH " + material.name);
                }
            }
        }

        // If not given objectMaterialNames, just change all object materials to the first configMaterialFile.
        Material singleConfigMaterial = assignments.Count > 0 ? null : this.LoadMaterial(configMaterialFiles[0],
            objectMaterialRestrictions);

        if (assignments.Count == 0 && singleConfigMaterial == null) {
            return;
        }

        // Sometimes objects have multiple renderers with their own materials (like flaps of boxes), so modify each.
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        renderers.ToList().ForEach((renderer) => {
            renderer.materials = renderer.materials.ToList().Select((material) => {
                if (assignments.Count > 0) {
                    if (assignments.ContainsKey(material.name)) {
                        return assignments[material.name];
                    }
                    // Object material names sometimes end with " (Instance)" though I'm not sure why.
                    if (assignments.ContainsKey(material.name.Replace(" (Instance)", ""))) {
                        return assignments[material.name.Replace(" (Instance)", "")];
                    }
                    return material;
                }
                return singleConfigMaterial;
            }).ToArray();
        });
    }

    private GameObject AssignProperties(
        GameObject gameObject,
        MCSConfigGameObject objectConfig,
        MCSConfigObjectDefinition objectDefinition
    ) {
        // NOTE: The objectConfig applies to THIS object and the objectDefinition applies to ALL objects of this type.

        gameObject.name = objectConfig.id;
        gameObject.tag = "SimObj"; // AI2-THOR Tag
        gameObject.layer = 8; // AI2-THOR Layer SimObjVisible
        // Add all new objects to the "Objects" object because the AI2-THOR SceneManager seems to care.
        gameObject.transform.parent = this.objectParent.transform;

        LogVerbose("CREATE " + objectDefinition.id.ToUpper() + " GAME OBJECT " + gameObject.name);

        // If scale is defined, set the object's scale to the defined scale; otherwise, use the object's default scale.
        if (objectDefinition.scale.isDefined()) {
            gameObject.transform.localScale = new Vector3(objectDefinition.scale.GetX(), objectDefinition.scale.GetY(),
                objectDefinition.scale.GetZ());
        }

        // See if each SimObjPhysics property is active on this specific object or on all objects of this type.
        // Currently you can't deactivate the properties on specific objects, since we don't need to do that right now.
        bool moveable = objectConfig.moveable || objectDefinition.moveable;
        bool openable = objectConfig.openable || objectDefinition.openable;
        bool pickupable = objectConfig.pickupable || objectDefinition.pickupable;
        bool receptacle = objectConfig.receptacle || objectDefinition.receptacle;

        bool shouldAddSimObjPhysicsScript = moveable || openable || pickupable || receptacle || objectConfig.physics ||
            objectDefinition.physics;

        if (objectConfig.structure) {
            // Add the AI2-THOR Structure script with specific properties.
            this.AssignStructureScript(gameObject);
            // Add the AI2-THOR SimObjPhysics script to generate visibility points for the structure so it will be
            // returned in the output object metadata.
            shouldAddSimObjPhysicsScript = true;
        }

        Collider[] colliders = new Collider[] { };
        Transform[] visibilityPoints = new Transform[] { };

        MCSConfigLegacyObjectDefinition legacy = this.RetrieveLegacyObjectDefinition(objectDefinition,
            this.currentScene.version);
        List<MCSConfigVector> visibilityPointsFromConfig = legacy != null && legacy.visibilityPoints.Count > 0 ?
            legacy.visibilityPoints : objectDefinition.visibilityPoints;

        if (shouldAddSimObjPhysicsScript) {
            // Add Unity Rigidbody and Collider components to enable physics on this object.
            this.AssignRigidbody(gameObject, objectConfig.mass > 0 ? objectConfig.mass : objectDefinition.mass,
                objectConfig.kinematic || objectDefinition.kinematic, objectDefinition.centerMassAtBottom);
            colliders = this.AssignColliders(gameObject, objectDefinition);
        }

        // The object's visibility points define a subset of points along the outside of the object for AI2-THOR.
        if (visibilityPointsFromConfig.Count > 0) {
            // Use the List constructor to copy the visibility points list from the object definition.
            List<MCSConfigVector> points = new List<MCSConfigVector>(visibilityPointsFromConfig);
            // For dynamically generated visibility points, set the scale of the visibility point parent component
            // to be the inverse of the object's scale when the object is first shown.
            MCSConfigShow showConfig = (objectConfig != null && objectConfig.shows.Count > 0) ?
                objectConfig.shows[0] : null;
            Vector3? scaleNull = null;
            visibilityPoints = this.AssignVisibilityPoints(gameObject, points,
                (objectDefinition.visibilityPointsScaleOne ? Vector3.one : scaleNull));
        }

        if (shouldAddSimObjPhysicsScript) {
            // Add the AI2-THOR SimObjPhysics script with specific properties.
            this.AssignSimObjPhysicsScript(gameObject, objectConfig, objectDefinition, colliders, visibilityPoints,
                moveable, openable, pickupable, receptacle);
        }
        // If the object has a SimObjPhysics script for some reason, ensure its tag and ID are set correctly.
        else if (gameObject.GetComponent<SimObjPhysics>() != null) {
            gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            gameObject.GetComponent<SimObjPhysics>().objectID = gameObject.name;
            gameObject.GetComponent<SimObjPhysics>().shape = objectConfig.structure ? "structural" :
                objectDefinition.shape;
        }

        string[] materialFiles = objectConfig.materials != null ? objectConfig.materials.ToArray() : new string[] { };
        // Backwards compatibility
        if (materialFiles.Length == 0 && objectConfig.materialFile != null && !objectConfig.materialFile.Equals("")) {
            materialFiles = new string[] { objectConfig.materialFile };
        }
        this.AssignMaterials(gameObject, materialFiles, objectDefinition.materials != null ?
            objectDefinition.materials.ToArray() : new string[] { }, objectDefinition.materialRestrictions.ToArray());

        this.ModifyChildrenInteractables(gameObject, objectDefinition.interactables);

        this.ModifyChildrenWithCustomOverrides(gameObject, objectDefinition.overrides);

        return gameObject;
    }

    private GameObject AssignReceptacleTrigglerBox(
        GameObject gameObject,
        MCSConfigTransform receptacleTriggerBoxDefinition
    ) {
        GameObject receptacleTriggerBoxObject = new GameObject {
            isStatic = true,
            layer = 9, // AI2-THOR Layer SimObjInvisible
            name = "ReceptacleTriggerBox",
            tag = "Receptacle" // AI2-THOR Tag
        };
        receptacleTriggerBoxObject.transform.parent = gameObject.transform;

        MCSConfigCollider colliderDefinition = new MCSConfigCollider {
            position = receptacleTriggerBoxDefinition.position,
            rotation = receptacleTriggerBoxDefinition.rotation,
            scale = receptacleTriggerBoxDefinition.scale,
            type = "box"
        };

        Collider receptacleCollider = this.AssignCollider(receptacleTriggerBoxObject, colliderDefinition);
        receptacleCollider.isTrigger = true;

        Contains containsScript = receptacleTriggerBoxObject.AddComponent<Contains>();
        containsScript.myParent = gameObject;

        return receptacleTriggerBoxObject;
    }

    private GameObject[] AssignReceptacleTriggerBoxes(
        GameObject gameObject,
        MCSConfigObjectDefinition objectDefinition
    ) {
        // If we've configured new trigger box definitions but trigger boxes already exist, delete them.
        if (gameObject.transform.Find("ReceptacleTriggerBox") != null) {
            Destroy(gameObject.transform.Find("ReceptacleTriggerBox").gameObject);
        }

        MCSConfigLegacyObjectDefinition legacy = this.RetrieveLegacyObjectDefinition(objectDefinition,
            this.currentScene.version);
        List<MCSConfigTransform> triggerBoxesFromConfig = legacy != null && legacy.receptacleTriggerBoxes.Count > 0 ?
            legacy.receptacleTriggerBoxes : objectDefinition.receptacleTriggerBoxes;

        // If this object will have multiple trigger boxes, create a common parent.
        GameObject receptacleParentObject = null;
        if (triggerBoxesFromConfig.Count > 1) {
            receptacleParentObject = new GameObject {
                isStatic = true,
                layer = 9, // AI2-THOR Layer SimObjInvisible
                name = "ReceptacleTriggerBox",
                tag = "Receptacle" // AI2-THOR Tag
            };
            receptacleParentObject.transform.parent = gameObject.transform;
        }

        int index = 0;
        return triggerBoxesFromConfig.Select((triggerBoxDefinition) => {
            GameObject receptacleObject = this.AssignReceptacleTrigglerBox(gameObject, triggerBoxDefinition);
            // If this object will have multiple trigger boxes, rename and assign each to a common parent.
            if (receptacleParentObject != null) {
                ++index;
                receptacleObject.name = gameObject.name + "_receptacle_trigger_box_" + index;
                receptacleObject.transform.parent = receptacleParentObject.transform;
            }
            return receptacleObject;
        }).ToArray();
    }

    private void AssignRigidbody(GameObject gameObject, float mass, bool kinematic, bool centerMassAtBottom) {
        // Note that some prefabs may already have a Rigidbody component.
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null) {
            rigidbody = gameObject.AddComponent<Rigidbody>();
            LogVerbose("ASSIGN RIGID BODY TO GAME OBJECT " + gameObject.name);
        }
        // Set isKinematic to false by default so the objects are always affected by the physics simulation.
        rigidbody.isKinematic = kinematic;
        // Set the mode to continuous dynamic or else fast moving objects may pass through other objects.
        rigidbody.collisionDetectionMode = kinematic ? CollisionDetectionMode.Discrete :
            CollisionDetectionMode.ContinuousDynamic;
        if (mass > 0) {
            rigidbody.mass = mass;
        }
        if (centerMassAtBottom) {
            rigidbody.centerOfMass = new Vector3(0, 0, 0);
        }
    }

    private void AssignSimObjPhysicsScript(
        GameObject gameObject,
        MCSConfigGameObject objectConfig,
        MCSConfigObjectDefinition objectDefinition,
        Collider[] colliders,
        Transform[] visibilityPoints,
        bool moveable,
        bool openable,
        bool pickupable,
        bool receptacle
    ) {
        gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag

        // Remove the unneeded SimObj script from AI2-THOR prefabs that we want to make moveable/pickupable.
        if (gameObject.GetComponent<SimObj>() != null) {
            Destroy(gameObject.GetComponent<SimObj>());
        }

        // Most AI2-THOR objects will already have the SimObjPhysics script with the relevant properties.
        SimObjPhysics ai2thorPhysicsScript = gameObject.GetComponent<SimObjPhysics>();
        if (ai2thorPhysicsScript == null) {
            ai2thorPhysicsScript = gameObject.AddComponent<SimObjPhysics>();
            ai2thorPhysicsScript.isInteractable = true;
            ai2thorPhysicsScript.PrimaryProperty = SimObjPrimaryProperty.Static;
            ai2thorPhysicsScript.SecondaryProperties = new SimObjSecondaryProperty[] { };
            ai2thorPhysicsScript.MyColliders = colliders ?? (new Collider[] { });
            ai2thorPhysicsScript.ReceptacleTriggerBoxes = new List<GameObject>().ToArray();
        }

        ai2thorPhysicsScript.shape = objectConfig.structure ? "structural" : objectDefinition.shape;

        if (objectConfig.physicsProperties != null && objectConfig.physicsProperties.enable) {
            AssignPhysicsMaterialAndRigidBodyValues(objectConfig.physicsProperties, gameObject, ai2thorPhysicsScript);
        } else if (objectDefinition.physicsProperties != null && objectDefinition.physicsProperties.enable) {
            AssignPhysicsMaterialAndRigidBodyValues(objectDefinition.physicsProperties, gameObject, ai2thorPhysicsScript);
        }

        ai2thorPhysicsScript.PrimaryProperty = (pickupable ? SimObjPrimaryProperty.CanPickup : (moveable ?
            SimObjPrimaryProperty.Moveable : ai2thorPhysicsScript.PrimaryProperty));

        if (receptacle && !ai2thorPhysicsScript.SecondaryProperties.ToList().Contains(SimObjSecondaryProperty.Receptacle)) {
            ai2thorPhysicsScript.SecondaryProperties = ai2thorPhysicsScript.SecondaryProperties.ToList().Concat(
                new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle }).ToArray();
        }

        if (openable && !ai2thorPhysicsScript.SecondaryProperties.ToList().Contains(SimObjSecondaryProperty.CanOpen)) {
            ai2thorPhysicsScript.SecondaryProperties = ai2thorPhysicsScript.SecondaryProperties.ToList().Concat(
                new SimObjSecondaryProperty[] { SimObjSecondaryProperty.CanOpen }).ToArray();
        }

        if (objectDefinition.stacking || objectConfig.stacking) {
            ai2thorPhysicsScript.SecondaryProperties = ai2thorPhysicsScript.SecondaryProperties.ToList().Concat(
                new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Stacking }).ToArray();
        }

        // Always set the uniqueID to a new name (we don't want to use AI2-THOR's default names).
        ai2thorPhysicsScript.objectID = gameObject.name;

        // Remove the CanBreak property from the SecondaryProperties array (we don't want objects to break).
        ai2thorPhysicsScript.SecondaryProperties = ai2thorPhysicsScript.SecondaryProperties.Where((property) =>
            !property.Equals(SimObjSecondaryProperty.CanBreak)).ToArray();

        // Also remove the AI2-THOR Break script.
        if (gameObject.GetComponent<Break>() != null) {
            Destroy(gameObject.GetComponent<Break>());
        }

        // Override the object's AI2-THOR simulation type or else the object may have odd behavior.
        if (pickupable) {
            // TODO Should we make the AI2-THOR object type configurable? Does it even matter?
            ai2thorPhysicsScript.Type = SimObjType.IgnoreType;
        }

        if (objectDefinition.salientMaterials.Count > 0) {
            ai2thorPhysicsScript.salientMaterials = this.RetrieveSalientMaterials(objectDefinition.salientMaterials);
        }

        if (objectConfig.salientMaterials.Count > 0) {
            ai2thorPhysicsScript.salientMaterials = this.RetrieveSalientMaterials(objectConfig.salientMaterials);
        }

        // If no salient materials were assigned, set a default or else the script will emit errors.
        if (ai2thorPhysicsScript.salientMaterials == null || ai2thorPhysicsScript.salientMaterials.Length == 0) {
            // TODO What should we set as the default material? Does it even matter?
            ai2thorPhysicsScript.salientMaterials = new ObjectMetadata.ObjectSalientMaterial[] {
                ObjectMetadata.ObjectSalientMaterial.Wood
            };
        }

        // The object's receptacle trigger boxes define the area in which objects may be placed for AI2-THOR.
        if (receptacle && objectDefinition.receptacleTriggerBoxes.Count > 0) {
            ai2thorPhysicsScript.ContainedObjectReferences = new List<SimObjPhysics>();
            ai2thorPhysicsScript.ReceptacleTriggerBoxes = this.AssignReceptacleTriggerBoxes(gameObject,
                objectDefinition);
        }

        // The object's bounding box defines the complete bounding box around the object for AI2-THOR.
        // JsonUtility always sets objectDefinition.boundingBox, so verify that the position is not null.
        if (objectDefinition.boundingBox != null && objectDefinition.boundingBox.position != null) {
            MCSConfigLegacyObjectDefinition legacy = this.RetrieveLegacyObjectDefinition(objectDefinition,
                this.currentScene.version);
            MCSConfigCollider boundingBoxFromConfig = legacy != null && legacy.boundingBox != null ?
                legacy.boundingBox : objectDefinition.boundingBox;
            ai2thorPhysicsScript.BoundingBox = this.AssignBoundingBox(gameObject, boundingBoxFromConfig).gameObject;
        }

        this.EnsureCanOpenObjectScriptAnimationTimeIsZero(gameObject);

        // Open or close an openable receptacle as demaned by the object's config.
        // Note: Do this AFTER calling EnsureCanOpenObjectScriptAnimationTimeIsZero
        if (openable) {
            CanOpen_Object ai2thorCanOpenObjectScript = gameObject.GetComponent<CanOpen_Object>();
            if (ai2thorCanOpenObjectScript != null) {
                if ((ai2thorCanOpenObjectScript.isOpen && !objectConfig.opened) ||
                    (!ai2thorCanOpenObjectScript.isOpen && objectConfig.opened)) {

                    ai2thorCanOpenObjectScript.SetOpenPercent(objectConfig.opened ? 1 : 0);
                    ai2thorCanOpenObjectScript.Interact();
                }
                ai2thorCanOpenObjectScript.isOpenByPercentage = ai2thorCanOpenObjectScript.isOpen ? 1 : 0;
            }
        }

        if (visibilityPoints.Length > 0) {
            ai2thorPhysicsScript.VisibilityPoints = visibilityPoints;
        }

        // Call Start to initialize the script since it did not exist on game start.
        ai2thorPhysicsScript.Start();
    }

    private void AssignPhysicsMaterialAndRigidBodyValues(
        MCSConfigPhysicsProperties physicsObject,
        GameObject gameObject,
        SimObjPhysics ai2thorPhysicsScript
    ) {
        ai2thorPhysicsScript.HFdynamicfriction = physicsObject.dynamicFriction;
        ai2thorPhysicsScript.HFstaticfriction = physicsObject.staticFriction;
        ai2thorPhysicsScript.HFbounciness = physicsObject.bounciness;
        ai2thorPhysicsScript.HFrbdrag = physicsObject.drag;
        ai2thorPhysicsScript.HFrbangulardrag = physicsObject.angularDrag;

        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        //Gets rigid body of object and changes drag/angular drag
        rigidbody.drag = ai2thorPhysicsScript.HFrbdrag;
        rigidbody.angularDrag = ai2thorPhysicsScript.HFrbangulardrag;

        //Loops through each collider on the object, creates a new material (other wise it would change)
        //(every instance where this material is used) and then assigns each collider with an updated physics material
        PhysicMaterial physicMaterial = new PhysicMaterial();
        foreach (Collider collider in ai2thorPhysicsScript.MyColliders) {
            physicMaterial.dynamicFriction = ai2thorPhysicsScript.HFdynamicfriction;
            physicMaterial.staticFriction = ai2thorPhysicsScript.HFstaticfriction;
            physicMaterial.bounciness = ai2thorPhysicsScript.HFbounciness;
            collider.material = physicMaterial;
        }
    }

    private void AssignStructureScript(GameObject gameObject) {
        // Ensure this object is not moveable.
        gameObject.isStatic = true;
        // Add AI2-THOR specific properties.
        gameObject.tag = "Structure"; // AI2-THOR Tag
        StructureObject ai2thorStructureScript = gameObject.AddComponent<StructureObject>();
        ai2thorStructureScript.WhatIsMyStructureObjectTag = StructureObjectTag.Wall; // TODO Make configurable
    }

    private void AssignTransform(
        GameObject gameObject,
        MCSConfigTransform transformDefinition
    ) {
        gameObject.transform.localPosition = transformDefinition.position == null ? Vector3.zero :
            new Vector3(transformDefinition.position.x, transformDefinition.position.y,
            transformDefinition.position.z);
        gameObject.transform.localRotation = transformDefinition.rotation == null ? Quaternion.identity :
            Quaternion.Euler(transformDefinition.rotation.x, transformDefinition.rotation.y,
            transformDefinition.rotation.z);
        gameObject.transform.localScale = transformDefinition.scale == null ? Vector3.one :
            new Vector3(transformDefinition.scale.GetX(), transformDefinition.scale.GetY(),
            transformDefinition.scale.GetZ());
    }

    private Transform[] AssignVisibilityPoints(
        GameObject gameObject,
        List<MCSConfigVector> points,
        Vector3? scaleOverride
    ) {
        // The AI2-THOR scripts assume the visibility points have a parent object with the name VisibilityPoints.
        GameObject visibilityPointsParentObject = new GameObject {
            isStatic = true,
            name = "VisibilityPoints"
        };
        visibilityPointsParentObject.transform.parent = gameObject.transform;
        visibilityPointsParentObject.transform.localPosition = Vector3.zero;
        visibilityPointsParentObject.transform.localRotation = Quaternion.identity;
        if (scaleOverride.HasValue) {
            visibilityPointsParentObject.transform.localScale = scaleOverride.Value;
        }
        int index = 0;
        return points.Select((point) => {
            ++index;
            GameObject visibilityPointsObject = new GameObject {
                isStatic = true,
                layer = 8, // AI2-THOR Layer SimObjVisible
                name = gameObject.name + "_visibility_point_" + index
            };
            visibilityPointsObject.transform.parent = visibilityPointsParentObject.transform;
            visibilityPointsObject.transform.localPosition = new Vector3(point.x, point.y, point.z);
            return visibilityPointsObject.transform;
        }).ToArray();
    }

    // if open is true, try to open the container.  If open is false, try to close the container
    private void OpenCloseContainer(bool open, GameObject gameOrParentObject) {
        CanOpen_Object canOpen = gameOrParentObject.GetComponentInChildren<CanOpen_Object>();
        if (canOpen != null) {
            canOpen.SetOpenPercent(open ? 1 : 0);
            if (canOpen.isOpen != open) {
                canOpen.Interact();
            }
        }
    }

    private GameObject CreateCustomGameObject(
        MCSConfigGameObject objectConfig,
        MCSConfigObjectDefinition objectDefinition
    ) {
        MCSConfigLegacyObjectDefinition legacy = this.RetrieveLegacyObjectDefinition(objectDefinition,
            this.currentScene.version);
        string resourceFile = legacy != null ? legacy.resourceFile : objectDefinition.resourceFile;

        GameObject gameObject = Instantiate(Resources.Load(MCSMain.PATH_PREFIX + resourceFile, typeof(GameObject))) as GameObject;

        LogVerbose("LOAD CUSTOM GAME OBJECT " + objectDefinition.id + " FROM FILE Assets/Resources/MCS/" +
            resourceFile + (gameObject == null ? " IS NULL" : " IS DONE"));

        gameObject = AssignProperties(gameObject, objectConfig, objectDefinition);

        // Set animations.
        if (objectDefinition.animations.Any((animationDefinition) => animationDefinition.animationFile != null &&
            !animationDefinition.animationFile.Equals(""))) {

            Animation animation = gameObject.GetComponent<Animation>();
            if (animation == null) {
                animation = gameObject.AddComponent<Animation>();
                LogVerbose("ASSIGN NEW ANIMATION TO GAME OBJECT " + gameObject.name);
            }
            objectDefinition.animations.ForEach((animationDefinition) => {
                if (animationDefinition.animationFile != null && !animationDefinition.animationFile.Equals("")) {
                    AnimationClip clip = Resources.Load<AnimationClip>(MCSMain.PATH_PREFIX +
                        animationDefinition.animationFile);
                    LogVerbose("LOAD OF ANIMATION CLIP FILE Assets/Resources/MCS/" +
                        animationDefinition.animationFile + (clip == null ? " IS NULL" : " IS DONE"));
                    animation.AddClip(clip, animationDefinition.id);
                    LogVerbose("ASSIGN ANIMATION CLIP " + animationDefinition.animationFile + " TO ACTION " +
                        animationDefinition.id);
                }
            });
        }

        // Set animation controller.
        if (objectConfig.controller != null && !objectConfig.controller.Equals("")) {
            MCSConfigAnimator animatorDefinition = objectDefinition.animators
                .Where(cont => cont.id.Equals(objectConfig.controller)).ToList().First();
            if (animatorDefinition.animatorFile != null && !animatorDefinition.animatorFile.Equals("")) {
                Animator animator = gameObject.GetComponent<Animator>();
                if (animator == null) {
                    animator = gameObject.AddComponent<Animator>();
                    LogVerbose("ASSIGN NEW ANIMATOR CONTROLLER TO GAME OBJECT " + gameObject.name);
                }
                RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>(
                    MCSMain.PATH_PREFIX + animatorDefinition.animatorFile);
                LogVerbose("LOAD OF ANIMATOR CONTROLLER FILE Assets/Resources/MCS/" +
                    animatorDefinition.animatorFile + (animatorController == null ? " IS NULL" : " IS DONE"));
                animator.runtimeAnimatorController = animatorController;
            }
        }

        return gameObject;
    }

    private GameObject CreateGameObject(MCSConfigGameObject objectConfig) {
        MCSConfigObjectDefinition objectDefinition = this.objectDictionary[objectConfig.type.ToUpper()];
        if (objectDefinition != null) {
            return CreateCustomGameObject(objectConfig, objectDefinition);
        }
        return null;
    }

    private GameObject CreateNullParentObjectIfNeeded(MCSConfigGameObject objectConfig) {
        // Null parents are useful if we want to rotate an object but don't want to pivot around its center point.
        if (objectConfig.nullParent != null && (objectConfig.nullParent.position != null ||
            objectConfig.nullParent.rotation != null)) {

            GameObject parentObject = new GameObject();
            parentObject.transform.parent = objectConfig.GetGameObject().transform.parent;
            objectConfig.SetParentObject(parentObject);
            parentObject.name = objectConfig.id + "Parent";
            parentObject.transform.localPosition = new Vector3(objectConfig.nullParent.position.x,
                objectConfig.nullParent.position.y, objectConfig.nullParent.position.z);
            parentObject.transform.localRotation = Quaternion.Euler(objectConfig.nullParent.rotation.x,
                objectConfig.nullParent.rotation.y, objectConfig.nullParent.rotation.z);
            objectConfig.GetGameObject().transform.parent = objectConfig.GetParentObject().transform;
            LogVerbose("CREATE PARENT GAME OBJECT " + parentObject.name);
            return parentObject;
        }

        return null;
    }

    private void EnsureCanOpenObjectScriptAnimationTimeIsZero(GameObject gameObject) {
        CanOpen_Object ai2thorCanOpenObjectScript = gameObject.GetComponent<CanOpen_Object>();
        if (ai2thorCanOpenObjectScript != null) {
            // We need to set the animation time to zero because we keep the physics simulation paused.
            ai2thorCanOpenObjectScript.animationTime = 0;
        }
    }

    private void InitializeGameObject(MCSConfigGameObject objectConfig) {
        try {
            GameObject gameObject = CreateGameObject(objectConfig);
            objectConfig.SetGameObject(gameObject);
            if (gameObject != null) {
                GameObject parentObject = CreateNullParentObjectIfNeeded(objectConfig);
                // Hide the object until the frame defined in MachineCommonSenseConfigGameObject.shows
                (parentObject ?? gameObject).SetActive(false);
            }
        } catch (Exception e) {
            Debug.LogError("MCS: " + e);
        }
    }

    private List<MCSConfigObjectDefinition> LoadObjectRegistryFromFile(String filePath) {
        TextAsset objectRegistryFile = Resources.Load<TextAsset>(MCSMain.PATH_PREFIX + filePath);
        Debug.Log("MCS: Config file Assets/Resources/MCS/" + filePath + ".json" + (objectRegistryFile == null ?
            " is null!" : (":\n" + objectRegistryFile.text)));
        MCSConfigObjectRegistry objectRegistry = JsonUtility
            .FromJson<MCSConfigObjectRegistry>(objectRegistryFile.text);
        return objectRegistry.objects;
    }

    public void LogVerbose(String text) {
        if (this.enableVerboseLog) {
            Debug.Log("MCS: " + text);
        }
    }

    private void ModifyChildrenInteractables(
        GameObject gameObject,
        List<MCSConfigInteractables> interactableDefinitions
    ) {
        interactableDefinitions.ForEach((interactableDefinition) => {
            // Override properties of SimObjPhysics children in existing AI2-THOR prefab objects if needed.
            Transform interactableTransform = gameObject.transform.Find(interactableDefinition.name);
            if (interactableTransform != null) {
                GameObject interactableObject = interactableTransform.gameObject;
                SimObjPhysics ai2thorPhysicsScript = interactableObject.GetComponent<SimObjPhysics>();
                if (ai2thorPhysicsScript) {
                    ai2thorPhysicsScript.objectID = gameObject.name + "_" + interactableDefinition.id;
                    // The type of a child interactable should be something like "drawer" or "shelf" so use that as
                    // the object's shape.
                    ai2thorPhysicsScript.shape = ai2thorPhysicsScript.Type.ToString().ToLower();
                }
                this.EnsureCanOpenObjectScriptAnimationTimeIsZero(interactableObject);
                Rigidbody rigidbody = interactableObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    Renderer renderer = interactableObject.GetComponent<Renderer>();
                    // Interactable children that don't have renderers (like shelves) should normally be kinematic
                    // (or at least shelves). Other interactable children should definitely never be kinematic.
                    rigidbody.isKinematic = (renderer == null);
                }
            }
        });
    }

    private void ModifyChildrenWithCustomOverrides(
        GameObject gameObject,
        List<MCSConfigOverride> overrideDefinitions
    ) {
        overrideDefinitions.ForEach((overrideDefinition) => {
            // Override properties of colliders, meshes, or other children in existing prefab objects if needed.
            Transform overrideTransform = gameObject.transform.Find(overrideDefinition.name);
            if (overrideTransform != null) {
                GameObject overrideObject = overrideTransform.gameObject;
                this.AssignTransform(overrideObject, overrideDefinition);
            }
        });
    }

    private ObjectMetadata.ObjectSalientMaterial[] RetrieveSalientMaterials(List<string> salientMaterials) {
        return salientMaterials.Select((salientMaterial) => {
            switch (salientMaterial.ToLower()) {
                case "ceramic":
                    return ObjectMetadata.ObjectSalientMaterial.Ceramic;
                case "fabric":
                    return ObjectMetadata.ObjectSalientMaterial.Fabric;
                case "food":
                    return ObjectMetadata.ObjectSalientMaterial.Food;
                case "glass":
                    return ObjectMetadata.ObjectSalientMaterial.Glass;
                case "hollow":
                    return ObjectMetadata.ObjectSalientMaterial.Hollow;
                case "metal":
                    return ObjectMetadata.ObjectSalientMaterial.Metal;
                case "organic":
                    return ObjectMetadata.ObjectSalientMaterial.Organic;
                case "paper":
                    return ObjectMetadata.ObjectSalientMaterial.Paper;
                case "plastic":
                    return ObjectMetadata.ObjectSalientMaterial.Plastic;
                case "rubber":
                    return ObjectMetadata.ObjectSalientMaterial.Rubber;
                case "soap":
                    return ObjectMetadata.ObjectSalientMaterial.Soap;
                case "sponge":
                    return ObjectMetadata.ObjectSalientMaterial.Sponge;
                case "stone":
                    return ObjectMetadata.ObjectSalientMaterial.Stone;
                case "wax":
                    return ObjectMetadata.ObjectSalientMaterial.Wax;
                case "wood":
                // TODO What should the default case be? Does it even matter?
                default:
                    return ObjectMetadata.ObjectSalientMaterial.Wood;
            }
        }).ToArray();
    }

    private void PostUpdateGameObjectOnStep(List<MCSConfigGameObject> objectConfigs, int step) {
        objectConfigs.ForEach(objectConfig => {
            // If an object's location is set in relation to another object, modify its location in the PostUpdate to
            // ensure that any update to the other object's location is finished.
            if (objectConfig.locationParent != null && !objectConfig.locationParent.Equals("")) {
                objectConfig.shows.Where(show => show.stepBegin == step).ToList().ForEach((show) => {
                    MCSConfigGameObject[] originObjectConfigs = objectConfigs.Where(possibleConfig =>
                        possibleConfig.id.Equals(objectConfig.locationParent)).ToArray();

                    if (originObjectConfigs.Length > 0) {
                        GameObject originObject = originObjectConfigs[0].GetGameObject();
                        GameObject targetObject = objectConfig.GetGameObject();
                        // First, copy the origin object's position and rotation to the target object.
                        targetObject.transform.localPosition = new Vector3(originObject.transform.localPosition.x,
                            originObject.transform.localPosition.y, originObject.transform.localPosition.z);
                        targetObject.transform.localRotation = Quaternion.Euler(
                            originObject.transform.localRotation.eulerAngles.x,
                            originObject.transform.localRotation.eulerAngles.y,
                            originObject.transform.localRotation.eulerAngles.z);
                        // Then, reposition the target object as configured in relation to its rotation.
                        targetObject.transform.Translate(new Vector3(show.position.x, show.position.y,
                            show.position.z));
                        // Finally, rotate the target object as configured in relation to its rotation.
                        targetObject.transform.Rotate(new Vector3(show.rotation.x, show.rotation.y, show.rotation.z));
                    }
                });
            }
        });
    }

    private MCSConfigLegacyObjectDefinition RetrieveLegacyObjectDefinition(
        MCSConfigObjectDefinition objectDefinition, int sceneVersion
    ) {
        if (objectDefinition.legacy.Count > 0) {
            MCSConfigLegacyObjectDefinition[] legacy = objectDefinition.legacy
                .Where(item => item.version == sceneVersion).ToArray();
            return legacy.Length > 0 ? legacy[0] : null;
        }
        return null;
    }

    private bool UpdateGameObjectOnStep(MCSConfigGameObject objectConfig, int step) {
        bool objectsWereShown = false;

        GameObject gameOrParentObject = objectConfig.GetParentObject() ?? objectConfig.GetGameObject();

        // Do the hides before the shows so any teleports work as expected.
        objectConfig.hides.Where(hide => hide.stepBegin == step).ToList().ForEach((hide) => {
            gameOrParentObject.SetActive(false);
        });

        objectConfig.shows.Where(show => show.stepBegin == step).ToList().ForEach((show) => {
            // Set the position, rotation, and scale on the game object, not on the parent object.
            GameObject gameObject = objectConfig.GetGameObject();
            if (show.position != null) {
                gameObject.transform.localPosition = new Vector3(show.position.x, show.position.y,
                    show.position.z);
            }
            if (show.rotation != null) {
                gameObject.transform.localRotation = Quaternion.Euler(show.rotation.x,
                    show.rotation.y, show.rotation.z);
            }
            if (show.scale != null) {
                // Use the scale as a multiplier because the scale of prefab objects may be very specific.
                gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x * show.scale.GetX(),
                    gameObject.transform.localScale.y * show.scale.GetY(),
                    gameObject.transform.localScale.z * show.scale.GetZ());
            }
            gameOrParentObject.SetActive(true);
            objectsWereShown = true;
        });

        objectConfig.togglePhysics.Where(toggle => toggle.stepBegin == step).ToList().ForEach((toggle) => {
            Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
            if (rigidbody != null) {
                this.AssignRigidbody(gameOrParentObject, rigidbody.mass, !rigidbody.isKinematic, false);
            }
        });

        objectConfig.resizes.Where(resize => resize.stepBegin <= step && resize.stepEnd >= step && resize.size != null)
            .ToList().ForEach((resize) => {
                // Set the scale on the game object, not on the parent object.
                GameObject gameObject = objectConfig.GetGameObject();
                // Use the scale as a multiplier because the scale of prefab objects may be very specific.
                gameObject.transform.localScale = new Vector3(gameObject.transform.localScale.x * resize.size.GetX(),
                    gameObject.transform.localScale.y * resize.size.GetY(),
                    gameObject.transform.localScale.z * resize.size.GetZ());
            });

        objectConfig.teleports.Where(teleport => teleport.stepBegin == step && teleport.position != null).ToList()
            .ForEach((teleport) => {
                gameOrParentObject.transform.localPosition = new Vector3(teleport.position.x, teleport.position.y,
                    teleport.position.z);
            });

        objectConfig.forces.Where(force => force.stepBegin <= step && force.stepEnd >= step && force.vector != null)
            .ToList().ForEach((force) => {
                Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    if (force.relative) {
                        rigidbody.AddRelativeForce(new Vector3(force.vector.x, force.vector.y, force.vector.z));
                    } else {
                        rigidbody.AddForce(new Vector3(force.vector.x, force.vector.y, force.vector.z));
                    }
                }
            });

        objectConfig.torques.Where(torque => torque.stepBegin <= step && torque.stepEnd >= step &&
            torque.vector != null).ToList().ForEach((torque) => {
                Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    rigidbody.AddTorque(new Vector3(torque.vector.x, torque.vector.y, torque.vector.z));
                }
            });

        // Ghosting an object will make it temporarily intangible: disable its colliders and the effects of physics.
        bool ghosted = false;
        objectConfig.ghosts.Where(ghost => ghost.stepBegin <= step && ghost.stepEnd >= step).ToList()
            .ForEach((ghost) => {
                ghosted = true;
                gameOrParentObject.GetComponentInChildren<Rigidbody>().isKinematic = true;
                gameOrParentObject.GetComponentInChildren<SimObjPhysics>().MyColliders.ToList().ForEach((collider) => {
                    collider.enabled = false;
                });
            });

        // If this object's config has a "ghosts" element, assume that is should always be non-kinematic and have its
        // colliders enabled by default (whenever it's not ghosted).
        if (!ghosted && objectConfig.ghosts.Count > 0) {
            gameOrParentObject.GetComponentInChildren<Rigidbody>().isKinematic = false;
            gameOrParentObject.GetComponentInChildren<SimObjPhysics>().MyColliders.ToList().ForEach((collider) => {
                collider.enabled = true;
            });
        }

        // Shrouding an object will make it temporarily invisible.
        bool shrouded = false;
        objectConfig.shrouds.Where(shroud => shroud.stepBegin <= step && shroud.stepEnd >= step).ToList()
            .ForEach((shroud) => {
                shrouded = true;
                gameOrParentObject.GetComponentInChildren<Renderer>().enabled = false;
            });

        if (!shrouded) {
            gameOrParentObject.GetComponentInChildren<Renderer>().enabled = true;
        }

        objectConfig.actions.Where(action => action.stepBegin == step).ToList().ForEach((action) => {
            // Play the animation on the game object, not on the parent object.
            Animator animator = objectConfig.GetGameObject().GetComponent<Animator>();
            if (animator != null) {
                animator.Play(action.id);
            } else {
                // If the animator does not exist on this game object, then it must use legacy animations.
                objectConfig.GetGameObject().GetComponent<Animation>().Play(action.id);
            }
        });

        objectConfig.changeMaterials.Where(change => change.stepBegin == step).ToList().ForEach((change) => {
            this.AssignMaterials(gameOrParentObject, change.materials.ToArray(), new string[] { }, new string[] { });
        });

        objectConfig.openClose.Where(change => change.step == step).ToList().ForEach((change) => {
            this.OpenCloseContainer(change.open, gameOrParentObject);
        });

        return objectsWereShown;
    }

    public void UpdateOnPhysicsSubstep(int numberOfSubsteps) {
        if (this.currentScene != null && this.currentScene.objects != null) {
            // Loop over each configuration object in the scene and update if needed.
            this.currentScene.objects.Where(objectConfig => objectConfig.GetGameObject() != null).ToList()
                .ForEach(objectConfig => {
                    GameObject gameOrParentObject = objectConfig.GetParentObject() ?? objectConfig.GetGameObject();
                    // If the object should move during this step, move it a little during each individual substep, so
                    // it looks like the object is moving slowly if we take a snapshot of the scene after each substep.
                    objectConfig.moves.Where(move => move.stepBegin <= this.lastStep &&
                        move.stepEnd >= this.lastStep && move.vector != null).ToList().ForEach((move) => {
                            gameOrParentObject.transform.Translate(new Vector3(move.vector.x, move.vector.y,
                                move.vector.z) / (float)numberOfSubsteps);
                        });
                    objectConfig.rotates.Where(rotate => rotate.stepBegin <= this.lastStep &&
                        rotate.stepEnd >= this.lastStep && rotate.vector != null).ToList().ForEach((rotate) => {
                            gameOrParentObject.transform.Rotate(new Vector3(rotate.vector.x, rotate.vector.y,
                                rotate.vector.z) / (float)numberOfSubsteps);
                        });
                });
        }
    }

    public string GetCurrentSceneName() {
        return currentScene.name;
    }
}

// Definitions of serializable objects from JSON config files.

[Serializable]
public class MCSConfigAbstractObject {
    public string id;
    public bool kinematic;
    public float mass;
    public bool moveable;
    public bool openable;
    public bool opened;
    public bool physics;
    public bool pickupable;
    public bool receptacle;
    public bool stacking;
    public List<string> materials = new List<string>();
    public List<string> salientMaterials = new List<string>();
    public MCSConfigPhysicsProperties physicsProperties;
}

[Serializable]
public class MCSConfigAction : MCSConfigStepBegin {
    public string id;
}

[Serializable]
public class MCSConfigAnimation {
    public string id;
    public string animationFile;
}

[Serializable]
public class MCSConfigAnimator {
    public string id;
    public string animatorFile;
}

[Serializable]
public class MCSConfigChangeMaterial : MCSConfigStepBegin {
    public List<string> materials = new List<string>();
}

[Serializable]
public class MCSConfigCollider : MCSConfigTransform {
    public bool assignToColliderItself;
    public string type;
    public float height;
    public float radius;
}

[Serializable]
public class MCSConfigGameObject : MCSConfigAbstractObject {
    public string controller;
    public string locationParent;
    public string materialFile; // deprecated; please use materials
    public MCSConfigTransform nullParent = null;
    public bool structure;
    public string type;
    public List<MCSConfigAction> actions = new List<MCSConfigAction>();
    public List<MCSConfigChangeMaterial> changeMaterials = new List<MCSConfigChangeMaterial>();
    public List<MCSConfigForce> forces = new List<MCSConfigForce>();
    public List<MCSConfigStepBegin> hides = new List<MCSConfigStepBegin>();
    public List<MCSConfigMove> moves = new List<MCSConfigMove>();
    public List<MCSConfigResize> resizes = new List<MCSConfigResize>();
    public List<MCSConfigMove> rotates = new List<MCSConfigMove>();
    public List<MCSConfigShow> shows = new List<MCSConfigShow>();
    public List<MCSConfigStepBeginEnd> ghosts = new List<MCSConfigStepBeginEnd>();
    public List<MCSConfigStepBeginEnd> shrouds = new List<MCSConfigStepBeginEnd>();
    public List<MCSConfigTeleport> teleports = new List<MCSConfigTeleport>();
    public List<MCSConfigStepBegin> togglePhysics = new List<MCSConfigStepBegin>();
    public List<MCSConfigMove> torques = new List<MCSConfigMove>();
    public List<MCSContainerOpenClose> openClose = new List<MCSContainerOpenClose>();

    private GameObject gameObject;
    private GameObject parentObject;

    public GameObject GetGameObject() {
        return this.gameObject;
    }

    public void SetGameObject(GameObject gameObject) {
        this.gameObject = gameObject;
    }

    public GameObject GetParentObject() {
        return this.parentObject;
    }

    public void SetParentObject(GameObject parentObject) {
        this.parentObject = parentObject;
    }
}

[Serializable]
public class MCSConfigInteractables {
    public string id;
    public string name;
}

[Serializable]
public class MCSConfigForce : MCSConfigMove {
    public bool relative;
}

[Serializable]
public class MCSConfigMove : MCSConfigStepBeginEnd {
    public MCSConfigVector vector;
}

[Serializable]
public class MCSConfigObjectDefinition : MCSConfigAbstractObject {
    public string resourceFile;
    public string shape;
    public bool centerMassAtBottom;
    public bool keepColliders;
    public bool visibilityPointsScaleOne;
    public MCSConfigCollider boundingBox = null;
    public MCSConfigSize scale = null;
    public List<MCSConfigAnimation> animations = new List<MCSConfigAnimation>();
    public List<MCSConfigAnimator> animators = new List<MCSConfigAnimator>();
    public List<MCSConfigCollider> colliders = new List<MCSConfigCollider>();
    public List<MCSConfigInteractables> interactables = new List<MCSConfigInteractables>();
    public List<string> materialRestrictions = new List<string>();
    public List<MCSConfigOverride> overrides = new List<MCSConfigOverride>();
    public List<MCSConfigTransform> receptacleTriggerBoxes = new List<MCSConfigTransform>();
    public List<MCSConfigVector> visibilityPoints = new List<MCSConfigVector>();
    public List<MCSConfigLegacyObjectDefinition> legacy = new List<MCSConfigLegacyObjectDefinition>();
}

[Serializable]
public class MCSConfigLegacyObjectDefinition {
    public int version;
    public string resourceFile;
    public MCSConfigCollider boundingBox = null;
    public List<MCSConfigCollider> colliders = new List<MCSConfigCollider>();
    public List<MCSConfigTransform> receptacleTriggerBoxes = new List<MCSConfigTransform>();
    public List<MCSConfigVector> visibilityPoints = new List<MCSConfigVector>();
}

[Serializable]
public class MCSConfigOverride : MCSConfigTransform {
    public string name;
}

[Serializable]
public class MCSConfigResize : MCSConfigStepBeginEnd {
    public MCSConfigSize size;
}

[Serializable]
public class MCSConfigShow : MCSConfigStepBegin {
    public MCSConfigVector position;
    public MCSConfigVector rotation;
    public MCSConfigSize scale;
}

[Serializable]
public class MCSConfigSize {
    // The X/Y/Z properties must be public or else they don't work correctly.
    [SerializeField]
    public float x;
    [SerializeField]
    public float y;
    [SerializeField]
    public float z;

    public float GetX() {
        return (this.x > 0 ? this.x : 1);
    }

    public float GetY() {
        return (this.y > 0 ? this.y : 1);
    }

    public float GetZ() {
        return (this.z > 0 ? this.z : 1);
    }

    public bool isDefined() {
        return this.x > 0 && this.y > 0 && this.z > 0;
    }
}

[Serializable]
public class MCSConfigStepBegin {
    public int stepBegin;
}

[Serializable]
public class MCSConfigStepBeginEnd : MCSConfigStepBegin {
    public int stepEnd;
}

[Serializable]
public class MCSConfigTransform {
    public MCSConfigVector position = null;
    public MCSConfigVector rotation = null;
    public MCSConfigSize scale = null;
}

[Serializable]
public class MCSConfigVector {
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class MCSContainerOpenClose {
    public bool open;
    public int step;
}

[Serializable]
public class MCSConfigScene {
    public String name;
    public int version;
    public String ceilingMaterial;
    public String floorMaterial;
    public String wallMaterial;

    public MCSConfigWallMaterials roomMaterials;
    public bool intuitivePhysics;
    public bool observation; // deprecated; please use intuitivePhysics
    public bool screenshot;
    public bool isometric;

    public MCSConfigGoal goal;
    public MCSConfigTransform performerStart = null;
    public List<MCSConfigGameObject> objects = new List<MCSConfigGameObject>();
    public MCSConfigPhysicsProperties floorProperties;
    public MCSConfigPhysicsProperties wallProperties;

    public Vector3 roomDimensions;
}

[Serializable]
public class MCSConfigWallMaterials {
    public string left;
    public string right;
    public string front;
    public string back;
}

[Serializable]
public class MCSConfigTeleport : MCSConfigStepBegin {
    public MCSConfigVector position;
}

[Serializable]
public class MCSConfigGoal {
    public string description;
}

[Serializable]
public class MCSConfigObjectRegistry {
    public List<MCSConfigObjectDefinition> objects = new List<MCSConfigObjectDefinition>();
}

[Serializable]
public class MCSConfigPhysicsProperties {
    public bool enable;
    public float dynamicFriction;
    public float staticFriction;
    public float bounciness;
    public float drag;
    public float angularDrag;
}

