using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class VR_FPSAgentController : PhysicsRemoteFPSAgentController {

        public VR_FPSAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public bool TeleportCheck (Vector3 position, Vector3 rotation, bool forceAction, float? horizon = null) {
            if (forceAction) return true;

            position.y = transform.position.y;
            horizon = horizon == null ? m_Camera.transform.localEulerAngles.x : (float)horizon;

            // Note: using Mathf.Approximately uses Mathf.Epsilon, which is significantly
            // smaller than 1e-2f. I'm not confident that will work in many cases.
            //if ((Mathf.Abs(rotation.x) >= 1e-2f || Mathf.Abs(rotation.z) >= 1e-2f)) {
            //    return false;
            //}

            // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
            //if ((horizon > maxDownwardLookAngle || horizon < -maxUpwardLookAngle)) {
            //    return false;
            //}

            if (!agentManager.SceneBounds.Contains(position)) {
                return false;
            }

            //if (!isPositionOnGrid(position)) {
            //    return false;
            //}

            // cache old values in case there's a failure
            Vector3 oldPosition = transform.position;
            //Quaternion oldRotation = transform.rotation;
            //float oldHorizon = m_Camera.transform.localEulerAngles.x;

            // here we actually teleport
            transform.position = position;
            //transform.localEulerAngles = new Vector3(0, rotation.y, 0);
            //m_Camera.transform.localEulerAngles = new Vector3((float)horizon, 0, 0);

            if (isAgentCapsuleColliding(
                    collidersToIgnore: collidersToIgnoreDuringMovement, includeErrorMessage: true
                )
            ) {
                transform.position = oldPosition;
                //transform.rotation = oldRotation;
                //m_Camera.transform.localEulerAngles = new Vector3(oldHorizon, 0, 0);
                return false;
            }

            transform.position = oldPosition;
            //transform.rotation = oldRotation;
            //m_Camera.transform.localEulerAngles = new Vector3(oldHorizon, 0, 0);
            return true;
        }
    }
}
