using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalSpawner : MonoBehaviour
{
 // If true Guarantees that other spawn planes under the same parent will have the same stencil value
    [SerializeField]
    private bool sameStencilAsSiblings = false;
    [SerializeField]
    private int stencilWriteValue = 1;
    [SerializeField]
    private GameObject[] decals = null;
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
        //breakType = BreakType.Decal;
        prevTime = Time.time;

        var mr = this.GetComponent<MeshRenderer>();
        if (mr && mr.enabled) {
            if (transparent) {
                if (!sameStencilAsSiblings) {
                    setStencilWriteValue(mr);
                } else {
                    var otherPlanes = this.transform.parent.gameObject.GetComponentsInChildren<DecalSpawner>();
                    // var otherPlanes = this.gameObject.GetComponentsInParent<DecalCollision>();
                    // Debug.Log("other planes id " + this.stencilWriteValue + " len " + otherPlanes.Length);
                    foreach (var spawnPlane in otherPlanes) {

                        if (spawnPlane.isActiveAndEnabled && spawnPlane.stencilSet && spawnPlane.sameStencilAsSiblings) {
                            this.stencilWriteValue = spawnPlane.stencilWriteValue;
                            this.stencilSet = true;
                            mr.material.SetInt("_StencilRef", this.stencilWriteValue);
                            // Debug.Log("Value for " + gameObject.name + " set to " + this.stencilWriteValue);
                            break;
                        }
                    }
                    if (!stencilSet) {
                        setStencilWriteValue(mr);
                    }
                }
            } else {
                this.stencilWriteValue = 1;
                mr.material.SetInt("_StencilRef", this.stencilWriteValue);
            }
        }
    }

    public void Update() {
        //test if the decals are spawning at all
        // if (Input.GetKeyDown(KeyCode.Space)) {
        //     SpawnDirt(200);
        // }
    }

    public void SpawnDirt(int howMany = 1, int randomSeed = 0) {
        //get spawn points for this 
        SimObjPhysics myParentSimObj = this.transform.GetComponentInParent<SimObjPhysics>();
        Vector3[] spawnPointsArray = myParentSimObj.FindMySpawnPointsFromTriggerBox().ToArray();

        float minX, maxX, minZ, maxZ;
        minX = spawnPointsArray[0].x;
        maxX = spawnPointsArray[0].x;
        minZ = spawnPointsArray[0].z;
        maxZ = spawnPointsArray[0].z;

        foreach (Vector3 v in spawnPointsArray) {
            if (v.x < minX) {
                minX = v.x;
            }

            if (v.x > maxX) {
                maxX = v.x;
            }

            if (v.z < minZ) {
                minZ = v.z;
            }

            if (v.z > maxZ) {
                maxZ = v.z;
            }
        }

        Random.InitState(randomSeed);

        for (int i = 0; i < howMany; i++) {
            //var randomPoint = Random.Range(0, spawnPointsArray.Length);
            var randomX = Random.Range(minX, maxX);
            var randomZ = Random.Range(minZ, maxZ);

            //generate random scale cause dirt
            var randomScale = new Vector3(Random.Range(0.1f, 0.4f), Random.Range(0.1f, 0.4f), 0.2f);
            spawnDecal(new Vector3(randomX, this.transform.position.y, randomZ) + this.transform.rotation * transparentDecalSpawnOffset
            , this.transform.rotation,
            randomScale, DecalRotationAxis.FORWARD);
        }
    }

    private void setStencilWriteValue(MeshRenderer mr) {
        DecalSpawner.currentStencilId = DecalSpawner.currentStencilId + 1;
        this.stencilWriteValue = DecalSpawner.currentStencilId << 1;
        if (this.stencilWriteValue > 0xFF) {
            this.stencilWriteValue = this.stencilWriteValue % 0xFF;
            // Debug.LogWarning("Stencil buffer write value overflow with: " + this.stencilWriteValue + " for " + this.gameObject.name + " wraping back to " + ", decal overlap with other spawn planes with same stencil value.");
        }
        mr.material.SetInt("_StencilRef", this.stencilWriteValue);
        // Debug.Log("Setting stencil for " +  this.gameObject.name + " write for shader to " + this.stencilWriteValue);
        this.stencilSet = true;
    }

    public void SpawnDecal(Vector3 position, bool worldSpace = false, Vector3? scale = null,  DecalRotationAxis randomRotationAxis = DecalRotationAxis.NONE) {

        var pos = position;
        if (worldSpace) {
            pos =  this.transform.InverseTransformPoint(position);
        }
        spawnDecal(pos, this.transform.rotation, scale.GetValueOrDefault(this.decalScale), randomRotationAxis);
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
            randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
        } else if (randomRotationAxis == DecalRotationAxis.SIDE) {
            randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.right);
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
    }
}
