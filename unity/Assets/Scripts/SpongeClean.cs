using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpongeClean : MonoBehaviour {
    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    public void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Dirt") || other.CompareTag("Pen")) {
            //other.transform.position = other.transform.position + new Vector3(0, 1.0f, 0);
            Destroy(other.transform.gameObject);
        }
    }
}
