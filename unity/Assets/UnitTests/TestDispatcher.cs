using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestDispatcher : TestBase {
        [UnityTest]
        public IEnumerator TestDispatchInvalidArguments() {
            yield return initalizeDefaultDiscrete();
            var controller = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();
            var args = new Dictionary<string, object>() {
                {"action", "PutObject"},
                {"x", 0.3f},
                {"y", 0.3f},
                {"z", 0.3f},
                {"forceAction", false},
                {"placeStationary", true}
            };
            Assert.Throws<InvalidArgumentsException>(() => {
                ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            });
        }

        [UnityTest]
        public IEnumerator TestStepInvalidArguments() {
            yield return initalizeDefaultDiscrete();

            var args = new Dictionary<string, object>() {
                {"action", "PutObject"},
                {"x", 0.3f},
                {"y", 0.3f},
                {"z", 0.3f},
                {"forceAction", false},
                {"placeStationary", true}
            };

            yield return step(args);
            BaseFPSAgentController agent = GameObject.FindObjectOfType<BaseFPSAgentController>();
            Assert.IsFalse(lastActionSuccess);
            Assert.IsTrue(error.Contains("invalid argument: 'z'"));
        }
    }
}
