using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using RandomExtensions;
using Thor.Procedural;
using Thor.Procedural.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class BoxBounds {
        public Vector3 worldCenter;
        public Vector3 agentRelativeCenter;
        public Vector3 size;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class LoadInUnityProceduralAsset {
        public string id;
        public string dir;
        public string extension = ".msgpack.gz";
        public ObjectAnnotations annotations = null;
    }

#nullable enable
    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BodyAsset {
        public string? assetId = null;
        public LoadInUnityProceduralAsset? dynamicAsset = null;
        public ProceduralAsset? asset = null;
    }

#nullable disable

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BackwardsCompatibleInitializeParams {
        //  Make what parameters it uses explicit
        public float maxUpwardLookAngle = 0.0f;
        public float maxDownwardLookAngle = 0.0f;
        public string antiAliasing = null;
        public float gridSize;
        public float fieldOfView;
        public float cameraNearPlane;
        public float cameraFarPlane;
        public float timeScale = 1.0f;
        public float rotateStepDegrees = 90.0f;
        public bool snapToGrid = true;
        public bool renderImage = true;
        public bool renderImageSynthesis = true;
        public bool renderDepthImage;
        public bool renderSemanticSegmentation;
        public bool renderInstanceSegmentation;
        public bool renderNormalsImage;
        public float maxVisibleDistance = 1.5f;
        public float visibilityDistance;
        public float TimeToWaitForObjectsToComeToRest = 10.0f;
        public string visibilityScheme = VisibilityScheme.Collider.ToString();
    }

    public class FpinAgentController : PhysicsRemoteFPSAgentController {
        private static readonly Vector3 agentSpawnOffset = new Vector3(100.0f, 100.0f, 100.0f);
        private FpinMovableContinuous fpinMovable;
        public BoxCollider spawnedBoxCollider = null;
        public BoxCollider spawnedTriggerBoxCollider = null;
        public GameObject fpinVisibilityCapsule = null;
        private Transform topMeshTransform = null;
        private Bounds? agentBounds = null;
        public BoxBounds boxBounds = null;
        public BoxBounds BoxBounds {
            get {
                if (spawnedBoxCollider != null) {
                    BoxBounds currentBounds = new BoxBounds();

                    currentBounds.worldCenter = spawnedBoxCollider.transform.TransformPoint(
                        spawnedBoxCollider.center
                    );
                    currentBounds.size = GetTrueSizeOfBoxCollider(spawnedBoxCollider);
                    currentBounds.agentRelativeCenter = this.transform.InverseTransformPoint(
                        currentBounds.worldCenter
                    );

                    boxBounds = currentBounds;

                    // Debug.Log($"world center: {boxBounds.worldCenter}");
                    // Debug.Log($"size: {boxBounds.size}");
                    // Debug.Log($"agentRelativeCenter: {boxBounds.agentRelativeCenter}");
                } else {
                    // Debug.Log("why is it nullll");
                    return null;
                }

                return boxBounds;
            }
            set { boxBounds = value; }
        }

        public CollisionListener collisionListener;

        public FpinAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager)
            : base(baseAgentComponent, agentManager) { }

        public void Start() {
            //put stuff we need here when we need it maybe
        }

        public override RaycastHit[] CastBodyTrayectory(
            Vector3 startPosition,
            Vector3 direction,
            float skinWidth,
            float moveMagnitude,
            int layerMask,
            CapsuleData cachedCapsule
        ) {
            Vector3 startPositionBoxCenter =
                startPosition
                + this.transform.TransformDirection(this.boxBounds.agentRelativeCenter);

            return Physics.BoxCastAll(
                center: startPositionBoxCenter,
                halfExtents: this.boxBounds.size / 2.0f,
                direction: direction,
                orientation: this.transform.rotation,
                maxDistance: moveMagnitude,
                layerMask: layerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );
        }

        //since the size returned by the box collider only reflects its size in local space DISREGARDING ANY TRANSFORM SCALES
        //IN ITS PARENTS OR ITSELF here we go
        public Vector3 GetTrueSizeOfBoxCollider(BoxCollider collider) {
            Vector3 trueSize = collider.size;

            // get the transform of the collider itself
            Transform currentTransform = collider.transform;

            // Apply the scale from the collider's transform and all parent transforms
            while (currentTransform != null) {
                trueSize.x *= currentTransform.localScale.x;
                trueSize.y *= currentTransform.localScale.y;
                trueSize.z *= currentTransform.localScale.z;

                //grab the transform of any parents and GO AGAIN
                currentTransform = currentTransform.parent;
            }

            return trueSize;
        }

        //override so we can access to fpin specific stuff
        public override MetadataWrapper generateMetadataWrapper() {
            //get all the usual stuff from base agent's implementation
            MetadataWrapper metaWrap = base.generateMetadataWrapper();

            //here's the fpin specific stuff
            if (boxBounds != null) {
                //get from BoxBounds as box world center will update as agent moves so we can't cache it
                metaWrap.agent.fpinColliderSize = BoxBounds.size;
                metaWrap.agent.fpinColliderWorldCenter = BoxBounds.worldCenter;
                metaWrap.agent.fpinColliderAgentRelativeCenter = BoxBounds.agentRelativeCenter;
            } else {
                metaWrap.agent.fpinColliderSize = new Vector3(0, 0, 0);
            }

            return metaWrap;
        }

        public List<Vector3> SamplePointsOnNavMesh(int sampleCount, float maxDistance) {
            float minX = agentManager.SceneBounds.min.x;
            float minZ = agentManager.SceneBounds.min.z;
            float maxX = agentManager.SceneBounds.max.x;
            float maxZ = agentManager.SceneBounds.max.z;

            Debug.Log($"Scene bounds: X: {minX} z: {minZ} max x: {maxX} z: {maxZ}");

            int n = (int)Mathf.Ceil(Mathf.Sqrt(sampleCount));

            List<Vector3> initPoints = new List<Vector3>();
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    initPoints.Add(
                        new Vector3(
                            Mathf.Lerp(minX, maxX, (i + 0.5f) / n),
                            0f,
                            Mathf.Lerp(minZ, maxZ, (j + 0.5f) / n)
                        )
                    );
                }
            }
            initPoints.Shuffle_();

            List<Vector3> pointsOnMesh = new List<Vector3>();
            for (int i = 0; i < initPoints.Count; i++) {
                if (pointsOnMesh.Count >= sampleCount) {
                    break;
                }

                NavMeshHit hit;
                Vector3 randomPoint = initPoints[i];
                if (NavMesh.SamplePosition(randomPoint, out hit, maxDistance, NavMesh.AllAreas)) {
# if UNITY_EDITOR
                    Debug.DrawLine(
                        hit.position,
                        hit.position + new Vector3(0f, 0.1f, 0f),
                        Color.cyan,
                        15f
                    );
# endif
                    pointsOnMesh.Add(hit.position);
                }
            }

            Debug.Log($"On navmesh count {pointsOnMesh} size: {pointsOnMesh.Count}");

            return pointsOnMesh;
        }

        public void RandomlyPlaceAgentOnNavMesh(int n = 200, float maxDistance = 0.1f) {
            List<Vector3> pointsOnMesh = SamplePointsOnNavMesh(n, maxDistance: maxDistance);
            if (pointsOnMesh.Count == 0) {
                throw new InvalidOperationException("No points on the navmesh");
            }

            Bounds b = UtilityFunctions.CreateEmptyBounds();
            foreach (Collider c in GetComponentsInChildren<Collider>()) {
                b.Encapsulate(c.bounds);
            }

            //Debug.Log($"current transform.position.y: {transform.position.y}");
            float yOffset = 0.001f + transform.position.y - b.min.y;
            //Debug.Log($"yOffset is: {yOffset}");

            bool success = false;
            foreach (Vector3 point in pointsOnMesh) {
                try {
                    //Debug.Log($"what is the point we are trying from the pointsOnMesh? {point:F8}");
                    teleportFull(
                        position: point + new Vector3(0, yOffset, 0),
                        rotation: new Vector3(0f, UnityEngine.Random.Range(0, 360) * 1f, 0f),
                        null,
                        true
                    );
                    success = true;
                    break;
                } catch (InvalidOperationException) {
                    continue;
                }
            }
            actionFinished(success);
        }

        public void spawnAgentBoxCollider(
            GameObject agent,
            Type agentType,
            Vector3 originalPosition,
            Quaternion originalRotation,
            Bounds agentBounds,
            bool useVisibleColliderBase = false,
            bool spawnCollidersWithoutMesh = false
        ) {
            //create colliders based on the agent bounds
            var col = new GameObject("fpinCollider", typeof(BoxCollider));
            col.layer = LayerMask.NameToLayer("Agent");
            spawnedBoxCollider = col.GetComponent<BoxCollider>();

            //get a set of trigger colliders as well
            var tCol = new GameObject("fpinTriggerCollider", typeof(BoxCollider));
            tCol.layer = LayerMask.NameToLayer("Agent");
            spawnedTriggerBoxCollider = tCol.GetComponent<BoxCollider>();
            spawnedTriggerBoxCollider.isTrigger = true;

            //move both of these colliders to the bounds center
            spawnedBoxCollider.transform.position = spawnedTriggerBoxCollider.transform.position =
                agentBounds.center;
            spawnedBoxCollider.transform.rotation = spawnedTriggerBoxCollider.transform.rotation =
                Quaternion.identity;

            //parent these colliders to the viscap really quick, so if we scale the fpinVisibilityCapsule later it all stays the same
            spawnedBoxCollider.transform.parent = spawnedTriggerBoxCollider.transform.parent =
                fpinVisibilityCapsule.transform;

            //calculate collider size based on what the size of the bounds of the mesh are
            Vector3 colliderSize = new Vector3(
                agentBounds.size.x,
                agentBounds.size.y,
                agentBounds.size.z
            );
            spawnedBoxCollider.size = spawnedTriggerBoxCollider.size = colliderSize;

            return;
        }

        //helper function to remove the currently generated agent box collider
        //make sure to follow this up with a subsequent generation so BoxBounds isn't left null
        public void destroyAgentBoxCollider() {
            GameObject visibleBox = GameObject.Find("VisibleBox");
            if (spawnedBoxCollider != null) {
                UnityEngine.Object.DestroyImmediate(spawnedBoxCollider.transform.gameObject);
                spawnedBoxCollider = null;
            }
            if (spawnedTriggerBoxCollider != null) {
                UnityEngine.Object.DestroyImmediate(spawnedTriggerBoxCollider.transform.gameObject);
                spawnedTriggerBoxCollider = null;
            }
            if (visibleBox != null) {
                UnityEngine.Object.DestroyImmediate(visibleBox);
            }
#if UNITY_EDITOR
            GameObject visualizedBoxCollider = GameObject.Find("VisualizedBoxCollider");
            if (visualizedBoxCollider != null) {
                UnityEngine.Object.DestroyImmediate(visualizedBoxCollider);
            }
#endif

            //clear out any leftover values for BoxBounds just in case
            BoxBounds = null;

            actionFinished(true);
            return;
        }

        private Transform CopyMeshChildrenRecursive(
            Transform sourceTransform,
            Transform targetTransform,
            bool isTopMost = true
        ) {
            Transform thisTransform = null;
            foreach (Transform child in sourceTransform) {
                GameObject copiedChild = null;
                // Check if the child has a MeshFilter component
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter != null) {
                    copiedChild = CopyMeshToTarget(child, targetTransform);
                }

                // Process children only if necessary (i.e., they contain MeshFilters)
                if (HasMeshInChildrenOrSelf(child)) {
                    Transform parentForChildren =
                        (copiedChild != null)
                            ? copiedChild.transform
                            : CreateContainerForHierarchy(child, targetTransform).transform;
                    CopyMeshChildrenRecursive(child, parentForChildren, false);
                    if (isTopMost) {
                        thisTransform = parentForChildren;
                    }
                }
            }

            if (isTopMost) {
                // Set up intermediate object for scaling the mesh in agent-space (since the top-level mesh could be rotated, potentially introducing a complex matrix of scales)
                GameObject agentScaleObject = new GameObject("AgentScaleObject");
                agentScaleObject.transform.position = thisTransform.position;
                agentScaleObject.transform.SetParent(targetTransform);
                thisTransform.SetParent(agentScaleObject.transform);
                return agentScaleObject.transform;
            }

            return null;
        }

        private GameObject CopyMeshToTarget(Transform child, Transform targetParent) {
            // Create a new GameObject and copy components
            GameObject copiedChild = new GameObject(child.name);
            copiedChild.transform.SetParent(targetParent);

            MeshFilter meshFilter = child.GetComponent<MeshFilter>();
            MeshFilter copiedMeshFilter = copiedChild.AddComponent<MeshFilter>();
            copiedMeshFilter.mesh = meshFilter.mesh;

            MeshRenderer sourceMeshRenderer = child.GetComponent<MeshRenderer>();
            if (sourceMeshRenderer != null) {
                MeshRenderer copiedMeshRenderer = copiedChild.AddComponent<MeshRenderer>();
                copiedMeshRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
            }

            copiedChild.transform.localPosition = child.localPosition;
            copiedChild.transform.localRotation = child.localRotation;
            copiedChild.transform.localScale = child.localScale;

            return copiedChild;
        }

        private bool HasMeshInChildrenOrSelf(Transform transform) {
            foreach (Transform child in transform) {
                if (child.GetComponent<MeshFilter>() != null || HasMeshInChildrenOrSelf(child)) {
                    return true;
                }
            }

            if (transform.GetComponent<MeshFilter>() != null) {
                return true;
            }

            return false;
        }

        private GameObject CreateContainerForHierarchy(Transform child, Transform targetParent) {
            GameObject container = new GameObject(child.name + "_Container");
            container.transform.SetParent(targetParent);
            container.transform.localPosition = child.localPosition;
            container.transform.localRotation = child.localRotation;
            container.transform.localScale = child.localScale;
            return container;
        }

        private HashSet<Vector3> TransformedMeshRendererVertices(
            MeshRenderer mr,
            bool returnFirstVertexOnly = false
        ) {
            MeshFilter mf = mr.gameObject.GetComponent<MeshFilter>();
            Matrix4x4 localToWorld = mr.transform.localToWorldMatrix;
            HashSet<Vector3> vertices = new HashSet<Vector3>(mf.sharedMesh.vertices);
            HashSet<Vector3> transformedVertices = new HashSet<Vector3>();
            foreach (Vector3 vertex in vertices) {
                transformedVertices.Add(localToWorld.MultiplyPoint3x4(vertex));
            }
            return transformedVertices;
        }

        public ActionFinished GetBoxBounds() {
            return new ActionFinished() { success = true, actionReturn = this.BoxBounds };
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
            // Move the point to the pivot's origin
            Vector3 dir = point - pivot;
            // Rotate it
            dir = Quaternion.Euler(angles) * dir;
            // Move it back
            point = dir + pivot;
            return point;
        }

        public ActionFinished BackwardsCompatibleInitialize(
            BackwardsCompatibleInitializeParams args
        ) {
            Debug.Log("RUNNING BackCompatInitialize from FpinAgentController.cs");
            // limit camera from looking too far down/up
            //default max are 30 up and 60 down, different agent types may overwrite this
            if (Mathf.Approximately(args.maxUpwardLookAngle, 0.0f)) {
                this.maxUpwardLookAngle = 30f;
            } else {
                this.maxUpwardLookAngle = args.maxUpwardLookAngle;
            }

            if (Mathf.Approximately(args.maxDownwardLookAngle, 0.0f)) {
                this.maxDownwardLookAngle = 60f;
            } else {
                this.maxDownwardLookAngle = args.maxDownwardLookAngle;
            }

            if (args.antiAliasing != null) {
                agentManager.updateAntiAliasing(
                    postProcessLayer: m_Camera.gameObject.GetComponentInChildren<PostProcessLayer>(),
                    antiAliasing: args.antiAliasing
                );
            }
            // m_Camera.GetComponent<FirstPersonCharacterCull>().SwitchRenderersToHide(this.VisibilityCapsule);

            if (args.gridSize == 0) {
                args.gridSize = 0.25f;
            }

            // note: this overrides the default FOV values set in InitializeBody()
            if (args.fieldOfView > 0 && args.fieldOfView < 180) {
                m_Camera.fieldOfView = args.fieldOfView;
            } else if (args.fieldOfView < 0 || args.fieldOfView >= 180) {
                errorMessage = "fov must be set to (0, 180) noninclusive.";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            }

            if (args.cameraNearPlane > 0) {
                m_Camera.nearClipPlane = args.cameraNearPlane;
            }

            if (args.cameraFarPlane > 0) {
                m_Camera.farClipPlane = args.cameraFarPlane;
            }

            if (args.timeScale > 0) {
                if (Time.timeScale != args.timeScale) {
                    Time.timeScale = args.timeScale;
                }
            } else {
                errorMessage = "Time scale must be > 0";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            }

            if (args.rotateStepDegrees <= 0.0) {
                errorMessage = "rotateStepDegrees must be a non-zero, non-negative float";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            }

            // default is 90 defined in the ServerAction class, specify whatever you want the default to be
            if (args.rotateStepDegrees > 0.0) {
                this.rotateStepDegrees = args.rotateStepDegrees;
            }

            if (args.snapToGrid && !ValidRotateStepDegreesWithSnapToGrid(args.rotateStepDegrees)) {
                errorMessage =
                    $"Invalid values 'rotateStepDegrees': ${args.rotateStepDegrees} and 'snapToGrid':${args.snapToGrid}. 'snapToGrid': 'True' is not supported when 'rotateStepDegrees' is different from grid rotation steps of 0, 90, 180, 270 or 360.";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            }

            if (args.maxDownwardLookAngle < 0) {
                errorMessage = "maxDownwardLookAngle must be a non-negative float";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            }

            if (args.maxUpwardLookAngle < 0) {
                errorMessage = "maxUpwardLookAngle must be a non-negative float";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            }

            this.snapToGrid = args.snapToGrid;

            if (
                args.renderDepthImage
                || args.renderSemanticSegmentation
                || args.renderInstanceSegmentation
                || args.renderNormalsImage
            ) {
                this.updateImageSynthesis(true);
            }

            if (args.visibilityDistance > 0.0f) {
                this.maxVisibleDistance = args.visibilityDistance;
            }

            var navmeshAgent = this.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
            var collider = this.GetComponent<CapsuleCollider>();

            if (collider != null && navmeshAgent != null) {
                navmeshAgent.radius = collider.radius;
                navmeshAgent.height = collider.height;
                navmeshAgent.transform.localPosition = new Vector3(
                    navmeshAgent.transform.localPosition.x,
                    navmeshAgent.transform.localPosition.y,
                    collider.center.z
                );
            }

            // navmeshAgent.radius =

            if (args.gridSize <= 0 || args.gridSize > 5) {
                errorMessage = "grid size must be in the range (0,5]";
                Debug.Log(errorMessage);
                return new ActionFinished(success: false, errorMessage: errorMessage);
            } else {
                gridSize = args.gridSize;

                // Don't know what this was for
                // StartCoroutine(checkInitializeAgentLocationAction());
            }

            // initialize how long the default wait time for objects to stop moving is
            this.TimeToWaitForObjectsToComeToRest = args.TimeToWaitForObjectsToComeToRest;

            // Debug.Log("Object " + action.controllerInitialization.ToString() + " dict "  + (action.controllerInitialization.variableInitializations == null));//+ string.Join(";", action.controllerInitialization.variableInitializations.Select(x => x.Key + "=" + x.Value).ToArray()));

            this.visibilityScheme = ServerAction.GetVisibilitySchemeFromString(
                args.visibilityScheme
            );
            // this.originalLightingValues = null;
            // Physics.autoSimulation = true;
            // Debug.Log("True if physics is auto-simulating: " + Physics.autoSimulation);

            this.AgentHand.gameObject.SetActive(false);

            return new ActionFinished(
                success: true,
                actionReturn: new InitializeReturn {
                    cameraNearPlane = m_Camera.nearClipPlane,
                    cameraFarPlane = m_Camera.farClipPlane
                }
            );
        }

        public ActionFinished Initialize(
            BodyAsset bodyAsset = null,
            // TODO: do we want to allow non relative to the box offsets?
            float originOffsetX = 0.0f,
            float originOffsetZ = 0.0f,
            Vector3? colliderScaleRatio = null,
            bool useAbsoluteSize = false,
            bool useVisibleColliderBase = false
        ) {
            this.visibilityScheme = VisibilityScheme.Distance;
            var actionFinished = this.InitializeBody(
                bodyAsset: bodyAsset,
                originOffsetX: originOffsetX,
                originOffsetZ: originOffsetZ,
                colliderScaleRatio: colliderScaleRatio,
                useAbsoluteSize: useAbsoluteSize,
                useVisibleColliderBase: useVisibleColliderBase
            );
            // Needs to be done to update Agent's imageSynthesis reference, should be removed... and just get the component
            this.updateImageSynthesis(true);
            return actionFinished;
        }

        public ActionFinished InitializeBody(
            BodyAsset bodyAsset = null,
            float originOffsetX = 0.0f,
            float originOffsetZ = 0.0f,
            Vector3? colliderScaleRatio = null,
            bool useAbsoluteSize = false,
            bool useVisibleColliderBase = false
        ) {
            // if using no source body mesh, we default to using absolute size via the colliderScaleRatio
            // since a non absolute size doesn't make sense if we have no default mesh size to base the scale
            // ratio on
            Vector3 meshScaleRatio = colliderScaleRatio.GetValueOrDefault(Vector3.one);

            bool noMesh = false;
            if (bodyAsset == null) {
                useAbsoluteSize = true;
                noMesh = true;
            }

            // Store the current rotation
            Vector3 originalPosition = this.transform.position;
            Quaternion originalRotation = this.transform.rotation;

            // Move the agent to a safe place and temporarily align the agent's rotation with the world coordinate system (i.e. zero it out)
            this.transform.position = originalPosition + agentSpawnOffset;
            this.transform.rotation = Quaternion.identity;

            //remove any old copied meshes or generated colliders from previous fpin agent now
            destroyAgentBoxCollider();
            if (fpinVisibilityCapsule != null) {
                UnityEngine.Object.DestroyImmediate(fpinVisibilityCapsule);
            }

            var spawnAssetActionFinished = new ActionFinished();

            Bounds meshBoundsWorld = new Bounds(this.transform.position, Vector3.zero);
            if (bodyAsset != null) {
                //spawn in a default mesh in an out-of-the-way location (currently 200,200,200) to base the new bounds on
                spawnAssetActionFinished = spawnBodyAsset(bodyAsset, out GameObject spawnedMesh);
                // Return early if spawn failed
                if (!spawnAssetActionFinished.success) {
                    return spawnAssetActionFinished;
                }

                // duplicate the entire mesh hierarchy from "agentMesh" to "FPSController" (with all of the local-transforms intact), and return top-level transform
                topMeshTransform = CopyMeshChildrenRecursive(
                    sourceTransform: spawnedMesh.transform,
                    targetTransform: this.transform
                );

                // get unscaled bounds of mesh

                // we need a bounds-center to start from that is guaranteed to fall inside of the mesh's geometry,
                // so we'll take the bounds-center of the first meshRenderer
                MeshRenderer[] meshRenderers =
                    topMeshTransform.gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mr in meshRenderers) {
                    // No need to run TransformedMeshRendererVertices if the meshRenderer's GameObject isn't rotated
                    if (mr.transform.eulerAngles.magnitude < 1e-4f) {
                        meshBoundsWorld.Encapsulate(mr.bounds);
                    } else {
                        HashSet<Vector3> vertices = TransformedMeshRendererVertices(mr);
                        foreach (Vector3 vertex in vertices) {
                            meshBoundsWorld.Encapsulate(vertex);
                        }
                    }
                }

                // scale mesh (and mesh-bounds) either absolutely or proportionately, ensuring the topMeshTransform is centered at the meshBoundsWorld center first
                Vector3 currentTopMeshTransformChildPos = topMeshTransform.GetChild(0).position;
                topMeshTransform.position = meshBoundsWorld.center;
                topMeshTransform.GetChild(0).position = currentTopMeshTransformChildPos;

                if (useAbsoluteSize) {
                    topMeshTransform.localScale = new Vector3(
                        meshScaleRatio.x / meshBoundsWorld.size.x,
                        meshScaleRatio.y / meshBoundsWorld.size.y,
                        meshScaleRatio.z / meshBoundsWorld.size.z
                    );
                    meshBoundsWorld.size = meshScaleRatio;
                } else {
                    topMeshTransform.localScale = meshScaleRatio;
                    meshBoundsWorld.size = new Vector3(
                        meshScaleRatio.x * meshBoundsWorld.size.x,
                        meshScaleRatio.y * meshBoundsWorld.size.y,
                        meshScaleRatio.z * meshBoundsWorld.size.z
                    );
                }

                // Move the topMeshTransform by a Vector3 that closes the distance between the current bounds-center's
                // and the agent-origin, where it should be
                topMeshTransform.position += this.transform.position - meshBoundsWorld.center;
                // Move topMeshTransform so its bounds-footprint is centered on the  FPSAgentController-origin
                topMeshTransform.position += Vector3.up * meshBoundsWorld.extents.y;
                // Now that meshBoundsWorld's position is no longer accurate, update it
                meshBoundsWorld.center =
                    this.transform.position + Vector3.up * meshBoundsWorld.extents.y;

                // remove the spawned mesh cause we are done with it
                foreach (var sop in spawnedMesh.GetComponentsInChildren<SimObjPhysics>()) {
                    agentManager.physicsSceneManager.RemoveFromObjectsInScene(sop);
                }

                if (spawnedMesh.activeInHierarchy) {
                    UnityEngine.Object.DestroyImmediate(spawnedMesh);
                }
            } else {
                meshBoundsWorld = new Bounds(
                    this.transform.position + (Vector3.up * meshScaleRatio.y / 2),
                    meshScaleRatio
                );
            }

            // Create new "viscap" object to hold all the meshes and use it as the new pivot poitn for them
            GameObject viscap = new GameObject("fpinVisibilityCapsule");
            viscap.transform.SetParent(this.transform);
            viscap.transform.localPosition = Vector3.zero;
            viscap.transform.localRotation = Quaternion.identity;
            // set reference to the meshes so the base agent and fpin agent are happy
            VisibilityCapsule = fpinVisibilityCapsule = viscap;

            if (topMeshTransform != null) {
                topMeshTransform.SetParent(viscap.transform);
            }

            // ok now generate colliders, we are still up at agentSpawnOffset and aligned with world axes
            spawnAgentBoxCollider(
                agent: this.gameObject,
                agentType: this.GetType(),
                originalPosition: originalPosition,
                originalRotation: originalRotation,
                agentBounds: meshBoundsWorld,
                useVisibleColliderBase: useVisibleColliderBase,
                spawnCollidersWithoutMesh: noMesh //if noMesh is true, we have no mesh so we need to spawn colliders without a mesh
            );

            // spawn the visible collider base if we need to
            if (useVisibleColliderBase) {
                GameObject visibleBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visibleBase.name = "visibleBase";
                visibleBase.GetComponent<BoxCollider>().enabled = false;
                visibleBase.transform.position = meshBoundsWorld.center;
                visibleBase.transform.parent = fpinVisibilityCapsule.transform;
                visibleBase.transform.localScale = new Vector3(
                    meshBoundsWorld.size.x,
                    meshBoundsWorld.size.y / 4,
                    meshBoundsWorld.size.z
                );
                // get the y-offset for how low we need to move the visible collider base so it is flush with the bottomost extents of the spawnedBoxCollider
                float yOffset =
                    visibleBase.GetComponent<MeshRenderer>().bounds.min.y - meshBoundsWorld.min.y;
                // we have the offset now so lets set the local position for the visible base as needed
                visibleBase.transform.localPosition -= yOffset * Vector3.up;
            }

            // now lets reposition the agent origin with originOffsetX and originOffsetZ
            fpinVisibilityCapsule.transform.position += new Vector3(
                -originOffsetX,
                0,
                -originOffsetZ
            );
            // now that meshBoundsWorld's position is no longer accurate, update it
            meshBoundsWorld.center += new Vector3(-originOffsetX, 0, -originOffsetZ);

            // adjust agent's CharacterController and CapsuleCollider according to the mesh-bounds, because it needs to fit inside
            var characterController = this.GetComponent<CharacterController>();
            Vector3 boxCenter = meshBoundsWorld.center - this.transform.position;
            characterController.center = boxCenter;

            // set the radius to fit inside the box, considering the smallest length, width, or height
            float minRadius = Mathf.Min(
                Mathf.Min(meshBoundsWorld.extents.x, meshBoundsWorld.extents.z),
                meshBoundsWorld.extents.y
            );
            characterController.radius = minRadius;

            // adjust the capsule size based on the size of the bounds
            float boxHeight = meshBoundsWorld.size.y;
            characterController.height = boxHeight;

            var myTriggerCap = this.GetComponent<CapsuleCollider>();
            myTriggerCap.center = boxCenter;
            myTriggerCap.height = boxHeight;
            myTriggerCap.radius = minRadius;

            // ok recalibrate navmesh child component based on the new agent capsule now that its updated
            var navmeshchild = this.transform.GetComponentInChildren<NavMeshAgent>();
            navmeshchild.transform.localPosition = new Vector3(
                meshBoundsWorld.center.x - this.transform.position.x,
                0.0f,
                meshBoundsWorld.center.z - this.transform.position.z
            );
            navmeshchild.baseOffset = 0.0f;
            navmeshchild.height = boxHeight;
            navmeshchild.radius = minRadius;

            // ok now check if we were to teleport back to our original position and rotation....
            // will our current box colliders clip with anything? If so, send a failure message
            Vector3 boxCenterAtInitialTransform = RotatePointAroundPivot(
                meshBoundsWorld.center - agentSpawnOffset,
                originalPosition,
                originalRotation.eulerAngles
            );

#if UNITY_EDITOR
            // /////////////////////////////////////////////////
            // for visualization lets spawn a cube at the center of where the boxCenter supposedly is
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "VisualizedBoxCollider";
            cube.transform.position = boxCenterAtInitialTransform;
            cube.transform.rotation = originalRotation;
            cube.transform.localScale = meshBoundsWorld.size;
            cube.GetComponent<BoxCollider>().enabled = false;

            var material = cube.GetComponent<MeshRenderer>().material;
            material.SetColor("_Color", new Color(1.0f, 0.0f, 0.0f, 0.4f));
            // Set transparency XD ...
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            // ////////////////////////////////////////////////
#endif

            // used to check if there is enough free space given the generated colliders for the agent to return to its original pose
            int checkBoxLayerMask = LayerMask.GetMask(
                "SimObjVisible",
                "Procedural1",
                "Procedural2",
                "Procedural3",
                "Procedural0"
            );

            // check if we were to teleport our agent back to its starting position, will the new box colliders generated clip with anything?
            // if we do clip with something, leave the agent where it is, and send a message saying there is clipping actively happening
            // the reccomended thing to do here is either reset the scene entirely and load in with a new agent, or try and use `InitializeBody` with
            // a smaller mesh size that would potentially fit here
            Debug.Log($"{boxCenterAtInitialTransform:F5} and {meshBoundsWorld.extents:F5}");
            if (
                Physics.CheckBox(
                    boxCenterAtInitialTransform,
                    meshBoundsWorld.extents,
                    originalRotation,
                    checkBoxLayerMask
                )
            ) {
                this.transform.position = originalPosition;
                this.transform.rotation = originalRotation;
                string error =
                    "Spawned box collider is colliding with other objects. Cannot spawn box collider.";
                actionReturn = error;
                errorMessage = error;
                throw new InvalidOperationException(error);
            }

            // we are safe to return to our original position and rotation
            this.transform.position = originalPosition;
            this.transform.rotation = originalRotation;

            // enable cameras now
            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            // make sure we are hooked up to the collision listener
            fpinMovable = new FpinMovableContinuous(this.GetComponentInParent<CollisionListener>());

            // we had a body asset used, so actionFinished returns info related to that
            if (bodyAsset != null) {
                return new ActionFinished(spawnAssetActionFinished) {
                    // TODO: change to a proper class once metadata return is defined
                    actionReturn = new Dictionary<string, object>()
                    {
                        //objectSphereBounds... how is this being used??? currently if we scale the mesh asset this value may not be correct
                        {
                            "objectSphereBounds",
                            spawnAssetActionFinished.actionReturn as ObjectSphereBounds
                        },
                        { "BoxBounds", this.BoxBounds },
                        { "cameraNearPlane", m_Camera.nearClipPlane },
                        { "cameraFarPlane", m_Camera.farClipPlane }
                    }
                };
            } else {
                return new ActionFinished()
                {
                    // TODO: change to a proper class once metadata return is defined
                    actionReturn: new Dictionary<string, object>()
                    {
                        { "BoxBounds", this.BoxBounds },
                        { "cameraNearPlane", m_Camera.nearClipPlane },
                        { "cameraFarPlane", m_Camera.farClipPlane }
                    }
                );
            }
        }

        private ActionFinished spawnBodyAsset(BodyAsset bodyAsset, out GameObject spawnedMesh) {
            if (bodyAsset == null) {
                throw new ArgumentNullException("bodyAsset is null");
            } else if (
                  bodyAsset.assetId == null
                  && bodyAsset.dynamicAsset == null
                  && bodyAsset.asset == null
              ) {
                throw new ArgumentNullException(
                    "`bodyAsset.assetId`, `bodyAsset.dynamicAsset` or `bodyAsset.asset` must be provided all are null."
                );
            }
            ActionFinished actionFinished = new ActionFinished(
                success: false,
                errorMessage: "No body specified"
            );
            spawnedMesh = null;

            if (
                (bodyAsset.dynamicAsset != null || bodyAsset.asset != null)
                && bodyAsset.assetId == null
            ) {
                var id =
                    bodyAsset.dynamicAsset != null
                        ? bodyAsset.dynamicAsset.id
                        : bodyAsset.asset.name;

                var assetMap = ProceduralTools.getAssetMap();
                // Check if asset is in AssetDatabase already
                if (assetMap.ContainsKey(id)) {
                    Debug.Log("------- Already contains key");
                    bodyAsset.assetId = id;
                }
            }

            if (bodyAsset.assetId != null) {
                actionFinished = SpawnAsset(
                    bodyAsset.assetId,
                    "agentMesh",
                    new Vector3(200f, 200f, 200f)
                );
                spawnedMesh = GameObject.Find("agentMesh");
            } else if (bodyAsset.dynamicAsset != null) {
                actionFinished = this.CreateRuntimeAsset(
                    id: bodyAsset.dynamicAsset.id,
                    dir: bodyAsset.dynamicAsset.dir,
                    extension: bodyAsset.dynamicAsset.extension,
                    annotations: bodyAsset.dynamicAsset.annotations,
                    serializable: true
                );
                spawnedMesh = GameObject.Find("mesh");
            } else if (bodyAsset.asset != null) {
                bodyAsset.asset.serializable = true;
                actionFinished = this.CreateRuntimeAsset(asset: bodyAsset.asset);
            }

            if (
                bodyAsset.assetId == null
                && (bodyAsset.dynamicAsset != null || bodyAsset.asset != null)
            ) {
                var id =
                    bodyAsset.dynamicAsset != null
                        ? bodyAsset.dynamicAsset.id
                        : bodyAsset.asset.name;
                Debug.Log(
                    $"-- checks {bodyAsset.assetId == null} {bodyAsset.dynamicAsset != null} {bodyAsset.asset != null} "
                );
                if (!actionFinished.success || actionFinished.actionReturn == null) {
                    return new ActionFinished(
                        success: false,
                        errorMessage: $"Could not create asset `{bodyAsset.dynamicAsset}` error: {actionFinished.errorMessage}"
                    );
                }
                var assetData = actionFinished.actionReturn as Dictionary<string, object>;
                Debug.Log($"-- dynamicAsset id: {id} keys {string.Join(", ", assetData.Keys)}");
                spawnedMesh = assetData["gameObject"] as GameObject; //.transform.Find("mesh").gameObject;
            }
            return actionFinished;
        }

        protected override LayerMask GetVisibilityRaycastLayerMask(bool withSimObjInvisible = false) {
            // No agent because camera can be in the path of colliders
            string[] layers = new string[]
            {
                "SimObjVisible",
                "Procedural1",
                "Procedural2",
                "Procedural3",
                "Procedural0" //, "Agent"
            };
            if (withSimObjInvisible) {
                layers = layers.Append("SimObjInvisible").ToArray();
            }
            return LayerMask.GetMask(layers);
        }

        //override to ensure that the TeleportFull action calls fpin's version of teleportFull
        public override void TeleportFull(
            Vector3? position = null,
            Vector3? rotation = null,
            float? horizon = null,
            bool? standing = null,
            bool forceAction = false
        ) {
            teleportFull(
                position: position,
                rotation: rotation,
                horizon: horizon,
                forceAction: forceAction
            );
        }

        protected override void teleportFull(
            Vector3? position,
            Vector3? rotation,
            float? horizon,
            bool forceAction
        ) {
            //Debug.Log($"what even is the position passed in at the start? {position:F8}");
            if (
                rotation.HasValue
                && (
                    !Mathf.Approximately(rotation.Value.x, 0f)
                    || !Mathf.Approximately(rotation.Value.z, 0f)
                )
            ) {
                throw new ArgumentOutOfRangeException(
                    "No agents currently can change in pitch or roll. So, you must set rotation(x=0, y=yaw, z=0)."
                        + $" You gave {rotation.Value.ToString("F6")}."
                );
            }

            // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
            if (
                !forceAction
                && horizon.HasValue
                && (horizon.Value > maxDownwardLookAngle || horizon.Value < -maxUpwardLookAngle)
            ) {
                throw new ArgumentOutOfRangeException(
                    $"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]. You gave {horizon}."
                );
            }

            if (
                !forceAction
                && position.HasValue
                && !agentManager.SceneBounds.Contains(position.Value)
            ) {
                throw new ArgumentOutOfRangeException(
                    $"Teleport position {position.Value.ToString("F6")} out of scene bounds! Ignore this by setting forceAction=true."
                );
            }

            if (!forceAction && position.HasValue && !isPositionOnGrid(position.Value)) {
                throw new ArgumentOutOfRangeException(
                    $"Teleport position {position.Value.ToString("F6")} is not on the grid of size {gridSize}."
                );
            }

            // cache old values in case there's a failure
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            Vector3 oldCameraLocalEulerAngles = m_Camera.transform.localEulerAngles;

            //Debug.Log($"what are we trying to set the position to? {position.GetValueOrDefault(transform.position):F8}");

            // here we actually teleport
            autoSyncTransforms();

            transform.position = position.GetValueOrDefault(transform.position);
            transform.localEulerAngles = rotation.GetValueOrDefault(transform.localEulerAngles);
            m_Camera.transform.localEulerAngles = new Vector3(
                horizon.GetValueOrDefault(oldCameraLocalEulerAngles.x),
                oldCameraLocalEulerAngles.y,
                oldCameraLocalEulerAngles.z
            );

            //we teleported the agent a little bit above the ground just so we are clear, now snap agent flush with the floor
            this.assertTeleportedNearGround(
                targetPosition: position.GetValueOrDefault(transform.position)
            );

            if (!forceAction) {
                if (
                    isAgentCapsuleColliding(
                        collidersToIgnore: collidersToIgnoreDuringMovement,
                        includeErrorMessage: true
                    )
                ) {
                    transform.position = oldPosition;
                    transform.rotation = oldRotation;
                    autoSyncTransforms();
                    m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngles;
                    throw new InvalidOperationException(errorMessage);
                }

                if (
                    isAgentBoxColliding(
                        transformWithBoxCollider: spawnedBoxCollider.transform,
                        collidersToIgnore: collidersToIgnoreDuringMovement,
                        includeErrorMessage: true
                    )
                ) {
                    transform.position = oldPosition;
                    transform.rotation = oldRotation;
                    autoSyncTransforms();
                    m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngles;
                    throw new InvalidOperationException(errorMessage);
                }
            }

            actionFinished(success: true);
        }

        protected override void assertTeleportedNearGround(Vector3? targetPosition) {
            // position should not change if it's null.
            if (targetPosition == null) {
                return;
            }

            Vector3 pos = (Vector3)targetPosition;
            // we must sync the rigidbody prior to executing the
            // move otherwise the agent will end up in a different
            // location from the targetPosition
            autoSyncTransforms();
            m_CharacterController.Move(
                new Vector3(0f, Physics.gravity.y * this.m_GravityMultiplier, 0f)
            );
            autoSyncTransforms();

            // perhaps like y=2 was specified, with an agent's standing height of 0.9
            if (Mathf.Abs(transform.position.y - pos.y) > 1.0f) {
                throw new InvalidOperationException(
                    "After teleporting and adjusting agent position to floor, there was too large a change."
                        + " This may be due to the target teleport coordinates causing the agent to fall through the floor."
                        + $"({Mathf.Abs(transform.position.y - pos.y)} > 1.0f) in the y position."
                );
            }
        }

        public IEnumerator MoveAgent(
            bool returnToStart = true,
            float ahead = 0,
            float right = 0,
            float speed = 1
        ) {
            if (ahead == 0 && right == 0) {
                throw new ArgumentException("Must specify ahead or right!");
            }
            Vector3 direction = new Vector3(x: right, y: 0, z: ahead);

            CollisionListener collisionListener = fpinMovable.collisionListener;

            Vector3 directionWorld = transform.TransformDirection(direction);
            Vector3 targetPosition = transform.position + directionWorld;

            collisionListener.Reset();

            return ContinuousMovement.move(
                movable: fpinMovable,
                controller: this,
                moveTransform: this.transform,
                targetPosition: targetPosition,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                unitsPerSecond: speed,
                returnToStartPropIfFailed: returnToStart,
                localPosition: false
            );
        }

        public IEnumerator MoveAhead(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                ahead: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator MoveBack(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                ahead: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator MoveRight(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                right: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator MoveLeft(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                right: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator RotateRight(
            float? degrees = null,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            return RotateAgent(
                degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator RotateLeft(
            float? degrees = null,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            return RotateAgent(
                degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public virtual IEnumerator RotateAgent(
            float degrees,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            CollisionListener collisionListener = fpinMovable.collisionListener;
            collisionListener.Reset();

            // this.transform.Rotate()
            return ContinuousMovement.rotate(
                movable: this.fpinMovable,
                controller: this,
                moveTransform: this.transform,
                targetRotation: this.transform.rotation * Quaternion.Euler(0.0f, degrees, 0.0f),
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                radiansPerSecond: speed,
                returnToStartPropIfFailed: returnToStart
            );
        }
    }
}
