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
    public class TestProceduralTemplates : TestBaseProcedural
    {

        protected HouseTemplate houseTemplate = new HouseTemplate() {
                    metadata = new HouseMetadata() {
                        schema = ProceduralTools.CURRENT_HOUSE_SCHEMA
                    },
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
                                material = new MaterialProperties() {
                                    color = SerializableColor.fromUnityColor(Color.red),
                                    unlit = true
                                }
                            },
                            floorTemplate = new RoomHierarchy() {
                                floorMaterial = new MaterialProperties() { name = "DarkWoodFloors" },
                                roomType = "Bedroom"
                            },
                            wallHeight = 3.0f
                        }},
                        {"2", new RoomTemplate(){ 
                            wallTemplate = new PolygonWall() {
                                material = new MaterialProperties() {
                                    color = SerializableColor.fromUnityColor(Color.blue),
                                    unlit = true
                                }
                            },
                            floorTemplate = new RoomHierarchy() {
                                floorMaterial = new MaterialProperties() { name = "RedBrick" },
                                roomType = "LivingRoom"
                            },
                            wallHeight = 3.0f
                        }}
                    },
                    doors = new Dictionary<string, Thor.Procedural.Data.Door>() {
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
                        ceilingMaterial = new MaterialProperties() { name = "ps_mat" },
                        floorColliderThickness = 1.0f,
                        receptacleHeight = 0.7f,
                        skyboxId = "Sky1",
                    }
                };

        [UnityTest]
        public IEnumerator TestRooms() {
            // $@"
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 1 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",
            var house = createTestHouse();
           
            Assert.AreEqual(house.rooms.Count, 2);
            var roomIds = new HashSet<string>() { "1", "2"};
            Assert.IsTrue(new HashSet<string>(house.rooms.Select(x => x.id).Intersect(roomIds)).SetEquals(roomIds));
            var room2Poly = new List<Vector3>() {
                Vector3.zero,
                new Vector3(0.0f, 0.0f, 4.0f),
                new Vector3(2.0f, 0.0f, 4.0f),
                new Vector3(2.0f, 0.0f, 0.0f)
            };
            Assert.IsTrue(house.rooms.Find(r => r.id == "2").floorPolygon.Select((p, i) => (point: p, index: i)).All(e => room2Poly.ElementAt(e.index) == e.point));

            var room1Poly = new List<Vector3>() {
                new Vector3(2.0f, 0.0f, 0.0f),
                new Vector3(2.0f, 0.0f, 4.0f),
                new Vector3(4.0f, 0.0f, 4.0f),
                new Vector3(4.0f, 0.0f, 0.0f)
            };
            Assert.IsTrue(house.rooms.Find(r => r.id == "1").floorPolygon.Select((p, i) => (point: p, index: i)).All(e => room1Poly.ElementAt(e.index) == e.point));
            yield return true;
        }

        [UnityTest]
        public IEnumerator TestWalls() {
            // $@"
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 1 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",
            var house = createTestHouse();
            
           
            Assert.AreEqual(house.walls.Count, 8);

            Assert.IsTrue(house.walls.All(w => w.polygon.Max(p => p.y) == houseTemplate.rooms[w.roomId].wallHeight));
            
            var room2WallPoints = new List<(Vector3, Vector3)>() {
                (new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 4.0f)),
                (new Vector3(0.0f, 0.0f, 4.0f), new Vector3(2.0f, 0.0f, 4.0f)),
                (new Vector3(2.0f, 0.0f, 4.0f), new Vector3(2.0f, 0.0f, 0.0f)),
                (new Vector3(2.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f))
            };
            Assert.IsTrue(house.walls.Where(w => w.roomId == "2").Select((wall, index) => (wall, index)).All(e => room2WallPoints.ElementAt(e.index) == (e.wall.polygon[0], e.wall.polygon[1])));

            var room1WallPoints = new List<(Vector3, Vector3)>() {
                (new Vector3(2.0f, 0.0f, 0.0f), new Vector3(2.0f, 0.0f, 4.0f)),
                (new Vector3(2.0f, 0.0f, 4.0f), new Vector3(4.0f, 0.0f, 4.0f)),
                (new Vector3(4.0f, 0.0f, 4.0f), new Vector3(4.0f, 0.0f, 0.0f)),
                (new Vector3(4.0f, 0.0f, 0.0f), new Vector3(2.0f, 0.0f, 0.0f))
            };
            Assert.IsTrue(house.walls.Where(w => w.roomId == "1").Select((wall, index) => (wall, index)).All(e => room1WallPoints.ElementAt(e.index) == (e.wall.polygon[0], e.wall.polygon[1])));
        
            yield return true;
        }


        [UnityTest]
        public IEnumerator TestObjects() {
            // $@"
            //     0 0 0 0 0 0
            //     0 2 2 2 2 0
            //     0 2 2 2 2 0
            //     0 1 1 1 1 0
            //     0 1 * 1 +$ 0
            //     0 0 0 0 0 0
            // "
            var house = createTestHouse();
            
           
            Assert.AreEqual(house.objects.Count, 3);

            var table = house.objects.Find(x => x.id == "*");
            var chair = house.objects.Find(x => x.id == "+");
            var apple = house.objects.Find(x => x.id == "$");

            // TODO add y position of floor to house Template object
            Debug.Log(table.position);
            // Without bounding box center offset
            // Assert.IsTrue(table.position == new Vector3(4.0f, 0.0f, 2.0f));
            var delta = 1e-1;
            Assert.That(Vector3.Distance(table.position, new Vector3(3.7f, 0.8f, 1.7f)), Is.LessThanOrEqualTo(delta));
            Assert.IsTrue(table.assetId == "Dining_Table_16_2");
            Debug.Log(chair.position);
            // Assert.IsTrue(chair.position == new Vector3(4.0f, 0.0f, 4.0f));

            Assert.That(Vector3.Distance(chair.position, new Vector3(3.2f, 1.1f, 3.2f)), Is.LessThanOrEqualTo(delta));
            Assert.IsTrue(chair.assetId == "Chair_007_1");
            Debug.Log(apple.position);
            // Assert.IsTrue(apple.position == new Vector3(4.0f, 2.0f, 4.0f));

            Assert.That(Vector3.Distance(apple.position, new Vector3(3.1f, 2.1f, 3.1f)), Is.LessThanOrEqualTo(delta));
            
            Assert.IsTrue(apple.assetId == "Apple_4");
            Assert.AreEqual(table.room, "1");
            Assert.AreEqual(chair.room, "1");
            Assert.AreEqual(apple.room, "1");
             
            yield return true;
        }

        [UnityTest]
        public IEnumerator TestDoors() {
            // $@"
            //     0 0 0 0 0 0
            //     0 2 2 2 2 0
            //     0 2 2 2 = 0
            //     0 1 1 1 = 0
            //     0 1 1 1 1 0
            //     0 0 0 0 0 0
            // "
            var house = createTestHouse();
            
           
            Assert.AreEqual(house.doors.Count, 1);

            var door = house.doors[0];

            Assert.IsTrue(door.room0 == "1");
            Assert.IsTrue(door.room1 == "2");

            Assert.IsTrue(door.holePolygon[0] == new Vector3(3.0f, 0.0f, 0.0f));

            Assert.AreEqual(
                house.walls.Where(w => w.roomId =="1")
                    .Select(
                        w => (id: w.id, coords: (w.polygon[0], w.polygon[1]))
                    ).ToList()
                    .Find(w => w.coords == (new Vector3(2.0f, 0.0f, 0.0f), new Vector3(2.0f, 0.0f, 4.0f))).id,
                door.wall0
            );

            Assert.AreEqual(
                house.walls.Where(w => w.roomId =="2")
                    .Select(
                        w => (id: w.id, coords: (w.polygon[0], w.polygon[1]))
                    ).ToList()
                    .Find(w => w.coords == (new Vector3(2.0f, 0.0f, 4.0f), new Vector3(2.0f, 0.0f, 0.0f))).id,
                door.wall1
             );

            yield return true;
        }

        [UnityTest]
        public IEnumerator TestCeilings() {
            // $@"
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 1 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",
            var house = createTestHouse();
           
            var room2Poly = new List<Vector3>() {
                Vector3.zero,
                new Vector3(0.0f, 0.0f, 4.0f),
                new Vector3(2.0f, 0.0f, 4.0f),
                new Vector3(2.0f, 0.0f, 0.0f)
            }.Select(v => v + new Vector3(0.0f, houseTemplate.rooms["2"].wallHeight, 0.0f));

            var room2 = house.rooms.Find(r => r.id == "2");
            Assert.IsTrue(room2.ceilings.Count == 1);
            Assert.IsTrue(room2.ceilings[0].material == houseTemplate.proceduralParameters.ceilingMaterial);
            Assert.IsTrue(room2.ceilings[0].polygon.Select((p, i) => (point: p, index: i)).All(e => room2Poly.ElementAt(e.index) == e.point));

            var room1Poly = new List<Vector3>() {
                new Vector3(2.0f, 0.0f, 0.0f),
                new Vector3(2.0f, 0.0f, 4.0f),
                new Vector3(4.0f, 0.0f, 4.0f),
                new Vector3(4.0f, 0.0f, 0.0f)
            }.Select(v => v + new Vector3(0.0f, houseTemplate.rooms["1"].wallHeight, 0.0f));

            var room1 = house.rooms.Find(r => r.id == "1");
            Assert.IsTrue(room1.ceilings.Count == 1);
            Assert.IsTrue(room1.ceilings[0].material == houseTemplate.proceduralParameters.ceilingMaterial);
            Assert.IsTrue(room1.ceilings[0].polygon.Select((p, i) => (point: p, index: i)).All(e => room1Poly.ElementAt(e.index) == e.point));
            yield return true;
        }

        [UnityTest]
        public IEnumerator TestHouseNullVersion() { 
            Assert.That(() => {
                var house = createTestHouse();
                house.metadata.schema = null;
                Debug.Log(house.metadata.schema);
                ProceduralTools.CreateHouse(house, ProceduralTools.GetMaterials());
            }, Throws.ArgumentException);
            yield return true;
        }

        [UnityTest]
        public IEnumerator TestHouseLowerVersion() { 
            Assert.That(() => {
                var house = createTestHouse();
                house.metadata.schema = "0.0.1";
                Debug.Log(house.metadata.schema);
                ProceduralTools.CreateHouse(house, ProceduralTools.GetMaterials());
            }, Throws.ArgumentException);
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


