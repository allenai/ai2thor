using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;
using RandomExtensions;

namespace UnityStandardAssets.Characters.FirstPerson {
    public partial class ArmAgentController : PhysicsRemoteFPSAgentController {

        public void AttachObjectToArmWithFixedJoint(string objectId, bool right = true) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            actionFinished(getArm(right: right).AttachObjectToArmWithFixedJoint(target));
        }

        public void BreakFixedJoints(bool right = true) {
            actionFinished(getArm(right: right).BreakFixedJoints());
        }

    }
}