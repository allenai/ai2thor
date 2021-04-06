using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests
{
    public class TestRotate : TestBase
    {
        [UnityTest]
        public IEnumerator TestRotateRight()
        {
            yield return ExecuteDebugAction("init");
            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();
            agent.gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);
            yield return ExecuteDebugAction("rr");
            Assert.AreEqual((int)agent.gameObject.transform.rotation.eulerAngles.y, 90);
        }
    }
}
