using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class ColorChanger : MonoBehaviour {
    // These will eventually be turned into sets, such that they are
    // easily checkable at runtime.
    public Material[] rawTrainMaterials,
                      rawValMaterials,
                      rawTestMaterials,
                      rawRobothorMaterials,
                      rawKitchenMaterials,
                      rawLivingRoomMaterials,
                      rawBedroomMaterials,
                      rawBathroomMaterials;


    public Material[] alarmClockMaterials,
                      appleMaterials,
                      basketballMaterials,
                      bowlMaterials,
                      garbageBinMaterials,
                      houseplantMaterials,
                      pillowMaterials,
                      sprayBottleMaterials,

                      boxMaterials,
                      cellphoneMaterials,
                      cupMaterials,
                      floorLampMaterials,
                      penPencilMaterials,
                      plateMaterials,
                      potMaterials,
                      statueMaterials,
                      watchMaterials,

                      armchairMaterials,
                      bedMaterials,
                      chairMaterials,
                      coffeeTableMaterials,
                      deskMaterials,
                      diningTableMaterials,
                      dresserMaterials,
                      officeChairMaterials,
                      shelvingUnitMaterials,
                      sideTableMaterials,
                      sofaMaterials,

                      fabricMaterials,
                      glassMaterials,
                      metalMaterials,

                      paperMaterials,
                      grungeMaterials,
                      waxMaterials,
                      soapMaterials,

                      plasticMaterials,
                      woodMaterials,

                      wallMaterials,
                      fridgeMaterials,
                      microwaveMaterials,
                      toasterMaterials,

                      lettuceMaterials,
                      lettuceSlicedMaterials,

                      soapBottleMaterials,

                      potatoMaterials,
                      potatoCookedMaterials,
                      potatoSlicedMaterials,

                      laptopMaterials,
                      breadMaterials,
                      panMaterials,
                      panDecalMaterials,

                      coffeeMachineMaterials;

    Dictionary<string, Material[]> materials = null;
    Dictionary<string, Color[]> origColors;
    Dictionary<string, Texture[]> origTextures;
    Dictionary<string, HashSet<Material>> materialGroups;

    protected void cacheMaterials() {
        if (materials == null) {
            materials = new Dictionary<string, Material[]> {
                ["AlarmClock"] = alarmClockMaterials,
                ["Apple"] = appleMaterials,
                ["Basketball"] = basketballMaterials,
                ["Bowl"] = bowlMaterials,
                ["GarbageBin"] = garbageBinMaterials,
                ["HousePlant"] = houseplantMaterials,
                ["Pillow"] = pillowMaterials,
                ["SprayBottle"] = sprayBottleMaterials,
                ["Box"] = boxMaterials,
                ["CellPhone"] = cellphoneMaterials,
                ["Cup"] = cupMaterials,
                ["FloorLamp"] = floorLampMaterials,
                ["PenPencil"] = penPencilMaterials,
                ["Plate"] = plateMaterials,
                ["Pot"] = potMaterials,
                ["Statue"] = statueMaterials,
                ["Watch"] = watchMaterials,
                ["ArmChair"] = armchairMaterials,
                ["Bed"] = bedMaterials,
                ["Chair"] = chairMaterials,
                ["CoffeeTable"] = coffeeTableMaterials,
                ["Desk"] = deskMaterials,
                ["DiningTable"] = diningTableMaterials,
                ["Dresser"] = dresserMaterials,
                ["OfficeChair"] = officeChairMaterials,
                ["ShelvingUnit"] = shelvingUnitMaterials,
                ["SideTable"] = sideTableMaterials,
                ["Sofa"] = sofaMaterials,
                ["Fabric"] = fabricMaterials,
                ["Glass"] = glassMaterials,
                ["Metal"] = metalMaterials,
                ["Plastic"] = plasticMaterials,
                ["Wood"] = woodMaterials,
                ["Wall"] = wallMaterials,
                ["Fridge"] = fridgeMaterials,
                ["Paper"] = paperMaterials,
                ["Grunge"] = grungeMaterials,
                ["Wax"] = waxMaterials,
                ["Soap"] = soapMaterials,
                ["Microwave"] = microwaveMaterials,
                ["Toaster"] = toasterMaterials,
                ["Lettuce"] = lettuceMaterials,
                ["SoapBottle"] = soapBottleMaterials,
                ["Potato"] = potatoMaterials,
                ["PotatoCooked"] = potatoCookedMaterials,
                ["PotatoSliced"] = potatoSlicedMaterials,
                ["Laptop"] = laptopMaterials,
                ["Bread"] = breadMaterials,
                ["Pan"] = panMaterials,
                ["PanDecal"] = panDecalMaterials,
                ["CoffeeMachine"] = coffeeMachineMaterials,
            };

            // makes indexing into them faster
            materialGroups = new Dictionary<string, HashSet<Material>> {
                ["train"] = new HashSet<Material>(rawTrainMaterials),
                ["val"] = new HashSet<Material>(rawValMaterials),
                ["test"] = new HashSet<Material>(rawTestMaterials),
                ["robothor"] = new HashSet<Material>(rawRobothorMaterials),
                ["kitchen"] = new HashSet<Material>(rawKitchenMaterials),
                ["livingroom"] = new HashSet<Material>(rawLivingRoomMaterials),
                ["bedroom"] = new HashSet<Material>(rawBedroomMaterials),
                ["bathroom"] = new HashSet<Material>(rawBathroomMaterials)
            };

            // cache all the original values
            origColors = new Dictionary<string, Color[]>();
            origTextures = new Dictionary<string, Texture[]>();
            foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
                Color[] groupColors = new Color[materialGroup.Value.Length];
                Texture[] groupTextures = new Texture[materialGroup.Value.Length];
                for (int i = 0; i < materialGroup.Value.Length; i++) {
                    Material mat = materialGroup.Value[i];
                    if (mat.HasProperty("_Color")) {
                        groupColors[i] = mat.color;
                    }
                    if (mat.HasProperty("_MainTex")) {
                        groupTextures[i] = mat.mainTexture;
                    }
                }
                origColors[materialGroup.Key] = groupColors;
                origTextures[materialGroup.Key] = groupTextures;
            }
        }
    }

    private void swapMaterial(Material mat1, Material mat2) {
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

    private void shuffleMaterials(Material[] materialGroup) {
        for (int n = materialGroup.Length - 1; n >= 0; n--) {
            int i = Random.Range(0, n + 1);
            swapMaterial(materialGroup[i], materialGroup[n]);
        }
    }

    private void shuffleMaterials(List<Material> materialGroup) {
        for (int n = materialGroup.Count - 1; n >= 0; n--) {
            int i = Random.Range(0, n + 1);
            swapMaterial(materialGroup[i], materialGroup[n]);
        }
    }

    public int RandomizeMaterials(
        bool useTrainMaterials,
        bool useValMaterials,
        bool useTestMaterials,
        bool useExternalMaterials,
        HashSet<string> inRoomTypes
    ) {
        if (materials == null) {
            cacheMaterials();
        }
        int numTotalMaterials = 0;
        if (inRoomTypes == null) {
            // select from all room types
            foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
                List<Material> validMaterials = new List<Material>();
                foreach (Material material in materialGroup.Value) {
                    if (
                        useTrainMaterials && materialGroups["train"].Contains(material) ||
                        useValMaterials && materialGroups["val"].Contains(material) ||
                        useTestMaterials && materialGroups["test"].Contains(material)
                    ) {
                        validMaterials.Add(material);
                        numTotalMaterials++;
                    }
                }
                shuffleMaterials(materialGroup: validMaterials);
            }
        } else {
            // select from only specific room types
            foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
                List<Material> validMaterials = new List<Material>();
                foreach (Material material in materialGroup.Value) {
                    if (
                        useTrainMaterials && materialGroups["train"].Contains(material) ||
                        useValMaterials && materialGroups["val"].Contains(material) ||
                        useTestMaterials && materialGroups["test"].Contains(material)
                    ) {
                        foreach (string roomType in inRoomTypes) {
                            if (materialGroups[roomType].Contains(material)) {
                                validMaterials.Add(material);
                                numTotalMaterials++;
                                break;
                            }
                        }
                    }
                }
                shuffleMaterials(materialGroup: validMaterials);
            }
        }
        return numTotalMaterials;
    }

    public void ResetMaterials() {
        foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
            for (int i = 0; i < materialGroup.Value.Length; i++) {
                Material mat = materialGroup.Value[i];
                if (mat.HasProperty("_Color")) {
                    mat.color = origColors[materialGroup.Key][i];
                }
                if (mat.HasProperty("_MainTex")) {
                    mat.mainTexture = origTextures[materialGroup.Key][i];
                }
            }
        }
    }

    public void RandomizeColor() {
        if (materials == null) {
            cacheMaterials();
        }
        foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
            for (int i = 0; i < materialGroup.Value.Length; i++) {
                Material mat = materialGroup.Value[i];
                if (mat.HasProperty("_Color")) {
                    mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                }
            }
        }
    }

    public void ResetColors() {
        foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
            for (int i = 0; i < materialGroup.Value.Length; i++) {
                Material mat = materialGroup.Value[i];
                if (mat.HasProperty("_Color")) {
                    mat.color = origColors[materialGroup.Key][i];
                }
            }
        }
    }
}