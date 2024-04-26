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

        protected bool applyActionNoise = true;
        protected float movementGaussianMu = 0.001f;
        protected float movementGaussianSigma = 0.005f;
        protected float rotateGaussianMu = 0.0f;
        protected float rotateGaussianSigma = 0.5f;
        protected bool allowHorizontalMovement = false;

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

        public override ActionFinished InitializeBody(ServerAction initializeAction) {
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

            // var secondaryCameraName = "SecondaryCamera";

            // // activate arm-camera
            // Camera fp_camera_2 = m_CharacterController.transform.Find(secondaryCameraName).GetComponent<Camera>();
            // fp_camera_2.gameObject.SetActive(true);
            // agentManager.registerAsThirdPartyCamera(fp_camera_2);
            // if (initializeAction.antiAliasing != null) {
            //     agentManager.updateAntiAliasing(
            //         postProcessLayer: fp_camera_2.gameObject.GetComponentInChildren<PostProcessLayer>(),
            //         antiAliasing: initializeAction.antiAliasing
            //     );
            // }

            // set up primary camera parameters for stretch specific parameters
            m_Camera.transform.localPosition = defaultMainCameraLocalPosition;
            m_Camera.transform.localEulerAngles = defaultMainCameraLocalRotation;
            m_Camera.fieldOfView = defaultMainCameraFieldOfView;

            // // set up secondary camera paremeters for stretch bot
            // fp_camera_2.transform.localPosition = defaultSecondaryCameraLocalPosition;
            // fp_camera_2.transform.localEulerAngles = defaultSecondaryCameraLocalRotation;
            // fp_camera_2.fieldOfView = defaultSecondaryCameraFieldOfView;

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

            // var secondaryCameraParams = new CameraParameters();
            // var setSecondaryParams = initializeAction.thirdPartyCameraParameters?.TryGetValue(secondaryCameraName, out secondaryCameraParams);

            // if (setSecondaryParams.GetValueOrDefault()) {
            //     CameraParameters.setCameraParameters(fp_camera_2, secondaryCameraParams);
            // }

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
            return ActionFinished.Success;
        }

        public void SetUpSecondaryCamera(ServerAction initializeAction){
            if (agentManager.thirdPartyCameras.Count == 0) {
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
            }
            actionFinished(true);
        }

        public void DisableSecondaryCamera(){
            if (agentManager.thirdPartyCameras.Count > 0) {
                var secondaryCameraName = "SecondaryCamera";
                Camera fp_camera_2 = m_CharacterController.transform.Find(secondaryCameraName).GetComponent<Camera>();
                fp_camera_2.gameObject.SetActive(false);
                agentManager.thirdPartyCameras.Remove(fp_camera_2);
            }
            actionFinished(true);
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

        
        public void TeleportArm(
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
                posRotManip.transform.localEulerAngles = new Vector3 (0, (float)rotation % 360, 0);
            } else {
                posRotManip.transform.position = (Vector3)position;
                posRotManip.transform.eulerAngles = new Vector3 (0, (float)rotation % 360, 0);
            }

            if (SArm.IsArmColliding() && !forceAction) {
                errorMessage = "collision detected at desired transform, cannot teleport";
                posRotManip.transform.localPosition = oldLocalPosition;
                posRotManip.transform.localEulerAngles = new Vector3(0, oldLocalRotationAngle, 0);
                actionFinished(false);
            } else {
                actionFinished(true);
            }
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
                returnToStartPositionIfFailed: returnToStart,
                isRelativeRotation: false
            );
        }

        public void SetGripperOpenness(float openness) {
            foreach (GameObject opennessState in GripperOpennessStates) {
                opennessState.SetActive(false);
            }
            if (-100 <= openness && openness < 0) {
                GripperOpennessStates[0].SetActive(true);
                gripperOpennessState = 0;
            } else if (0 <= openness && openness < 5) {
                GripperOpennessStates[1].SetActive(true);
                gripperOpennessState = 1;
            } else if (5 <= openness && openness < 15) {
                GripperOpennessStates[2].SetActive(true);
                gripperOpennessState = 2;
            } else if (15 <= openness && openness < 25) {
                GripperOpennessStates[3].SetActive(true);
                gripperOpennessState = 3;
            } else if (25 <= openness && openness < 35) {
                GripperOpennessStates[4].SetActive(true);
                gripperOpennessState = 4;
            } else if (35 <= openness && openness < 45) {
                GripperOpennessStates[5].SetActive(true);
                gripperOpennessState = 5;
            } else if (45 <= openness && openness <= 50) {
                GripperOpennessStates[6].SetActive(true);
                gripperOpennessState = 6;
            } else {
                throw new InvalidOperationException(
                    $"Invalid value for `openness`: '{openness}'. Value should be between -100 and 50"
                );
            }
            actionFinished(true);
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

        public IEnumerator MoveAhead(
            float? moveMagnitude = null,
            float noise = 0f,
            bool forceAction = false,
            float speed = 1,
            bool returnToStart = true
        ) {
            bool quickMoveSuccess = MoveRelative(
                    z: 1.0f,
                    moveMagnitude: moveMagnitude,
                    noise: noise,
                    forceAction: forceAction
            );
            
            if (quickMoveSuccess){
                Debug.Log("Use quick MoveAhead");
                yield return new ActionFinished() {success = true};
            } else {
                Debug.Log("Use slow MoveAhead");
                yield return base.MoveAgent(
                    ahead: moveMagnitude.GetValueOrDefault(gridSize),
                    speed: speed,
                    returnToStart: returnToStart
                );
            }
        }

        public IEnumerator MoveBack(
            float? moveMagnitude = null,
            float noise = 0f,
            bool forceAction = false,
            float speed = 1,
            bool returnToStart = true
        ) {
            bool quickMoveSuccess = MoveRelative(
                    z: -1.0f,
                    moveMagnitude: moveMagnitude,
                    noise: noise,
                    forceAction: forceAction
            );
            
            if (quickMoveSuccess){
                Debug.Log("Use quick MoveBack");
                yield return new ActionFinished() {success = true};
            } else {
                Debug.Log("Use slow MoveBack");
                yield return base.MoveAgent(
                    ahead: -moveMagnitude.GetValueOrDefault(gridSize),
                    speed: speed,
                    returnToStart: returnToStart
                );
            }
        }

        public IEnumerator RotateRight(
            float? degrees = null,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            bool quickRotateSuccess = Rotate(rotation: new Vector3(0, degrees.GetValueOrDefault(rotateStepDegrees), 0));
            if (quickRotateSuccess){
                Debug.Log("Use quick RotateRight");
                yield return new ActionFinished() {success = true};
            } else {
                Debug.Log("Use slow RotateRight");
                yield return base.RotateAgent(
                    degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                    speed: speed,
                    returnToStart: returnToStart
                );
            }
        }

        public IEnumerator RotateLeft(
            float? degrees = null,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            bool quickRotateSuccess = Rotate(rotation: new Vector3(0, -degrees.GetValueOrDefault(rotateStepDegrees), 0));
            if (quickRotateSuccess){
                Debug.Log("Use quick RotateLeft");
                yield return new ActionFinished() {success = true};
            } else {
                Debug.Log("Use small RotateLeft");
                yield return base.RotateAgent(
                    degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                    speed: speed,
                    returnToStart: returnToStart
                );
            }
        }

        public bool MoveRelative(
            float? moveMagnitude = null,
            float x = 0f,
            float z = 0f,
            float noise = 0f,
            bool forceAction = false
        ) {

            if (!moveMagnitude.HasValue) {
                moveMagnitude = gridSize;
            } else if (moveMagnitude.Value <= 0f) {
                throw new InvalidOperationException("moveMagnitude must be null or >= 0.");
            }

            if (!allowHorizontalMovement && Math.Abs(x) > 0) {
                throw new InvalidOperationException("Controller does not support horizontal movement. Set AllowHorizontalMovement to true on the Controller.");
            }

            var moveLocal = new Vector3(x, 0, z);
            float xzMag = moveLocal.magnitude;
            if (xzMag > 1e-5f) {
                // rotate a small amount with every movement since robot doesn't always move perfectly straight
                if (this.applyActionNoise) {
                    var rotateNoise = (float)systemRandom.NextGaussian(rotateGaussianMu, rotateGaussianSigma / 2.0f);
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0.0f, rotateNoise, 0.0f));
                }

                var moveLocalNorm = moveLocal / xzMag;
                var magnitudeWithNoise = GetMoveMagnitudeWithNoise(
                    moveMagnitude: xzMag * moveMagnitude.Value,
                    noise: noise
                );

                return base.moveInDirection(
                    direction: this.transform.rotation * (moveLocalNorm * magnitudeWithNoise),
                    forceAction: forceAction
                );
            } else {
                errorMessage = "either x or z must be != 0 for the MoveRelative action";
                return false;
            }
        }

        protected float GetMoveMagnitudeWithNoise(float moveMagnitude, float noise) {
            float internalNoise = applyActionNoise ? (float)systemRandom.NextGaussian(movementGaussianMu, movementGaussianSigma) : 0;
            return moveMagnitude + noise + (float)internalNoise;
        }

        protected bool moveInDirection(
            Vector3 direction,
            string objectId = "",
            float maxDistanceToObject = -1.0f,
            bool forceAction = false,
            bool manualInteract = false,
            HashSet<Collider> ignoreColliders = null
        ) {
            Vector3 targetPosition = transform.position + direction;
            if (checkIfSceneBoundsContainTargetPosition(targetPosition) &&
                CheckIfItemBlocksAgentMovement(direction, forceAction) && // forceAction = true allows ignoring movement restrictions caused by held objects
                CheckIfAgentCanMove(direction, ignoreColliders)) {

                // only default hand if not manually interacting with things
                if (!manualInteract) {
                    DefaultAgentHand();
                }

                Vector3 oldPosition = transform.position;
                transform.position = targetPosition;
                this.snapAgentToGrid();

                if (objectId != "" && maxDistanceToObject > 0.0f) {
                    if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                        errorMessage = "No object with ID " + objectId;
                        transform.position = oldPosition;
                        return false;
                    }
                    SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                    if (distanceToObject(sop) > maxDistanceToObject) {
                        errorMessage = "Agent movement would bring it beyond the max distance of " + objectId;
                        transform.position = oldPosition;
                        return false;
                    }
                }
                return true;
            } else {
                return false;
            }
        }

        public bool Rotate(Vector3 rotation) {
            return Rotate(rotation: rotation, noise: 0);
        }

        public bool Rotate(Vector3 rotation, float noise, bool manualInteract = false) {
            // only default hand if not manually Interacting with things
            if (!manualInteract) {
                DefaultAgentHand();
            }

            float rotateAmountDegrees = GetRotateMagnitudeWithNoise(rotation: rotation, noise: noise);

            // multiply quaternions to apply rotation based on rotateAmountDegrees
            transform.rotation = (
                transform.rotation
                * Quaternion.Euler(new Vector3(0.0f, rotateAmountDegrees, 0.0f))
            );
            if (isAgentCapsuleColliding()) {
                transform.rotation = (
                    transform.rotation
                    * Quaternion.Euler(new Vector3(0.0f, -rotateAmountDegrees, 0.0f))
                );
                return false;
            }
            return true;
        }

        protected float GetRotateMagnitudeWithNoise(Vector3 rotation, float noise) {
            float internalNoise = applyActionNoise ? (float)systemRandom.NextGaussian(rotateGaussianMu, rotateGaussianSigma) : 0;
            return rotation.y + noise + (float)internalNoise;
        }

        protected bool isAgentCapsuleColliding(
            HashSet<Collider> collidersToIgnore = null,
            bool includeErrorMessage = false
        ) {
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            foreach (
                Collider c in PhysicsExtensions.OverlapCapsule(
                    GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore
                )
            ) {
                if ((!hasAncestor(c.transform.gameObject, gameObject)) && (
                    collidersToIgnore == null || !collidersToIgnoreDuringMovement.Contains(c))
                ) {
                    if (includeErrorMessage) {
                        SimObjPhysics sop = ancestorSimObjPhysics(c.gameObject);
                        String collidedWithName;
                        if (sop != null) {
                            collidedWithName = sop.ObjectID;
                        } else {
                            collidedWithName = c.gameObject.name;
                        }
                        errorMessage = $"Collided with: {collidedWithName}.";
                    }
#if UNITY_EDITOR
                    Debug.Log("Collided with: ");
                    Debug.Log(c);
                    Debug.Log(c.enabled);
#endif
                    return true;
                }
            }
            return false;
        }

    }

}
