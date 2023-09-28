using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using System.Threading.Tasks;

namespace Tests {
    public class TestDispatcher : TestBase {

        public class TestController : ActionInvokable {
            protected AgentManager agentManager;
            public bool ranAsCoroutine;
            public bool ranAsBackCompat;
            public bool ranCompleteCallback = false;

            public ActionFinished actionFinished;
            public TestController(AgentManager agentManager) {
                this.agentManager =agentManager;
            }
            public void BackCompatAction(int x) {
                ranAsBackCompat = true;
                // Calls action finished/changes internal state, who knows? ...
            }
            public ActionFinished NewAction(int x) {
                return new ActionFinished() {
                    success = true,
                    actionReturn = x,
                };
            }

             public IEnumerator AsyncAction(int x) {
                for (var i = 0; i < x; i++) {
                    yield return new WaitForFixedUpdate();
                }

                yield return new ActionFinished() {
                    success = true,
                    actionReturn = x,
                };
            }

            public IEnumerator AsyncActionMeasureUnityTime(int x) {
                for (var i = 0; i < x; i++) {
                    yield return new WaitForFixedUpdate();
                }

                yield return new ActionFinished() {
                    success = true,
                    actionReturn = Time.fixedTime,
                };
            }

            // For ActionFinished return type it's a compile error
             public IEnumerator AsyncActionMissingActionFinished() {
                yield return new WaitForFixedUpdate();
            }


             public IEnumerator AsyncActionThrows() {
                yield return new WaitForFixedUpdate();
                object k = null;
                // Null Ref exception
                k.ToString();
            }

             public IEnumerator AsyncActionPhysicsParams(PhysicsSimulationParams physicsSimulationParams) {
                var count = (int)Math.Round(physicsSimulationParams.minSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime);
                for (var i = 0; i < count; i++) {
                    yield return new WaitForFixedUpdate();
                }

                yield return new ActionFinished() {
                    success = true,
                    actionReturn = physicsSimulationParams
                };
            }

            public void Complete(ActionFinished actionFinished) {
                // Simulating what BaseFPSDoes for old actions where isDummy will be true
                if (!actionFinished.isDummy) {
                    ranCompleteCallback = true;
                }
                this.actionFinished = actionFinished;
            }
            public Coroutine StartCoroutine(IEnumerator routine) {
                ranAsCoroutine = true;
                return agentManager.StartCoroutine(routine);
            }
        }

        public class TestControllerChild : TestController {
            public TestControllerChild(AgentManager agentManager) : base(agentManager) {}
            public bool calledChild = false;
            // Ambiguous actions
            public void BackCompatAction(int x, string defaultParam = "") {
                ranAsBackCompat = true;
                calledChild = true;
            }

            public ActionFinished NewAction(int x) {
                calledChild = true;
                return new ActionFinished() {
                    success = true,
                    actionReturn = x,
                };
            }

            public IEnumerable AsyncAction(int x, int otherParams = 0) {
                calledChild = true;
                yield return new ActionFinished() {
                    success = true,
                    actionReturn = x,
                };
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
                {"action", "BackCompatAction"},
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
                {"action", "NewAction"},
                {"x", result}
            };

            // This happens at init
            PhysicsSceneManager.SetDefaultSimulationParams(new PhysicsSimulationParams() {
                autoSimulation = false
            });
            
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
                minSimulateTimeSeconds = 0.1f,
                autoSimulation = false
            };

            var args = new Dictionary<string, object>() {
                {"action", "BackCompatAction"},
                {"x", result},
                {"physicsSimulationParams", physicsSimulationParams}
            };
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // For legacy actions we don't want to call Complete as they handle their own state. Avoid call to actionFinished twice
            Assert.IsFalse(controller.ranCompleteCallback);

            // 5 physics steps of pure padding
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, physicsSimulationParams.minSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime);

            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - physicsSimulationParams.minSimulateTimeSeconds) < 1e-5;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);

            // Only one iteration since it's a backcompat action
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, 1);
            yield return true;
        }

        [UnityTest]
         public IEnumerator TestNewActionPhysicsPadding() {

            var controller = new TestController(this.agentManager);

            var result = 5;
            const float eps = 1e-5f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.02f,
                minSimulateTimeSeconds = 0.1f,
                autoSimulation = false
            };

            var args = new Dictionary<string, object>() {
                {"action", "NewAction"},
                {"x", result},
                {"physicsSimulationParams", physicsSimulationParams}
            };
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            

            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);

            // 5 physics steps of pure padding
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, physicsSimulationParams.minSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime);

            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - physicsSimulationParams.minSimulateTimeSeconds) < eps;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);
            
            // 1 Iterator expansion from the return Action Finished 
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, 1);
            yield return true;
        }

        [UnityTest]
        public IEnumerator TestAsyncAction() {

            var controller = new TestController(this.agentManager);

            var simulateTimes = 5;
            const float eps = 1e-5f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = false
            };

            var args = new Dictionary<string, object>() {
                {"action", "AsyncAction"},
                {"x", simulateTimes}
            };

            // This happens at init
            PhysicsSceneManager.SetDefaultSimulationParams(physicsSimulationParams);

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);

            // 5 simulations steps, simulatetime / fixedDeltaTime
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, PhysicsSceneManager.PhysicsSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime);

            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - (physicsSimulationParams.fixedDeltaTime * simulateTimes)) < eps;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);
            // Times of simulation + yield return ActionFinished
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, simulateTimes + 1);

            Assert.AreEqual(controller.actionFinished.actionReturn, simulateTimes);
            yield return true;

        }

        [UnityTest]
        public IEnumerator TestAsyncActionPassPhysicsSimulationParams() {

            var controller = new TestController(this.agentManager);

            var simulateTimes = 10;
            const float eps = 1e-5f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = false
            };

            var args = new Dictionary<string, object>() {
                {"action", "AsyncAction"},
                {"x", simulateTimes},
                {"physicsSimulationParams", physicsSimulationParams}
            };

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);

            Assert.IsTrue(Math.Abs(PhysicsSceneManager.PhysicsSimulateCallCount - PhysicsSceneManager.PhysicsSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime) < eps);

            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - (physicsSimulationParams.fixedDeltaTime * simulateTimes)) < eps;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);
            // Times of simulation + yield return ActionFinished
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, simulateTimes + 1);
            // Returns value in action return
            Assert.AreEqual(controller.actionFinished.actionReturn, simulateTimes);
            yield return true;

        }

        [UnityTest]
        public IEnumerator TestAsyncActionPassPhysicsSimulationParamsAndPadding() {

            var controller = new TestController(this.agentManager);

            var simulateTimes = 10;
            const float eps = 1e-5f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = false,
                // has to run 1 second so 0.1 seconds of action time and  0.9 of padding
                minSimulateTimeSeconds = 1
            };

            var args = new Dictionary<string, object>() {
                {"action", "AsyncAction"},
                {"x", simulateTimes},
                {"physicsSimulationParams", physicsSimulationParams}
            };

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);

            Assert.IsTrue(Math.Abs(PhysicsSceneManager.PhysicsSimulateCallCount - (physicsSimulationParams.minSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime)) < eps);
           
            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - physicsSimulationParams.minSimulateTimeSeconds) < eps;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);
            // Times of simulation + yield return ActionFinished
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, simulateTimes + 1);
            // Returns value in action return
            Assert.AreEqual(controller.actionFinished.actionReturn, simulateTimes);
            yield return true;

        }

        [UnityTest]
        public IEnumerator TestAsyncActionPhysicsParams() {

            var controller = new TestController(this.agentManager);

            // Target value
            var simulateTimes = 20;
            const float eps = 1e-5f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = false,
                minSimulateTimeSeconds = 0.2f
            };

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionPhysicsParams"},
                {"physicsSimulationParams", physicsSimulationParams}
            };

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);

            Assert.IsTrue(Math.Abs(PhysicsSceneManager.PhysicsSimulateCallCount - PhysicsSceneManager.PhysicsSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime) < eps);

            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - (physicsSimulationParams.fixedDeltaTime * simulateTimes)) < eps;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);
            // Times of simulation + yield return ActionFinished
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, simulateTimes + 1);

            var returnPhysicsParams = controller.actionFinished.actionReturn as PhysicsSimulationParams;
            Debug.Log($"{returnPhysicsParams.autoSimulation} {returnPhysicsParams.fixedDeltaTime} {returnPhysicsParams.minSimulateTimeSeconds}");
            Debug.Log($"{physicsSimulationParams.autoSimulation} {physicsSimulationParams.fixedDeltaTime} {physicsSimulationParams.minSimulateTimeSeconds}");
            // Returns back the physics params
            Assert.AreEqual(controller.actionFinished.actionReturn, physicsSimulationParams);
            yield return true;

        }

        [UnityTest]
        public IEnumerator TestAsyncActionDefaultPhysicsParams() {

            var controller = new TestController(this.agentManager);

            
            const float eps = 1e-5f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = false,
                minSimulateTimeSeconds = 0.1f
            };

            var simulateTimes = (int)((physicsSimulationParams.minSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime) + eps);

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionPhysicsParams"},
            };

            // This happens at init
            PhysicsSceneManager.SetDefaultSimulationParams(physicsSimulationParams);


            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);

            Assert.IsTrue(Math.Abs(PhysicsSceneManager.PhysicsSimulateCallCount - PhysicsSceneManager.PhysicsSimulateTimeSeconds / physicsSimulationParams.fixedDeltaTime) < eps);

            var simulateTimeMatched = Math.Abs(PhysicsSceneManager.PhysicsSimulateTimeSeconds - (physicsSimulationParams.fixedDeltaTime * simulateTimes)) < eps;
            // Mathf.Approximately(PhysicsSceneManager.PhysicsSimulateTimeSeconds, physicsSimulationParams.maxActionTimeMilliseconds / 1000.0f);
            Assert.IsTrue(simulateTimeMatched);
            // Times of simulation + yield return ActionFinished
            Assert.AreEqual(PhysicsSceneManager.IteratorExpandCount, simulateTimes + 1);
            // Returns back the physics params
            Assert.AreEqual(controller.actionFinished.actionReturn, physicsSimulationParams);
            yield return true;

        }

        [UnityTest]
        public IEnumerator TestAsyncActionAutosimulation() {

            var controller = new TestController(this.agentManager);

            // Target value
            var simulateTimes =8;
            // For unity time epsilon is much larger, therfore non-deterministic behavior
            const float eps = 1e-2f;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = true
            };

            var args = new Dictionary<string, object>() {
                {"action", "AsyncAction"},
                {"x", simulateTimes},
                {"physicsSimulationParams", physicsSimulationParams}
            };

            var startTime = Time.time;
            var startFixedTime = Time.fixedTime;

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            // This wait until can wait more than the actual simulation time, therfore fixedTime be larger than simulationtimes * fixedDeltatime
            // For a unity synced loop like EmitFrame this is not an issue
            yield return new WaitUntil(() => controller.ranCompleteCallback == true);
            var endTime = Time.time;
            var endFixedTime = Time.fixedTime;
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);
            // Because of autosimulation it was launched as an async coroutine
            Assert.IsTrue(controller.ranAsCoroutine);

            var runTime = endTime - startTime;
            var runTimeFixed = endFixedTime - startFixedTime;
            var smallEps = 1e-5f;

            Debug.Log($"------ Fixed time {startFixedTime} {endFixedTime} _ {runTimeFixed}");
            // Flaky because WaitUntil can run longer
            // var simulateTimeMatched = Math.Abs(runFixedTime - (physicsSimulationParams.fixedDeltaTime * (simulateTimes + 1))) < smallEps;
            var simulateTimeMatched = physicsSimulationParams.fixedDeltaTime * simulateTimes - smallEps < runTimeFixed;
            Assert.IsTrue(simulateTimeMatched);
            
            
            // Assert.IsTrue(simulateTimeMatched);
            // Below is flaky assert as Time.time is virtual and finding good epsilon is tricky
            // Assert.IsTrue( (runTime + eps) >= physicsSimulationParams.fixedDeltaTime * simulateTimes);

            // Returns back the physics params
            Assert.AreEqual(controller.actionFinished.actionReturn, simulateTimes);
            yield return true;

        }

        [UnityTest]
        public IEnumerator TestAsyncActionAutosimulationExactTime() {
            // Same as above but action itself measures Time.fixedTime to get accurate simulation time
            var controller = new TestController(this.agentManager);

            // Target value
            var simulateTimes =8;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = true
            };

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionMeasureUnityTime"},
                {"x", simulateTimes},
                {"physicsSimulationParams", physicsSimulationParams}
            };

            var startFixedTime = Time.fixedTime;

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));

            yield return new WaitUntil(() => controller.actionFinished != null);
            var endFixedTime = (float)controller.actionFinished.actionReturn;
            
            // New Action types call Complete
            Assert.IsTrue(controller.ranCompleteCallback);
            // Because of autosimulation it was launched as an async coroutine
            Assert.IsTrue(controller.ranAsCoroutine);

            var runTimeFixed = endFixedTime - startFixedTime;
            var smallEps = 1e-5f;

            Debug.Log($"------ Fixed time {startFixedTime} {endFixedTime} _ {runTimeFixed}");

            var simulateTimeMatched = Math.Abs(runTimeFixed - (physicsSimulationParams.fixedDeltaTime * simulateTimes)) < smallEps;
            Assert.IsTrue(simulateTimeMatched);
            
            yield return true;

        }

         [UnityTest]
        public IEnumerator TestNewActionAutosimulation() {

            var controller = new TestController(this.agentManager);

            var result = 5;
            var args = new Dictionary<string, object>() {
                {"action", "NewAction"},
                {"x", result}
            };

            // This happens at init
            PhysicsSceneManager.SetDefaultSimulationParams(new PhysicsSimulationParams() {
                autoSimulation = true
            });
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));

            yield return new WaitUntil(() => controller.ranCompleteCallback);
            
            // Complete (new actionFinished) was called automatically
            Assert.IsTrue(controller.ranCompleteCallback);

            // No physics sim happened
            Assert.AreEqual(PhysicsSceneManager.PhysicsSimulateCallCount, 0);

            // returns result
            Assert.AreEqual((int)controller.actionFinished.actionReturn, result);
            yield return true;
        }

         [UnityTest]
        public IEnumerator TestBackCompatActionAutosimulation() {

            var controller = new TestController(this.agentManager);

            var x = 10;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = true
            };

            var args = new Dictionary<string, object>() {
                {"action", "BackCompatAction"},
                {"x", x},
                {"physicsSimulationParams", physicsSimulationParams}
            };

            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            
            // Old Action types don't call true Complete, they have their own actionFinished
            Assert.IsFalse(controller.ranCompleteCallback);
            Assert.IsTrue(controller.ranAsBackCompat);

            yield return true;
        }

         [UnityTest]
        public IEnumerator TestBackCompatActionAutosimulationWithPadding() {
            // Can't add padding to legacy actions run in coroutines, as actionFinished
            // returns controll and can't add padding if not blocked because can create "deathloop"
            var controller = new TestController(this.agentManager);

            var x = 10;

            var physicsSimulationParams = new PhysicsSimulationParams() {
                fixedDeltaTime = 0.01f,
                autoSimulation = true,
                minSimulateTimeSeconds = 0.1f
            };

            var args = new Dictionary<string, object>() {
                {"action", "BackCompatAction"},
                {"x", x},
                {"physicsSimulationParams", physicsSimulationParams}
            };


            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));

            // No time simulating physics because of autosim
            Assert.IsTrue(PhysicsSceneManager.PhysicsSimulateTimeSeconds <  1e-5);

            // Old actions don't call real complete
            Assert.IsFalse(controller.ranCompleteCallback);
            Assert.IsTrue(controller.ranAsBackCompat);

            yield return true;
        }

        [UnityTest]
         public IEnumerator TestAsyncActionMissingActionFinished() {

            var controller = new TestController(this.agentManager);

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionMissingActionFinished"},
            };

            PhysicsSceneManager.SetDefaultSimulationParams(
                new PhysicsSimulationParams() {
                    autoSimulation = false
                }
            );
        
             Assert.Throws<MissingActionFinishedException>(() => {
                ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            });

            yield return true;
        }

        [UnityTest]
         public IEnumerator TestAsyncActionCoroutineMissingActionFinished() {

            var controller = new TestController(this.agentManager);

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionMissingActionFinished"},
            };

            PhysicsSceneManager.SetDefaultSimulationParams(
                new PhysicsSimulationParams() {
                    autoSimulation = true
                }
            );
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));


            yield return new WaitUntil(() => controller.actionFinished != null);

            Assert.IsTrue(controller.ranCompleteCallback);
            Assert.IsFalse(controller.actionFinished.success);
            Assert.AreEqual(controller.actionFinished.errorCode, ServerActionErrorCode.MissingActionFinished);
        }

        [UnityTest]
         public IEnumerator TestAsyncActionThrows() {

            var controller = new TestController(this.agentManager);

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionThrows"},
            };

            PhysicsSceneManager.SetDefaultSimulationParams(
                new PhysicsSimulationParams() {
                    autoSimulation = false
                }
            );
            // Exceptions are handlede in ProcessControll command of FPS Controller
            Assert.Throws<NullReferenceException>(() => {
                ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            });

            yield return true;
        }

        [UnityTest]
         public IEnumerator TestAsyncActionThrowsCoroutine() {

            var controller = new TestController(this.agentManager);

            var args = new Dictionary<string, object>() {
                {"action", "AsyncActionThrows"},
            };

            PhysicsSceneManager.SetDefaultSimulationParams(
                new PhysicsSimulationParams() {
                    autoSimulation = true
                }
            );
            
            ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));


            yield return new WaitUntil(() => controller.actionFinished != null);

            Assert.IsTrue(controller.ranCompleteCallback);
            // Bacause it's run in an "async" coroutine exception is handled inside and propagates to ActionFinished
            Assert.IsFalse(controller.actionFinished.success);
            Debug.Log(controller.actionFinished.errorMessage);
            Assert.AreEqual(controller.actionFinished.errorCode, ServerActionErrorCode.UnhandledException);
        }

        [UnityTest]
         public IEnumerator TestChildAmbiguousAction() {

            var controller = new TestControllerChild(this.agentManager);

            var args = new Dictionary<string, object>() {
                {"action", "BackCompatAction"},
                {"x", 1},
            };
           
            Assert.Throws<AmbiguousActionException>(() => {
                ActionDispatcher.Dispatch(controller, new DynamicServerAction(args));
            });

            yield return true;
        }

    }
}
