using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum AgentType : int {
	ToonPeopleFemale = 0,
	ToonPeopleMale = 1
}

public class MCSSimulationAgent : MonoBehaviour {
    private static int ELDER_BLEND_SHAPE = 26;

    public AgentType type;
    public ObjectMaterialOption beard = null;
    public HeadObjectMaterialOption head;
    public ObjectMaterialOption glasses;

    public Material[] skinOptions;
    public Material[] elderSkinOptions;

    public ChestObjectMaterialOption[] chestOptions;
    public SkinObjectMaterialOption[] feetOptions;
    public HairObjectMaterialOption[] hairOptions;
    public ObjectMaterialOption[] jacketOptions;
    public SkinObjectMaterialOption[] legsOptions;
    public ObjectMaterialOption[] tieOptions;

    private ChestObjectMaterialOption chest = null;
    private bool elder = false;
    private SkinObjectMaterialOption feet = null;
    private HairObjectMaterialOption hair = null;
    private ObjectMaterialOption jacket = null;
    private SkinObjectMaterialOption legs = null;
    private int skin = 0;
    private ObjectMaterialOption tie = null;
    private MCSMain mcsMain;


    private Animator animator;
    public static int ANIMATION_FRAME_RATE = 25;
    public static int AGENT_INTERACTION_ACTION_STARTING_ANIMATION_FRAME = 3;
    public int currentAnimationFrame = 0;
    [SerializeField] private string currentClip;
    public Dictionary<string, float> clipNamesAndDurations = new Dictionary<string,float>();
    private bool resetAnimationToIdleAfterPlayingOnce = false;
    private int stepToEndAnimation = -1;
    
    
    //local position adjustments of the held object throughout the animation sequence, calculated by hand in the editor
    private static Vector3 REACH_INTO_BACK_POSITION_1 = new Vector3(-0.116f, 0.219f, 0.211f);
    private static Vector3 REACH_INTO_BACK_POSITION_2 = new Vector3(-0.18f, 0.044f, -0.01f);
    private static Vector3 REACH_INTO_BACK_POSITION_3 = new Vector3(-0.104f, 0.074f, -0.074f);
    private static Vector3 HOLDING_POSITION = new Vector3(-0.102f, 0.114f, 0);
    private static Vector3[] AGENT_INTERACTION_ACTION_OBJECT_POSITIONS = {Vector3.zero, REACH_INTO_BACK_POSITION_1, REACH_INTO_BACK_POSITION_2, REACH_INTO_BACK_POSITION_3};
    private static string[] AGENT_INTERACTION_ACTION_ANIMATIONS = {"TPF_phone1", "TPM_phone1", "TPM_phone1", "TPM_phone2"};
    public static string NOT_HOLDING_OBJECT_ANIMATION = "TPM_idle5";
    public static int NOT_HOLDING_OBJECT_ANIMATION_LENGTH = 5;
    private static string HAND_NAME = "TP R Hand";
    private Transform hand;
    public SimObjPhysics heldObject;
    public bool isHoldingHeldObject;
    public bool gettingHeldObject;
    public bool holdingOutHeldObjectForPickup;
    private int currentGetHeldObjectAnimation = 0;

    void Awake() {
        // Activate a default chest, legs, and feet option so we won't have a disembodied floating head.
        this.SetChest(0, 0);
        this.SetFeet(0, 0);
        this.SetLegs(0, 0);
        this.SetEyes(0);
        this.SetSkin(0);
        this.SetElder(false);
        // Deactivate all the optional body parts and accessories by default.
        this.glasses.gameObject.SetActive(false);
        this.DeactivateGameObjects(this.hairOptions);
        this.DeactivateGameObjects(this.jacketOptions);
        this.DeactivateGameObjects(this.tieOptions);
        if (this.beard != null && this.beard.gameObject != null) {
            this.beard.gameObject.SetActive(false);
        }
        this.animator = this.gameObject.GetComponent<Animator>();
        animator.speed = 0;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips) {
            clipNamesAndDurations.Add(clip.name, clip.length);
        }

        mcsMain = FindObjectOfType<MCSMain>();
        mcsMain.GetSimulationAgents().Add(this);
        SetDefaultAnimation();
        IncrementAnimationFrame();
        
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach(Transform child in children) {
            if(child.name == HAND_NAME) {
               hand = child;
               break;
            }
        }
        currentGetHeldObjectAnimation = 0;
        isHoldingHeldObject = false;
        holdingOutHeldObjectForPickup = false;
        gettingHeldObject = false;
    }
    
    void Start() {
        if(heldObject != null) {
            isHoldingHeldObject = true;
            foreach(Collider c in heldObject.MyColliders)
                c.enabled = false;
            heldObject.transform.parent = hand;
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.GetComponent<Rigidbody>().isKinematic = true;
            heldObject.gameObject.SetActive(false);
        }
    }

    public void SetDefaultAnimation(string name = null) {
        if (this.type == AgentType.ToonPeopleFemale) {
            this.currentClip = name != null ? name : "TPF_idle1";
        }
        if (this.type == AgentType.ToonPeopleMale) {
            this.currentClip = name != null ? name : "TPM_idle1";
        }
        resetAnimationToIdleAfterPlayingOnce = false;
        currentAnimationFrame = 0;

    }

    public void PlayGetObjectOutOfBackpackAnimation() {
        gettingHeldObject = true;
        heldObject.gameObject.SetActive(true);
        heldObject.transform.localPosition = AGENT_INTERACTION_ACTION_OBJECT_POSITIONS[currentGetHeldObjectAnimation];
        AssignClip(AGENT_INTERACTION_ACTION_ANIMATIONS[currentGetHeldObjectAnimation]);
        if(currentGetHeldObjectAnimation == 2) {
            int animationFrameToEnhanceInteractionAction = 34;
            currentAnimationFrame = animationFrameToEnhanceInteractionAction;
        }
    }

    public void HoldHeldObjectOutForPickup() {
        gettingHeldObject = false;
        holdingOutHeldObjectForPickup = true;
        heldObject.transform.localPosition = HOLDING_POSITION;
        foreach(Collider c in heldObject.MyColliders)
            c.enabled = true;
        AssignClip(AGENT_INTERACTION_ACTION_ANIMATIONS[AGENT_INTERACTION_ACTION_ANIMATIONS.Length-1]);
        AnimationPlaysOnce(isLoopAnimation: true);
    }


    public void AssignClip(string clipId) {
        currentAnimationFrame = 0;
        currentClip = clipId;
    }

    public void IncrementAnimationFrame() {
        currentAnimationFrame++;
        int totalFrames = Mathf.FloorToInt(MCSSimulationAgent.ANIMATION_FRAME_RATE * clipNamesAndDurations[this.currentClip]);
        if (resetAnimationToIdleAfterPlayingOnce && currentAnimationFrame > totalFrames)
            SetDefaultAnimation();
        if(mcsMain.GetStepNumber() == stepToEndAnimation) {
            SetDefaultAnimation();
            stepToEndAnimation = -1;
        }
        currentAnimationFrame = currentAnimationFrame > totalFrames ? 0 : currentAnimationFrame;
        float percentOfAnimation = currentAnimationFrame / (float)(totalFrames);
        animator.Play(currentClip, 0, percentOfAnimation);

        if(gettingHeldObject) {
            currentGetHeldObjectAnimation++;
            if(currentGetHeldObjectAnimation >= AGENT_INTERACTION_ACTION_ANIMATIONS.Length - 1) {
                HoldHeldObjectOutForPickup();
            }
            else {
                PlayGetObjectOutOfBackpackAnimation();
            }
        }
    }

    public void AnimationPlaysOnce(bool isLoopAnimation) {
        resetAnimationToIdleAfterPlayingOnce = !isLoopAnimation;
    }

    public void SetStepToEndAnimation(int step) {
        this.stepToEndAnimation = step;
    }

    public void SetBeard(int? beardIndex = -1) {
        if (this.beard == null || this.beard.gameObject == null || this.beard.materials == null) {
            return;
        }
        // Choose a random beard material now to ensure each part of the beard is the same.
        if (beardIndex == null || beardIndex >= this.beard.materials.Length || beardIndex < 0) {
            beardIndex = ChooseDefaultIndex(this.beard.materials.Length);
        }
        // The beard has four separate material elements to set in its renderer's materials array.
        for (int i = 0; i <= 3; ++i) {
            this.SetMaterialFromList(this.beard, this.beard.materials, (int)beardIndex, i);
        }
        this.beard.gameObject.SetActive(true);
    }

    public void SetChest(int? chestIndex = -1, int? chestMaterialIndex = -1) {
        if (chestIndex == null || chestIndex >= this.chestOptions.Length || chestIndex < 0) {
            chestIndex = ChooseDefaultIndex(this.chestOptions.Length);
        }
        this.DeactivateGameObjects(this.chestOptions);
        this.chest = this.chestOptions[(int)chestIndex];
        this.chest.gameObject.SetActive(true);
        this.SetMaterial(this.chest, chestMaterialIndex, this.chest.skinRendererMaterialIndex == 0 ? 1 : 0);
        // Update other game objects based on the active chest model.
        if (this.legs != null && this.legs.gameObject != null) {
            this.legs.gameObject.SetActive(!this.chest.deactivateLegs);
        }
        if (this.tie != null && this.tie.gameObject != null) {
            this.tie.gameObject.SetActive(this.chest.enableTies);
        }
    }

    public void SetElder(bool elder) {
        this.elder = elder;
        SkinnedMeshRenderer skinRenderer = this.head.gameObject.GetComponent<SkinnedMeshRenderer>();
        skinRenderer.SetBlendShapeWeight(MCSSimulationAgent.ELDER_BLEND_SHAPE, elder ? 100 : 0);
        // Ensure elders have elder skin, and visa-versa.
        this.SetSkin(this.skin);
    }

    public void SetEyes(int? eyesIndex = -1) {
        // The eyes are a material on the head game object.
        this.SetMaterial(this.head, eyesIndex, this.head.eyesRendererMaterialIndex);
    }

    public void SetFeet(int? feetIndex = -1, int? feetMaterialIndex = -1) {
        if (feetIndex == null || feetIndex >= this.feetOptions.Length || feetIndex < 0) {
            feetIndex = ChooseDefaultIndex(this.feetOptions.Length);
        }
        this.DeactivateGameObjects(this.feetOptions);
        this.feet = this.feetOptions[(int)feetIndex];
        this.feet.gameObject.SetActive(true);
        this.SetMaterial(this.feet, feetMaterialIndex, this.feet.skinRendererMaterialIndex == 0 ? 1 : 0);
    }

    public void SetGlasses(int? glassesIndex = -1) {
        this.SetMaterial(this.glasses, glassesIndex);
        this.glasses.gameObject.SetActive(true);
    }

    public void SetHair(int? hairIndex = -1, int? hairMaterialIndex = -1, int? hatMaterialIndex = -1) {
        if (hairIndex == null || hairIndex >= this.hairOptions.Length || hairIndex < 0) {
            hairIndex = ChooseDefaultIndex(this.hairOptions.Length);
        }
        this.DeactivateGameObjects(this.hairOptions);
        this.hair = this.hairOptions[(int)hairIndex];
        this.hair.gameObject.SetActive(true);
        this.SetMaterial(this.hair, hairMaterialIndex, this.hair.hatRendererMaterialIndex == 0 ? 1 : 0);
        // If the current hair model has a hat, set the hat's material.
        if (this.hair.hatRendererMaterialIndex >= 0 && this.hair.hatMaterials != null) {
            if (hatMaterialIndex == null || hatMaterialIndex >= this.hair.hatMaterials.Length || hatMaterialIndex < 0) {
                hatMaterialIndex = ChooseDefaultIndex(this.hair.hatMaterials.Length);
            }
            this.SetMaterialFromList(this.hair, this.hair.hatMaterials, (int)hatMaterialIndex,
                    this.hair.hatRendererMaterialIndex);
        }
    }

    public void SetJacket(int? jacketIndex = -1, int? jacketMaterialIndex = -1) {
        // TODO Fix issue with jackets clipping into other clothes.
        /*
        if (jacketIndex == null || jacketIndex >= this.jacketOptions.Length || jacketIndex < 0) {
            jacketIndex = ChooseDefaultIndex(this.jacketOptions.Length);
        }
        this.DeactivateGameObjects(this.jacketOptions);
        this.jacket = this.jacketOptions[(int)jacketIndex];
        this.jacket.gameObject.SetActive(true);
        this.SetMaterial(this.jacket, jacketMaterialIndex);
        */
    }

    public void SetLegs(int? legsIndex = -1, int? legsMaterialIndex = -1) {
        if (legsIndex == null || legsIndex >= this.legsOptions.Length || legsIndex < 0) {
            legsIndex = ChooseDefaultIndex(this.legsOptions.Length);
        }
        this.DeactivateGameObjects(this.legsOptions);
        this.legs = this.legsOptions[(int)legsIndex];
        this.legs.gameObject.SetActive(true);
        this.SetMaterial(this.legs, legsMaterialIndex, this.legs.skinRendererMaterialIndex == 0 ? 1 : 0);
        // If the current chest model already has legs, then deactivate the separate legs models.
        if (this.chest != null && this.chest.deactivateLegs) {
            this.legs.gameObject.SetActive(false);
        }
    }

    public void SetSkin(int? skinIndex = -1) {
        if (skinIndex == null || skinIndex >= this.skinOptions.Length || skinIndex < 0) {
            skinIndex = ChooseDefaultIndex(this.skinOptions.Length);
        }
        // There are 4 elder skins corresponding to the 4 male or 12 female skins.
        this.skin = this.elder ? ((int)skinIndex % 4) : (int)skinIndex;
        Material[] skinOptions = this.elder ? this.elderSkinOptions : this.skinOptions;
        this.SetSkinMaterial(new SkinObjectMaterialOption[]{this.head}, skinOptions, this.skin);
        this.SetSkinMaterial(this.chestOptions, skinOptions, this.skin);
        this.SetSkinMaterial(this.feetOptions, skinOptions, this.skin);
        this.SetSkinMaterial(this.legsOptions, skinOptions, this.skin);
    }

    public void SetTie(int? tieIndex = -1, int? tieMaterialIndex = -1) {
        if (tieIndex == null || tieIndex >= this.tieOptions.Length || tieIndex < 0) {
            tieIndex = ChooseDefaultIndex(this.tieOptions.Length);
        }
        this.DeactivateGameObjects(this.tieOptions);
        this.tie = this.tieOptions[(int)tieIndex];
        this.tie.gameObject.SetActive(true);
        this.SetMaterial(this.tie, tieMaterialIndex);
        // If the current chest model is not compatible with ties, then deactivate the tie.
        if (this.chest != null && !this.chest.enableTies) {
            this.tie.gameObject.SetActive(false);
        }
    }

    private int ChooseDefaultIndex(int listSize) {
        // Here in case we want to change this behavior in the future.
        return 0;
    }

    private void DeactivateGameObjects(ObjectMaterialOption[] options) {
        foreach (ObjectMaterialOption option in options) {
            option.gameObject.SetActive(false);
        }
    }

    private void SetMaterial(ObjectMaterialOption option, int? materialOptionIndex, int rendererMaterialIndex = 0) {
        if (materialOptionIndex == null || materialOptionIndex >= option.materials.Length || materialOptionIndex < 0) {
            materialOptionIndex = ChooseDefaultIndex(option.materials.Length);
        }
        this.SetMaterialFromList(option, option.materials, (int)materialOptionIndex, rendererMaterialIndex);
    }

    private void SetMaterialFromList(
        ObjectMaterialOption option,
        Material[] materialOptions,
        int materialOptionIndex,
        int rendererMaterialIndex = 0
    ) {
        // If we haven't cached the game object's renderer yet, do it now.
        if (option.renderer == null) {
            option.renderer = option.gameObject.GetComponent<Renderer>();
        }
        // Copy the renderer's existing materials array.
        Material[] materials = new Material[option.renderer.materials.Length];
        option.renderer.materials.CopyTo(materials, 0);
        // Change the specific material in the copied array.
        materials[rendererMaterialIndex] = materialOptions[materialOptionIndex];
        // Must completely reassign the renderer's materials array here (see the Unity docs).
        option.renderer.materials = materials;
    }

    private void SetSkinMaterial(ObjectMaterialOption[] options, Material[] skinOptions, int skinMaterialIndex) {
        foreach (SkinObjectMaterialOption option in options) {
            // If this body part shows skin...
            if (option.skinRendererMaterialIndex >= 0) {
                // Set the skin material for the body part using the chosen index.
                this.SetMaterialFromList(option, skinOptions, skinMaterialIndex, option.skinRendererMaterialIndex);
            }
        }
    }
}

[System.Serializable]
public class ObjectMaterialOption {
    public GameObject gameObject;
    public Material[] materials;
    [HideInInspector]
    public Renderer renderer;
}

[System.Serializable]
public class HairObjectMaterialOption : ObjectMaterialOption {
    // Some hair options may show hats as an additional material.
    public Material[] hatMaterials;
    public int hatRendererMaterialIndex = -1;
}

[System.Serializable]
public class SkinObjectMaterialOption : ObjectMaterialOption {
    // Some clothing options may show skin as an additional material.
    public int skinRendererMaterialIndex = -1;
}

[System.Serializable]
public class ChestObjectMaterialOption : SkinObjectMaterialOption {
    // Some chest options may deactivate legs.
    public bool deactivateLegs = false;
    // Some chest options may have ties (configured separately).
    public bool enableTies = false;
}

[System.Serializable]
public class HeadObjectMaterialOption : SkinObjectMaterialOption {
    public int eyesRendererMaterialIndex = 1;
}
