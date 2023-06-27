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

        [SerializeField]
        private Collider FloorCollider;

        [SerializeField]
        private PhysicMaterial FloorColliderPhysicsMaterial;

        // void Awake() {
        //     standingLocalCameraPosition = m_Camera.transform.localPosition;
        //     Debug.Log($"------ AWAKE {standingLocalCameraPosition}");
        // }

        // TODO: Reimplemebt for Articulation body
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

            getArmImplementation().manipulateArm();

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
                disableRendering: disableRendering
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
                disableRendering: disableRendering
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

        //helper functions to set physics material values
        public void SetFloorColliderToSlippery(){
            FloorColliderPhysicsMaterial.staticFriction = 0;
            FloorColliderPhysicsMaterial.dynamicFriction = 0;
        }

        public void SetFloorColliderToHighFriction(){
            FloorColliderPhysicsMaterial.staticFriction = 1;
            FloorColliderPhysicsMaterial.dynamicFriction = 1;
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

        // public void PickupObject(List<string> objectIdCandidates = null) {
        //     var arm = getArm();
        //     actionFinished(arm.PickupObject(objectIdCandidates, ref errorMessage), errorMessage);
        // }

        // public void ReleaseObject() {
        //     var arm = getArm();
        //     arm.DropObject();

        //     // TODO: only return after object(s) dropped have finished moving
        //     // currently this will return the frame the object is released
        //     actionFinished(true);
        // }

        // TODO: Eli implement MoveAgent and RotateAgent functions below
        public override void MoveAgent(
            float ahead = 1,
            float right = 0,
            float speed = 1,
            float acceleration = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            Vector3 agentDirection = transform.TransformDirection(Vector3.forward);
            this.transform.GetComponent<ArticulationBody>().AddForce(17f * agentDirection);

            // float distance = ahead;
            // ArticulationBody ab = transform.GetComponent<ArticulationBody>();
            // Vector3 initialPosition = ab.transform.position;
            // Vector3 finalPosition = ab.transform.TransformPoint(Vector3.forward * distance);

            // // determine if agent can even accelerate to max velocity and decelerate to 0 before reaching target position
            // float accelerationDistance = Mathf.Pow(speed,2) / (2 * acceleration);

            // if (2 * accelerationDistance > distance) {
            //     speed = Mathf.Sqrt(distance * acceleration);
            // }

            // float accelerationTime = speed / acceleration;

            // Vector3 currentPosition = ab.transform.position;
            // // Debug.Log($"position of agent: {currentPosition}");
            
            // Vector3 forceDirection = new Vector3(0,0,acceleration);
            
            // if (finalPosition.magnitude - currentPosition.magnitude < 1e-3f) {
            //     ab.AddForce(ab.mass * Vector3.back * ab.velocity.magnitude * Time.fixedDeltaTime);
            //     moveState = MoveState.Idle;
            //     Debug.Log("STOP!");
            // } 

            // // Apply acceleration over acceleration-time
            // if (timePassed < accelerationTime) {
            //     ab.AddForce(ab.mass * forceDirection);
            //     Debug.Log("Accelerating!");
            // }

            // if (accelerationDistance >= (finalPosition - currentPosition).magnitude) {
            //     ab.AddForce(ab.mass * -forceDirection);
            //     Debug.Log("Decelerating!");
            // }

            // timePassed += Time.fixedDeltaTime;


            // Use Continuous move

            // if (ahead == 0 && right == 0) {
            //     throw new ArgumentException("Must specify ahead or right!");
            // }
            // Vector3 direction = new Vector3(x: right, y: 0, z: ahead);
            // float fixedDeltaTimeFloat = fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);

            // CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();

            // Vector3 directionWorld = transform.TransformDirection(direction);
            // Vector3 targetPosition = transform.position + directionWorld;

            // collisionListener.Reset();

            // IEnumerator move = ContinuousMovement.moveAB(
            //     controller: this,
            //     collisionListener: collisionListener,
            //     moveTransform: this.transform,
            //     targetPosition: targetPosition,
            //     fixedDeltaTime: fixedDeltaTimeFloat,
            //     unitsPerSecond: speed,
            //     returnToStartPropIfFailed: returnToStart,
            //     localPosition: false
            // );

            // if (disableRendering) {
            //     unrollSimulatePhysics(
            //         enumerator: move,
            //         fixedDeltaTime: fixedDeltaTimeFloat
            //     );
            // } else {
            //     StartCoroutine(move);
            // }
        }

      

        public override void RotateAgent(
            float degrees,
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            var ab = GetComponent<ArticulationBody>();
            var axis = Vector3.up;
            Vector3 target = Quaternion.Inverse(ab.anchorRotation) * axis * degrees;
            ab.anchorRotation = Quaternion.Euler(0, degrees, 0);


        // assign to the drive targets...
        // ArticulationDrive xDrive = ab.xDrive;
        // xDrive.target = target.x;
        // ab.xDrive = xDrive;

        // ArticulationDrive yDrive = ab.yDrive;
        // yDrive.target = target.y;
        // ab.yDrive = yDrive;

        // ArticulationDrive zDrive = ab.zDrive;
        // zDrive.target = target.z;
        // ab.zDrive = zDrive;

        actionFinished(true);

            // Something like below

            //  IEnumerator rotate = ContinuousMovement.rotateAB(
            //     controller: this,
            //     collisionListener: this.GetComponentInParent<CollisionListener>(),
            //     moveTransform: this.transform,
            //     targetRotation: this.transform.rotation * Quaternion.Euler(0.0f, degrees, 0.0f),
            //     fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
            //     radiansPerSecond: speed,
            //     returnToStartPropIfFailed: returnToStart
            // );

            // if (disableRendering) {
            //     unrollSimulatePhysics(
            //         enumerator: rotate,
            //         fixedDeltaTime: fixedDeltaTime
            //     );
            // } else {
            //     StartCoroutine(rotate);
            // }
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
            float speed = 10f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            // pitch and roll are not supported for the stretch and so we throw an error
            if (pitch != 0f || roll != 0f) {
                throw new System.NotImplementedException("Pitch and roll are not supported for the stretch agent.");
            }

            Debug.Log("executing RotateWristRelative from ArticulatedAgentController");
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

    }
}
