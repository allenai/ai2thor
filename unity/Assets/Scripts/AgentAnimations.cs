using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class AgentAnimations : MonoBehaviour
{

	public PhysicsRemoteFPSAgentController agent;

	// Start is called before the first frame update
	void Start()
    {
		Debug.Log(agent);
		Debug.Log(agent.sceneBounds);
		Debug.Log(agent.AgentHandLocation());
    }

    // Update is called once per frame
    void Update()
    {
	    transform.Translate(Vector3.forward * Time.deltaTime * 0.2f);
        if (((int) Time.deltaTime ) % 4 == 0) {
			//agent.AgentHandLocations();
		}
    }
}
