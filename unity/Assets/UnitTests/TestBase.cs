
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests
{
    public class TestBase
    {

        public IEnumerator ExecuteDebugAction(string action) {
            var debugInputField = GameObject.FindObjectOfType<DebugInputField>();
            yield return debugInputField.ExecuteBatch(new List<string>{action});
        }

        [SetUp]
        public virtual void Setup(){
            UnityEngine.SceneManagement.SceneManager.LoadScene ("FloorPlan1_physics");
        }

    }
}
