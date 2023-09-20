using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;

namespace Tests {
    public class TestDispatcher : TestBase {

        // [Serializable]
        // public class ReturnTest {
        //     public bool ranWithPhysicsAutoSim;
        //     public fixedDe
        // }

        public class TestController : ActionInvokable {
            AgentManager agentManager;
            public bool ranAsCoroutine;
            public bool ranAsBackCompat;
            public bool ranCompleteCallback = false;

            public ActionFinished actionFinished;
            public TestController(AgentManager agentManager) {
                this.agentManager =agentManager;
            }
            public void TestBackCompatAction(int x) {
                ranAsBackCompat = true;
                // Calls action finished/changes internal state, who knows? ...
            }
            public ActionFinished TestNewAction(int x) {
                return new ActionFinished() {
                    success = true,
                    actionReturn = x,
                };
            }

             public IEnumerator TestAsyncAction(int x) {
                for (var i = 0; i < x; i++) {
                    yield return new WaitForFixedUpdate();
                }

                yield return new ActionFinished() {
                    success = true,
                    actionReturn = x,
                };
            }

             public IEnumerator TestAsyncActionPhysicsParams(PhysicsSimulationParams physicsSimulationParams) {
                var count = physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f;
                for (var i = 0; i < count; i++) {
                    yield return new WaitForFixedUpdate();
                }

                yield return new ActionFinished() {
                    success = true,
                    actionReturn = physicsSimulationParams
                };
            }

            public void Complete(ActionFinished actionFinished) {
                ranCompleteCallback = true;
                this.actionFinished = actionFinished;
            }
            public Coroutine StartCoroutine(IEnumerator routine) {
                ranAsCoroutine = true;
                return agentManager.StartCoroutine(routine);
            }
        }

        [UnityTest]
        public IEnumerator TestDispatchInvalidArguments() {
            yield return Initialize();
            var args = new Dictionary<string, object>() {
                {"action", "PutObject"},
                {"x", 0.3f},
                {"y", 0.3f},
                {"z", 0.3f},
                {"forceAction", false},
                {"placeStationary", true}
            };
            Assert.Throws<InvalidArgumentsException>(() => {
                ActionDispatcher.Dispatch(agentManager.PrimaryAgent, new DynamicServerAction(args));
            });
        }

        [UnityTest]
        public IEnumerator TestStepInvalidArguments() {
            yield return Initialize();

            var args = new Dictionary<string, object>() {
                {"action", "PutObject"},
                {"x", 0.3f},
                {"y", 0.3f},
                {"z", 0.3f},
                {"forceAction", false},
                {"placeStationary", true}
            };

            yield return step(args);
            Assert.IsFalse(lastActionSuccess);
            Assert.IsTrue(error.Contains("invalid argument: 'z'"));
        }

        [UnityTest]
        public IEnumerator TestBackCompatAction() {

            var controller = new TestController(this.agentManager);

            var args = new Dictionary<string, object>() {
                {"action", "TestBackCompatAction"},
                {"x", 0}
            };
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            Assert.IsTrue(controller.ranAsBackCompat);
            // Complete interface is not called so this simulates not calling action finished to change inner state
            Assert.IsFalse(controller.ranCompleteCallback);
            // No manual physics step ran
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, 0);
            yield return true;
        }

        [UnityTest]
        public IEnumerator TestNewAction() {

            var controller = new TestController(this.agentManager);

            var result = 5;
            var args = new Dictionary<string, object>() {
                {"action", "TestNewAction"},
                {"x", result}
            };

            // This happens at init
            PhysicsSceneManager.SetDefaultSimulationParams(new PhysicsSimulationParams());
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // Complete (new actionFinished) was called automatically
            Assert.IsTrue(controller.ranCompleteCallback);

            // No physics sim happened
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, 0);

            // 1 iteration corresponding the yield return new ActionFinished()
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, 1);
            // returns result
            Assert.AreEqual((int)controller.actionFinished.actionReturn, result);
            yield return true;
        }

        [UnityTest]
         public IEnumerator TestBackCompatActionPhysicsPadding() {

            var controller = new TestController(this.agentManager);

            var result = 5;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.02f,
                maxActionTimeMilliseconds = 100,
                autoSimulation = false
            };

            var args = new Dictionary<string, object>() {
                {"action", "TestNewAction"},
                {"x", result},
                {"physicsSimulationParams", physicsSimulationParams}
            };
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // For legacy actions we don't want to call Complete as they handle their own state. Avoid call to actionFinished twice
            Assert.IsFalse(controller.ranCompleteCallback);

            // 5 physics steps of pure padding
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, physicsSimulationParams.maxActionTimeMilliseconds / physicsSimulationParams.fixedDeltaTime);

            // Time passed
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds * 1000.0f);

            // 6 iterations corresponding to 5 simulate steps and 1 yield return new ActionFinished()
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, 6);
            yield return true;
        }
    }
}
