using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;
using RandomExtensions;

namespace UnityStandardAssets.Characters.FirstPerson {
    public partial class PhysicsRemoteFPSAgentController : BaseFPSAgentController {
        protected Dictionary<string, SimObjPhysics> cachedAvailableExpRoomObjectsDict = null;
        protected Dictionary<string, SimObjPhysics> cachedAvailableExpRoomContainersDict = null;

        public Dictionary<string, SimObjPhysics> availableExpRoomObjectsDict {
            get {
                if (cachedAvailableExpRoomObjectsDict == null) {
                    cachedAvailableExpRoomObjectsDict = new Dictionary<string, SimObjPhysics>();
                    foreach (Transform t in GameObject.Find("AvailableObjects").transform) {
                        SimObjPhysics sop = t.gameObject.GetComponent<SimObjPhysics>();
                        cachedAvailableExpRoomObjectsDict.Add(sop.name, sop);
                    }
                }
                return cachedAvailableExpRoomObjectsDict;
            }
        }

        public Dictionary<string, SimObjPhysics> availableExpRoomContainersDict {
            get {
                if (cachedAvailableExpRoomContainersDict == null) {
                    cachedAvailableExpRoomContainersDict = new Dictionary<string, SimObjPhysics>();
                    foreach (Transform t in GameObject.Find("AvailableContainers").transform) {
                        SimObjPhysics sop = t.gameObject.GetComponent<SimObjPhysics>();
                        cachedAvailableExpRoomContainersDict.Add(sop.name, sop);
                    }
                }
                return cachedAvailableExpRoomContainersDict;
            }
        }

        public static void emptyEnumerator(IEnumerator enumerator) {
            while (enumerator.MoveNext()) { }
        }

        public void WhichContainersDoesAvailableObjectFitIn(
            string objectName, int? thirdPartyCameraIndex = null
        ) {
            Camera camera = m_Camera;
            if (thirdPartyCameraIndex.HasValue) {
                camera = agentManager.thirdPartyCameras[thirdPartyCameraIndex.Value];
            }
            PhysicsSceneManager.StartPhysicsCoroutine(
                startCoroutineUsing: this,
                enumerator: whichContainersDoesAvailableObjectFitIn(
                    objectName: objectName, visibilityCheckCamera: camera
                )
            );
        }

        public IEnumerator<float?> whichContainersDoesAvailableObjectFitIn(
            string objectName,
            Camera visibilityCheckCamera
        ) {
            Action<SimObjPhysics> activateSop = (sop) => {
                sop.gameObject.SetActive(true);
                sop.ObjectID = sop.name;
                physicsSceneManager.AddToObjectsInScene(sop);
            };

            Action<SimObjPhysics> deactivateSop = (sop) => {
                sop.gameObject.SetActive(false);
                physicsSceneManager.RemoveFromObjectsInScene(sop);
            };

            Vector3 middleOfTable = new Vector3(-0.5f, 0.9f, 0.2f);
            Vector3 rightOfTable = new Vector3(0.534f, 0.9f, 0.2f);

            Dictionary<string, float> coverNameToScale = new Dictionary<string, float>();

            if (!availableExpRoomObjectsDict.ContainsKey(objectName)) {
                errorMessage = $"Could not find object with name {objectName}";
                actionFinished(false);
                yield break;
            }

            SimObjPhysics toCover = availableExpRoomObjectsDict[objectName];

            if (toCover.GetComponent<Break>()) {
                toCover.GetComponent<Break>().Unbreakable = true;
            }

            activateSop(toCover);

            if (!PlaceObjectAtPoint(
                target: toCover,
                position: middleOfTable,
                rotation: null,
                forceKinematic: true
            )) {
                deactivateSop(toCover);
                errorMessage = $"{toCover.name} failed to place";
                actionFinished(false);
                yield break;
            }

            yield return 0f;

            Vector3 toCoverPos = toCover.transform.position;

            int numberFit = 0;
            foreach (
                SimObjPhysics cover in availableExpRoomContainersDict.OrderBy(
                        kvp => kvp.Key
                    ).Select(kvp => kvp.Value)
            ) {
                if (cover.GetComponent<Break>()) {
                    cover.GetComponent<Break>().Unbreakable = true;
                }

                cover.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
                activateSop(cover);

                BoxCollider coverBbox = cover.BoundingBox.GetComponent<BoxCollider>();
                float maxScale = Mathf.Min(
                    0.5f / coverBbox.size.y,
                    0.7f / coverBbox.size.x,
                    0.7f / coverBbox.size.z,
                    1.75f
                );
                float minScale = Mathf.Min(0.5f, maxScale / 2.0f);

                float lastScale = 1.0f;
                Func<float, bool> tryScale = (scale) => {
                    emptyEnumerator(scaleObject(
                        scale: scale / lastScale,
                        target: cover,
                        scaleOverSeconds: 0f,
                        skipActionFinished: true
                    ));
                    lastScale = scale;

                    Physics.SyncTransforms();

                    if (!PlaceObjectAtPoint(
                        target: cover,
                        position: rightOfTable,
                        rotation: null,
                        forceKinematic: true,
                        includeErrorMessage: true
                    )) {
#if UNITY_EDITOR
                        Debug.Log($"{cover.name} failed to place: {errorMessage}");
#endif
                        deactivateSop(cover);
                        return false;
                    }

                    float coverY = cover.transform.position.y;
                    float toCoverHeight = toCover.BoundingBox.GetComponent<BoxCollider>().size.y;
                    for (int i = 0; i < 4; i++) {
                        cover.transform.position = new Vector3(
                            toCoverPos.x,
                            coverY + toCoverHeight * (i / 4f),
                            toCoverPos.z
                        );

                        Physics.SyncTransforms();

                        Collider coverCollidingWith = UtilityFunctions.firstColliderObjectCollidingWith(
                            go: cover.gameObject
                        );
                        if (coverCollidingWith != null) {
                            //Debug.Log($"{cover.name} colliding with {coverCollidingWith.transform.parent.name}");
                            return false;
                        }
                        if (i == 0) {
                            if (isSimObjVisible(visibilityCheckCamera, toCover, 10f)) {
                                return false;
                            }
                        }
                    }
                    return true;
                };

                if (tryScale(maxScale)) {
                    for (int i = 0; i <= 5; i++) {
                        float newScale = (minScale + maxScale) / 2.0f;
                        if (tryScale(newScale)) {
                            maxScale = newScale;
                        } else {
                            minScale = newScale;
                        }
                        yield return null;
                    }
#if UNITY_EDITOR
                    Debug.Log($"{toCover.name} fits under {cover.name} at {maxScale} scale");
#endif
                    coverNameToScale[cover.name] = maxScale;
                    numberFit += 1;
                }

                emptyEnumerator(scaleObject(
                    scale: 1.0f / lastScale,
                    target: cover,
                    scaleOverSeconds: 0f,
                    skipActionFinished: true
                ));

                deactivateSop(cover);
            }

#if UNITY_EDITOR
            Debug.Log(
                Newtonsoft.Json.JsonConvert.SerializeObject(
                    coverNameToScale,
                    Newtonsoft.Json.Formatting.None,
                    new Newtonsoft.Json.JsonSerializerSettings() {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        ContractResolver = new ShouldSerializeContractResolver()
                    }
                )
            );
#endif

            deactivateSop(toCover);

            yield return 0f;

            actionFinished(true, coverNameToScale);
        }

        public void SetCollisionDetectionModeToContinuousSpeculative(string objectId) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinished(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (!rb) {
                errorMessage = $"Could not find rigid body for {objectId}";
                actionFinished(false);
                return;
            } else {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                actionFinished(true, availableExpRoomContainersDict.Keys.ToList());
            }
        }

        public void AvailableExpRoomObjects() {
            actionFinished(true, availableExpRoomObjectsDict.Keys.ToList());
        }

        public void AvailableExpRoomContainers() {
            actionFinished(true, availableExpRoomContainersDict.Keys.ToList());
        }

        public void ToggleExpRoomObject(string objectName, bool? enable = null) {
            SimObjPhysics target = null;
            if (availableExpRoomObjectsDict.ContainsKey(objectName)) {
                target = availableExpRoomObjectsDict[objectName];
            } else if (availableExpRoomContainersDict.ContainsKey(objectName)) {
                target = availableExpRoomContainersDict[objectName];
            } else {
                errorMessage = $"Could not find object with name {objectName}";
                actionFinishedEmit(false);
                return;
            }

            if (!enable.HasValue) {
                enable = !target.gameObject.activeSelf;
            }

            target.gameObject.SetActive(enable.Value);
            if (enable.Value) {
                foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) {
                    if (!r.enabled) {
                        initiallyDisabledRenderers.Add(r.GetInstanceID());
                    }
                }
                target.ObjectID = target.name;
                physicsSceneManager.AddToObjectsInScene(target);
                actionFinished(true, target.ObjectID);
            } else {
                foreach (Renderer r in target.GetComponentsInChildren<Renderer>()) {
                    if (initiallyDisabledRenderers.Contains(r.GetInstanceID())) {
                        initiallyDisabledRenderers.Remove(r.GetInstanceID());
                        r.enabled = false;
                    } else {
                        r.enabled = true;
                    }
                }
                physicsSceneManager.RemoveFromObjectsInScene(target);
                actionFinished(true);
            }
        }

        public void ToggleObjectIsKinematic(string objectId, bool? isKinematic = null) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinished(false);
                return;
            }

            Rigidbody rb = physicsSceneManager.ObjectIdToSimObjPhysics[objectId].GetComponent<Rigidbody>();
            if (isKinematic.HasValue) {
                rb.isKinematic = isKinematic.Value;
            } else {
                rb.isKinematic = !rb.isKinematic;
            }
            actionFinishedEmit(true);
        }

        public void SetRigidbodyConstraints(
            string objectId,
            bool freezeX = false,
            bool freezeY = false,
            bool freezeZ = false
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinished(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            Rigidbody rb = target.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;

            if (freezeX) {
                rb.constraints = rb.constraints | RigidbodyConstraints.FreezePositionX;
            }

            if (freezeY) {
                rb.constraints = rb.constraints | RigidbodyConstraints.FreezePositionY;
            }

            if (freezeZ) {
                rb.constraints = rb.constraints | RigidbodyConstraints.FreezePositionZ;
            }

            actionFinished(true);
        }

        public void PointOnObjectsCollidersClosestToPoint(
            string objectId, Vector3 point
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinished(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            List<Vector3> closePoints = new List<Vector3>();
            foreach (Collider c in target.GetComponentsInChildren<Collider>()) {
                if (c.enabled && !c.isTrigger) {
                    closePoints.Add(c.ClosestPoint(point));
                }
            }
            closePoints.OrderBy(x => Vector3.Distance(point, x));
#if UNITY_EDITOR
            foreach (Vector3 p in closePoints) {
                Debug.Log($"{p} has dist {Vector3.Distance(p, point)}");
            }
#endif
            actionFinishedEmit(true, closePoints);
        }

        // action to return points from a grid that have an experiment receptacle below it
        // creates a grid starting from the agent's current hand position and projects that grid
        // forward relative to the agent
        // grid will be a 2n+1 by n grid in the orientation of agent right/left by agent forward
        public void GetReceptacleCoordinatesExpRoom(float gridSize, int maxStepCount) {
            var agent = this.agentManager.agents[0];
            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            // good defaults would be gridSize 0.1m, maxStepCount 20 to cover the room
            var ret = ersm.ValidGrid(agent.AgentHand.transform.position, gridSize, maxStepCount, agent);
            // var ret = ersm.ValidGrid(agent.AgentHand.transform.position, action.gridSize, action.maxStepCount, agent);
            actionFinished(true, ret);
        }

        // spawn receptacle object at array index <objectVariation> rotated to <y>
        // on <receptacleObjectId> using position <position>
        public void SpawnExperimentObjAtPoint(
            string objectType,
            string receptacleObjectId,
            Vector3 position,
            float rotation,
            int objectVariation = 0
        ) {
            if (receptacleObjectId == null) {
                errorMessage = "please give valid receptacleObjectId for SpawnExperimentReceptacleAtPoint action";
                actionFinished(false);
                return;
            }

            if (objectType == null) {
                errorMessage = "please use either 'receptacle' or 'screen' to specify which experiment object to spawn";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            // find the object in the scene, disregard visibility
            foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                if (sop.objectID == receptacleObjectId) {
                    target = sop;
                }
            }

            if (target == null) {
                errorMessage = "no receptacle object with id: " +
                receptacleObjectId + " could not be found during SpawnExperimentReceptacleAtPoint";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            if (ersm.SpawnExperimentObjAtPoint(objectType, objectVariation, target, position, rotation)) {
                actionFinished(true);
            } else {
                errorMessage = $"Experiment object could not be placed on {receptacleObjectId}";
                actionFinished(false);
            }
        }

        // spawn receptacle object at array index <objectVariation> rotated to <y>
        // on <receptacleObjectId> using random seed <randomSeed>
        public void SpawnExperimentObjAtRandom(
            string objectType,
            string receptacleObjectId,
            float rotation,
            int randomSeed,
            int objectVariation = 0
        ) {
            if (receptacleObjectId == null) {
                errorMessage = "please give valid receptacleObjectId for SpawnExperimentReceptacleAtRandom action";
                actionFinished(false);
                return;
            }

            if (objectType == null) {
                errorMessage = "please use either 'receptacle' or 'screen' to specify which experiment object to spawn";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            // find the object in the scene, disregard visibility
            foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                if (sop.objectID == receptacleObjectId) {
                    target = sop;
                }
            }

            if (target == null) {
                errorMessage = "no receptacle object with id: " +
                receptacleObjectId + " could not be found during SpawnExperimentReceptacleAtRandom";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            if (ersm.SpawnExperimentObjAtRandom(objectType, objectVariation, randomSeed, target, rotation)) {
                actionFinished(true);
            } else {
                errorMessage = "Experiment object could not be placed on " + receptacleObjectId;
                actionFinished(false);
            }
        }

        // specify a screen by objectId in exp room and change material to objectVariation
        public void ChangeScreenMaterialExpRoom(string objectId, int objectVariation) {
            // only 5 material options at the moment
            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "please use objectVariation [0, 4] inclusive";
                actionFinished(false);
                return;
            }

            if (objectId == null) {
                errorMessage = "please give valid objectId for ChangeScreenMaterialExpRoom action";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            // find the object in the scene, disregard visibility
            foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                if (sop.objectID == objectId) {
                    target = sop;
                }
            }

            if (target == null) {
                errorMessage = "no object with id: " +
                objectId + " could be found during ChangeScreenMaterialExpRoom";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeScreenMaterial(target, objectVariation);
            actionFinished(true);
        }

        // specify a screen in exp room by objectId and change material color to rgb
        public void ChangeScreenColorExpRoom(string objectId, float r, float g, float b) {
            if (
                r < 0 || r > 255 ||
                g < 0 || g > 255 ||
                b < 0 || b > 255
            ) {
                errorMessage = "rgb values must be [0-255]";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            // find the object in the scene, disregard visibility
            foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                if (sop.objectID == objectId) {
                    target = sop;
                }
            }

            if (target == null) {
                errorMessage = "no object with id: " +
                objectId + " could not be found during ChangeScreenColorExpRoom";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeScreenColor(target, r, g, b);
            actionFinished(true);
        }

        // change wall to material [variation]
        public void ChangeWallMaterialExpRoom(int objectVariation) {
            // only 5 material options at the moment
            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "please use objectVariation [0, 4] inclusive";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeWallMaterial(objectVariation);
            actionFinished(true);
        }

        // change wall color to rgb (0-255, 0-255, 0-255)
        public void ChangeWallColorExpRoom(float r, float g, float b) {
            if (
                r < 0 || r > 255 ||
                g < 0 || g > 255 ||
                b < 0 || b > 255
            ) {
                errorMessage = "rgb values must be [0-255]";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeWallColor(r, g, b);
            actionFinished(true);
        }

        // change floor to material [variation]
        public void ChangeFloorMaterialExpRoom(int objectVariation) {
            // only 5 material options at the moment
            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "please use objectVariation [0, 4] inclusive";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeFloorMaterial(objectVariation);
            actionFinished(true);
        }

        // change wall color to rgb (0-255, 0-255, 0-255)
        public void ChangeFloorColorExpRoom(float r, float g, float b) {
            if (
                r < 0 || r > 255 ||
                g < 0 || g > 255 ||
                b < 0 || b > 255
            ) {
                errorMessage = "rgb values must be [0-255]";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeFloorColor(r, g, b);
            actionFinished(true);
        }

        // change color of ceiling lights in exp room to rgb (0-255, 0-255, 0-255)
        public void ChangeLightColorExpRoom(float r, float g, float b) {
            if (
                r < 0 || r > 255 ||
                g < 0 || g > 255 ||
                b < 0 || b > 255
            ) {
                errorMessage = "rgb values must be [0-255]";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeLightColor(r, g, b);
            actionFinished(true);
        }

        // change intensity of lights in exp room [0-5] these arent in like... lumens or anything
        // just a relative intensity value
        public void ChangeLightIntensityExpRoom(float intensity) {
            // restrict this to [0-5]
            if (intensity < 0 || intensity > 5) {
                errorMessage = "light intensity must be [0.0 , 5.0] inclusive";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeLightIntensity(intensity);
            actionFinished(true);
        }

        public void ChangeTableTopMaterialExpRoom(int objectVariation) {
            // only 5 material options at the moment
            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "please use objectVariation [0, 4] inclusive";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeTableTopMaterial(objectVariation);
            actionFinished(true);
        }

        public void ChangeTableTopColorExpRoom(float r, float g, float b) {
            if (
                r < 0 || r > 255 ||
                g < 0 || g > 255 ||
                b < 0 || b > 255
            ) {
                errorMessage = "rgb values must be [0-255]";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeTableTopColor(r, g, b);
            actionFinished(true);
        }

        public void ChangeTableLegMaterialExpRoom(int objectVariation) {
            // only 5 material options at the moment
            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "please use objectVariation [0, 4] inclusive";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeTableLegMaterial(objectVariation);
            actionFinished(true);
        }

        public void ChangeTableLegColorExpRoom(float r, float g, float b) {
            if (
                r < 0 || r > 255 ||
                g < 0 || g > 255 ||
                b < 0 || b > 255
            ) {
                errorMessage = "rgb values must be [0-255]";
                actionFinished(false);
                return;
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeTableLegColor(r, g, b);
            actionFinished(true);
        }

        // returns valid spawn points for spawning an object on a receptacle in the experiment room
        // checks if <objectId> at <y> rotation can spawn without falling off
        // table <receptacleObjectId>
        public void ReturnValidSpawnsExpRoom(string objectType, string receptacleObjectId, float rotation, int objectVariation = 0) {
            if (receptacleObjectId == null) {
                errorMessage = "please give valid receptacleObjectId for ReturnValidSpawnsExpRoom action";
                actionFinished(false);
                return;
            }

            if (objectType == null) {
                errorMessage = "please use either 'receptacle' or 'screen' to specify which experiment object to spawn";
                actionFinished(false);
                return;
            }

            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(receptacleObjectId)) {
                errorMessage = $"Cannot find object with id {receptacleObjectId}.";
                actionFinished(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[receptacleObjectId];

            // return all valid spawn coordinates
            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            actionFinished(true, ersm.ReturnValidSpawns(objectType, objectVariation, target, rotation));
        }
    }
}