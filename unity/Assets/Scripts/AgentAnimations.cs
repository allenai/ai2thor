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

        collisionsInAction = new List<string>();

		//agent.LookUp(a);
        /*
        for (int i = 0; i < 11; i++) {
            GameObject newLine = Instantiate(gridLine, new Vector3(5, 0, -i + 0.5f), gridLine.transform.rotation);
			newLine.transform.localScale = new Vector3(10, newLine.transform.localScale.y, newLine.transform.localScale.z);
		}

        for (int i = 0; i < 11; i++) {
            GameObject newLine = Instantiate(gridLine, new Vector3(i + 0.5f, 0, -5),
				Quaternion.Euler(new Vector3(gridLine.transform.rotation.x, 90, gridLine.transform.rotation.z)));
			newLine.transform.localScale = new Vector3(10, newLine.transform.localScale.y, newLine.transform.localScale.z);
        }*/

		int GridSize = 25;

        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        var mesh = new Mesh();
        var verticies = new List<Vector3>();

        var indicies = new List<int>();
        for (int i = 0; i < GridSize; i++)
        {
            verticies.Add(new Vector3(i, 0, 0));
            verticies.Add(new Vector3(i, 0, GridSize));

            indicies.Add(4 * i + 0);
            indicies.Add(4 * i + 1);

            verticies.Add(new Vector3(0, 0, i));
            verticies.Add(new Vector3(GridSize, 0, i));

            indicies.Add(4 * i + 2);
            indicies.Add(4 * i + 3);
        }

        mesh.vertices = verticies.ToArray();
        mesh.SetIndices(indicies.ToArray(), MeshTopology.Lines, 0);
        filter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material.color = Color.white;

}

//int i = 0;

// Update is called once per frame
void Update()
    {
        if(yugi)
        {
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
        if(other.name == "YouveActivatedMyTrapCard" && yugi == false)
        {
            yugi = true;
            other.gameObject.SetActive(false);
        }
    }

}
