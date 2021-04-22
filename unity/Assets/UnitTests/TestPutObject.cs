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
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return ExecuteAction(action);
            
            action.Clear();

            action["action"] = "RotateRight";
            yield return ExecuteAction(action);
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "LookDown";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "MoveRight";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PickupObject";
            action["objectId"] = "CreditCard|-00.46|+01.10|+00.87";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PutObject";
            action["x"] = 0.5f;
            action["y"] = 0.5f;
            action["putNearXY"] = true;
            yield return ExecuteAction(action);

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
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "RotateRight";
            yield return ExecuteAction(action);
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "LookDown";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "MoveRight";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PickupObject";
            action["objectId"] = "CreditCard|-00.46|+01.10|+00.87";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PutObject";
            action["x"] = 0.5f;
            action["y"] = 0.5f;
            action["putNearXY"] = false;
            yield return ExecuteAction(action);

            GameObject creditCard = GameObject.Find("CreditCard_acee2f3e");

            bool result = false;
            result = Mathf.Approximately(creditCard.transform.position.x, -0.4736856f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(creditCard.transform.position.y, 1.105045f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(creditCard.transform.position.z, 0.7573217f);
            Assert.AreEqual(result, true);

        }

        // this should fail if user calls PlaceHeldObject directly and tries to pass in a z value
        // PlaceHeldObject is undocumented and the normal call is PutObject, which is a wrapper around PlaceHeldObject
        // note: if a user passes in both 'z' and 'maxDistance' then they may get a silent, unintended behavior.
        // we should decide how to handle extraneous parameters in the future
        [UnityTest]
        public IEnumerator PlaceHeldObject_Deprecated_Z_objectId()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "RotateRight";
            yield return ExecuteAction(action);
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "LookDown";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "MoveRight";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PickupObject";
            action["objectId"] = "CreditCard|-00.46|+01.10|+00.87";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PlaceHeldObject";
            action["objectId"] = "CounterTop|-00.08|+01.15|00.00";

            // this should cause the exception
            action["z"] = 5.0f;
            yield return ExecuteAction(action);

            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();
            Assert.AreEqual(agent.lastActionSuccess, false);
        }

        [UnityTest]
        public IEnumerator PlaceHeldObject_Deprecated_Z_XY()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "RotateRight";
            yield return ExecuteAction(action);
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "LookDown";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "MoveRight";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PickupObject";
            action["objectId"] = "CreditCard|-00.46|+01.10|+00.87";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PlaceHeldObject";
            action["x"] = 0.5f;
            action["y"] = 0.5f;

            // this should cause the exception
            action["z"] = 5.0f;
            yield return ExecuteAction(action);

            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();
            Assert.AreEqual(agent.lastActionSuccess, false);
        }

        [UnityTest]
        public IEnumerator PlaceHeldObject_Deprecated_Z_XY_PutNearXY_True()
        {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "RotateRight";
            yield return ExecuteAction(action);
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "LookDown";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "MoveRight";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PickupObject";
            action["objectId"] = "CreditCard|-00.46|+01.10|+00.87";
            yield return ExecuteAction(action);

            action.Clear();

            action["action"] = "PlaceHeldObject";
            action["x"] = 0.5f;
            action["y"] = 0.5f;
            action["putNearXY"] = true;

            // this should cause the exception
            action["z"] = 5.0f;
            yield return ExecuteAction(action);

            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();
            Assert.AreEqual(agent.lastActionSuccess, false);
        }
    }
}