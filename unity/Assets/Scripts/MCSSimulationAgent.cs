using System.Collections;
using UnityEngine;

public class MCSSimulationAgent : MonoBehaviour {
    public ObjectMaterialOption beard = null;
    public HeadObjectMaterialOption head;
    public ObjectMaterialOption glasses;

    public Material[] skinOptions;

    public ChestObjectMaterialOption[] chestOptions;
    public SkinObjectMaterialOption[] feetOptions;
    public HairObjectMaterialOption[] hairOptions;
    public ObjectMaterialOption[] jacketOptions;
    public SkinObjectMaterialOption[] legsOptions;
    public ObjectMaterialOption[] tieOptions;

    [HideInInspector]
    public ChestObjectMaterialOption chest = null;
    [HideInInspector]
    public SkinObjectMaterialOption feet = null;
    [HideInInspector]
    public HairObjectMaterialOption hair = null;
    [HideInInspector]
    public ObjectMaterialOption jacket = null;
    [HideInInspector]
    public SkinObjectMaterialOption legs = null;
    [HideInInspector]
    public ObjectMaterialOption tie = null;

    void Awake() {
        // Activate a default chest, legs, and feet option so we won't have a disembodied floating head.
        this.SetChest(0, 0);
        this.SetFeet(0, 0);
        this.SetLegs(0, 0);
        // Deactivate all the optional body parts and accessories by default.
        this.glasses.gameObject.SetActive(false);
        this.DeactivateGameObjects(this.hairOptions);
        this.DeactivateGameObjects(this.jacketOptions);
        this.DeactivateGameObjects(this.tieOptions);
        if (this.beard != null && this.beard.gameObject != null) {
            this.beard.gameObject.SetActive(false);
        }
    }

    public void SetBeard(int? beardIndex = -1) {
        if (this.beard == null || this.beard.gameObject == null || this.beard.materials == null) {
            return;
        }
        // Choose a random beard material now to ensure each part of the beard is the same.
        if (beardIndex == null || beardIndex >= this.beard.materials.Length || beardIndex < 0) {
            beardIndex = Random.Range(0, this.beard.materials.Length);
        }
        // The beard has four separate material elements to set in its renderer's materials array.
        for (int i = 0; i <= 3; ++i) {
            this.SetMaterialFromList(this.beard, this.beard.materials, (int)beardIndex, i);
        }
        this.beard.gameObject.SetActive(true);
    }

    public void SetChest(int? chestIndex = -1, int? chestMaterialIndex = -1) {
        if (chestIndex == null || chestIndex >= this.chestOptions.Length || chestIndex < 0) {
            chestIndex = Random.Range(0, this.chestOptions.Length);
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

    public void SetEyes(int? eyesIndex = -1) {
        // The eyes are a material on the head game object.
        this.SetMaterial(this.head, eyesIndex, this.head.eyesRendererMaterialIndex);
    }

    public void SetFeet(int? feetIndex = -1, int? feetMaterialIndex = -1) {
        if (feetIndex == null || feetIndex >= this.feetOptions.Length || feetIndex < 0) {
            feetIndex = Random.Range(0, this.feetOptions.Length);
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
            hairIndex = Random.Range(0, this.hairOptions.Length);
        }
        this.DeactivateGameObjects(this.hairOptions);
        this.hair = this.hairOptions[(int)hairIndex];
        this.hair.gameObject.SetActive(true);
        this.SetMaterial(this.hair, hairMaterialIndex, this.hair.hatRendererMaterialIndex == 0 ? 1 : 0);
        // If the current hair model has a hat, set the hat's material.
        if (this.hair.hatRendererMaterialIndex >= 0 && this.hair.hatMaterials != null) {
            if (hatMaterialIndex == null || hatMaterialIndex >= this.hair.hatMaterials.Length || hatMaterialIndex < 0) {
                hatMaterialIndex = Random.Range(0, this.hair.hatMaterials.Length);
            }
            this.SetMaterialFromList(this.hair, this.hair.hatMaterials, (int)hatMaterialIndex,
                    this.hair.hatRendererMaterialIndex);
        }
    }

    public void SetJacket(int? jacketIndex = -1, int? jacketMaterialIndex = -1) {
        if (jacketIndex == null || jacketIndex >= this.jacketOptions.Length || jacketIndex < 0) {
            jacketIndex = Random.Range(0, this.jacketOptions.Length);
        }
        this.DeactivateGameObjects(this.jacketOptions);
        this.jacket = this.jacketOptions[(int)jacketIndex];
        this.jacket.gameObject.SetActive(true);
        this.SetMaterial(this.jacket, jacketMaterialIndex);
    }

    public void SetLegs(int? legsIndex = -1, int? legsMaterialIndex = -1) {
        if (legsIndex == null || legsIndex >= this.legsOptions.Length || legsIndex < 0) {
            legsIndex = Random.Range(0, this.legsOptions.Length);
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
            skinIndex = Random.Range(0, this.skinOptions.Length);
        }
        this.SetSkinMaterial(new SkinObjectMaterialOption[]{this.head}, (int)skinIndex);
        this.SetSkinMaterial(this.chestOptions, (int)skinIndex);
        this.SetSkinMaterial(this.feetOptions, (int)skinIndex);
        this.SetSkinMaterial(this.legsOptions, (int)skinIndex);
    }

    public void SetTie(int? tieIndex = -1, int? tieMaterialIndex = -1) {
        if (tieIndex == null || tieIndex >= this.tieOptions.Length || tieIndex < 0) {
            tieIndex = Random.Range(0, this.tieOptions.Length);
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

    private void DeactivateGameObjects(ObjectMaterialOption[] options) {
        foreach (ObjectMaterialOption option in options) {
            option.gameObject.SetActive(false);
        }
    }

    private void SetMaterial(ObjectMaterialOption option, int? materialOptionIndex, int rendererMaterialIndex = 0) {
        if (materialOptionIndex == null || materialOptionIndex >= option.materials.Length || materialOptionIndex < 0) {
            materialOptionIndex = Random.Range(0, option.materials.Length);
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

    private void SetSkinMaterial(ObjectMaterialOption[] options, int skinMaterialIndex) {
        foreach (SkinObjectMaterialOption option in options) {
            // If this body part shows skin...
            if (option.skinRendererMaterialIndex >= 0) {
                // Set the skin material for the body part using the chosen index.
                this.SetMaterialFromList(option, this.skinOptions, skinMaterialIndex,
                        option.skinRendererMaterialIndex);
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
