using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using Thor.Procedural.Data;
using Newtonsoft.Json.Linq;

namespace Tests {
    public class TestLightManagerProcedural : TestBase {
        [SetUp]
        public override void Setup() {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Procedural");
        }

        [UnityTest]
        public IEnumerator TestGetLightsProceduralOutput() {
            Debug.Log("what is the current scene? " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "CreateHouse";
            string path = Application.dataPath + "/Resources/rooms/lighthouse_test.json";
            var jsonStr = System.IO.File.ReadAllText(path);
            JObject obj = JObject.Parse(jsonStr);
            action["house"] = obj;
            yield return step(action);

            action.Clear();

            action["action"] = "GetLights";
            yield return step(action);

            List<LightParameters> listOfLP = (List<LightParameters>)actionReturn;
            bool result = false;
            foreach (LightParameters lp in listOfLP) {
                if (lp.id == "scene|Point|0") {

                    result = Mathf.Approximately(lp.position.x, 12.0349874f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.position.y, 2.41352987f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.position.z, 2.78932643f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rotation.axis.x, 1.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rotation.axis.y, 0.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rotation.axis.z, 0.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.spotAngle, 0.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.intensity, 0.45f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.range, 15.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.r, 1.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.g, 0.855f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.b, 0.722f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.a, 1.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.indirectMultiplier, 0.0f);
                    Assert.AreEqual(result, true);

                    string[] nu = new string[] { "Procedural0", "Procedural1", "Procedural3" };
                    Assert.AreEqual(lp.cullingMaskOff, nu);

                    Assert.AreEqual(lp.enabled, false);
                    string[] ora = new string[] { "LightSwitch|0|1" };
                    Assert.AreEqual(lp.controllerSimObjIds, ora);
                    Assert.AreEqual(lp.parentSimObjObjectId, null);

                    Assert.AreEqual(lp.shadow.type, "Soft");

                    Assert.AreEqual(lp.shadow.resolution, "FromQualitySettings");

                    result = Mathf.Approximately(lp.shadow.strength, 1.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.normalBias, 0.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.bias, 0.05f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.nearPlane, 0.2f);
                    Assert.AreEqual(result, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator TestSetLightsProcedural() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            yield return step(action);

            action.Clear();

            action["action"] = "SetLights";

            //now to see if light properties were set according to exportedLightParams_FloorPlan1_TestSet.json
            string path = Application.dataPath + "/DebugTextFiles/exportedLightParams_Procedural_TestSet_lighthouse_test.json";

            var jsonStr = System.IO.File.ReadAllText(path);
            Debug.Log($"json: being read {jsonStr}");

            JArray obj = JArray.Parse(jsonStr);
            action["lightParams"] = obj;

            yield return step(action);

            action.Clear();

            //get light properties to make sure everything changed as expected
            action["action"] = "GetLights";
            yield return step(action);

            List<LightParameters> listOfLP = (List<LightParameters>)actionReturn;
            bool result = false;

            foreach (LightParameters lp in listOfLP) {
                if (lp.id == "scene|Directional|0") {

                    //check type changed to point
                    var lightParamType = (LightType)Enum.Parse(typeof(LightType), lp.type, ignoreCase: true);
                    if (lightParamType == LightType.Spot) {
                        result = true;
                    }
                    Assert.AreEqual(result, true);

                    //position
                    result = Mathf.Approximately(lp.position.x, 10.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.position.y, 10.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.position.z, 10.0f);
                    Assert.AreEqual(result, true);

                    //rotation
                    result = Mathf.Approximately(lp.rotation.axis.x, 0.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rotation.axis.y, 1.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rotation.axis.z, 0.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rotation.degrees, 10f);
                    Assert.AreEqual(result, true);

                    //intensity
                    result = Mathf.Approximately(lp.intensity, 10.0f);
                    Assert.AreEqual(result, true);

                    //range
                    result = Mathf.Approximately(lp.range, 15.0f);
                    Assert.AreEqual(result, true);

                    //color
                    result = Mathf.Approximately(lp.rgb.r, 0.1f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.g, 0.1f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.b, 0.1f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.rgb.a, 0.5f);
                    Assert.AreEqual(result, true);

                    Assert.AreEqual(lp.enabled, false);
                    Assert.AreEqual(lp.controllerSimObjIds[0], "LightSwitch|0|1");
                    Assert.AreEqual(lp.parentSimObjObjectId, "Bowl|surface|6|65");

                    result = Mathf.Approximately(lp.indirectMultiplier, 10.0f);

                    Assert.AreEqual(lp.shadow.type, "Hard");

                    Assert.AreEqual(lp.shadow.resolution, "FromQualitySettings");

                    result = Mathf.Approximately(lp.shadow.strength, 2.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.normalBias, 0.4f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.bias, 0.05f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.nearPlane, 0.1f);
                    Assert.AreEqual(result, true);
                }

                if (lp.id == "scene|Directional|0") {

                    //check type changed to point
                    var lightParamType = (LightType)Enum.Parse(typeof(LightType), lp.type, ignoreCase: true);
                    if (lightParamType == LightType.Point) {
                        result = true;
                    }

                    Assert.AreEqual(result, true);
                    Assert.AreEqual(lp.controllerSimObjIds[0], "DeskLamp|surface|5|46");
                    Assert.AreEqual(lp.enabled, true);
                }

                if (lp.id == "scene|Point|1") {
                    Assert.AreEqual(lp.controllerSimObjIds[0], "LightSwitch|0|1");
                    Assert.AreEqual(lp.controllerSimObjIds[1], "DeskLamp|surface|5|46");
                }
            }
        }
    }
}
