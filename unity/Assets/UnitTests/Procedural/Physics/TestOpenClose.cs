using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;

namespace Tests {
    public class TestOpenClose : TestBase {
        [SetUp]
        public override void Setup() {
            UnityEngine.SceneManagement.SceneManager.LoadScene("FloorPlan1_physics");
        }

        [UnityTest]
        public IEnumerator TestOpening() {

            yield return step(new Dictionary<string, object>() {
                    { "action", "Initialize"},
                    { "agentMode", "arm"},
                    { "agentControllerType", "mid-level"},
                    { "renderInstanceSegmentation", true}
                });
            yield return new WaitForSeconds(2f);

            yield return step(new Dictionary<string, object>() {
                    { "action", "MoveAgent"},
                    { "right", -0.7f},
                    { "disableRendering", true},
                    { "returnToStart", true}
                });

            //yield return new WaitForSeconds(2f);
            yield return step(new Dictionary<string, object>() {
                    { "action", "MoveArm"},
                    { "coordinateSpace", "world"},
                    { "disableRendering", true},
                    { "position", new Vector3(-1.3149f, 0.6049995f, 0.04580003f)}
                });

            //yield return new WaitForSeconds(2f);
            yield return step(new Dictionary<string, object>() {
                    { "action", "LookDown"},
                    { "degrees", 30f} 
                });

            //yield return new WaitForSeconds(2f);
            yield return step(new Dictionary<string, object>() {
                    { "action", "OpenObject"},
                    { "objectId", "Cabinet|-01.55|+00.50|+00.38"},
                    { "useGripper", true},
                    { "returnToStart", true},
                    { "stopAtNonStaticCol", false}
                });
            yield return new WaitForSeconds(2f);

            Transform testCabinetDoor = GameObject.Find("Cabinet_67e9cbea").transform.Find("CabinetDoor");
            bool testCabinetDoorOpened = Mathf.Approximately((testCabinetDoor.localEulerAngles.y + 360) % 360, 270);
            Assert.AreEqual(testCabinetDoorOpened, true);

            yield return true;
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