using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;

namespace UnityStandardAssets.Characters.FirstPerson {
    public partial class ArticulatedAgentController : ArmAgentController {
        public ArticulatedAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }
        public ArticulatedAgentSolver agent;

        [SerializeField]
        private Collider FloorCollider;

        [SerializeField]
        private PhysicMaterial FloorColliderPhysicsMaterial;

        private CapsuleData originalCapsule;

        protected override CapsuleData GetAgentCapsule() {
            Debug.Log("calling Override GetAgentCapsule in ArticulatedAgentController");
            if (originalCapsule == null) {
                var cc = this.GetComponent<CapsuleCollider>();

                return new CapsuleData {
                    radius = cc.radius,
                    height = cc.height,
                    center = cc.center,
                    transform = cc.transform
                };
            } else {
                return originalCapsule;
            }
        }

        // TODO: Reimplement for Articulation body
        public override void InitializeBody(ServerAction initializeAction) {
            // TODO; Articulation Body init
            VisibilityCapsule = StretchVisCap;
            m_CharacterController.center = new Vector3(0, 1.5f, 0);
            m_CharacterController.radius = 0.01f;
            m_CharacterController.height = 0.02f;
            m_CharacterController.skinWidth = 0.01f;

            var ab = this.GetComponent<ArticulationBody>();
            ab.TeleportRoot(this.transform.position, this.transform.rotation);

            // TODO: REMOVE
            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            originalCapsule = new CapsuleData {
                radius = cc.radius,
                height = cc.height,
                center = cc.center,
                transform = cc.transform
            };
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;

            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            // // camera position
            // m_Camera.transform.localPosition = new Vector3(0, 0.378f, 0.0453f);

            // camera FOV
            m_Camera.fieldOfView = 69f;

            // set camera stand/crouch local positions for Tall mode
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition;

            // // set secondary arm-camera
            // Camera fp_camera_2 = m_CharacterController.transform.Find("SecondaryCamera").GetComponent<Camera>();
            // fp_camera_2.gameObject.SetActive(true);
            // fp_camera_2.transform.localPosition = new Vector3(0.0353f, 0.5088f, -0.076f);
            // fp_camera_2.transform.localEulerAngles = new Vector3(45f, 90f, 0f);
            // fp_camera_2.fieldOfView = 90f;

            if (initializeAction != null) {

                if (initializeAction.cameraNearPlane > 0) {
                    m_Camera.nearClipPlane = initializeAction.cameraNearPlane;
                    // fp_camera_2.nearClipPlane = initializeAction.cameraNearPlane;
                }

                if (initializeAction.cameraFarPlane > 0) {
                    m_Camera.farClipPlane = initializeAction.cameraFarPlane;
                    // fp_camera_2.farClipPlane = initializeAction.cameraFarPlane;
                }

            }

            // //            fp_camera_2.fieldOfView = 75f;
            // agentManager.registerAsThirdPartyCamera(fp_camera_2);

            // limit camera from looking too far down
            this.maxDownwardLookAngle = 90f;
            this.maxUpwardLookAngle = 25f;

            // // enable stretch arm component
            // Debug.Log("initializing stretch arm AB");
            // StretchArm.SetActive(true);
            // SArm = this.GetComponentInChildren<Stretch_Robot_Arm_Controller>();
            // var armTarget = SArm.transform.Find("stretch_robot_arm_rig").Find("stretch_robot_pos_rot_manipulator");
            // Vector3 pos = armTarget.transform.localPosition;
            // pos.z = 0.0f; // pulls the arm in to be fully contracted
            // armTarget.transform.localPosition = pos;


            // var StretchSolver = this.GetComponentInChildren<Stretch_Arm_Solver>();
            // Debug.Log("running manipulate stretch arm AB");
            // // StretchSolver.ManipulateStretchArm();

            //get references to floor collider and physica material to change when moving arm
            FloorCollider = this.gameObject.transform.Find("abFloorCollider").GetComponent<Collider>();
            FloorColliderPhysicsMaterial = FloorCollider.material;

            getArmImplementation().ContinuousUpdate(Time.fixedDeltaTime);
        }

        private ArticulatedArmController getArmImplementation() {
            ArticulatedArmController arm = GetComponentInChildren<ArticulatedArmController>();
            if (arm == null) {
                throw new InvalidOperationException(
                    "Agent does not havSe Stretch arm or is not enabled.\n" +
                    $"Make sure there is a '{typeof(ArticulatedArmController).Name}' component as a child of this agent."
                );
            }
            return arm;
        }

        protected override ArmController getArm() {
            return getArmImplementation();
        }

        public IEnumerator setFloorToHighFrictionAsLastStep(IEnumerator steps) {
            while (steps.MoveNext()) {
                yield return steps.Current;
            }
            SetFloorColliderToHighFriction();
        }

        public override IEnumerator MoveArmBaseUp(
            PhysicsSimulationParams physicsSimulationParams,

             float distance,
             float speed = 1,
             bool returnToStart = true
         ) {
            Debug.Log("MoveArmBaseUp from ArticulatedAgentController");
            SetFloorColliderToHighFriction();
            var arm = getArmImplementation();
            return arm.moveArmBaseUp(
                controller: this,
                distance: distance,
                unitsPerSecond: speed,
                fixedDeltaTime: physicsSimulationParams.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart,
                useLimits: false
            );
        }

        //with limits
        public IEnumerator MoveArmBaseUp(
            PhysicsSimulationParams physicsSimulationParams,

             float distance,
             bool useLimits,
             float speed = 1,
             bool returnToStart = true
         ) {
            Debug.Log("MoveArmBaseUp from ArticulatedAgentController");
            SetFloorColliderToHighFriction();
            var arm = getArmImplementation();
            return arm.moveArmBaseUp(
                controller: this,
                distance: distance,
                unitsPerSecond: speed,
                fixedDeltaTime: physicsSimulationParams.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart,
                useLimits: useLimits
            );
        }

        public override IEnumerator MoveArmBaseDown(
            PhysicsSimulationParams physicsSimulationParams,

            float distance,
            float speed = 1,
            bool returnToStart = true
        ) {
            Debug.Log("MoveArmBaseDown from ArticulatedAgentController (pass negative distance to MoveArmBaseUp)");
            return MoveArmBaseUp(
                physicsSimulationParams: physicsSimulationParams,
                distance: -distance,
                speed: speed,
                returnToStart: returnToStart,
                useLimits: false
            );
        }

        //with limits
        public IEnumerator MoveArmBaseDown(
            PhysicsSimulationParams physicsSimulationParams,

            float distance,
            bool useLimits,
            float speed = 1,
            bool returnToStart = true
        ) {
            Debug.Log("MoveArmBaseDown from ArticulatedAgentController (pass negative distance to MoveArmBaseUp)");
            return MoveArmBaseUp(
                physicsSimulationParams: physicsSimulationParams,
                distance: -distance,
                speed: speed,
                returnToStart: returnToStart,
                useLimits: useLimits
            );
        }

        public override IEnumerator MoveArm(
			PhysicsSimulationParams physicsSimulationParams,

            Vector3 position,
            float speed = 1,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false
        ) {
            var arm = getArmImplementation();
            SetFloorColliderToHighFriction();
            return arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: physicsSimulationParams.fixedDeltaTime
            );
        }

        //move arm overload with limits
        public IEnumerator MoveArm(
			PhysicsSimulationParams physicsSimulationParams,

            Vector3 position,
            bool useLimits,
            float speed = 1
        ) {
            var arm = getArmImplementation();
            SetFloorColliderToHighFriction();
            return arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: physicsSimulationParams.fixedDeltaTime,
                useLimits: useLimits
            );
        }

        //helper functions to set physics material values
        public void SetFloorColliderToSlippery() {
            FloorColliderPhysicsMaterial.staticFriction = 0;
            FloorColliderPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum; //ensure min friction take priority

            //FloorColliderPhysicsMaterial.dynamicFriction = 0;
        }

        public void SetFloorColliderToHighFriction() {
            FloorColliderPhysicsMaterial.staticFriction = 1;
            FloorColliderPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Maximum; //ensure max friction takes priority
            //FloorColliderPhysicsMaterial.dynamicFriction = 1;
        }

        public void TeleportFull(Vector3 position, Vector3 rotation, float? horizon = null, bool forceAction = false) {
            Debug.Log($"Original Position: {this.transform.position}");

            if (horizon == null) {
                horizon = m_Camera.transform.localEulerAngles.x;
            }

            if (!forceAction && (horizon > maxDownwardLookAngle || horizon < -maxUpwardLookAngle)) {
                throw new ArgumentOutOfRangeException(
                    $"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]. You gave {horizon}."
                );
            }

            float horizonf = (float)horizon;

            Quaternion realRotationAsQuaternionBecauseYes = Quaternion.Euler(rotation);

            //teleport must be finished in a coroutine because synctransforms DoESNt WoRK for ArTIcuLAtIONBodies soooooo
            StartCoroutine(TeleportThenWait(position, realRotationAsQuaternionBecauseYes, horizonf));
        }

        IEnumerator TeleportThenWait(Vector3 position, Quaternion rotation, float cameraHorizon) {
            Debug.Log("TeleportThenWait coroutine starting");
            ArticulationBody myBody = this.GetComponent<ArticulationBody>();
            myBody.TeleportRoot(position, rotation);
            m_Camera.transform.localEulerAngles = new Vector3(cameraHorizon, 0, 0);

            yield return new WaitForFixedUpdate();

            Debug.Log($"After TeleportRoot from TeleportThenWait: {this.transform.position}");
            actionFinished(true);
        }

        //we have to override this because the teleporting must be done within a coroutine
        public override void GetInteractablePoses(
            string objectId,
            Vector3[] positions = null,
            float[] rotations = null,
            float[] horizons = null,
            bool[] standings = null,
            float? maxDistance = null,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            getInteractablePosesAB(
                objectId: objectId,
                markActionFinished: true,
                positions: positions,
                rotations: rotations,
                horizons: horizons,
                standings: standings,
                maxDistance: maxDistance,
                maxPoses: maxPoses
            );
        }

        public void getInteractablePosesAB(
            string objectId,
            bool markActionFinished,
            Vector3[] positions = null,
            float[] rotations = null,
            float[] horizons = null,
            bool[] standings = null,
            float? maxDistance = null,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            Debug.Log("Calling getInteractablePosesAB");

            if (standings != null) {
                errorMessage = "Articulation Agent does not support 'standings' for GetInteractablePoses";
                throw new InvalidActionException();
            }

            Debug.Log($"Position of agent at start of getInteractablePosesAB: {this.transform.position}");
            if (360 % rotateStepDegrees != 0 && rotations != null) {
                throw new InvalidOperationException($"360 % rotateStepDegrees (360 % {rotateStepDegrees} != 0) must be 0, unless 'rotations: float[]' is overwritten.");
            }

            if (maxPoses <= 0) {
                throw new ArgumentOutOfRangeException("maxPoses must be > 0.");
            }

            // default "visibility" distance
            float maxDistanceFloat;
            if (maxDistance == null) {
                maxDistanceFloat = maxVisibleDistance;
            } else if ((float)maxDistance <= 0) {
                throw new ArgumentOutOfRangeException("maxDistance must be >= 0 meters from the object.");
            } else {
                maxDistanceFloat = (float)maxDistance;
            }

            // populate default horizons
            if (horizons == null) {
                horizons = new float[] { -30, 0, 30, 60 };
            } else {
                foreach (float horizon in horizons) {
                    // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
                    if (horizon > maxDownwardLookAngle || horizon < -maxUpwardLookAngle) {
                        throw new ArgumentException(
                            $"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]. You gave {horizon}."
                        );
                    }
                }
            }

            // populate the rotations based on rotateStepDegrees
            if (rotations == null) {
                // Consider the case where one does not want to move on a perfect grid, and is currently moving
                // with an offsetted set of rotations like {10, 100, 190, 280} instead of the default {0, 90, 180, 270}.
                // This may happen if the agent starts by teleports with the rotation of 10 degrees.
                int offset = (int)Math.Round(transform.eulerAngles.y % rotateStepDegrees);

                // Examples:
                // if rotateStepDegrees=10 and offset=70, then the paths would be [70, 80, ..., 400, 410, 420].
                // if rotateStepDegrees=90 and offset=10, then the paths would be [10, 100, 190, 280]
                rotations = new float[(int)Math.Round(360 / rotateStepDegrees)];
                int i = 0;
                for (float rotation = offset; rotation < 360 + offset; rotation += rotateStepDegrees) {
                    rotations[i++] = rotation;
                }
            }

            SimObjPhysics theObject = getInteractableSimObjectFromId(objectId: objectId, forceAction: true);

            // populate the positions by those that are reachable
            if (positions == null) {
                positions = getReachablePositions();
            }

            // Don't want to consider all positions in the scene, just those from which the object
            // is plausibly visible. The following computes a "fudgeFactor" (radius of the object)
            // which is then used to filter the set of all reachable positions to just those plausible positions.
            Bounds objectBounds = UtilityFunctions.CreateEmptyBounds();
            objectBounds.Encapsulate(theObject.transform.position);
            foreach (Transform vp in theObject.VisibilityPoints) {
                objectBounds.Encapsulate(vp.position);
            }
            float fudgeFactor = objectBounds.extents.magnitude;
            List<Vector3> filteredPositions = positions.Where(
                p => (Vector3.Distance(a: p, b: theObject.transform.position) <= maxDistanceFloat + fudgeFactor + gridSize)
            ).ToList();

            // save current agent pose
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            Vector3 oldHorizon = m_Camera.transform.localEulerAngles;

            //now put all this into a coroutine to actually teleport and do the visibility checks on a delay
            //since TeleportRoot needs to sync with a waitForFixedUpdate
            StartCoroutine(TeleportThenWaitThenCheckInteractable(
                filteredPositions: filteredPositions,
                horizons: horizons,
                rotations: rotations,
                theObject: theObject,
                maxDistanceFloat: maxDistanceFloat,
                maxPoses: maxPoses,
                oldPosition: oldPosition,
                oldHorizon: oldHorizon,
                oldRotation: oldRotation
            ));
        }

        IEnumerator TeleportThenWaitThenCheckInteractable(
            List<Vector3> filteredPositions,
            float[] horizons,
            float[] rotations,
            SimObjPhysics theObject,
            float maxDistanceFloat,
            int maxPoses,
            Vector3 oldPosition,
            Vector3 oldHorizon,
            Quaternion oldRotation) {

            // set each key to store a list
            List<Dictionary<string, object>> validAgentPoses = new List<Dictionary<string, object>>();

            bool stopEarly = false;
            foreach (float horizon in horizons) {
                m_Camera.transform.localEulerAngles = new Vector3(horizon, 0f, 0f);
                Debug.Log($"camera horizon set to: {m_Camera.transform.localEulerAngles.x}");

                foreach (float rotation in rotations) {
                    Vector3 rotationVector = new Vector3(x: 0, y: rotation, z: 0);
                    //SetTransform(transform: transform, rotation: (Quaternion?)Quaternion.Euler(rotationVector));
                    //yield return new WaitForFixedUpdate();

                    foreach (Vector3 position in filteredPositions) {
                        Debug.Log("////////////////////");
                        Debug.Log($"position Before SetTransform: {transform.position}");
                        Debug.Log($"Passing in position to SetTransform: Vector3 {position}, Vector3? {(Vector3?)position}");
                        SetTransform(transform: transform, position: (Vector3?)position, rotation: (Quaternion?)Quaternion.Euler(rotationVector));
                        yield return new WaitForFixedUpdate();
                        Debug.Log($"Position After SetTransform(): {transform.position}");
                        Debug.Log("////////////////////");

                        // Each of these values is directly compatible with TeleportFull
                        // and should be used with .step(action='TeleportFull', **interactable_positions[0])
                        if (objectIsCurrentlyVisible(theObject, maxDistanceFloat)) {
                            validAgentPoses.Add(new Dictionary<string, object> {
                                ["x"] = position.x,
                                ["y"] = position.y,
                                ["z"] = position.z,
                                ["rotation"] = rotation,
                                ["horizon"] = horizon
                            });

                            if (validAgentPoses.Count >= maxPoses) {
                                stopEarly = true;
                                break;
                            }

#if UNITY_EDITOR
                            // In the editor, draw lines indicating from where the object was visible.
                            Debug.DrawLine(position, position + transform.forward * (gridSize * 0.5f), Color.red, 20f);
#endif
                        }
                    }
                    if (stopEarly) { break; }
                }
                if (stopEarly) { break; }
            }

            //reset to original position/rotation/horizon now that we are done
            SetTransform(transform: transform, position: (Vector3?) oldPosition, rotation: (Quaternion?) oldRotation);
            m_Camera.transform.localEulerAngles = oldHorizon;

#if UNITY_EDITOR
            Debug.Log(validAgentPoses.Count);
            Debug.Log(validAgentPoses);
#endif

            actionFinishedEmit(success: true, actionReturn: validAgentPoses);
            // TODO: change to and test
            // return new ActionFinished() {
            //     success = true,
            //     actionReturn: validAgentPoses,
            //     toEmitState: true
            // }
        }

        public IEnumerator MoveAgent(
			PhysicsSimulationParams physicsSimulationParams,

            float moveMagnitude = 1,
            float speed = 1,
            float acceleration = 1
        ) {
            // Debug.Log("(3) ArticulatedAgentController: PREPPING MOVEAGENT COMMAND");
            int direction = 0;
            if (moveMagnitude < 0) {
                direction = -1;
            }
            if (moveMagnitude > 0) {
                direction = 1;
            }

            Debug.Log("Move magnitude is now officially " + moveMagnitude);
            // Debug.Log($"preparing agent {this.transform.name} to move");
            if (Mathf.Approximately(moveMagnitude, 0.0f)) {
                Debug.Log("Error! distance to move must be nonzero");
                yield return new ActionFinished() {
                    success = false,
                    errorMessage = "Error! distance to move must be nonzero"
                };
            }

            float fixedDeltaTimeFloat = physicsSimulationParams.fixedDeltaTime;

            AgentMoveParams amp = new AgentMoveParams {
                agentState = ABAgentState.Moving,
                distance = Mathf.Abs(moveMagnitude),
                speed = speed,
                acceleration = acceleration,
                agentMass = CalculateTotalMass(this.transform),
                minMovementPerSecond = 0.001f,
                maxTimePassed = 10.0f,
                haltCheckTimeWindow = 0.2f,
                direction = direction,
                maxForce = 200f
            };

            this.GetComponent<ArticulatedAgentSolver>().PrepToControlAgentFromAction(amp);

            // now that move call happens
            SetFloorColliderToSlippery();
            yield return setFloorToHighFrictionAsLastStep(
                ContinuousMovement.moveAB(
                    movable: this.getBodyMovable(),
                    controller: this,
                    fixedDeltaTime: fixedDeltaTimeFloat
                )
            );
        }


        public IEnumerator MoveAhead(
			PhysicsSimulationParams physicsSimulationParams,

            float? moveMagnitude = null,
            float speed = 0.14f,
            float acceleration = 0.14f
        ) {
            return MoveAgent(
                physicsSimulationParams: physicsSimulationParams,
                moveMagnitude: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                acceleration: acceleration
            );
        }

        public IEnumerator MoveBack(
			PhysicsSimulationParams physicsSimulationParams,

            float? moveMagnitude = null,
            float speed = 0.14f,
            float acceleration = 0.14f
        ) {
            return MoveAgent(
                physicsSimulationParams: physicsSimulationParams,
                moveMagnitude: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                acceleration: acceleration
            );
        }

        public IEnumerator RotateRight(
			PhysicsSimulationParams physicsSimulationParams,

            float? degrees = null,
            float speed = 22.5f,
            float acceleration = 22.5f
        ) {
            return RotateAgent(
                physicsSimulationParams: physicsSimulationParams,

                degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                acceleration: acceleration
            );
        }

        public IEnumerator RotateLeft(
			PhysicsSimulationParams physicsSimulationParams,

            float? degrees = null,
            float speed = 22.5f,
            float acceleration = 22.5f
        ) {
            return RotateAgent(
                physicsSimulationParams: physicsSimulationParams,

                degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                acceleration: acceleration
            );
        }

        public IEnumerator RotateAgent(
			PhysicsSimulationParams physicsSimulationParams,

            float degrees,
            float speed = 22.5f,
            float acceleration = 22.5f
        ) {
            int direction = 0;
            if (degrees < 0) {
                direction = -1;
            }
            if (degrees > 0) {
                direction = 1;
            }

            Debug.Log($"preparing agent {this.transform.name} to rotate");
            if (Mathf.Approximately(degrees, 0.0f)) {
                Debug.Log("Error! distance to rotate must be nonzero");
                yield return new ActionFinished() {
                    success = false,
                    errorMessage = "Error! distance to rotate must be nonzero"
                };
            }

            AgentMoveParams amp = new AgentMoveParams {
                agentState = ABAgentState.Rotating,
                distance = Mathf.Abs(Mathf.Deg2Rad * degrees),
                speed = Mathf.Deg2Rad * speed,
                acceleration = Mathf.Deg2Rad * acceleration,
                agentMass = CalculateTotalMass(this.transform),
                minMovementPerSecond = 1f * Mathf.Deg2Rad,
                maxTimePassed = 10.0f,
                haltCheckTimeWindow = 0.2f,
                direction = direction,
                maxForce = 200f
            };

            this.GetComponent<ArticulatedAgentSolver>().PrepToControlAgentFromAction(amp);

            // now that rotate call happens
            SetFloorColliderToSlippery();
            yield return setFloorToHighFrictionAsLastStep(
                ContinuousMovement.moveAB(
                    movable: this.getBodyMovable(),
                    controller: this,
                    fixedDeltaTime: physicsSimulationParams.fixedDeltaTime,
                    unitsPerSecond: speed,
                    acceleration: acceleration
                )
            );
        }

        // not doing these for benchmark yet cause no
        public ActionFinished TeleportArm(
            Vector3? position = null,
            float? rotation = null,
            bool worldRelative = false,
            bool forceAction = false
        ) {
            GameObject posRotManip = this.GetComponent<BaseAgentComponent>().StretchArm.GetComponent<Stretch_Robot_Arm_Controller>().GetArmTarget();

            // cache old values in case there's a failure
            Vector3 oldLocalPosition = posRotManip.transform.localPosition;
            float oldLocalRotationAngle = posRotManip.transform.localEulerAngles.y;

            // establish defaults in the absence of inputs
            if (position == null) {
                position = new Vector3(0f, 0.1f, 0f);
            }

            if (rotation == null) {
                rotation = -180f;
            }

            // teleport arm!
            if (!worldRelative) {
                posRotManip.transform.localPosition = (Vector3)position;
                posRotManip.transform.localEulerAngles = new Vector3(0, (float)rotation % 360, 0);
            } else {
                posRotManip.transform.position = (Vector3)position;
                posRotManip.transform.eulerAngles = new Vector3(0, (float)rotation % 360, 0);
            }

            if (SArm.IsArmColliding() && !forceAction) {
                posRotManip.transform.localPosition = oldLocalPosition;
                posRotManip.transform.localEulerAngles = new Vector3(0, oldLocalRotationAngle, 0);
                return new ActionFinished() {
                    success = false,
                    errorMessage = "collision detected at desired transform, cannot teleport"
                };
            } else {
                return ActionFinished.Success;
            }
        }

        public override IEnumerator RotateWristRelative(
			PhysicsSimulationParams physicsSimulationParams,

            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 400f,
            bool returnToStart = true
        ) {
            // pitch and roll are not supported for the stretch and so we throw an error
            if (pitch != 0f || roll != 0f) {
                throw new System.NotImplementedException("Pitch and roll are not supported for the stretch agent.");
            }
            Debug.Log($"executing RotateWristRelative from ArticulatedAgentController with speed {speed}");
            var arm = getArmImplementation();
            SetFloorColliderToHighFriction();
            return arm.rotateWrist(
                controller: this,
                distance: yaw,
                degreesPerSecond: speed,
                fixedDeltaTime: physicsSimulationParams.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart
            );
        }

        private float CalculateTotalMass(Transform rootTransform) {
            float totalMass = 0f;
            ArticulationBody rootBody = rootTransform.GetComponent<ArticulationBody>();
            if (rootBody != null) {
                totalMass += rootBody.mass;
            }

            foreach (Transform childTransform in rootTransform) {
                totalMass += CalculateTotalMass(childTransform);
            }

            return totalMass;
        }

        private MovableContinuous getBodyMovable() {
            return this.transform.GetComponent<ArticulatedAgentSolver>();
        }
    }
}
