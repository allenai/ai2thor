using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenDraw : MonoBehaviour
{
    public GameObject penDecal;
    public GameObject raycastOrigin;

    private bool shouldSpawn = true;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("DecalSpawnPlane") && shouldSpawn)
        {
            RaycastHit hit;
            //check if we hit the spawn plane below the pencil
            if (
                Physics.Raycast(
                    raycastOrigin.transform.position,
                    raycastOrigin.transform.forward,
                    out hit,
                    Mathf.Infinity,
                    LayerMask.GetMask("Default")
                )
            )
            {
                //Debug.DrawRay(hit.point, Vector3.up * 10, Color.red);

                //check if we hit another pen mark, if so don't place anything because its too close
                if (hit.collider.tag == "Pen")
                {
                    return;
                }
                //ok so if its not a pen mark, that means we hit a dirt mark which means we can spawn on the table
                else
                {
                    if (
                        Physics.Raycast(
                            raycastOrigin.transform.position,
                            raycastOrigin.transform.forward,
                            out hit,
                            Mathf.Infinity,
                            LayerMask.GetMask("SimObjVisible")
                        )
                    )
                    {
                        Object.Instantiate(penDecal, hit.point, Quaternion.Euler(-90, 0, 0));
                    }
                }
            }
            else
            {
                if (
                    Physics.Raycast(
                        raycastOrigin.transform.position,
                        raycastOrigin.transform.forward,
                        out hit,
                        Mathf.Infinity,
                        LayerMask.GetMask("SimObjVisible")
                    )
                )
                {
                    Object.Instantiate(penDecal, hit.point, Quaternion.Euler(-90, 0, 0));
                }
            }
        }
    }
}
