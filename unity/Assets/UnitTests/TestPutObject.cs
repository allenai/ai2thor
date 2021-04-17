using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests
{
    public class TestPutObject : TestBase
    {
        [UnityTest]
        public IEnumerator TestPutObject_PutNearXY_True()
        {
            yield return ExecuteDebugAction("init");
            yield return ExecuteDebugAction("rr");
            yield return ExecuteDebugAction("rr");
            yield return ExecuteDebugAction("ld");
            yield return ExecuteDebugAction("mr");

            yield return ExecuteDebugAction("pu CreditCard|-00.46|+01.10|+00.87");
            yield return ExecuteDebugAction("putxy true");
            GameObject creditCard = GameObject.Find("CreditCard_acee2f3e");

            bool result = false;
            result = Mathf.Approximately(creditCard.transform.position.x, -0.1972949f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(creditCard.transform.position.y, 1.105045f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(creditCard.transform.position.z, 0.7573217f);
            Assert.AreEqual(result, true);
        }

        [UnityTest]
        public IEnumerator TestPutObject_PutNearXY_False()
        {
            yield return ExecuteDebugAction("init");
            yield return ExecuteDebugAction("rr");
            yield return ExecuteDebugAction("rr");
            yield return ExecuteDebugAction("ld");
            yield return ExecuteDebugAction("mr");

            yield return ExecuteDebugAction("pu CreditCard|-00.46|+01.10|+00.87");
            yield return ExecuteDebugAction("putxy false");
            GameObject creditCard = GameObject.Find("CreditCard_acee2f3e");

            bool result = false;
            result = Mathf.Approximately(creditCard.transform.position.x, -0.4736856f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(creditCard.transform.position.y, 1.105045f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(creditCard.transform.position.z, 0.7573217f);
            Assert.AreEqual(result, true);

        }
    }
}