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
            if (originalCapsule == null) {
                var cc = this.GetComponent<CapsuleCollider>();

                return new CapsuleData {
                    radius = cc.radius,
                    height = cc.height,
                    center = cc.center,
                    transform = cc.transform
                };
            }
            else {
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
            Debug.Log($"prev Position {this.transform.position}");

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

            Debug.Log($"Position {this.transform.position}");
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

        public override void MoveArmBaseUp(
             float distance,
             float speed = 1,
             float? fixedDeltaTime = null,
             bool returnToStart = true,
             bool disableRendering = true
         ) {
            Debug.Log("MoveArmBaseUp from ArticulatedAgentController");
            SetFloorColliderToHighFriction();
            var arm = getArmImplementation();
            arm.moveArmBaseUp(
                controller: this,
                distance: distance,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart,
                disableRendering: disableRendering,
                useLimits: false
            );
        }

        //with limits
        public void MoveArmBaseUp(
             float distance,
             bool useLimits,
             float speed = 1,
             float? fixedDeltaTime = null,
             bool returnToStart = true,
             bool disableRendering = true
         ) {
            Debug.Log("MoveArmBaseUp from ArticulatedAgentController");
            SetFloorColliderToHighFriction();
            var arm = getArmImplementation();
            arm.moveArmBaseUp(
                controller: this,
                distance: distance,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart,
                disableRendering: disableRendering,
                useLimits: useLimits
            );
        }

        public override void MoveArmBaseDown(
            float distance,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {            
            Debug.Log("MoveArmBaseDown from ArticulatedAgentController (pass negative distance to MoveArmBaseUp)");
            MoveArmBaseUp(
                distance: -distance,
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                useLimits: false
            );
        }

        //with limits
        public void MoveArmBaseDown(
            float distance,
            bool useLimits,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {            
            Debug.Log("MoveArmBaseDown from ArticulatedAgentController (pass negative distance to MoveArmBaseUp)");
            MoveArmBaseUp(
                distance: -distance,
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                useLimits: useLimits
            );
        }

        public override void MoveArm(
            Vector3 position,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = true
        ) {
            var arm = getArmImplementation();
            SetFloorColliderToHighFriction();
            arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement,
                disableRendering: disableRendering
            );
        }

        //move arm overload with limits
        public void MoveArm(
            Vector3 position,
            bool useLimits,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = true
        ) {
            var arm = getArmImplementation();
            SetFloorColliderToHighFriction();
            arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement,
                disableRendering: disableRendering,
                useLimits: useLimits
            );
        }

        //helper functions to set physics material values
        public void SetFloorColliderToSlippery(){
            FloorColliderPhysicsMaterial.staticFriction = 0;
            FloorColliderPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum; //ensure min friction take priority

            //FloorColliderPhysicsMaterial.dynamicFriction = 0;
        }

        public void SetFloorColliderToHighFriction(){
            FloorColliderPhysicsMaterial.staticFriction = 1;
            FloorColliderPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Maximum; //ensure max friction takes priority
            //FloorColliderPhysicsMaterial.dynamicFriction = 1;
        }

        public void TeleportFull(Vector3 position, Vector3 rotation, float? horizon = null, bool forceAction = false) {
            //Vector3 oldPosition = transform.position;
            //Quaternion oldRotation = transform.rotation;
            //float oldHorizon = m_Camera.transform.localEulerAngles.x;
            
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

            ArticulationBody myBody = this.GetComponent<ArticulationBody>();
            myBody.TeleportRoot(position, realRotationAsQuaternionBecauseYes);
            m_Camera.transform.localEulerAngles = new Vector3(horizonf, 0, 0);

            actionFinished(true);
        }

        public void MoveAgent(
            float moveMagnitude = 1,
            float speed = 1,
            float acceleration = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            SetFloorColliderToSlippery();
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
                return;
            }
            
            AgentMoveParams amp = new AgentMoveParams {
                agentState = ABAgentState.Moving,
                distance = Mathf.Abs(moveMagnitude),
                speed = speed,
                acceleration = acceleration,
                agentMass = CalculateTotalMass(this.transform),
                tolerance = 1e-6f,
                maxTimePassed = 10.0f,
                positionCacheSize = 10,
                direction = direction,
                maxForce = 200f
            };
        
            float fixedDeltaTimeFloat = fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);
            
            this.GetComponent<ArticulatedAgentSolver>().PrepToControlAgentFromAction(amp);

            // now that move call happens
            IEnumerator move = ContinuousMovement.moveAB(
                movable: this.getBodyMovable(),
                controller: this,
                fixedDeltaTime: fixedDeltaTimeFloat
            );

            if (disableRendering) {
                ContinuousMovement.unrollSimulatePhysics(
                    enumerator: move,
                    fixedDeltaTime: fixedDeltaTimeFloat
                );
            } else {
                StartCoroutine(move);
            }
        }


        public void MoveAhead(
            float? moveMagnitude = null,
            float speed = 0.14f,
            float acceleration = 0.14f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = false
        ) {
            MoveAgent(
                moveMagnitude: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                acceleration: acceleration,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public void MoveBack(
            float? moveMagnitude = null,
            float speed = 0.14f,
            float acceleration = 0.14f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = false
        ) {
            MoveAgent(
                moveMagnitude: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                acceleration: acceleration,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public void RotateRight(
            float? degrees = null,
            bool manualInteract = false, // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,    // TODO: Unused, remove when refactoring the controllers
            float speed = 22.5f,
            float acceleration = 22.5f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = false,
            float fixedDeltaTime = 0.02f
        ) {
            RotateAgent(
                degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                acceleration: acceleration,
                waitForFixedUpdate: waitForFixedUpdate,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime
            );
        }

        public void RotateLeft(
            float? degrees = null,
            bool manualInteract = false, // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,    // TODO: Unused, remove when refactoring the controllers
            float speed = 22.5f,
            float acceleration = 22.5f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = false,
            float fixedDeltaTime = 0.02f
        ) {
            RotateAgent(
                degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                acceleration: acceleration,
                waitForFixedUpdate: waitForFixedUpdate,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime
            );
        }

        public void RotateAgent(
            float degrees,
            float speed = 22.5f,
            float acceleration = 22.5f,
            float? fixedDeltaTime = null,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            SetFloorColliderToSlippery();
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
                return;
            }

            AgentMoveParams amp = new AgentMoveParams {
                agentState = ABAgentState.Rotating,
                distance = Mathf.Abs(Mathf.Deg2Rad * degrees),
                speed = Mathf.Deg2Rad * speed,
                acceleration = Mathf.Deg2Rad * acceleration,
                agentMass = CalculateTotalMass(this.transform),
                tolerance = 1e-6f,
                maxTimePassed = 10.0f,
                positionCacheSize = 10,
                direction = direction,
                maxForce = 200f
            };

            float fixedDeltaTimeFloat = fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);

            this.GetComponent<ArticulatedAgentSolver>().PrepToControlAgentFromAction(amp);

            // now that rotate call happens
            IEnumerator rotate = ContinuousMovement.moveAB(
                movable: this.getBodyMovable(),
                controller: this,
                fixedDeltaTime: fixedDeltaTimeFloat,
                unitsPerSecond: speed,
                acceleration: acceleration
            );

            if (disableRendering) {
                ContinuousMovement.unrollSimulatePhysics(
                    enumerator: rotate,
                    fixedDeltaTime: fixedDeltaTimeFloat
                );
            } else {
                StartCoroutine(rotate);
            }
        }

        // not doing these for benchmark yet cause no
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
                posRotManip.transform.localEulerAngles = new Vector3(0, (float)rotation % 360, 0);
            } else {
                posRotManip.transform.position = (Vector3)position;
                posRotManip.transform.eulerAngles = new Vector3(0, (float)rotation % 360, 0);
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

        public override void RotateWristRelative(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 400f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            // pitch and roll are not supported for the stretch and so we throw an error
            if (pitch != 0f || roll != 0f) {
                throw new System.NotImplementedException("Pitch and roll are not supported for the stretch agent.");
            }
            Debug.Log($"executing RotateWristRelative from ArticulatedAgentController with speed {speed}");
            var arm = getArmImplementation();
            SetFloorColliderToHighFriction();
            arm.rotateWrist(
                controller: this,
                distance: yaw,
                //rotation: Quaternion.Euler(pitch, yaw, -roll),
                degreesPerSecond: speed,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart
            );
        }

        private float CalculateTotalMass(Transform rootTransform)
            {
                float totalMass = 0f;
                ArticulationBody rootBody = rootTransform.GetComponent<ArticulationBody>();
                if (rootBody != null)
                {
                    totalMass += rootBody.mass;
                }

                foreach (Transform childTransform in rootTransform)
                {
                    totalMass += CalculateTotalMass(childTransform);
                }

                return totalMass;
            }

        private MovableContinuous getBodyMovable() {
            return this.transform.GetComponent<ArticulatedAgentSolver>();
        }
    }
}
