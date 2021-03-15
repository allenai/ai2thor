using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
 
public class ColorChanger : MonoBehaviour {
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
                      lightMaterials,
                      metalMaterials,

                      paperMaterials,
                      grungeMaterials,
                      waxMaterials,
                      soapMaterials,

                      plasticMaterials,
                      woodMaterials,

                      wallMaterials,
                      fridgeMaterials;

    Dictionary<string, Material[]> materials;
    Dictionary<string, Color[]> origColors;
    Dictionary<string, Texture[]> origTextures;

    public void Start() {
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
            ["Light"] = lightMaterials,
            ["Metal"] = metalMaterials,
            ["Plastic"] = plasticMaterials,
            ["Wood"] = woodMaterials,
            ["Wall"] = wallMaterials,
            ["Fridge"] = fridgeMaterials,
            ["Paper"] = paperMaterials,
            ["Grunge"] = grungeMaterials,
            ["Wax"] = waxMaterials,
            ["Soap"] = soapMaterials
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

    public void RandomizeMaterials() {
        foreach (KeyValuePair<string, Material[]> materialGroup in materials) {
            shuffleMaterials(materialGroup: materialGroup.Value);
        }
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