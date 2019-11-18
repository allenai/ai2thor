using System.Linq;
using UnityEngine;
 
public class ColorChanger : MonoBehaviour
{
    //Define all material types
    public Material[] alarmClockMaterials;
    public Material[] appleMaterials;
    public Material[] basketballMaterials;
    public Material[] bowlMaterials;
    public Material[] garbageBinMaterials;
    public Material[] houseplantMaterials;
    public Material[] pillowMaterials;
    public Material[] sprayBottleMaterials;

    public Material[] boxMaterials;
    public Material[] cellphoneMaterials;
    public Material[] cupMaterials;
    public Material[] floorLampMaterials;
    public Material[] penPencilMaterials;
    public Material[] plateMaterials;
    public Material[] potMaterials;
    public Material[] statueMaterials;
    public Material[] watchMaterials;

    public Material[] armchairMaterials;
    public Material[] bedMaterials;
    public Material[] chairMaterials;
    public Material[] coffeeTableMaterials;
    public Material[] deskMaterials;
    public Material[] diningTableMaterials;
    public Material[] dresserMaterials;
    public Material[] officeChairMaterials;
    public Material[] shelvingUnitMaterials;
    public Material[] sideTableMaterials;
    public Material[] sofaMaterials;

    public Material[] ceramicMaterials;
    public Material[] fabricMaterials;
    public Material[] glassMaterials;
    public Material[] lightMaterials;
    public Material[] metalMaterials;
    public Material[] miscMaterials;
    public Material[] plasticMaterials;
    public Material[] woodMaterials;

    Material[] targetMaterials;
    Material[] backgroundMaterials;
    Material[] furnitureMaterials;
    Material[] quickMaterials;

    Material[] allMaterials;

    public void Start()
    {
        targetMaterials = alarmClockMaterials.Concat(appleMaterials).Concat(basketballMaterials).Concat(bowlMaterials).Concat(garbageBinMaterials).Concat(houseplantMaterials).Concat(pillowMaterials).Concat(sprayBottleMaterials).ToArray();
        backgroundMaterials = bedMaterials.Concat(boxMaterials).Concat(cellphoneMaterials).Concat(cupMaterials).Concat(floorLampMaterials).Concat(penPencilMaterials).Concat(plateMaterials).Concat(potMaterials).Concat(statueMaterials).Concat(watchMaterials).ToArray();
        furnitureMaterials = armchairMaterials.Concat(bedMaterials).Concat(chairMaterials).Concat(coffeeTableMaterials).Concat(deskMaterials).Concat(diningTableMaterials).Concat(dresserMaterials).Concat(officeChairMaterials).Concat(shelvingUnitMaterials).Concat(sideTableMaterials).Concat(sofaMaterials).ToArray();
        quickMaterials = ceramicMaterials.Concat(fabricMaterials).Concat(glassMaterials).Concat(lightMaterials).Concat(metalMaterials).Concat(miscMaterials).Concat(plasticMaterials).Concat(woodMaterials).ToArray();

        allMaterials = targetMaterials.Concat(backgroundMaterials).Concat(furnitureMaterials).Concat(quickMaterials).ToArray();
        //print(allMaterials[allMaterials.Length - 1]);
    }

    public void Update()
    {
        // MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        //for (int i = 0; i < quickMaterials.Length; i++)
        //{
        //    quickMaterials[i].color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        //}
    }

    public void RandomizeColor()
    {
        //MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        for (int i = 0; i < allMaterials.Length; i++)
        {
            allMaterials[i].color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
        }
    }
}