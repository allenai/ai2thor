using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;

namespace Tests {
    public class TestOpenCloseS : TestBase {

        [SetUp]
        public override void Setup() {
            UnityEngine.SceneManagement.SceneManager.LoadScene("FloorPlan401_physics");
        }

        [UnityTest]
        public IEnumerator TestOpenCloseScale() {

            yield return step(new Dictionary<string, object>() {
                    { "action", "Initialize"},
                    { "agentMode", "arm"},
                    { "agentControllerType", "mid-level"},
                    { "renderInstanceSegmentation", true}
                });
            yield return new WaitForSeconds(2f);

            yield return step(new Dictionary<string, object>() {
                    { "action", "RotateRight"},
                    { "disableRendering", true},
                    { "returnToStart", true}
                });
            yield return new WaitForSeconds(2f);

            yield return step(new Dictionary<string, object>() {
                    { "action", "MoveAgent"},
                    { "ahead", 0.65f},
                    { "right", 1.05f},
                    { "disableRendering", true},
                    { "returnToStart", true}
                });

            //yield return new WaitForSeconds(2f);
            yield return step(new Dictionary<string, object>() {
                    { "action", "MoveArm"},
                    { "coordinateSpace", "world"},
                    { "disableRendering", true},
                    { "position", new Vector3(-1.1267f, 0.8986163f, -0.1051f)}
                });

            yield return new WaitForSeconds(2f);
            yield return step(new Dictionary<string, object>() {
                    { "action", "OpenObject"},
                    { "objectId", "ShowerCurtain|-00.79|+02.22|-00.24"},
                    { "useGripper", true},
                    { "returnToStart", true},
                    { "stopAtNonStaticCol", false}
                });
            yield return new WaitForSeconds(2f);

            Transform testShowerCurtain = GameObject.Find("ShowerCurtain_d39572d2").transform.Find("Curtain");
            bool testShowerCurtainOpened = Mathf.Approximately(testShowerCurtain.localScale.x, 5);
            Assert.AreEqual(testShowerCurtainOpened, true);

            yield return step(new Dictionary<string, object>() {
                    { "action", "CloseObject"},
                    { "objectId", "ShowerCurtain|-00.79|+02.22|-00.24"},
                    { "useGripper", true},
                    { "returnToStart", true},
                    { "stopAtNonStaticCol", false}
                });
            yield return new WaitForSeconds(2f);

            bool testShowerCurtainClosed = Mathf.Approximately(testShowerCurtain.localScale.x, 1);
            Assert.AreEqual(testShowerCurtainClosed, true);
        }

        protected string serializeObject(object obj) {
            var jsonResolver = new ShouldSerializeContractResolver();
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                obj,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings() {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    ContractResolver = jsonResolver
                }
            );
        }
    }
}