using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class AgentAnimations : PhysicsRemoteFPSAgentController
{

    [SerializeField] private PhysicsRemoteFPSAgentController agent;
    [SerializeField] private ServerAction a;
	public GameObject gridLine;

    public bool yugi = false;

    // Start is called before the first frame update
    void Start()
    {
		a = new ServerAction();
		a.moveMagnitude = 0.1f;
		gridSize = 0.01f;
        a.rotateDegrees = 5.0f;
        //agent.LookUp(a);
        for (int i = 0; i <= 10; i++) {
            GameObject newLine = Instantiate(gridLine, new Vector3(5, 0, -i), gridLine.transform.rotation);
			//newLine.transform.localScale = new Vector3(11, newLine.transform.localScale.y, newLine.transform.localScale.z);
			//newLine.transform.localScale = new Vector3(11, newLine.transform.localScale.y, newLine.transform.localScale.z);
			newLine.transform.localScale = new Vector3(newLine.transform.localScale.x, 5, newLine.transform.localScale.z);
		}

        for (int i = 0; i <= 10; i++) {
            GameObject newLine = Instantiate(gridLine, new Vector3(i, 0, -5),
				Quaternion.Euler(new Vector3(90, 0, 0)));
			//newLine.transform.localScale = new Vector3(11, newLine.transform.localScale.y, newLine.transform.localScale.z);
			newLine.transform.localScale = new Vector3(newLine.transform.localScale.x, 5, newLine.transform.localScale.z);
        }

    }

    //int i = 0;

    // Update is called once per frame
    void Update()
    {
        if (yugi) {
            Vector3 moveDirection = Vector3.zero;
            moveDirection.y -= (-Physics.gravity.y * Time.deltaTime * m_GravityMultiplier);

            m_CharacterController.Move(moveDirection * Time.deltaTime);
        }
        //a.rotateDegrees = a.rotateDegrees + 5.0f;
        //Debug.Log(a.rotateDegrees);
        //agent.RotateDegrees(a);
        //agent.actionComplete = false;
        //if (i == 5) {
        //MA();
        //}
        //if (i < 10) {
        //agent.actionComplete = false;
        //agent.MoveAhead(a);
        //}
        //i++;
        //agent.actionComplete = false;
        //agent.MoveAhead(a);
        //agent.MoveAhead(a);
        //agent.actionComplete = false;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.name == "YouveActivatedMyTrapCard" && yugi == false)
        {
            yugi = true;
            MetadataWrapper.inPitt = true;
            other.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "apple_1") {
            Destroy(other.gameObject);
            MetadataWrapper.capturedApple = true;
        }
    }

}
