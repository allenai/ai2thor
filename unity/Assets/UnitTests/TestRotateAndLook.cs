using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests
{
    public class TestRotateAndLook : TestBase
    {
        [UnityTest]
        public IEnumerator TestRotateRight()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            action.Clear();

            action["action"] = "RotateRight";
            yield return step(action);

            action.Clear();

            Assert.AreEqual((int)agent.gameObject.transform.rotation.eulerAngles.y, 90);
        }

        [UnityTest]
        public IEnumerator TestLookUpDownDefault()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 330);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 30 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 60);

            yield return step(action);
            Assert.AreEqual(error, "can't look down beyond 60 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }

        [UnityTest]
        public IEnumerator TestLookUpDownDefaultLocobot()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "locobot";
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 330);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 30 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 30);

            yield return step(action);
            Assert.AreEqual(error, "can't look down beyond 30 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }

        [UnityTest]
        public IEnumerator TestLookUpDownDefaultStretch()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "stretch";
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            action["degrees"] = 55.0f;
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.eulerAngles.x, 335);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 25 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            action["degrees"] = 25.0f;
            yield return step(action);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.eulerAngles.x, 90);

            yield return step(action);
            Assert.AreEqual(error, "can't look down beyond 90 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }

        [UnityTest]
        public IEnumerator TestSetLookUpDownLimits()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["maxUpwardLookAngle"] = 90f;
            action["maxDownwardLookAngle"] = 90f;
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 270);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 90 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 90);

            //lookdown again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look down beyond 90 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }

        [UnityTest]
        public IEnumerator TestSetLookUpDownLimitsLocoBot()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "locobot";
            action["maxUpwardLookAngle"] = 90f;
            action["maxDownwardLookAngle"] = 90f;
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 270);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 90 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 90);

            //lookdown again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look down beyond 90 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }

        [UnityTest]
        public IEnumerator TestSetLookUpDownLimitsArm()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "arm";
            action["maxUpwardLookAngle"] = 90f;
            action["maxDownwardLookAngle"] = 90f;
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 270);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 90 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)agentCamera.transform.localRotation.eulerAngles.x, 90);

            //lookdown again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look down beyond 90 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }

        [UnityTest]
        public IEnumerator TestSetLookUpDownLimitsStretch()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["agentMode"] = "stretch";
            action["maxUpwardLookAngle"] = 90f;
            action["maxDownwardLookAngle"] = 90f;
            yield return step(action);

            BaseFPSAgentController agent = agentManager.PrimaryAgent;
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            Camera agentCamera = agent.m_Camera;

            action.Clear();

            action["action"] = "LookUp";
            action["degrees"] = 40f;
            yield return step(action);
            yield return step(action);
            yield return step(action);

            Assert.AreEqual((int)Mathf.Round(agentCamera.transform.eulerAngles.x), 270);

            //lookup again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look up beyond 90 degrees above the forward horizon");
            Assert.AreEqual(actionReturn, null);

            action.Clear();
            action["action"] = "LookDown";
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);
            yield return step(action);

            // Debug.Log("LookDown 6. RAW: " + agentCamera.transform.eulerAngles.x
            // + " vs. INT: " + (int)agentCamera.transform.eulerAngles.x
            // + " vs ROUNDED INT: " + (int)Mathf.Round(agentCamera.transform.eulerAngles.x));

            Assert.AreEqual((int)Mathf.Round(agentCamera.transform.eulerAngles.x), 90);

            //lookdown again and hit limit
            yield return step(action);

            Assert.AreEqual(error, "can't look down beyond 90 degrees below the forward horizon");
            Assert.AreEqual(actionReturn, null);
        }
    }
}
