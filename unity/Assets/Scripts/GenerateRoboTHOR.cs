// TODO: randomize the agent starting position!
// TODO: randomize object positions! This requires a bit of annotation.

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
    protected const float centerX = 2.696059f + 2.704441f;
    protected const float centerY = -2.327353f + 2.327353f; // default panel height
    protected const float centerZ = 1.3127378f - 4.268738f;

    protected const float structureX = 2.704441f;
    protected const float structureY = 2.327353f;
    protected const float structureZ = -4.268738f;

    protected const float wallCenterX = centerX + structureX;
    protected const float wallCenterY = centerY + structureY;
    protected const float wallCenterZ = centerZ + structureZ;

    protected const float wallPanelWidth = 0.978f;
    protected WallCell[,] wallCells;
    protected int cellsVisited;

    protected int xWalls, zWalls;
    protected Transform wallParent;

    protected float[] validStartingAgentRotations = new float[] {0, 90, 180, 270};
    protected string[] validOrientations = new string[] {
        "left", "right", "top", "bottom"
    };

    protected class WallCell {
        public bool visited;
        public Dictionary<string, bool?> walls;

        /**
         * Walls are null if they are boundary walls. That is,
         * they are unable to be toggled on or off.
         */
        public WallCell(
            bool visited,
            bool? left,
            bool? right,
            bool? top,
            bool? bottom
        ) {
            this.visited = visited;
            this.walls = new Dictionary<string, bool?> {
                ["left"] = left,
                ["right"] = right,
                ["top"] = top,
                ["bottom"] = bottom
            };
        }
    }

    // Returns the neighbor cell if a wall boundary is removed, and returns
    // null if all neighbors have been visited.
    protected (int, int)? VisitCell(int xGridCell, int zGridCell) {
        if (xGridCell < 0 || xGridCell >= xWalls) {
            throw new ArgumentOutOfRangeException($"xGridCell must be in [0:xWalls), not {xGridCell}");
        } else if (zGridCell < 0 || zGridCell >= zWalls) {
            throw new ArgumentOutOfRangeException($"zGridCell must be in [0:zWalls), not {zGridCell}");
        }

        if (!wallCells[xGridCell, zGridCell].visited) {
            cellsVisited += 1;
            wallCells[xGridCell, zGridCell].visited = true;
        }

        List<string> choicesToRemove = new List<string>();
        foreach (string orientation in validOrientations) {
            (int, int)? neighborCell = GetNeighbor(
                xGridCell: xGridCell,
                zGridCell: zGridCell,
                orientation: orientation
            );

            if (
                neighborCell.HasValue
                && !wallCells[neighborCell.Value.Item1, neighborCell.Value.Item2].visited
            ) {
                choicesToRemove.Add(orientation);
            }
        }

        // all neighbors are visited
        if (choicesToRemove.Count == 0) {
            return null;
        }

        // remove self
        string wallToRemove = choicesToRemove[Random.Range(0, choicesToRemove.Count)];
        wallCells[xGridCell, zGridCell].walls[wallToRemove] = false;

        // remove neighbor
        (int, int)? neighbor = RemoveNeighborWallBoundary(
            xGridCell: xGridCell,
            zGridCell: zGridCell,
            orientation: wallToRemove
        );

        return neighbor;
    }

    /**
     * Returns the position of the neighbor. If the neighbor is out of bounds, null is returned.
     */
    protected (int, int)? GetNeighbor(int xGridCell, int zGridCell, string orientation) {
        if (xGridCell < 0 || xGridCell >= xWalls) {
            throw new ArgumentOutOfRangeException($"xGridCell must be in [0:xWalls), not {xGridCell}");
        } else if (zGridCell < 0 || zGridCell >= zWalls) {
            throw new ArgumentOutOfRangeException($"zGridCell must be in [0:zWalls), not {zGridCell}");
        }

        if (!wallCells[xGridCell, zGridCell].walls[orientation].HasValue) {
            return null;
        }

        // remove neighboring instance
        switch (orientation) {
            case "left":
                if (xGridCell > 0) {
                    return (xGridCell - 1, zGridCell);
                }
                break;
            case "right":
                if (xGridCell < xWalls - 1) {
                    return (xGridCell + 1, zGridCell);
                }
                break;
            case "bottom":
                if (zGridCell < zWalls - 1) {
                    return (xGridCell, zGridCell + 1);
                }
                break;
            case "top":
                if (zGridCell > 0) {
                    return (xGridCell, zGridCell - 1);
                }
                break;
            default:
                throw new ArgumentException($"Invalid orientation {orientation}.");
        }
        return null;
    }

    /**
     * Returns the position of the neighbor. If the neighbor is out of bounds, null is returned.
     */
    protected (int, int)? RemoveNeighborWallBoundary(int xGridCell, int zGridCell, string orientation) {
        (int, int)? neighbor = GetNeighbor(
            xGridCell: xGridCell,
            zGridCell: zGridCell,
            orientation: orientation
        );

        if (!neighbor.HasValue) {
            return null;
        }

        switch (orientation) {
            case "left":
                wallCells[neighbor.Value.Item1, neighbor.Value.Item2].walls["right"] = false;
                break;
            case "right":
                wallCells[neighbor.Value.Item1, neighbor.Value.Item2].walls["left"] = false;
                break;
            case "bottom":
                wallCells[neighbor.Value.Item1, neighbor.Value.Item2].walls["top"] = false;
                break;
            case "top":
                wallCells[neighbor.Value.Item1, neighbor.Value.Item2].walls["bottom"] = false;
                break;
        }
        return neighbor;
    }

    /**
     * @param xGridCell is in [0:xWalls)
     * @param zGridCell is in [0:zWalls)
     */
    protected Vector3 GetWallGridPointCenter(int xGridCell, int zGridCell) {
        if (xGridCell < 0 || xGridCell >= xWalls) {
            throw new ArgumentOutOfRangeException($"xGridCell must be in [0:xWalls), not {xGridCell}");
        } else if (zGridCell < 0 || zGridCell >= zWalls) {
            throw new ArgumentOutOfRangeException($"zGridCell must be in [0:zWalls), not {zGridCell}");
        }

        float xPos = (
            xWalls % 2 == 1
            ? centerX + wallPanelWidth * (xGridCell - xWalls / 2) - wallPanelWidth / 2
            : centerX + wallPanelWidth * (xGridCell - (xWalls - 1) / 2 - 1)
        );
        float zPos = (
            zWalls % 2 == 1
            ? centerZ + wallPanelWidth * (zGridCell - zWalls / 2 + 1) + wallPanelWidth / 2
            : centerZ - wallPanelWidth * (zGridCell - (zWalls - 1) / 2 - 1)
        );

        return new Vector3(
            x: xPos,
            y: centerY,
            z: zPos
        );
    }

    protected void PlaceWall(Vector3 gridPointCenter, string orientation) {
        Quaternion rotation;
        Vector3 position = new Vector3(gridPointCenter.x, gridPointCenter.y, gridPointCenter.z);
        switch (orientation) {
            case "top":
                rotation = Quaternion.Euler(0, 180, 0);
                break;
            case "bottom":
                rotation = Quaternion.Euler(0, 0, 0);
                position.z -= wallPanelWidth;
                position.x += wallPanelWidth;
                break;
            case "left":
                rotation = Quaternion.Euler(0, 270, 0);
                break;
            case "right":
                rotation = Quaternion.Euler(0, 90, 0);
                position.x += wallPanelWidth;
                position.z -= wallPanelWidth;
                break;
            default:
                throw new ArgumentException($"Invalid orientation: {orientation}");
        }

        Instantiate(
            original: wallPrefab,
            parent: wallParent,
            position: position,
            rotation: rotation
        );
    }

    protected void PlaceWall(int xGridCell, int zGridCell, string orientation) {
        Vector3 gridPointCenter = GetWallGridPointCenter(xGridCell: xGridCell, zGridCell: zGridCell);
        PlaceWall(gridPointCenter: gridPointCenter, orientation: orientation);
    }

    protected void PlaceWalls() {
        for (int x = 0; x < xWalls; x++) {
            PlaceWall(xGridCell: x, zGridCell: 0, orientation: "top");
        }
        for (int z = 0; z < zWalls; z++) {
            PlaceWall(xGridCell: 0, zGridCell: z, orientation: "left");
        }

        for (int x = 0; x < xWalls; x++) {
            for (int z = 0; z < zWalls; z++) {
                if (wallCells[x, z].walls["right"] != false) {
                    PlaceWall(xGridCell: x, zGridCell: z, orientation: "right");
                }
                if (wallCells[x, z].walls["bottom"] != false) {
                    PlaceWall(xGridCell: x, zGridCell: z, orientation: "bottom");
                }
            }
        }
    }

    /**
     * Defaults are set based on the current RoboTHOR room configurations.
     *
     * @param agentTransform allows the agent to be teleported to a position
     *        and rotation to start the episode.
     */
    public void GenerateConfig(
        Transform agentTransform, int xWalls = 9, int zWalls = 4
    ) {
        if (xWalls <= 0 || zWalls <= 0) {
            throw new ArgumentOutOfRangeException(
                $"Must use > 0 walls in each direction, not xWalls={xWalls}, zWalls={zWalls}."
            );
        }
        this.xWalls = xWalls;
        this.zWalls = zWalls;

        wallParent = GameObject.Find("WallPanels").transform;

        #if UNITY_EDITOR
            for (int i = wallParent.childCount - 1; i >= 0; i--) {
                Debug.Log("destroying!");
                Destroy(wallParent.GetChild(i).gameObject);
            }
        #endif

        wallCells = new WallCell[xWalls, zWalls];
        cellsVisited = 0;

        // Teleport the agent to a new starting position
        // round position to nearest 0.25 -- but make sure that doesn't collide with wall position!
        /*
        float pX = Random.Range(2, 8);
        float pZ = Random.Range(-2, -4);
        agentTransform.position = new Vector3(
            x: pX,
            y: agentTransform.position.y,
            z: pZ
        );
        int startRotationI = Random.Range(0, validStartingRotations.Length);
        agentTransform.localEulerAngles = new Vector3(0, validStartingRotations[startRotationI], 0);
        */

        // Start with walls everywhere!
        for (int x = 0; x < xWalls; x++) {
            for (int z = 0; z < zWalls; z++) {
                wallCells[x, z] = new WallCell(
                    visited: false,
                    left: x == 0 ? (bool?) null : true,
                    top: z == 0 ? (bool?) null : true,
                    bottom: z == zWalls - 1 ? (bool?) null : true,
                    right: x == xWalls - 1 ? (bool?) null : true
                );
            }
        }

        Stack<(int, int)> stack = new Stack<(int, int)>();
        (int, int) startingPosition = (Random.Range(0, xWalls), Random.Range(0, zWalls));
        stack.Push(startingPosition);

        while (cellsVisited != xWalls * zWalls) {
            (int xGridCell, int zGridCell) = stack.Peek();
            (int, int)? neighbor = VisitCell(xGridCell: xGridCell, zGridCell: zGridCell);
            if (neighbor.HasValue) {
                stack.Push(neighbor.Value);
            } else {
                stack.Pop();
            }
        }

        PlaceWalls();
    }
}
