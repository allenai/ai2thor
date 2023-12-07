using UnityEngine;

public class RobotArm : MonoBehaviour
{
    public Vector3 lastGoodPosition;

    
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