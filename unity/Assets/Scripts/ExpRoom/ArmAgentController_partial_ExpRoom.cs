using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Priority_Queue;
using RandomExtensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Characters.FirstPerson {
    public partial class KinovaArmAgentController : ArmAgentController {
        public void AttachObjectToArmWithFixedJoint(string objectId) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            actionFinished(getArmImplementation().AttachObjectToArmWithFixedJoint(target));
        }

        public void BreakFixedJoints() {
            actionFinished(getArmImplementation().BreakFixedJoints());
        }
    }
}
