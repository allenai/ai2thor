using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using Thor.Procedural;
using Thor.Procedural.Data;
using System.Linq;

namespace Tests {
    public class TestSomething : TestBaseProcedural
    {

        protected HouseTemplate houseTemplate = new HouseTemplate() {
                    id = "house_0",
                    // TODO, some assumptions can be done to place doors and objects in `layout`
                    // and use `objectsLayouts` for any possible inconsistencies or layering instead of being mandatory for objects
                    layout = $@"
                        0 0 0 0 0 0
                        0 2 2 2 2 0
                        0 2 2 2 2 0
                        0 1 1 1 1 0
                        0 1 1 1 1 0
                        0 0 0 0 0 0
                    ",
                    objectsLayouts = new List<string>() {
                        $@"
                            0 0 0 0 0 0
                            0 2 2 2 2 0
                            0 2 2 2 = 0
                            0 1 1 1 = 0
                            0 1 * 1 + 0
                            0 0 0 0 0 0
                        "
                        ,
                        $@"
                            0 0 0 0 0 0
                            0 2 2 2 2 0
                            0 2 2 2 2 0
                            0 1 1 1 1 0
                            0 1 1 1 $ 0
                            0 0 0 0 0 0
                        "
                    },
                    rooms =  new Dictionary<string, RoomTemplate>() {
                        {"1", new RoomTemplate(){ 
                            wallTemplate = new PolygonWall() {
                                color = SerializableColor.fromUnityColor(Color.red),
                                unlit = true
                            },
                            floorTemplate = new RoomHierarchy() {
                                floorMaterial = "DarkWoodFloors",
                                roomType = "Bedroom"
                            },
                            wallHeight = 3.0f
                        }},
                        {"2", new RoomTemplate(){ 
                            wallTemplate = new PolygonWall() {
                                color = SerializableColor.fromUnityColor(Color.blue),
                                unlit = true
                            },
                            floorTemplate = new RoomHierarchy() {
                                floorMaterial = "RedBrick",
                                roomType = "LivingRoom"
                            },
                            wallHeight = 3.0f
                        }}
                    },
                    doors = new Dictionary<string, WallRectangularHole>() {
                        {"=", new Thor.Procedural.Data.Door(){ 
                            openness = 1.0f,
                            assetId = "Doorway_1",
                            room0 = "1"

                        }}
                    },
                    objects = new Dictionary<string, HouseObject>() {
                        {"*", new HouseObject(){ 
                            assetId = "Dining_Table_16_2",
                            rotation = new FlexibleRotation() { axis = new Vector3(0, 1, 0), degrees = 90}
                        }},
                        {"+", new HouseObject(){ 
                            assetId = "Chair_007_1"
                        }},
                        {"$", new HouseObject(){ 
                            assetId = "Apple_4",
                            position = new Vector3(0, 2, 0)
                        }}
                    },
                    proceduralParameters = new ProceduralParameters() {
                        ceilingMaterial = "ps_mat",
                        floorColliderThickness = 1.0f,
                        receptacleHeight = 0.7f,
                        skyboxId = "Sky1",
                    }
                };

        [UnityTest]
        public IEnumerator TestTheThing() {

            // Dictionary<string, object> action = new Dictionary<string, object>();
            // action["action"] = "Initialize";
            // action["fieldOfView"] = 90f;
            // action["snapToGrid"] = true;
            // yield return step(action);

            yield return step(new Dictionary<string, object>() {
                            { "gridSize", 0.25f},
                            { "agentCount", 1},
                            { "fieldOfView", 90f},
                            { "snapToGrid", false},
                            { "procedural", true},
                            { "renderDepthImage", true},
                            { "action", "Initialize"}
                    });

            createTestHouse();

            yield return true;
        }

         protected virtual ProceduralHouse createTestHouse() {
            var house = Templates.createHouseFromTemplate(
                this.houseTemplate
            );

            Debug.Log($"#######   TEST HOUSE:\n {serializeHouse(house)}");
            return house;
        }

        protected string serializeObject(object obj) {
            var jsonResolver = new ShouldSerializeContractResolver();
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                obj,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings() {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    ContractResolver = jsonResolver
                }
            );
        }

        protected string serializeHouse(ProceduralHouse house) {
            var jsonResolver = new ShouldSerializeContractResolver();
            var houseString = Newtonsoft.Json.JsonConvert.SerializeObject(
                house,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings() {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    ContractResolver = jsonResolver
                }
            );

            return houseString;       
        }
    }
}


