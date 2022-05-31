using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;



public class ColorChanger : MonoBehaviour {
    // These will eventually be turned into sets, such that they are
    // easily checkable at runtime.

    Dictionary<string, List<ResourceAssetReference<Material>>> objectMaterials = null;
    private Dictionary<Material, Material> origMaterials = new Dictionary<Material, Material>();
    Dictionary<string, HashSet<string>> materialGroupPaths;
    private ResourceAssetManager assetManager;

    protected void cacheMaterials() {
        var objectMaterialLabels = new string[] {
            "AlarmClockMaterials",
            "AppleMaterials",
            "BasketballMaterials",
            "BowlMaterials",
            "GarbageBinMaterials",
            "HousePlantMaterials",
            "PillowMaterials",
            "SprayBottleMaterials",
            "BoxMaterials",
            "CellPhoneMaterials",
            "CupMaterials",
            "FloorLampMaterials",
            "PenPencilMaterials",
            "PlateMaterials",
            "PotMaterials",
            "StatueMaterials",
            "WatchMaterials",
            "ArmChairMaterials",
            "BedMaterials",
            "BedsheetMaterials",
            "ChairMaterials",
            "CoffeeTableMaterials",
            "DeskMaterials",
            "DiningTableMaterials",
            "DresserMaterials",
            "OfficeChairMaterials",
            "ShelvingUnitMaterials",
            "SideTableMaterials",
            "SofaMaterials",
            "FabricMaterials",
            "GlassMaterials",
            "MetalMaterials",
            "PlasticMaterials",
            "WoodMaterials",
            "WallMaterials",
            "FridgeMaterials",
            "PaperMaterials",
            "GrungeMaterials",
            "WaxMaterials",
            "SoapMaterials",
            "MicrowaveMaterials",
            "ToasterMaterials",
            "LettuceMaterials",
            "SoapBottleMaterials",
            "PotatoMaterials",
            "PotatoCookedMaterials",
            "PotatoSlicedMaterials",
            "LaptopMaterials",
            "BreadMaterials",
            "PanMaterials",
            "PanDecalMaterials",
            "CoffeeMachineMaterials"
        };
        var materialGroupLabels = new string[] {
            "RawTrainMaterials",
            "RawValMaterials",
            "RawTestMaterials",
            "RawRobothorMaterials",
            "RawKitchenMaterials",
            "RawLivingRoomMaterials",
            "RawBedroomMaterials",
            "RawBathroomMaterials"
        };

        this.assetManager = new ResourceAssetManager();

        objectMaterials = new Dictionary<string, List<ResourceAssetReference<Material>>>();
        foreach (var label in objectMaterialLabels) {
            objectMaterials[label] = this.assetManager.FindResourceAssetReferences<Material>(label);
        }

        materialGroupPaths = new Dictionary<string, HashSet<string>>();
        foreach (var label in materialGroupLabels) {
            HashSet<string> paths = new HashSet<string>();
            foreach (var resourceAssetRef in this.assetManager.FindResourceAssetReferences<Material>(label)) {
                paths.Add(resourceAssetRef.ResourcePath);
            }

            materialGroupPaths[label] = paths;
        }
    }

    private void storeOriginal(Material mat) {
        if (!origMaterials.ContainsKey(mat)) {
            origMaterials[mat] = new Material(mat);
        }
    }

    private void swapMaterial(Material mat1, Material mat2) {
        storeOriginal(mat1);
        storeOriginal(mat2);

        if (mat1.HasProperty("_MainTex") && mat2.HasProperty("_MainTex")) {
            Texture tempTexture = mat1.mainTexture;
            mat1.mainTexture = mat2.mainTexture;
            mat2.mainTexture = tempTexture;
        }

        if (mat1.HasProperty("_Color") && mat2.HasProperty("_Color")) {
            Color tempColor = mat1.color;
            mat1.color = mat2.color;
            mat2.color = tempColor;
        }
    }

    private void shuffleMaterials(HashSet<string> activeMaterialNames, List<ResourceAssetReference<Material>> materialGroup) {
        for (int n = materialGroup.Count - 1; n >= 0; n--) {
            int i = Random.Range(0, n + 1);

            ResourceAssetReference<Material> refA = materialGroup[n];
            ResourceAssetReference<Material> refB = materialGroup[i];
            if (activeMaterialNames.Contains(refA.Name) || activeMaterialNames.Contains(refB.Name)) {
                swapMaterial(refA.Load(), refB.Load());
            } else {
                // just swap the references if neither material is actively being used in the scene
                materialGroup[n] = refB;
                materialGroup[i] = refA;
            }
        }
    }



    public int RandomizeMaterials(
        bool useTrainMaterials,
        bool useValMaterials,
        bool useTestMaterials,
        bool useExternalMaterials,
        HashSet<string> inRoomTypes
    ) {
        if (objectMaterials == null) {
            cacheMaterials();
        }

        Dictionary<string, string> roomTypeLabelMap = new Dictionary<string, string>() {
            ["kitchen"] = "RawKitchenMaterials",
            ["livingroom"] = "RawLivingRoomMaterials",
            ["bedroom"] = "RawBedroomMaterials",
            ["bathroom"] = "RawBathroomMaterials",
            ["robothor"] = "RawRobothorMaterials",
        };

        int numTotalMaterials = 0;

        // the purpose of finding all the active materials is to limit what
        // assets actually get loaded.  Without this filter, all assets would 
        // need to get loaded and shuffled, using this the shuffle step
        // can limit what materials actually get loaded through Resources.Load()
        HashSet<string> activeMaterialNames = new HashSet<string>();
        foreach (var renderer in GameObject.FindObjectsOfType<Renderer>()) {
            foreach (var mat in renderer.sharedMaterials) {
                if (mat != null && mat.name != null) {
#if UNITY_EDITOR
                    Debug.Log("Found material: " + mat.name);
#endif
                    activeMaterialNames.Add(mat.name);
                }
            }
        }

        foreach (KeyValuePair<string, List<ResourceAssetReference<Material>>> materialGroup in objectMaterials) {
            List<ResourceAssetReference<Material>> validMaterials = new List<ResourceAssetReference<Material>>();
            foreach (ResourceAssetReference<Material> resourceAssetReference in materialGroup.Value) {
                if (
                    useTrainMaterials && materialGroupPaths["RawTrainMaterials"].Contains(resourceAssetReference.ResourcePath) ||
                    useValMaterials && materialGroupPaths["RawValMaterials"].Contains(resourceAssetReference.ResourcePath) ||
                    useTestMaterials && materialGroupPaths["RawTestMaterials"].Contains(resourceAssetReference.ResourcePath)
                ) {
                    if (inRoomTypes == null) {
                        validMaterials.Add(resourceAssetReference);
                        numTotalMaterials++;
                    } else {
                        foreach (string roomType in inRoomTypes) {
                            string roomLabel = roomTypeLabelMap[roomType];
                            if (materialGroupPaths[roomLabel].Contains(resourceAssetReference.ResourcePath)) {
                                validMaterials.Add(resourceAssetReference);
                                numTotalMaterials++;
                                break;
                            }
                        }
                    }
                }
            }

            shuffleMaterials(activeMaterialNames, validMaterials);
        }



        return numTotalMaterials;
    }

    public void ResetMaterials() {
        foreach (KeyValuePair<Material, Material> matPair in origMaterials) {
            Material mat = matPair.Key;
            if (mat.HasProperty("_Color")) {
                mat.color = matPair.Value.color;
            }
            if (mat.HasProperty("_MainTex")) {
                mat.mainTexture = matPair.Value.mainTexture;
            }
        }
    }

    public void RandomizeColor() {
        if (objectMaterials == null) {
            cacheMaterials();
        }

        foreach (var renderer in GameObject.FindObjectsOfType<Renderer>()) {
            foreach (var mat in renderer.sharedMaterials) {
                storeOriginal(mat);
                if (mat.HasProperty("_Color")) {
                    mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                }
            }
        }
    }

    public void ResetColors() {
        foreach (KeyValuePair<Material, Material> matPair in origMaterials) {
            Material mat = matPair.Key;
            if (mat.HasProperty("_Color")) {
                mat.color = matPair.Value.color;
            }
        }
    }

    public Dictionary<string, string[]> GetMaterials() {
        Dictionary<string, string[]> orSomething = new Dictionary<string, string[]>();

        if (objectMaterials == null) {
            cacheMaterials();
        }

        foreach (KeyValuePair<string, List<ResourceAssetReference<Material>>> sm in objectMaterials) {
            List<string> materialNames = new List<string>();

            foreach (ResourceAssetReference<Material> resourceAssetReference in sm.Value) {
                materialNames.Add(resourceAssetReference.Name);
            }

            orSomething.Add(sm.Key, materialNames.ToArray());
        }

        return orSomething;
    }
}
