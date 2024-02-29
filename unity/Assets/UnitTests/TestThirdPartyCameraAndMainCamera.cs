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
            action["position"] = Vector3.zero;
            action["rotation"] = Vector3.zero;
            action["orthographic"] = true;
            action["orthographicSize"] = 5;
            yield return step(action);

            Assert.NotNull(GameObject.Find("ThirdPartyCamera0"));
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
