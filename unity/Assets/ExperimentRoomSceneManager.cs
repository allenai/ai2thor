using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class ExperimentRoomSceneManager : MonoBehaviour
{

    //set of experiment receptacle objects
    public GameObject[] receptaclesToSpawn = null;

    //screens to place on table
    public GameObject[] screensToSpawn = null;

    //reference to wall renderer
    public Renderer wall;

    //wall materials to swap between
    public Material[] wallMaterials = null;

    //wall material color selector
    public Color wallColor = Color.white;

    //reference to floor renderer
    public Renderer floor;

    //floor materials to swap between
    public Material[] floorMaterials = null;

    //floor material color selector
    public Color floorColor = Color.white;

    //the target table to spawn stuff on in the experiment room
    // [SerializeField]
    // public SimObjPhysics targetReceptacle;

    //object to spawn
    public SimObjPhysics toSpawn;

    private AgentManager agentManager;
    private PhysicsSceneManager sceneManager;

    //this is the location to spawn object into the scene before positioning them into place
    Vector3 initialSpawnPosition = new Vector3(0, 100, 0);

    // Start is called before the first frame update
    void Start()
    {
        agentManager = gameObject.GetComponent<AgentManager>();
        sceneManager = gameObject.GetComponent<PhysicsSceneManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #if UNITY_EDITOR
    List<Vector3> debugCoords = new List<Vector3>();
    #endif


    //change wall material variation
    public void ChangeWallMaterial(int variation = 0)
    {
        wall.material = wallMaterials[variation];
    }

    //change wall color r g b
    public void ChangeWallColor(float r, float g, float b)
    {
        //Color() takes 0-1.0, so yeah convert
        var color = new Color(r/255f, g/255f, b/255f);
        wall.material.color = color;
    }

    //change floor material variation
    public void ChangeFloorMaterial(int variation = 0)
    {
        floor.material = floorMaterials[variation];
    }

    //change floor color
    public void ChangeFloorColor(float r, float g, float b)
    {
        //Color() takes 0-1.0, so yeah convert
        var color = new Color(r/255f, g/255f, b/255f);
        floor.material.color = color;
    }

    //return spawn coordinates above the <receptacleObjectId> that the <objectId> will fit at a given rotation <yRot>
    //excludes coordinates that would cause object <objectId> to fall off the table
    public List<Vector3> ReturnValidSpawns(string objType, int variation, SimObjPhysics targetReceptacle, float yRot = 0)
    {
        toSpawn = null;

        if(objType == "screen")
        {
            toSpawn = screensToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if(objType == "receptacle")
        {
            toSpawn = receptaclesToSpawn[variation].GetComponent<SimObjPhysics>();
        }
        
        SimObjPhysics spawned = Object.Instantiate(toSpawn, initialSpawnPosition, Quaternion.identity);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();

        //apply rotation to object, default quaternion.identity
        spawned.transform.Rotate(new Vector3(0, yRot, 0), Space.Self);

        //generate grid of potential spawn points
        //GetSpawnCoordinatesAboveReceptacle
        List<Vector3> spawnCoordinates = new List<Vector3>();
        PhysicsRemoteFPSAgentController fpsAgent = agentManager.ReturnPrimaryAgent().GetComponent<PhysicsRemoteFPSAgentController>();
        spawnCoordinates = fpsAgent.GetSpawnCoordinatesAboveReceptacle(targetReceptacle);

        List<Vector3> returnCoordinates = new List<Vector3>();

        //try and place object at every spawn coordinate and if it works, add it to the valid coords to return
        for(int i = 0; i < spawnCoordinates.Count; i++)
        {
            //place object at the given point, then check if the corners are ok
            fpsAgent.PlaceObjectAtPoint(toSpawn, spawnCoordinates[i]);

            List<Vector3> corners = GetCorners(spawned);

            Contains con = targetReceptacle.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
            bool cornerCheck = true;
            foreach(Vector3 p in corners)
            {
                if(!con.CheckIfPointIsAboveReceptacleTriggerBox(p))
                {
                    cornerCheck = false;
                    //this position would cause object to fall off table
                    //double back and reset object to try again with another point
                    spawned.transform.position = initialSpawnPosition;
                    break;
                }
            }

            if(cornerCheck)
            {
                returnCoordinates.Add(spawnCoordinates[i]);
                //all corners were ok, so add it to the points that are valid
            }

            spawned.transform.position = initialSpawnPosition;
        }

        #if UNITY_EDITOR
        //debug draw
        debugCoords = returnCoordinates;
        #endif

        Destroy(spawned.transform.gameObject);
        return returnCoordinates;
    }

    //Note: always run ReturnValidSpawns - to get the current scene state's set of useable coordinates for the objType and Variation
    //spawn receptacle/screen <objType> of index [variation] on <targetReceptacle> table object at coordinate <point> 
    //a valid <point> should be generated from the ReturnValidSpawns() return
    public bool SpawnExperimentObjAtPoint(string objType, int variation, SimObjPhysics targetReceptacle, Vector3 point, float yRot = 0)
    {
        toSpawn = null;

        bool success = false;

        if(objType == "screen")
        {
            toSpawn = screensToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if(objType == "receptacle")
        {
            toSpawn = receptaclesToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        //instantiate the prefab toSpawn away from every other object
        SimObjPhysics spawned = Object.Instantiate(toSpawn, initialSpawnPosition, Quaternion.identity);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        //make sure object doesn't fall until we are done preparing to reposition it on the target receptacle
        rb.isKinematic = true;

        //apply rotation to object, default quaternion.identity
        spawned.transform.Rotate(new Vector3(0, yRot, 0), Space.Self);

        PhysicsRemoteFPSAgentController fpsAgent = agentManager.ReturnPrimaryAgent().GetComponent<PhysicsRemoteFPSAgentController>();
        if(fpsAgent.PlaceObjectAtPoint(toSpawn, point))
        {
            //we set success to true, if one of the corners doesn't fit on the table
            //this will be switched to false and will be returned at the end
            success = true;

            //double check if all corners of spawned object's bounding box are
            //above the targetReceptacle table
            //note this only accesses the very first receptacle trigger box, so
            //for EXPERIMENT ROOM TABLES make sure there is only one
            //receptacle trigger box on the square table
            List<Vector3> corners = GetCorners(spawned);

            Contains con = targetReceptacle.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
            foreach(Vector3 p in corners)
            {
                if(!con.CheckIfPointIsAboveReceptacleTriggerBox(p))
                {
                    success = false;
                    //this position would cause object to fall off table
                    //double back and reset object to try again with another point
                    spawned.transform.position = initialSpawnPosition;
                    break;
                }
            }
        }

        if(success)
        {
            rb.isKinematic = false;
            //run scene setup to grab reference to object and give it objectId
            sceneManager.SetupScene();
            sceneManager.ResetObjectIdToSimObjPhysics();
        }
        
        //no objects could be spawned at any of the spawn points
        //destroy the thing we tried to place on target receptacle
        if(!success)
        {
            Destroy(spawned.transform.gameObject);
        }

        return success;
    }


    //spawn receptacle/screen <objType> of index [variation] on <targetReceptacle> table object using random seed to pick which spawn coordinate used
    public bool SpawnExperimentObjAtRandom(string objType, int variation, int seed, SimObjPhysics targetReceptacle, float yRot = 0)
    {
        toSpawn = null;

        bool success = false;
        //init random state
        Random.InitState(seed);

        if(objType == "screen")
        {
            toSpawn = screensToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        if(objType == "receptacle")
        {
            toSpawn = receptaclesToSpawn[variation].GetComponent<SimObjPhysics>();
        }

        List<Vector3> spawnCoordinates = new List<Vector3>();
        PhysicsRemoteFPSAgentController fpsAgent = agentManager.ReturnPrimaryAgent().GetComponent<PhysicsRemoteFPSAgentController>();
        spawnCoordinates = fpsAgent.GetSpawnCoordinatesAboveReceptacle(targetReceptacle);
        spawnCoordinates.Shuffle_(seed);

        //instantiate the prefab toSpawn away from every other object
        SimObjPhysics spawned = Object.Instantiate(toSpawn, initialSpawnPosition, Quaternion.identity);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        //make sure object doesn't fall until we are done preparing to reposition it on the target receptacle
        rb.isKinematic = true;

        //apply rotation to object, default quaternion.identity
        spawned.transform.Rotate(new Vector3(0, yRot, 0), Space.Self);

        for(int i = 0; i < spawnCoordinates.Count; i++)
        {
            //place object at the given point, this also checks the spawn area to see if its clear
            //if not clear, it will return false
            if(fpsAgent.PlaceObjectAtPoint(toSpawn, spawnCoordinates[i]))
            {
                //we set success to true, if one of the corners doesn't fit on the table
                //this will be switched to false and will be returned at the end
                success = true;

                //double check if all corners of spawned object's bounding box are
                //above the targetReceptacle table
                //note this only accesses the very first receptacle trigger box, so
                //for EXPERIMENT ROOM TABLES make sure there is only one
                //receptacle trigger box on the square table
                List<Vector3> corners = GetCorners(spawned);

                Contains con = targetReceptacle.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
                bool cornerCheck = true;
                foreach(Vector3 p in corners)
                {
                    if(!con.CheckIfPointIsAboveReceptacleTriggerBox(p))
                    {
                        cornerCheck = false;
                        //this position would cause object to fall off table
                        //double back and reset object to try again with another point
                        spawned.transform.position = initialSpawnPosition;
                        break;
                    }
                }

                if(!cornerCheck)
                {
                    success = false;
                    continue;
                }
            }

            //if all corners were succesful, break out of this loop, don't keep trying
            if(success)
            {
                rb.isKinematic = false;
                //run scene setup to grab reference to object and give it objectId
                sceneManager.SetupScene();
                sceneManager.ResetObjectIdToSimObjPhysics();
                break;
            }
        }

        //no objects could be spawned at any of the spawn points
        //destroy the thing we tried to place on target receptacle
        if(!success)
        {
            Destroy(spawned.transform.gameObject);
        }

        return success;
    }

    //helper function to return world coordinates of all 8 corners of a
    //sim object's bounding box
    private List<Vector3> GetCorners(SimObjPhysics sop)
    {
        //get corners of the bounding box of the object spawned in
        GameObject bb = sop.BoundingBox.transform.gameObject;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);
        //keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        //bottom forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        //bottom forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        //bottom back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        //bottom back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        //top forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        //top forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        //top back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));
        //top back right
        corners.Add(bb.transform.TransformPoint(bbCenter+ new Vector3(bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));

        return corners;
    }

    #if UNITY_EDITOR
	void OnDrawGizmos()
	{
        foreach(Vector3 v in debugCoords)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(v, 0.05f);
        }
    }
    #endif
}
