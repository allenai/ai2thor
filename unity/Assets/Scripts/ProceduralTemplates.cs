using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Thor.Procedural.Data;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Thor.Procedural {

    [Serializable]
    public class HouseTemplate {
        public string id;
        public string layout;
        public HouseMetadata metadata;
        public IEnumerable<string> objectsLayouts;
        public Dictionary<string, RoomTemplate> rooms;
        public Dictionary<string, Thor.Procedural.Data.Door> doors;
        public Dictionary<string, Window> windows;
        public Dictionary<string, HouseObject> objects;
        public ProceduralParameters proceduralParameters;
    }

    [Serializable]
    public class RoomTemplate {
        public PolygonWall wallTemplate;
        public RoomHierarchy floorTemplate;

        public float floorYPosition = 0.0f;
        public float wallHeight = 2.0f;
    }

    public static class Templates {

        public static ProceduralHouse createHouseFromTemplate(
            HouseTemplate houseTemplate,
            int outsideBoundaryId = 0
         
        ) {
            var layout = houseTemplate.layout;
            var objects = houseTemplate.objectsLayouts;
            
            Func<string, int[][]> toIntArray = (string l) => l.Split('\n').Select(x => x.Split(' ').Select(i => {
                int res;
                var isInt = int.TryParse(i, out res);
                return (res, isInt);
            }).Where(m => m.isInt).Select(m => m.res).ToArray()).Where(a => a.Length > 0).ToArray();
            Func<string, string[][]> toStringArray = (string l) => l.Split('\n').Select(x => x.Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(i => i).ToArray()).Where(a => a.Length > 0).ToArray();
            
            var layoutIntArray = toIntArray(layout);

            var rows = layoutIntArray.Length;
            var columns = layoutIntArray[0].Length;

            var layoutStringArray = toStringArray(layout);
            var objectArray = objects.Select(toStringArray).ToArray();

            IEnumerable<KeyValuePair<string, WallRectangularHole>> holePairs = new List<KeyValuePair<string, WallRectangularHole>>();

            if (houseTemplate.doors != null) {
                holePairs = holePairs.Concat(houseTemplate.doors.Select(d => new KeyValuePair<string, WallRectangularHole>(d.Key, d.Value as WallRectangularHole)));
            }
            if (houseTemplate.windows != null) {
                holePairs = holePairs.Concat(houseTemplate.windows.Select(d => new KeyValuePair<string, WallRectangularHole>(d.Key, d.Value as WallRectangularHole)));
            }
            
            var holeTemplates = holePairs.ToDictionary(e => e.Key, e => e.Value);;

            var doorIds = new HashSet<string>(holeTemplates.Keys.Distinct());
            Func<int, int, (int row, int column)[]> manhattanNeighbors = (int row, int column) => new (int row, int column)[]{
                (row - 1, column),
                (row, column - 1),
                (row + 1, column),
                (row, column + 1)
            };
            
            if (layoutIntArray.Select(r => r.Length).Distinct().Count() > 1) {
                throw new ArgumentException("Invalid layout all rows must be the same size");
            }

            var doorDict = new Dictionary<(string id, int layer, (int row, int column) index), HashSet<(int row, int column)>>();
            
            var objectCoordinates = objectArray
                .SelectMany(
                    (objectLayer, layer) => objectLayer
                        .SelectMany((objectRow, row) => 
                            objectRow.Select(
                                (id, column) => {
                                    // ignore no door ones
                                     var isHole = holeTemplates.Keys.Contains(id);
                                    if (isHole) {

                                        var neighborIndices = new List<(int row, int column)>() { (row, column) }.Concat(manhattanNeighbors(row, column));
                                        var holeContinuation = isHole ? 
                                            neighborIndices.Where(n => ProceduralTools.withinArrayBoundary(n, rows, columns) && objectArray[layer][n.row][n.column] == id) 
                                            : new List<(int, int)>();
                                    
                                        var nf = new List<Func<(int, int), (int, int)>>(){
                                            ((int row, int col) c) => (c.row - 1, c.col),
                                            ((int row, int col) c) => (c.row, c.col - 1)
                                    };

                                    var neighborFunctions = neighborIndices.Take(2).Select((index, i) => (index, i)).Where(
                                        v => { 
                                            return ProceduralTools.withinArrayBoundary(v.index, rows, columns) && 
                                             objectArray[layer][v.index.row][v.index.column] == id; // id of door continues in neighbor
                                        // && layoutStringArray[v.index.row][v.index.col] != layoutStringArray[row][column] // room crossing
                                        }
                                    ).Select(tup => nf[tup.i]);

                                    var twoHeaded = false;
                                    if (neighborFunctions.Count() > 1) {
                                        var cornerLeftTopNeighbor = (row: row - 1, column: column - 1);
                                        if (ProceduralTools.withinArrayBoundary(cornerLeftTopNeighbor, rows, columns) && objectArray[layer][cornerLeftTopNeighbor.row][cornerLeftTopNeighbor.column] == id) {
                                            neighborFunctions = new List<Func<(int, int), (int, int)>>(){
                                                ((int row, int col) c) => (c.row - 1, c.col - 1)
                                            };
                                        }
                                        else {
                                            twoHeaded = true;
                                        }
                                    } 
                                
                                    var doorStarts = neighborFunctions.Select( f => 
                                            {
                                                var minIndex = (row, column);
                                                var prevMinIndex = minIndex;
                                                while (ProceduralTools.withinArrayBoundary(minIndex, rows, columns) && objectArray[layer][minIndex.row][minIndex.column] == id) {
                                                    prevMinIndex = minIndex;
                                                    minIndex = f(minIndex);
                                                }
                                                return prevMinIndex;
                                            }
                                        ).ToList();

                                    foreach (var doorStartIndex in doorStarts) {
                                        (string, int, (int, int)) key = (id, layer, doorStartIndex);
                                        if (doorDict.ContainsKey(key)) {
                                            var connected = holeContinuation;
                                            if (!twoHeaded) {
                                                var k = holeContinuation.SelectMany(c => doorDict[(id, layer, c)]);
                                                connected = connected.Concat(k);
                                            }
                                            doorDict[key] = new HashSet<(int, int)>(doorDict[key].Union(connected));
                    
                                        }
                                        else {
                                            doorDict[key] = new HashSet<(int, int)>(holeContinuation);
                                        }
                                    }
                                }
                                    return (id, layer, (row, column), isHole);
                                }
                            )
                )
            ).ToList();
            
            var interiorBoundary = findWalls(layoutIntArray);
            var boundaryGroups = consolidateWalls(interiorBoundary);
            // TODO: do global scaling on everything after room creation
            var floatingPointBoundaryGroups = scaleBoundaryGroups(boundaryGroups, 1.0f, 3);

            var roomIds = layoutIntArray.SelectMany(x => x.Distinct()).Distinct();
            var roomToWallsXZ = getXZRoomToWallDict(floatingPointBoundaryGroups, roomIds);

            var defaultRoomTemplate = getDefaultRoomTemplate();
            var wallCoordinatesToId = new Dictionary<((double, double), (double, double)), string>();
            var roomsWithWalls = roomIds.Where(id => id != outsideBoundaryId).Select(intId => {
                    var id = intId.ToString();
                    RoomTemplate template;
                    var inDict = houseTemplate.rooms.TryGetValue(id, out template);
                    template = inDict ? template : defaultRoomTemplate;
                    var room = template.floorTemplate.DeepClone();
                
                    room.id = roomIntToId(intId, template.floorTemplate);

                    var wallHeight = template.wallHeight;
                    var walls2D = roomToWallsXZ[intId];
                    room.floorPolygon = walls2D.Select(p => new Vector3((float)p.Item1.row, template.floorYPosition, (float)p.Item1.column)).ToList();
                    MaterialProperties ceilingMat = new MaterialProperties();

                    if (room.ceilings == null || room.ceilings.Count < 1 || room.ceilings[0].material == null) {
                        ceilingMat = houseTemplate.proceduralParameters.ceilingMaterial;
                    }
                    else {
                        ceilingMat = room.ceilings[0].material;
                    }
                    room.ceilings = (room.ceilings != null && room.ceilings.Count > 0) ? room.ceilings : new List<Ceiling>() {new Ceiling()};
                    
                    room.ceilings[0].material = ceilingMat;
                    room.ceilings[0].polygon = room.floorPolygon.Select(p => new Vector3(p.x, p.y + wallHeight, p.z)).ToList();

                    var walls = walls2D.Select((w, i) => {
                        var wallCopy = template.wallTemplate.DeepClone();
                        wallCopy.id = wallToId(i, room.id, template.wallTemplate.id);
                        wallCopy.polygon = new List<Vector3>() {
                                new Vector3((float)w.Item1.row, template.floorYPosition, (float)w.Item1.column),
                                new Vector3((float)w.Item2.row, template.floorYPosition, (float)w.Item2.column),
                                new Vector3((float)w.Item2.row, template.floorYPosition + wallHeight, (float)w.Item2.column),
                                new Vector3((float)w.Item1.row, template.floorYPosition + wallHeight, (float)w.Item1.column)
                            };
                        wallCopy.roomId = room.id;
                        wallCoordinatesToId[w] =  wallCopy.id;
                        return wallCopy;
                    }).ToList();
                    return (room, walls);
                }
                
            ).ToList();

            var doorIdCounter = new Dictionary<string, int>();
            var assetMap = ProceduralTools.getAssetMap();
            doorDict = doorDict.GroupBy(d => d.Value, new CustomHashSetEqualityComparer<(int, int)>()).ToDictionary(p => p.First().Key, p => p.Key);// .SelectMany(d => d.Select(x => x)).ToDictionary(p => p.Key, p => p.Value);
            var objectIdCounter = new Dictionary<string, int>();

            // TODO -1 be a padding value as the distance between the first room and the zero padding at the
            // begining of the array in x, z
            var distToZeros = new Vector3(1.0f, 0.0f, 1.0f);

            var holes = doorDict.SelectMany((d, i) => {
                var holeTemplateId = d.Key.id;
                var holeStart = d.Key.index; // d.Value.Min();
                var holeEnd = d.Value.Max();
                WallRectangularHole hole;
                var inDict = holeTemplates.TryGetValue(holeTemplateId, out hole);

                var isDoor = hole is Data.Door;
                var isWindow = hole is Data.Window;
                if (inDict) {
                    if (isDoor) {
                        hole = (hole as Data.Door).DeepClone();
                    }
                    else if (isWindow) {
                        hole = (hole as Data.Window).DeepClone();
                    }
                }
                else {
                    hole = getDefaultHoleTemplate();
                }

                if (hole == null) {
                    return new List<WallRectangularHole>(){};
                }
                
                var index = doorIdCounter.AddCount(holeTemplateId) - 1;

                var room0Id = layoutIntArray[holeStart.row][holeStart.column];
                var room1Id = layoutIntArray[holeEnd.row][holeEnd.column];

                int room0ArrayValue;
                var isInt = int.TryParse(hole.room0, out room0ArrayValue);

                if (!string.IsNullOrEmpty(hole.room0) && isInt && room0ArrayValue == room1Id) {
                    var tmp = room0Id;
                    room0Id = room1Id;
                    room1Id = tmp;

                    var holeTmp =holeStart;
                    holeStart = holeEnd;
                    holeEnd = holeTmp;
                }

                var doorDir = (row: holeEnd.row - holeStart.row, column: holeEnd.column - holeStart.column);

                if (doorDir.row > 1 || doorDir.column > 1) {
                    throw new ArgumentException(" invalid door thickness with id " + d.Key);
                }

                var indexOffset = new Dictionary<(int, int), (int row, int column)>() {
                    {(0, -1), (-1, -1)},
                    {(-1, 0), (-1, 0)},
                    {(0, 1), (-1, 0)},
                    {(1, 0), (0, -1)}
                };

                hole.room0 = roomIntToId(room0Id, houseTemplate.rooms.GetValueOrDefault(room0Id.ToString(), defaultRoomTemplate).floorTemplate);
                hole.room1 = roomIntToId(room1Id, houseTemplate.rooms.GetValueOrDefault(room1Id.ToString(), defaultRoomTemplate).floorTemplate);

                var room0Walls2D = roomToWallsXZ[room0Id];
                var room1Walls2D = roomToWallsXZ[room1Id];

                var holeAdjustOffset = indexOffset[doorDir];

                var holeStartCoords = (row: holeStart.row + holeAdjustOffset.row, column: holeStart.column + holeAdjustOffset.column);

                var wall0 = room0Walls2D.Find( x => {
                    

                    var xDiff = Math.Abs(holeStartCoords.row - x.Item1.row);
                    var zDiff = Math.Abs(holeStartCoords.column  - x.Item1.column);

                    var xDiff1 = Math.Abs(holeStartCoords.row - x.Item2.row);
                    var zDiff1 = Math.Abs(holeStartCoords.column - x.Item2.column);


                    var start = new Vector2((float)x.Item1.column, (float)x.Item1.row);
                    var end = new Vector2((float)x.Item2.column, (float)x.Item2.row);
                    var wallDir = (end - start).normalized;

                    var doorDirVec = new Vector2((float)doorDir.column, (float)doorDir.row).normalized;
                    var dot = Vector2.Dot(wallDir, doorDirVec);
                    // TODO add eps
                    return ((xDiff == 0.0 && xDiff1 == 0.0) || (zDiff == 0.0 && zDiff1 == 0.0)) && Math.Abs(dot) < 1e-4;
                });

                    

                var wall1 = room1Walls2D.Find( x => {
                    var xDiff = Math.Abs(holeStartCoords.row - x.Item1.row);
                    var zDiff = Math.Abs(holeStartCoords.column - x.Item1.column);

                    var xDiff1 = Math.Abs(holeStartCoords.row - x.Item2.row);
                    var zDiff1 = Math.Abs(holeStartCoords.column - x.Item2.column);

                    var start = new Vector2((float)x.Item1.column, (float)x.Item1.row);
                    var end = new Vector2((float)x.Item2.column, (float)x.Item2.row);
                    var wallDir = (end - start).normalized;

                    var doorDirVec = new Vector2((float)doorDir.column, (float)doorDir.row).normalized;
                    var dot = Vector2.Dot(wallDir, doorDirVec);
                    // TODO add eps
                    return ((xDiff == 0.0 && xDiff1 == 0.0) || (zDiff == 0.0 && zDiff1 == 0.0)) && Math.Abs(dot) < 1e-4;
                });
                
                hole.wall0 = wallCoordinatesToId[wall0];
                hole.wall1 = wallCoordinatesToId[wall1];
                // TODO asset offset
                hole.id = holeTemplateIdToHouseId(holeTemplateId, index, hole.id);
                Debug.Log("---- Hole being created " + hole.id);

                if ( string.IsNullOrEmpty(hole.assetId) || !assetMap.ContainsKey(hole.assetId)) {
                    return new List<WallRectangularHole>(){};
                }

                var holeOffset = ProceduralTools.getHoleAssetBoundingBox(hole.assetId);

                 Debug.Log("---- Hole offset null? " + (hole == null));

                if (holeOffset == null) {
                    return new List<WallRectangularHole>(){};
                }

                var floorTemplate = houseTemplate.rooms[room0Id.ToString()];

                // TODO -1 be a padding value as the distance between the first room and the zero padding at the
                // begining of the array in x, z
                var minVector = new Vector3((float)(holeStartCoords.column - distToZeros.x), (float)floorTemplate.floorYPosition, 0.0f);
                var maxVector = minVector + holeOffset.max;
                hole.holePolygon = new List<Vector3> {
                    minVector,
                    maxVector
                };
                var right = (
                    new Vector3((float)wall0.Item2.column, 0.0f, (float)wall0.Item2.row) - 
                    new Vector3((float)wall0.Item1.column, 0.0f, (float)wall0.Item1.row)
                ).normalized;

                var forward = Vector3.Cross(right, Vector3.up);
                Matrix4x4 wallSpace = new Matrix4x4();
                wallSpace.SetColumn(0, right);
                wallSpace.SetColumn(1, Vector3.up);
                wallSpace.SetColumn(2, forward);

                hole.assetPosition = wallSpace * (minVector + holeOffset.offset + (holeOffset.max / 2.0f));
                Debug.Log("---- Hole def being created " + hole.id);
                return new List<WallRectangularHole>(){isDoor ? hole as Data.Door : isWindow ? hole as Data.Window : hole};
            }).ToList();

            objectIdCounter = new Dictionary<string, int>();

            var strRoomIds =  new HashSet<string>(roomIds.Select(x => x.ToString()));
            var houseObjects = objectCoordinates.Where(item => !doorIds.Contains(item.id) && !strRoomIds.Contains(item.id)).SelectMany(item => {
                var coord = item.Item3;
                var result = new List<HouseObject>();

                var template = houseTemplate.objects.GetValueOrDefault(item.id, null);


                
                if (template != null) {
                    var obj = template.DeepClone();

                    var count = objectIdCounter.AddCount(item.id);
                    obj.id = objectToId(item.id, count - 1,  obj.id);
                    var roomId = layoutIntArray[coord.row][coord.column];
                    var floorTemplate = houseTemplate.rooms[roomId.ToString()];
                    obj.room = roomIntToId(roomId, floorTemplate.floorTemplate);

                    if ( string.IsNullOrEmpty(obj.assetId) || !assetMap.ContainsKey(obj.assetId)) {
                        return result;
                    }

                    var asset = assetMap.getAsset(obj.assetId);
                    var simObj = asset.GetComponent<SimObjPhysics>();
                    var bb = simObj.AxisAlignedBoundingBox;

                    var centerOffset =  bb.center + bb.size / 2.0f;

                    // TODO use bounding box to center
                    obj.position = new Vector3((float)coord.row - distToZeros.x + obj.position.x, floorTemplate.floorYPosition + obj.position.y, (float)coord.column - distToZeros.z + obj.position.z) + centerOffset;// - new Vector3(centerOffset.x, -centerOffset.y, centerOffset.z);
                    obj.rotation = obj.rotation == null ? new FlexibleRotation() { axis = new Vector3(0.0f, 1.0f, 0.0f), degrees = 0} : obj.rotation;
                    if (asset != null) {
                        result.Add(obj);
                    }
                }

                return result;
            });

            HouseMetadata metadata = new HouseMetadata {
                schema = ProceduralTools.CURRENT_HOUSE_SCHEMA
            };

            return new ProceduralHouse() {
                metadata = new HouseMetadata() { schema=houseTemplate.metadata.schema },
                proceduralParameters = houseTemplate.proceduralParameters.DeepClone(),
                id = !string.IsNullOrEmpty(houseTemplate.id) ? houseTemplate.id : houseId(),
                rooms = roomsWithWalls.Select(p => p.room).ToList(),
                walls = roomsWithWalls.SelectMany(p => p.walls).ToList(),
                doors = holes.Where(d => d is Data.Door).Select(d => d as Data.Door).ToList(),
                windows = holes.Where(d => d is Data.Window).Select(d => d as Data.Window).ToList(),
                objects = houseObjects.ToList()
            };

        }

         public static RoomTemplate getDefaultRoomTemplate() {
            return new RoomTemplate() {
                wallTemplate = new PolygonWall() {
                    polygon = new List<Vector3>() { new Vector3(0.0f, 3.0f, 0.0f)}
                },
                floorTemplate = new RoomHierarchy() {}
            };
        }

        public static WallRectangularHole getDefaultHoleTemplate() {
            return new Data.Door();
        }

        public static Dictionary<(int, int), List<((int row, int col), (int row, int col))>> findWalls(int[][] floorplan) {
            var walls = new DefaultDictionary<(int, int), List<((int row, int col), (int row, int col))>>();
            for(var row = 0; row < floorplan.Length - 1; row++) {
                for(var col = 0; col < floorplan[row].Length - 1; col++) {
                    var a = floorplan[row][col];
                    var b = floorplan[row][col + 1];
                    if (a != b) {
                        walls[(Math.Min(a, b), Math.Max(a,b))].Add(((row-1, col), (row, col)));
                    }
                    b = floorplan[row+1][col];
                    if (a != b) {
                        walls[(Math.Min(a, b), Math.Max(a,b))].Add(((row, col-1), (row, col)));
                    }
                }
            }

            return walls;
        }


        public static Dictionary<(int, int), HashSet<((int row, int col), (int row, int col))>> consolidateWalls(Dictionary<(int, int), List<((int row, int col), (int row, int col))>> walls) {
            var output = new Dictionary<(int, int), HashSet<((int row, int col), (int row, int col))>>();
            foreach (var item in walls) {
                var wallGroupId = item.Key;
                var wallPairs = item.Value;
                var wallMap = new Dictionary<(int, int), HashSet<(int, int)>>();

                foreach (var wall in wallPairs) {
                    if (!wallMap.ContainsKey(wall.Item1)) {
                        wallMap[wall.Item1] = new HashSet<(int, int)>();
                    }
                    wallMap[wall.Item1].Add(wall.Item2);
                }
                var didUpdate = true;
                while (didUpdate) {
                    didUpdate = false;
                    var wallMapCopy = wallMap.Keys.ToDictionary(_ => _, _ => wallMap[_]);
                    foreach (var w1_1 in wallMapCopy.Keys) {
                        if (!wallMap.ContainsKey(w1_1)) {
                            continue;
                        }
                        var breakLoop = false;
                        foreach (var w1_2 in wallMap[w1_1]) {
                            if (wallMap.ContainsKey(w1_2)) {
                                var w2_1 = w1_2;
                                foreach (var w2_2 in wallMap[w2_1]) {
                                    if (
                                        (w1_1.Item1 == w1_2.Item1 && w1_2.Item1 == w2_1.Item1 && w2_1.Item1  == w2_2.Item1)||
                                        (w1_1.Item2 == w1_2.Item2 &&  w1_2.Item2 == w2_1.Item2 && w2_1.Item2 == w2_2.Item2) 
                                    ){
                                        wallMap[w2_1].Remove(w2_2);
                                        if (wallMap.ContainsKey(w2_1) && (wallMap[w2_1] == null || wallMap[w2_1].Count == 0)) {
                                            wallMap.Remove(w2_1);
                                        }
                                        wallMap[w1_1].Remove(w2_1);
                                        wallMap[w1_1].Add(w2_2);
                                        didUpdate = true;
                                        breakLoop = true;
                                        break;
                                    }

                                }
                                if (breakLoop) {
                                    break;
                                }
                            }
                            if (breakLoop) {
                                break;
                            }
                            
                        }
                        
                    }

                }
                output[wallGroupId] = new HashSet<((int row, int col), (int row, int col))>();
                foreach (var w1 in wallMap.Keys) {
                    foreach (var w2 in wallMap[w1]) {
                        output[wallGroupId].Add((w1, w2));
                    }
                }

            }
            
            return output;
        }

        public static Dictionary<(int, int), HashSet<((double row, double col), (double row, double col))>> scaleBoundaryGroups(
            Dictionary<(int, int), HashSet<((int row, int col), (int row, int col))>> boundaryGroups,
            float scale, 
            int precision
        ) {
            return boundaryGroups.ToDictionary(bg => 
                bg.Key, bg => new HashSet<((double, double),(double, double))>(bg.Value.Select(pair => 
                (
                    (Math.Round(pair.Item1.row * scale, precision), Math.Round(pair.Item1.col * scale, precision)),
                    (Math.Round(pair.Item2.row * scale, precision), Math.Round(pair.Item2.col * scale, precision))
                )))
            );
        }
        
        private static List<((double row, double col), (double row, double col))> getWallLoop(List<((double row, double col), (double row, double col))> walls) {
            var remainingWalls = new HashSet<((double row, double col), (double row, double col))>(walls);
            var output = new List<((double row, double col), (double row, double col))>() { walls[0] };
            remainingWalls.Remove(walls[0]);
            while (remainingWalls.Count > 0) {
                var didBreak = false;
                foreach (var wall in remainingWalls) {
                    
                    if (output.Last().Item2 == wall.Item1) {
                        output.Add(wall);
                        remainingWalls.Remove(wall);
                        didBreak = true;
                        break;
                    }
                    else if (output.Last().Item2 == wall.Item2) {
                        output.Add((wall.Item2, wall.Item1));
                        remainingWalls.Remove(wall);
                        didBreak = true;
                        break;
                    }   
                }
                if (!didBreak) {
                    throw new ArgumentException($"No connecting wall for {output.Last()}!");
                }
            }
            return output;
        }

        public static Dictionary<int, List<((double row, double column), (double row, double column))>> getXZRoomToWallDict(Dictionary<(int, int), HashSet<((double row, double col), (double row, double col))>> boundaryGroups, IEnumerable<int> roomIds) {
            var output = new Dictionary<int, List<((double row, double col), (double row, double col))>>();
            foreach (var roomId in roomIds) {
                var roomWals = new List<((double row, double col), (double row, double col))>();
                foreach (var k in boundaryGroups.Keys.Where(k => TupleContains(k, roomId))) {
                    roomWals.AddRange(boundaryGroups[k]);
                }
                var wallLoop = getWallLoop(roomWals);
                var edgeSum = 0.0;
                foreach ( ((double x0, double z0), (double x1, double z1)) in wallLoop) {
                    var dist = x0 * z1 - x1 * z0;
                    edgeSum += dist;
                }
                if (edgeSum > 0) {
                
                    wallLoop = wallLoop.Reverse<((double row, double col), (double row, double col))>().Select(p => (p.Item2, p.Item1)).ToList();
                }
                output[roomId] = wallLoop;
            }
            return output;
        }

        public static string roomIntToId(int id, RoomHierarchy room) {
            return $"{room.id}{id}";
        }

        public static string houseId() {
            return "house";
        }

        public static string holeTemplateIdToHouseId(string holeTemplateId, int count = 0, string idPrefix = "") {
            
            var name = string.IsNullOrEmpty(idPrefix) ? holeTemplateId : idPrefix;
            return count == 0 ? $"{name}" : $"{name}_{count}";
            // return $"{wallId}{holeId}{index}";
        }

        public static string wallToId(int wallIndexInRoom, string roomId, string wallIdPrefix = "") {
            return $"{roomId}_{wallIdPrefix}{wallIndexInRoom}";
        }

         public static string objectToId(string objectId, int count = 0, string idPrefix = "") {
            var name = string.IsNullOrEmpty(idPrefix) ? objectId : idPrefix;
            return count == 0 ? $"{name}" : $"{name}_{count}";
            // return count == 0 && string.IsNullOrEmpty(idPrefix) ? $"{objectId}" : $"{objectId}_{idPrefix}{count}";
        }

        public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TValue : new()
        {
            public new TValue this[TKey key]
            {
                get
                {
                    TValue val;
                    if (!TryGetValue(key, out val))
                    {
                        val = new TValue();
                        Add(key, val);
                    }
                    return val;
                }
                set { base[key] = value; }
            }
        }

        public class CustomHashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
        {
            public bool Equals(HashSet<T> x, HashSet<T> y)
            {
                if (ReferenceEquals(x, null))
                    return false;

                return x.SetEquals(y);
            }

            public int GetHashCode(HashSet<T> set)
            {
                int hashCode = 0;

                if (set != null)
                {
                    foreach (T t in set)
                    {
                        hashCode = hashCode ^ 
                            (set.Comparer.GetHashCode(t) & 0x7FFFFFFF);
                    }
                }
                return hashCode;
            }
        }

        private static bool TupleContains((int, int) t, int id) {
            return t.Item1 == id || t.Item2 == id;
        }

    }
}