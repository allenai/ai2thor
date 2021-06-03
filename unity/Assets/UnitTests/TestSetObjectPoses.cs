using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestSetObjectPoses : TestBase {
        [UnityTest]
        public IEnumerator TestSetObjectPoses_ObjectHierarchyReset_True() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();

            GameObject topObject = GameObject.Find("Objects");
            int numObjectsWithBadParents = 0;
            List<ObjectPose> ops = new List<ObjectPose>();
            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if ((!sop.isStatic) || sop.IsPickupable) {
                    numObjectsWithBadParents += sop.gameObject.transform.parent != topObject.transform ? 1 : 0;
                    ops.Add(new ObjectPose(objectName: sop.name, position: sop.transform.position, rotation: sop.transform.eulerAngles));
                }
            }

            Assert.That(
                numObjectsWithBadParents > 0,
                "Looks like there are not sim objects whose parents are sim objects." + 
                " This test isn't meaningful without this so you'll need to set it up so that there are."
            );

            action["action"] = "SetObjectPoses";
            action["objectPoses"] = ops;
            action["placeStationary"] = true;
            yield return step(action);

            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if ((!sop.isStatic) || sop.IsPickupable) {
                    Assert.That(sop.gameObject.transform.parent == topObject.transform);
                }
            }
        }
    }
}
