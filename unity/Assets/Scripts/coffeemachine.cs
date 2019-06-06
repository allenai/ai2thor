using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class coffeemachine : MonoBehaviour
{
    private ObjectSpecificReceptacle osr;
    private CanToggleOnOff onOff;

    // Start is called before the first frame update
    void Start()
    {
		osr = gameObject.GetComponent<ObjectSpecificReceptacle>();
		onOff = gameObject.GetComponent<CanToggleOnOff>();
    }

    // Update is called once per frame
    void Update()
    {
        Serve();
    }

    public void Serve()
    {
		SimObjPhysics target;

		if(osr.attachPoint.transform.GetComponentInChildren<SimObjPhysics>() && onOff.isTurnedOnOrOff())
		{
			target = osr.attachPoint.transform.GetComponentInChildren<SimObjPhysics>();
			Fill f = target.GetComponent<Fill>();

			//if not already toasted, toast it!
			if(!f.IsFilled())
			{
				f.FillObject("coffee");
			}
		}
    }
}
