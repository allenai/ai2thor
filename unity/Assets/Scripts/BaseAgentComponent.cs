using UnityEngine;
using System.Collections.Generic;
namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    public class BaseAgentComponent : MonoBehaviour {
        // debug draw bounds of objects in editor
#if UNITY_EDITOR
        protected List<Bounds> gizmobounds = new List<Bounds>();
#endif
        public GameObject AgentHand = null;
        public GameObject DefaultHandPosition = null;
        public Transform rotPoint;
        public GameObject DebugPointPrefab;
        public GameObject GridRenderer = null;
        public GameObject DebugTargetPointPrefab;
        public GameObject VisibilityCapsule = null;// used to keep track of currently active VisCap: see different vis caps for modes below
        public GameObject TallVisCap;// meshes used for Tall mode
        public GameObject IKArm; // reference to the IK_Robot_Arm_Controller arm
        public GameObject BotVisCap;// meshes used for Bot mode
        public GameObject DroneVisCap;// meshes used for Drone mode
        public GameObject DroneBasket;// reference to the drone's basket object
        public GameObject StretchVisCap; // meshes used for Stretch mode
        public GameObject StretchArm; // reference to the Stretch_Arm_Controller arm
        public GameObject CrackedCameraCanvas = null;

        public GameObject[] ToSetActive = null;
        public Material[] ScreenFaces; // 0 - neutral, 1 - Happy, 2 - Mad, 3 - Angriest
        public MeshRenderer MyFaceMesh;
        public GameObject[] TargetCircles = null;

        [HideInInspector]
        public BaseFPSAgentController agent;

        public DroneObjectLauncher DroneObjectLauncher;

        void LateUpdate() {
            if (this.agent == null) {
                return;
            }


#if UNITY_WEBGL
                // For object highlight shader to properly work, all visible objects should be populated not conditioned
                // on the objectid of a completed action
                this.agent.VisibleSimObjPhysics = this.agent.VisibleSimObjs(false);
#endif

            // editor
#if UNITY_EDITOR
            if (this.agent.agentState == AgentState.ActionComplete) {
                this.agent.VisibleSimObjPhysics = this.agent.VisibleSimObjs(false);
            }
#endif

        }

        void FixedUpdate() {
            if (this.agent != null) {
                this.agent.FixedUpdate();
            }
        }
        // Handle collisions - CharacterControllers don't apply physics innately, see "PushMode" check below
        // XXX: this will be used for truly continuous movement over time, for now this is unused
        protected void OnControllerColliderHit(ControllerColliderHit hit) {
            if (this.agent == null) {
                return;
            }

            if (hit.gameObject.GetComponent<StructureObject>()) {
                if (hit.gameObject.GetComponent<StructureObject>().WhatIsMyStructureObjectTag == StructureObjectTag.Floor) {
                    return;
                }
            }


            if (!this.agent.collisionsInAction.Contains(hit.gameObject.name)) {
                this.agent.collisionsInAction.Add(hit.gameObject.name);
            }

            Rigidbody body = hit.collider.attachedRigidbody;
            // don't move the rigidbody if the character is on top of it
            if (this.agent.m_CollisionFlags == CollisionFlags.Below) {
                return;
            }

            if (body == null || body.isKinematic) {
                return;
            }

            // push objects out of the way if moving through them and they are Moveable or CanPickup (Physics)
            if (this.agent.PushMode) {
                float pushPower = 2.0f;
                Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
                body.velocity = pushDir * pushPower;
            }
            // if we touched something with a rigidbody that needs to simulate physics, generate a force at the impact point
            // body.AddForce(m_CharacterController.velocity * 15f, ForceMode.Force);
            // body.AddForceAtPosition (m_CharacterController.velocity * 15f, hit.point, ForceMode.Acceleration);// might have to adjust the force vector scalar later
        }

    }
}
