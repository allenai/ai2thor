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
    public string mcsObjectRegistryFile = "mcs_object_registry";
    public string primitiveObjectRegistryFile = "primitive_object_registry";

    private MachineCommonSenseConfigScene currentScene;
    private Dictionary<String, MachineCommonSenseConfigObjectDefinition> objectDictionary =
        new Dictionary<string, MachineCommonSenseConfigObjectDefinition>();

    private int lastStep = -1;

    private MachineCommonSenseController agentController;
    private PhysicsSceneManager physicsSceneManager;

    // Unity's Start method is called before the first frame update
    void Start() {
        this.agentController = GameObject.Find("FPSController").GetComponent<MachineCommonSenseController>();
        this.physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();

        // Disable all physics simulation (we re-enable it on each step in MachineCommonSenseController).
        Physics.autoSimulation = false;
        this.physicsSceneManager.physicsSimulationPaused = true;

        // Load the configurable game objects from our custom registry files.
        List<MachineCommonSenseConfigObjectDefinition> mcsObjects = LoadObjectRegistryFromFile(
            this.mcsObjectRegistryFile);
        List<MachineCommonSenseConfigObjectDefinition> primitiveObjects = LoadObjectRegistryFromFile(
            this.primitiveObjectRegistryFile);
        mcsObjects.Concat(primitiveObjects).ToList().ForEach((objectDefinition) => {
            this.objectDictionary.Add(objectDefinition.id, objectDefinition);
        });

        // Load the default MCS scene set in the Unity Editor.
        if (!this.defaultSceneFile.Equals("")) {
            this.currentScene = LoadCurrentSceneFromFile(this.defaultSceneFile);
            this.currentScene.id = ((this.currentScene.id == null || this.currentScene.id.Equals("")) ?
                this.defaultSceneFile : this.currentScene.id);
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
                            this.physicsSceneManager.ResetUniqueIdToSimObjPhysics();
                            // Notify ImageSynthesis so the objects will appear in the masks.
                            ImageSynthesis imageSynthesis = GameObject.Find("FPSController")
                                .GetComponentInChildren<ImageSynthesis>();
                            if (imageSynthesis != null && imageSynthesis.enabled) {
                                imageSynthesis.OnSceneChange();
                            }
                        }
                    });
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
            Debug.Log("MCS:  Switching the current MCS scene to " + scene.id);
        } else {
            Debug.Log("MCS:  Resetting the current MCS scene...");
        }

        this.currentScene.objects.ForEach(InitializeGameObject);

        AssignMaterial(GameObject.Find("Ceiling"), this.currentScene.ceilingMaterial);
        AssignMaterial(GameObject.Find("Floor"), this.currentScene.floorMaterial);
        AssignMaterial(GameObject.Find("Wall Back"), this.currentScene.wallMaterial);
        AssignMaterial(GameObject.Find("Wall Front"), this.currentScene.wallMaterial);
        AssignMaterial(GameObject.Find("Wall Left"), this.currentScene.wallMaterial);
        AssignMaterial(GameObject.Find("Wall Right"), this.currentScene.wallMaterial);

        if (this.currentScene.performerStart != null) {
            GameObject controller = GameObject.Find("FPSController");
            controller.transform.position = new Vector3(this.currentScene.performerStart.x,
                this.currentScene.performerStart.y, this.currentScene.performerStart.z);
        }

        this.lastStep = -1;
        this.physicsSceneManager.SetupScene();
    }

    private Collider AssignBoundingBox(
        GameObject gameObject,
        MachineCommonSenseConfigColliderDefinition colliderDefinition
    ) {
        // The AI2-THOR bounding box property is always a box collider.
        colliderDefinition.type = "box";
        GameObject boundingBoxObject = new GameObject();
        boundingBoxObject.name = gameObject.name + "_bounding_box";
        boundingBoxObject.transform.parent = gameObject.transform;
        Collider boundingBox = AssignCollider(boundingBoxObject, colliderDefinition);
        // The AI2-THOR documentation says to deactive the bounding box collider.
        boundingBox.enabled = false;
        return boundingBox;
    }

    private Collider AssignCollider(
        GameObject gameObject,
        MachineCommonSenseConfigColliderDefinition colliderDefinition
    ) {
        Vector3 center = colliderDefinition.center == null ? new Vector3(0, 0, 0) : new Vector3(
            colliderDefinition.center.x, colliderDefinition.center.y, colliderDefinition.center.z);
        Vector3 size = colliderDefinition.size == null ? new Vector3(1, 1, 1) : new Vector3(
            colliderDefinition.size.GetX(), colliderDefinition.size.GetY(), colliderDefinition.size.GetZ());

        if (colliderDefinition.type.Equals("box")) {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = center;
            boxCollider.size = size;
            LogVerbose("ASSIGN BOX COLLIDER TO GAME OBJECT " + gameObject.name);
            return boxCollider;
        }

        if (colliderDefinition.type.Equals("capsule")) {
            CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            capsuleCollider.center = center;
            capsuleCollider.height = colliderDefinition.height;
            capsuleCollider.radius = colliderDefinition.radius;
            LogVerbose("ASSIGN CAPSULE COLLIDER TO GAME OBJECT " + gameObject.name);
            return capsuleCollider;
        }

        if (colliderDefinition.type.Equals("sphere")) {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.center = center;
            sphereCollider.radius = colliderDefinition.radius;
            return sphereCollider;
        }

        return null;
    }

    private Material AssignMaterial(GameObject gameObject, String materialFile) {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (materialFile != null && !materialFile.Equals("")) {
            Material material = Resources.Load<Material>("MCS/Materials/" + materialFile);
            LogVerbose("LOAD OF MATERIAL FILE Assets/Resources/MCS/Materials/" + materialFile + (material == null ?
                " IS NULL" : " IS DONE"));
            if (material != null) {
                renderer.material = material;
                LogVerbose("ASSIGN MATERIAL " + materialFile + " TO GAME OBJECT " + gameObject.name);
            }
            return material;
        }
        return null;
    }

    private GameObject AssignProperties(
        GameObject gameObject,
        MachineCommonSenseConfigGameObject objectConfig,
        MachineCommonSenseConfigObjectDefinition objectDefinition
    ) {
        gameObject.name = objectConfig.id;
        gameObject.tag = "SimObj"; // AI2-THOR Tag
        gameObject.layer = 8; // AI2-THOR Layer SimObjVisible

        LogVerbose("CREATE " + objectDefinition.id.ToUpper() + " GAME OBJECT " + gameObject.name);

        if (objectConfig.structure) {
            // Ensure this object is not moveable.
            gameObject.isStatic = true;
            // Add AI2-THOR specific properties.
            gameObject.tag = "Structure"; // AI2-THOR Tag
            StructureObject ai2thorStructureScript = gameObject.AddComponent<StructureObject>();
            ai2thorStructureScript.WhatIsMyStructureObjectTag = StructureObjectTag.Wall; // TODO Make configurable
        }

        if (objectConfig.physics) {
            // Add Unity RigidBody and Collider components to enable physics on this object.
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.mass = objectConfig.mass == 0 ? 1 : objectConfig.mass;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            LogVerbose("ASSIGN RIGID BODY TO GAME OBJECT " + gameObject.name);
            Collider collider = this.AssignCollider(gameObject, objectDefinition.collider);
            List<Transform> visibilityPoints = this.AssignVisibilityPoints(gameObject,
                objectDefinition.visibilityPoints);

            // Add AI2-THOR specific properties.
            gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            SimObjPhysics ai2thorPhysicsScript = gameObject.AddComponent<SimObjPhysics>();
            ai2thorPhysicsScript.uniqueID = gameObject.name;
            ai2thorPhysicsScript.Type = SimObjType.MachineCommonSenseObject; // TODO Make configurable
            ai2thorPhysicsScript.PrimaryProperty = SimObjPrimaryProperty.CanPickup; // TODO Make configurable
            ai2thorPhysicsScript.isInteractable = true; // TODO Make configurable
            // TODO We should probably use these properties
            ai2thorPhysicsScript.SecondaryProperties = new List<SimObjSecondaryProperty>().ToArray();
            ai2thorPhysicsScript.VisibilityPoints = visibilityPoints.ToArray();
            ai2thorPhysicsScript.ReceptacleTriggerBoxes = new List<GameObject>().ToArray();
            ai2thorPhysicsScript.MyColliders = collider != null ? new Collider[] { collider } : new Collider[] {};
            ai2thorPhysicsScript.salientMaterials = new ObjectMetadata.ObjectSalientMaterial[] {
                ObjectMetadata.ObjectSalientMaterial.Plastic
            };
            ai2thorPhysicsScript.BoundingBox = this.AssignBoundingBox(gameObject, objectDefinition.boundingBox)
                .gameObject;
            /* TODO We should probably set these properties
            ai2thorPhysicsScript.HFdynamicfriction
            ai2thorPhysicsScript.HFstaticfriction
            ai2thorPhysicsScript.HFbounciness
            ai2thorPhysicsScript.HFrbdrag
            ai2thorPhysicsScript.HFrbangulardrag
            */
            // Call Start to initialize the script since it did not exist on game start.
            ai2thorPhysicsScript.Start();
        }

        this.AssignMaterial(gameObject, objectConfig.materialFile);

        return gameObject;
    }

    private List<Transform> AssignVisibilityPoints(
        GameObject gameObject,
        List<MachineCommonSenseConfigVector> points
    ) {
        int index = 0;
        return points.Select((point) => {
            ++index;
            GameObject visibilityPointsObject = new GameObject();
            visibilityPointsObject.name = gameObject.name + "_visibility_point_" + index;
            visibilityPointsObject.transform.parent = gameObject.transform;
            visibilityPointsObject.transform.localPosition = new Vector3(point.x, point.y, point.z);
            return visibilityPointsObject.transform;
        }).ToList();
    }

    private GameObject CreateCustomGameObject(
        MachineCommonSenseConfigGameObject objectConfig,
        MachineCommonSenseConfigObjectDefinition objectDefinition
    ) {
        GameObject gameObject = Instantiate(Resources.Load("MCS/Objects/" + objectDefinition.resourceFile,
            typeof(GameObject))) as GameObject;

        LogVerbose("LOAD CUSTOM GAME OBJECT Assets/Resources/MCS/Objects/" + objectDefinition.id + " FROM FILE " +
            objectDefinition.resourceFile + (gameObject == null ? " IS NULL" : " IS DONE"));

        gameObject = AssignProperties(gameObject, objectConfig, objectDefinition);

        // Set animations.
        if (objectDefinition.actions.Any((action) => action.animationFile != null &&
            !action.animationFile.Equals(""))) {

            Animation animation = gameObject.GetComponent<Animation>();
            if (animation == null) {
                animation = gameObject.AddComponent<Animation>();
                LogVerbose("ASSIGN NEW ANIMATION TO GAME OBJECT " + gameObject.name);
            }
            objectDefinition.actions.ForEach((action) => {
                if (action.animationFile != null && !action.animationFile.Equals("")) {
                    AnimationClip clip = Resources.Load<AnimationClip>("MCS/Animations/" + action.animationFile);
                    LogVerbose("LOAD OF ANIMATION CLIP FILE Assets/Resources/MCS/Animations/" + action.animationFile +
                        (clip == null ? " IS NULL" : " IS DONE"));
                    animation.AddClip(clip, action.id);
                    LogVerbose("ASSIGN ANIMATION CLIP " + action.animationFile + " TO ACTION " + action.id);
                }
            });
        }

        // Set animation controller.
        if (objectConfig.controller != null && !objectConfig.controller.Equals("")) {
            MachineCommonSenseConfigControllerDefinition controllerDefinition = objectDefinition.controllers
                .Where(cont => cont.id.Equals(objectConfig.controller)).ToList().First();
            if (controllerDefinition.controllerFile != null && !controllerDefinition.controllerFile.Equals("")) {
                Animator animator = gameObject.GetComponent<Animator>();
                if (animator == null) {
                    animator = gameObject.AddComponent<Animator>();
                    LogVerbose("ASSIGN NEW ANIMATOR CONTROLLER TO GAME OBJECT " + gameObject.name);
                }
                RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>(
                    "MCS/Animators/" + controllerDefinition.controllerFile);
                LogVerbose("LOAD OF ANIMATOR CONTROLLER FILE Assets/Resources/MCS/Animators/" +
                    controllerDefinition.controllerFile + (animatorController == null ? " IS NULL" : " IS DONE"));
                animator.runtimeAnimatorController = animatorController;
            }
        }

        return gameObject;
    }

    private GameObject CreateGameObject(MachineCommonSenseConfigGameObject objectConfig) {
        MachineCommonSenseConfigObjectDefinition objectDefinition = this.objectDictionary[objectConfig.type];
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

    private bool UpdateGameObjectOnStep(MachineCommonSenseConfigGameObject objectConfig, int step) {
        bool objectsWereShown = false;

        GameObject gameOrParentObject = objectConfig.GetParentObject() ?? objectConfig.GetGameObject();

        // Do the hides before the shows so any teleports work as expected.
        objectConfig.hides.Where(hide => hide.stepBegin == step).ToList().ForEach((hide) => {
            gameOrParentObject.SetActive(false);
        });

        objectConfig.shows.Where(show => show.stepBegin == step).ToList().ForEach((show) => {
            if (show.position != null) {
                objectConfig.GetGameObject().transform.localPosition = new Vector3(show.position.x, show.position.y,
                    show.position.z);
            }
            if (show.rotation != null) {
                objectConfig.GetGameObject().transform.localRotation = Quaternion.Euler(show.rotation.x,
                    show.rotation.y, show.rotation.z);
            }
            if (show.scale != null) {
                // Set the scale on the game object, not on the parent object.
                objectConfig.GetGameObject().transform.localScale = new Vector3(show.scale.GetX(), show.scale.GetY(),
                    show.scale.GetZ());
            }
            gameOrParentObject.SetActive(true);
            objectsWereShown = true;
        });

        objectConfig.resizes.Where(resize => resize.stepBegin <= step && resize.stepEnd >= step && resize.size != null)
            .ToList().ForEach((resize) => {
                // Set the scale on the game object, not on the parent object.
                objectConfig.GetGameObject().transform.localScale = new Vector3(resize.size.GetX(), resize.size.GetY(),
                    resize.size.GetZ());
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
public class MachineCommonSenseConfigActionDefinition {
    public string id;
    public string animationFile;
}

[Serializable]
public class MachineCommonSenseConfigColliderDefinition {
    public string type;
    public MachineCommonSenseConfigVector center;
    public float height;
    public float radius;
    public MachineCommonSenseConfigSize size;
}

[Serializable]
public class MachineCommonSenseConfigControllerDefinition {
    public string id;
    public string controllerFile;
}

[Serializable]
public class MachineCommonSenseConfigGameObject {
    public string id;
    public string controller;
    public float mass;
    public string materialFile;
    public bool physics;
    public bool structure;
    public string type;
    public List<MachineCommonSenseConfigAction> actions;
    public List<MachineCommonSenseConfigMove> forces;
    public List<MachineCommonSenseConfigStepBegin> hides;
    public List<MachineCommonSenseConfigMove> moves;
    public MachineCommonSenseConfigParentObject nullParent = null;
    public List<MachineCommonSenseConfigResize> resizes;
    public List<MachineCommonSenseConfigMove> rotates;
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
public class MachineCommonSenseConfigMove : MachineCommonSenseConfigStepBeginEnd {
    public MachineCommonSenseConfigVector vector;
}

[Serializable]
public class MachineCommonSenseConfigObjectDefinition {
    public string id;
    public string resourceFile;
    public bool primitive;
    public MachineCommonSenseConfigColliderDefinition boundingBox;
    public MachineCommonSenseConfigColliderDefinition collider;
    public List<MachineCommonSenseConfigActionDefinition> actions;
    public List<MachineCommonSenseConfigControllerDefinition> controllers;
    public List<MachineCommonSenseConfigVector> visibilityPoints;
}

[Serializable]
public class MachineCommonSenseConfigParentObject {
    public MachineCommonSenseConfigVector position;
    public MachineCommonSenseConfigVector rotation;
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
public class MachineCommonSenseConfigVector {
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class MachineCommonSenseConfigScene {
    public String id;
    public String ceilingMaterial;
    public String floorMaterial;
    public String wallMaterial;
    public MachineCommonSenseConfigVector performerStart;
    public List<MachineCommonSenseConfigGameObject> objects;
}

[Serializable]
public class MachineCommonSenseConfigObjectRegistry {
    public List<MachineCommonSenseConfigObjectDefinition> objects;
}
