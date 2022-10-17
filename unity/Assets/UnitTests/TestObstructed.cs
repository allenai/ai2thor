using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestObstructed : TestBase {

        [SetUp]
        public override void Setup() {
            UnityEngine.SceneManagement.SceneManager.LoadScene("FloorPlan402_physics");
        }

        [UnityTest]
        public IEnumerator TestBehindGlassThenOpenDoor() {
            Debug.Log("what is the current scene? " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            
            Dictionary<string, object> action = new Dictionary<string, object>();

            action["action"] = "Initialize";
            action["fieldOfView"] = 90f;
            action["snapToGrid"] = true;
            //action["scene"] = "FloorPlan402_physics";
            yield return step(action);

            action.Clear();

            //teleport to position
            action["action"] = "Teleport";
            action["position"] = new Vector3(-1.25f, 0.9006702f, 2.75f);
            action["horizon"] = 30f;
            action["rotation"] = new Vector3(0, -180f, 0);
            yield return step(action);

            action.Clear();

            action["action"] = "SetObjectPoses";
            ObjectPose pose = new ObjectPose() {
                objectName = "SoapBottle_a48be41a",
                position = new Vector3(-1.022f, 0f, 1.456f),
                rotation = new Vector3(0, -180f, 0)
            };
            ObjectPose[] poses = new ObjectPose[1];
            poses[0] = pose;
            action["objectPoses"] = poses;
            yield return step(action);

            action.Clear();

            GameObject bottle = GameObject.Find("SoapBottle_a48be41a");

            action["action"] = "PickupObject";
            action["objectId"] = bottle.GetComponent<SimObjPhysics>().objectID;
            yield return step(action);

            Assert.AreEqual(lastActionSuccess, false);

            action.Clear();

            //note normal OpenObject doesn't seem to work as the next action executes before door is fully open?
            action["action"] = "OpenObjectImmediate";
            action["objectId"] = "ShowerDoor|-00.28|+01.23|+01.73";
            yield return step(action);

            action["action"] = "PickupObject";
            action["objectId"] = bottle.GetComponent<SimObjPhysics>().objectID;
            yield return step(action);

            Assert.AreEqual(lastActionSuccess, true);
        } 
    }
}
