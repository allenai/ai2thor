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
        
    public partial class StretchAgentController : ArmAgentController {
        public int gripperOpennessState = 0;

        //define default parameters for both main camera and secondary camera, specific to real-life stretch bot rig
        //these are kind of magic numbers, but can be adjusted via UpdateMainCamera and UpdateThirdPartyCamera as needed if our
        //real rig changes
        private Vector3 defaultMainCameraLocalPosition = new Vector3(0.001920350f, 0.544700900f, 0.067880400f);
        private Vector3 defaultMainCameraLocalRotation = new Vector3(30f, 0, 0);
        private float defaultMainCameraFieldOfView = 59f;
        private Vector3 defaultSecondaryCameraLocalPosition = new Vector3(0.053905130f, 0.523833600f, -0.058848570f);
        private Vector3 defaultSecondaryCameraLocalRotation =new Vector3(50f, 90f, 0);
        private float defaultSecondaryCameraFieldOfView = 59f;

        public StretchAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public override void updateImageSynthesis(bool status) {
            base.updateImageSynthesis(status);

            // updateImageSynthesis is run in BaseFPSController's Initialize method after the
            // Stretch Agent's unique secondary camera has been added to the list of third party
            // cameras in InitializeBody, so a third-party camera image synthesis update is
            // necessary if we want the secondary camera's image synthesis componenent to match
            // the primary camera's
            agentManager.updateThirdPartyCameraImageSynthesis(status);
        }

        public override void InitializeBody(ServerAction initializeAction) {
            VisibilityCapsule = StretchVisCap;
            m_CharacterController.center = new Vector3(0, -0.1821353f, -0.1092373f);
            m_CharacterController.radius = 0.1854628f;
            m_CharacterController.height = 1.435714f;

            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;
            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            // set camera stand/crouch local positions for Tall mode
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition;

            // set up main camera parameters
            m_Camera.fieldOfView = 65f;

            var secondaryCameraName = "SecondaryCamera";

            // activate arm-camera
            Camera fp_camera_2 = m_CharacterController.transform.Find(secondaryCameraName).GetComponent<Camera>();
            fp_camera_2.gameObject.SetActive(true);
            agentManager.registerAsThirdPartyCamera(fp_camera_2);
            if (initializeAction.antiAliasing != null) {
                agentManager.updateAntiAliasing(
                    postProcessLayer: fp_camera_2.gameObject.GetComponentInChildren<PostProcessLayer>(),
                    antiAliasing: initializeAction.antiAliasing
                );
            }

            // set up primary camera parameters for stretch specific parameters
            m_Camera.transform.localPosition = defaultMainCameraLocalPosition;
            m_Camera.transform.localEulerAngles = defaultMainCameraLocalRotation;
            m_Camera.fieldOfView = defaultMainCameraFieldOfView;

            // set up secondary camera paremeters for stretch bot
            fp_camera_2.transform.localPosition = defaultSecondaryCameraLocalPosition;
            fp_camera_2.transform.localEulerAngles = defaultSecondaryCameraLocalRotation;
            fp_camera_2.fieldOfView = defaultSecondaryCameraFieldOfView;

            // limit camera from looking too far down/up
            if (Mathf.Approximately(initializeAction.maxUpwardLookAngle, 0.0f)) {
                this.maxUpwardLookAngle = 25f;
            } else {
                this.maxUpwardLookAngle = initializeAction.maxUpwardLookAngle;
            }

            if (Mathf.Approximately(initializeAction.maxDownwardLookAngle, 0.0f)) {
                this.maxDownwardLookAngle = 90f;
            } else {
                this.maxDownwardLookAngle = initializeAction.maxDownwardLookAngle;
            }

            var secondaryCameraParams = new CameraParameters();
            var setSecondaryParams = initializeAction.thirdPartyCameraParameters?.TryGetValue(secondaryCameraName, out secondaryCameraParams);

            if (setSecondaryParams.GetValueOrDefault()) {
                CameraParameters.setCameraParameters(fp_camera_2, secondaryCameraParams);
            }

            // enable stretch arm component
            Debug.Log("initializing stretch arm");
            StretchArm.SetActive(true);
            //initialize all things needed for the stretch arm controller
            SArm = this.GetComponentInChildren<Stretch_Robot_Arm_Controller>();
            SArm.PhysicsController = this;
            var armTarget = SArm.transform.Find("stretch_robot_arm_rig").Find("stretch_robot_pos_rot_manipulator");
            Vector3 pos = armTarget.transform.localPosition;
            pos.z = 0.0f; // pulls the arm in to be fully contracted
            armTarget.transform.localPosition = pos;
            var StretchSolver = this.GetComponentInChildren<Stretch_Arm_Solver>();
            Debug.Log("running manipulate stretch arm");
            StretchSolver.ManipulateStretchArm();
        }

        private ArmController getArmImplementation() {
            Stretch_Robot_Arm_Controller arm = GetComponentInChildren<Stretch_Robot_Arm_Controller>();
            if (arm == null) {
                throw new InvalidOperationException(
                    "Agent does not have Stretch arm or is not enabled.\n" +
                    $"Make sure there is a '{typeof(Stretch_Robot_Arm_Controller).Name}' component as a child of this agent."
                );
            }
            return arm;
        }

        protected override ArmController getArm() { 
            return getArmImplementation();
        }


        public bool teleportArm(
            Vector3? position = null,
            float? rotation = null,
            bool worldRelative = false,
            bool forceAction = false
        ) {
            Stretch_Robot_Arm_Controller arm = getArmImplementation() as Stretch_Robot_Arm_Controller;
            GameObject posRotManip = arm.GetArmTarget();

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
                posRotManip.transform.localEulerAngles = new Vector3 (0, (float)rotation % 360, 0);
            } else {
                posRotManip.transform.position = (Vector3)position;
                posRotManip.transform.eulerAngles = new Vector3 (0, (float)rotation % 360, 0);
            }

            bool success = false;
            arm.ContinuousUpdate(0f);
            if ((!forceAction) && SArm.IsArmColliding()) {
                errorMessage = "collision detected at desired transform, cannot teleport";
                posRotManip.transform.localPosition = oldLocalPosition;
                posRotManip.transform.localEulerAngles = new Vector3(0, oldLocalRotationAngle, 0);
                arm.ContinuousUpdate(0f);
            } else {
                success = true;
            }
            arm.resetPosRotManipulator();
            return success;
        }

        public void TeleportArm(
            Vector3? position = null,
            float? rotation = null,
            bool worldRelative = false,
            bool forceAction = false
        ) {
            actionFinished(
                teleportArm(
                    position: position,
                    rotation: rotation,
                    worldRelative: worldRelative,
                    forceAction: forceAction
                )
            );
        }

        /// <summary>
        /// Attempts to reach a specified object in the scene by teleporting the agent and its arm towards the object.
        /// </summary>
        /// <param name="objectId">The ID of the object to reach.</param>
        /// <param name="position">Optional. The target position for teleporting the agent. If null, the agent's current position is used.</param>
        /// <param name="returnToInitialPosition">If true, the agent and its arm return to their initial positions after the attempt, regardless of success.</param>
        /// <returns>
        /// An <see cref="ActionFinished"/> object containing the outcome of the attempt. This includes whether the action was successful, and, if so, details about the final position and orientation of the agent and its arm, and the point on the object that was reached. If <paramref name="returnToInitialPosition"/> is true, the state is emitted after returning.
        /// </returns>
        /// <remarks>
        /// This method first checks if the specified object exists within the scene. If not, it returns an unsuccessful result. If the object exists, the agent and its arm attempt to teleport to a position close to the object, adjusting their orientation to face the object and reach it with the arm's gripper.
        /// The method calculates the closest points on the object to the agent and iteratively attempts to position the arm such that it can reach the object without causing any collisions. If a successful position is found, the agent's arm is teleported to that position, and the method returns success. If the attempt is unsuccessful or if the arm cannot reach the object without colliding, the action is marked as failed.
        /// Optionally, the agent and its arm can return to their initial positions after the attempt. This is useful for scenarios where the agent's position before the attempt is critical to maintain regardless of the outcome.
        /// Finally: grip positions ALWAYS assume that the gripper has been openned to its maximum size (50 degrees) so if you
        /// are trying to reach an object with a closed gripper, you will need to open the gripper first, then reach the object.
        /// </remarks>
        public ActionFinished TryReachObject(
            string objectId,
            Vector3? position = null,
            bool returnToInitialPosition = false
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                return new ActionFinished() {
                    success = false,
                    toEmitState = true
                };
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            Vector3 oldPos = transform.position;
            Quaternion oldRot = transform.rotation;

            Stretch_Robot_Arm_Controller arm = getArmImplementation() as Stretch_Robot_Arm_Controller;
            GameObject armTarget = arm.GetArmTarget();

            Vector3 oldArmLocalPosition = armTarget.transform.localPosition;
            Vector3 oldArmLocalEulerAngles = armTarget.transform.localEulerAngles;
            int oldGripperOpenState = gripperOpennessState;

            if (position.HasValue) {
                transform.position = position.Value;
                Physics.SyncTransforms();
            }

            // Find points on object closest to agent
            List<Vector3> closePoints = pointOnObjectsCollidersClosestToPoint(
                objectId: objectId,
                point: new Vector3(
                    transform.position.x,
                    target.AxisAlignedBoundingBox.center.y + target.AxisAlignedBoundingBox.size.y / 2f, // Y from the object
                    transform.position.z
                )
            );

            teleportArm(
                position: armTarget.transform.localPosition,
                rotation: 0f,
                worldRelative: false,
                forceAction: true
            );
            SetGripperOpenness(openness: 50f);
            Physics.SyncTransforms();

            bool success = false;
            Vector3 pointOnObject = Vector3.zero;

            foreach (Vector3 point in closePoints) {
#if UNITY_EDITOR
                Debug.DrawLine(
                    point,
                    point + transform.up * 0.3f,
                    Color.red,
                    20f
                );
#endif
                transform.LookAt(
                    worldPosition: new Vector3(
                        point.x, transform.position.y, point.z
                    ),
                    worldUp: transform.up
                );

                Physics.SyncTransforms();

                Vector3 agentPos = transform.position;
                Vector3 magnetSpherePos = arm.MagnetSphereWorldCenter();

                Vector3 agentToPoint = point - agentPos;
                Vector3 agentToMagnetSphere = magnetSpherePos - agentPos;
                float angle = Vector3.Angle(
                    from: new Vector3(agentToPoint.x, 0f, agentToPoint.z),
                    to: new Vector3(agentToMagnetSphere.x, 0f, agentToMagnetSphere.z)
                );

                // Rotate transform by angle around y
                transform.localEulerAngles = new Vector3(
                    transform.localEulerAngles.x,
                    transform.localEulerAngles.y - angle,
                    transform.localEulerAngles.z
                );
                Physics.SyncTransforms();

                agentToMagnetSphere = arm.MagnetSphereWorldCenter() - transform.position;
                Vector3 agentToMagnetSphereDir2D = new Vector3(agentToMagnetSphere.x, 0f, agentToMagnetSphere.z).normalized;

                if (
                    !teleportArm(
                        position: (
                            point
                            + (armTarget.transform.position - arm.MagnetSphereWorldCenter())
                            - 0.25f * arm.magnetSphere.radius * agentToMagnetSphereDir2D
                        ),
                        rotation: armTarget.transform.eulerAngles.y,
                        worldRelative: true
                    )
                ) {
# if UNITY_EDITOR
                    Debug.Log("Agent arm is colliding after teleporting arm");
# endif
                    continue;
                }

                Physics.SyncTransforms();
                if (isAgentCapsuleColliding(null)) {
# if UNITY_EDITOR
                    Debug.Log("Agent capsule is colliding after teleporting arm");
# endif
                    continue;
                }

                bool touchingObject = false;
                foreach (SimObjPhysics sop in arm.WhatObjectsAreInsideMagnetSphereAsSOP(false)) {
                    if (sop.ObjectID == objectId) {
                        touchingObject = true;
                        break;
                    }
                }

                if (!touchingObject) {
# if UNITY_EDITOR
                    Debug.Log("Agent is not touching object after teleporting arm");
# endif
                    continue;
                }


# if UNITY_EDITOR
                Debug.Log($"Found successful position {point}.");
# endif
                success = true;
                pointOnObject = point;
                break;
            }

            Dictionary<string, Vector3> actionReturn = null;
            if (success) {
                actionReturn = new Dictionary<string, Vector3>() {
                    {"position", transform.position},
                    {"rotation", transform.rotation.eulerAngles},
                    {"localArmPosition", armTarget.transform.localPosition},
                    {"localArmRotation", armTarget.transform.localEulerAngles},
                    {"pointOnObject", pointOnObject}
                };
            }

            if (returnToInitialPosition) {
                teleportArm(
                    position: oldArmLocalPosition,
                    rotation: oldArmLocalEulerAngles.y,
                    worldRelative: false,
                    forceAction: true
                );
                transform.position = oldPos;
                transform.rotation = oldRot;
                SetGripperOpenness(openness: null, openState: oldGripperOpenState);
                Physics.SyncTransforms();
            }

            return new ActionFinished() {
                success = success,
                actionReturn = actionReturn,
                toEmitState = returnToInitialPosition
            };
        }

        /// <summary>
        /// Retrieves a list of positions from which an agent can successfully touch a specified object within a given distance.
        /// </summary>
        /// <param name="objectId">The ID of the object the agent attempts to touch.</param>
        /// <param name="positions">Optional. An array of Vector3 positions to test for the ability to touch the object. If null, the method computes reachable positions based on the agent's current environment.</param>
        /// <param name="maxDistance">The maximum distance from the object at which a position is considered for a successful touch. Default is 1 meter.</param>
        /// <param name="maxPoses">The maximum number of touching poses to return. Acts as a cap to limit the result size. Default is <see cref="int.MaxValue"/>, which effectively means no limit.</param>
        /// <returns>
        /// An <see cref="ActionFinished"/> object containing the outcome of the retrieval. This includes a success flag and, if successful, a list of dictionaries detailing the successful positions and related data for touching the object. Each dictionary in the list provides details such as the agent's position, rotation, local arm position, local arm rotation, and the point on the object that was successfully touched.
        /// </returns>
        /// <remarks>
        /// This method first validates the existence of the specified object and the positivity of <paramref name="maxPoses"/>. It computes a set of candidate positions based on the provided positions or, if none are provided, based on reachable positions within the environment. It then filters these positions to include only those within a certain distance threshold from the object, adjusted for the object's bounding box size.
        ///
        /// For each candidate position, the method attempts to touch the object by invoking <see cref="TryReachObject"/> with the option to return the agent to its initial position after each attempt. This ensures that the agent's initial state is preserved between attempts. The method accumulates successful attempts up to the specified <paramref name="maxPoses"/> limit.
        ///
        /// The method is particularly useful in simulations where determining potential interaction points with objects is necessary for planning or executing tasks that involve direct physical manipulation or contact with objects in the environment.
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="maxPoses"/> is less than or equal to zero.</exception>
        public ActionFinished GetTouchingPoses(
            string objectId,
            Vector3[] positions = null,
            float maxDistance = 1f,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                return new ActionFinished() {
                    success = false,
                    toEmitState = true
                };
            }
            if (maxPoses <= 0) {
                throw new ArgumentOutOfRangeException("maxPoses must be > 0.");
            }

            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            Vector3 bboxSize = sop.AxisAlignedBoundingBox.size;
            float fudgeFactor = Mathf.Sqrt(bboxSize.x * bboxSize.x + bboxSize.z * bboxSize.z) / 2f;

            if (positions == null) {
                positions = getReachablePositions();
            }

            List<Vector3> filteredPositions = positions.Where(
                p => (Vector3.Distance(a: p, b: sop.transform.position) <= maxDistance + fudgeFactor + gridSize)
            ).ToList();

            List<object> poses = new List<object>();
            foreach (Vector3 position in filteredPositions) {
                ActionFinished af = TryReachObject(
                    objectId: objectId,
                    position: position,
                    returnToInitialPosition: true
                );

                if (af.success) {
                    poses.Add(af.actionReturn);
# if UNITY_EDITOR
                    Debug.DrawLine(
                        start: position,
                        end: ((Dictionary<string, Vector3>) af.actionReturn)["pointOnObject"],
                        color: Color.green,
                        duration: 15f
                    );
# endif
                }

                if (poses.Count >= maxPoses) {
                    break;
                }
            }

# if UNITY_EDITOR
            Debug.Log($"Found {poses.Count} touching poses");
# endif

            return new ActionFinished() { success = true, actionReturn = poses };
        }


        public override IEnumerator RotateWristRelative(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            bool returnToStart = true
        ) {
            // pitch and roll are not supported for the stretch and so we throw an error
            if (pitch != 0f || roll != 0f) {
                throw new System.NotImplementedException("Pitch and roll are not supported for the stretch agent.");
            }

            var arm = getArmImplementation() as Stretch_Robot_Arm_Controller;

            yaw %= 360;

            return arm.rotateWrist(
                controller: this,
                rotation: yaw,
                degreesPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart,
                isRelativeRotation: true
            );
        }

        public IEnumerator RotateWrist(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            bool returnToStart = true
        ) {
            // pitch and roll are not supported for the stretch and so we throw an error
            if (pitch != 0f || roll != 0f) {
                throw new System.NotImplementedException("Pitch and roll are not supported for the stretch agent.");
            }

            // GameObject posRotManip = this.GetComponent<BaseAgentComponent>().StretchArm.GetComponent<Stretch_Robot_Arm_Controller>().GetArmTarget();

            var arm = getArmImplementation() as Stretch_Robot_Arm_Controller;
            float startingRotation = arm.GetArmTarget().transform.localEulerAngles.y;

            // Normalize target yaw to be bounded by [0, 360) (startingRotation is defaults to this)
            yaw %= 360;
            if (yaw < 0) {
                yaw += 360;
            }

            // Find shortest relativeRotation to feed into rotateWrist
            yaw -= startingRotation;

            if (Mathf.Abs(yaw) > 180) {
                yaw = (Mathf.Abs(yaw) - 360) * Mathf.Sign(yaw);
            }

            return arm.rotateWrist(
                controller: this,
                rotation: yaw,
                degreesPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart,
                isRelativeRotation: false
            );
        }

        protected int gripperOpenFloatToState(float openness) {
            if (-100 <= openness && openness < 0) {
                return 0;
            } else if (0 <= openness && openness < 5) {
                return 1;
            } else if (5 <= openness && openness < 15) {
                return 2;
            } else if (15 <= openness && openness < 25) {
                return 3;
            } else if (25 <= openness && openness < 35) {
                return 4;
            } else if (35 <= openness && openness < 45) {
                return 5;
            } else if (45 <= openness && openness <= 50) {
                return 6;
            } else {
                throw new InvalidOperationException(
                    $"Invalid value for `openness`: '{openness}'. Value should be between -100 and 50"
                );
            }
        }
        public ActionFinished SetGripperOpenness(float? openness, int? openState = null) {
            if (openness.HasValue == openState.HasValue) {
                throw new InvalidOperationException(
                    $"Only one of openness or openState should have a value"
                );
            }
            if (openness.HasValue) {
                openState = gripperOpenFloatToState(openness.Value);
            }

            foreach (GameObject opennessState in GripperOpennessStates) {
                opennessState.SetActive(false);
            }
            
            GripperOpennessStates[openState.Value].SetActive(true);
            gripperOpennessState = openState.Value;
            return ActionFinished.Success;
        }

//        public void RotateCameraBase(float yawDegrees, float rollDegrees) {
//            var target = gimbalBase;
//            var maxDegree = maxBaseXYRotation;
//            Debug.Log("yaw is " + yawDegrees + " and roll is " + rollDegrees);
//            if (yawDegrees < -maxDegree || maxDegree < yawDegrees) {
//                throw new InvalidOperationException(
//                    $"Invalid value for `yawDegrees`: '{yawDegrees}'. Value should be between '{-maxDegree}' and '{maxDegree}'."
//                );
//            } else if (rollDegrees < -maxDegree || maxDegree < rollDegrees) {
//                throw new InvalidOperationException(
//                    $"Invalid value for `rollDegrees`: '{rollDegrees}'. Value should be between '{-maxDegree}' and '{maxDegree}'."
//                );
//            } else {
//                gimbalBase.localEulerAngles = new Vector3(
//                    gimbalBaseStartingXRotation + rollDegrees,
//                    gimbalBaseStartingYRotation + yawDegrees,
//                    gimbalBase.transform.localEulerAngles.z
//                );
//            }
//            actionFinished(true);
//        }

        public void RotateCameraMount(float degrees, bool secondary = false) {
            var minDegree = -80.00001f;
            var maxDegree = 80.00001f;
            if (degrees >= minDegree && degrees <= maxDegree) {

                Camera cam;
                if (secondary) {
                    cam = agentManager.thirdPartyCameras[0];
                } else {
                    cam = m_Camera;
                }
                AgentManager.OptionalVector3 localEulerAngles = new AgentManager.OptionalVector3(
                    x: degrees, y: cam.transform.localEulerAngles.y, z: cam.transform.localEulerAngles.z
                );

                int agentId = -1;
                for (int i = 0; i < agentManager.agents.Count; i++) {
                    if (agentManager.agents[i] == this) {
                        agentId = i;
                        break;
                    }
                }
                if (agentId != 0) {
                    errorMessage = "Only the primary agent can rotate the camera for now.";
                    actionFinished(false);
                    return;
                }

                if (secondary) {
                    agentManager.UpdateThirdPartyCamera(
                        thirdPartyCameraId: 0,
                        rotation: localEulerAngles,
                        agentPositionRelativeCoordinates: true,
                        agentId: agentId
                    );
                } else {
                    agentManager.UpdateMainCamera(rotation: localEulerAngles);
                }
            }
            else {
                errorMessage = $"Invalid value for `degrees`: '{degrees}'. Value should be between '{minDegree}' and '{maxDegree}'.";
                actionFinished(false);
            }
        }

    }

}
