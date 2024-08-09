using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class CollisionListenerChild : MonoBehaviour
{
    // references to the joints of the mid level arm

    public CollisionListener parent;
    public Collider us;

    public void Start()
    {
        us = this.gameObject.GetComponent<Collider>();
    }

    public void OnDestroy()
    {
        if (parent != null)
        {
            parent.deregisterChild(this.GetComponent<Collider>());
        }
    }

    public void OnTriggerExit(Collider col)
    {
        if (parent)
        {
            parent.DeregisterCollision(this.us, col);
        }
    }

    public void OnTriggerStay(Collider col)
    {
#if UNITY_EDITOR
        if (!parent.externalColliderToInternalCollisions.ContainsKey(col))
        {
            if (col.gameObject.name == "StandardIslandHeight" || col.gameObject.name == "Sphere")
            {
                Debug.Log(
                    "got collision stay with "
                        + col.gameObject.name
                        + " this"
                        + this.gameObject.name
                );
            }
        }
#endif
        if (parent)
        {
            parent.RegisterCollision(this.us, col);
        }
    }

    public void OnTriggerEnter(Collider col)
    {
#if UNITY_EDITOR
        if (!parent.externalColliderToInternalCollisions.ContainsKey(col))
        {
            if (col.gameObject.name == "StandardIslandHeight" || col.gameObject.name == "Sphere")
            {
                Debug.Log(
                    "got collision enter with "
                        + col.gameObject.name
                        + " this"
                        + this.gameObject.name
                );
            }
        }
#endif
        // Debug.Log("got collision with " + col.gameObject.name + " this" + this.gameObject.name);
        if (parent)
        {
            parent.RegisterCollision(this.us, col);
        }
    }
}
