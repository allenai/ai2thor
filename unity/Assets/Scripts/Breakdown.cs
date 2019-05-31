using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this logic is for controlling how pieces shatter after spawning in a broken version of a sim object (mug, plate, etc)
public class Breakdown : MonoBehaviour
{
    public float power = 10.0f;
    protected float explosionRadius = 0.25f;
    
    // Start is called before the first frame update
    void Start()
    {
      Vector3 explosionPos = transform.position;
      Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);

      foreach (Collider col in colliders)
      {
        if(col.GetComponent<Rigidbody>())
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();            
            rb.AddExplosionForce(power, gameObject.transform.position, explosionRadius, 0.005f);
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value));
        }
      }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
