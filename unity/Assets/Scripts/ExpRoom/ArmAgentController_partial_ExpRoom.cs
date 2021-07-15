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

        public void AttachedObjectToArmWithFixedJoint(string objectId) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinished(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            actionFinished(getArm().AttachedObjectToArmWithFixedJoint(target));
        }

        public void BreakFixedJoints() {
            actionFinished(getArm().BreakFixedJoints());
        }

    }
}