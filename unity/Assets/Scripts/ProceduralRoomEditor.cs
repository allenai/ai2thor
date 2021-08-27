using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using EasyButtons.Editor;
using EasyButtons;
using System;
using Thor.Procedural;
using Thor.Procedural.Data;
using System.Linq;
using System.IO;

[ExecuteInEditMode]
public class ProceduralRoomEditor : MonoBehaviour
{
    private IEnumerable<NamedSimObj> namedSimObjects;
    public ProceduralHouse loadedHouse;
    protected class NamedSimObj {
        public string assetId;
        public string id;
        public SimObjPhysics simObj;
    } 

    [UnityEngine.Header("Loading")]
    public string LoadBasePath = "/Resources/rooms/";
    public string layoutJSONFilename;
    [Button(Expanded=true)]
    public void LoadLayout() {
        var path =  BuildLayoutPath(this.layoutJSONFilename);
        Debug.Log($"Loading: '{path}'");
        var jsonStr = System.IO.File.ReadAllText(path);
        Debug.Log($"json: {jsonStr}");

        JObject obj = JObject.Parse(jsonStr);

        this.loadedHouse = obj.ToObject<ProceduralHouse>();

        var houseObj = ProceduralTools.CreateHouse(
            this.loadedHouse,
            ProceduralTools.GetMaterials()
        );

    }


    [Button] 
    public void AssignIds() {
        if (namedSimObjects == null) {
            var root = GameObject.Find("Objects");
            //var counter = new Dictionary<SimObjType, int>();
            if (root != null) {
                var simobjs = root.transform.GetComponentsInChildren<SimObjPhysics>();
                
                this.namedSimObjects = simobjs
                    .GroupBy(s => s.Type)
                    .SelectMany(objsOfType => objsOfType.Select((simObj, index) => new NamedSimObj {
                        assetId = simObj.gameObject.name.Split('(')[0].TrimEnd(),
                        simObj = simObj,
                        id = $"{Enum.GetName(typeof(SimObjType), simObj.ObjType)}_{index}"
                    })).ToList();
                foreach (var namedObj in this.namedSimObjects) {
                    Debug.Log($" Renaming obj: {namedObj.simObj.gameObject.name} to {namedObj.id}, asset_id: {namedObj.assetId}" );
                    namedObj.simObj.objectID = namedObj.id;
                    namedObj.simObj.gameObject.name = namedObj.id;
                }
                // foreach (var namedObj in this.namedSimObjects) {
                //     Debug.Log($" Renamed obj: {namedObj.simObj.gameObject.name} to {namedObj.id}, asset_id: {namedObj.assetId}" );
                // }
            }
            Debug.Log("--- Ids assigned");
        }
        else {
            Debug.LogError("Ids already assigned!");
        }
    }

     [Button]
    public void ReloadScene() {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
       
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path);
    }


    [Button(Expanded=true)] 
    public void SerializeScene(string outFilename) {
        // var path = BuildLayoutPath(layoutJSONFilename);
        // var jsonStr = System.IO.File.ReadAllText(path);
        // JObject jsonObj = JObject.Parse(jsonStr);
        // this.loadedHouse = jsonObj.ToObject<ProceduralHouse>();

        if (this.loadedHouse != null) {
            var outPath = BuildLayoutPath(outFilename);
            Debug.Log($"Serializing to: '{outFilename}'");
           

            // var house = jsonObj.ToObject<ProceduralHouse>();
            
            
            if (this.namedSimObjects != null) {
                this.loadedHouse.objects = this.namedSimObjects.Select(obj => {
                    Vector3 axis;
                    float degrees; 
                    obj.simObj.transform.rotation.ToAngleAxis(out degrees, out axis);
                    var bb = obj.simObj.AxisAlignedBoundingBox;
                    RaycastHit hit;
                    var didHit = Physics.Raycast(obj.simObj.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 12);
                    string room = "";
                    if (didHit) {
                        room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
                    }
                    Debug.Log(" processing " + obj.assetId);
                    return new HouseObject(){
                        id = obj.id,
                        position = obj.simObj.transform.position,
                        rotation = new AxisAngleRotation() { axis = axis, degrees = degrees },
                        kinematic = (obj.simObj.GetComponentInChildren<Rigidbody>()?.isKinematic).GetValueOrDefault(),
                        bounding_box = new BoundingBox() { min =  bb.center - (bb.size / 2.0f), max = bb.center + (bb.size / 2.0f) },
                        room = room,
                        types = new List<Taxonomy>() { new Taxonomy() { name = Enum.GetName(typeof(SimObjType), obj.simObj.ObjType) } },
                        asset_id = obj.assetId
                    };
                }
                ).ToList();
            }

            GameObject floorRoot; 
            floorRoot = GameObject.Find(loadedHouse.id);
            if (floorRoot == null) {
                floorRoot = GameObject.Find(ProceduralTools.DefaultFloorRootObjectName);
            }

            var roomIdToProps = floorRoot.GetComponentsInChildren<RoomProperties>()
                .ToDictionary(
                    rp => rp.GetComponentInParent<SimObjPhysics>().ObjectID,
                    rp => new {
                        roomProps = rp,
                        simOb = rp.GetComponentInParent<SimObjPhysics>()
            });

            loadedHouse.rooms = loadedHouse.rooms.Select(r => {
                r.type = roomIdToProps[r.id].roomProps.RoomType;
                // TODO add more room annotations here
                return r;
            }).ToList();
            
            var sceneLights = GameObject.Find(ProceduralTools.DefaultLightingRootName).GetComponentsInChildren<Light>().Concat( 
                GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<Light>()
            );
            Debug.Log("Scene light count " + sceneLights.Count());

            var gatheredLights = new List<LightParameters>();

            //this.loadedHouse.procedural_parameters.lights = new List<LightParameters>();

            this.loadedHouse.procedural_parameters.lights = sceneLights.Select(l => {
                 RaycastHit hit;
                    var didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 12);
                    string room = "";
                    if (didHit) {
                        room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
                    }
                    // didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, 1.0f, 1 << 8);
                    string objectLink = "";
                    var parentSim = l.GetComponentInParent<SimObjPhysics>();
                    //SimObjType.Lamp
                    if( parentSim != null) { //( parentSim?.ObjType).GetValueOrDefault() == SimObjType.FloorLamp )
                        objectLink = parentSim.ObjectID;
                    }
                    // if (didHit) {
                    //     objectLink = hit.transform.GetComponentInParent<SimObjPhysics>()?.objectID;
                    // }
                    ShadowParameters sp = null;
                    if (l.shadows != LightShadows.None) {
                        sp = new ShadowParameters() {
                            strength = l.shadowStrength,
                            type = Enum.GetName(typeof(LightShadows), l.shadows),
                            normal_bias = l.shadowNormalBias,
                            bias = l.shadowBias,
                            near_plane = l.shadowNearPlane,
                            resolution = Enum.GetName(typeof(UnityEngine.Rendering.LightShadowResolution), l.shadowResolution)
                        };
                    }
                    return new LightParameters()  {
                        id = l.gameObject.name,
                        room_id = room,
                        type = LightType.GetName(typeof(LightType), l.type),

                        

                        position = l.transform.position,
                        //rotation = AxisAngleRotation.fromQuaternion(l.transform.rotation),
                        intensity = l.intensity,
                        indirect_multiplier = l.bounceIntensity,
                        range = l.range,
                        rgb = new SerializableColor() {r = l.color.r, g = l.color.g, b = l.color.b, a = l.color.a},
                        shadow = sp,
                        object_id = objectLink
                };
            }).ToList();
            
          
          
            

            // var m = sceneLights.Select(l => 
            //      new LightParameters()  {
            //             id = l.gameObject.name,
            //             room_id = "room",
            //             type = LightType.GetName(typeof(LightType), l.type),

                        

            //             position = l.transform.position,
            //             rotation = AxisAngleRotation.fromQuaternion(l.transform.rotation),
            //             intensity = l.intensity,
            //             range = l.range,
            //             rgb = l.color,
            //             shadow = null,
            //             object_id = ""

            //     }
            // ).ToList();
            //Debug.Log(gatheredLights.Count);

            //this.loadedHouse.procedural_parameters.lights = new List<LightParameters>() {gatheredLights[0]};
            //this.loadedHouse.procedural_parameters.lights.

            // this.loadedHouse.procedural_parameters.lights = new List<LightParameters>(gatheredLights.Count);
             //this.loadedHouse.procedural_parameters.lights.AddRange(Enumerable.Repeat(this.loadedHouse.procedural_parameters.lights[0], 12));

            //  this.loadedHouse.procedural_parameters = new ProceduralParameters() {
            //     lights = gatheredLights
            //  };


            // for (int i = 0; i < gatheredLights.Count; i++) {
            //     Debug.Log("Light copy: " + i);
            //     this.loadedHouse.procedural_parameters.lights[i] =gatheredLights[i];
            // }
            //loadedHouse.procedural_parameters.lights = gatheredLights;


            // loadedHouse.procedural_parameters.lights = sceneLights.Select(l => {
            //     RaycastHit hit;
            //     //var didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 12);
            //     string room = "";
            //     // if (didHit) {
            //     //     room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
            //     // }
            //     //didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 8);
            //     string objectLink = "";
            //     // if (didHit) {
            //     //     objectLink = hit.transform.GetComponentInParent<SimObjPhysics>()?.objectID;
            //     // }
            //     ShadowParameters sp = null;
            //     if (l.shadows != LightShadows.None) {
            //         sp = new ShadowParameters() {
            //             strength = l.shadowStrength
            //         };
            //     }
            //     return new LightParameters()  {
            //         id = l.gameObject.name,
            //         room_id = room,
            //         type = Enum.GetName(typeof(LightType), l.type),

                    

            //         position = l.transform.position,
            //         rotation = AxisAngleRotation.fromQuaternion(l.transform.rotation),
            //         intensity = l.intensity,
            //         range = l.range,
            //         rgb = l.color,
            //         shadow = sp,
            //         object_id = objectLink

            // };}).ToList();


            loadedHouse.procedural_parameters.skybox_id = RenderSettings.skybox.name;

            Debug.Log("Lights " + this.loadedHouse.procedural_parameters.lights.Count);

            var jsonResolver = new ShouldSerializeContractResolver();
                    var outJson = JObject.FromObject(this.loadedHouse,
                                new Newtonsoft.Json.JsonSerializer() {
                                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                                    ContractResolver = jsonResolver
                                });
            
            Debug.Log($"output json: {outJson.ToString()}");
            System.IO.File.WriteAllText(outPath, outJson.ToString());
        }
        else {
            Debug.LogError("No loaded layout load a layout first");
        }

    }

    private string BuildLayoutPath(string layoutFilename) {
        layoutFilename = layoutFilename.Trim();
        if (!layoutFilename.EndsWith(".json")) {
            layoutFilename += ".json";
        }
        var path = Application.dataPath + LoadBasePath + layoutFilename;
        return path;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
