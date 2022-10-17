using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class ExperimentRoomSceneManager : MonoBehaviour {
    public GameObject[] replacementObjectsToSpawn = null;
    // set of experiment receptacle objects
    public GameObject[] receptaclesToSpawn = null;
    // screens to place on table
    public GameObject[] screensToSpawn = null;
    // reference to wall renderer
    public Renderer wall;
    // wall materials to swap between
    public Material[] wallMaterials = null;
    // reference to floor renderer
    public Renderer floor;
    // floor materials to swap between
    public Material[] floorMaterials = null;
    // reference to table renderer, material[0] is top, material [1] are legs
    public Renderer table;
    // table top materials to swap between
    public Material[] tableTopMaterials = null;
    // reference to table leg renderer
    public Material[] tableLegMaterials = null;
    // reference to lights in screen
    public GameObject[] allOfTheLights;
    // material screen options
    public Material[] screenMaterials = null;

    // object to spawn
    private SimObjPhysics toSpawn;
    private AgentManager agentManager;
    private PhysicsSceneManager sceneManager;

    // this is the location to spawn object into the scene before positioning them into place
    Vector3 initialSpawnPosition = new Vector3(0, 100, 0);

    // Start is called before the first frame update
    void Start() {
        agentManager = gameObject.GetComponent<AgentManager>();
        sceneManager = gameObject.GetComponent<PhysicsSceneManager>();
    }

    // Update is called once per frame
    void Update() {

    }

#if UNITY_EDITOR
    List<Vector3> debugCoords = new List<Vector3>();
#endif

    // returns grid points where there is an experiment receptacle (screens too) on the table
    // this only returns areas where the ReceptacleTriggerBox of the object is, not the geometry of the object itself
    // use agent's current forward as directionality- agent forward and agent left
    // the y value will be wherever the agent's hand currently is
    // gridIncrement- size between grid points in meters
    // count - casts a grid forward <2 * count + 1> by <count>
    public List<Vector3> ValidGrid(Vector3 origin, float gridIncrement, int count, BaseFPSAgentController agent) {
        // start from origin which will be agent's hand
        List<Vector3> pointsOnGrid = new List<Vector3>();

        for (int i = 0; i < count; i++) {
            // from origin, go gridIncrement a number of times equal to gridDimension
            // in the agent's forward direction
            Vector3 thisPoint = origin + agent.transform.forward * gridIncrement * i;
            pointsOnGrid.Add(thisPoint);

            // then, from this point, go gridDimension times in both left and right direction
            for (int j = 1; j < count + 1; j++) {
                pointsOnGrid.Add(thisPoint + agent.transform.right * gridIncrement * j);
                pointsOnGrid.Add(thisPoint + -agent.transform.right * gridIncrement * j);
            }
        }

        // #if UNITY_EDITOR
        // debugCoords = pointsOnGrid;
        // #endif

        List<Vector3> actualPoints = new List<Vector3>();
        RaycastHit[] hits;

        foreach (Vector3 point in pointsOnGrid) {
            hits = Physics.RaycastAll(
                point + new Vector3(0, 5, 0),
                Vector3.down,
                20.0f,
                LayerMask.GetMask("SimObjInvisible"),
                QueryTriggerInteraction.Collide
            );
            float[] hitDistances = new float[hits.Length];

            for (int i = 0; i < hitDistances.Length; i++) {
                hitDistances[i] = hits[i].distance;
            }

            Array.Sort(hitDistances, hits);

            foreach (RaycastHit h in hits) {
                if (h.transform.GetComponent<SimObjPhysics>()) {
                    var o = h.transform.GetComponent<SimObjPhysics>();
                    if (o.Type != SimObjType.DiningTable && o.Type != SimObjType.Floor) {
                        actualPoints.Add(point);
                    }
                }
            }
        }

        // #if UNITY_EDITOR
        // debugCoords = actualPoints;
        // #endif

        // ok we now have grid points in a grid, now raycast down from each of those and see if we hit a receptacle...
        return actualPoints;

    }
    // change specified screen object's material to color rgb
    public void ChangeScreenColor(SimObjPhysics screen, float r, float g, float b) {
        List<MeshRenderer> renderers = GetAllRenderersOfObject(screen);
        foreach (MeshRenderer sr in renderers) {
            // set first element, the primary mat, of the mat array's color
            sr.material.color = new Color(r / 255f, g / 255f, b / 255f);
        }
    }

    // change specified screen object's material to variation
    public void ChangeScreenMaterial(SimObjPhysics screen, int variation) {
        List<MeshRenderer> renderers = GetAllRenderersOfObject(screen);
        foreach (MeshRenderer sr in renderers) {
            sr.material = screenMaterials[variation];
        }
    }

    public List<MeshRenderer> GetAllRenderersOfObject(SimObjPhysics obj) {
        List<MeshRenderer> renderers = new List<MeshRenderer>();
        renderers.AddRange(obj.transform.gameObject.GetComponentsInChildren<MeshRenderer>());
        return renderers;
    }

    public void ChangeLightColor(float r, float g, float b) {
        foreach (GameObject light in allOfTheLights) {
            light.GetComponent<Light>().color = new Color(r / 255f, g / 255f, b / 255f);
        }
    }

    // 0 to like 5 is reasonable
    public void ChangeLightIntensity(float intensity) {
        foreach (GameObject light in allOfTheLights) {
            light.GetComponent<Light>().intensity = intensity;
        }
    }

    public void ChangeTableTopMaterial(int variation = 0) {
        Material[] mats = table.materials;
        mats[0] = tableTopMaterials[variation];
        table.materials = mats;
    }

    public void ChangeTableTopColor(float r, float g, float b) {
        Material[] mats = table.materials;
        mats[0].color = new Color(r / 255f, g / 255f, b / 255f);
        table.materials = mats;
    }

    public void ChangeTableLegMaterial(int variation = 0) {
        Material[] mats = table.materials;
        mats[1] = tableTopMaterials[variation];
        table.materials = mats;
    }

    public void ChangeTableLegColor(float r, float g, float b) {
        Material[] mats = table.materials;
        mats[1].color = new Color(r / 255f, g / 255f, b / 255f);
        table.materials = mats;
    }

    public void ChangeLightConfig(int variation = 0) {
        // disable all lights
        // enable the specific variation
    }

    // change wall material variation
    public void ChangeWallMaterial(int variation = 0) {
        wall.material = wallMaterials[variation];
    }

    // change wall color r g b
    public void ChangeWallColor(float r, float g, float b) {
        // Color() takes 0-1.0, so yeah convert
        var color = new Color(r / 255f, g / 255f, b / 255f);
        wall.material.color = color;
    }

    // change floor material variation
    public void ChangeFloorMaterial(int variation = 0) {
        floor.material = floorMaterials[variation];
    }

    // change floor color
    public void ChangeFloorColor(float r, float g, float b) {
        // Color() takes 0-1.0, so yeah convert
        var color = new Color(r / 255f, g / 255f, b / 255f);
        floor.material.color = color;
    }

    // return spawn coordinates above the <receptacleObjectId> that the <objectId> will fit at a given rotation <yRot>
    // excludes coordinates that would cause object <objectId> to fall off the table
    public List<Vector3> ReturnValidSpawns(PhysicsRemoteFPSAgentController agent, string objType, int variation, SimObjPhysics targetReceptacle, float yRot = 0) {
        toSpawn = null;

        if (objType == "screen") {
            toSpawn = screensToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if (objType == "receptacle") {
            toSpawn = receptaclesToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        SimObjPhysics spawned = GameObject.Instantiate(toSpawn, initialSpawnPosition, Quaternion.identity);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();

        // apply rotation to object, default quaternion.identity
        spawned.transform.Rotate(new Vector3(0, yRot, 0), Space.Self);

        // generate grid of potential spawn points
        // GetSpawnCoordinatesAboveReceptacle
        List<Vector3> spawnCoordinates = new List<Vector3>();
        spawnCoordinates = agent.getSpawnCoordinatesAboveReceptacle(targetReceptacle);

        List<Vector3> returnCoordinates = new List<Vector3>();

        // try and place object at every spawn coordinate and if it works, add it to the valid coords to return
        for (int i = 0; i < spawnCoordinates.Count; i++) {
            // place object at the given point, then check if the corners are ok
            agent.placeObjectAtPoint(toSpawn, spawnCoordinates[i]);

            List<Vector3> corners = GetCorners(spawned);

            Contains con = targetReceptacle.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
            bool cornerCheck = true;
            foreach (Vector3 p in corners) {
                if (!con.CheckIfPointIsAboveReceptacleTriggerBox(p)) {
                    cornerCheck = false;
                    // this position would cause object to fall off table
                    // double back and reset object to try again with another point
                    spawned.transform.position = initialSpawnPosition;
                    break;
                }
            }

            if (cornerCheck) {
                returnCoordinates.Add(spawnCoordinates[i]);
                // all corners were ok, so add it to the points that are valid
            }

            spawned.transform.position = initialSpawnPosition;
        }

#if UNITY_EDITOR
        // debug draw
        debugCoords = returnCoordinates;
#endif

        Destroy(spawned.transform.gameObject);
        return returnCoordinates;
    }

    // Note: always run ReturnValidSpawns - to get the current scene state's set of useable coordinates for the objType and Variation
    // spawn receptacle/screen <objType> of index [variation] on <targetReceptacle> table object at coordinate <point> 
    // a valid <point> should be generated from the ReturnValidSpawns() return
    public bool SpawnExperimentObjAtPoint(PhysicsRemoteFPSAgentController agent, string objType, int variation, SimObjPhysics targetReceptacle, Vector3 point, float yRot = 0) {
        toSpawn = null;

        bool success = false;

        if (objType == "screen") {
            toSpawn = screensToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if (objType == "receptacle") {
            toSpawn = receptaclesToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if (objType == "replacement") {
            toSpawn = replacementObjectsToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        // instantiate the prefab toSpawn away from every other object
        SimObjPhysics spawned = GameObject.Instantiate(toSpawn, initialSpawnPosition, Quaternion.identity);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        // make sure object doesn't fall until we are done preparing to reposition it on the target receptacle
        rb.isKinematic = true;

        // apply rotation to object, default quaternion.identity
        spawned.transform.Rotate(new Vector3(0, yRot, 0), Space.Self);

        if (agent.placeObjectAtPoint(toSpawn, point)) {
            // we set success to true, if one of the corners doesn't fit on the table
            // this will be switched to false and will be returned at the end
            success = true;

            // double check if all corners of spawned object's bounding box are
            // above the targetReceptacle table
            // note this only accesses the very first receptacle trigger box, so
            // for EXPERIMENT ROOM TABLES make sure there is only one
            // receptacle trigger box on the square table
            List<Vector3> corners = GetCorners(spawned);

            Contains con = targetReceptacle.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
            foreach (Vector3 p in corners) {
                if (!con.CheckIfPointIsAboveReceptacleTriggerBox(p)) {
                    success = false;
                    // this position would cause object to fall off table
                    // double back and reset object to try again with another point
                    spawned.transform.position = initialSpawnPosition;
                    break;
                }
            }
        }

        if (success) {
            rb.isKinematic = false;
            // run scene setup to grab reference to object and give it objectId
            sceneManager.SetupScene();
            sceneManager.ResetObjectIdToSimObjPhysics();
        }

        // no objects could be spawned at any of the spawn points
        // destroy the thing we tried to place on target receptacle
        if (!success) {
            Destroy(spawned.transform.gameObject);
        }

        return success;
    }


    // spawn receptacle/screen <objType> of index [variation] on <targetReceptacle> table object using random seed to pick which spawn coordinate used
    public bool SpawnExperimentObjAtRandom(PhysicsRemoteFPSAgentController agent, string objType, int variation, int seed, SimObjPhysics targetReceptacle, float yRot = 0) {
        toSpawn = null;

        bool success = false;
        // init random state
        UnityEngine.Random.InitState(seed);

        if (objType == "screen") {
            toSpawn = screensToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if (objType == "receptacle") {
            toSpawn = receptaclesToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        List<Vector3> spawnCoordinates = new List<Vector3>();
        spawnCoordinates = agent.getSpawnCoordinatesAboveReceptacle(targetReceptacle);
        spawnCoordinates.Shuffle_(seed);

        // instantiate the prefab toSpawn away from every other object
        SimObjPhysics spawned = GameObject.Instantiate(toSpawn, initialSpawnPosition, Quaternion.identity);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        // make sure object doesn't fall until we are done preparing to reposition it on the target receptacle
        rb.isKinematic = true;

        // apply rotation to object, default quaternion.identity
        spawned.transform.Rotate(new Vector3(0, yRot, 0), Space.Self);

        for (int i = 0; i < spawnCoordinates.Count; i++) {
            // place object at the given point, this also checks the spawn area to see if its clear
            // if not clear, it will return false
            if (agent.placeObjectAtPoint(toSpawn, spawnCoordinates[i])) {
                // we set success to true, if one of the corners doesn't fit on the table
                // this will be switched to false and will be returned at the end
                success = true;

                // double check if all corners of spawned object's bounding box are
                // above the targetReceptacle table
                // note this only accesses the very first receptacle trigger box, so
                // for EXPERIMENT ROOM TABLES make sure there is only one
                // receptacle trigger box on the square table
                List<Vector3> corners = GetCorners(spawned);

                Contains con = targetReceptacle.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
                bool cornerCheck = true;
                foreach (Vector3 p in corners) {
                    if (!con.CheckIfPointIsAboveReceptacleTriggerBox(p)) {
                        cornerCheck = false;
                        // this position would cause object to fall off table
                        // double back and reset object to try again with another point
                        spawned.transform.position = initialSpawnPosition;
                        break;
                    }
                }

                if (!cornerCheck) {
                    success = false;
                    continue;
                }
            }

            // if all corners were succesful, break out of this loop, don't keep trying
            if (success) {
                rb.isKinematic = false;
                // run scene setup to grab reference to object and give it objectId
                sceneManager.SetupScene();
                sceneManager.ResetObjectIdToSimObjPhysics();
                break;
            }
        }

        // no objects could be spawned at any of the spawn points
        // destroy the thing we tried to place on target receptacle
        if (!success) {
            Destroy(spawned.transform.gameObject);
        }

        return success;
    }

    // helper function to return world coordinates of all 8 corners of a
    // sim object's bounding box
    private List<Vector3> GetCorners(SimObjPhysics sop) {
        // get corners of the bounding box of the object spawned in
        GameObject bb = sop.BoundingBox.transform.gameObject;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);
        // keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        // bottom forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        // bottom forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        // bottom back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        // bottom back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        // top forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        // top forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        // top back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));
        // top back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));

        return corners;
    }

#if UNITY_EDITOR
    void OnDrawGizmos() {
        foreach (Vector3 v in debugCoords) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(v, 0.05f);
        }
    }
#endif
}
