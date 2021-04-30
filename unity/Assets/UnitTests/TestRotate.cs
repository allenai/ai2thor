using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestRotate : TestBase {
        [UnityTest]
        public IEnumerator TestRotateRight() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);

            action.Clear();

            action["action"] = "RotateRight";
            yield return step(action);

            action.Clear();

            Assert.AreEqual((int)agent.gameObject.transform.rotation.eulerAngles.y, 90);
        }
    }
}
