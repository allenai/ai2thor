using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleArmCollidersWithZ : MonoBehaviour
{
    public Transform[] myColliders;
    public float initialZPos;
    public float scaleMultiplier = 1f;
    // Start is called before the first frame update
    void Start()
    {
        initialZPos = this.gameObject.transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        var currentZPos = this.gameObject.transform.position.z;

        foreach (Transform go in myColliders) {
            go.localScale = new Vector3(go.localScale.x, go.localScale.y, 1 + (initialZPos - currentZPos) * scaleMultiplier);
        }
    }
}
