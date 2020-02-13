using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum DecalRotationAxis {
    NONE,
    FORWARD,
    SIDE
}

public class DecalCollision : Break
{
    // If true Guarantees that other spawn planes under the same parent will have the same stencil value
    [SerializeField]
    private bool sameStencilAsSiblings = false;
    [SerializeField]
    private int stencilWriteValue = 1;
    [SerializeField]
    private GameObject[] decals;
    [SerializeField]
    private float nextDecalWaitTimeSeconds = 1;
    [SerializeField]
    private Vector3 decalScale = new Vector3(0.3f, 0.3f, 0.2f);
    [SerializeField]
    private bool transparent = false;

    [SerializeField]

    protected bool stencilSet = false;
    // In local space
    private Vector3 transparentDecalSpawnOffset = new Vector3(0, 0, 0);
    private float prevTime;

    private static int currentStencilId = 0;

    
    
    void OnEnable() {
        breakType = BreakType.Decal;
        prevTime = Time.time;

        var mr = this.GetComponent<MeshRenderer>();
        if (mr && mr.enabled) {
            if (transparent) {
                if (!sameStencilAsSiblings) {
                    setStencilWriteValue(mr);
                }
                else {
                    var otherPlanes = this.transform.parent.gameObject.GetComponentsInChildren<DecalCollision>();
                    // var otherPlanes = this.gameObject.GetComponentsInParent<DecalCollision>();
                    //Debug.Log("other planes id " + this.stencilWriteValue + " len " + otherPlanes.Length);
                    foreach (var spawnPlane in otherPlanes) {
                       
                        if (spawnPlane.isActiveAndEnabled && spawnPlane.stencilSet && spawnPlane.sameStencilAsSiblings) {
                            this.stencilWriteValue = spawnPlane.stencilWriteValue;
                            this.stencilSet = true;
                            mr.material.SetInt("_StencilRef", this.stencilWriteValue);
                            //Debug.Log("Value for " + gameObject.name + " set to " + this.stencilWriteValue);
                            break;
                        }
                    }
                    if (!stencilSet) {
                         setStencilWriteValue(mr);
                    }
                }
            }
            else {
                this.stencilWriteValue = 1;
                mr.material.SetInt("_StencilRef", this.stencilWriteValue);
            }
        }

    }

    private void setStencilWriteValue(MeshRenderer mr) {
         DecalCollision.currentStencilId = DecalCollision.currentStencilId + 1;
        this.stencilWriteValue =  DecalCollision.currentStencilId << 1;
        if (this.stencilWriteValue > 0xFF) {
            this.stencilWriteValue = this.stencilWriteValue % 0xFF;
            //Debug.LogWarning("Stencil buffer write value overflow with: " + this.stencilWriteValue + " for " + this.gameObject.name + " wraping back to " + ", decal overlap with other spawn planes with same stencil value.");
        }
        mr.material.SetInt("_StencilRef", this.stencilWriteValue);
        //Debug.Log("Setting stencil for " +  this.gameObject.name + " write for shader to " + this.stencilWriteValue);
        this.stencilSet = true;
    }

    protected override void BreakForDecalType(Collision collision) {
        if (!transparent) {
            if (collision != null) {
                foreach (ContactPoint contact in collision.contacts)
                {
                    float newTime = Time.time;
                    float timeDiff = newTime - prevTime;
                    var scale = contact.otherCollider.bounds.size;
 
                    if (timeDiff > nextDecalWaitTimeSeconds) {
                        this.prevTime = Time.time;
            
                        // Taking into account the collider box of the object is breaking to resize the decal looks weirder than having the same decal size
                        // Maybe factor the other object size somehow but not directly, also first collider that hits somtimes has size 0 :(
                        // decalCopy.transform.localScale = scale + new Vector3(0.0f, 0.0f, 0.02f);

                        spawnDecal(contact.point, this.transform.rotation, decalScale, DecalRotationAxis.FORWARD);
                        break;
                    }
                }
            }
            else {
                spawnDecal(transform.position, this.transform.rotation, decalScale * 2);
            }
        }
        else {

            if (collision != null) {
                 foreach (ContactPoint contact in collision.contacts)
                {
                    Debug.Log("Decal pre for " + this.stencilWriteValue);
                    float newTime = Time.time;
                    float timeDiff = newTime - prevTime;
                    var scale = contact.otherCollider.bounds.size;

                    if (timeDiff > nextDecalWaitTimeSeconds) {
                        this.prevTime = Time.time;
            
                        // Taking into account the collider box of the object is breaking to resize the decal looks weirder than having the same decal size
                        // Maybe factor the other object size somehow but not directly, also first collider that hits somtimes has size 0 :(
                        // decalCopy.transform.localScale = scale + new Vector3(0.0f, 0.0f, 0.02f);

                        // Projects contact point on spawn plane
                        var planeToCollision = contact.point - this.transform.position;
                        var forwardNormalized = this.transform.forward.normalized;
                        var proyOnForward = Vector3.Dot(forwardNormalized, planeToCollision);
                        var proyectedPoint = contact.point - forwardNormalized * proyOnForward;
                        spawnDecal(proyectedPoint, this.transform.rotation, decalScale, DecalRotationAxis.FORWARD);
                        break;
                    }
                }
            }
            else {
                // Debug.Log("Spawn decal break " + this.transform.rotation + " final " + this.transform.rotation * Quaternion.Euler(-90, 0, 0));
                // spawnDecal(this.transform.position,  this.transform.rotation * Quaternion.Euler(-90, 0, 0), decalScale * 2);
                  spawnDecal(this.transform.position + this.transform.rotation * transparentDecalSpawnOffset, this.transform.rotation, this.transform.localScale); 
            }
        }
    }

    private void spawnDecal(Vector3 position, Quaternion rotation, Vector3 scale, DecalRotationAxis randomRotationAxis = DecalRotationAxis.NONE, int index = -1) {
        var minimumScale = this.transform.localScale;
        var decalScale = scale;
        if (minimumScale.x < scale.x || minimumScale.y < scale.y) {
            var minimumDim = Mathf.Min(minimumScale.x, minimumScale.y);
            decalScale = new Vector3(minimumDim, minimumDim, scale.z);
        }
        var selectIndex = index;
        if (index < 0) {
            selectIndex = Random.Range(0, decals.Length);
        }

        var randomRotation = Quaternion.identity;
        var randomAngle = Random.Range(-180.0f, 180.0f);
        if (randomRotationAxis == DecalRotationAxis.FORWARD) {
            randomRotation =  Quaternion.AngleAxis(randomAngle, Vector3.forward) ;
        }
        else if (randomRotationAxis == DecalRotationAxis.SIDE) {
            randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.right) ;
        }

        var decalCopy = Object.Instantiate(decals[selectIndex], position, rotation * randomRotation, this.transform.parent);
        decalCopy.transform.localScale = decalScale;
        
        var mr = decalCopy.GetComponent<MeshRenderer>();
        if (transparent && mr && mr.enabled) {
            mr.material.SetInt("_StencilRef", this.stencilWriteValue);
        }
        // Not needed if deffered decal prefab is correctly set with  _StencilRef to 1 in material
        else {
            var decal = decalCopy.GetComponent<DeferredDecal>();
            if (decal) {
                decal.material.SetInt("_StencilRef", this.stencilWriteValue);
            }
        }
        
        broken = true;
        readytobreak = true;
    }
}
