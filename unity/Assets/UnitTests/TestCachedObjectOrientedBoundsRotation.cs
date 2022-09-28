using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestCachedObjectOrientedBoundsRotation : TestBase
    {

        //check to make sure the automatic caching of the object oriented bounds is assigning
        //the localRotation of the BoundingBox child object of any given SimObject is set to
        //(0,0,0) or Quaternion.identity, correctly
        [UnityTest]
        public IEnumerator TestCachedBoundsRotation() {
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            yield return step(action);

            GameObject book = GameObject.Find("Book_e5ef3174");

            bool result = false;
            GameObject boundingBox = book.transform.Find("BoundingBox").gameObject;

            result = Mathf.Approximately(boundingBox.transform.localRotation.x, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(boundingBox.transform.localRotation.y, 0.0f);
            Assert.AreEqual(result, true);

            result = Mathf.Approximately(boundingBox.transform.localRotation.z, 0.0f);
            Assert.AreEqual(result, true);
        }
    }
}


