using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

//NOTE: This fails to compile because it is calling some Setup scripts that are currently EDITOR define only
//for now, i'm also wrapping this script so it can only be used in editor until we have more time to investigate
#if UNITY_EDITOR

[ExecuteInEditMode]
public class FK_IK_Switch : MonoBehaviour
{
    public bool FKOrIKSwitch = false;
    bool switchFlipped = false;
    RotationConstraint[] rotationConstraints;

    void Start()
    {
        rotationConstraints = transform.GetComponentsInChildren<RotationConstraint>();
    }
    
    void Update()
    {
        if (switchFlipped != FKOrIKSwitch)
        {
            if (FKOrIKSwitch == false)
            {
                transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                transform.GetChild(1).GetChild(1).gameObject.SetActive(false);

                foreach (RotationConstraint rotationConstraint in rotationConstraints)
                {
                    rotationConstraint.enabled = true;
                }
            }

            else if (FKOrIKSwitch == true)
            {
                transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
                transform.GetChild(1).GetChild(1).gameObject.SetActive(true);

                foreach (RotationConstraint rotationConstraint in rotationConstraints)
                {
                    rotationConstraint.enabled = false;
                }
            }

            switchFlipped = FKOrIKSwitch;
        }
    }
}
#endif