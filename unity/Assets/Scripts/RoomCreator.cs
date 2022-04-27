// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

// TODO move to editor after fixing multiple assemblies problem
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

public static class IEnnumerableExtension {
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences) {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item })
            );
    }

    // public static IEnumerable<T> IndexOf() {

    // }
}
namespace Thor.Procedural {
    using Data;

    public class RoomCreator : MonoBehaviour {

        [SerializeField]
        private Mesh floorMesh;
        [SerializeField]
        private Material floorMaterial;
        private Material wallMaterial;

        private GameObject floorGameObject;
        public void Start() {

            var position = new Vector3(0.0f, 0.0f, 0.0f);
            //floorGameObject = ProceduralTools.createFloorGameObject();

            var width = 10.0f;
            var depth = 5.0f;

            var colliderThickness = 1.0f;

            var receptacleHeight = 0.7f;

            var wallHeight = 3.0f;
            var wallThickness = 0.25f;

            var offsetX = width / 2.0f;
            var offsetZ = depth / 2.0f;
            var height = 0.0f;
            var simObjId = "FloorGen|+00.00|+00.00|+00.00";

            // var floorMaterialId = "THORKEA_Carpet_Mat";
            // var wallMaterialId = "THORKEA_Wall_Panel_Fabric_Mat";
            var floorMaterialId = "DarkWoodFloors";
            var wallMaterialId = "DrywallOrange";

            var marginX = 0.1f;
            var marginZ = 0.1f;


            //new RoomCreatorFactory(materials, prefabs);

            var floor = new RectangleFloor() {
                center = Vector3.zero,
                width = width,
                depth = depth,
                marginWidth = marginX,
                marginDepth = marginZ,
                materialId = floorMaterialId
            };
            var room = new RectangleRoom() {
                rectangleFloor = floor,
                walls = RectangleFloor.createSurroundingWalls(floor, wallMaterialId, wallHeight, wallThickness)
            };

            // Old constructor style
            // var room = new RectangleRoom(Vector3.zero, width, depth, marginX, marginZ, wallHeight, wallThickness, "", "DrywallOrange");

            var room2 = RectangleRoom.roomFromWallPoints(new Vector3[]{
            new Vector3(-offsetX, height, -offsetZ),


            new Vector3(-offsetX, height, offsetZ),

            new Vector3(offsetX, height, offsetZ),
            new Vector3(offsetX, height, offsetZ / 2.0f),

            new Vector3(offsetX+2.0f, height, offsetZ / 2.0f),

            new Vector3(offsetX+2.0f, height, offsetZ / 2.0f - 3.0f),


            new Vector3(offsetX, height, offsetZ / 2.0f - 3.0f),


            // new Vector3(offsetX/2.0f, height, offsetZ - 0.1f),
            // new Vector3(offsetX/2.0f - 0.2f, height, offsetZ - 0.1f),

            // new Vector3(offsetX/2.0f - 0.2f, height, offsetZ),
            new Vector3(offsetX, height, -offsetZ)
        },
            wallHeight, wallThickness, floorMaterialId, wallMaterialId);

            //var mats = new AssetMap<Material>(ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));

            var assetDB = FindObjectOfType<ProceduralAssetDatabase>();
            var mats = new AssetMap<Material>(assetDB.materials.GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));
            Debug.Log($"null? {assetDB == null} assetc {assetDB.totalMats}");
            Debug.Log($"null? {assetDB == null} Got {mats.Count()} matetials");
            var prefabs = assetDB.prefabs;

            // var arrayMap = new int[][] {
            // 	new int[]{1, 1, 1, 1, 1, 1, 1},
            // 	new int[]{1, 2, 2, 2, 2, 2, 1},
            // 	new int[]{1, 2, 2, 2, 2, 2, 1},
            // 	new int[]{1, 2, 2, 2, 2, 2, 1},
            // 	new int[]{1, 1, 2, 2, 2, 2, 1},
            // 	new int[]{1, 1, 2, 2, 2, 1, 1},
            // 	new int[]{1, 1, 1, 1, 1, 1, 1},
            // };

            var arrayMap = new int[][] {
                new int[]{1, 1, 1, 1, 1, 1, 1},
                new int[]{1, 2, 2, 2, 3, 3, 1},
                new int[]{1, 2, 2, 2, 3, 3, 1},
                new int[]{1, 2, 2, 2, 2, 2, 1},
                new int[]{1, 1, 2, 2, 2, 2, 1},
                new int[]{1, 1, 2, 2, 2, 1, 1},
                new int[]{1, 1, 1, 1, 1, 1, 1},
            };


            var arrayMap2 = new int[][] {
                new int[]{1, 1, 1, 1, 1, 1, 1, 1},
                new int[]{1, 3, 3, 3, 4, 4, 4, 4},
                new int[]{1, 3, 3, 3, 4, 4, 4, 4},
                new int[]{1, 3, 3, 3, 1, 4, 4, 4},
                new int[]{1, 3, 3, 3, 1, 4, 4, 4},
                new int[]{1, 2, 2, 2, 2, 2, 2, 2},
                new int[]{1, 2, 2, 2, 2, 2, 2, 2},
                new int[]{1, 2, 2, 2, 2, 2, 2, 2},
            };

            var arrayMap3 = new int[][] {
                new int[]{1, 1, 1, 1, 1, 1, 1},
                new int[]{1, 1, 1, 1, 1, 1, 1},
                new int[]{1, 1, 1, 1, 1, 1, 1},
                new int[]{1, 1, 1, 1, 1, 1, 1},
                new int[]{1, 2, 2, 2, 2, 2, 1},
                new int[]{1, 2, 2, 2, 2, 2, 1},
                new int[]{1, 1, 1, 1, 1, 1, 1},
            };

            // var roomsToWalls = ProceduralTools.createRoomsFromGenerationArray(arrayMap2);


            // ProceduralTools.roomFromWallIndexDictionary(
            // 	"Floor",
            // 	roomsToWalls.Where(entry => entry.Key == 4).ToDictionary(m => m.Key, m => m.Value),
            // 	arrayMap2.Length,
            // 	arrayMap2[0].Length,
            // 	height,
            // 	wallHeight,
            // 	0.0f,
            // 	new Dictionary<int, (string wallMaterial, string floorMaterial)>() {
            // 		{2, (wallMaterialId, floorMaterialId)},
            // 		{3, (wallMaterialId, floorMaterialId)},
            // 		{4, (wallMaterialId, floorMaterialId)}
            // 	},
            // 	mats,
            // 	scale: new Vector2(6.0f, 6.0f)
            // );

            // ProceduralTools.roomFromWallIndexDictionary(
            // 	"Floor",
            // 	roomsToWalls.Where(entry => entry.Key == 2).ToDictionary(m => m.Key, m => m.Value),
            // 	arrayMap2.Length,
            // 	arrayMap2[0].Length,
            // 	height,
            // 	wallHeight,
            // 	wallThickness,
            // 	new Dictionary<int, (string wallMaterial, string floorMaterial)>() {
            // 		{2, (wallMaterialId, floorMaterialId)},
            // 		{3, (wallMaterialId, floorMaterialId)},
            // 		{4, (wallMaterialId, floorMaterialId)}
            // 	},
            // 	mats,
            // 	scale: new Vector2(6.0f, 6.0f)
            // );


            // For object spawning
            // ProceduralTools.spawnObjectAtReceptacle(prefabs, "Coffee_Table_211_1", floorGameObject.GetComponentInChildren<SimObjPhysics>(), new Vector3(0, 3, 0));


        }

        private GameObject setReceptacle(
            GameObject floorGameObject, float width, float depth, float height, float marginX, float marginZ, float offsetX, float offsetZ
        ) {
            var receptacleTriggerBox = new GameObject("ReceptacleTriggerBox");
            var receptacleCollider = receptacleTriggerBox.AddComponent<BoxCollider>();
            receptacleCollider.isTrigger = true;
            var widthMinusMargin = width - 2.0f * marginX;

            var depthMinusMargin = depth - 2.0f * marginZ;

            receptacleCollider.size = new Vector3(widthMinusMargin, height, depthMinusMargin);
            //var offsetRatioX= offsetX / width;
            //var offsetRatioZ= offsetZ / depth;
            receptacleCollider.center = new Vector3(width / 2.0f - offsetX, height / 2.0f, depth / 2.0f - offsetZ);

            receptacleTriggerBox.transform.parent = floorGameObject.transform;
            return receptacleTriggerBox;
        }

        private GameObject setCollider(GameObject floorGameObject, float width, float depth, float offsetX, float offsetZ, float thickness) {
            var colliders = new GameObject("Colliders");

            var collider = new GameObject("Col");
            var box = collider.AddComponent<BoxCollider>();

            var center = new Vector3(width / 2.0f - offsetX, -thickness / 2.0f, depth / 2.0f - offsetZ);
            var size = new Vector3(width, thickness, depth);
            // TODO set collider params
            box.size = new Vector3(width, thickness, depth);
            box.center = center;
            collider.transform.parent = colliders.transform;

            colliders.transform.parent = floorGameObject.transform;

            var triggerColliders = new GameObject("TriggerColliders");
            var triggerCollider = new GameObject("Col");
            var triggerBox = triggerCollider.AddComponent<BoxCollider>();

            triggerBox.center = center;
            triggerBox.size = size;
            triggerBox.isTrigger = true;

            triggerCollider.transform.parent = triggerColliders.transform;
            triggerColliders.transform.parent = floorGameObject.transform;

            return collider;
        }


        private GameObject createWall(GameObject structure, float height, Vector3 p1, Vector3 p2) {

            var wall = new GameObject("Wall");

            var meshF = wall.AddComponent<MeshFilter>();
            var meshC = wall.AddComponent<MeshCollider>();

            var mesh = meshF.mesh;

            var p1p2 = (p2 - p2);

            var normal = Vector3.Cross(p1p2, Vector3.up);

            mesh.vertices = new Vector3[] { p1, p1 + new Vector3(0.0f, height, 0.0f), p2 + new Vector3(0.0f, height, 0.0f), p2 };
            mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new Vector3[] { normal, normal, normal, normal };

            //mesh.vertices 

            var meshRenderer = wall.AddComponent<MeshRenderer>();
            meshRenderer.material = wallMaterial;

            wall.transform.parent = structure.transform;


            return wall;
        }

        //public enum WallType

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Procedural/Build Asset Database")]
        public static void BuildAssetDB() {
            var proceduralADB = GameObject.FindObjectOfType<ProceduralAssetDatabase>();
            // proceduralADB.prefabs = new AssetMap<GameObject>(ProceduralTools.FindPrefabsInAssets().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));
            // proceduralADB.materials = new AssetMap<Material>(ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));

            proceduralADB.prefabs = ProceduralTools.FindPrefabsInAssets();
            proceduralADB.materials = ProceduralTools.FindAssetsByType<Material>();
            proceduralADB.totalMats = proceduralADB.materials.Count();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
#endif

    }



}