using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestGetShortestPath : TestBase {
        [UnityTest]
        public IEnumerator TestShortestPath() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "GetShortestPath";
            action["objectId"] = "Kettle|+01.04|+00.90|-02.60";
            yield return step(action);

            UnityEngine.AI.NavMeshPath path = (UnityEngine.AI.NavMeshPath)actionReturn;

            bool result = false;

            //corner 1
            result = Mathf.Approximately(-1.0f, path.corners[0].x);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(0.00500001f, path.corners[0].y);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(1.0f, path.corners[0].z);
            Assert.AreEqual(result, true);

            //corner 2
            result = Mathf.Approximately(-0.8100001f, path.corners[1].x);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(0.00500001f, path.corners[1].y);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(-1.14f, path.corners[1].z);
            Assert.AreEqual(result, true);

            //corner 3
            result = Mathf.Approximately(-0.66f, path.corners[2].x);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(0.00500001f, path.corners[2].y);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(-1.26f, path.corners[2].z);
            Assert.AreEqual(result, true);

        }
    }
}