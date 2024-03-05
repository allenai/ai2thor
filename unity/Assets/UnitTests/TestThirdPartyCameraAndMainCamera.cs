using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class TestThirdPartyCameraAndMainCamera : TestBase {
        [UnityTest]
        public IEnumerator TestAddThirdPartyCamera() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = new Vector3(0, 2, 1);
            action["rotation"] = new Vector3(15, 20, 89);
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            yield return step(action);

            Assert.NotNull(GameObject.Find("ThirdPartyCamera0"));
            //assert camera position is in world space as expected
            GameObject camera = GameObject.Find("ThirdPartyCamera0");

            bool result = false;
            //check position set as expected
            result = Mathf.Approximately(camera.transform.position.x, 0.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.position.y, 2.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.position.z, 1.0f);
            Assert.AreEqual(result, true);

            //check rotation set as expected
            result = Mathf.Approximately(camera.transform.eulerAngles.x, 15f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.eulerAngles.y, 20f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.eulerAngles.z, 89f);
            Assert.AreEqual(result, true);

            //now add a third party camera to the agent
            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = new Vector3(0, 2, 1);
            action["rotation"] = new Vector3(15, 20, 89);
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["attachToAgent"] = true;
            yield return step(action);

            Assert.NotNull(GameObject.Find("ThirdPartyCamera1"));

            //assert it is a child of the agent and its local position and rotation is as expected
            var agentThirdPartyCam = GameObject.Find("ThirdPartyCamera1");
            Assert.That(agentThirdPartyCam.transform.parent == getActiveAgent().transform);
        }

        [UnityTest]
        public IEnumerator TestAddMultipleThirdPartyCamera() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = Vector3.zero;
            action["rotation"] = Vector3.zero;
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            yield return step(action);
            yield return step(action);

            Assert.NotNull(GameObject.Find("ThirdPartyCamera0"));
            Assert.NotNull(GameObject.Find("ThirdPartyCamera1"));
        }

        [UnityTest]
        public IEnumerator TestUpdateThirdPartyCamera() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = Vector3.zero;
            action["rotation"] = Vector3.zero;
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            yield return step(action);

            action.Clear();

            //update third party camera to be attached to agent
            action["action"] = "UpdateThirdPartyCamera";
            action["thirdPartyCameraId"]=0;
            action["position"] = new Vector3(1, 2, 3);
            action["rotation"] = new Vector3(20, 20, 20);
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["agentPositionRelativeCoordinates"]=true;
            yield return step(action);

            //make sure camera is now a child of the primary agent
            var agentThirdPartyCam = GameObject.Find("ThirdPartyCamera0");
            Assert.That(agentThirdPartyCam.transform.parent == getActiveAgent().transform);

            //ok now also make sure the position and rotation updated now that we are attached to the primary agent
            bool result = false;
            //check position set as expected
            result = Mathf.Approximately(agentThirdPartyCam.transform.localPosition.x, 1.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.localPosition.y, 2.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.localPosition.z, 3.0f);
            Assert.AreEqual(result, true);

            //check rotation set as expected
            result = Mathf.Approximately(agentThirdPartyCam.transform.localEulerAngles.x, 20f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.localEulerAngles.y, 20f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.localEulerAngles.z, 20f);
            Assert.AreEqual(result, true);

            action.Clear();
            //ok now update camera so it detaches from the agent and also repositions in world space
            action["action"] = "UpdateThirdPartyCamera";
            action["thirdPartyCameraId"]=0;
            action["position"] = new Vector3(10, 10, 10);
            action["rotation"] = new Vector3(1, 1, 1);
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["agentPositionRelativeCoordinates"]=false;
            yield return step(action);

            //ok now also make sure the position and rotation updated now that we are attached to the primary agent
            result = false;
            //check position set as expected
            result = Mathf.Approximately(agentThirdPartyCam.transform.position.x, 10.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.position.y, 10.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.position.z, 10.0f);
            Assert.AreEqual(result, true);

            //check rotation set as expected
            result = Mathf.Approximately(agentThirdPartyCam.transform.eulerAngles.x, 1.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.eulerAngles.y, 1.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(agentThirdPartyCam.transform.eulerAngles.z, 1.0f);
            Assert.AreEqual(result, true);
        }

        [UnityTest]
        public IEnumerator TestUpdateMainCamera() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "UpdateMainCamera";
            action["position"] = Vector3.zero;
            action["rotation"] = Vector3.zero;
            yield return step(action);

            GameObject camera = GameObject.Find("FirstPersonCharacter");
            bool result = false;
            //check position changed as expected
            result = Mathf.Approximately(camera.transform.localPosition.x, 0.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.localPosition.y, 0.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.localPosition.z, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(camera.transform.localEulerAngles.x, 0.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.localEulerAngles.y, 0.0f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(camera.transform.localEulerAngles.z, 0.0f);
            Assert.AreEqual(result, true);
        }
    }
}
