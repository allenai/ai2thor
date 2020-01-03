using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class SimpleSceneRandomObjectPositions : MonoBehaviour
{
    public GameObject[] prefabs;
    public GameObject robot;
    public GameObject objectsParent;

	// get the bounds of the scene
    public GameObject maxXWall;
    public GameObject maxZWall;
    public GameObject minXWall;
    public GameObject minZWall;

    // Start is called before the first frame update
    void Start()
    {
        int maxX = Mathf.FloorToInt(maxXWall.transform.position.x) - 1;
        int maxZ = Mathf.FloorToInt(maxZWall.transform.position.z) - 1;
        int minX = Mathf.CeilToInt(minXWall.transform.position.x) + 1;
        int minZ = Mathf.CeilToInt(minZWall.transform.position.z) + 1;

        // generate a list of all possible positions
        ArrayList combinations = new ArrayList();
        for (int i = minX; i <= maxX; i++) {
            for (int j = minZ; j <= maxZ; j++) {
                int[] arr = new int[] {i, j};
                combinations.Add(arr);
			}
		}

        // generate the random int positions
        ArrayList pairs = new ArrayList();
        System.Random rand = new System.Random();
        for (int i = 0; i < prefabs.Length + 1; i++) {
            int nextIdx = rand.Next(0, combinations.Count);
            pairs.Add(combinations[nextIdx]);
            combinations.RemoveAt(nextIdx);
		}

        foreach (GameObject prefab in prefabs) {
            int[] nextPos = (int[]) pairs[0];

            var newObject = Instantiate(prefab,
                new Vector3(nextPos[0], prefab.transform.position.y, nextPos[1]),
                Quaternion.Euler(new Vector3(prefab.transform.rotation.x, UnityEngine.Random.Range(0, 4) * 90, prefab.transform.rotation.z))
			);
            newObject.transform.parent = objectsParent.transform;
            pairs.RemoveAt(0);
		}
        int[] lastPos = (int[]) pairs[0];
        robot.transform.position = new Vector3(lastPos[0], robot.transform.position.y, lastPos[1]);
        robot.transform.rotation = Quaternion.Euler(new Vector3(robot.transform.rotation.x, UnityEngine.Random.Range(0, 4) * 90, robot.transform.rotation.z));

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
