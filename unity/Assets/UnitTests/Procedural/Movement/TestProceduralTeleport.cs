using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Thor.Procedural;
using Thor.Procedural.Data;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests
{
    public class TestProceduralTeleport : TestBaseProcedural
    {
        protected HouseTemplate houseTemplate = new HouseTemplate()
        {
            metadata = new HouseMetadata() { schema = ProceduralTools.CURRENT_HOUSE_SCHEMA },
            id = "house_0",
            layout =
                $@"
                        0 0 0 0 0 0
                        0 2 2 2 2 0
                        0 2 2 2 2 0
                        0 1 1 1 1 0
                        0 1 1 1 1 0
                        0 0 0 0 0 0
                    ",
            objectsLayouts = new List<string>()
            {
                $@"
                            0 0 0 0 0 0
                            0 2 2 2 2 0
                            0 2 2 2 = 0
                            0 1 1 1 = 0
                            0 1 * 1 + 0
                            0 0 0 0 0 0
                        ",
                $@"
                            0 0 0 0 0 0
                            0 2 2 2 2 0
                            0 2 2 2 2 0
                            0 1 1 1 1 0
                            0 1 1 1 $ 0
                            0 0 0 0 0 0
                        "
            },
            rooms = new Dictionary<string, RoomTemplate>()
            {
                {
                    "1",
                    new RoomTemplate()
                    {
                        wallTemplate = new PolygonWall()
                        {
                            material = new MaterialProperties()
                            {
                                color = SerializableColor.fromUnityColor(Color.red),
                                unlit = true
                            }
                        },
                        floorTemplate = new RoomHierarchy()
                        {
                            floorMaterial = new MaterialProperties() { name = "DarkWoodFloors" },
                            roomType = "Bedroom"
                        },
                        wallHeight = 3.0f
                    }
                },
                {
                    "2",
                    new RoomTemplate()
                    {
                        wallTemplate = new PolygonWall()
                        {
                            material = new MaterialProperties()
                            {
                                color = SerializableColor.fromUnityColor(Color.blue),
                                unlit = true
                            }
                        },
                        floorTemplate = new RoomHierarchy()
                        {
                            floorMaterial = new MaterialProperties() { name = "RedBrick" },
                            roomType = "LivingRoom"
                        },
                        wallHeight = 3.0f
                    }
                }
            },
            doors = new Dictionary<string, Thor.Procedural.Data.Door>()
            {
                {
                    "=",
                    new Thor.Procedural.Data.Door()
                    {
                        openness = 1.0f,
                        assetId = "Doorway_1",
                        room0 = "1"
                    }
                }
            },
            objects = new Dictionary<string, HouseObject>()
            {
                {
                    "*",
                    new HouseObject()
                    {
                        assetId = "Dining_Table_16_2",
                        rotation = new FlexibleRotation()
                        {
                            axis = new Vector3(0, 1, 0),
                            degrees = 90
                        }
                    }
                },
                {
                    "+",
                    new HouseObject() { assetId = "Chair_007_1" }
                },
                {
                    "$",
                    new HouseObject() { assetId = "Apple_4", position = new Vector3(0, 2, 0) }
                }
            },
            proceduralParameters = new ProceduralParameters()
            {
                ceilingMaterial = new MaterialProperties() { name = "ps_mat" },
                floorColliderThickness = 1.0f,
                receptacleHeight = 0.7f,
                skyboxId = "Sky1",
            }
        };

        [UnityTest]
        public IEnumerator TestTeleport()
        {
            // House Layout:
            // $@"
            // 0 0 0 0 0 0
            // 0 2 2 2 2 0
            // 0 2 2 2 2 0
            // 0 1 1 1 1 0
            // 0 1 1 1 1 0
            // 0 0 0 0 0 0
            // ",

            var house = Templates.createHouseFromTemplate(this.houseTemplate);

            yield return step(
                new Dictionary<string, object>()
                {
                    { "gridSize", 0.25f },
                    { "agentCount", 1 },
                    { "fieldOfView", 90f },
                    { "snapToGrid", false },
                    { "procedural", true },
                    { "action", "Initialize" },
                    { "agentMode", "stretch" }
                }
            );

            yield return new WaitForSeconds(2f);
            Debug.Log($"ActionSuccess: {lastActionSuccess}");

            yield return step(
                new Dictionary<string, object>() { { "house", house }, { "action", "CreateHouse" } }
            );
            Debug.Log($"ActionSuccess: {lastActionSuccess}");

            yield return step(
                new Dictionary<string, object>()
                {
                    { "action", "TeleportFull" },
                    { "position", new Vector3(3f, 0.91f, 1.0f) }, //adjusting Y value to be within the error (0.05) of the floor.
                    { "rotation", new Vector3(0f, 180f, 0f) },
                    { "horizon", -20f },
                }
            );

            Debug.Log($"ActionSuccess: {lastActionSuccess}");

            //yield return new WaitForSeconds(60f);

            // TODO add back assert as it is failing without forceAction
            Assert.IsTrue(lastActionSuccess);

            //Assert.Tr(cache.priorityMinValue, minRankingVal);lastActionSuccess

            // Debug.Log("180 rot_manipulator " + GameObject.Find("stretch_robot_pos_rot_manipulator").transform.eulerAngles.y);



            yield return true;
        }
    }
}
