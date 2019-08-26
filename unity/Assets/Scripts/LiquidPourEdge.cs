using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


 public class ReadOnlyAttribute : PropertyAttribute
 {
 
 }
 
 [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
 public class ReadOnlyDrawer : PropertyDrawer
 {
     public override float GetPropertyHeight(SerializedProperty property,
                                             GUIContent label)
     {
         return EditorGUI.GetPropertyHeight(property, label, true);
     }
 
     public override void OnGUI(Rect position,
                                SerializedProperty property,
                                GUIContent label)
     {
         GUI.enabled = false;
         EditorGUI.PropertyField(position, property, label, true);
         GUI.enabled = true;
     }
 }
 
public abstract class LiquidPourEdge : MonoBehaviour
{
    public float radiusRaycastOffset = 0.03f;
    public Mesh debugQuad = null;
    public bool renderDebugLevelPlane = false;
    public GameObject waterEmiter = null;
    public float liquidVolumeLiters = 0f; 
    public LiquidType initialLiquidType = LiquidType.none;
    [ReadOnly] public float emptyValue = 0.6f;
    [ReadOnly] public float fullValue = 0.4f;
    [ReadOnly] public float containerMaxLiters = 1f;
    [ReadOnly] public float shaderFill = 0.0f;
    [ReadOnly] public float normalizedCurrentFill = 0.0f;
    protected Wobble wobbleComponent = null;
    private GameObject activeFlow = null;
    private Dictionary<LiquidType, float> solutionPercentages = new Dictionary<LiquidType, float>();
    private bool setupColor = false;
    private float secondsMix = 3.0f;
    private float currentTimeSeconds = 0f;

    protected abstract Vector3 getEdgeLowestPointWorldSpace(Vector3 up, bool withOffset = false);

    // Start is called before the first frame update
    void Start()
    {
        wobbleComponent = this.GetComponentInParent<Wobble>();
        
        foreach (var typeObj in LiquidType.GetValues(typeof(LiquidType))) {
            LiquidType liquidType = (LiquidType) typeObj;
            if (liquidType != LiquidType.none) {
                solutionPercentages.Add(liquidType, 0);
            }
        }

        if (initialLiquidType != LiquidType.none) {
            solutionPercentages[initialLiquidType] = liquidVolumeLiters > 0 ? 1.0f : 0;

            Liquid liquid = LiquidProperties.liquids[initialLiquidType];

            var mr = this.transform.GetComponentInParent<MeshRenderer>();

            //Debug.Log(" Setting tint for " + initialLiquidType + " to " + liquid.color);
            mr.material.SetColor("_Tint", liquid.color);
            mr.material.SetColor("_MixTint", liquid.color);

            normalizedCurrentFill = liquidVolumeLiters / containerMaxLiters;
            if (normalizedCurrentFill > 0.0001f) {
                mr.enabled = true;
            }
            shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
            mr.material.SetFloat("_FillAmount", shaderFill);
        }
    }

    public Dictionary<string, float> GetSolutionPrecentages() {
        var result = new Dictionary<string, float>();
        foreach (KeyValuePair<LiquidType, float> entry in solutionPercentages) {
            result.Add(entry.Key.ToString(), entry.Value);
        }
        return result;
    }

    public bool isFilled() {
        return liquidVolumeLiters > 0;
    }

    public LiquidMetadata getLiquidMetadata() {
         return new LiquidMetadata() {
            isFilled = this.isFilled(),
            solutionPercentages = this.GetSolutionPrecentages(),
            totalLiters = this.liquidVolumeLiters,
            maxLiters = this.containerMaxLiters
        };
    }

    void SetFillAmount(float normalizedFill, Color mixColor, float liters) {
        normalizedCurrentFill = normalizedFill;
        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        mr.material.SetFloat("_FillAmount", shaderFill);

        var currentMixTint = mr.material.GetColor("_MixTint");
        mr.material.SetColor("_MixTint", mixColor);
        var currentRim = mr.material.GetFloat("_MixRim");

        var tintDiff = currentMixTint - mixColor;
        var dot = Vector4.Dot(tintDiff, tintDiff);
        if (dot > 0.1) {
            
            // TODO use solution to calculate mix
            var currentColor = mr.material.GetColor("_Tint");
            mr.material.SetColor("_Tint", (currentColor * (1.0f - 2*currentRim) + currentMixTint * 2*currentRim) );
            currentRim = 0;
        }
       
        var worldSpaceDiff = (emptyValue - fullValue) * (liters / containerMaxLiters);
        Debug.Log("^^^^^^^ liters " + liters + " woldSPace val " + worldSpaceDiff);
        mr.material.SetFloat("_MixRim", currentRim + worldSpaceDiff * 2);
    }

     void TransferLiquidVolume(float liters, LiquidPourEdge transferer, float deltaEdgeDifference) {
        // TODO: Calculate correctly how much this gets filled based on the transferer properties
        transferer.liquidVolumeLiters -= liters;
        transferer.normalizedCurrentFill = transferer.liquidVolumeLiters / transferer.containerMaxLiters;

        Color transferColor = transferer.getLiquidColor();

        // Update target solution
        foreach(KeyValuePair<LiquidType, float> transferSolutionEntry in transferer.solutionPercentages)
        {
            this.solutionPercentages[transferSolutionEntry.Key] = ((transferSolutionEntry.Value * liters) + (this.solutionPercentages[transferSolutionEntry.Key] * liquidVolumeLiters)) / (this.liquidVolumeLiters + liters);
        }

        this.liquidVolumeLiters += liters;

        var solutionString = "";
        foreach (KeyValuePair<LiquidType, float> entry in solutionPercentages) {
            solutionString += entry.Key.ToString() + " : " + entry.Value + ", ";
        }
        Debug.Log("Transfering liquid volume from " +  this.gameObject.name + " to  " + transferer.gameObject.name + " amount " + liters + " total in new " + liquidVolumeLiters + " str " + solutionString);

        this.normalizedCurrentFill = this.liquidVolumeLiters / this.containerMaxLiters;
        this.SetFillAmount(normalizedCurrentFill, transferColor, liters);
    }

     void LoseLiquidVolume(float liters) {
        // TODO: Calculate correctly how much this gets filled based on the transferer properties
        this.liquidVolumeLiters -= liters;
        this.normalizedCurrentFill = this.liquidVolumeLiters / this.containerMaxLiters;
    }

    // Update is called once per frame
    void Update()
    {

        // if (!setupColor) {
        //     setupColor = true;
        //     this.transform.parent.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Tint", new Color(0, 100, 0, 120));
        // }
        // var mr = this.transform.GetComponentInParent<MeshRenderer>();
        // shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        // Debug.Log("fullValue " + fullValue + ", emptyValue ," + emptyValue + " normalizedCurrentFill " + normalizedCurrentFill);
        // mr.material.SetFloat("_FillAmount", shaderFill);

        MixSolution();
        

        // if (liquidVolumeLiters > 0) {
            var up = this.transform.parent.up;
            var edgeLowestWorld = getEdgeLowestPointWorldSpace(up);
            var waterLevelWorld = getWaterLevelPositionWorld();

            var edgeLiquidDifference = waterLevelWorld.y - edgeLowestWorld.y;
            if (edgeLiquidDifference > 0) {
                var containerRotationRadians = Mathf.Acos(Vector3.Dot(Vector3.up, up));
                Debug.Log("Release liquid edge diff: " + edgeLiquidDifference  + " water level:  " + waterLevelWorld.y + " cup edge lowest: "+ edgeLowestWorld.y + " container angle: " + (180.0f/Mathf.PI) * containerRotationRadians) ;
                
                ReleaseLiquid(edgeLiquidDifference, edgeLowestWorld, containerRotationRadians);
            }
            else if (activeFlow != null)
            {
                // Make flow smaller do not turn off
                activeFlow.transform.position = edgeLowestWorld;
                // var containerRotationRadians = Mathf.Acos(Vector3.Dot(Vector3.up, up));
                // activeFlow.transform.localRotation = Quaternion.AngleAxis(containerRotationRadians * 180.0f /  Mathf.PI , Vector3.up);
                // activeFlow.transform.rotation = 
                // activeFlow.SetActive(false);
            }
        // }
    }

    protected void MixSolution() {
        var mr = this.transform.parent.GetComponent<MeshRenderer>();
        if (mr) {
            var mixRim = mr.material.GetFloat("_MixRim");
            var mixTint = mr.material.GetColor("_MixTint");
            var mixColor = mr.material.GetColor("_Tint");

            var diff = mixColor - mixTint;
            var dot = Vector4.Dot(diff, diff);
            if (dot > 0.08) { // mixRim > 0.001f &&

                var alphaRatio = mixTint.a / mixColor.a;
                var speedDividerSlow = 45.0f;
                var speedDividerFast = 20.0f;
                // var speedDividerSlow = speedDividerFast * alphaRatio * alphaRatio;

                var mainColorDivide = mixColor.a > mixTint.a ? speedDividerSlow : speedDividerFast;
                var mixColorDivide = mixTint.a > mixColor.a ? speedDividerSlow : speedDividerFast;
                
                // var targetColor = (alphaRatio * mixTint + mixColor) / (alphaRatio + 1);
                var colorVelocityDelta = (Time.deltaTime / mainColorDivide);
                var targetColor = (mixColor + mixTint * colorVelocityDelta) / (1.0f + colorVelocityDelta);

                colorVelocityDelta = (Time.deltaTime / mixColorDivide);
                var targetMixColor = (mixTint + mixColor * colorVelocityDelta) / (1.0f + colorVelocityDelta);

                 // Debug.Log("****** Target Mix " + targetColor + " obj: " + this.transform.parent.parent.gameObject.name + " dotRaw: " + speedDividerSlow + " dot: " + dot + " lenmix: " + lenMix + " lenCol "+ lenCol);
                mr.material.SetColor("_Tint", targetColor);
                // mr.material.SetFloat("_MixRim", mixRim - (Time.deltaTime / 500.0f));
                mr.material.SetColor("_MixTint", targetMixColor);
            }
            // else {
            //     mr.material.SetFloat("_MixRim", 0.0f);
            // }
        }
    }

    protected void ReleaseLiquid(float edgeDifference, Vector3 edgePositionWorld, float containerRotationRadians) {
        //Debug.Log("Fluid out!!!! " + edgeDifference);
        // var edgeLocalSpace = this.transform.InverseTransformPoint(edgePositionWorld);

        var centerToEdge = edgePositionWorld - this.transform.position;
        // var edgeLocalSpace = this.transform.worldToLocalMatrix.MultiplyPoint(edgePositionWorld);
        var edgeAngle = Mathf.Atan2(centerToEdge.x, centerToEdge.z);
        if (activeFlow == null) {
            activeFlow = Object.Instantiate(
                this.waterEmiter,
                edgePositionWorld,
                Quaternion.identity,
                this.transform.parent
            );

            

            activeFlow.transform.rotation = Quaternion.AngleAxis((edgeAngle * 180.0f /  Mathf.PI), Vector3.up);
        }
        else {
            activeFlow.SetActive(true);
            activeFlow.transform.rotation = Quaternion.AngleAxis((edgeAngle * 180.0f /  Mathf.PI) , Vector3.up);
            // activeFlow.transform.rotation = Quaternion.Inverse(this.transform.parent.rotation);
        }

       // activeFlow.transform.rotation = Quaternion.identity;

        const float liquidTransferConstant = 1f;
        var normalizedFillDifference = liquidTransferConstant * edgeDifference / (emptyValue - fullValue);
        // var normalizedNew = (emptyValue - edgeDifference) / (emptyValue - fullValue);
       

        var edgeLowesPosXZ = edgePositionWorld;
        edgeLowesPosXZ.y = 0;
        var posXZ = this.transform.position;
        posXZ.y = 0;

        var lenXZ = (edgeLowesPosXZ - posXZ).magnitude;
        var angleDiff = Mathf.Atan2(edgeDifference, lenXZ);

        Debug.Log(" Angle Diff " + angleDiff * 180.0f / Mathf.PI   + " lenxz: " + lenXZ + " normdiff " + normalizedFillDifference +  " edge local " + centerToEdge + " edge angle " + edgeAngle * 180.0f / Mathf.PI);

        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        var currentFill = mr.material.GetFloat("_FillAmount");

        const float magicConstant = 1f;
        var newFill = currentFill + magicConstant * edgeDifference;

        var normalizedNew = (emptyValue - newFill) / (emptyValue - fullValue);

        normalizedCurrentFill = normalizedNew;

        // var newCurrentNormalizedFill = (emptyValue - currentFill) / (emptyValue - fullValue);

        var litersTransfer = normalizedFillDifference * containerMaxLiters;

        var newLiters = liquidVolumeLiters - litersTransfer;

        // var ratio = containerRotationRadians / (Mathf.PI / 2.0f);
       
        // litersTransfer = ratio * liquidVolumeLiters;
        //  Debug.Log("Rotation radians " + containerRotationRadians + " ratio " + ratio + " liters transfer " + litersTransfer);

        //var ratio = containerRotationRadians / (Mathf.PI / 2.0f);
        // litersTransfer = ratio * liquidVolumeLiters;
        // normalizedFillDifference = litersTransfer / containerMaxVolumeLiters;
        // newFill = currentFill + normalizedFillDifference * (emptyValue - fullValue);

        

        if (litersTransfer > liquidVolumeLiters) {
             Debug.Log("------- Liters transfer > than available liters " + litersTransfer);
            if (Mathf.Abs(containerRotationRadians) - 0.001f < (Mathf.PI / 2.0f)) {
                var ratio = containerRotationRadians / (Mathf.PI / 2.0f);
                litersTransfer = ratio * liquidVolumeLiters;
                Debug.Log("-------- Aproximate transfer of " + litersTransfer + " for angle " + containerRotationRadians * 180.0f / Mathf.PI);
                
            //  newFill = emptyValue + 1.0f;
            //  litersTransfer = liquidVolumeLiters;
            }
            else {
                newFill = emptyValue + 1.0f;
                litersTransfer = liquidVolumeLiters;
            }
            // edgeDifference = (liquidVolumeLiters / containerMaxVolumeLiters) * (emptyValue - fullValue);
        }

        // If there is too little liquid left
        if (liquidVolumeLiters < 0.001f) {
            newFill = emptyValue + 1.0f;
            litersTransfer = liquidVolumeLiters;
        }

        

        //LayerMask.GetMask("SimObjVisible"); 
        RaycastHit hit;

        var fromRay  = this.getEdgeLowestPointWorldSpace(this.getUpVector(), true);
        var raycastTrue = Physics.Raycast(fromRay, Vector3.down, out hit, 100, Physics.AllLayers & ~LayerMask.GetMask("SimObjInVisible"));

        Debug.DrawRay(fromRay, Vector3.down, Color.green, 2f);
        if (raycastTrue) {
             Debug.Log("Fluid transfer before to game object " + hit.collider.gameObject.name);

            var particleSystem = activeFlow.GetComponent<ParticleSystem>();
            //Time.deltaTime
            Debug.Log("***************** Curve max " + particleSystem.main.startSpeed.curveMultiplier);
            //particleSystem.Stop();
            var psMain = particleSystem.main;
            //psMain.duration = 4f;
            
            Debug.Log("Lifetime " + psMain.startLifetime.constant);
            psMain.startLifetime = hit.distance / particleSystem.main.startSpeed.curveMultiplier;
            //particleSystem.Play();
            // particleSystem.main.startLifetime.constant = hit.distance / particleSystem.main.startSpeed.constantMin; 
            // hit.distance

            var otherLiquidEdge = hit.collider.GetComponent<LiquidPourEdge>();
           
            if (otherLiquidEdge) {
                var mrOther = otherLiquidEdge.GetComponentInParent<MeshRenderer>();

                //otherLiquidEdge.TransferLiquid(normalizedFillDifference, this);

                otherLiquidEdge.TransferLiquidVolume(litersTransfer, this, edgeDifference);

                Debug.Log("Transfered " + edgeDifference);
                
                // /otherFillAmmount - 7.5f * edgeDifference
                // otherLiquidEdge.TransferLiquid()
                if (mrOther) {
                    mrOther.enabled = true;
                    // var otherFillAmmount = mrOther.material.GetFloat("_FillAmount");
                    // var flowTransferConstant = 0.01f;
                    // mrOther.material.SetFloat("_FillAmount", otherFillAmmount - magicConstant * edgeDifference);
                    // Debug.Log("Fluid transfer");
                }
            }
            else {
                this.LoseLiquidVolume(litersTransfer);
            }
        }
        else {
            this.LoseLiquidVolume(litersTransfer);
            Debug.Log("Raycast fail");
        }

        mr.material.SetFloat("_FillAmount", newFill);
        
    }

    private Color getLiquidColor() {
        var mr = this.transform.parent.GetComponent<MeshRenderer>();
        if (mr) {
            return mr.material.GetColor("_Tint");
        }
        return Color.magenta;
    }

    protected Vector3 getUpVector() {
        var up = this.transform.parent.up;
        // if (wobbleComponent == null) {
        //     wobbleComponent = this.transform.GetComponentInParent<Wobble>();
        // }

        
        // var wobbleRot = Quaternion.Euler(wobbleComponent.wobbleAmountX, 0, wobbleComponent.wobbleAmountZ);
        // return wobbleRot * up;

        return up;
    }


    protected Vector3 getWaterLevelPositionWorld() {
        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        if (mr != null) {
            var fillAmount = mr.material.GetFloat("_FillAmount");

            Gizmos.color = new Color(1f, 1f, 0.0f, 0.7f);

            var pos = this.transform.parent.position;
            pos.y += 0.5f - fillAmount;
            return pos;
        }
        else {
            Debug.LogError("No mesh renderer, with liquid material to get fill value");
        }
        return Vector3.zero;
    }


#if UNITY_EDITOR
    public static void SetLiquidComponent(string liquidPoutPrefabPath) {
        GameObject prefabRoot = Selection.activeGameObject;
        GameObject liquidPourEdge = GameObject.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath(liquidPoutPrefabPath, typeof(GameObject)) as GameObject);
        liquidPourEdge.transform.parent = prefabRoot.transform;

        var material = UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/SpecialFX/DynamicMixLiquidVolume.mat",typeof(Material)) as Material;

        prefabRoot.GetComponent<MeshRenderer>().material = material;
        var wobble = prefabRoot.AddComponent<Wobble>();
        wobble.MaxWobble = 0.03f;
        wobble.Recovery = 1;
        wobble.WobbleSpeed = 1;
        wobble.wobbleAmountX = 0;
        wobble.wobbleAmountZ = 0;
        
        var liquidEdge = liquidPourEdge.GetComponent<LiquidPourEdge>();
        var worldOrigin = prefabRoot.transform.position;

		Mesh mesh = liquidEdge.GetComponentInParent<MeshFilter>().sharedMesh;

        Vector3[] vertices = mesh.vertices;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < mesh.vertices.Length; i++) {
            minY = vertices[i].y < minY ? vertices[i].y : minY;
            maxY = vertices[i].y > maxY ? vertices[i].y : maxY;
        }
        
        var mr = liquidEdge.GetComponentInParent<MeshRenderer>();
        var offset = (maxY - minY) * 0.00001f;
        var volume = mesh_volume_calculator.VolumeOfMesh(mesh);

        liquidEdge.emptyValue = 0.5f - minY - offset;
        liquidEdge.fullValue = 0.5f - maxY + offset;

        var floatVolume = ((float) volume) * 1000.0f;
        liquidEdge.containerMaxLiters = floatVolume;

        mr.enabled = false;

        liquidPourEdge.transform.localPosition = new Vector3(0, maxY, 0);
        liquidPourEdge.transform.localScale = new Vector3(1, 1 , 1);

        Debug.Log("Constants, empty: " + liquidEdge.emptyValue + " full: " + liquidEdge.fullValue + "maxVolume liters: " + floatVolume + " minY: " + minY + " maxY: " + maxY + " maxY - minY: " + (maxY - minY));

    }
#endif

}


