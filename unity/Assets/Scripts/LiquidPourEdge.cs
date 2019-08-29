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

    private float flowFadeOutSeconds = 0.4f;
    private float flowTimer = 0.0f;

    private float flowEmissionRate;

    private Color topColorOffset = new Color(0.08f, 0.08f, 0.08f, 0);

    protected abstract Vector3 getEdgeLowestPointWorldSpace(Vector3 up, bool withOffset = false);

    // Start is called before the first frame update
    void Start()
    {
        wobbleComponent = this.GetComponentInParent<Wobble>();
        Fill(initialLiquidType, liquidVolumeLiters);
    }

    public void Fill(LiquidType liquidType, float litersAmount) {
        
        foreach (var typeObj in LiquidType.GetValues(typeof(LiquidType))) {
            LiquidType availableLiquidType = (LiquidType) typeObj;
            if (availableLiquidType != LiquidType.none) {
                solutionPercentages[availableLiquidType] = 0;
            }
        }

        if (liquidType != LiquidType.none) {
            liquidVolumeLiters = litersAmount;
            solutionPercentages[liquidType] = liquidVolumeLiters > 0 ? 1.0f : 0;

            Liquid liquid = LiquidProperties.liquids[liquidType];

            Debug.Log(" Call to fill wiht " + liquidType + " liters "+ litersAmount + " Color r " + liquid.color.r + " g " + liquid.color.g + " b " + liquid.color.b);

            var mr = this.transform.GetComponentInParent<MeshRenderer>();

            //Debug.Log(" Setting tint for " + liquidType + " to " + liquid.color);
            mr.material.SetColor("_Tint", liquid.color);
            mr.material.SetColor("_MixTint", liquid.color);
            mr.material.SetColor("_TopColor", liquid.topColor + topColorOffset);

            normalizedCurrentFill = liquidVolumeLiters / containerMaxLiters;
            if (normalizedCurrentFill > 0.0001f) {
                mr.enabled = true;
            }
            shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
            mr.material.SetFloat("_FillAmount", shaderFill);
        }
    }

    public void Empty() {
         var mr =GetComponentInParent<MeshRenderer>();
         mr.enabled = false;
         liquidVolumeLiters = 0.0f;
         normalizedCurrentFill = 0.0f;
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

    public Color getAverageFlowColor() {
        Color mixColor = Vector4.zero;
        foreach (KeyValuePair<LiquidType, float> entry in solutionPercentages) {
            var color = LiquidProperties.liquids[entry.Key].flowColor;
            mixColor += entry.Value * color;
        }
        return mixColor;
    }

    void SetFillAmount(float normalizedFill, Color mixColor, float liters, bool wasEmpty) {
        normalizedCurrentFill = normalizedFill;
        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        mr.material.SetFloat("_FillAmount", shaderFill);

        var currentMixTint = mr.material.GetColor("_MixTint");
        mr.material.SetColor("_MixTint", mixColor);
        mr.material.SetColor("_TopColor", mixColor + topColorOffset);
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
        mr.material.SetFloat("_MixRim", currentRim + worldSpaceDiff * 2);

        if (wasEmpty) {
            mr.material.SetColor("_Tint", mixColor);
            mr.material.SetFloat("_MixRim", 0);
        }
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
        var wasEmpty = !this.isFilled();

        this.liquidVolumeLiters += liters;

        // var solutionString = "";
        // foreach (KeyValuePair<LiquidType, float> entry in solutionPercentages) {
        //     solutionString += entry.Key.ToString() + " : " + entry.Value + ", ";
        // }
        //  Debug.Log("Transfering liquid volume from " +  this.gameObject.name + " to  " + transferer.gameObject.name + " amount " + liters + " total in new " + liquidVolumeLiters + " str " + solutionString);

        this.normalizedCurrentFill = this.liquidVolumeLiters / this.containerMaxLiters;
        this.SetFillAmount(normalizedCurrentFill, transferColor, liters, wasEmpty);
    }

     void LoseLiquidVolume(float liters) {
        // TODO: Calculate correctly how much this gets filled based on the transferer properties
        this.liquidVolumeLiters -= liters;
        this.normalizedCurrentFill = this.liquidVolumeLiters / this.containerMaxLiters;
    }

    void Update()
    {
        MixSolution();

            var up = this.transform.parent.up;
            var edgeLowestWorld = getEdgeLowestPointWorldSpace(up);
            var waterLevelWorld = getWaterLevelPositionWorld();

            var edgeLiquidDifference = waterLevelWorld.y - edgeLowestWorld.y;
            if (edgeLiquidDifference > 0.0001) {
                var containerRotationRadians = Mathf.Acos(Vector3.Dot(Vector3.up, up));
                Debug.Log("Release liquid edge diff: " + edgeLiquidDifference  + " water level:  " + waterLevelWorld.y + " cup edge lowest: "+ edgeLowestWorld.y + " container angle: " + (180.0f/Mathf.PI) * containerRotationRadians) ;
                
                ReleaseLiquid(edgeLiquidDifference, edgeLowestWorld, containerRotationRadians);
            }
            else if (activeFlow != null)
            {
                // TODO Make flow smaller do not turn off
                activeFlow.transform.position = edgeLowestWorld;
                var currentTime = Time.time;
                 var ps = activeFlow.GetComponent<ParticleSystem>();
                var e = ps.emission;

                if (e.rateOverTime.constant > 0) {
                    float alpha = Mathf.Min((currentTime - flowTimer) / flowFadeOutSeconds, 1.0f);
                    e.rateOverTime = (1 - alpha) * flowEmissionRate;
                }

                // if (currentTime - flowTimer > flowFadeOutSeconds) {
                //     // activeFlow.SetActive(false);
                   
                //     var e = ps.emission;
                //     e.rateOverTime = 0;
                // }
            }
    }

    protected void MixSolution() {
        var mr = this.transform.parent.GetComponent<MeshRenderer>();
        if (mr) {
            var mixRim = mr.material.GetFloat("_MixRim");
            var mixTint = mr.material.GetColor("_MixTint");
            var mixColor = mr.material.GetColor("_Tint");

            var diff = mixColor - mixTint;
            var dot = Vector4.Dot(diff, diff);
            if (dot > 0.08) {
                var alphaRatio = mixTint.a / mixColor.a;
                var speedDividerSlow = 45.0f;
                var speedDividerFast = 20.0f;

                var mainColorDivide = mixColor.a > mixTint.a ? speedDividerSlow : speedDividerFast;
                var mixColorDivide = mixTint.a > mixColor.a ? speedDividerSlow : speedDividerFast;
                
                var colorVelocityDelta = (Time.deltaTime / mainColorDivide);
                var targetColor = (mixColor + mixTint * colorVelocityDelta) / (1.0f + colorVelocityDelta);

                colorVelocityDelta = (Time.deltaTime / mixColorDivide);
                var targetMixColor = (mixTint + mixColor * colorVelocityDelta) / (1.0f + colorVelocityDelta);
                mr.material.SetColor("_Tint", targetColor);
                mr.material.SetColor("_MixTint", targetMixColor);
                mr.material.SetColor("_TopColor", targetMixColor + topColorOffset);
            }
        }
    }

    protected void ReleaseLiquid(float edgeDifference, Vector3 edgePositionWorld, float containerRotationRadians) {
        var centerToEdge = edgePositionWorld - this.transform.position;
        var edgeAngle = Mathf.Atan2(centerToEdge.x, centerToEdge.z);
        if (activeFlow == null) {
            activeFlow = Object.Instantiate(
                this.waterEmiter,
                edgePositionWorld,
                Quaternion.identity,
                this.transform.parent
            );
            flowEmissionRate = activeFlow.GetComponent<ParticleSystem>().emission.rateOverTime.constant;
            activeFlow.transform.rotation = Quaternion.AngleAxis((edgeAngle * 180.0f /  Mathf.PI), Vector3.up);
            flowTimer = Time.time;
        }
        else {
            var e = activeFlow.GetComponent<ParticleSystem>().emission;
            if (e.rateOverTime.constant == 0) {
            //  if (!activeFlow.activeSelf) {
                flowTimer = Time.time;
                activeFlow.SetActive(true);
                Debug.Log("Previous rate " + flowEmissionRate);
                e.rateOverTime = flowEmissionRate;;
                activeFlow.transform.rotation = Quaternion.AngleAxis((edgeAngle * 180.0f /  Mathf.PI) , Vector3.up);
             }
        }
        //activeFlow.transform.rotation = Quaternion.identity * Quaternion.AngleAxis(90, Vector3.right);

        const float liquidTransferConstant = 1f;
        var normalizedFillDifference = liquidTransferConstant * edgeDifference / (emptyValue - fullValue);
       
        var edgeLowesPosXZ = edgePositionWorld;
        edgeLowesPosXZ.y = 0;
        var posXZ = this.transform.position;
        posXZ.y = 0;

        var lenXZ = (edgeLowesPosXZ - posXZ).magnitude;
        var angleDiff = Mathf.Atan2(edgeDifference, lenXZ);

        // Debug.Log(" Angle Diff " + angleDiff * 180.0f / Mathf.PI   + " lenxz: " + lenXZ + " normdiff " + normalizedFillDifference +  " edge local " + centerToEdge + " edge angle " + edgeAngle * 180.0f / Mathf.PI);

        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        var currentFill = mr.material.GetFloat("_FillAmount");

        const float magicConstant = 1f;
        var newFill = currentFill + magicConstant * edgeDifference;

        var normalizedNew = (emptyValue - newFill) / (emptyValue - fullValue);

        normalizedCurrentFill = normalizedNew;

        var litersTransfer = normalizedFillDifference * containerMaxLiters;
        var newLiters = liquidVolumeLiters - litersTransfer;
        

        if (litersTransfer > liquidVolumeLiters) {
            //  Debug.Log("------- Liters transfer > than available liters " + litersTransfer);
            if (Mathf.Abs(containerRotationRadians) - 0.001f < (Mathf.PI / 2.0f)) {
                var ratio = containerRotationRadians / (Mathf.PI / 2.0f);
                litersTransfer = ratio * liquidVolumeLiters;
                // Debug.Log("-------- Aproximate transfer of " + litersTransfer + " for angle " + containerRotationRadians * 180.0f / Mathf.PI);
            }
            else {
                newFill = emptyValue + 1.0f;
                litersTransfer = liquidVolumeLiters;
            }
        }

        // If there is too little liquid left
        if (liquidVolumeLiters < 0.001f) {
            newFill = emptyValue + 1.0f;
            litersTransfer = liquidVolumeLiters;
        }

        RaycastHit hit;
        var fromRay  = this.getEdgeLowestPointWorldSpace(this.getUpVector(), true);
        var raycastTrue = Physics.Raycast(fromRay, Vector3.down, out hit, 100, Physics.AllLayers & ~LayerMask.GetMask("SimObjInVisible") & ~LayerMask.GetMask("Agent"));
        
        if (raycastTrue) {
            Debug.Log("Fluid transfer before to game object " + hit.collider.gameObject.name);
             Debug.DrawLine(fromRay, fromRay + (Vector3.down * hit.distance), Color.green,  2f);

            // TODO set lifetime
            var particleSystem = activeFlow.GetComponent<ParticleSystem>();
            //Time.deltaTime
            Debug.Log("***************** Curve max " + particleSystem.main.startSpeed.curveMultiplier);
            // particleSystem.Stop();
            var psMain = particleSystem.main;
            //psMain.duration = 4f;
            
            Debug.Log("Lifetime " + psMain.startLifetime.constant + " Distance "+ hit.distance + " velocity from gravity " + Physics.gravity.y * Time.fixedDeltaTime + " gravity raw" + Physics.gravity.y + " Time.fixedDeltaTime " + Time.fixedDeltaTime + " Deltat " + Time.deltaTime + " PS gravity mod " + psMain.gravityModifier.constant);
            var particleSpeed =  0.5f;
            var lifetimeConstant = 1f; 
            var b = 0.2f;
            // psMain.startLifetime = ((hit.distance + 0.01f) * lifetimeConstant) / particleSpeed - (hit.distance  * hit.distance) * b;
            // psMain.startLifetime = hit.distance / particleSystem.main.startSpeed.constant;
            // psMain.startLifetime = hit.distance / (psMain.gravityModifier.constant * -Physics.gravity.y * Time.fixedDeltaTime);

            var distance = hit.distance;
            // psMain.startLifetime = Mathf.Sqrt(2.0f * hit.distance / (psMain.gravityModifier.constant * Mathf.Abs(Physics.gravity.y)));
            // Debug.Log("Lifetime " + psMain.startLifetime.constant + " Distance "+ hit.distance + " velocity from gravity " + Physics.gravity.y * Time.fixedDeltaTime + " gravity raw" + Physics.gravity.y + " Time.fixedDeltaTime " + Time.fixedDeltaTime + " Deltat " + Time.deltaTime + " PS gravity mod " + psMain.gravityModifier.constant);
            // psMain.startLifetime = Mathf.Sqrt(10 / 49) * hit.distance;
            // psMain.duration = 50.0f;
            // particleSystem.Play();
            // psMain.startLifetime = hit.distance / particleSystem.main.startSpeed.constant;

            var averageColor = this.getAverageFlowColor();

            var ps = activeFlow.GetComponent<ParticleSystem>();
            if (ps) {
                var rend = ps.GetComponent<ParticleSystemRenderer>();
                var currCol = rend.material.GetColor("_Color");
                var diff = currCol - averageColor;
                if (Vector4.Dot(diff, diff) > 0.1) {
                    Debug.Log("Curr color of particles " + currCol.r + " " + currCol.g + " " + currCol.b + " " + currCol.a);
                    rend.material.SetColor("_Color", averageColor);
                }
                
            }

            var otherLiquidEdge = hit.collider.GetComponent<LiquidPourEdge>();
           
            if (otherLiquidEdge) {

                var otherWaterWorldPos = otherLiquidEdge.getWaterLevelPositionWorld();
                var edgeToReceiver =  edgePositionWorld - otherWaterWorldPos;

                // var distanceSquared = Vector3.Dot(edgeToReceiver, edgeToReceiver);

                distance = edgeToReceiver.magnitude;

                var mrOther = otherLiquidEdge.GetComponentInParent<MeshRenderer>();

                otherLiquidEdge.TransferLiquidVolume(litersTransfer, this, edgeDifference);
                if (mrOther) {
                    mrOther.enabled = true;
                }
            }
            else {
                this.LoseLiquidVolume(litersTransfer);
            }


            psMain.startLifetime = Mathf.Sqrt(2.0f * distance / (psMain.gravityModifier.constant * Mathf.Abs(Physics.gravity.y)));
            Debug.Log("Lifetime " + psMain.startLifetime.constant + " Distance "+ hit.distance + " velocity from gravity " + Physics.gravity.y * Time.fixedDeltaTime + " gravity raw" + Physics.gravity.y + " Time.fixedDeltaTime " + Time.fixedDeltaTime + " Deltat " + Time.deltaTime + " PS gravity mod " + psMain.gravityModifier.constant);
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


