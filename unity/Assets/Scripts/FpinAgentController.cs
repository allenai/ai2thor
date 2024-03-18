using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class FpinAgentController : PhysicsRemoteFPSAgentController {

        private static readonly Vector3 agentSpawnOffset = new Vector3(100.0f, 100.0f, 100.0f);
        public GameObject spawnedBoxCollider;
        public GameObject spawnedTriggerBoxCollider;
        public FpinAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        void Start() {
            //put stuff we need here when we need it maybe
        }

        private Bounds GetAgentBoundsFromMesh(GameObject gameObject, Type agentType) {
            Debug.Log(agentType);
            Debug.Log(typeof(StretchAgentController));
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

        public void spawnAgentBoxCollider(GameObject agent, Type agentType, Vector3 scaleRatio, bool useAbsoluteSize = false, bool useVisibleColliderBase = false) {
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

            //Debug.Log($"the global position of the agent bounds is: {bounds.center:F8}");

            // Check if the spawned boxCollider is colliding with other objects
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            
            Vector3 newBoxCenter = bounds.center - agentSpawnOffset;
            newBoxCenter = originalRotation * (newBoxCenter - originalPosition) + originalPosition;
            Vector3 newBoxExtents = new Vector3(
                scaleRatio.x * bounds.extents.x,
                scaleRatio.y * bounds.extents.y,
                scaleRatio.z * bounds.extents.z
            );
            if (useAbsoluteSize){
                newBoxExtents = new Vector3(
                    scaleRatio.x,
                    scaleRatio.y,
                    scaleRatio.z
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

            
            // And rotation should be originalRotation * boxRotation but since it's a world-axis-aligned bounding box boxRotation is Identity
            if (Physics.CheckBox(newBoxCenter, newBoxExtents, originalRotation, layerMask)) {
                this.transform.position = originalPosition;
                this.transform.rotation = originalRotation;
                throw new InvalidOperationException(
                    "Spawned box collider is colliding with other objects. Cannot spawn box collider."
                );
            }

            // Move the agent back to its original position and rotation
            this.transform.rotation = originalRotation;
            this.transform.position = originalPosition;

            // Spawn the box collider
            Vector3 colliderSize = newBoxExtents * 2;

            spawnedBoxCollider = new GameObject("NonTriggeredEncapsulatingBox");
            spawnedBoxCollider.transform.position = newBoxCenter;

            BoxCollider nonTriggeredBoxCollider = spawnedBoxCollider.AddComponent<BoxCollider>();
            nonTriggeredBoxCollider.size = colliderSize; // Scale the box to the agent's size
            nonTriggeredBoxCollider.enabled = true;

            spawnedBoxCollider.transform.parent = agent.transform;
            // Attatching it to the parent changes the rotation so set it back to none
            spawnedBoxCollider.transform.localRotation = Quaternion.identity;

            spawnedTriggerBoxCollider = new GameObject("triggeredEncapsulatingBox");
            spawnedTriggerBoxCollider.transform.position = newBoxCenter;

            BoxCollider triggeredBoxCollider = spawnedTriggerBoxCollider.AddComponent<BoxCollider>();
            triggeredBoxCollider.size = colliderSize; // Scale the box to the agent's size
            triggeredBoxCollider.enabled = true;
            triggeredBoxCollider.isTrigger = true;
            spawnedTriggerBoxCollider.transform.parent = agent.transform;

            // triggeredEncapsulatingBox.transform.localRotation = Quaternion.identity;
            // Attatching it to the parent changes the rotation so set it back to identity
            spawnedTriggerBoxCollider.transform.localRotation = Quaternion.identity;

            //make sure to set the collision layer correctly as part of the `agent` layer so the collision matrix is happy
            spawnedBoxCollider.layer = LayerMask.NameToLayer("Agent");
            spawnedTriggerBoxCollider.layer = LayerMask.NameToLayer("Agent");

            // Spawn the visible box if useVisibleColliderBase is true
            if (useVisibleColliderBase){
                colliderSize = new Vector3(colliderSize.x, 0.15f, colliderSize.z);
                GameObject visibleBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visibleBox.name = "VisibleBox";
                visibleBox.transform.position = new Vector3(newBoxCenter.x, newBoxCenter.y - newBoxExtents.y + 0.1f, newBoxCenter.z);
                visibleBox.transform.localScale = colliderSize;
                visibleBox.transform.parent = agent.transform;
                // Attatching it to the parent changes the rotation so set it back to none
                visibleBox.transform.localRotation = Quaternion.identity;
            }
        }

        public void DestroyAgentBoxCollider(){
            GameObject nonTriggeredEncapsulatingBox = GameObject.Find("NonTriggeredEncapsulatingBox");
            GameObject triggeredEncapsulatingBox = GameObject.Find("triggeredEncapsulatingBox");
            GameObject visibleBox = GameObject.Find("VisibleBox");
            if (nonTriggeredEncapsulatingBox != null) {
                GameObject.Destroy(nonTriggeredEncapsulatingBox);
            }
            if (triggeredEncapsulatingBox != null) {
                GameObject.Destroy(triggeredEncapsulatingBox);
            }
            if (visibleBox != null) {
                GameObject.Destroy(visibleBox);
            }
            #if UNITY_EDITOR
            GameObject visualizedBoxCollider = GameObject.Find("VisualizedBoxCollider");
            if (visualizedBoxCollider != null) {
                GameObject.Destroy(visualizedBoxCollider);
            }
            #endif
            actionFinished(true);
            return;
        }

        public void UpdateAgentBoxCollider(Vector3 colliderScaleRatio, bool useAbsoluteSize = false, bool useVisibleColliderBase = false) {
            this.DestroyAgentBoxCollider();
            this.spawnAgentBoxCollider(this.gameObject, this.GetType(), colliderScaleRatio, useAbsoluteSize, useVisibleColliderBase);
            actionFinished(true);
            return;
        }
        public void CopyMeshChildren(GameObject source, GameObject target) {
            // Initialize the recursive copying process
            CopyMeshChildrenRecursive(source.transform, target.transform);
        }

        private void CopyMeshChildrenRecursive(Transform sourceTransform, Transform targetParent, bool isTopMost = true) {
            Transform thisTransform = null;

            foreach (Transform child in sourceTransform) {
                GameObject copiedChild = null;

                // Check if the child has a MeshFilter component
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                if (meshFilter != null) {
                    copiedChild = CopyMeshToTarget(child, targetParent);
                }

                // Process children only if necessary (i.e., they contain MeshFilters)
                if (HasMeshInChildren(child)) {
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
                thisTransform.SetParent(viscap.transform);
                thisTransform.localPosition = Vector3.zero;
                thisTransform.localRotation = Quaternion.identity;

                viscap.transform.SetParent(targetParent);
                viscap.transform.localPosition = Vector3.zero;
                viscap.transform.localRotation = Quaternion.identity;
                viscap.transform.localScale = new Vector3(1, 1, 1);
            }
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

        private bool HasMeshInChildren(Transform transform) {
            foreach (Transform child in transform) {
                if (child.GetComponent<MeshFilter>() != null || HasMeshInChildren(child)) {
                    return true;
                }
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

        public new void Initialize(ServerAction action) {

            this.InitializeBody(action);

        }

        public override void InitializeBody(ServerAction initializeAction) {
            VisibilityCapsule = null;

            Debug.Log("running InitializeBody in FpingAgentController");

            if (initializeAction.assetId == null) {
                throw new ArgumentNullException("assetId is null");
            }

            //spawn in a default mesh to base the created box collider on
            SpawnAsset(initializeAction.assetId, "agentMesh", new Vector3(200f, 200f, 200f));
            var spawnedMesh = GameObject.Find("agentMesh");

            //copy all mesh renderers found on the spawnedMesh onto this agent now
            CopyMeshChildren(source: spawnedMesh.transform.gameObject, target: this.transform.gameObject);

            //remove the spawned mesh cause we are done with it
            UnityEngine.Object.DestroyImmediate(spawnedMesh);

            //assign agent visibility capsule to new meshes
            VisibilityCapsule = GameObject.Find("fpinVisibilityCapsule");

            //ok now create box collider based on the mesh
            this.spawnAgentBoxCollider(
                agent: this.gameObject,
                agentType: this.GetType(),
                scaleRatio: initializeAction.colliderScaleRatio,
                useAbsoluteSize: initializeAction.useAbsoluteSize,
                useVisibleColliderBase: initializeAction.useVisibleColliderBase
            );

            var spawnedBox = GameObject.Find("NonTriggeredEncapsulatingBox");
            //reposition agent transform relative to the generated box
            //i think we need to unparent the FPSController from all its children.... then reposition
            repositionAgentOrigin(newRelativeOrigin: new Vector3 (initializeAction.newRelativeOriginX, 0.0f, initializeAction.newRelativeOriginZ));

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

            //default camera position somewhere??????
            // m_Camera.transform.localPosition = defaultMainCameraLocalPosition;
            // m_Camera.transform.localEulerAngles = defaultMainCameraLocalRotation;
            // m_Camera.fieldOfView = defaultMainCameraFieldOfView;

            //probably don't need camera limits since we are going to manipulate camera via the updateCameraProperties

        }

        //function to reassign the agent's origin relativ to the spawned in box collider
        //should be able to use this after initialization as well to adjust the origin on the fly as needed
        public void RepositionAgentOrigin(Vector3 newRelativeOrigin) {
            repositionAgentOrigin(newRelativeOrigin);
            actionFinishedEmit(true);
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
            Vector3 origin = new Vector3(newRelativeOrigin.x, 0.0f - distanceToBottom, newRelativeOrigin.z);
            this.transform.localPosition = origin;

            //ensure all transforms are fully updated
            Physics.SyncTransforms();

            //ok now reparent everything accordingly
            this.transform.SetParent(null);
            Physics.SyncTransforms();

            foreach(Transform child in allMyChildren) {
                child.SetParent(this.transform);
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

            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();

            Vector3 directionWorld = transform.TransformDirection(direction);
            Vector3 targetPosition = transform.position + directionWorld;

            collisionListener.Reset();

            return ContinuousMovement.move(
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
            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();
            collisionListener.Reset();

            // this.transform.Rotate()
            return ContinuousMovement.rotate(
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
