using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Break : MonoBehaviour
{

    [SerializeField]
    private GameObject PrefabToSwapTo;
    protected bool readytobreak = true;

    public List<float> ImpulseForce;

    // protected Vector3 LastPosition;
    // protected Vector3 CurrentPosition;

    // protected Vector3 LastVelocity;
    // protected Vector3 CurrentVelocity;

    // public List<float> PositionDifferences;

    // public List<float> VelocityMagnitudeDifferences;


    // Start is called before the first frame update
    void Start()
    {
        // LastPosition = gameObject.transform.position;

        // PositionDifferences.Add(0.0f);

        // LastVelocity = Vector3.zero;
        // VelocityMagnitudeDifferences.Add(0.0f);
    }

    void FixedUpdate()
    {
        // CurrentPosition = gameObject.transform.position;

        // float difference = Mathf.Abs(CurrentPosition.magnitude - LastPosition.magnitude);

        // //print(PositionDifferences.Count);
        // if(difference >= PositionDifferences[PositionDifferences.Count - 1])
        // PositionDifferences.Add(difference);

        // LastPosition = CurrentPosition;

        // /////////

        // Rigidbody rb = gameObject.GetComponent<Rigidbody>();

        // CurrentVelocity = rb.velocity;

        // float vdiff = Mathf.Abs(CurrentVelocity.magnitude - LastVelocity.magnitude);

        // if(vdiff >= VelocityMagnitudeDifferences[VelocityMagnitudeDifferences.Count - 1])
        // {
        //     VelocityMagnitudeDifferences.Add(vdiff);
        // }
        // LastVelocity = CurrentVelocity;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.C))
        {
            BreakObject();
        }
    }

    public void BreakObject()
    {
        Destroy(gameObject);
        GameObject pieces = Instantiate(PrefabToSwapTo, transform.position, transform.rotation);
    }

    void OnCollisionEnter(Collision col)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();

        ImpulseForce.Add(col.impulse.magnitude);
        if(col.impulse.magnitude > 4.0f && !col.transform.GetComponentInParent<PhysicsRemoteFPSAgentController>())
        {
            if(readytobreak)
            {
                readytobreak = false;
                BreakObject();
            }
        }
    }
}
