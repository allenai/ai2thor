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

    }
}
