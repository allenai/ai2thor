using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalCollision : Break
{
    [SerializeField]
    private int stencilWriteValue = 1;
    [SerializeField]
    private GameObject[] decals;
    [SerializeField]
    private float nextDecalWaitTimeSeconds = 1;
    [SerializeField]
    private Vector3 decalScale = new Vector3(0.3f, 0.3f, 0.2f);
    [SerializeField]
    private float minimumImpulseMagnitudeForDecal = 10;
    [SerializeField]
    private bool transparent = false;

    [SerializeField]
    // In local space
    private Vector3 transparentDecalSpawnOffset = new Vector3(0, 0, 0);
    private float prevTime;

    private static int currentStencilId = 0;

    private System.Random random;
    
    void OnEnable() {
        breakType = BreakType.Decal;
        prevTime = Time.time;
        random = new System.Random();

        var mr = this.GetComponent<MeshRenderer>();
        if (mr && mr.enabled) {
            DecalCollision.currentStencilId = DecalCollision.currentStencilId + 1;
            this.stencilWriteValue =  DecalCollision.currentStencilId << 1;
            mr.material.SetInt("_StencilRef", this.stencilWriteValue);
            Debug.Log("Setting stencil write for shader to " + this.stencilWriteValue);
        }
    }

    protected override void BreakForDecalType(Collision collision) {
        if (!transparent) {
            if (collision != null) {
                foreach (ContactPoint contact in collision.contacts)
                {
                    float newTime = Time.time;
                    float timeDiff = newTime - prevTime;
                    var scale = contact.otherCollider.bounds.size;
                    // unused for now
                    var comp = scale.sqrMagnitude > 0.0f;

                    var comp1 = timeDiff > nextDecalWaitTimeSeconds;

                    if (timeDiff > nextDecalWaitTimeSeconds) {
                        this.prevTime = Time.time;
            
                        // Taking into account the collider box of the object is breaking to resize the decal looks weirder than having the same decal size
                        // Maybe factor the other object size somehow but not directly, also first collider that hits somtimes has size 0 :(
                        // decalCopy.transform.localScale = scale + new Vector3(0.0f, 0.0f, 0.02f);
               
                        // TODO: remove quaternion multiplication by -90 when we get a z oriented simole plane
                        spawnDecal(contact.point, this.transform.rotation, decalScale);
                        break;
                    }
                }
            }
            else {
                spawnDecal(transform.position, this.transform.rotation, decalScale * 2);
            }
        }
        else {
                 foreach (ContactPoint contact in collision.contacts)
                {
                    Debug.Log("Decal pre for " + this.stencilWriteValue);
                    float newTime = Time.time;
                    float timeDiff = newTime - prevTime;
                    var scale = contact.otherCollider.bounds.size;
                    // unused for now
                    var comp = scale.sqrMagnitude > 0.0f;

                    var comp1 = timeDiff > nextDecalWaitTimeSeconds;

                    if (timeDiff > nextDecalWaitTimeSeconds) {
                        Debug.Log("Decal spawn for " + this.stencilWriteValue);
                        this.prevTime = Time.time;
            
                        // Taking into account the collider box of the object is breaking to resize the decal looks weirder than having the same decal size
                        // Maybe factor the other object size somehow but not directly, also first collider that hits somtimes has size 0 :(
                        // decalCopy.transform.localScale = scale + new Vector3(0.0f, 0.0f, 0.02f);
               
                        // TODO: remove quaternion multiplication by -90 when we get a z oriented simole plane
                        spawnDecal(contact.point, this.transform.rotation, decalScale);
                        break;
                    }
                }
                // spawnDecal(this.transform.position + this.transform.rotation * transparentDecalSpawnOffset, this.transform.rotation, this.transform.localScale); 
        }
    }

    private void spawnDecal(Vector3 position, Quaternion rotation, Vector3 scale, int index = -1) {

        var selectIndex = index;
        if (index < 0) {
            selectIndex = random.Next(0, decals.Length);
        }
        var decalCopy = Object.Instantiate(decals[selectIndex], position, rotation, this.transform.parent);
        decalCopy.transform.localScale = scale;
        var mr = decalCopy.GetComponent<MeshRenderer>();
        if (mr && mr.enabled) {
            mr.material.SetInt("_StencilRef", this.stencilWriteValue);
            Debug.Log("Setting stencil write for deca; shader to " + this.stencilWriteValue);
        
        }
        broken = true;
        readytobreak = true;
    }
}
