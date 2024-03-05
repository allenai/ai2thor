using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestStretchArmMeta : TestBase
    {
        [UnityTest]
        public IEnumerator TestStretchArmGripperOpennessMetadata () {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "stretch";
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            Stretch_Robot_Arm_Controller armController = agent.GetComponentInChildren<Stretch_Robot_Arm_Controller>();

            action.Clear();

            action["action"] = "RotateRight";
            yield return step(action);

            //Debug.Log("about to generate arm meta");
            //call arm metadata
            ArmMetadata meta = armController.GenerateMetadata();
            //check meta is in correct state
            Debug.Log($"openness should be 0 but is: {meta.gripperOpennessState}");
            Assert.AreEqual(meta.gripperOpennessState, 0);
            //now lets open the gripper a bit
            action.Clear();

            action["action"] = "SetGripperOpenness";
            action["openness"] = 10f;
            //this should set the gripper to state 2
            yield return step(action);        

            meta = armController.GenerateMetadata();
            Debug.Log($"openness should be 2 but is: {meta.gripperOpennessState}");
            Assert.AreEqual(meta.gripperOpennessState, 2);
        }

        [UnityTest]
        public IEnumerator TestStretchArmRootRelativeHandSphere () {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "stretch";
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            Stretch_Robot_Arm_Controller armController = agent.GetComponentInChildren<Stretch_Robot_Arm_Controller>();

            action.Clear();

            action["action"] = "RotateRight";
            yield return step(action);

            //Debug.Log("about to generate arm meta");
            //call arm metadata
            ArmMetadata meta = armController.GenerateMetadata();
            //check meta is in correct state
            string handSphereCenter = meta.handSphereCenter.ToString("F4");
            Assert.AreEqual(handSphereCenter, "(-1.0610, 0.2524, 0.9761)");
            string rootRelativeCenter = meta.rootRelativeHandSphereCenter.ToString("F5");
            Assert.AreEqual(rootRelativeCenter, "(-0.00007, 0.16460, -0.20476)");


            //Debug.Log($"world handSphereCenter is {meta.handSphereCenter.ToString("F5")}");
            //Debug.Log($"root relative sphere center: {meta.rootRelativeHandSphereCenter.ToString("F5")}");
        }
    }

}
