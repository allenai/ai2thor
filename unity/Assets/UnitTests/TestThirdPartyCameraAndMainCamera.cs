using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TestThirdPartyCameraAndMainCamera : TestBase
    {
        [UnityTest]
        public IEnumerator TestAddThirdPartyCamera()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = new Vector3(0, 2, 1);
            action["rotation"] = new Vector3(15, 20, 89);
            action["parent"] = "world";
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
            action["parent"] = "agent";
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["agentPositionRelativeCoordinates"] = true;
            yield return step(action);

            Assert.NotNull(GameObject.Find("ThirdPartyCamera1"));

            //assert it is a child of the agent and its local position and rotation is as expected
            var agentThirdPartyCam = GameObject.Find("ThirdPartyCamera1");
            Assert.That(agentThirdPartyCam.transform.parent == getActiveAgent().transform);
        }

        [UnityTest]
        public IEnumerator TestAddMultipleThirdPartyCamera()
        {
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
        public IEnumerator TestUpdateThirdPartyCamera()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = Vector3.zero;
            action["rotation"] = Vector3.zero;
            action["parent"] = "world";
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            yield return step(action);

            action.Clear();

            //update third party camera to be attached to agent
            action["action"] = "UpdateThirdPartyCamera";
            action["thirdPartyCameraId"] = 0;
            action["position"] = new Vector3(1, 2, 3);
            action["rotation"] = new Vector3(20, 20, 20);
            action["parent"] = "agent";
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["agentPositionRelativeCoordinates"] = true;
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
            action["thirdPartyCameraId"] = 0;
            action["position"] = new Vector3(10, 10, 10);
            action["rotation"] = new Vector3(1f, 1f, 1f);
            action["parent"] = "world";
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["agentPositionRelativeCoordinates"] = false;
            action["parent"] = "agent";
            yield return step(action);

            //ok now also make sure the position and rotation updated now that we are attached to the primary agent
            result = false;
            //check position set as expected
            Debug.Log("x pos");
            result = Mathf.Approximately(agentThirdPartyCam.transform.position.x, 10.0f);
            Assert.AreEqual(result, true);
            Debug.Log("y pos");
            result = Mathf.Approximately(agentThirdPartyCam.transform.position.y, 10.0f);
            Assert.AreEqual(result, true);
            Debug.Log("z pos");
            result = Mathf.Approximately(agentThirdPartyCam.transform.position.z, 10.0f);
            Assert.AreEqual(result, true);

            Debug.Log(
                $"ok what even are the eulers: {agentThirdPartyCam.transform.eulerAngles.x}, {agentThirdPartyCam.transform.eulerAngles.y}, {agentThirdPartyCam.transform.eulerAngles.z}"
            );
            //check rotation set as expected
            Debug.Log("x rot");
            result = Mathf.Approximately(agentThirdPartyCam.transform.eulerAngles.x, 1.0f);
            Assert.AreEqual(result, true);
            Debug.Log("y rot");

            result = Mathf.Approximately(agentThirdPartyCam.transform.eulerAngles.y, 0.9999983f);
            Assert.AreEqual(result, true);
            Debug.Log("z rot");

            result = Mathf.Approximately(agentThirdPartyCam.transform.eulerAngles.z, 1.0f);
            Assert.AreEqual(result, true);
        }

        [UnityTest]
        public IEnumerator TestGetVisibleObjectsFromCamera()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = new Vector3(-0.67f, 1.315f, 0.46f);
            action["rotation"] = new Vector3(0, 180, 0);
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            action["parent"] = "world";
            action["agentPositionRelativeCoordinates"] = false;
            yield return step(action);

            action.Clear();

            //testing third party camera return
            action["action"] = "GetVisibleObjectsFromCamera";
            action["thirdPartyCameraId"] = 0;
            yield return step(action);

            List<string> visibleObjects = (List<string>)actionReturn;
#if UNITY_EDITOR
            foreach (string obj in visibleObjects)
            {
                Debug.Log(obj);
            }
#endif

            //check for expected object at first few elements
            //also check for total count of visible objects to be the expected amount
            Assert.AreEqual(visibleObjects.Count, 16);
            Assert.AreEqual(visibleObjects[0], "Apple|-00.47|+01.15|+00.48");

            //test with objectId filter now
            action.Clear();

            action["action"] = "GetVisibleObjectsFromCamera";
            action["thirdPartyCameraId"] = 0;
            action["filterObjectIds"] = new List<string> { "Apple|-00.47|+01.15|+00.48" };
            yield return step(action);

            visibleObjects.Clear();
            visibleObjects = (List<string>)actionReturn;

#if UNITY_EDITOR
            Debug.Log($"Checking Visible Objects from ThirdPartyCamera Index 0");
            Debug.Log($"Total Visible Objects: {visibleObjects.Count}");
            foreach (string obj in visibleObjects)
            {
                Debug.Log(obj);
            }
#endif

            Assert.AreEqual(visibleObjects.Count, 1);
            Assert.AreEqual(visibleObjects[0], "Apple|-00.47|+01.15|+00.48");

            //Test main camera return
            action.Clear();

            action["action"] = "GetVisibleObjectsFromCamera";
            action["thirdPartyCameraId"] = null; //null ID queries main camera instead
            yield return step(action);

            visibleObjects.Clear();
            visibleObjects = (List<string>)actionReturn;

#if UNITY_EDITOR
            Debug.Log($"Checking Visible Objects from Main Camera");
            Debug.Log($"Total Visible Objects: {visibleObjects.Count}");
            foreach (string obj in visibleObjects)
            {
                Debug.Log(obj);
            }
#endif

            Assert.AreEqual(visibleObjects.Count, 4);
            Assert.AreEqual(visibleObjects[0], "Cabinet|-01.85|+02.02|+00.38");
        }

        [UnityTest]
        public IEnumerator TestUpdateMainCamera()
        {
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
