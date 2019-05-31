using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalCollision : Break
{
    [SerializeField]
    private GameObject[] decals;
    [SerializeField]
    private float nextDecalWaitTimeSeconds = 1;
    [SerializeField]
    private Vector3 decalScale = new Vector3(0.3f, 0.3f, 0.2f);
    [SerializeField]
    private float minimumImpulseMagnitudeForDecal = 10;
    private float prevTime;

    private System.Random random;
    
    void OnEnable() {
        breakType = BreakType.Decal;
        prevTime = Time.time;
        random = new System.Random();
    }

    protected override void BreakForDecalType(Collision collision) {
        if (collision != null) {
            foreach (ContactPoint contact in collision.contacts)
            {
                float newTime = Time.time;
                float timeDiff = newTime - prevTime;
                var scale = contact.otherCollider.bounds.size;
                // unused for now
                var comp = scale.sqrMagnitude > 0.0f;

                var comp1 = timeDiff > nextDecalWaitTimeSeconds;

                Debug.Log(collision.impulse.sqrMagnitude + " size " + scale);
                if (timeDiff > nextDecalWaitTimeSeconds) {
                    this.prevTime = Time.time;
                    var index = random.Next(0, decals.Length);
                    var decalCopy = Object.Instantiate(decals[index], contact.point, new Quaternion(), this.transform.parent);
                
                    // Taking into account the collider box of the object is breaking to resize the decal looks weirder than having the same decal size
                    // Maybe factor the other object size somehow but not directly, also first collider that hits somtimes has size 0 :(
                    // decalCopy.transform.localScale = scale + new Vector3(0.0f, 0.0f, 0.02f);
                    decalCopy.transform.localScale = decalScale;
                    broken = true;
                    readytobreak = true;
                    break;
                }
            }
        }
        else {
            var index = random.Next(0, decals.Length);
            var decalCopy = Object.Instantiate(decals[index], transform.position, new Quaternion(), this.transform.parent);
            decalCopy.transform.localScale = decalScale * 2;
            broken = true;
            readytobreak = true;
        }
    }
}
