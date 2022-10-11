using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using Thor.Procedural;
using Thor.Procedural.Data;
using System.Linq;
using System.IO;
using System.Media;

namespace Tests {
    public class TestRendering : TestBaseProcedural {
        protected HouseTemplate houseTemplate = new HouseTemplate() {
            metadata = new HouseMetadata() {
                schema = "1.0.0"
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
                            0 1 1 1 1 0
                            0 0 0 0 0 0
                        "
                    },
            rooms = new Dictionary<string, RoomTemplate>() {
                        {"1", new RoomTemplate(){
                            wallTemplate = new PolygonWall() {
                                material = new MaterialProperties() {
                                    color = SerializableColor.fromUnityColor(Color.red),
                                    unlit = true
                                }
                            },
                            floorTemplate = new RoomHierarchy() {
                                floorMaterial = new MaterialProperties() { name ="DarkWoodFloors" },
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
            proceduralParameters = new ProceduralParameters() {
                ceilingMaterial = new MaterialProperties() { name = "ps_mat" },
                floorColliderThickness = 1.0f,
                receptacleHeight = 0.7f,
                skyboxId = "Sky1",
            }
        };

        [UnityTest]
        public IEnumerator TestUnlitRgbImage() {
            // $@"
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 1 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",
            var house = createTestHouse();

            yield return step(new Dictionary<string, object>() {
                { "gridSize", 0.25f},
                { "agentCount", 1},
                { "fieldOfView", 45.0f},
                { "snapToGrid", true},
                { "procedural", true},
                { "action", "Initialize"}
            });

            Debug.Log("Pre Agent pos " + this.getLastActionMetadata().agent.position);

            ProceduralTools.CreateHouse(house, ProceduralTools.GetMaterials());

            yield return step(
                new Dictionary<string, object>() {
                    { "position", new Vector3(3.0f, 1.0f, 0.2f)},
                    { "rotation", new Vector3(0, 180, 0)},
                    { "horizon", 0.0f},
                    { "standing", true},
                    { "forceAction", true},
                    { "action", "TeleportFull"}
            });

            var rgbBytes = this.renderPayload.Find(e => e.Key == "image").Value;

            var eps = 40;
            var assertResult = rgbBytes.Select((pixel, index) => (pixel, index))
                .All(
                    e =>
                    (e.index % 3 == 0 && 255 - e.pixel <= eps) ||
                    (e.index % 3 != 0 && e.pixel <= eps)
                );

            if (!assertResult) {
                savePng(rgbBytes, getBuildMachineTestOutputPath("test_comp.png", false));
            }

            // TODO: Disabling test because lighting in build machine is always enabled,
            // requires investigation, probably save the scene in this test with editor tools
            // and examine scene in machine  
            // Unlit red wall
            // Assert.True(
            //    assertResult
            // );
        }

        [UnityTest]
        public IEnumerator TestLitRgbImage() {
            // $@" // A is agent
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 A 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",
            foreach (var room in houseTemplate.rooms.Values) {
                room.wallTemplate.unlit = false;
            }
            houseTemplate.objectsLayouts = houseTemplate.objectsLayouts.Concat(
                new List<string>() {
                        $@"
                            0 0 0 0 0 0
                            0 2 2 2 2 0
                            0 2 2 2 2 0
                            0 1 1 1 1 0
                            0 1 1 1 + 0
                            0 0 0 0 0 0
                        "
                    }
            ).ToList();
            houseTemplate.objects = new Dictionary<string, HouseObject>() {
                {"+", new HouseObject(){
                    assetId = "Chair_007_1",
                    kinematic = true
                }}
            };

            var house = createTestHouse();

            yield return Initialize();

            ProceduralTools.CreateHouse(house, ProceduralTools.GetMaterials());

            Debug.Log($"Window width {Screen.width} height {Screen.height}");
            // Does not work in editor
            Screen.SetResolution(600, 600, false);
            Debug.Log($"After Window width {Screen.width} height {Screen.height}");

            yield return step(
                new Dictionary<string, object>() {
                    { "position", new Vector3(3.0f, 1.0f, 1.0f)},
                    { "rotation", new Vector3(0, 0, 0)},
                    { "horizon", 0.0f},
                    { "standing", true},
                    { "forceAction", true},
                    { "action", "TeleportFull"}
            });

            var rgbBytes = this.renderPayload.Find(e => e.Key == "image").Value;
            //Debug.Break();

            Debug.Log($" path  {getTestResourcesPath("img")}");

            // TODO: when width height are fixed add back images and folder paths
            // savePng(rgbBytes, getTestResourcesPath("test.png", false));
            // var tex = Resources.Load<Texture2D>(getTestResourcesPath("img"));
            // var t = duplicateTexture(tex);
            // t.ReadPixels(agentManager.readPixelsRect, 0 , 0);
            // t.Apply();
            // var compareToPixels = t.GetRawTextureData();;
            // savePng(compareToPixels, getTestResourcesPath("test_comp.png", false));
            // Debug.Log("render size "+ rgbBytes.Count() + " compare size  " + compareToPixels.Count());
            // Debug.Log("template: " + this.serializeObject(houseTemplate));

            // TODO, assertion not working on build machine due to unity's lack of support for determining
            // player screen width in editor and UTF reliance on editor for testing
            // Assert.True(
            //     rgbBytes.Zip(compareToPixels, (pixel, comp) => (pixel, comp))
            //             .All(e => e.pixel == e.comp)
            // );

        }

        Texture2D duplicateTexture(Texture2D source) {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        [UnityTest]

        public IEnumerator TestDepth() {
            // $@" // A is agent
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 A 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",
            Screen.SetResolution(600, 600, false);
            yield return step(new Dictionary<string, object>() {
                { "gridSize", 0.25f},
                { "agentCount", 1},
                { "fieldOfView", 90f},
                { "snapToGrid", false},
                { "procedural", true},
                { "renderDepthImage", true},
                { "action", "Initialize"}
        });
            var house = createTestHouse();
            ProceduralTools.CreateHouse(house, ProceduralTools.GetMaterials());

            yield return step(
                new Dictionary<string, object>() {

                { "position", new Vector3(3.0f, 1.0f, 4.0f)},
                { "rotation", new Vector3(0, 180, 0)},
                { "horizon", 0.0f},
                { "standing", true},
                { "forceAction", true},
                { "action", "TeleportFull"}
            });

            var depth = this.renderPayload.Find(e => e.Key == "image_depth").Value;

            var itemSize = depth.Length / (Screen.width * Screen.height);

            var centerIndex = (Screen.height / 2) + ((Screen.width / 2) * Screen.height);

            Debug.Log($"Screen width: {Screen.width} height: {Screen.height}, Depth Element size: {itemSize}");
            // TODO: Convert byte array into floats
            Debug.Log($"r {depth[centerIndex]} g {depth[centerIndex + 1]} b {depth[centerIndex + 2]} a {depth[centerIndex + 3]} r1 {depth[centerIndex + 4]}");

        }

        protected virtual ProceduralHouse createTestHouse() {
            var house = Templates.createHouseFromTemplate(
                this.houseTemplate
            );
            Debug.Log($"####### TEST HOUSE:\n {serializeHouse(house)}");
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