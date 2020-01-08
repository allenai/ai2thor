using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class SimpleSceneRandomObjectPositions : MonoBehaviour
{
    public GameObject apple;
    public GameObject trap;
    public GameObject interiorWall;
    public GameObject robot;
    public GameObject objectsParent;

    public GameObject floor;

    private GameObject appleInstance;

    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(42);
        int minX = 1;
        int minZ = -10;
        int maxX = 10;
        int maxZ = -1;

        // currently only 1 interior wall since with multiple it may
        // make it impossible to reach the apple in a scene
        // (maybe replace interiorWall with multiple wall panels?)
        GameObject[] objects = { apple, trap, interiorWall, robot };

        // generate a list of all possible positions
        ArrayList combinations = new ArrayList();
        for (int i = minX; i <= maxX; i++) {
            for (int j = minZ; j <= maxZ; j++) {
                int[] arr = {i, j};
                combinations.Add(arr);
			}
		}

        // generate the random int positions
        ArrayList pairs = new ArrayList();
        System.Random rand = new System.Random(47);
        //System.Random rand = new System.Random();
        for (int i = 0; i < objects.Length; i++) {
            int nextIdx = (int) rand.Next(0, combinations.Count);
            pairs.Add(combinations[nextIdx]);
            combinations.RemoveAt(nextIdx);
		}

        // set each object to a random, unique grid position
        foreach (GameObject prefab in objects) {
            // extracts the {x, z} position
            int[] nextPos = (int[]) pairs[0];

            if (prefab == robot) {
                // don't add the robot object, since it's already in the scene
                prefab.transform.position = new Vector3(nextPos[0] - 0.5f, prefab.transform.position.y, nextPos[1] + 0.5f);
                prefab.transform.localRotation = Quaternion.Euler(new Vector3(prefab.transform.rotation.x, UnityEngine.Random.Range(0, 4) * 90, prefab.transform.rotation.z));
            } else {
                // adds the prefab to the scene
                // uses delta 0.5f to center the object in the grid cell
                var newObject = Instantiate(prefab,
				    new Vector3(nextPos[0] - 0.5f, prefab.transform.position.y, nextPos[1] + 0.5f),
					Quaternion.Euler(new Vector3(prefab.transform.rotation.x, UnityEngine.Random.Range(0, 4) * 90, prefab.transform.rotation.z))
			    );

                // sets the parent of the prefab
                newObject.transform.parent = objectsParent.transform;

                if (prefab == apple) {
                    appleInstance = newObject;
				}

                if (prefab == trap) {
                    // destroy the floor piece covering the trap
                    // hard coded in solution based on how the floor's children are ordered
                    Destroy(floor.transform.GetChild(
						(nextPos[0] + 1) * 12 - 2 - (10 - Math.Abs(nextPos[1]))).gameObject
					);
                }
            }

			// moves to the next random pair
            pairs.RemoveAt(0);
		}
    }

    int frame = 1;
    // Update is called once per frame
    void Update()
    {
        float speed = 0.1f;
        appleInstance.transform.position = new Vector3(
			appleInstance.transform.position.x,
			1.25f + 0.2f * (float) Math.Sin(speed * frame),
			appleInstance.transform.position.z);
        frame++;
        //appleInstance.transform.position = new Vector3(0, 0, 0);
    }
}
