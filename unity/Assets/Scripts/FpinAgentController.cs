using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements;
using Thor.Procedural.Data;
using Thor.Procedural;
using MessagePack;

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
    public class FpinAgentController : PhysicsRemoteFPSAgentController{

        private static readonly Vector3 agentSpawnOffset = new Vector3(100.0f, 100.0f, 100.0f);
        private FpinMovableContinuous fpinMovable;
        public BoxCollider spawnedBoxCollider = null;
        public BoxCollider spawnedTriggerBoxCollider = null;
        public GameObject fpinVisibilityCapsule = null;

        public BoxBounds boxBounds = null;

        public BoxBounds BoxBounds {
            get {
                if(spawnedBoxCollider != null) {
                    BoxBounds currentBounds = new BoxBounds();

                    currentBounds.worldCenter = spawnedBoxCollider.transform.TransformPoint(spawnedBoxCollider.center);
                    currentBounds.size = spawnedBoxCollider.size;
                    currentBounds.agentRelativeCenter = this.transform.InverseTransformPoint(currentBounds.worldCenter);

                    boxBounds = currentBounds;

                    Debug.Log($"world center: {boxBounds.worldCenter}");
                    Debug.Log($"size: {boxBounds.size}");
                    Debug.Log($"agentRelativeCenter: {boxBounds.agentRelativeCenter}");
                } else { 
                    Debug.Log("why is it nullll");
                    return null;
                }

                return boxBounds;
            }
            set {
                boxBounds = value;
            }
        }

        public CollisionListener collisionListener;
        
        public FpinAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public void Start() {
            //put stuff we need here when we need it maybe
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

        public List<Vector3> SamplePointsOnNavMesh(
            int sampleCount, float maxDistance
        ) {
            float minX = agentManager.SceneBounds.min.x;
            float minZ = agentManager.SceneBounds.min.z;
            float maxX = agentManager.SceneBounds.max.x;
            float maxZ = agentManager.SceneBounds.max.z;

            Debug.Log($"Scene bounds: X: {minX} z: {minZ} max x: {maxX} z: {maxZ}");

            int n = (int) Mathf.Ceil(Mathf.Sqrt(sampleCount));

            List<Vector3> initPoints = new List<Vector3>();
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    initPoints.Add(new Vector3(
                        Mathf.Lerp(minX, maxX, (i + 0.5f) / n),
                        0f,
                        Mathf.Lerp(minZ, maxZ, (j + 0.5f) / n)
                    ));
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
                    Debug.DrawLine(hit.position, hit.position + new Vector3(0f, 0.1f, 0f), Color.cyan, 15f);
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
            float yOffset = 0.01f + transform.position.y - b.min.y;

            bool success = false;
            foreach (Vector3 point in pointsOnMesh) {
                try {
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

        private Bounds GetAgentBoundsFromMesh(GameObject gameObject, Type agentType) {
            Bounds bounds = new Bounds(gameObject.transform.position, Vector3.zero);
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            if (agentType == typeof(LocobotFPSAgentController)) {
                meshRenderers = this.baseAgentComponent.BotVisCap.GetComponentsInChildren<MeshRenderer>();
            } else if (agentType == typeof(StretchAgentController)) {
                meshRenderers = this.baseAgentComponent.StretchVisCap.GetComponentsInChildren<MeshRenderer>();
            } else if (agentType == typeof(FpinAgentController)) { 
                meshRenderers = this.baseAgentComponent.VisibilityCapsule.GetComponentsInChildren<MeshRenderer>();
            }
            foreach (MeshRenderer meshRenderer in meshRenderers) {
                bounds.Encapsulate(meshRenderer.bounds);
            }
            return bounds;
        }

        public BoxBounds spawnAgentBoxCollider(
            GameObject agent, 
            Type agentType, 
            Vector3 scaleRatio, 
            bool useAbsoluteSize = false, 
            bool useVisibleColliderBase = false,
            float originOffsetX = 0.0f, 
            float originOffsetY = 0.0f, 
            float originOffsetZ = 0.0f
            ) {
            // Store the current rotation
            Vector3 originalPosition = this.transform.position;
            Quaternion originalRotation = this.transform.rotation;

            //Debug.Log($"the original position of the agent is: {originalPosition:F8}");

            // Move the agent to a safe place and align the agent's rotation with the world coordinate system
            this.transform.position = originalPosition + agentSpawnOffset;
            this.transform.rotation = Quaternion.identity;

            //Debug.Log($"agent position after moving it out of the way is: {this.transform.position:F8}");

            // Get the agent's bounds
            var bounds = GetAgentBoundsFromMesh(agent, agentType);

            // Check if the spawned boxCollider is colliding with other objects
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            
            
            Vector3 newBoxCenter = bounds.center - agentSpawnOffset;

            // var m = (newBoxCenter +  bounds.extents) - originalPosition;
            
            newBoxCenter = originalRotation * (newBoxCenter - originalPosition) + originalPosition;
            Vector3 newBoxExtents = new Vector3(
                scaleRatio.x * bounds.extents.x,
                scaleRatio.y * bounds.extents.y,
                scaleRatio.z * bounds.extents.z
            );
            if (useAbsoluteSize){
                newBoxExtents = new Vector3(
                    scaleRatio.x / 2,
                    scaleRatio.y / 2,
                    scaleRatio.z / 2
                );
            }  

            #if UNITY_EDITOR
            /////////////////////////////////////////////////
            //for visualization lets spawna cube at the center of where the boxCenter supposedly is
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "VisualizedBoxCollider";
            cube.transform.position = newBoxCenter;
            cube.transform.rotation = originalRotation;

            cube.transform.localScale = newBoxExtents * 2;
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
            ////////////////////////////////////////////////
            #endif

            if (Physics.CheckBox(newBoxCenter, newBoxExtents, originalRotation, layerMask)) {
                // throw new InvalidOperationException(
                //     "Spawned box collider is colliding with other objects. Cannot spawn box collider."
                // );
            }
            
            // Move the agent back to its original position and rotation because CheckBox passed
            this.transform.rotation = originalRotation;
            this.transform.position = originalPosition;

            // agent.transform.localPosition = agent.transform.localPosition + toBottomBoxOffset;
            Physics.SyncTransforms();

            // Spawn the box collider
            Vector3 colliderSize = newBoxExtents * 2;

            var nonTriggerBox = new GameObject("NonTriggeredEncapsulatingBox");
            nonTriggerBox.transform.position = newBoxCenter;

            spawnedBoxCollider = nonTriggerBox.AddComponent<BoxCollider>();
            spawnedBoxCollider.size = colliderSize; // Scale the box to the agent's size
            spawnedBoxCollider.enabled = true;
            spawnedBoxCollider.transform.parent = agent.transform;

            // Attatching it to the parent changes the rotation so set it back to none
            spawnedBoxCollider.transform.localRotation = Quaternion.identity;

            var triggerBox = new GameObject("triggeredEncapsulatingBox");
            triggerBox.transform.position = newBoxCenter;

            spawnedTriggerBoxCollider = triggerBox.AddComponent<BoxCollider>();
            spawnedTriggerBoxCollider.size = colliderSize; // Scale the box to the agent's size
            spawnedTriggerBoxCollider.enabled = true;
            spawnedTriggerBoxCollider.isTrigger = true;
            spawnedTriggerBoxCollider.transform.parent = agent.transform;

            // triggeredEncapsulatingBox.transform.localRotation = Quaternion.identity;
            // Attatching it to the parent changes the rotation so set it back to identity
            spawnedTriggerBoxCollider.transform.localRotation = Quaternion.identity;

            //make sure to set the collision layer correctly as part of the `agent` layer so the collision matrix is happy
            spawnedBoxCollider.transform.gameObject.layer = LayerMask.NameToLayer("Agent");
            spawnedTriggerBoxCollider.transform.gameObject.layer = LayerMask.NameToLayer("Agent");

            // Spawn the visible box if useVisibleColliderBase is true
            if (useVisibleColliderBase){
                colliderSize = new Vector3(colliderSize.x, 0.15f * colliderSize.y, colliderSize.z);
                GameObject visibleBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visibleBox.name = "VisibleBox";
                visibleBox.transform.position = new Vector3(newBoxCenter.x, newBoxCenter.y - newBoxExtents.y, newBoxCenter.z);
                visibleBox.transform.localScale = colliderSize;
                visibleBox.transform.parent = agent.transform;

                var bc = visibleBox.GetComponent<BoxCollider>();
                //also offset it by the distance from this box's transform to the bottom of its extents
                var distanceToBottomOfVisibleBoxCollider = bc.size.y * 0.5f * bc.transform.localScale.y;
                bc.enabled = false;

                visibleBox.transform.localPosition = new Vector3(visibleBox.transform.localPosition.x, visibleBox.transform.localPosition.y + distanceToBottomOfVisibleBoxCollider, visibleBox.transform.localPosition.z);
                // Attatching it to the parent changes the rotation so set it back to none
                visibleBox.transform.localRotation = Quaternion.identity;
            }
            
            repositionAgentOrigin(newRelativeOrigin: new Vector3 (originOffsetX, originOffsetY, originOffsetZ));

            //BoxBounds should now be able to retrieve current box information
            return BoxBounds;
        }

        //helper function to remove the currently generated agent box collider
        //make sure to follow this up with a subsequent generation so BoxBounds isn't left null
        public void destroyAgentBoxCollider(){
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

        //no need for this anymore as we can now do this via InitializeBody directly
        // public void UpdateAgentBoxCollider(Vector3 colliderScaleRatio, bool useAbsoluteSize = false, bool useVisibleColliderBase = false) {
        //     this.destroyAgentBoxCollider();
        //     this.spawnAgentBoxCollider(this.gameObject, this.GetType(), colliderScaleRatio, useAbsoluteSize, useVisibleColliderBase);
        //     actionFinished(true);
        //     return;
        // }

        public Transform CopyMeshChildren(GameObject source, GameObject target) {
            // Initialize the recursive copying process
            //Debug.Log($"is null {source == null} {target == null}");
            return CopyMeshChildrenRecursive(source.transform, target.transform);
        }

        private Transform CopyMeshChildrenRecursive(Transform sourceTransform, Transform targetParent, bool isTopMost = true) {
            Transform thisTransform = null;
            foreach (Transform child in sourceTransform) {
                GameObject copiedChild = null;
                // Check if the child has a MeshFilter component
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter != null) {
                    copiedChild = CopyMeshToTarget(child, targetParent);
                }

                // Process children only if necessary (i.e., they contain MeshFilters)
                if (HasMeshInChildrenOrSelf(child)) {
                    Transform parentForChildren = (copiedChild != null) ? copiedChild.transform : CreateContainerForHierarchy(child, targetParent).transform;
                    CopyMeshChildrenRecursive(child, parentForChildren, false);
                    if (isTopMost) {
                        thisTransform = parentForChildren;
                    }
                }
            }
            //organize the heirarchy of all the meshes copied under a single vis cap so we can use it real nice
            if (isTopMost) {
                GameObject viscap = new GameObject("fpinVisibilityCapsule");

                Debug.Log($"what is thisTransform: {thisTransform.name}");

                //get teh bounds of all the meshes we have copied over so far
                Bounds thisBounds = new Bounds(thisTransform.position, Vector3.zero);

                MeshRenderer[] meshRenderers = thisTransform.gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach(MeshRenderer mr in meshRenderers) {
                    thisBounds.Encapsulate(mr.bounds);
                }

                Debug.Log($"thisBounds center is now at {thisBounds.center}, and the size is {thisBounds.size}");
                Debug.Log($"world position of the bottom of the bounds is {thisBounds.min.y}");

                float distanceFromTransformToBottomOfBounds = thisTransform.position.y - thisBounds.min.y;

                Debug.Log($"distance from transform to bottom of bounds {distanceFromTransformToBottomOfBounds}");

                //set all the meshes up as children of the viscap
                thisTransform.SetParent(viscap.transform);
                thisTransform.localPosition = new Vector3(0.0f, distanceFromTransformToBottomOfBounds, 0.0f);

                Physics.SyncTransforms();

                //update bounds again because we have no moved in world space
                thisBounds = new Bounds(thisTransform.position, Vector3.zero);
                meshRenderers = thisTransform.gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach(MeshRenderer mr in meshRenderers) {
                    thisBounds.Encapsulate(mr.bounds);
                }

                Debug.Log($"thisBounds center is now at {thisBounds.center}, and the size is {thisBounds.size}");

                Vector3 dirFromBoundsCenterToVisCapTransform = viscap.transform.position - thisBounds.center;
                Debug.Log($"dirFromBoundsCenterToVisCapTransform: {dirFromBoundsCenterToVisCapTransform:f8}");

                thisTransform.localPosition = new Vector3(dirFromBoundsCenterToVisCapTransform.x, thisTransform.localPosition.y, dirFromBoundsCenterToVisCapTransform.z);

                Physics.SyncTransforms();

                thisTransform.localRotation = Quaternion.identity;

                Physics.SyncTransforms();

                //set viscap up as child of FPSAgent
                viscap.transform.SetParent(targetParent);
                Physics.SyncTransforms();
                viscap.transform.localPosition = Vector3.zero;
                Physics.SyncTransforms();
                viscap.transform.localRotation = Quaternion.identity;
                viscap.transform.localScale = new Vector3(1, 1, 1);
                

                //return reference to viscap so we can scaaaale it
                fpinVisibilityCapsule = viscap;
                return viscap.transform;
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


        public ActionFinished GetBoxBounds() {
            return new ActionFinished() {
                success = true,
                actionReturn = this.BoxBounds
            };
        }

        public ActionFinished Initialize(
            BodyAsset bodyAsset,
            // TODO: do we want to allow non relative to the box offsets?
            float originOffsetX = 0.0f,
            float originOffsetY = 0.0f,
            float originOffsetZ = 0.0f,
            Vector3? colliderScaleRatio = null,  
            bool useAbsoluteSize = false, 
            bool useVisibleColliderBase = false
        ) {
            var actionFinished = this.InitializeBody(
                bodyAsset: bodyAsset,
                originOffsetX: originOffsetX,
                originOffsetY: originOffsetY,
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
            BodyAsset bodyAsset,
            // TODO: do we want to allow non relative to the box offsets?
            float originOffsetX = 0.0f,
            float originOffsetY = 0.0f,
            float originOffsetZ = 0.0f,
            Vector3? colliderScaleRatio = null,  
            bool useAbsoluteSize = false, 
            bool useVisibleColliderBase = false
        ) {
            //spawn in a default mesh to base the created box collider on
            var spawnAssetActionFinished = spawnBodyAsset(bodyAsset, out GameObject spawnedMesh);
            // Return early if spawn failed
            if (!spawnAssetActionFinished.success) {
                return spawnAssetActionFinished;
            }

            //remove any previously generated colliders
            destroyAgentBoxCollider();
            
            //remove old fpin visibility capsule since we are using a new mesh
            if (fpinVisibilityCapsule != null) {
                UnityEngine.Object.DestroyImmediate(fpinVisibilityCapsule);
            }

            //copy all mesh renderers found on the spawnedMesh onto this agent now
            Transform visCap = CopyMeshChildren(source: spawnedMesh.transform.gameObject, target: this.transform.gameObject);
            //This is where we would scale the spawned meshes based on the collider scale but uhhhhhhhHHHHHHHHHHH
            // Vector3 ratio = colliderScaleRatio.GetValueOrDefault(Vector3.one);
            // Vector3 newVisCapScale = new Vector3(
            //     ratio.x * visCap.localScale.x,
            //     ratio.y * visCap.localScale.y,
            //     ratio.z * visCap.localScale.z
            // );
            // if(useAbsoluteSize){
            //     newVisCapScale = new Vector3(ratio.x, ratio.y, ratio.z);
            // }
            // visCap.localScale = newVisCapScale;

            //remove the spawned mesh cause we are done with it
            foreach (var sop in spawnedMesh.GetComponentsInChildren<SimObjPhysics>()) {
                agentManager.physicsSceneManager.RemoveFromObjectsInScene(sop);
            }
            if (spawnedMesh.activeInHierarchy) {
                UnityEngine.Object.DestroyImmediate(spawnedMesh);
            }

            //assign agent visibility capsule to new meshes
            VisibilityCapsule = visCap.transform.gameObject;

            //ok now create box collider based on the mesh
            this.spawnAgentBoxCollider(
                agent: this.gameObject,
                agentType: this.GetType(),
                scaleRatio: colliderScaleRatio.GetValueOrDefault(Vector3.one),
                useAbsoluteSize: useAbsoluteSize,
                useVisibleColliderBase: useVisibleColliderBase, 
                originOffsetX: originOffsetX,
                originOffsetY: originOffsetY,
                originOffsetZ: originOffsetZ
            );

            //reposition agent transform relative to the generated box
            //i think we need to unparent the FPSController from all its children.... then reposition
            //repositionAgentOrigin(newRelativeOrigin: new Vector3 (originOffsetX, originOffsetY, originOffsetZ));

            Physics.SyncTransforms();

            //adjust agent character controller and capsule according to extents of box collider
            var characterController = this.GetComponent<CharacterController>();
            var myBox = spawnedBoxCollider.GetComponent<BoxCollider>();

            // Transform the box collider's center to the world space and then into the capsule collider's local space
            Vector3 boxCenterWorld = myBox.transform.TransformPoint(myBox.center);
            Vector3 boxCenterCapsuleLocal = characterController.transform.InverseTransformPoint(boxCenterWorld);

            // Now the capsule's center can be set to the transformed center of the box collider
            characterController.center = boxCenterCapsuleLocal;

            // Adjust the capsule size
            // Set the height to the smallest dimension of the box
            //float minHeight = Mathf.Min(myBox.size.x, myBox.size.y, myBox.size.z);
            //characterController.height = minHeight;
            float boxHeight = myBox.size.y;
            characterController.height = boxHeight;

            // Set the radius to fit inside the box, considering the smallest width or depth
            float minRadius = Mathf.Min(myBox.size.x, myBox.size.z) / 2f;
            characterController.radius = minRadius;

            //ok now also adjust this for the trigger capsule collider of the agent.
            var myTriggerCap = this.GetComponent<CapsuleCollider>();
            myTriggerCap.center = boxCenterCapsuleLocal;
            myTriggerCap.height = boxHeight;
            myTriggerCap.radius = minRadius;

            //ok recalibrate navmesh child component based on the new agent capsule now that its updated
            var navmeshchild = this.transform.GetComponentInChildren<NavMeshAgent>();
            navmeshchild.transform.localPosition = new Vector3(boxCenterCapsuleLocal.x, 0.0f, boxCenterCapsuleLocal.z);
            navmeshchild.baseOffset = 0.0f;
            navmeshchild.height = boxHeight;
            navmeshchild.radius = minRadius;

            //enable cameras I suppose
            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            fpinMovable = new FpinMovableContinuous(this.GetComponentInParent<CollisionListener>());

            return new ActionFinished(spawnAssetActionFinished) {
                // TODO: change to a proper class once metadata return is defined
                actionReturn = new Dictionary<string, object>() {
                    {"objectSphereBounds", spawnAssetActionFinished.actionReturn as ObjectSphereBounds},
                    {"BoxBounds", this.BoxBounds},
                    {"cameraNearPlane", m_Camera.nearClipPlane},
                    {"cameraFarPlane", m_Camera.farClipPlane}
                }
            };
        }

        private ActionFinished spawnBodyAsset(BodyAsset bodyAsset, out GameObject spawnedMesh) {
            if (bodyAsset == null) {
                throw new ArgumentNullException("bodyAsset is null");
            }
            else if (bodyAsset.assetId == null && bodyAsset.dynamicAsset == null && bodyAsset.asset == null) {
                throw new ArgumentNullException("`bodyAsset.assetId`, `bodyAsset.dynamicAsset` or `bodyAsset.asset` must be provided all are null.");
            }
            ActionFinished actionFinished = new ActionFinished(success: false, errorMessage: "No body specified");
            spawnedMesh = null;
            

            if ( (bodyAsset.dynamicAsset != null || bodyAsset.asset != null) && bodyAsset.assetId == null) {
                var id = bodyAsset.dynamicAsset != null ? bodyAsset.dynamicAsset.id : bodyAsset.asset.name;

                var assetMap = ProceduralTools.getAssetMap();
                // Check if asset is in AssetDatabase already 
                if (assetMap.ContainsKey(id)) {
                    Debug.Log("------- Already contains key");
                    bodyAsset.assetId = id;
                }
            }
            

            if (bodyAsset.assetId != null) {
                actionFinished = SpawnAsset(bodyAsset.assetId, "agentMesh", new Vector3(200f, 200f, 200f));
                spawnedMesh = GameObject.Find("agentMesh");
            }
            else if (bodyAsset.dynamicAsset != null) {
                actionFinished = this.CreateRuntimeAsset(
                    id: bodyAsset.dynamicAsset.id,
                    dir: bodyAsset.dynamicAsset.dir,
                    extension: bodyAsset.dynamicAsset.extension,
                    annotations: bodyAsset.dynamicAsset.annotations,
                    serializable: true
                );
                
            }
            else if (bodyAsset.asset != null) {
                bodyAsset.asset.serializable = true;
                actionFinished = this.CreateRuntimeAsset(
                    asset: bodyAsset.asset
                );
            }
            if (bodyAsset.assetId == null && (bodyAsset.dynamicAsset != null || bodyAsset.asset != null)) {

                var id = bodyAsset.dynamicAsset != null ? bodyAsset.dynamicAsset.id : bodyAsset.asset.name;
                Debug.Log($"-- checks {bodyAsset.assetId == null} {bodyAsset.dynamicAsset != null} {bodyAsset.asset != null} ");
                if (!actionFinished.success || actionFinished.actionReturn == null) {
                    return new ActionFinished(
                        success: false,
                        errorMessage: $"Could not create asset `{bodyAsset.dynamicAsset}` error: {actionFinished.errorMessage}"
                    );
                }
                var assetData = actionFinished.actionReturn as Dictionary<string, object>;
                Debug.Log($"-- dynamicAsset id: {id} keys {string.Join(", ", assetData.Keys)}");
                spawnedMesh = assetData["gameObject"] as GameObject;//.transform.Find("mesh").gameObject;
            }
            return actionFinished;
        }

        protected override void teleportFull(
            Vector3? position,
            Vector3? rotation,
            float? horizon,
            bool forceAction
        ) {
            if (rotation.HasValue && (!Mathf.Approximately(rotation.Value.x, 0f) || !Mathf.Approximately(rotation.Value.z, 0f))) {
                throw new ArgumentOutOfRangeException(
                    "No agents currently can change in pitch or roll. So, you must set rotation(x=0, y=yaw, z=0)." +
                    $" You gave {rotation.Value.ToString("F6")}."
                );
            }

            // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
            if (!forceAction && horizon.HasValue && (horizon.Value > maxDownwardLookAngle || horizon.Value < -maxUpwardLookAngle)) {
                throw new ArgumentOutOfRangeException(
                    $"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]. You gave {horizon}."
                );
            }

            if (!forceAction && position.HasValue && !agentManager.SceneBounds.Contains(position.Value)) {
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

            // here we actually teleport
            transform.position = position.GetValueOrDefault(transform.position);
            transform.localEulerAngles = rotation.GetValueOrDefault(transform.localEulerAngles);
            m_Camera.transform.localEulerAngles = new Vector3(
                horizon.GetValueOrDefault(oldCameraLocalEulerAngles.x),
                oldCameraLocalEulerAngles.y,
                oldCameraLocalEulerAngles.z
            );

            if (!forceAction) {

                if (isAgentCapsuleColliding(collidersToIgnore: collidersToIgnoreDuringMovement, includeErrorMessage: true)) {
                    transform.position = oldPosition;
                    transform.rotation = oldRotation;
                    m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngles;
                    throw new InvalidOperationException(errorMessage);
                }

                if (isAgentBoxColliding(collidersToIgnore: collidersToIgnoreDuringMovement, includeErrorMessage: true)) {
                    transform.position = oldPosition;
                    transform.rotation = oldRotation;
                    m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngles;
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        //assumes the agent origin will only be repositioned via local x and z values relative to
        //the generated box collider's center. This will automatically set the local Y value
        //to the bottom of the spawned box collider's lowest extent in the -Y direction
        public void repositionAgentOrigin (Vector3 newRelativeOrigin) {
            //get the world coordinates of the center of the spawned box
            var addedCollider = spawnedBoxCollider.GetComponent<BoxCollider>();
            Vector3 spawnedBoxWorldCenter = spawnedBoxCollider.transform.TransformPoint(spawnedBoxCollider.GetComponent<BoxCollider>().center);

            List<Transform> allMyChildren = new List<Transform>();

            foreach (Transform child in this.transform) {
                allMyChildren.Add(child);
            }
            //OK WHY DONT WE JUST DO THIS IN THE ABOVE LOOP WELL LET ME TELL YOU WHY
            //TURNS OUT the SetParent() call doesn't execute instantly as it seems to rely on
            //the transform heirarchy changing and the order is ambiguous??
            foreach(Transform child in allMyChildren) {
                child.SetParent(null);
            }

            //ensure all transforms are fully updated
            Physics.SyncTransforms();

            //ok now reposition this.transform in world space relative to the center of the box collider
            this.transform.SetParent(spawnedBoxCollider.transform);

            float distanceToBottom = addedCollider.size.y * 0.5f * addedCollider.transform.localScale.y;
            Vector3 origin = new Vector3(newRelativeOrigin.x, newRelativeOrigin.y - distanceToBottom, newRelativeOrigin.z);
            this.transform.localPosition = origin;

            //ensure all transforms are fully updated
            Physics.SyncTransforms();

            //ok now reparent everything accordingly
            this.transform.SetParent(null);
            Physics.SyncTransforms();

            foreach(Transform child in allMyChildren) {
                child.SetParent(this.transform);
            }
            Physics.SyncTransforms();
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
