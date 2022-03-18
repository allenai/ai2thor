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


    private Animator animator;
    private static int ANIMATION_FRAME_RATE = 25;
    public static int AGENT_INTERACTION_ACTION_STARTING_ANIMATION_FRAME = 3;
    public int currentAnimationFrame = 0;
    [SerializeField] private string currentClip;
    private Dictionary<string, float> clipNamesAndDurations = new Dictionary<string,float>();
    private bool resetAnimationToIdleAfterPlayingOnce = false;
    private int stepToEndAnimation = -1;
    public static string[] agentInteractionActionAnimations = {"TPF_land", "TPM_land"};
    private MCSMain mcsMain;

    
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
        if (jacketIndex == null || jacketIndex >= this.jacketOptions.Length || jacketIndex < 0) {
            jacketIndex = ChooseDefaultIndex(this.jacketOptions.Length);
        }
        this.DeactivateGameObjects(this.jacketOptions);
        this.jacket = this.jacketOptions[(int)jacketIndex];
        this.jacket.gameObject.SetActive(true);
        this.SetMaterial(this.jacket, jacketMaterialIndex);
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


/*
---ALL AVAILABLE ANIMATIONS---
TPM_brake
TPM_clap
TPM_cry
TPM_fallbackwardsFLY
TPM_fallbackwardsIN
TPM_fallbackwardsOUT
TPM_fallforwardFLY
TPM_fallforwardIN
TPM_fallforwardOUT
TPM_freefall
TPM_hitbackwards
TPM_hitforward
TPM_idle1
TPM_idle2
TPM_idle3
TPM_idle4
TPM_idle5
TPM_idleafraid
TPM_idleangry
TPM_idlehappy
TPM_idlesad
TPM_jump
TPM_land
TPM_laugh
TPM_lookback
TPM_phone1
TPM_phone2
TPM_run
TPM_runbackwards
TPM_runjumpFLY
TPM_runjumpIN
TPM_runjumpOUT
TPM_runIN
TPM_runL
TPM_runOUT
TPM_runR
TPM_runstrafeL
TPM_runstrafeR
TPM_scream
TPM_sitphone1
TPM_sitphone2
TPM_sitdownIN
TPM_sitdownOUT
TPM_sitidle1
TPM_sitidle2
TPM_stairsDOWN
TPM_stairsUP
TPM_static
TPM_talk1
TPM_talk2
TPM_telloff
TPM_turnL45
TPM_turnL90
TPM_turnR45
TPM_turnR90
TPM_walk
TPM_walkbackwards
TPM_strafeL
TPM_strafeR
TPM_wave
TPF_brake
TPF_clap
TPF_cry
TPF_fallbackwardsFLY
TPF_fallbackwardsIN
TPF_fallbackwardsOUT
TPF_fallforwardFLY
TPF_fallforwardIN
TPF_fallforwardOUT
TPF_freefall
TPF_hitbackwards
TPF_hitforward
TPF_idle1
TPF_idle2
TPF_idle3
TPF_idle4
TPF_idle5
TPF_idleafraid
TPF_idleangry
TPF_idlehappy
TPF_idlesad
TPF_jump
TPF_land
TPF_laugh
TPF_lookback
TPF_phone1
TPF_phone2
TPF_run
TPF_runbackwards
TPF_runjumpFLY
TPF_runjumpIN
TPF_runjumpOUT
TPF_runIN
TPF_runL
TPF_runOUT
TPF_runR
TPF_runstrafeL
TPF_runstrafeR
TPF_scream
TPF_sitphone1
TPF_sitphone2
TPF_sitdownIN
TPF_sitdownOUT
TPF_sitidle1
TPF_sitidle2
TPF_stairsDOWN
TPF_stairsUP
TPF_static
TPF_talk1
TPF_talk2
TPF_telloff
TPF_turnL45
TPF_turnL90
TPF_turnR45
TPF_turnR90
TPF_walk
TPF_walkbackwards
TPF_strafeL
TPF_strafeR
TPF_wave
TPE_clap
TPE_cry
TPE_freefall
TPE_hitbackwards
TPE_hitforward
TPE_idle1
TPE_idle2
TPE_idle3
TPE_idle4
TPE_idle5
TPE_idleafraid
TPE_idleangry
TPE_idlehappy
TPE_idlesad
TPE_jump
TPE_land
TPE_laugh
TPE_lookback
TPE_phone1
TPE_phone2
TPE_run
TPE_runbackwards
TPE_runIN
TPE_runjumpFLY
TPE_runjumpIN
TPE_runjumpOUT
TPE_runL
TPE_runOUT
TPE_runR
TPE_scream
TPE_sitphone1
TPE_sitphone2
TPE_sitdownIN
TPE_sitdownOUT
TPE_sitidle1
TPE_sitidle2
TPE_stairsDOWN
TPE_stairsUP
TPE_talk1
TPE_talk2
TPE_telloff
TPE_turnL45
TPE_turnR45
TPE_turnL90
TPE_turnR90
TPE_walk
TPE_walkbackwards
TPE_strafeL
TPE_strafeR
TPE_wave
happy
sad
angry
amazed
disgust
*/