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
        public FpinAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        void Start() {
            //put stuff we need here when we need it maybe
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


            //adjust agent character controller and capsule according to extents of box collider

            //enable cameras I suppose
            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            //default camera position somewhere??????
            // m_Camera.transform.localPosition = defaultMainCameraLocalPosition;
            // m_Camera.transform.localEulerAngles = defaultMainCameraLocalRotation;
            // m_Camera.fieldOfView = defaultMainCameraFieldOfView;

            //probably don't need camera limits since we are going to manipulate camera via the updateCameraProperties

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
