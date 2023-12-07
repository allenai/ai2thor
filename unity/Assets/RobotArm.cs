using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotArmCollision : MonoBehaviour
{
public Vector3 lastGoodLocation = new Vector3();

    void FixedUpdate() {
        lastGoodLocation = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.isTrigger) return;
        if (collision.collider.gameObject.isStatic)
        {
            Debug.Log("Robot arm collided with static object: " + collision.collider.name);
            // Report collision event as needed
        }
    }
}