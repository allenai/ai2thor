using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateRoboTHOR : MonoBehaviour {
    [SerializeField]
    public GameObject wallPrefab;

    // Keeping these consistent matters because of PointNav,
    // where dramatically changing the coordinate system's bias
    // may have dire results. The added values come from the structure
    // parent's position.
    private const float centerX = 2.696059f + 2.704441f; // + 0.4885f;
    private const float centerY = -2.327353f + 2.327353f; // default panel height
    private const float centerZ = 1.3127378f - 4.268738f;

    private const float wallPanelWidth = 0.978f;

    private float[] validStartingRotations = new float[] {0, 90, 180, 270};

    // private struct WallGroup {
    //     public bool visited = false;
    //     public bool leftWall = false;
    //     public bool rightWall = false;
    //     public bool topWall = false;
    //     public bool bottomWall = false;
    // }

    /**
     * Defaults are set based on the current RoboTHOR room configurations.
     *
     * @param agentTransform allows the agent to be teleported to a position
     *        and rotation to start the episode.
     */
    public void GenerateConfig(Transform agentTransform, uint xWalls = 10, uint zWalls = 5) {
        if (xWalls == 0 || zWalls == 0) {
            throw new ArgumentOutOfRangeException(
                $"Must use > 0 walls in each direction, not xWalls={xWalls}, zWalls={zWalls}."
            );
        }

        Transform wallParent = GameObject.Find("WallPanels").transform;

        // set up the x sided walls
        // the +0.4885f is another bias based on parent transforms
        Vector3[] xSides = new Vector3[] {
            new Vector3(x: centerX + 0.4885f, y: centerY, z: centerZ + ((float) zWalls / 2) * wallPanelWidth),
            new Vector3(x: centerX + 0.4885f, y: centerY, z: centerZ - ((float) zWalls / 2) * wallPanelWidth)
        };
        if (xWalls % 2 == 0) {
            foreach (Vector3 xSide in xSides) {
                // just place on sides
                Vector3 wall1 = new Vector3(xSide.x + wallPanelWidth / 2, xSide.y, xSide.z);
                Vector3 wall2 = new Vector3(xSide.x - wallPanelWidth / 2, xSide.y, xSide.z);
                for (int i = 0; i < xWalls / 2; i++) {
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall1, rotation: Quaternion.Euler(0, 0, 0)
                    );
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall2, rotation: Quaternion.Euler(0, 0, 0)
                    );
                    wall1.x += wallPanelWidth;
                    wall2.x -= wallPanelWidth;
                }
            }
        } else {
            foreach (Vector3 xSide in xSides) {
                // place in middle, then place on sides
                Instantiate(
                    original: wallPrefab,
                    parent: wallParent,
                    position: xSide,
                    rotation: Quaternion.Euler(0, 0, 0)
                );

                // place on sides
                Vector3 wall1 = new Vector3(xSide.x + wallPanelWidth, xSide.y, xSide.z);
                Vector3 wall2 = new Vector3(xSide.x - wallPanelWidth, xSide.y, xSide.z);
                for (int i = 0; i < xWalls / 2; i++) {
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall1, rotation: Quaternion.Euler(0, 0, 0)
                    );
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall2, rotation: Quaternion.Euler(0, 0, 0)
                    );
                    wall1.x += wallPanelWidth;
                    wall2.x -= wallPanelWidth;
                }
            }
        }

        // set up the z sided walls
        // z also has a bias based on parent transform
        Vector3[] zSides = new Vector3[] {
            new Vector3(x: centerX + ((float) xWalls / 2) * wallPanelWidth, y: centerY, z: centerZ - 0.4890001f),
            new Vector3(x: centerX - ((float) xWalls / 2) * wallPanelWidth, y: centerY, z: centerZ - 0.4890001f)
        };
        if (zWalls % 2 == 0) {
            foreach (Vector3 side in zSides) {
                // just place on sides
                Vector3 wall1 = new Vector3(side.x, side.y, side.z + wallPanelWidth / 2);
                Vector3 wall2 = new Vector3(side.x, side.y, side.z - wallPanelWidth / 2);
                for (int i = 0; i < zWalls / 2; i++) {
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall1, rotation: Quaternion.Euler(0, 90, 0)
                    );
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall2, rotation: Quaternion.Euler(0, 90, 0)
                    );
                    wall1.z += wallPanelWidth;
                    wall2.z -= wallPanelWidth;
                }
            }
        } else {
            foreach (Vector3 side in zSides) {
                // place in middle, then place on sides
                Instantiate(
                    original: wallPrefab,
                    parent: wallParent,
                    position: side,
                    rotation: Quaternion.Euler(0, 90, 0)
                );

                // place on sides
                Vector3 wall1 = new Vector3(side.x, side.y, side.z + wallPanelWidth);
                Vector3 wall2 = new Vector3(side.x, side.y, side.z - wallPanelWidth);
                for (int i = 0; i < zWalls / 2; i++) {
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall1, rotation: Quaternion.Euler(0, 90, 0)
                    );
                    Instantiate(
                        original: wallPrefab, parent: wallParent, position: wall2, rotation: Quaternion.Euler(0, 90, 0)
                    );
                    wall1.z += wallPanelWidth;
                    wall2.z -= wallPanelWidth;
                }
            }
        }


        // Teleport the agent to a new starting position
        agentTransform.position = new Vector3(agentTransform.position.x, agentTransform.position.y, agentTransform.position.z);

        int startRotationI = Random.Range(0, validStartingRotations.Length);
        agentTransform.localEulerAngles = new Vector3(0, validStartingRotations[startRotationI], 0);

        // WallGroup[][] wallGroups = new WallGroup[xWalls][zWalls];

        // TODO: randomize the agent starting position!
        // Debug.Log("Hello, world!");
    }
}
