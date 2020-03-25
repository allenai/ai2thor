using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System.Text;

public class MachineCommonSenseMain : MonoBehaviour {
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
    private List<String> materials;

    // AI2-THOR Objects and Scripts
    private MachineCommonSenseController agentController;
    private GameObject objectParent;
    private PhysicsSceneManager physicsSceneManager;

    // Unity's Start method is called before the first frame update
    void Start() {
        this.agentController = GameObject.Find("FPSController").GetComponent<MachineCommonSenseController>();
        this.objectParent = GameObject.Find("Objects");
        this.physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();

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
        this.materials = LoadMaterialRegistryFromFile(this.materialRegistryFile).materials;

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
        if (this.lastStep < agentController.step) {
            this.lastStep++;
            LogVerbose("Run Step " + this.lastStep + " at Frame " + Time.frameCount);
            if (this.currentScene != null && this.currentScene.objects != null) {
                // Loop over each configuration object in the scene and update if needed.
                this.currentScene.objects.Where(objectConfig => objectConfig.GetGameObject() != null).ToList()
                    .ForEach(objectConfig => {
                        bool objectsWereShown = UpdateGameObjectOnStep(objectConfig, this.lastStep);
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
                    });
            }
            if (agentController.step == 0) {
                // After initialization, simulate the physics so that objects can settle down onto the floor.
                agentController.SimulatePhysics();
            }
        }
    }

    // Custom Methods

    public void ChangeCurrentScene(MachineCommonSenseConfigScene scene) {
        if (scene == null && this.currentScene == null) {
            Debug.LogError("MCS:  Cannot switch the MCS scene to null... Keeping the current MCS scene.");
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
            Debug.Log("MCS:  Switching the current MCS scene to " + scene.name);
        } else {
            Debug.Log("MCS:  Resetting the current MCS scene...");
        }

        if (this.currentScene != null && this.currentScene.objects != null) {
            this.currentScene.objects.ForEach(InitializeGameObject);
        }

        String ceiling = (this.currentScene.ceilingMaterial != null && !this.currentScene.ceilingMaterial.Equals("")) ?
            this.currentScene.ceilingMaterial : this.defaultCeilingMaterial;
        String floor = (this.currentScene.floorMaterial != null && !this.currentScene.floorMaterial.Equals("")) ?
            this.currentScene.floorMaterial : this.defaultFloorMaterial;
        String walls = (this.currentScene.wallMaterial != null && !this.currentScene.wallMaterial.Equals("")) ?
            this.currentScene.wallMaterial : this.defaultWallsMaterial;

        AssignMaterial(GameObject.Find("Ceiling"), ceiling);
        AssignMaterial(GameObject.Find("Floor"), floor);
        AssignMaterial(GameObject.Find("Wall Back"), walls);
        AssignMaterial(GameObject.Find("Wall Front"), walls);
        AssignMaterial(GameObject.Find("Wall Left"), walls);
        AssignMaterial(GameObject.Find("Wall Right"), walls);

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

        // Deactivate any concave MeshCollider.  We expect other collider(s) to be defined on the object.
        // Concave MeshCollider cause Unity errors with our object's ContinuousDynamic Rigidbody component.
        colliders.ToList().ForEach((collider) => {
            if (collider is MeshCollider) {
                if (!((MeshCollider)collider).convex) {
                    // TODO Do we need to do more?
                    Debug.LogWarning("Deactivating concave MeshCollider in GameObject " + gameObject.name);
                    collider.enabled = false;
                }
            }
        });

        if (objectDefinition.colliders.Count > 0) {
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

        return colliders;
    }

    private Material AssignMaterial(GameObject gameObject, String materialFile) {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (materialFile != null && !materialFile.Equals("")) {
            // Backwards compatibility
            String fileToLoad = (materialFile.StartsWith("AI2-THOR/Materials/") ? "" : "AI2-THOR/Materials/") +
                materialFile; 
            if (this.materials.Contains(fileToLoad)) {
                Material material = Resources.Load<Material>("MCS/" + fileToLoad);
                LogVerbose("LOAD OF MATERIAL FILE Assets/Resources/MCS/" + fileToLoad +
                    (material == null ?  " IS NULL" : " IS DONE"));
                if (material != null) {
                    renderer.material = material;
                    LogVerbose("ASSIGN MATERIAL " + fileToLoad + " TO GAME OBJECT " + gameObject.name);
                }
                return material;
            }
            else {
                LogVerbose("MATERIAL " + fileToLoad + " NOT IN MATERIAL REGISTRY");
            }
        }
        return null;
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

        bool shouldAddSimObjPhysicsScript = moveable || openable || pickupable || receptacle;

        Collider[] colliders = new Collider[] { };

        if (objectConfig.physics || shouldAddSimObjPhysicsScript) {
            // Add Unity Rigidbody and Collider components to enable physics on this object.
            this.AssignRigidbody(gameObject, objectConfig);
            colliders = this.AssignColliders(gameObject, objectDefinition);
        }

        if (shouldAddSimObjPhysicsScript) {
            // Add the AI2-THOR SimObjPhysics script with specific properties.
            this.AssignSimObjPhysicsScript(gameObject, objectConfig, objectDefinition, colliders, moveable,
                openable, pickupable, receptacle);
        }
        // If the object has a SimObjPhysics script for some reason, ensure its tag and ID are set correctly.
        else if (gameObject.GetComponent<SimObjPhysics>() != null) {
            gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            gameObject.GetComponent<SimObjPhysics>().uniqueID = gameObject.name;
        }

        this.AssignMaterial(gameObject, objectConfig.materialFile);

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

    private void AssignRigidbody(GameObject gameObject, MachineCommonSenseConfigGameObject objectConfig) {
        // Note that some prefabs may already have a Rigidbody component.
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null) {
            rigidbody = gameObject.AddComponent<Rigidbody>();
            LogVerbose("ASSIGN RIGID BODY TO GAME OBJECT " + gameObject.name);
        }
        // Set isKinematic to false by default so the objects are always affected by the physics simulation.
        rigidbody.isKinematic = objectConfig.kinematic;
        // Set the mode to continuous dynamic or else fast moving objects may pass through other objects.
        rigidbody.collisionDetectionMode = objectConfig.kinematic ? CollisionDetectionMode.Discrete :
            CollisionDetectionMode.ContinuousDynamic;
        if (objectConfig.mass > 0) {
            rigidbody.mass = objectConfig.mass;
        }
    }

    private void AssignSimObjPhysicsScript(
        GameObject gameObject,
        MachineCommonSenseConfigGameObject objectConfig,
        MachineCommonSenseConfigObjectDefinition objectDefinition,
        Collider[] colliders,
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
            ai2thorPhysicsScript.PrimaryProperty = (pickupable ? SimObjPrimaryProperty.CanPickup : (moveable ?
                SimObjPrimaryProperty.Moveable : SimObjPrimaryProperty.Static));
            ai2thorPhysicsScript.SecondaryProperties = (receptacle ? new SimObjSecondaryProperty[] {
                SimObjSecondaryProperty.Receptacle
            } : new SimObjSecondaryProperty[] { }).Concat(openable ? new SimObjSecondaryProperty[] {
                SimObjSecondaryProperty.CanOpen
            } : new SimObjSecondaryProperty[] { }).ToArray();
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
            ai2thorPhysicsScript.salientMaterials = this.RetrieveSalientMaterials(objectConfig.salientMaterials);
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

        // The object's visibility points define a subset of points along the outside of the object for AI2-THOR.
        if (objectDefinition.visibilityPoints.Count > 0) {
            List<Transform> visibilityPoints = this.AssignVisibilityPoints(gameObject,
                objectDefinition.visibilityPoints);
            ai2thorPhysicsScript.VisibilityPoints = visibilityPoints.ToArray();
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

                    ai2thorCanOpenObjectScript.Interact();
                }
                ai2thorCanOpenObjectScript.isOpenByPercentage = ai2thorCanOpenObjectScript.isOpen ? 1 : 0;
            }
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

    private List<Transform> AssignVisibilityPoints(
        GameObject gameObject,
        List<MachineCommonSenseConfigVector> points
    ) {
        // The AI2-THOR scripts assume the visibility points have a parent object with the name VisibilityPoints.
        GameObject visibilityPointsParentObject = new GameObject {
            isStatic = true,
            name = "VisibilityPoints"
        };
        visibilityPointsParentObject.transform.parent = gameObject.transform;
        visibilityPointsParentObject.transform.localPosition = Vector3.zero;
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
        }).ToList();
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
            Debug.LogError("MCS:  " + e);
        }
    }

    private MachineCommonSenseConfigScene LoadCurrentSceneFromFile(String filePath) {
        TextAsset currentSceneFile = Resources.Load<TextAsset>("MCS/Scenes/" + filePath);
        Debug.Log("MCS:  Config file Assets/Resources/MCS/Scenes/" + filePath + ".json" + (currentSceneFile == null ?
            " is null!" : (":\n" + currentSceneFile.text)));
        return JsonUtility.FromJson<MachineCommonSenseConfigScene>(currentSceneFile.text);
    }

    private MachineCommonSenseConfigMaterialRegistry LoadMaterialRegistryFromFile(String filePath) {
        TextAsset materialRegistryFile = Resources.Load<TextAsset>("MCS/" + filePath);
        Debug.Log("MCS:  Config file Assets/Resources/MCS/" + filePath + ".json" + (materialRegistryFile == null ?
            " is null!" : (":\n" + materialRegistryFile.text)));
        return JsonUtility.FromJson<MachineCommonSenseConfigMaterialRegistry>(materialRegistryFile.text);
    }

    private List<MachineCommonSenseConfigObjectDefinition> LoadObjectRegistryFromFile(String filePath) {
        TextAsset objectRegistryFile = Resources.Load<TextAsset>("MCS/" + filePath);
        Debug.Log("MCS:  Config file Assets/Resources/MCS/" + filePath + ".json" + (objectRegistryFile == null ?
            " is null!" : (":\n" + objectRegistryFile.text)));
        MachineCommonSenseConfigObjectRegistry objectRegistry = JsonUtility
            .FromJson<MachineCommonSenseConfigObjectRegistry>(objectRegistryFile.text);
        return objectRegistry.objects;
    }

    private void LogVerbose(String text) {
        if (this.enableVerboseLog) {
            Debug.Log("MCS:  " + text);
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
                    rigidbody.isKinematic = false;
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

        objectConfig.rotates.Where(rotate => rotate.stepBegin <= step && rotate.stepEnd >= step &&
            rotate.vector != null).ToList().ForEach((rotate) => {
                gameOrParentObject.transform.Rotate(new Vector3(rotate.vector.x, rotate.vector.y, rotate.vector.z));
            });

        objectConfig.moves.Where(move => move.stepBegin <= step && move.stepEnd >= step && move.vector != null)
            .ToList().ForEach((move) => {
                gameOrParentObject.transform.Translate(new Vector3(move.vector.x, move.vector.y, move.vector.z));
            });

        objectConfig.forces.Where(force => force.stepBegin <= step && force.stepEnd >= step && force.vector != null)
            .ToList().ForEach((force) => {
                Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    rigidbody.AddForce(new Vector3(force.vector.x, force.vector.y, force.vector.z));
                }
            });

        objectConfig.torques.Where(torque => torque.stepBegin <= step && torque.stepEnd >= step &&
            torque.vector != null).ToList().ForEach((torque) => {
                Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
                if (rigidbody != null) {
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
}

// Definitions of serializable objects from JSON config files.

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
public class MachineCommonSenseConfigGameObject {
    public string id;
    public string controller;
    public bool kinematic;
    public float mass;
    public string materialFile;
    public bool moveable;
    public MachineCommonSenseConfigTransform nullParent = null;
    public bool openable;
    public bool opened;
    public bool physics; // deprecated
    public bool pickupable;
    public bool receptacle;
    public bool structure;
    public string type;
    public List<MachineCommonSenseConfigAction> actions;
    public List<MachineCommonSenseConfigMove> forces;
    public List<MachineCommonSenseConfigStepBegin> hides;
    public List<MachineCommonSenseConfigMove> moves;
    public List<MachineCommonSenseConfigResize> resizes;
    public List<MachineCommonSenseConfigMove> rotates;
    public List<string> salientMaterials;
    public List<MachineCommonSenseConfigShow> shows;
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
public class MachineCommonSenseConfigObjectDefinition {
    public string id;
    public string resourceFile;
    public bool moveable;
    public bool openable;
    public bool pickupable;
    public bool primitive;
    public bool receptacle;
    public MachineCommonSenseConfigCollider boundingBox = null;
    public MachineCommonSenseConfigSize scale = null;
    public List<MachineCommonSenseConfigAnimation> animations;
    public List<MachineCommonSenseConfigAnimator> animators;
    public List<MachineCommonSenseConfigCollider> colliders;
    public List<MachineCommonSenseConfigInteractables> interactables;
    public List<MachineCommonSenseConfigOverride> overrides;
    public List<MachineCommonSenseConfigTransform> receptacleTriggerBoxes;
    public List<string> salientMaterials;
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
    public MachineCommonSenseConfigTransform performerStart = null;
    public List<MachineCommonSenseConfigGameObject> objects;
}

[Serializable]
public class MachineCommonSenseConfigMaterialRegistry {
    public List<String> materials;
}

[Serializable]
public class MachineCommonSenseConfigObjectRegistry {
    public List<MachineCommonSenseConfigObjectDefinition> objects;
}
