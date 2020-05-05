using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System.Text;

public class MachineCommonSenseMain : MonoBehaviour {
    private static float LIGHT_RANGE = 20f;
    private static float LIGHT_RANGE_SCREENSHOT = 10f;
    private static float LIGHT_Y_POSITION = 2.95f;
    private static float LIGHT_Y_POSITION_SCREENSHOT = 0.5f;
    private static float LIGHT_Z_POSITION = 0;
    private static float LIGHT_Z_POSITION_SCREENSHOT = -2.0f;
    private static float WALL_X_POSITION_OBSERVATION = 7.0f;
    private static float WALL_X_POSITION_INTERACTION = 5.5f;
    private static float WALL_Y_POSITION = 1.5f;
    private static float WALL_Z_POSITION = 0;

    public string defaultSceneFile = "";
    public bool enableVerboseLog = false;
    public string ai2thorObjectRegistryFile = "ai2thor_object_registry";
    public string materialRegistryFile = "material_registry";
    public string mcsObjectRegistryFile = "mcs_object_registry";
    public string primitiveObjectRegistryFile = "primitive_object_registry";
    public string defaultCeilingMaterial = "AI2-THOR/Materials/Walls/Drywall";
    public string defaultFloorMaterial = "AI2-THOR/Materials/Fabrics/CarpetWhite 3";
    public string defaultWallsMaterial = "AI2-THOR/Materials/Walls/DrywallBeige";

    private MachineCommonSenseConfigScene currentScene;
    private int lastStep = -1;
    private Dictionary<String, MachineCommonSenseConfigObjectDefinition> objectDictionary =
        new Dictionary<string, MachineCommonSenseConfigObjectDefinition>();
    private Dictionary<string, List<string>> materialRegistry;

    // AI2-THOR Objects and Scripts
    private MachineCommonSenseController agentController;
    private GameObject objectParent;
    private PhysicsSceneManager physicsSceneManager;

    // Room objects
    private GameObject ceiling;
    private GameObject floor;
    private GameObject light;
    private GameObject wallLeft;
    private GameObject wallRight;
    private GameObject wallFront;
    private GameObject wallBack;

    // Unity's Start method is called before the first frame update
    void Start() {
        this.agentController = GameObject.Find("FPSController").GetComponent<MachineCommonSenseController>();
        this.objectParent = GameObject.Find("Objects");
        this.physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
        this.ceiling = GameObject.Find("Ceiling");
        this.floor = GameObject.Find("Floor");
        this.light = GameObject.Find("Point light");
        this.wallLeft = GameObject.Find("Wall Left");
        this.wallRight = GameObject.Find("Wall Right");
        this.wallFront = GameObject.Find("Wall Front");
        this.wallBack = GameObject.Find("Wall Back");

        // Disable all physics simulation (we re-enable it on each step in MachineCommonSenseController).
        Physics.autoSimulation = false;
        this.physicsSceneManager.physicsSimulationPaused = true;

        // Load the configurable game objects from our custom registry files.
        List<MachineCommonSenseConfigObjectDefinition> ai2thorObjects = LoadObjectRegistryFromFile(
            this.ai2thorObjectRegistryFile);
        List<MachineCommonSenseConfigObjectDefinition> mcsObjects = LoadObjectRegistryFromFile(
            this.mcsObjectRegistryFile);
        List<MachineCommonSenseConfigObjectDefinition> primitiveObjects = LoadObjectRegistryFromFile(
            this.primitiveObjectRegistryFile);
        ai2thorObjects.Concat(mcsObjects).Concat(primitiveObjects).ToList().ForEach((objectDefinition) => {
            this.objectDictionary.Add(objectDefinition.id.ToUpper(), objectDefinition);
        });

        // Save the materials (strings) that are accepted in the scene configuration files.
        this.materialRegistry = LoadMaterialRegistryFromFile(this.materialRegistryFile);

        // Load the default MCS scene set in the Unity Editor.
        if (!this.defaultSceneFile.Equals("")) {
            this.currentScene = LoadCurrentSceneFromFile(this.defaultSceneFile);
            this.currentScene.name = ((this.currentScene.name == null || this.currentScene.name.Equals("")) ?
                this.defaultSceneFile : this.currentScene.name);
            ChangeCurrentScene(this.currentScene);
        }
    }

    // Unity's Update method is called once per frame
    void Update() {
        // If the player made a step, update the scene based on the current configuration.
        if (this.lastStep < this.agentController.step) {
            this.lastStep++;
            Debug.Log("MCS: Run Step " + this.lastStep + " at Frame " + Time.frameCount);
            if (this.currentScene != null && this.currentScene.objects != null) {
                bool objectsWereShown = false;
                List<MachineCommonSenseConfigGameObject> objects = this.currentScene.objects.Where(objectConfig =>
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
                    ImageSynthesis imageSynthesis = GameObject.Find("FPSController")
                        .GetComponentInChildren<ImageSynthesis>();
                    if (imageSynthesis != null && imageSynthesis.enabled) {
                        imageSynthesis.OnSceneChange();
                    }
                }
            }
            this.agentController.SimulatePhysics();
        }
    }

    // Custom Methods

    public void ChangeCurrentScene(MachineCommonSenseConfigScene scene) {
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
            this.currentScene.objects.ForEach(InitializeGameObject);
        }

        String ceilingMaterial = (this.currentScene.ceilingMaterial != null &&
            !this.currentScene.ceilingMaterial.Equals("")) ? this.currentScene.ceilingMaterial :
            this.defaultCeilingMaterial;
        String floorMaterial = (this.currentScene.floorMaterial != null &&
            !this.currentScene.floorMaterial.Equals("")) ? this.currentScene.floorMaterial :
            this.defaultFloorMaterial;
        String wallsMaterial = (this.currentScene.wallMaterial != null &&
            !this.currentScene.wallMaterial.Equals("")) ? this.currentScene.wallMaterial :
            this.defaultWallsMaterial;

        if (this.currentScene.observation) {
            this.ceiling.SetActive(false);
            this.wallLeft.transform.position = new Vector3(-1 * MachineCommonSenseMain.WALL_X_POSITION_OBSERVATION,
                MachineCommonSenseMain.WALL_Y_POSITION, MachineCommonSenseMain.WALL_Z_POSITION);
            this.wallRight.transform.position = new Vector3(MachineCommonSenseMain.WALL_X_POSITION_OBSERVATION,
                MachineCommonSenseMain.WALL_Y_POSITION, MachineCommonSenseMain.WALL_Z_POSITION);
            this.currentScene.performerStart = new MachineCommonSenseConfigTransform();
            this.currentScene.performerStart.position = new MachineCommonSenseConfigVector();
            this.currentScene.performerStart.position.z = -4.5f;
            this.currentScene.performerStart.rotation = new MachineCommonSenseConfigVector();
        }
        else {
            this.ceiling.SetActive(true);
            AssignMaterial(this.ceiling, ceilingMaterial);
            this.wallLeft.transform.position = new Vector3(-1 * MachineCommonSenseMain.WALL_X_POSITION_INTERACTION,
                MachineCommonSenseMain.WALL_Y_POSITION, MachineCommonSenseMain.WALL_Z_POSITION);
            this.wallRight.transform.position = new Vector3(MachineCommonSenseMain.WALL_X_POSITION_INTERACTION,
                MachineCommonSenseMain.WALL_Y_POSITION, MachineCommonSenseMain.WALL_Z_POSITION);
        }


        if (this.currentScene.screenshot) {
            this.floor.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallLeft.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallRight.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallFront.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.wallBack.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            this.light.GetComponent<Light>().range = MachineCommonSenseMain.LIGHT_RANGE_SCREENSHOT;
            this.light.transform.position = new Vector3(0, MachineCommonSenseMain.LIGHT_Y_POSITION_SCREENSHOT,
                MachineCommonSenseMain.LIGHT_Z_POSITION_SCREENSHOT);
        }
        else {
            AssignMaterial(this.floor, floorMaterial);
            AssignMaterial(this.wallLeft, wallsMaterial);
            AssignMaterial(this.wallRight, wallsMaterial);
            AssignMaterial(this.wallFront, wallsMaterial);
            AssignMaterial(this.wallBack, wallsMaterial);
            this.light.GetComponent<Light>().range = MachineCommonSenseMain.LIGHT_RANGE;
            this.light.transform.position = new Vector3(0, MachineCommonSenseMain.LIGHT_Y_POSITION,
                MachineCommonSenseMain.LIGHT_Z_POSITION);
        }

        if (this.currentScene.goal != null && this.currentScene.goal.description != null) {
            Debug.Log("MCS: Goal = " + this.currentScene.goal.description);
        }

        GameObject controller = GameObject.Find("FPSController");
        if (this.currentScene.performerStart != null && this.currentScene.performerStart.position != null) {
            // Always keep the Y position on the floor.
            controller.transform.position = new Vector3(this.currentScene.performerStart.position.x,
                MachineCommonSenseController.POSITION_Y, this.currentScene.performerStart.position.z);
        }
        else {
            controller.transform.position = new Vector3(0, MachineCommonSenseController.POSITION_Y, 0);
        }

        if (this.currentScene.performerStart != null && this.currentScene.performerStart.rotation != null) {
            // Only permit rotating left or right (along the Y axis).
            controller.transform.rotation = Quaternion.Euler(0,
                this.currentScene.performerStart.rotation.y, 0);
        }
        else {
            controller.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        this.lastStep = -1;
        this.physicsSceneManager.SetupScene();
    }

    private Collider AssignBoundingBox(
        GameObject gameObject,
        MachineCommonSenseConfigCollider colliderDefinition
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
        MachineCommonSenseConfigCollider colliderDefinition
    ) {
        this.AssignTransform(gameObject, colliderDefinition);

        if (colliderDefinition.type.Equals("box")) {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;
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
        MachineCommonSenseConfigObjectDefinition objectDefinition
    ) {
        // We don't care about existing trigger colliders here so just ignore them.
        Collider[] colliders = gameObject.GetComponentsInChildren<Collider>().Where((collider) =>
            !collider.isTrigger).ToArray();

        if (objectDefinition.colliders.Count > 0) {
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
            colliders.ToList().ForEach((collider) => {
                collider.enabled = false;
            });
            // The AI2-THOR scripts assume the colliders have a parent object with the name Colliders.
            GameObject colliderParentObject = new GameObject {
                isStatic = true,
                name = "Colliders"
            };
            colliderParentObject.transform.parent = gameObject.transform;
            colliderParentObject.transform.localPosition = Vector3.zero;
            int index = 0;
            colliders = objectDefinition.colliders.Select((colliderDefinition) => {
                ++index;
                GameObject colliderObject = new GameObject {
                    isStatic = true,
                    layer = 8, // AI2-THOR Layer SimObjVisible
                    name = gameObject.name + "_collider_" + index,
                    tag = "SimObjPhysics" // AI2-THOR Tag
                };
                colliderObject.transform.parent = colliderParentObject.transform;
                Collider collider = this.AssignCollider(colliderObject, colliderDefinition);
                return collider;
            }).ToArray();
        }
        else {
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

        foreach (KeyValuePair<string, List<string>> materialType in this.materialRegistry) {
            if (materialType.Value.Contains(filename)) {
                if (restrictions.Length == 0 || Array.IndexOf(restrictions, materialType.Key) >= 0) {
                    Material material = Resources.Load<Material>("MCS/" + filename);
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
        MachineCommonSenseConfigGameObject objectConfig,
        MachineCommonSenseConfigObjectDefinition objectDefinition
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

        if (objectConfig.structure) {
            // Add the AI2-THOR Structure script with specific properties.
            this.AssignStructureScript(gameObject);
        }

        // See if each SimObjPhysics property is active on this specific object or on all objects of this type.
        // Currently you can't deactivate the properties on specific objects, since we don't need to do that right now.
        bool moveable = objectConfig.moveable || objectDefinition.moveable;
        bool openable = objectConfig.openable || objectDefinition.openable;
        bool pickupable = objectConfig.pickupable || objectDefinition.pickupable;
        bool receptacle = objectConfig.receptacle || objectDefinition.receptacle;

        bool shouldAddSimObjPhysicsScript = moveable || openable || pickupable || receptacle || objectConfig.physics ||
            objectDefinition.physics;

        Collider[] colliders = new Collider[] { };
        Transform[] visibilityPoints = new Transform[] { };

        if (shouldAddSimObjPhysicsScript) {
            // Add Unity Rigidbody and Collider components to enable physics on this object.
            this.AssignRigidbody(gameObject, objectConfig.mass > 0 ? objectConfig.mass : objectDefinition.mass,
                objectConfig.kinematic || objectDefinition.kinematic);
            colliders = this.AssignColliders(gameObject, objectDefinition);
        }

        // The object's visibility points define a subset of points along the outside of the object for AI2-THOR.
        if (objectDefinition.visibilityPoints.Count > 0) {
            visibilityPoints = this.AssignVisibilityPoints(gameObject, objectDefinition.visibilityPoints,
                objectDefinition.visibilityPointsScaleOne);
        }

        if (shouldAddSimObjPhysicsScript) {
            // Add the AI2-THOR SimObjPhysics script with specific properties.
            this.AssignSimObjPhysicsScript(gameObject, objectConfig, objectDefinition, colliders, visibilityPoints,
                moveable, openable, pickupable, receptacle);
        }
        // If the object has a SimObjPhysics script for some reason, ensure its tag and ID are set correctly.
        else if (gameObject.GetComponent<SimObjPhysics>() != null) {
            gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            gameObject.GetComponent<SimObjPhysics>().uniqueID = gameObject.name;
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
        MachineCommonSenseConfigTransform receptacleTriggerBoxDefinition
    ) {
        GameObject receptacleTriggerBoxObject = new GameObject {
            isStatic = true,
            layer = 9, // AI2-THOR Layer SimObjInvisible
            name = "ReceptacleTriggerBox",
            tag = "Receptacle" // AI2-THOR Tag
        };
        receptacleTriggerBoxObject.transform.parent = gameObject.transform;

        MachineCommonSenseConfigCollider colliderDefinition = new MachineCommonSenseConfigCollider {
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
        MachineCommonSenseConfigObjectDefinition objectDefinition
    ) {
        // If we've configured new trigger box definitions but trigger boxes already exist, delete them.
        if (gameObject.transform.Find("ReceptacleTriggerBox") != null) {
            Destroy(gameObject.transform.Find("ReceptacleTriggerBox").gameObject);
        }

        // If this object will have multiple trigger boxes, create a common parent.
        GameObject receptacleParentObject = null;
        if (objectDefinition.receptacleTriggerBoxes.Count > 1) {
            receptacleParentObject = new GameObject {
                isStatic = true,
                layer = 9, // AI2-THOR Layer SimObjInvisible
                name = "ReceptacleTriggerBox",
                tag = "Receptacle" // AI2-THOR Tag
            };
            receptacleParentObject.transform.parent = gameObject.transform;
        }

        int index = 0;
        return objectDefinition.receptacleTriggerBoxes.Select((receptacleDefinition) => {
            GameObject receptacleObject = this.AssignReceptacleTrigglerBox(gameObject, receptacleDefinition);
            // If this object will have multiple trigger boxes, rename and assign each to a common parent.
            if (receptacleParentObject != null) {
                ++index;
                receptacleObject.name = gameObject.name + "_receptacle_trigger_box_" + index;
                receptacleObject.transform.parent = receptacleParentObject.transform;
            }
            return receptacleObject;
        }).ToArray();
    }

    private void AssignRigidbody(GameObject gameObject, float mass, bool kinematic) {
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
    }

    private void AssignSimObjPhysicsScript(
        GameObject gameObject,
        MachineCommonSenseConfigGameObject objectConfig,
        MachineCommonSenseConfigObjectDefinition objectDefinition,
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
            /* TODO MCS-75 We should let people set these properties in the JSON config file.
            ai2thorPhysicsScript.HFdynamicfriction
            ai2thorPhysicsScript.HFstaticfriction
            ai2thorPhysicsScript.HFbounciness
            ai2thorPhysicsScript.HFrbdrag
            ai2thorPhysicsScript.HFrbangulardrag
            */
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

        // Always set the uniqueID to a new name (we don't want to use AI2-THOR's default names).
        ai2thorPhysicsScript.uniqueID = gameObject.name;

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
            ai2thorPhysicsScript.ReceptacleTriggerBoxes = this.AssignReceptacleTriggerBoxes(gameObject,
                objectDefinition);
        }

        // The object's bounding box defines the complete bounding box around the object for AI2-THOR.
        // JsonUtility always sets objectDefinition.boundingBox, so verify that the position is not null.
        if (objectDefinition.boundingBox != null && objectDefinition.boundingBox.position != null) {
            ai2thorPhysicsScript.BoundingBox = this.AssignBoundingBox(gameObject, objectDefinition.boundingBox)
                .gameObject;
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
        MachineCommonSenseConfigTransform transformDefinition
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
        List<MachineCommonSenseConfigVector> points,
        bool scaleOne
    ) {
        // The AI2-THOR scripts assume the visibility points have a parent object with the name VisibilityPoints.
        GameObject visibilityPointsParentObject = new GameObject {
            isStatic = true,
            name = "VisibilityPoints"
        };
        visibilityPointsParentObject.transform.parent = gameObject.transform;
        visibilityPointsParentObject.transform.localPosition = Vector3.zero;
        visibilityPointsParentObject.transform.localRotation = Quaternion.identity;
        if (scaleOne) {
            visibilityPointsParentObject.transform.localScale = Vector3.one;
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

    private GameObject CreateCustomGameObject(
        MachineCommonSenseConfigGameObject objectConfig,
        MachineCommonSenseConfigObjectDefinition objectDefinition
    ) {
        GameObject gameObject = Instantiate(Resources.Load("MCS/" + objectDefinition.resourceFile,
            typeof(GameObject))) as GameObject;

        LogVerbose("LOAD CUSTOM GAME OBJECT " + objectDefinition.id + " FROM FILE Assets/Resources/MCS/" +
            objectDefinition.resourceFile + (gameObject == null ? " IS NULL" : " IS DONE"));

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
                    AnimationClip clip = Resources.Load<AnimationClip>("MCS/" +
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
            MachineCommonSenseConfigAnimator animatorDefinition = objectDefinition.animators
                .Where(cont => cont.id.Equals(objectConfig.controller)).ToList().First();
            if (animatorDefinition.animatorFile != null && !animatorDefinition.animatorFile.Equals("")) {
                Animator animator = gameObject.GetComponent<Animator>();
                if (animator == null) {
                    animator = gameObject.AddComponent<Animator>();
                    LogVerbose("ASSIGN NEW ANIMATOR CONTROLLER TO GAME OBJECT " + gameObject.name);
                }
                RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>(
                    "MCS/" + animatorDefinition.animatorFile);
                LogVerbose("LOAD OF ANIMATOR CONTROLLER FILE Assets/Resources/MCS/" +
                    animatorDefinition.animatorFile + (animatorController == null ? " IS NULL" : " IS DONE"));
                animator.runtimeAnimatorController = animatorController;
            }
        }

        return gameObject;
    }

    private GameObject CreateGameObject(MachineCommonSenseConfigGameObject objectConfig) {
        MachineCommonSenseConfigObjectDefinition objectDefinition = this.objectDictionary[objectConfig.type.ToUpper()];
        if (objectDefinition != null) {
            if (objectDefinition.primitive) {
                switch (objectConfig.type) {
                    case "capsule":
                        return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Capsule), objectConfig,
                            objectDefinition);
                    case "cube":
                        return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Cube), objectConfig,
                            objectDefinition);
                    case "cylinder":
                        return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Cylinder), objectConfig,
                            objectDefinition);
                    case "plane":
                        return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Plane), objectConfig,
                            objectDefinition);
                    case "quad":
                        return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Quad), objectConfig,
                            objectDefinition);
                    case "sphere":
                        return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Sphere), objectConfig,
                            objectDefinition);
                }
            }
            return CreateCustomGameObject(objectConfig, objectDefinition);
        }
        return null;
    }

    private GameObject CreateNullParentObjectIfNeeded(MachineCommonSenseConfigGameObject objectConfig) {
        // Null parents are useful if we want to rotate an object but don't want to pivot around its center point.
        if (objectConfig.nullParent != null && (objectConfig.nullParent.position != null ||
            objectConfig.nullParent.rotation != null)) {

            GameObject parentObject = new GameObject();
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

    private void InitializeGameObject(MachineCommonSenseConfigGameObject objectConfig) {
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

    private MachineCommonSenseConfigScene LoadCurrentSceneFromFile(String filePath) {
        TextAsset currentSceneFile = Resources.Load<TextAsset>("MCS/Scenes/" + filePath);
        Debug.Log("MCS: Config file Assets/Resources/MCS/Scenes/" + filePath + ".json" + (currentSceneFile == null ?
            " is null!" : (":\n" + currentSceneFile.text)));
        return JsonUtility.FromJson<MachineCommonSenseConfigScene>(currentSceneFile.text);
    }

    private Dictionary<string, List<string>> LoadMaterialRegistryFromFile(String filePath) {
        TextAsset materialRegistryFile = Resources.Load<TextAsset>("MCS/" + filePath);
        Debug.Log("MCS: Config file Assets/Resources/MCS/" + filePath + ".json" + (materialRegistryFile == null ?
            " is null!" : (":\n" + materialRegistryFile.text)));
        MachineCommonSenseConfigMaterialRegistry materialJson =
            JsonUtility.FromJson<MachineCommonSenseConfigMaterialRegistry>(materialRegistryFile.text);
        Dictionary<string, List<string>> materialDictionary = new Dictionary<string, List<string>>() {
            { "block_blank", materialJson.block_blank },
            { "block_design", materialJson.block_design },
            { "ceramic", materialJson.ceramic },
            { "fabric", materialJson.fabric },
            { "metal", materialJson.metal },
            { "plastic", materialJson.plastic },
            { "rubber", materialJson.rubber },
            { "wall", materialJson.wall },
            { "wood", materialJson.wood }
        };
        return materialDictionary;
    }

    private List<MachineCommonSenseConfigObjectDefinition> LoadObjectRegistryFromFile(String filePath) {
        TextAsset objectRegistryFile = Resources.Load<TextAsset>("MCS/" + filePath);
        Debug.Log("MCS: Config file Assets/Resources/MCS/" + filePath + ".json" + (objectRegistryFile == null ?
            " is null!" : (":\n" + objectRegistryFile.text)));
        MachineCommonSenseConfigObjectRegistry objectRegistry = JsonUtility
            .FromJson<MachineCommonSenseConfigObjectRegistry>(objectRegistryFile.text);
        return objectRegistry.objects;
    }

    private void LogVerbose(String text) {
        if (this.enableVerboseLog) {
            Debug.Log("MCS: " + text);
        }
    }

    private void ModifyChildrenInteractables(
        GameObject gameObject,
        List<MachineCommonSenseConfigInteractables> interactableDefinitions
    ) {
        interactableDefinitions.ForEach((interactableDefinition) => {
            // Override properties of SimObjPhysics children in existing AI2-THOR prefab objects if needed.
            Transform interactableTransform = gameObject.transform.Find(interactableDefinition.name);
            if (interactableTransform != null) {
                GameObject interactableObject = interactableTransform.gameObject;
                SimObjPhysics ai2thorPhysicsScript = interactableObject.GetComponent<SimObjPhysics>();
                if (ai2thorPhysicsScript) {
                    ai2thorPhysicsScript.uniqueID = gameObject.name + "_" + interactableDefinition.id;
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
        List<MachineCommonSenseConfigOverride> overrideDefinitions
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

    private void PostUpdateGameObjectOnStep(List<MachineCommonSenseConfigGameObject> objectConfigs, int step) {
        objectConfigs.ForEach(objectConfig => {
            // If an object's location is set in relation to another object, modify its location in the PostUpdate to
            // ensure that any update to the other object's location is finished.
            if (objectConfig.locationParent != null && !objectConfig.locationParent.Equals("")) {
                objectConfig.shows.Where(show => show.stepBegin == step).ToList().ForEach((show) => {
                    MachineCommonSenseConfigGameObject[] originObjectConfigs = objectConfigs.Where(possibleConfig =>
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

    private bool UpdateGameObjectOnStep(MachineCommonSenseConfigGameObject objectConfig, int step) {
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
                Debug.Log("TELEPORT " + gameOrParentObject.transform.position.ToString("F4"));
            });

        objectConfig.forces.Where(force => force.stepBegin <= step && force.stepEnd >= step && force.vector != null)
            .ToList().ForEach((force) => {
                Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.AddForce(new Vector3(force.vector.x, force.vector.y, force.vector.z));
                }
            });

        objectConfig.torques.Where(torque => torque.stepBegin <= step && torque.stepEnd >= step &&
            torque.vector != null).ToList().ForEach((torque) => {
                Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.AddTorque(new Vector3(torque.vector.x, torque.vector.y, torque.vector.z));
                }
            });

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

}

// Definitions of serializable objects from JSON config files.

[Serializable]
public class MachineCommonSenseConfigAbstractObject {
    public string id;
    public bool kinematic;
    public float mass;
    public bool moveable;
    public bool openable;
    public bool opened;
    public bool physics;
    public bool pickupable;
    public bool receptacle;
    public List<string> materials;
    public List<string> salientMaterials;
}

[Serializable]
public class MachineCommonSenseConfigAction : MachineCommonSenseConfigStepBegin {
    public string id;
}

[Serializable]
public class MachineCommonSenseConfigAnimation {
    public string id;
    public string animationFile;
}

[Serializable]
public class MachineCommonSenseConfigAnimator {
    public string id;
    public string animatorFile;
}

[Serializable]
public class MachineCommonSenseConfigCollider : MachineCommonSenseConfigTransform {
    public string type;
    public float height;
    public float radius;
}

[Serializable]
public class MachineCommonSenseConfigGameObject : MachineCommonSenseConfigAbstractObject {
    public string controller;
    public string locationParent;
    public string materialFile; // deprecated; please use materials
    public MachineCommonSenseConfigTransform nullParent = null;
    public bool structure;
    public string type;
    public List<MachineCommonSenseConfigAction> actions;
    public List<MachineCommonSenseConfigMove> forces;
    public List<MachineCommonSenseConfigStepBegin> hides;
    public List<MachineCommonSenseConfigMove> moves;
    public List<MachineCommonSenseConfigResize> resizes;
    public List<MachineCommonSenseConfigMove> rotates;
    public List<MachineCommonSenseConfigShow> shows;
    public List<MachineCommonSenseConfigTeleport> teleports;
    public List<MachineCommonSenseConfigMove> torques;

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
public class MachineCommonSenseConfigInteractables {
    public string id;
    public string name;
}

[Serializable]
public class MachineCommonSenseConfigMove : MachineCommonSenseConfigStepBeginEnd {
    public MachineCommonSenseConfigVector vector;
}

[Serializable]
public class MachineCommonSenseConfigObjectDefinition : MachineCommonSenseConfigAbstractObject {
    public string resourceFile;
    public bool primitive;
    public bool visibilityPointsScaleOne;
    public MachineCommonSenseConfigCollider boundingBox = null;
    public MachineCommonSenseConfigSize scale = null;
    public List<MachineCommonSenseConfigAnimation> animations;
    public List<MachineCommonSenseConfigAnimator> animators;
    public List<MachineCommonSenseConfigCollider> colliders;
    public List<MachineCommonSenseConfigInteractables> interactables;
    public List<string> materialRestrictions;
    public List<MachineCommonSenseConfigOverride> overrides;
    public List<MachineCommonSenseConfigTransform> receptacleTriggerBoxes;
    public List<MachineCommonSenseConfigVector> visibilityPoints;
}

[Serializable]
public class MachineCommonSenseConfigOverride : MachineCommonSenseConfigTransform {
    public string name;
}

[Serializable]
public class MachineCommonSenseConfigResize : MachineCommonSenseConfigStepBeginEnd {
    public MachineCommonSenseConfigSize size;
}

[Serializable]
public class MachineCommonSenseConfigShow : MachineCommonSenseConfigStepBegin {
    public MachineCommonSenseConfigVector position;
    public MachineCommonSenseConfigVector rotation;
    public MachineCommonSenseConfigSize scale;
}

[Serializable]
public class MachineCommonSenseConfigSize {
    [SerializeField]
    private float x;
    [SerializeField]
    private float y;
    [SerializeField]
    private float z;

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
public class MachineCommonSenseConfigStepBegin {
    public int stepBegin;
}

[Serializable]
public class MachineCommonSenseConfigStepBeginEnd : MachineCommonSenseConfigStepBegin {
    public int stepEnd;
}

[Serializable]
public class MachineCommonSenseConfigTransform {
    public MachineCommonSenseConfigVector position = null;
    public MachineCommonSenseConfigVector rotation = null;
    public MachineCommonSenseConfigSize scale = null;
}

[Serializable]
public class MachineCommonSenseConfigVector {
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class MachineCommonSenseConfigScene {
    public String name;
    public String ceilingMaterial;
    public String floorMaterial;
    public String wallMaterial;
    public bool observation;
    public bool screenshot;
    public MachineCommonSenseConfigGoal goal;
    public MachineCommonSenseConfigTransform performerStart = null;
    public List<MachineCommonSenseConfigGameObject> objects;
}

[Serializable]
public class MachineCommonSenseConfigTeleport : MachineCommonSenseConfigStepBegin {
    public MachineCommonSenseConfigVector position;
}

[Serializable]
public class MachineCommonSenseConfigGoal {
    public string description;
}

[Serializable]
public class MachineCommonSenseConfigMaterialRegistry {
    public List<String> block_blank;
    public List<String> block_design;
    public List<String> ceramic;
    public List<String> fabric;
    public List<String> metal;
    public List<String> plastic;
    public List<String> rubber;
    public List<String> wall;
    public List<String> wood;
}

[Serializable]
public class MachineCommonSenseConfigObjectRegistry {
    public List<MachineCommonSenseConfigObjectDefinition> objects;
}
