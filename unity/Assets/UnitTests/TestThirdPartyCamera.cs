using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

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
        public IEnumerator TestObjectsFromThirdPartyCamera() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "AddThirdPartyCamera";
            action["position"] = new Vector3(0.0f, 1.3f, -0.3f);
            action["rotation"] = Vector3.zero;
            action["orthographic"] = false;
            yield return step(action);

            action.Clear();

            action["action"] = "ObjectsFromThirdPartyCamera";
            action["thirdPartyCameraIndex"] = 0;
            yield return step(action);

            bool result = true;
            List<String> visibleObjectNames = new List<String>() {
                "Apple_34d5f204", "Book_3d15d052", "Bread_a13c4e42", "CounterTop_bafd4140", "CreditCard_acee2f3e"
            };

            foreach (KeyValuePair<SimObjPhysics, VisibilityCheck> kvp in (Dictionary<SimObjPhysics, VisibilityCheck>)actionReturn) {
                Debug.Log(kvp.Key.name);
                if(!visibleObjectNames.Contains(kvp.Key.name)) {
                    result = false;
                    break;
                }

                if(!kvp.Value.visible || !kvp.Value.interactable) {
                    result = false;
                    break;
                }
            }

            Assert.AreEqual(result, true);
        }
    }
}
