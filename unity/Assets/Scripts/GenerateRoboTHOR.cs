// TODO: randomize object positions! This requires a bit of annotation.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerateRoboTHOR : MonoBehaviour
{
    [SerializeField]
    protected GameObject wallPrefab;

    [SerializeField]
    protected GameObject floorPrefab;

    [SerializeField]
    protected GameObject outerWallPrefab;

    [SerializeField]
    protected GameObject ceilingPrefab;

    // Keeping these consistent matters because of PointNav,
    // where dramatically changing the coordinate system's bias
    // may have dire results. The added values come from the structure
    // parent's position.
    protected const float wallCenterX = 2.696059f + 2.704441f;
    protected const float wallCenterY = -2.327353f + 2.327353f; // default panel height
    protected const float wallCenterZ = 1.3127378f - 4.268738f;

    protected const float ceilingSizeX = 9.76f;
    protected const float ceilingSizeZ = 5.95f;

    protected const float ceilingCenterX = 2.704441f - 0.18f;
    protected const float ceilingCenterY = 2.327353f;
    protected const float ceilingCenterZ = -4.268738f - 0.63f;

    protected const float wallPanelWidth = 0.978f;
    protected WallCell[,] wallCells;
    protected int cellsVisited;

    protected int xWalls,
        zWalls;
    protected int boundaryPadding;
    protected Transform wallParent;
    protected Transform floorParent;
    protected Transform structure;

    protected float[] validStartingAgentRotations = new float[] { 0, 90, 180, 270 };
    protected string[] validOrientations = new string[] { "left", "right", "top", "bottom" };

    protected class WallCell
    {
        public bool visited;
        public Dictionary<string, bool?> walls;

        /**
         * Walls are null if they are boundary walls. That is,
         * they are unable to be toggled on or off.
         */
        public WallCell(bool visited, bool? left, bool? right, bool? top, bool? bottom)
        {
            this.visited = visited;
            this.walls = new Dictionary<string, bool?>
            {
                ["left"] = left,
                ["right"] = right,
                ["top"] = top,
                ["bottom"] = bottom
            };
        }
    }

    // Returns the neighbor cell if a wall boundary is removed, and returns
    // null if all neighbors have been visited.
    protected (int, int)? VisitCell(int xGridCell, int zGridCell)
    {
        if (xGridCell < 0 || xGridCell >= xWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"xGridCell must be in [0:xWalls), not {xGridCell}"
            );
        }
        else if (zGridCell < 0 || zGridCell >= zWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"zGridCell must be in [0:zWalls), not {zGridCell}"
            );
        }

        if (!wallCells[xGridCell, zGridCell].visited)
        {
            cellsVisited += 1;
            wallCells[xGridCell, zGridCell].visited = true;
        }

        List<string> choicesToRemove = new List<string>();
        foreach (string orientation in validOrientations)
        {
            (int, int)? neighborCell = GetNeighbor(
                xGridCell: xGridCell,
                zGridCell: zGridCell,
                orientation: orientation
            );

            if (
                neighborCell.HasValue
                && !wallCells[neighborCell.Value.Item1, neighborCell.Value.Item2].visited
            )
            {
                choicesToRemove.Add(orientation);
            }
        }

        // all neighbors are visited
        if (choicesToRemove.Count == 0)
        {
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
    protected (int, int)? GetNeighbor(int xGridCell, int zGridCell, string orientation)
    {
        if (xGridCell < 0 || xGridCell >= xWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"xGridCell must be in [0:xWalls), not {xGridCell}"
            );
        }
        else if (zGridCell < 0 || zGridCell >= zWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"zGridCell must be in [0:zWalls), not {zGridCell}"
            );
        }

        if (!wallCells[xGridCell, zGridCell].walls[orientation].HasValue)
        {
            return null;
        }

        // remove neighboring instance
        switch (orientation)
        {
            case "left":
                if (xGridCell > 0)
                {
                    return (xGridCell - 1, zGridCell);
                }
                break;
            case "right":
                if (xGridCell < xWalls - 1)
                {
                    return (xGridCell + 1, zGridCell);
                }
                break;
            case "bottom":
                if (zGridCell < zWalls - 1)
                {
                    return (xGridCell, zGridCell + 1);
                }
                break;
            case "top":
                if (zGridCell > 0)
                {
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
    protected (int, int)? RemoveNeighborWallBoundary(
        int xGridCell,
        int zGridCell,
        string orientation
    )
    {
        (int, int)? neighbor = GetNeighbor(
            xGridCell: xGridCell,
            zGridCell: zGridCell,
            orientation: orientation
        );

        if (!neighbor.HasValue)
        {
            return null;
        }

        switch (orientation)
        {
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
    protected Vector3 GetWallGridPointCenter(int xGridCell, int zGridCell)
    {
        if (xGridCell < 0 || xGridCell >= xWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"xGridCell must be in [0:xWalls), not {xGridCell}"
            );
        }
        else if (zGridCell < 0 || zGridCell >= zWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"zGridCell must be in [0:zWalls), not {zGridCell}"
            );
        }

        float xPos = (
            xWalls % 2 == 1
                ? wallCenterX + wallPanelWidth * (xGridCell - xWalls / 2) - wallPanelWidth / 2
                : wallCenterX + wallPanelWidth * (xGridCell - (xWalls - 1) / 2 - 1)
        );
        float zPos = (
            zWalls % 2 == 1
                ? wallCenterZ - wallPanelWidth * (zGridCell - zWalls / 2) + wallPanelWidth / 2
                : wallCenterZ - wallPanelWidth * (zGridCell - (zWalls - 1) / 2 - 1)
        );

        return new Vector3(x: xPos, y: wallCenterY, z: zPos);
    }

    /**
     * @param xGridCell is in [0:xWalls)
     * @param zGridCell is in [0:zWalls)
     */
    protected Vector3 GetAgentGridPointCenter(int xGridCell, int zGridCell)
    {
        if (xGridCell < 0 || xGridCell >= xWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"xGridCell must be in [0:xWalls), not {xGridCell}"
            );
        }
        else if (zGridCell < 0 || zGridCell >= zWalls)
        {
            throw new ArgumentOutOfRangeException(
                $"zGridCell must be in [0:zWalls), not {zGridCell}"
            );
        }

        float agentCenterX = 5.387f;
        float agentCenterZ = -2.967f;

        float xPos = (
            xWalls % 2 == 1
                ? agentCenterX + wallPanelWidth * (xGridCell - xWalls / 2)
                : agentCenterX
                    + wallPanelWidth * (xGridCell - (xWalls - 1) / 2 - 1)
                    + wallPanelWidth / 2
        );
        float zPos = (
            zWalls % 2 == 1
                ? agentCenterZ - wallPanelWidth * (zGridCell - zWalls / 2)
                : agentCenterZ
                    - wallPanelWidth * (zGridCell - (zWalls - 1) / 2 - 1)
                    - wallPanelWidth / 2
        );

        // These are the empirical center position of the agent.
        // They don't need to be super precise because the position is rounded.
        return new Vector3(x: xPos, y: 0.9009997f, z: zPos);
    }

    /**
     * Place a single wall at a position and orientation.
     */
    protected void PlaceWall(Vector3 gridPointCenter, string orientation)
    {
        Quaternion rotation;
        Vector3 position = new Vector3(gridPointCenter.x, gridPointCenter.y, gridPointCenter.z);
        switch (orientation)
        {
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

    /**
     * Place a single wall at a position and orientation.
     */
    protected void PlaceWall(int xGridCell, int zGridCell, string orientation)
    {
        Vector3 gridPointCenter = GetWallGridPointCenter(
            xGridCell: xGridCell,
            zGridCell: zGridCell
        );
        PlaceWall(gridPointCenter: gridPointCenter, orientation: orientation);
    }

    /**
     * Place all the walls based on wallCells.
     */
    protected void PlaceWalls()
    {
        for (int x = 0; x < xWalls; x++)
        {
            PlaceWall(xGridCell: x, zGridCell: 0, orientation: "top");
        }
        for (int z = 0; z < zWalls; z++)
        {
            PlaceWall(xGridCell: 0, zGridCell: z, orientation: "left");
        }

        for (int x = 0; x < xWalls; x++)
        {
            for (int z = 0; z < zWalls; z++)
            {
                if (wallCells[x, z].walls["right"] != false)
                {
                    PlaceWall(xGridCell: x, zGridCell: z, orientation: "right");
                }
                if (wallCells[x, z].walls["bottom"] != false)
                {
                    PlaceWall(xGridCell: x, zGridCell: z, orientation: "bottom");
                }
            }
        }
    }

    protected void AddOuterWalls()
    {
        GameObject wall;

        // side 1
        wall =
            Instantiate(
                original: outerWallPrefab,
                parent: floorParent,
                position: new Vector3(
                    x: wallCenterX,
                    y: 1.298f,
                    z: wallCenterZ + (float)(zWalls + boundaryPadding * 2) / 2
                ),
                rotation: Quaternion.identity
            ) as GameObject;
        wall.transform.localScale = new Vector3(
            x: xWalls + boundaryPadding * 2,
            y: wall.transform.localScale.y,
            z: wall.transform.localScale.z
        );

        // side 2
        wall =
            Instantiate(
                original: outerWallPrefab,
                parent: floorParent,
                position: new Vector3(
                    x: wallCenterX,
                    y: 1.298f,
                    z: wallCenterZ - (float)(zWalls + boundaryPadding * 2) / 2
                ),
                rotation: Quaternion.Euler(0, 180, 0)
            ) as GameObject;
        wall.transform.localScale = new Vector3(
            x: xWalls + boundaryPadding * 2,
            y: wall.transform.localScale.y,
            z: wall.transform.localScale.z
        );

        // side 3
        wall =
            Instantiate(
                original: outerWallPrefab,
                parent: floorParent,
                position: new Vector3(
                    x: wallCenterX - (float)(xWalls + boundaryPadding * 2) / 2,
                    y: 1.298f,
                    z: wallCenterZ
                ),
                rotation: Quaternion.Euler(0, -90, 0)
            ) as GameObject;
        wall.transform.localScale = new Vector3(
            x: zWalls + boundaryPadding * 2,
            y: wall.transform.localScale.y,
            z: wall.transform.localScale.z
        );

        // side 4
        wall =
            Instantiate(
                original: outerWallPrefab,
                parent: floorParent,
                position: new Vector3(
                    x: wallCenterX + (float)(xWalls + boundaryPadding * 2) / 2,
                    y: 1.298f,
                    z: wallCenterZ
                ),
                rotation: Quaternion.Euler(0, 90, 0)
            ) as GameObject;
        wall.transform.localScale = new Vector3(
            x: zWalls + boundaryPadding * 2,
            y: wall.transform.localScale.y,
            z: wall.transform.localScale.z
        );
    }

    protected void PlaceCeilings()
    {
        int xCeilings = (int)Math.Ceiling((xWalls + 2 * boundaryPadding) / ceilingSizeX);
        int zCeilings = (int)Math.Ceiling((zWalls + 2 * boundaryPadding) / ceilingSizeZ);

        for (int x = 0; x < xCeilings; x++)
        {
            for (int z = 0; z < zCeilings; z++)
            {
                // Place ceilings
                Instantiate(
                    original: ceilingPrefab,
                    parent: structure,
                    position: new Vector3(
                        x: ceilingCenterX
                            + ceilingSizeX * (x - (float)xCeilings / 2)
                            + ceilingSizeX / 2,
                        y: ceilingCenterY,
                        z: ceilingCenterZ
                            + ceilingSizeZ * (z - (float)zCeilings / 2)
                            + ceilingSizeZ / 2
                    ),
                    rotation: Quaternion.identity
                );
            }
        }
    }

    /**
     * Defaults are set based on the current RoboTHOR room configurations.
     *
     * @param agentTransform allows the agent to be teleported to a position
     *        and rotation to start the episode.
     * @param boundaryPadding is the padding between the boundary inner wall panels
              and the outer wall.
     */
    public void GenerateConfig(
        Transform agentTransform,
        int xWalls = 16,
        int zWalls = 8,
        int boundaryPadding = 0
    )
    {
        if (xWalls <= 0 || zWalls <= 0)
        {
            throw new ArgumentOutOfRangeException(
                $"Must use > 0 walls in each direction, not xWalls={xWalls}, zWalls={zWalls}."
            );
        }
        if (boundaryPadding < 0)
        {
            throw new ArgumentOutOfRangeException(
                $"boundaryPadding must be >= 0, not {boundaryPadding}"
            );
        }
        this.xWalls = xWalls;
        this.zWalls = zWalls;
        this.boundaryPadding = boundaryPadding;

        wallParent = GameObject.Find("WallPanels").transform;
        floorParent = GameObject.Find("FloorTiles").transform;
        structure = GameObject.Find("Structure").transform;

        // There is a single floor tile under the agent at the start so that it
        // is caught by gravity.
        for (int i = floorParent.childCount - 1; i >= 0; i--)
        {
            Destroy(floorParent.GetChild(i).gameObject);
        }

        PlaceCeilings();

        for (int x = 0; x < xWalls + boundaryPadding * 2; x++)
        {
            for (int z = 0; z < zWalls + boundaryPadding * 2; z++)
            {
                Instantiate(
                    original: floorPrefab,
                    parent: floorParent,
                    position: new Vector3(
                        x: wallCenterX + (x - (float)(xWalls + boundaryPadding * 2) / 2) + 0.5f,
                        y: 0,
                        z: wallCenterZ + (z - (float)(zWalls + boundaryPadding * 2) / 2) + 0.5f
                    ),
                    rotation: Quaternion.identity
                );
            }
        }
        AddOuterWalls();

#if UNITY_EDITOR
        // Only necessary because Initialize can be called within the
        // editor without calling Reset(). However, this is not supported
        // from the Python API.
        for (int i = wallParent.childCount - 1; i >= 0; i--)
        {
            Destroy(wallParent.GetChild(i).gameObject);
        }
#endif

        wallCells = new WallCell[xWalls, zWalls];
        cellsVisited = 0;

        // Start with walls everywhere!
        for (int x = 0; x < xWalls; x++)
        {
            for (int z = 0; z < zWalls; z++)
            {
                wallCells[x, z] = new WallCell(
                    visited: false,
                    left: x == 0 ? (bool?)null : true,
                    top: z == 0 ? (bool?)null : true,
                    bottom: z == zWalls - 1 ? (bool?)null : true,
                    right: x == xWalls - 1 ? (bool?)null : true
                );
            }
        }

        // Search for good walls
        Stack<(int, int)> stack = new Stack<(int, int)>();
        (int, int) startingPosition = (Random.Range(0, xWalls), Random.Range(0, zWalls));
        stack.Push(startingPosition);
        while (cellsVisited != xWalls * zWalls)
        {
            (int xGridCell, int zGridCell) = stack.Peek();
            (int, int)? neighbor = VisitCell(xGridCell: xGridCell, zGridCell: zGridCell);
            if (neighbor.HasValue)
            {
                stack.Push(neighbor.Value);
            }
            else
            {
                stack.Pop();
            }
        }

        PlaceWalls();

        // Teleport the agent to a new starting position
        // round position to nearest 0.25 -- but make sure that doesn't collide with wall position!
        int agentXCell = Random.Range(0, xWalls);
        int agentZCell = Random.Range(0, zWalls);
        Vector3 agentPosition = GetAgentGridPointCenter(
            xGridCell: agentXCell,
            zGridCell: agentZCell
        );
        agentPosition.y = agentTransform.position.y;
        agentTransform.position = agentPosition;

        // change agent rotation
        int startRotationI = Random.Range(0, validStartingAgentRotations.Length);
        agentTransform.localEulerAngles = new Vector3(
            0,
            validStartingAgentRotations[startRotationI],
            0
        );
    }
}
