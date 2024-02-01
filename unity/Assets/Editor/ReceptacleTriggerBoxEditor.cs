using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class ReceptacleTriggerBoxEditor : EditorWindow {

    [MenuItem("Receptacles/Try create receptacle trigger box for object")]
    public static void TryToAddReceptacleTriggerBoxFromSelected() {
        GameObject obj = Selection.activeGameObject;
        SimObjPhysics sop = obj.GetComponent<SimObjPhysics>();
        TryToAddReceptacleTriggerBox(sop);
    }

    public static void TryToAddReceptacleTriggerBox(SimObjPhysics sop, float yThresMax = 0.075f, float worldOffset=-100f) {
        if (sop == null) {
            throw new NotImplementedException(
                $"Adding receptacle trigger box is only possible the active game object, has an associated SimObjPhysics script."
            );
        }

        Quaternion oldRot = sop.transform.rotation;
        Vector3 oldPos = sop.transform.position;

        List<MeshCollider> tmpMeshColliders = new List<MeshCollider>();
        List<Collider> enabledColliders = new List<Collider>();
        foreach (Collider c in sop.GetComponentsInChildren<Collider>()) {
            if (c.enabled) {
                enabledColliders.Add(c);
                c.enabled = false;
            }
        }
        try {
            sop.transform.rotation = Quaternion.identity;
            sop.transform.position = new Vector3(worldOffset, worldOffset, worldOffset);
            sop.GetComponent<Rigidbody>().isKinematic = true;

            foreach (MeshFilter mf in sop.GetComponentsInChildren<MeshFilter>()) {
                GameObject tmpGo = new GameObject();
                tmpGo.layer = LayerMask.NameToLayer("SimObjVisible");
                tmpGo.transform.position = mf.gameObject.transform.position;
                tmpGo.transform.rotation = mf.gameObject.transform.rotation;
                tmpGo.transform.parent = sop.transform;

                MeshCollider mc = tmpGo.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;

                Rigidbody rb = tmpGo.AddComponent<Rigidbody>();
                rb.isKinematic = true;

                tmpMeshColliders.Add(mc);
            }

            Physics.SyncTransforms();

            AxisAlignedBoundingBox aabb = sop.AxisAlignedBoundingBox;

            Vector3 center = aabb.center;
            Vector3 size = aabb.size;
            float rtbYSize = Mathf.Min(0.25f, Mathf.Max(aabb.size.x, aabb.size.y, aabb.size.z));

            float yThres = Mathf.Min(yThresMax, size.y * 0.15f);

            float xMin = center.x - 0.95f * aabb.size.x / 2f;
            float xMax = center.x + 0.95f * aabb.size.x / 2f;
            float zMin = center.z - 0.95f * aabb.size.z / 2f;
            float zMax = center.z + 0.95f * aabb.size.z / 2f;

            float yStart = center.y + size.y / 2f + 0.5f;
            float dummyY = -1000f;

            // Func<int, float> iXToX = (i => xMin + i * (xMax - xMin) / (n - 1.0f));

            List<List<float>> mat = new List<List<float>>();
            int n = 30;
            for (int iX = 0; iX < n; iX++) {
                float x = xMin + iX * (xMax - xMin) / (n - 1.0f);
                // Debug.Log($"x val: {x}");

                var yVals = new List<float>();
                for (int iZ = 0; iZ < n; iZ++) {
                    float z = zMin + iZ * (zMax - zMin) / (n - 1.0f);

                    // Debug.Log($"Pos: ({iX}, {iZ}), vals ({x}, {z})");

                    RaycastHit hit;
                    if (Physics.Raycast(
                        origin: new Vector3(x, yStart, z),
                        direction: new Vector3(0f, -1f, 0f),
                        hitInfo: out hit,
                        maxDistance: 10f,
                        layerMask: LayerMask.GetMask("SimObjVisible"),
                        queryTriggerInteraction: QueryTriggerInteraction.Ignore
                    )) {
                        // Debug.Log($"HITS {hit.point.y}");
                        // Debug.DrawLine(hit.point, hit.point + new Vector3(0f, 0.1f, 0f), Color.cyan, 15f);

                        if (Vector3.Angle(hit.normal, Vector3.up) < 30f) {
                            yVals.Add(hit.point.y);
                            Debug.Log(hit.point.y);
                        } else {
                            yVals.Add(dummyY);
                        }
                    } else {
                        yVals.Add(dummyY);
                    }
                }
                mat.Add(yVals);
            }

            Dictionary<(int, int), int> posToGroup = new Dictionary<(int, int), int>();
            Dictionary<int, float> groupToMaxYVal = new Dictionary<int, float>();
            Dictionary<int, float> groupToMinYVal = new Dictionary<int, float>();
            Dictionary<int, List<(int, int)>> groupToPos = new Dictionary<int, List<(int, int)>>();

            int nextGroup = 0;
            for (int iX = 0; iX < n; iX++) {
                for (int iZ = 0; iZ < n; iZ++) {
                    // Debug.Log($"Pos: ({iX}, {iZ})");
                    float curYVal = mat[iX][iZ];
                    // Debug.Log($"Cur Y: {curYVal}");

                    if (curYVal == dummyY) {
                        posToGroup[(iX, iZ)] = -1;
                        groupToMaxYVal[-1] = dummyY;
                        groupToMinYVal[-1] = dummyY;
                        continue;
                    }

                    if (iX > 0) {
                        int group = posToGroup[(iX - 1, iZ)];
                        float otherMaxYVal = groupToMaxYVal[group];
                        float otherMinYVal = groupToMinYVal[group];

                        if (
                            Mathf.Abs(curYVal - otherMaxYVal) < yThres &&
                            Mathf.Abs(curYVal - otherMinYVal) < yThres
                        ) {
                            posToGroup[(iX, iZ)] = group;
                            groupToPos[group].Add((iX, iZ));
                            groupToMaxYVal[group] = Mathf.Max(curYVal, otherMaxYVal);
                            groupToMinYVal[group] = Mathf.Min(curYVal, otherMinYVal);
                            continue;
                        }
                    }

                    if (iZ > 0) {
                        int group = posToGroup[(iX, iZ - 1)];
                        float otherMaxYVal = groupToMaxYVal[group];
                        float otherMinYVal = groupToMinYVal[group];

                        if (
                            Mathf.Abs(curYVal - otherMaxYVal) < yThres &&
                            Mathf.Abs(curYVal - otherMinYVal) < yThres
                        ) {
                            posToGroup[(iX, iZ)] = group;
                            groupToPos[group].Add((iX, iZ));
                            groupToMaxYVal[group] = Mathf.Max(curYVal, otherMaxYVal);
                            groupToMinYVal[group] = Mathf.Min(curYVal, otherMinYVal);
                            continue;
                        }
                    }

                    posToGroup[(iX, iZ)] = nextGroup;
                    groupToMaxYVal[nextGroup] = curYVal;
                    groupToMinYVal[nextGroup] = curYVal;
                    groupToPos[nextGroup] = new List<(int, int)>();
                    groupToPos[nextGroup].Add((iX, iZ));
                    nextGroup++;
                }
            }

            var groupToRectangles = new Dictionary<int, List<((int, int), (int, int))>>();
            foreach (int group in groupToPos.Keys) {
                var posSet = new HashSet<(int, int)>(groupToPos[group]);

                List<((int, int), (int, int))> rectangles = new List<((int, int), (int, int))>();

                while (posSet.Count > 0) {
                    (int, int) nextiXiZ = posSet.Min();

                    int startIX = nextiXiZ.Item1;
                    int startIZ = nextiXiZ.Item2;

                    int k = 1;
                    while (posSet.Contains((startIX + k, startIZ))) {
                        k++;
                    }

                    int endIX = startIX + k - 1;

                    k = 1;
                    while (true) {
                        bool allContained = true;
                        for (int iX = startIX; iX <= endIX; iX++) {
                            if (!posSet.Contains((iX, startIZ + k))) {
                                allContained = false;
                                break;
                            }
                        }
                        if (!allContained) {
                            break;
                        }
                        k++;
                    }
                    int endIZ = startIZ + k - 1;

                    for (int iX = startIX; iX <= endIX; iX++) {
                        for (int iZ = startIZ; iZ <= endIZ; iZ++) {
                            posSet.Remove((iX, iZ));
                        }
                    }

                    rectangles.Add(((startIX, startIZ), (endIX, endIZ)));
                }
                groupToRectangles[group] = rectangles;
            }

            var vector3CornerLists = new List<List<Vector3>>();
            List<Color> colors = new List<Color>{Color.cyan, Color.yellow, Color.red, Color.magenta, Color.green, Color.blue};
            int yar = -1;
            foreach (int group in groupToRectangles.Keys) {
                float y = groupToMinYVal[group];

                foreach (((int, int), (int, int)) extents in groupToRectangles[group]) {
                    yar++;
                    (int, int) start = extents.Item1;
                    (int, int) end = extents.Item2;

                    float startX = xMin + (start.Item1 - 0.5f) * (xMax - xMin) / (n - 1.0f);
                    float endX = xMin + (end.Item1 + 0.5f) * (xMax - xMin) / (n - 1.0f);

                    float startZ = zMin + (start.Item2  - 0.5f) * (zMax - zMin) / (n - 1.0f);
                    float endZ = zMin + (end.Item2 + 0.5f) * (zMax - zMin) / (n - 1.0f);

                    if (Math.Min(Math.Abs(start.Item1 - end.Item1), Math.Abs(start.Item2 - end.Item2)) <= 1) {
                        continue;
                    }

                    List<Vector3> corners = new List<Vector3>();
                    corners.Add(new Vector3(startX, y, startZ));
                    corners.Add(new Vector3(endX, y, startZ));
                    corners.Add(new Vector3(endX, y, endZ));
                    corners.Add(new Vector3(startX, y, endZ));

#if UNITY_EDITOR
                    Debug.DrawLine(corners[0], corners[1], colors[yar % colors.Count], 15f);
                    Debug.DrawLine(corners[1], corners[2], colors[yar % colors.Count], 15f);
                    Debug.DrawLine(corners[2], corners[3], colors[yar % colors.Count], 15f);
                    Debug.DrawLine(corners[3], corners[0], colors[yar % colors.Count], 15f);
#endif
                    vector3CornerLists.Add(corners);
                }
            }

            Transform t = sop.transform.Find("ReceptacleTriggerBoxes");
            GameObject go = null;
            if (t != null) {
                DestroyImmediate(t.gameObject);
            }
            if (t == null) {
                go = new GameObject("ReceptacleTriggerBoxes");
                go.transform.position = sop.transform.position;
                go.transform.parent = sop.transform;
            }
            Physics.SyncTransforms();

            int cornerListInd = 0;
            List<GameObject> boxGos = new List<GameObject>();
            foreach (List<Vector3> cornerList in vector3CornerLists) {
                Vector3 c0 = cornerList[0];
                Vector3 c1 = cornerList[1];
                Vector3 c2 = cornerList[2];
                Vector3 c3 = cornerList[3];

                GameObject rtb = new GameObject($"ReceptacleTriggerBox{cornerListInd++}");
                boxGos.Add(rtb);
                rtb.transform.position = sop.transform.position;
                rtb.transform.parent = go.transform;
                rtb.layer = LayerMask.NameToLayer("SimObjInvisible");
                BoxCollider bc = rtb.AddComponent<BoxCollider>();
                bc.center = (c0 + c1 + c2 + c3) * 0.25f - rtb.transform.position + new Vector3(0f, rtbYSize / 2.0f, 0f);
                bc.size = c2 - c0 + new Vector3(0f, rtbYSize, 0f);
                bc.isTrigger = true;
            }
            sop.ReceptacleTriggerBoxes = boxGos.ToArray();
        } finally {
            sop.transform.position = oldPos;
            sop.transform.rotation = oldRot;
            sop.GetComponent<Rigidbody>().isKinematic = false;

            foreach (MeshCollider tmc in tmpMeshColliders) {
                DestroyImmediate(tmc.gameObject);
            }
            foreach (Collider c in enabledColliders) {
                c.enabled = true;
            }
            Physics.SyncTransforms();
        }
    }
}