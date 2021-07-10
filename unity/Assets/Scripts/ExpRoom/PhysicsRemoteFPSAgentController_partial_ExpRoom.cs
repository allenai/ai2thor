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

        public void WhichContainersDoesAvailableObjectFitIn(string objectName) {
            PhysicsSceneManager.StartPhysicsCoroutine(
                startCoroutineUsing: this,
                enumerator: whichContainersDoesAvailableObjectFitIn(objectName)
            );
        }

        public IEnumerator<float?> whichContainersDoesAvailableObjectFitIn(string objectName) {
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
                    cover.transform.position = new Vector3(
                        toCoverPos.x,
                        coverY,
                        toCoverPos.z
                    );

                    Physics.SyncTransforms();

                    Collider coverCollidingWith = UtilityFunctions.firstColliderObjectCollidingWith(
                        go: cover.gameObject
                    );
                    if (coverCollidingWith == null) {
                        bool toCoverVisible = isSimObjVisible(m_Camera, toCover, 10f);
                        if (!toCoverVisible) {
                            return true;
                        }
                    }
                    // else {
                    //     Debug.Log($"{cover.name} colliding with {coverCollidingWith.transform.parent.name}");
                    // }
                    return false;
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

        public void AvailableExpRoomObjects() {
            actionFinished(true, availableExpRoomObjectsDict.Values.ToList());
        }

        public void AvailableExpRoomContainers() {
            actionFinished(true, availableExpRoomContainersDict.Values.ToList());
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
                target.ObjectID = target.name;
                physicsSceneManager.AddToObjectsInScene(target);
                actionFinished(true, target.ObjectID);
            } else {
                physicsSceneManager.RemoveFromObjectsInScene(target);
                actionFinished(true);
            }
        }
    }
}