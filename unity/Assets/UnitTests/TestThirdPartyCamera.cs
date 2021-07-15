using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests {
    public class TestThirdPartyCamera : TestBase {
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
        public IEnumerator TestCoordinateFromRaycastThirdPartyCamera() {
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

            //test raycast when hitting something in scene
            action["action"] = "CoordinateFromRaycastThirdPartyCamera";
            action["x"] = 0.5f;
            action["y"] = 0.5f;
            action["thirdPartyCameraId"] = 0;
            yield return step(action);

            bool result = false;
            Vector3 coord = (Vector3)actionReturn;

            result = Mathf.Approximately(coord.x, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(coord.y, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(coord.z, 2.472f);
            Assert.AreEqual(result, true);

            action.Clear();

            //test raycast when nothing hit
            action["action"] = "CoordinateFromRaycastThirdPartyCamera";
            action["x"] = 0f;
            action["y"] = 0f;
            action["thirdPartyCameraId"] = 0;
            yield return step(action);

            result = false;

            coord = (Vector3)actionReturn;

            result = Mathf.Approximately(coord.x, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(coord.y, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(coord.z, 0.0f);
            Assert.AreEqual(result, true);
        }
    }
}
