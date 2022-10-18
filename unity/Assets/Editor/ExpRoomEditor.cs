using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Security.Cryptography;


public class ExpRoomEditor : EditorWindow {
    Vector2 scroll;

    List <(SimObjPhysics, string)> sopAndPrefabTuples = null;

    [MenuItem("ExpRoom/Add all pickupable prefabs to AvailableObjects")]
    static void AddPickupableToAvailableObjects() {
        GameObject availableObjectsGameObject = GameObject.Find("AvailableObjects");
        AddAllPrefabsAsOnlyChildrenOfObject(
            parent: availableObjectsGameObject,
            onlyPickupable: true,
            onlyReceptacles: false,
            tag: ""
        );
    }

    [MenuItem("ExpRoom/Add all pickupable receptacle prefabs to AvailableContainers")]
    static void AddPickupableReceptaclesToAvailableContainers() {
        GameObject availableContainersGameObject = GameObject.Find("AvailableContainers");
        AddAllPrefabsAsOnlyChildrenOfObject(
            parent: availableContainersGameObject,
            onlyPickupable: true,
            onlyReceptacles: true,
            tag: "Container"
        );
    }

    [MenuItem("ExpRoom/Interactively add all pickupable prefabs to AvailableObjects")]
    static void Init() {
        ExpRoomEditor window = (ExpRoomEditor) EditorWindow.GetWindow(typeof(ExpRoomEditor));
        window.Show();
        window.position = new Rect(20, 80, 400, 300);
    }

    // Disable menu items if not in ExpRoom
    [MenuItem("ExpRoom/Add all pickupable prefabs to AvailableObjects", true)]
    [MenuItem("ExpRoom/Add all pickupable receptacle prefabs to AvailableContainers", true)]
    [MenuItem("ExpRoom/Interactively add all pickupable prefabs to AvailableObjects", true)]
    static bool HideMenuIfNotInExpRoom() {
        //Debug.Log(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "FloorPlan_ExpRoom";
    }

    void OnGUI() {
        GUILayout.Space(3);
        int oldValue = GUI.skin.window.padding.bottom;
        GUI.skin.window.padding.bottom = -20;
        Rect windowRect = GUILayoutUtility.GetRect(1, 17);
        windowRect.x += 4;
        windowRect.width -= 7;
        GUI.skin.window.padding.bottom = oldValue;

        if (GUILayout.Button("Find all pickupable")) {
            GameObject availableObjectsGameObject = GameObject.Find("AvailableObjects");
            sopAndPrefabTuples = AddAllPrefabsAsOnlyChildrenOfObject(
                parent: availableObjectsGameObject,
                onlyPickupable: true,
                onlyReceptacles: false
            );
        }

        if (sopAndPrefabTuples != null) {
            if (sopAndPrefabTuples.Count == 0) {
                GUILayout.Label("No pickupable objects found");
            } else {
                GUILayout.Label("The following prefabs were found:");
                scroll = GUILayout.BeginScrollView(scroll);


                foreach ((SimObjPhysics, string) sopAndPath in sopAndPrefabTuples) {
                    SimObjPhysics sop = sopAndPath.Item1;
                    string path = sopAndPath.Item2;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(path, GUILayout.Width(4 * position.width / 8));
                    GUILayout.Label(sop.Type.ToString(), GUILayout.Width(3 * position.width / 8));
                    if (GUILayout.Button("Select", GUILayout.Width(position.width / 8 - 2))) {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }
    }

    public static void OpenBoxFlapsMore(SimObjPhysics sop) {
        CanOpen_Object coo = sop.GetComponent<CanOpen_Object>();
        if (coo) {
            for (int i = 0; i < coo.MovingParts.Length; i++) {
                GameObject part = coo.MovingParts[i];
                Vector3 openRot = coo.openPositions[i];
                Vector3 closeRot = coo.closedPositions[i];

                Quaternion openRotQ = Quaternion.Euler(openRot);
                Quaternion closeRotQ = Quaternion.Euler(closeRot);

                float angle = Quaternion.Angle(openRotQ, closeRotQ);
                float newAngle = angle;
                while (newAngle < 180f) {
                     newAngle += 30f;
                }
                part.transform.rotation = Quaternion.Euler(
                    (openRot - closeRot) * (newAngle / angle) + closeRot
                );
                // Debug.Log($"Open = {openRot}, closed = {closeRot}, Euler = {(openRot - closeRot) * (newAngle / angle) + openRot}");
                Debug.Log($"Part = {part}, angle {angle}, new angle {newAngle}, true angle {Quaternion.Angle(openRotQ, part.transform.rotation)}");
            }
        }
    }

    public static void FixBoundingBox(SimObjPhysics sop) {

        Vector3 startPos = sop.transform.position;
        Quaternion startRot = sop.transform.rotation;

        sop.transform.position = new Vector3(0f, 0f, 0f);
        sop.transform.rotation = Quaternion.identity;

        sop.BoundingBox.transform.position = new Vector3();
        sop.BoundingBox.transform.rotation = Quaternion.identity;
        sop.BoundingBox.transform.localScale = new Vector3(1f, 1f, 1f);

        Physics.SyncTransforms();

        Bounds objBounds = UtilityFunctions.CreateEmptyBounds();
        foreach (Collider c in sop.GetComponentsInChildren<Collider>()) {
            if (c.enabled && !c.isTrigger) {
                objBounds.Encapsulate(c.bounds);
            }
        }

        BoxCollider bbox = sop.BoundingBox.GetComponent<BoxCollider>();
        bbox.center = objBounds.center;
        bbox.size = 2 * objBounds.extents;

        foreach (Transform child in sop.transform) {
            child.position = child.position - bbox.center;
        }
        bbox.transform.position = Vector3.zero;
        bbox.center = Vector3.zero;

        sop.transform.position = startPos;
        sop.transform.rotation = startRot;
    }

    public static string CreateSHA256Hash(string path) {
        SHA256 hasher = SHA256.Create();

        using (System.IO.FileStream fs = System.IO.File.OpenRead(path)) {
            return string.Join("", hasher.ComputeHash(fs).Select(b => b.ToString("x2")).ToArray());
        }
    }

    public static string[] GetAllPrefabs() {
        return AssetDatabase.GetAllAssetPaths().Where(
            s => s.Contains(".prefab")
                && !s.Contains("Custom Project")
                && !s.Contains("Assets/Resources/")
                && !s.Contains("PhysicsTestPrefabs")
                && !s.Contains("(Old)")
             // && s.Contains("Assets/Physics/SimObjsPhysics") &&
        ).ToArray();
    }

    public static List<(SimObjPhysics, string)> AddAllPrefabsAsOnlyChildrenOfObject(
        GameObject parent,
        bool onlyPickupable,
        bool onlyReceptacles,
        string tag = ""
    ) {
        List <(SimObjPhysics, string)> sopAndPrefabTuples = new List <(SimObjPhysics, string)>();
        string[] allPrefabs = GetAllPrefabs();
        int numAdded = 0;
        foreach (string prefab in allPrefabs) {
            GameObject go = null;
            try {
                Debug.Log($"Checking {prefab}");
                go = (GameObject) AssetDatabase.LoadMainAssetAtPath(prefab);
                SimObjPhysics sop = go.GetComponent<SimObjPhysics>();

                // if (sop != null && sop.Type.ToString() == "Box" && sop.GetComponent<CanOpen_Object>()) {
                if (sop != null) {
                    bool pickupable = sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup;
                    bool isReceptacle = Array.IndexOf(
                        sop.SecondaryProperties,
                        SimObjSecondaryProperty.Receptacle
                    ) > -1 && sop.ReceptacleTriggerBoxes != null;
                    if (
                        (pickupable || !onlyPickupable)
                        && (isReceptacle || !onlyReceptacles)
                    ) {
                        sopAndPrefabTuples.Add((go.GetComponent<SimObjPhysics>(), prefab));
                        numAdded += 1;
                    }
                }
            } catch {
                try {
                    if (go) {
                        DestroyImmediate(go);
                    }
                } catch {}
                Debug.LogWarning($"Prefab {prefab} failed to load.");
            }
        }

        sopAndPrefabTuples = sopAndPrefabTuples.OrderBy(
            sopAndPath => (
                sopAndPath.Item1.Type.ToString(),
                int.Parse("0" + new string(
                    sopAndPath.Item1.name
                    .Split('/')
                    .Last()
                    .Where(c => char.IsDigit(c))
                    .ToArray()))
            )
        ).ToList();
        Debug.Log(
            $"Found {allPrefabs.Length} total prefabs of which {sopAndPrefabTuples.Count} were SimObjPhysics satisfying"
            + $"onlyPickupable=={onlyPickupable} and onlyReceptacles=={onlyReceptacles}."
        );

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        // Note that you cannot (unfortunately) combine the two below loops into
        // a single loop as doing something like
        //   foreach (Transform child in parent.transform) {
        //       DestroyImmediate(child.gameObject);
        //   }
        // Will actually miss a large number of children because the way that looping through
        // parent.transform works (deleting while iterating results in missing elements).
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in parent.transform) {
            toDestroy.Add(child.gameObject);
        }
        foreach (GameObject child in toDestroy) {
            Debug.Log($"Attempting to destroy {child.gameObject}");
            DestroyImmediate(child);
        }

        if (tag != "") {
            tag = $"_{tag}";
        }
        for (int i = 0; i < sopAndPrefabTuples.Count; i++) {
            SimObjPhysics sop = sopAndPrefabTuples[i].Item1;
            string path = sopAndPrefabTuples[i].Item2;
            string hash = CreateSHA256Hash(path).Substring(0, 8);

            GameObject go = (GameObject) PrefabUtility.InstantiatePrefab(sop.gameObject);

            SimObjPhysics newSop = go.GetComponent<SimObjPhysics>();

            if (onlyReceptacles && sop.Type.ToString() == "Box") {
                OpenBoxFlapsMore(newSop);
            }

            FixBoundingBox(newSop);

            go.name = $"{sop.Type.ToString()}{tag}_{hash}";
            go.SetActive(false);
            go.transform.position = new Vector3(-5f, i * 0.05f, -5f);
            go.transform.parent = parent.transform;

            sopAndPrefabTuples[i] = (newSop, path);
        }

        return sopAndPrefabTuples;
    }


}