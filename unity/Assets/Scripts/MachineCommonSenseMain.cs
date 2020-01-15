using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System.Text;

public class MachineCommonSenseMain : MonoBehaviour {
    public static bool enableVerboseLog = false;
    public static int physicsFrameDuration = 10;

    private FileConfigGame gameConfig;
    private FileConfigObjectList objectConfig;

    private int lastPhysicsFrame = -1;
    private int lastStep = -1;

    private MachineCommonSensePerformerManager performerManager;

    // Unity's Start method is called before the first frame update
    void Start() {
        TextAsset mainConfigFile = Resources.Load<TextAsset>("MCS/config");
        Debug.Log(mainConfigFile == null ? "Config file config.json is null!" : mainConfigFile.text);
        FileConfigMain mainConfig = JsonUtility.FromJson<FileConfigMain>(mainConfigFile.text);

        gameConfig = LoadGameConfig(mainConfig);
        objectConfig = LoadObjectConfig(mainConfig);

        gameConfig.objects.ForEach(InitializeGameObject);

        AssignMaterial(GameObject.Find("Ceiling"), gameConfig.ceilingMaterial);
        AssignMaterial(GameObject.Find("Floor"), gameConfig.floorMaterial);
        AssignMaterial(GameObject.Find("Wall Back"), gameConfig.wallMaterial);
        AssignMaterial(GameObject.Find("Wall Front"), gameConfig.wallMaterial);
        AssignMaterial(GameObject.Find("Wall Left"), gameConfig.wallMaterial);
        AssignMaterial(GameObject.Find("Wall Right"), gameConfig.wallMaterial);

        if (gameConfig.performerStart != null) {
            GameObject controller = GameObject.Find("FPSController");
            controller.transform.position = new Vector3(gameConfig.performerStart.x, gameConfig.performerStart.y, gameConfig.performerStart.z);
        }

        this.performerManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<MachineCommonSensePerformerManager>();
    }

    // Unity's Update method is called once per frame
    void Update() {
        if (Time.frameCount == 1) {
            // Pause physics
            Time.timeScale = 0;
        }
        if (this.lastStep < MachineCommonSensePerformerManager.step) {
            this.lastStep++;
            this.lastPhysicsFrame = Time.frameCount;
            LogVerbose("Run Step " + this.lastStep + " and Unpause Game Physics at Frame " + Time.frameCount);
            Time.timeScale = 1;
            gameConfig.objects.Where(item => item.GetGameObject() != null).ToList().ForEach(item => UpdateGameObjectForFrame(item, this.lastStep));
        }
        if (Time.timeScale == 1 && Time.frameCount == (this.lastPhysicsFrame + MachineCommonSenseMain.physicsFrameDuration)) {
            LogVerbose("Pause Game Physics at Frame " + Time.frameCount);
            Time.timeScale = 0;
            this.performerManager.FinalizeEmit();
        }
    }
    
    // Custom Methods

    private GameObject AssignProperties(GameObject gameObject, ConfigGameObject item) {
        if (item.physics) {
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            gameObject.tag = "SimObjPhysics"; // AI2-THOR Tag
            LogVerbose("ASSIGN RIGID BODY TO GAME OBJECT " + gameObject.name);
        }

        AssignMaterial(gameObject, item.materialFile);
        
        return gameObject;
    }

    private void AssignCollider(GameObject gameObject, ConfigColliderDefinition collider) {
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
            Material material = Resources.Load<Material>("MCS/" + materialFile);
            LogVerbose("LOAD OF MATERIAL FILE " + materialFile + (material == null ? " IS NULL" : " IS DONE"));
            if (material != null) {
                renderer.material = material;
                LogVerbose("ASSIGN MATERIAL TO GAME OBJECT " + gameObject.name);
            }
        }
    }
        
    private GameObject CreateCustomGameObject(ConfigGameObject item, ConfigObjectDefinition definition) {
        GameObject gameObject = Instantiate(Resources.Load("MCS/" + definition.resourceFile, typeof(GameObject))) as GameObject;
        LogVerbose("CREATE CUSTOM GAME OBJECT " + gameObject.name + " FROM FILE " + definition.resourceFile);

        // Set animations.
        if (definition.actions.Any((action) => action.animationFile != null && !action.animationFile.Equals(""))) {
            Animation animation = gameObject.GetComponent<Animation>();
            if (animation == null) {
                animation = gameObject.AddComponent<Animation>();
                LogVerbose("ASSIGN ANIMATION TO GAME OBJECT " + gameObject.name);
            }
            definition.actions.ForEach((action) => {
                if (action.animationFile != null && !action.animationFile.Equals("")) {
                    AnimationClip clip = Resources.Load<AnimationClip>("MCS/" + action.animationFile);
                    LogVerbose("LOAD OF ANIMATION CLIP FILE " + action.animationFile + (clip == null ? " IS NULL" : " IS DONE"));
                    animation.AddClip(clip, action.id);
                    LogVerbose("ASSIGN ANIMATION CLIP TO ACTION " + action.id);
                }
            });
        }
    
        // Set animation controller.
        if (item.controller != null && !item.controller.Equals("")) {
            ConfigControllerDefinition controller = definition.controllers.Where(cont => cont.id.Equals(item.controller)).ToList().First();
            if (controller.controllerFile != null && !controller.controllerFile.Equals("")) {
                Animator animator = gameObject.GetComponent<Animator>();
                if (animator == null) {
                    animator = gameObject.AddComponent<Animator>();
                    LogVerbose("ASSIGN ANIMATOR CONTROLLER TO GAME OBJECT " + gameObject.name);
                }
                RuntimeAnimatorController animatorController = Resources.Load<RuntimeAnimatorController>("MCS/" + controller.controllerFile);
                LogVerbose("LOAD OF ANIMATOR CONTROLLER FILE " + controller.controllerFile + (animatorController == null ? " IS NULL" : " IS DONE"));
                animator.runtimeAnimatorController = animatorController;
            }
        }

        // Set collider.
        if (item.physics && definition.collider != null) {
            AssignCollider(gameObject, definition.collider);
        }

        return AssignProperties(gameObject, item);
    }

    private GameObject CreateGameObject(ConfigGameObject item) {
        switch (item.type) {
            case "capsule":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Capsule), item);
            case "cube":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Cube), item);
            case "cylinder":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Cylinder), item);
            case "plane":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Plane), item);
            case "quad":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Quad), item);
            case "sphere":
                return AssignProperties(GameObject.CreatePrimitive(PrimitiveType.Sphere), item);
        }
        ConfigObjectDefinition definition = objectConfig.prefabs.Where(prefab => prefab.id.Equals(item.type)).ToList().First();
        return definition != null ? CreateCustomGameObject(item, definition) : null;
    }
    
    private GameObject CreateNullParentObjectIfNeeded(ConfigGameObject item) {
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

    private void InitializeGameObject(ConfigGameObject item) {
        try {
            GameObject gameObject = CreateGameObject(item);
            item.SetGameObject(gameObject);
            if (gameObject != null) {
                gameObject.name = item.id;
                GameObject parentObject = CreateNullParentObjectIfNeeded(item);
                // Hide the object until the frame defined in ConfigGameObject.shows
                (parentObject ?? gameObject).SetActive(false);
            }
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }

    private FileConfigGame LoadGameConfig(FileConfigMain mainConfig) {
        TextAsset gameConfigFile = Resources.Load<TextAsset>("MCS/" + mainConfig.gameFile);
        Debug.Log(gameConfigFile == null ? "Config file " + mainConfig.gameFile + ".json is null!" : gameConfigFile.text);
        return JsonUtility.FromJson<FileConfigGame>(gameConfigFile.text);
    }

    private FileConfigObjectList LoadObjectConfig(FileConfigMain mainConfig) {
        TextAsset objectConfigFile = Resources.Load<TextAsset>("MCS/" + mainConfig.objectFile);
        Debug.Log(objectConfigFile == null ? "Config file " + mainConfig.objectFile + ".json is null!" : objectConfigFile.text);
        return JsonUtility.FromJson<FileConfigObjectList>(objectConfigFile.text);
    }

    private void LogVerbose(String text) {
        if (MachineCommonSenseMain.enableVerboseLog) {
            Debug.Log(text);
        }
    }

    private void UpdateGameObjectForFrame(ConfigGameObject item, int step) {
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
    }
}

// Definitions of serializable objects from JSON config files.

[Serializable]
class ConfigAction : ConfigStepBegin {
    public string id;
}

[Serializable]
class ConfigActionDefinition {
    public string id;
    public string animationFile;
}

[Serializable]
class ConfigColliderDefinition {
    public string type;
    public ConfigVector center;
    public float radius;
    public ConfigSize size;
}

[Serializable]
class ConfigControllerDefinition {
    public string id;
    public string controllerFile;
}

[Serializable]
class ConfigGameObject {
    public string id;
    public string controller;
    public string materialFile;
    public bool physics;
    public string type;
    public List<ConfigAction> actions;
    public List<ConfigMove> forces;
    public List<ConfigStepBegin> hides;
    public List<ConfigMove> moves;
    public ConfigParentObject nullParent;
    public List<ConfigResize> resizes;
    public List<ConfigMove> rotates;
    public List<ConfigShow> shows;

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
class ConfigMove : ConfigStepBeginEnd {
    public ConfigVector vector;
}

[Serializable]
class ConfigObjectDefinition {
    public string id;
    public string resourceFile;
    public ConfigColliderDefinition collider;
    public List<ConfigActionDefinition> actions;
    public List<ConfigControllerDefinition> controllers;
}

[Serializable]
class ConfigParentObject {
    public ConfigVector position;
    public ConfigVector rotation;
}

[Serializable]
class ConfigResize : ConfigStepBeginEnd {
    public ConfigSize size;
}

[Serializable]
class ConfigShow : ConfigStepBegin {
    public ConfigVector position;
    public ConfigVector rotation;
    public ConfigSize scale;
}

[Serializable]
class ConfigSize {
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
class ConfigStepBegin {
    public int stepBegin;
}

[Serializable]
class ConfigStepBeginEnd : ConfigStepBegin {
    public int stepEnd;
}

[Serializable]
class ConfigVector {
    public float x;
    public float y;
    public float z;
}

[Serializable]
class FileConfigGame {
    public String ceilingMaterial;
    public String floorMaterial;
    public String wallMaterial;
    public ConfigVector performerStart;
    public List<ConfigGameObject> objects;
}

[Serializable]
class FileConfigMain {
    public string gameFile;
    public string objectFile;
}

[Serializable]
class FileConfigObjectList {
    public List<ConfigObjectDefinition> prefabs;
}
