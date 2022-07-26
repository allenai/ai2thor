
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
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
                { "action", "Initialize"}
            };
            yield return step(action);
        }

        [SetUp]
        public override void Setup() {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Procedural");
        }
    }
}
