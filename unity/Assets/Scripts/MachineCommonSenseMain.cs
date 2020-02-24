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
    public string objectRegistryFile = "object_registry";

    private MachineCommonSenseConfigScene currentScene;
    private MachineCommonSenseConfigObjectRegistry objectRegistry;

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

        // Load the configurable prefab objects from our custom registry file.
        this.objectRegistry = LoadObjectRegistryFromFile(this.objectRegistryFile);

        // Load the default MCS scene set in the Unity Editor.
        if (!this.defaultSceneFile.Equals("")) {
            this.currentScene = LoadCurrentSceneFromFile(this.defaultSceneFile);
            this.currentScene.id = ((this.currentScene.id == null || this.currentScene.id.Equals("")) ? this.defaultSceneFile : this.currentScene.id);
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
                this.currentScene.objects.Where(item => item.GetGameObject() != null).ToList().ForEach(item => {
                    bool objectsWereShown = UpdateGameObjectOnStep(item, this.lastStep);
                    // If new objects were added to the scene, notify ImageSynthesis so the objects will appear in the masks.
                    if (objectsWereShown) {
                        ImageSynthesis imageSynthesis = GameObject.Find("FPSController").GetComponentInChildren<ImageSynthesis>();
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
            this.currentScene.objects.ForEach(item => {
                GameObject gameOrParentObject = item.GetParentObject() ?? item.GetGameObject();
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
            controller.transform.position = new Vector3(this.currentScene.performerStart.x, this.currentScene.performerStart.y, this.currentScene.performerStart.z);
        }

        this.lastStep = -1;
        this.physicsSceneManager.SetupScene();
    }

    private GameObject AssignProperties(GameObject gameObject, MachineCommonSenseConfigGameObject item, String type) {
        gameObject.name = item.id;
        gameObject.tag = "SimObj"; // AI2-THOR Tag
        gameObject.layer = 8; // AI2-THOR Layer SimObjVisible

        LogVerbose("CREATE " + type.ToUpper() + " GAME OBJECT " + gameObject.name);

        if (item.structure) {
            gameObject.isStatic = true;
            gameObject.tag = "Structure"; // AI2-THOR Tag
            StructureObject ai2thorStructureScript = gameObject.AddComponent<StructureObject>();
            ai2thorStructureScript.WhatIsMyStructureObjectTag = StructureObjectTag.Wall; // TODO Make configurable
        }

        if (item.physics) {
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            LogVerbose("ASSIGN RIGID BODY TO GAME OBJECT " + gameObject.name);
            // TODO Is SimObjPhysics correct here?
            gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            SimObjPhysics ai2thorPhysicsScript = gameObject.AddComponent<SimObjPhysics>();
            ai2thorPhysicsScript.uniqueID = gameObject.name;
            ai2thorPhysicsScript.Type = SimObjType.MachineCommonSenseObject; // TODO Make configurable
            ai2thorPhysicsScript.PrimaryProperty = SimObjPrimaryProperty.Moveable; // TODO Make configurable
            ai2thorPhysicsScript.isInteractable = true; // TODO Make configurable
            // TODO We should probably use these properties
            ai2thorPhysicsScript.SecondaryProperties = new List<SimObjSecondaryProperty>().ToArray();
            ai2thorPhysicsScript.VisibilityPoints = new List<Transform>().ToArray();
            ai2thorPhysicsScript.ReceptacleTriggerBoxes = new List<GameObject>().ToArray();
            ai2thorPhysicsScript.MyColliders = new List<Collider>().ToArray();
            ai2thorPhysicsScript.salientMaterials = new List<ObjectMetadata.ObjectSalientMaterial>().ToArray();
            /* TODO We should probably set these properties
            ai2thorPhysicsScript.BoundingBox
            ai2thorPhysicsScript.HFdynamicfriction
            ai2thorPhysicsScript.HFstaticfriction
            ai2thorPhysicsScript.HFbounciness
            ai2thorPhysicsScript.HFrbdrag
            ai2thorPhysicsScript.HFrbangulardrag
            */
            // Call Start to initialize the script since it did not exist on game start.
            ai2thorPhysicsScript.Start();
        }

        AssignMaterial(gameObject, item.materialFile);
        
        return gameObject;
    }

    private void AssignCollider(GameObject gameObject, MachineCommonSenseConfigColliderDefinition collider) {
        if (collider.type.Equals("box")) {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(collider.center.x, collider.center.y, collider.center.z);
            boxCollider.size = new Vector3(collider.size.GetX(), collider.size.GetY(), collider.size.GetZ());
            LogVerbose("ASSIGN BOX COLLIDER TO GAME OBJECT " + gameObject.name);
        }
        if (collider.type.Equals("capsule")) {
            CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            capsuleCollider.center = new Vector3(collider.center.x, collider.center.y, collider.center.z);
            capsuleCollider.radius = collider.radius;
            LogVerbose("ASSIGN CAPSULE COLLIDER TO GAME OBJECT " + gameObject.name);
        }
        if (collider.type.Equals("sphere")) {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.center = new Vector3(collider.center.x, collider.center.y, collider.center.z);
            sphereCollider.radius = collider.radius;
            LogVerbose("ASSIGN SPHERE COLLIDER TO GAME OBJECT " + gameObject.name);
        }
    }

    private void AssignMaterial(GameObject gameObject, String materialFile) {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (materialFile != null && !materialFile.Equals("")) {
            Material material = Resources.Load<Material>("MCS/Materials/" + materialFile);
            LogVerbose("LOAD OF MATERIAL FILE Assets/Resources/MCS/Materials/" + materialFile + (material == null ? " IS NULL" : " IS DONE"));
            if (material != null) {
                renderer.material = material;
                LogVerbose("ASSIGN MATERIAL " + materialFile + " TO GAME OBJECT " + gameObject.name);
            }
        }
    }
        
    private GameObject CreateCustomGameObject(MachineCommonSenseConfigGameObject item, MachineCommonSenseConfigObjectDefinition definition) {
        GameObject gameObject = Instantiate(Resources.Load("MCS/Objects/" + definition.resourceFile, typeof(GameObject))) as GameObject;

        LogVerbose("LOAD CUSTOM GAME OBJECT Assets/Resources/MCS/Objects/" + definition.id + " FROM FILE " + definition.resourceFile + (gameObject == null ? " IS NULL" : " IS DONE"));

        gameObject = AssignProperties(gameObject, item, "custom");

        // Set animations.
        if (definition.actions.Any((action) => action.animationFile != null && !action.animationFile.Equals(""))) {
            Animation animation = gameObject.GetComponent<Animation>();
            if (animation == null) {
                animation = gameObject.AddComponent<Animation>();
                LogVerbose("ASSIGN NEW ANIMATION TO GAME OBJECT " + gameObject.name);
            }
            definition.actions.ForEach((action) => {
                if (action.animationFile != null && !action.animationFile.Equals("")) {
                    AnimationClip clip = Resources.Load<AnimationClip>("MCS/Animations/" + action.animationFile);
                    LogVerbose("LOAD OF ANIMATION CLIP FILE Assets/Resources/MCS/Animations/" + action.animationFile + (clip == null ? " IS NULL" : " IS DONE"));
                    animation.AddClip(clip, action.id);
                    LogVerbose("ASSIGN ANIMATION CLIP " + action.animationFile + " TO ACTION " + action.id);
                }
            });
        }
    
        // Set animation controller.
        if (item.controller != null && !item.controller.Equals("")) {
            MachineCommonSenseConfigControllerDefinition controller = definition.controllers.Where(cont => cont.id.Equals(item.controller)).ToList().First();
            if (controller.controllerFile != null && !controller.controllerFile.Equals("")) {
                Animator animator = gameObject.GetComponent<Animator>();
                if (animator == null) {
                    animator = gameObject.AddComponent<Animator>();
                    LogVerbose("ASSIGN NEW ANIMATOR CONTROLLER TO GAME OBJECT " + gameObject.name);
                }
                RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("MCS/Animators/" + controller.controllerFile);
                LogVerbose("LOAD OF ANIMATOR CONTROLLER FILE Assets/Resources/MCS/Animators/" + controller.controllerFile + (animatorController == null ? " IS NULL" : " IS DONE"));
                animator.runtimeAnimatorController = animatorController;
            }
        }

        // Set collider.
        if (item.physics && definition.collider != null) {
            AssignCollider(gameObject, definition.collider);
        }

        return gameObject;
    }

    private GameObject CreateGameObject(MachineCommonSenseConfigGameObject item) {
        switch (item.type) {
            case "capsule":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Capsule), item, "capsule");
            case "cube":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Cube), item, "cube");
            case "cylinder":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Cylinder), item, "cylinder");
            case "plane":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Plane), item, "plane");
            case "quad":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Quad), item, "quad");
            case "sphere":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Sphere), item, "sphere");
        }
        MachineCommonSenseConfigObjectDefinition definition = this.objectRegistry.prefabs.Where(prefab => prefab.id.Equals(item.type)).ToList().First();
        return definition != null ? CreateCustomGameObject(item, definition) : null;
    }
    
    private GameObject CreateNullParentObjectIfNeeded(MachineCommonSenseConfigGameObject item) {
        // Null parents are useful if we want to rotate an object but don't want to pivot the object around its center point.
        if (item.nullParent != null && (item.nullParent.position != null || item.nullParent.rotation != null)) {
            GameObject parentObject = new GameObject();
            item.SetParentObject(parentObject);
            parentObject.name = item.id + "Parent";
            parentObject.transform.localPosition = new Vector3(item.nullParent.position.x,
                item.nullParent.position.y, item.nullParent.position.z);
            parentObject.transform.localRotation = Quaternion.Euler(item.nullParent.rotation.x,
                item.nullParent.rotation.y, item.nullParent.rotation.z);
            item.GetGameObject().transform.parent = item.GetParentObject().transform;
            LogVerbose("CREATE PARENT GAME OBJECT " + parentObject.name);
            return parentObject;
        }
        return null;
    }

    private void InitializeGameObject(MachineCommonSenseConfigGameObject item) {
        try {
            GameObject gameObject = CreateGameObject(item);
            item.SetGameObject(gameObject);
            if (gameObject != null) {
                GameObject parentObject = CreateNullParentObjectIfNeeded(item);
                // Hide the object until the frame defined in MachineCommonSenseConfigGameObject.shows
                (parentObject ?? gameObject).SetActive(false);
            }
        } catch (Exception e) {
            Debug.LogError("MCS:  " + e);
        }
    }

    private MachineCommonSenseConfigScene LoadCurrentSceneFromFile(String filePath) {
        TextAsset currentSceneFile = Resources.Load<TextAsset>("MCS/Scenes/" + filePath);
        Debug.Log("MCS:  Config file Assets/Resources/MCS/Scenes/" + filePath + ".json" + (currentSceneFile == null ? " is null!" : (":\n" + currentSceneFile.text)));
        return JsonUtility.FromJson<MachineCommonSenseConfigScene>(currentSceneFile.text);
    }

    private MachineCommonSenseConfigObjectRegistry LoadObjectRegistryFromFile(String filePath) {
        TextAsset objectRegistryFile = Resources.Load<TextAsset>("MCS/" + filePath);
        Debug.Log("MCS:  Config file Assets/Resources/MCS/" + filePath + ".json" + (objectRegistryFile == null ? " is null!" : (":\n" + objectRegistryFile.text)));
        return JsonUtility.FromJson<MachineCommonSenseConfigObjectRegistry>(objectRegistryFile.text);
    }

    private void LogVerbose(String text) {
        if (this.enableVerboseLog) {
            Debug.Log("MCS:  " + text);
        }
    }

    private bool UpdateGameObjectOnStep(MachineCommonSenseConfigGameObject item, int step) {
        bool objectsWereShown = false;

        GameObject gameOrParentObject = item.GetParentObject() ?? item.GetGameObject();

        // Do the hides before the shows so any teleports work as expected.
        item.hides.Where(hide => hide.stepBegin == step).ToList().ForEach((hide) => {
            gameOrParentObject.SetActive(false);
        });

        item.shows.Where(show => show.stepBegin == step).ToList().ForEach((show) => {
            if (show.position != null) {
                item.GetGameObject().transform.localPosition = new Vector3(show.position.x, show.position.y, show.position.z);
            }
            if (show.rotation != null) {
                item.GetGameObject().transform.localRotation = Quaternion.Euler(show.rotation.x, show.rotation.y, show.rotation.z);
            }
            if (show.scale != null) {
                // Set the scale on the game object, not on the parent object.
                item.GetGameObject().transform.localScale = new Vector3(show.scale.GetX(), show.scale.GetY(), show.scale.GetZ());
            }
            gameOrParentObject.SetActive(true);
            objectsWereShown = true;
        });

        item.resizes.Where(resize => resize.stepBegin <= step && resize.stepEnd >= step && resize.size != null).ToList().ForEach((resize) => {
            // Set the scale on the game object, not on the parent object.
            item.GetGameObject().transform.localScale = new Vector3(resize.size.GetX(), resize.size.GetY(), resize.size.GetZ());
        });

        item.rotates.Where(rotate => rotate.stepBegin <= step && rotate.stepEnd >= step && rotate.vector != null).ToList().ForEach((rotate) => {
            gameOrParentObject.transform.Rotate(new Vector3(rotate.vector.x, rotate.vector.y, rotate.vector.z));
        });

        item.moves.Where(move => move.stepBegin <= step && move.stepEnd >= step && move.vector != null).ToList().ForEach((move) => {
            gameOrParentObject.transform.Translate(new Vector3(move.vector.x, move.vector.y, move.vector.z));
        });

        item.forces.Where(force => force.stepBegin <= step && force.stepEnd >= step && force.vector != null).ToList().ForEach((force) => {
            Rigidbody rigidbody = gameOrParentObject.GetComponent<Rigidbody>();
            if (rigidbody != null) {
                rigidbody.AddForce(new Vector3(force.vector.x, force.vector.y, force.vector.z));
            }
        });

        item.actions.Where(action => action.stepBegin == step).ToList().ForEach((action) => {
            // Play the animation on the game object, not on the parent object.
            Animator animator = item.GetGameObject().GetComponent<Animator>();
            if (animator != null) {
                animator.Play(action.id);
            } else {
                // If the animator does not exist on this game object, then it must use legacy animations.
                item.GetGameObject().GetComponent<Animation>().Play(action.id);
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
    public MachineCommonSenseConfigColliderDefinition collider;
    public List<MachineCommonSenseConfigActionDefinition> actions;
    public List<MachineCommonSenseConfigControllerDefinition> controllers;
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
    public List<MachineCommonSenseConfigObjectDefinition> prefabs;
}
