using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.FirstPerson {

    public partial class KinovaArmAgentController : ArmAgentController {
        public KinovaArmAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public override ActionFinished InitializeBody(ServerAction initializeAction) {
            base.InitializeBody(initializeAction);
            Debug.Log("initializing arm");
            IKArm.SetActive(true);
            Arm = this.GetComponentInChildren<IK_Robot_Arm_Controller>();
            Arm.PhysicsController = this;
            var armTarget = Arm.transform.Find("robot_arm_FK_IK_rig").Find("IK_rig").Find("IK_pos_rot_manipulator");
            Vector3 pos = armTarget.transform.localPosition;
            pos.z = 0.4f; // pulls the arm in from being fully extended
            armTarget.transform.localPosition = pos;
            var ikSolver = this.GetComponentInChildren<FK_IK_Solver>();
            Debug.Log("running manipulate arm");
            ikSolver.ManipulateArm();
            return ActionFinished.Success;
        }

        private IK_Robot_Arm_Controller getArmImplementation() {
            IK_Robot_Arm_Controller arm = GetComponentInChildren<IK_Robot_Arm_Controller>();
            if (arm == null) {
                throw new InvalidOperationException(
                    "Agent does not have kinematic arm or is not enabled.\n" +
                    $"Make sure there is a '{typeof(IK_Robot_Arm_Controller).Name}' component as a child of this agent."
                );
            }
            return arm;
        }

        protected override ArmController getArm() {
            return getArmImplementation();
        }

        public void TeleportArm(
            Vector3? position = null,
            Vector4? rotation = null,
            float? armHeight = null,
            float? elbowOrientation = null,
            bool worldRelative = false,
            bool forceAction = false
        ) {
            GameObject heightManip = this.GetComponent<BaseAgentComponent>().IKArm;
            GameObject posRotManip = this.GetComponent<BaseAgentComponent>().IKArm.GetComponent<IK_Robot_Arm_Controller>().GetArmTarget();
            GameObject elbowManip = this.GetComponent<BaseAgentComponent>().IKArm.GetComponent<IK_Robot_Arm_Controller>().GetElbowTarget();

            // cache old values in case there's a failure
            Vector3 oldLocalPosition = posRotManip.transform.localPosition;
            float oldLocalRotationAngle;
            Vector3 oldLocalRotationAxis;
            posRotManip.transform.localRotation.ToAngleAxis(angle: out oldLocalRotationAngle, axis: out oldLocalRotationAxis);
            float oldArmHeight = heightManip.transform.localPosition.y;
            float oldElbowOrientation = elbowManip.transform.localEulerAngles.z;
            
            // establish defaults in the absence of inputs
            if (position == null) {
                position = new Vector3(0f, 0f, 0.4f);
            }

            if (rotation == null) {
                rotation = new Vector4(1f, 0f, 0f, 0f);
            }

            if (armHeight == null) {
                armHeight = -0.003f;
            }

            if (elbowOrientation == null) {
                elbowOrientation = 0f;
            }

            // teleport arm! (height first, since world-relative positioning needs to take it into account)
            heightManip.transform.localPosition = new Vector3(
                heightManip.transform.localPosition.x,
                (float)armHeight,
                heightManip.transform.localPosition.z
            );

            // teleport arm-elements
            if (!worldRelative) {
                    posRotManip.transform.localPosition = (Vector3)position;
                    posRotManip.transform.localRotation = Quaternion.AngleAxis(
                        ((Vector4)rotation).w % 360,
                        new Vector3(((Vector4)rotation).x, ((Vector4)rotation).y, ((Vector4)rotation).z)
                    );
            } else {
                    posRotManip.transform.position = (Vector3)position;
                    posRotManip.transform.rotation = Quaternion.AngleAxis(
                        ((Vector4)rotation).w % 360,
                        new Vector3(((Vector4)rotation).x, ((Vector4)rotation).y, ((Vector4)rotation).z)
                    );
            }

            elbowManip.transform.localEulerAngles = new Vector3(
                elbowManip.transform.localEulerAngles.x,
                elbowManip.transform.localEulerAngles.y,
                (float)elbowOrientation
            );

            if (Arm.IsArmColliding() && !forceAction) {
                errorMessage = "collision detected at desired transform, cannot teleport";
                heightManip.transform.localPosition = new Vector3(
                    heightManip.transform.localPosition.x,
                    oldArmHeight,
                    heightManip.transform.localPosition.z);
                posRotManip.transform.localPosition = oldLocalPosition;
                posRotManip.transform.localRotation = Quaternion.AngleAxis(oldLocalRotationAngle, oldLocalRotationAxis);
                elbowManip.transform.localEulerAngles = new Vector3(
                    elbowManip.transform.localEulerAngles.x,
                    elbowManip.transform.localEulerAngles.y,
                    oldElbowOrientation);
                actionFinished(false);
            } else {
                actionFinished(true);
            }
        }

        /*
        Let's say you wanted the agent to be able to rotate the object it's
        holding so that it could get multiple views of the object. You
        could do this by using the RotateWristRelative action but the downside
        of using that function is that object will be translated as the
        wrist rotates. This RotateWristAroundHeldObject action gets around this
        problem by allowing you to specify how much you'd like the object to
        rotate by and then figuring out how to translate/rotate the wrist so
        that the object rotates while staying fixed in space.

        Note that the object may stil be translated if the specified rotation
        of the object is not feasible given the arm's DOF and length/joint
        constraints.
        */
        public void RotateWristAroundHeldObject(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            bool returnToStart = true
        ) {
            var arm = getArm();

            if (arm.heldObjects.Count == 1) {
                SimObjPhysics sop = arm.heldObjects.Keys.ToArray()[0];
                RotateWristAroundPoint(
                    point: sop.gameObject.transform.position,
                    pitch: pitch,
                    yaw: yaw,
                    roll: roll,
                    speed: speed,
                    returnToStart: returnToStart
                );
            } else {
                actionFinished(
                    success: false,
                    errorMessage: $"Cannot RotateWristAroundHeldObject when holding" +
                        $" != 1 objects, currently holding {arm.heldObjects.Count} objects."
                );
            }
        }

        /*
        Rotates and translates the wrist so that its position
        stays fixed relative some given point as that point
        rotates some given amount.
        */
        public void RotateWristAroundPoint(
            Vector3 point,
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            bool returnToStart = true
        ) {
            IK_Robot_Arm_Controller arm = getArmImplementation();

            arm.rotateWristAroundPoint(
                controller: this,
                rotatePoint: point,
                rotation: Quaternion.Euler(pitch, yaw, -roll),
                degreesPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart
            );
        }

        /*
        Rotates the elbow (in a relative fashion) by some given
        number of degrees. Easiest to see how this works by
        using the editor debugging and shift+alt+(q/e).

        Currently not a completely finished action. New logic is needed
        to prevent self-collisions. In particular we need to
        account for the hierarchy of rigidbodies of each arm joint and
        determine how to detect collision between a given arm joint and other arm joints.
        */
        public void RotateElbowRelative(
            float degrees,
            float speed = 10f,
            bool returnToStart = true
        ) {
            IK_Robot_Arm_Controller arm = getArmImplementation();

            arm.rotateElbowRelative(
                controller: this,
                degrees: degrees,
                degreesPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart
            );
        }

        /*
        Same as RotateElbowRelative but rotates the elbow to a given angle directly.
        */
        public void RotateElbow(
            float degrees,
            float speed = 10f,
            bool returnToStart = true
        ) {
            IK_Robot_Arm_Controller arm = getArmImplementation();

            arm.rotateElbow(
                controller: this,
                degrees: degrees,
                degreesPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart
            );
        }
    }
}
