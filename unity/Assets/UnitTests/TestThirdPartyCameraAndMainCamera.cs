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

        //test main camera metadata
        [UnityTest]
        public IEnumerator TestMainCameraMetadataReturn() 
        {
            bool result = false;
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            MetadataWrapper metadata = getLastActionMetadata();

            result = Mathf.Approximately(metadata.worldRelativeCameraPosition.x, -1.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraPosition.y, 1.5759990000f);
            Assert.AreEqual(result, true);
            result= Mathf.Approximately(metadata.worldRelativeCameraPosition.z, 1.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraRotation.x, 0.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraRotation.y, 270.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraRotation.z, 0.0000000000f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(metadata.agentPositionRelativeCameraPosition.x, 0.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraPosition.y, 0.6750000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraPosition.z, 0.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraRotation.x, 0.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraRotation.y, 0.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraRotation.z, 0.0000000000f);
            Assert.AreEqual(result, true);

            action["action"] = "UpdateMainCamera";
            action["position"] = new Vector3(0.5f, 0.5f, 0.5f);
            action["rotation"] = new Vector3(30f, 10f, 12f);
            yield return step(action);

            metadata = getLastActionMetadata();

            result = Mathf.Approximately(metadata.worldRelativeCameraPosition.x, -1.5000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraPosition.y, 1.4009990000f);
            Assert.AreEqual(result, true);
            result= Mathf.Approximately(metadata.worldRelativeCameraPosition.z, 1.5000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraRotation.x, 30.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraRotation.y, 280.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.worldRelativeCameraRotation.z, 12.0000000000f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(metadata.agentPositionRelativeCameraPosition.x, 0.5000001000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraPosition.y, 0.4999999000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraPosition.z, 0.5000002000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraRotation.x, 30.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraRotation.y, 10.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.agentPositionRelativeCameraRotation.z, 12.0000000000f);
            Assert.AreEqual(result, true);
        }

        //test third party camera metadata
        [UnityTest]
        public IEnumerator TestThirdPartyCameraMetadataReturn() 
        {
            bool result = false;

            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = new Vector3(3, 2, 1);
            action["rotation"] = new Vector3(10, 20, 30);
            action["parent"] = "world";
            yield return step(action);

            MetadataWrapper metadata = getLastActionMetadata();

            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition.x, 3.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition.y, 2.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition.z, 1.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation.x, 10.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation.y, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation.z, 30.0000000000f);
            Assert.AreEqual(result, true);
            Assert.AreEqual(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraPosition, null);
            Assert.AreEqual(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraRotation, null);
            Assert.AreEqual(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraPosition, null);
            Assert.AreEqual(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraRotation, null);
            Assert.AreEqual(metadata.thirdPartyCameras[0].parentObjectName, "");

            action.Clear();

            //use update third party camera to change camera to be attached to agent
            action["action"] = "UpdateThirdPartyCamera";
            action["thirdPartyCameraId"] = 0;
            action["position"] = new Vector3(1, 2, 3);
            action["rotation"] = new Vector3(20, 20, 20);
            action["parent"] = "agent";
            action["agentPositionRelativeCoordinates"] = true;
            yield return step(action);

            metadata = getLastActionMetadata();

            // //world relative
            // Debug.Log($"world relative camera pos: {metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition:F10}");
            // Debug.Log($"world relative camera rot: {metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation:F10}");
            // //agent relative
            // Debug.Log($"agent relative camera pos: {metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraPosition:F10}");
            // Debug.Log($"agent relative camera rot: {metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraRotation:F10}");
            // //parent relative
            // Debug.Log($"parent relative camera rot: {metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraPosition:F10}");
            // Debug.Log($"parent relative camera rot: {metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraRotation:F10}");
            // Debug.Log($"parent object name: {metadata.thirdPartyCameras[0].parentObjectName}");

            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition.x, -4.0000010000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition.y, 2.9009990000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraPosition.z, 2.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation.x, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation.y, 290.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].worldRelativeThirdPartyCameraRotation.z, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraPosition.Value.x, 1.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraPosition.Value.y, 2.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraPosition.Value.z, 3.0000020000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraRotation.Value.x, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraRotation.Value.y, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].agentPositionRelativeThirdPartyCameraRotation.Value.z, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraPosition.Value.x, 1.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraPosition.Value.y, 2.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraPosition.Value.z, 3.0000020000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraRotation.Value.x, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraRotation.Value.y, 20.0000000000f);
            Assert.AreEqual(result, true);
            result = Mathf.Approximately(metadata.thirdPartyCameras[0].parentPositionRelativeThirdPartyCameraRotation.Value.z, 20.0000000000f);
            Assert.AreEqual(result, true);
            Assert.AreEqual(metadata.thirdPartyCameras[0].parentObjectName, "FPSController");
        }
    }
}
