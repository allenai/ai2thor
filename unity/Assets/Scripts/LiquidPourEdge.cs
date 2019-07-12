using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidPourEdge : MonoBehaviour
{
    public float radius = 1.0f;
    public float radiusRaycastOffset = 0.03f;
    public float threshold = 1e-4f;
    public Mesh debugQuad = null;

    public bool renderDebugLevelPlane = false;

    public GameObject waterEmiter = null;

    public float emptyValue = 0.6f;

    public float fullValue = 0.4f;

    public float normalizedCurrentFill = 0.5f;

    public float liquidVolumeLiters = 0f; 

    public float containerMaxVolumeLiters = 1f;

    public float shaderFill = 0.0f;
    private GameObject activeFlow = null;

    private Wobble wobbleComponent = null;

    private float minY;
    private float maxY;
    
    // Start is called before the first frame update
    void Start()
    {
        wobbleComponent = this.GetComponentInParent<Wobble>();

        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        normalizedCurrentFill = liquidVolumeLiters / containerMaxVolumeLiters;
        // shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        // float minY, maxY;
        this.getWaterLevelPositionWorld(out shaderFill, out minY, out maxY);

        mr.material.SetFloat("_FillAmount", shaderFill);
        
    }

    void SetFillAmount(float normalizedFill) {
        normalizedCurrentFill = normalizedFill;
        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        mr.material.SetFloat("_FillAmount", shaderFill);
    }

    void SetFillAmountLiters(float liters) {
        normalizedCurrentFill = liters / containerMaxVolumeLiters;
        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        //shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        shaderFill = GetShaderFill(this.minY, this.maxY);
        mr.material.SetFloat("_FillAmount", shaderFill);
    }

    void SetShaderFill(float shaderFillValue) {
        shaderFill = shaderFillValue;
        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        mr.material.SetFloat("_FillAmount", shaderFillValue);
    }

    void TransferLiquid(float normalizedDelta, LiquidPourEdge transferer) {
        // TODO: Calculate correctly how much this gets filled based on the transferer properties
        Debug.Log("Transfering liquid from " +  this.gameObject.name + " to  " + transferer.gameObject.name + " amount " + normalizedDelta);
        this.SetFillAmount(normalizedCurrentFill + normalizedDelta);
    }

     void TransferLiquidVolume(float liters, LiquidPourEdge transferer) {
        // TODO: Calculate correctly how much this gets filled based on the transferer properties, clamp

        transferer.liquidVolumeLiters -= liters;
        transferer.normalizedCurrentFill = transferer.liquidVolumeLiters / transferer.containerMaxVolumeLiters;
        transferer.SetFillAmountLiters(transferer.liquidVolumeLiters);

        this.liquidVolumeLiters += liters;
        Debug.Log("Transfering liquid volume from " +  this.gameObject.name + " to  " + transferer.gameObject.name + " amount " + liters);

        this.normalizedCurrentFill = this.liquidVolumeLiters / this.containerMaxVolumeLiters;
        this.SetFillAmountLiters(this.liquidVolumeLiters);
        //this.SetFillAmount(normalizedCurrentFill);
    }

    // Update is called once per frame
    void Update()
    {

        // var mr = this.transform.GetComponentInParent<MeshRenderer>();
        // shaderFill = emptyValue - (emptyValue - fullValue) * (normalizedCurrentFill);
        // Debug.Log("fullValue " + fullValue + ", emptyValue ," + emptyValue + " normalizedCurrentFill " + normalizedCurrentFill);
        // mr.material.SetFloat("_FillAmount", shaderFill);

        // TODO: maybe move to late update

        var up = this.transform.parent.up;
        var edgeLowestWorld = getLowestEdgePointWorld(up);
        float shaderFillValue;
        var waterLevelWorld = getWaterLevelPositionWorld(out shaderFillValue, out this.minY, out this.maxY);

        var edgeLiquidDifference = waterLevelWorld.y - edgeLowestWorld.y;
        if (edgeLiquidDifference > 0) {
            Debug.Log("Release liquid " + edgeLiquidDifference  + " water level  " + waterLevelWorld.y + " cup edge lowest "+ edgeLowestWorld.y);
            ReleaseLiquid(edgeLiquidDifference, edgeLowestWorld, shaderFillValue, minY, maxY);
        }
        else if (activeFlow != null)
        {
            // Make flow smaller do not turn off
            activeFlow.SetActive(false);

           
        }
       
        if (edgeLiquidDifference <= 0 && Mathf.Abs(shaderFillValue - shaderFill) >= 0.0001f)  {
        
            var m = Mathf.Abs(shaderFillValue - shaderFill) >= 0.0001f;
            Debug.Log("Previous shader val " + shaderFill + " new " + shaderFillValue);     
            Debug.Log("Condition " + m );
             shaderFill = shaderFillValue;

            var mr = GetComponentInParent<MeshRenderer>();

            //this.getWaterLevelPositionWorld(out shaderFill, out minY, out maxY);

            if (mr) {
                mr.material.SetFloat("_FillAmount", shaderFillValue);
            }
            
            
            // if (mr) {
            //      mr.material.SetFloat("_FillAmount", shaderFillValue);
            // }
        }
        
    }

    protected void ReleaseLiquid(float edgeDifference, Vector3 edgePositionWorld, float shaderFillValue, float minY, float maxY) {
        //Debug.Log("Fluid out!!!! " + edgeDifference);
        if (activeFlow == null) {
            activeFlow = Object.Instantiate(this.waterEmiter, edgePositionWorld, Quaternion.identity, this.transform.parent);
        }
        else {
            activeFlow.SetActive(true);
        }


        

        // var normalizedFillDifference = edgeDifference / (emptyValue - fullValue);
        // var normalizedNew = (emptyValue - edgeDifference) / (emptyValue - fullValue);
       

        var mr = this.transform.GetComponentInParent<MeshRenderer>();
        var currentFill = mr.material.GetFloat("_FillAmount");

        const float magicConstant = 1f;
        var newFill = currentFill + magicConstant * edgeDifference;

        // var originOffset =  this.transform.parent.position.y - minY;
        var fillRatio = GetLitersFromShaderFill(edgeDifference, minY, maxY);
        // var litersDeltaTransfer = containerMaxVolumeLiters * fillRatio;

        var litersDeltaTransfer = containerMaxVolumeLiters * fillRatio;


        // var normalizedNew = (litersDeltaTransfer + liquidVolumeLiters) / containerMaxVolumeLiters;

        // normalizedCurrentFill = normalizedNew;

       
        

        // var mr = this.transform.GetComponentInParent<MeshRenderer>();
        // var currentFill = mr.material.GetFloat("_FillAmount");

        // const float magicConstant = 1f;
        // var newFill = currentFill + magicConstant * edgeDifference;

        // var normalizedNew = (emptyValue - newFill) / (emptyValue - fullValue);

        // normalizedCurrentFill = normalizedNew;



        // var newCurrentNormalizedFill = (emptyValue - currentFill) / (emptyValue - fullValue);


        

        //LayerMask.GetMask("SimObjVisible"); 
        RaycastHit hit;

        var fromRay  = this.getLowestEdgePointWorld(this.getUpVector(), true);
        var raycastTrue = Physics.Raycast(fromRay, Vector3.down, out hit, 100, Physics.AllLayers);

        Debug.DrawRay(fromRay, Vector3.down, Color.green, 2f);
        if (raycastTrue) {
             Debug.Log("Fluid transfer before to game object " + hit.collider.gameObject.name);
            var otherLiquidEdge = hit.collider.GetComponent<LiquidPourEdge>();
           
            if (otherLiquidEdge) {
                var mrOther = otherLiquidEdge.GetComponentInParent<MeshRenderer>();

                //otherLiquidEdge.TransferLiquid(normalizedFillDifference, this);

                otherLiquidEdge.TransferLiquidVolume(litersDeltaTransfer, this);

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
        }
        else {
            Debug.Log("Raycast fail");
        }


        // TODO Clamp on liter value
        mr.material.SetFloat("_FillAmount", newFill);
        
    }

    private IEnumerator DecrementWaterValue(float waitTime, MeshRenderer mr, float newFill)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);
            print("WaitAndPrint " + Time.time);
            mr.material.SetFloat("_FillAmount", newFill);
        }
    }

    private Vector3 getUpVector() {
        var up = this.transform.parent.up;
        // if (wobbleComponent == null) {
        //     wobbleComponent = this.transform.GetComponentInParent<Wobble>();
        // }

        
        // var wobbleRot = Quaternion.Euler(wobbleComponent.wobbleAmountX, 0, wobbleComponent.wobbleAmountZ);
        // return wobbleRot * up;

        return up;
    }

    protected Vector3 getLowestEdgePointWorld(Vector3 up, bool withOffset = false) {
        var upXZ = new Vector3(up.x, 0, up.z);

        var parentRot = this.transform.parent.rotation;
        parentRot.x = 0;
        parentRot.z = 0;

        // Quat
        
        // upXZ = Quaternion.AngleAxis(-this.transform.parent.eulerAngles.y, Vector3.up) * upXZ.normalized;
        upXZ = Quaternion.Inverse(parentRot).normalized * upXZ.normalized;
        // Debug.Log("up xz " + upXZ);
        // Debug.Log("Local Pos " + this.transform.localPosition);
        var calculatedRadius = withOffset ? this.radius + this.radiusRaycastOffset : this.radius;
        var circleLowestLocal = Vector3.zero + calculatedRadius * upXZ;
        // var circleLowestWorld = this.transform.TransformPoint(circleLowestLocal);
        var circleLowestWorld = this.transform.TransformPoint(circleLowestLocal);

        return circleLowestWorld;
    }

    protected Vector3 getWaterLevelPositionWorld() {
        float shaderFillv;
        float minYv;
        float maxYv;
        return getWaterLevelPositionWorld(out shaderFillv, out minYv, out maxYv);
    }

    protected Vector3 getWaterLevelPositionWorld(out float shaderFillValue, out float minY, out float maxY) {
        var mr = this.transform.GetComponentInParent<MeshRenderer>();

        var visibilityPoints = this.transform.parent.parent.Find("VisibilityPoints");
        minY = float.MaxValue;
        maxY = float.MinValue;
        for (int i = 0; i <  visibilityPoints.childCount; i++) {
            var point = visibilityPoints.GetChild(i);
            minY = point.position.y < minY ? point.position.y : minY;
            maxY = point.position.y > minY ? point.position.y : maxY;
        }

        // var fillRatio = liquidVolumeLiters / containerMaxVolumeLiters;
        
        // //this.parent.position

        // var yRelative = (maxY - minY) * fillRatio;

        // var originOffset =  this.transform.parent.position.y - minY;

        // shaderFillValue = originOffset - yRelative + 0.5f;

        var pos = this.transform.parent.position;

        shaderFillValue = GetShaderFill(minY, maxY);

        pos.y += 0.5f - shaderFillValue;
        return pos;



        // if (mr != null) {
        //     var fillAmount = mr.material.GetFloat("_FillAmount");

        //     Gizmos.color = new Color(1f, 1f, 0.0f, 0.7f);

        //     var pos = this.transform.parent.position;
        //     pos.y += 0.5f - fillAmount;
        //     return pos;
        // }
        // else {
        //     Debug.LogError("No mesh renderer, with liquid material to get fill value");
        // }
        // return Vector3.zero;
    }

    // private getModelYBounds(out float minY, out float minY) {

    // }

    private float GetShaderFill(float minY, float maxY) {
        var fillRatio = liquidVolumeLiters / containerMaxVolumeLiters;
        var yRelative = (maxY - minY) * fillRatio;
        var originOffset =  this.transform.parent.position.y - minY;
        return originOffset - yRelative + 0.5f;
    }

    private float GetLitersFromShaderFill(float shaderFillValue, float minY, float maxY) {
        var originOffset =  this.transform.parent.position.y - minY;
        return (originOffset -  shaderFillValue) / (maxY - minY);
    }

    void OnDrawGizmos() {
       
        UnityEditor.Handles.color  = Color.red;

        var up =  getUpVector();
        
        UnityEditor.Handles.DrawWireDisc(this.transform.position, up, this.radius);

        // UnityEditor.Handles.color  = new Color(1.0f, 0.1f, 0.1f, 0.4f);

        UnityEditor.Handles.color  = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(this.transform.position, up, this.radius + this.radiusRaycastOffset);

        var circleLowestWorld = getLowestEdgePointWorld(up);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(circleLowestWorld, radius / 10.0f);


        var circleLowestWorldWithOffset = getLowestEdgePointWorld(up, true);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(circleLowestWorldWithOffset, radius / 10.0f);

        Gizmos.color = new Color(1f, 1f, 0.0f, 0.7f);

        if (renderDebugLevelPlane) {
            var pos = getWaterLevelPositionWorld();

            var rot = Quaternion.identity;
            if (wobbleComponent == null) {
                wobbleComponent = this.transform.GetComponentInParent<Wobble>();
            }

            //rot = Quaternion.Euler(-wobbleComponent.wobbleAmountX * 360, 0, -wobbleComponent.wobbleAmountZ * 360); 
               // Debug.Log("Wobble " + wobbleComponent.wobbleAmountX + wobbleComponent.wobbleAmountZ )

            Gizmos.DrawMesh(this.debugQuad, pos, Quaternion.Euler(90, 0, 0) * rot, new Vector3(0.5f, 0.5f, 0.5f));
        }

        // Gizmos.color = new Color(1f, 0f, 0.0f, 0.5f);
        // var bounds = this.GetComponentInParent<MeshRenderer>().bounds;
        // Gizmos.DrawCube(this.transform.parent.position, bounds.size);


    }
}
