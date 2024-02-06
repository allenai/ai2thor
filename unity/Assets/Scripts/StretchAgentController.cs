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

        protected Transform gimbalBase, primaryGimbal, secondaryGimbal;
        protected float gimbalBaseStartingXPosition, gimbalBaseStartingZPosition, gimbalBaseStartingXRotation, gimbalBaseStartingYRotation;
        protected float primaryStartingXRotation, secondaryStartingXRotation;
        protected float maxBaseXZOffset = 0.25f, maxBaseXYRotation = 10f;
        protected float minGimbalXRotation = -80.001f, maxGimbalXRotation = 80.001f;
        public int gripperOpennessState = 0;

        public StretchAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }
        GameObject CameraGimbal2;

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
            
            var gimbalBaseName = "FixedCameraGimbalBase";
            var primaryGimbalName = "FixedCameraGimbalPrimary";
            var secondaryGimbalName = "FixedCameraGimbalSecondary";

            this.gimbalBase = m_CharacterController.transform.FirstChildOrDefault(x => x.name == gimbalBaseName);
            this.primaryGimbal = m_CharacterController.transform.FirstChildOrDefault(x => x.name == primaryGimbalName);
            this.secondaryGimbal = m_CharacterController.transform.FirstChildOrDefault(x => x.name == secondaryGimbalName);

            gimbalBaseStartingXPosition = gimbalBase.transform.localPosition.x;
            gimbalBaseStartingZPosition = gimbalBase.transform.localPosition.z;
            gimbalBaseStartingXRotation = gimbalBase.transform.localEulerAngles.x;
            gimbalBaseStartingYRotation = gimbalBase.transform.localEulerAngles.y;
            primaryStartingXRotation = primaryGimbal.transform.localEulerAngles.x;
            secondaryStartingXRotation = secondaryGimbal.transform.localEulerAngles.x;

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

            // motor gimbals setup
            if (UseMotorCameraGimbals == true) {
                CameraGimbal2 = MotorCameraGimbals.transform.GetChild(0).gameObject;

                // rehierchize primary camera to motorized gimbals, to accurately reflect real-life camera rotation
                m_Camera.transform.SetParent(CameraGimbal2.transform);

                // set up primary camera parameters
                m_Camera.transform.localPosition = new Vector3(0.03f, 0.007f, 0.044f);
                m_Camera.transform.localEulerAngles = Vector3.zero;
                fp_camera_2.fieldOfView = 69f;

                // set up arm-camera parameters
                // ???

            // fixed gimbals setup
            } else {
                // rehierchize cameras to fixed gimbals
                m_Camera.transform.SetParent(FixedCameraGimbalPrimary.transform);
                fp_camera_2.transform.SetParent(FixedCameraGimbalSecondary.transform);

                // set up primary camera parameters
                m_Camera.transform.localPosition = new Vector3(0.015f, 0.01832385f, 0.06322689f);
                m_Camera.transform.localEulerAngles = Vector3.zero;
                m_Camera.fieldOfView = 59f;

                // set up arm-camera parameters
                fp_camera_2.transform.localPosition = new Vector3(0.015f, 0.01832385f, 0.06322689f);
                m_Camera.transform.localEulerAngles = Vector3.zero;
                fp_camera_2.fieldOfView = 59f;
            }

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

            return base.RotateWristRelative(
                pitch: 0f,
                yaw: yaw,
                roll: 0f,
                speed: speed,
                returnToStart: returnToStart
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

    }

}
