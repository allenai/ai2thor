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
    public class TestLightManager : TestBase {

        [SetUp]
        public override void Setup() {
            UnityEngine.SceneManagement.SceneManager.LoadScene("FloorPlan1_physics");
        }

        [UnityTest]
        public IEnumerator TestGetLightsOutput() {
            Debug.Log("what is the current scene? " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            action.Clear();

            action["action"] = "GetLights";
            yield return step(action);

            List<LightParameters> listOfLP = (List<LightParameters>) actionReturn;
            bool result = false;
            foreach (LightParameters lp in listOfLP) {
                if(lp.id == "scene|Point|0") {

                    result = Mathf.Approximately(lp.position.x, 0.852f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.position.y, 1.091f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.position.z, -0.891f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rotation.axis.x, 1.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rotation.axis.y, 0.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rotation.axis.z, 0.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.spotAngle, 0.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.intensity, 0.66f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.range, 3.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rgb.r, 0.75f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rgb.g, 0.9068965f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rgb.b, 1.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.rgb.a, 1.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately(lp.indirectMultiplier, 1.0f);
                    Assert.AreEqual (result, true);

                    string [] nu = new string[] {};
                    Assert.AreEqual (lp.cullingMaskOff, nu);

                    Assert.AreEqual (lp.enabled, true);
                    Assert.AreEqual (lp.controllerSimObjIds, null);
                    Assert.AreEqual (lp.parentSimObjObjectId, null);
                    
                    Assert.AreEqual (lp.shadow.type, "None");

                    Assert.AreEqual (lp.shadow.resolution, "FromQualitySettings");

                    result = Mathf.Approximately (lp.shadow.strength, 1.0f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately (lp.shadow.normalBias, 0.4f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately (lp.shadow.bias, 0.05f);
                    Assert.AreEqual (result, true);

                    result = Mathf.Approximately (lp.shadow.nearPlane, 0.2f);
                    Assert.AreEqual (result, true);
                }
            }
        } 

        [UnityTest]
        public IEnumerator TestSetLights() {
            Debug.Log("what is the current scene? " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            yield return step(action);

            action.Clear();
            
            action["action"] = "SetLights";

            string path = Application.dataPath + "/DebugTextFiles/exportedLightParams_FloorPlan1_TestSet.json";

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
                if (lp.id == "scene|Spot|0") {

                    //check type changed to point
                    var lightParamType = (LightType)Enum.Parse(typeof(LightType), lp.type, ignoreCase: true);
                    if(lightParamType == LightType.Point) {
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

                    result = Mathf.Approximately(lp.rotation.degrees, 45f);
                    Assert.AreEqual(result, true);

                    //intensity
                    result = Mathf.Approximately(lp.intensity, 0.0f);
                    Assert.AreEqual(result, true);

                    //range
                    result = Mathf.Approximately(lp.range, 10.0f);
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

                    string[] nu = new string[] { };
                    Assert.AreEqual(lp.cullingMaskOff, nu);

                    Assert.AreEqual(lp.enabled, false);
                    Debug.Log($"size of controllerSimObjIds array: {lp.controllerSimObjIds.Length}");
                    Debug.Log($"{lp.controllerSimObjIds[0]}");
                    //      "LightSwitch|+02.33|+01.31|-00.16",
                    //Assert.AreEqual(lp.controllerSimObjIds, "LightSwitch|+02.33|+01.31|-00.16");
                    Assert.AreEqual(lp.parentSimObjObjectId, null);

                    result = Mathf.Approximately(lp.indirectMultiplier, 10.0f);
                    Assert.AreEqual(result, true);

                    Assert.AreEqual(lp.shadow.type, "None");

                    Assert.AreEqual(lp.shadow.resolution, "FromQualitySettings");

                    result = Mathf.Approximately(lp.shadow.strength, 1.0f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.normalBias, 0.4f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.bias, 0.05f);
                    Assert.AreEqual(result, true);

                    result = Mathf.Approximately(lp.shadow.nearPlane, 0.2f);
                    Assert.AreEqual(result, true);
                }
            }
            //check for scene|Spot|0 changes
            //position//
            //rotation
            //degrees
            //enabled
            //spot angle
            //range
            //rgb
            //indirectMultiplier
            //shadow type -None, Hard, Soft
            //controllerSimObjIds - check that both "LightSwitch|+02.33|+01.31|-00.16" and "CoffeeMachine|-01.98|+00.90|-00.19" 


            //check for scene|Point|0 changes
            //parentsimObjObjectId to "Lettuce|-01.81|+00.97|-00.94"
            //change to spotlight, change spot angle to 10

            //change scene|Point|1 to directional light

            //now to see if light properties were set according to exportedLightParams_FloorPlan1_TestSet.json
        } 

    }
}
