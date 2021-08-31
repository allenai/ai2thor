using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class LocobotFPSAgentController : StochasticRemoteFPSAgentController {
        public LocobotFPSAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        protected override void SetAgentMode() {
            // toggle FirstPersonCharacterCull

            VisibilityCapsule = BotVisCap;
            m_CharacterController.center = new Vector3(0, -0.45f, 0);
            m_CharacterController.radius = 0.175f;
            m_CharacterController.height = 0.9f;

            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;

            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            // camera position
            m_Camera.transform.localPosition = new Vector3(0, -0.0312f, 0);

            // camera FOV
            m_Camera.fieldOfView = 60f;

            // set camera stand/crouch local positions for Tall mode
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition + new Vector3(0, -0.2206f, 0);// smaller y offset if Bot

            // limit camera from looking too far down
            this.maxDownwardLookAngle = 30f;
            this.maxUpwardLookAngle = 29f;
            // this.horizonAngles = new float[] { 30.0f, 0.0f, 330.0f };
        }
    }
}
    