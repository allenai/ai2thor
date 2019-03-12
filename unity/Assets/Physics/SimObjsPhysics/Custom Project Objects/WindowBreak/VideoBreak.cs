using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoBreak : MonoBehaviour 
{
	public Transform brokenWindow;
	public float power = 2.0f;
	public float explosionRadius = 1.0f;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	// void OnTriggerEnter(Collider col)
	// {
	// 	//if(col.GetComponentInParent<SimObjPhysics>().transform.name == "")
	// 	//{
	// 		Destroy(gameObject);
	// 		Instantiate(brokenWindow, transform.position, transform.rotation);
	// 	//}
	// }

	void OnCollisionEnter(Collision collision)
	{
		//print(collision.transform.name);
		if(collision.transform.name == "TeddyBear")
		return;
		
		Destroy(gameObject);
		Instantiate(brokenWindow, transform.position, transform.rotation);

		Vector3 explosionPos = transform.position;
		Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);

		foreach (Collider col in colliders)
		{
			if(col.GetComponent<Rigidbody>())
			{
				col.GetComponent<Rigidbody>().AddExplosionForce(power * collision.relativeVelocity.magnitude,explosionPos, explosionRadius, 1.0f);
			}
		}

	}
}
