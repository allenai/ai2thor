using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenDraw : MonoBehaviour
{
    public GameObject penDecal;
    public GameObject raycastOrigin;

    private bool shouldSpawn = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other) {

        if (other.CompareTag("DecalSpawnPlane") && shouldSpawn) {
            RaycastHit hit;
            //check if we hit the spawn plane below the pencil
            if (Physics.Raycast(raycastOrigin.transform.position, raycastOrigin.transform.forward, out hit, Mathf.Infinity, LayerMask.GetMask("SimObjVisible", "Default"))) {
                Debug.DrawRay(hit.point, Vector3.up * 10, Color.red);

                if (hit.collider.tag == "Dirt") {
                    return;
                } else {
                    Object.Instantiate(penDecal, hit.point, Quaternion.Euler(-90, 0, 0));
                }
            }
        }
    }
}
