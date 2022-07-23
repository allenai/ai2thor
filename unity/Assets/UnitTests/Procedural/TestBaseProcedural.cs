
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Tests {
    public class TestBaseProcedural : TestBase 
    {
        public override IEnumerator Initialize() {
            Dictionary<string, object> action = new Dictionary<string, object>() {
                { "gridSize", 0.25f},
                { "agentCount", 1},
                { "fieldOfView", 90f},
                { "snapToGrid", true},
                { "procedural", true},
                { "action", "Initialize"}
            };
            yield return step(action);
        }

        [SetUp]
        public override void Setup() {
            UnityEngine.Screen.SetResolution(600, 600, false);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => {
                var debugCanvas = GameObject.Find("DebugCanvasPhysics");
                if (debugCanvas != null) {
                    debugCanvas.SetActive(false);
                }
                UnityEngine.Screen.SetResolution(600, 600, false); 
            };
            UnityEngine.SceneManagement.SceneManager.LoadScene("Procedural");
        }

        protected string getTestResourcesPath(string filename = "", bool resourcesRelative = true) {
            var prefix = !resourcesRelative ? "Assets/Resources/" : "";
            var path = prefix + string.Join("/", NUnit.Framework.TestContext.CurrentContext.Test.FullName.Split('.'));
            return string.IsNullOrEmpty(filename) ? path : $"{path}/{filename}";
        }

    }
}
