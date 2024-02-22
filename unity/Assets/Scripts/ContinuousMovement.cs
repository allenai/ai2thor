using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System;

    public interface MovableContinuous {
        public bool ShouldHalt();
        public void ContinuousUpdate(float fixedDeltaTime);
        public ActionFinished FinishContinuousMove(BaseFPSAgentController controller);
        // TODO remove from API integrate in FinishContinuousMove
        public string GetHaltMessage();
    }


namespace UnityStandardAssets.Characters.FirstPerson {

    public class ContinuousMovement {

        public static int unrollSimulatePhysics(IEnumerator enumerator, float fixedDeltaTime) {
            int count = 0;
            PhysicsSceneManager.PhysicsSimulateCallCount = 0;
            var previousAutoSimulate = Physics.autoSimulation;
            Physics.autoSimulation = false;
            while (enumerator.MoveNext()) {
                // physics simulate happens in  updateTransformPropertyFixedUpdate as long
                // as autoSimulation is off
                count++;
            }
            Physics.autoSimulation = previousAutoSimulate;
            return count;
        }

        public static IEnumerator rotate(
            PhysicsRemoteFPSAgentController controller,
            Transform moveTransform,
            Quaternion targetRotation,
            float fixedDeltaTime,
            float radiansPerSecond,
            bool returnToStartPropIfFailed = false,
            Quaternion? secTargetRotation = null
        ) {
            bool teleport = (radiansPerSecond == float.PositiveInfinity) && fixedDeltaTime == 0f;

            float degreesPerSecond = radiansPerSecond * 180.0f / Mathf.PI;

            Func<Transform, Quaternion> getRotFunc = (t) => t.rotation;
            Action<Transform, Quaternion> setRotFunc = (t, target) => t.rotation = target;
            Func<Transform, Quaternion, Quaternion> nextRotFunc = (t, target) => Quaternion.RotateTowards(t.rotation, target, fixedDeltaTime * degreesPerSecond);

            if (teleport) {
                nextRotFunc = (t, target) => target;
            }

            return updateTransformPropertyFixedUpdate(
                controller: controller,
                moveTransform: moveTransform,
                target: targetRotation,
                getProp: getRotFunc,
                setProp: setRotFunc,
                nextProp: nextRotFunc,
                // Direction function for quaternion should just output target quaternion, since RotateTowards is used for addToProp
                getDirection: (target, current) => target,
                // Distance Metric
                distanceMetric: (target, current) => Quaternion.Angle(current, target),
                fixedDeltaTime: fixedDeltaTime,
                returnToStartPropIfFailed: returnToStartPropIfFailed,
                epsilon: 1e-3,
                secTarget: secTargetRotation
            );
        }

        public static IEnumerator move(
            PhysicsRemoteFPSAgentController controller,
            Transform moveTransform,
            Vector3 targetPosition,
            float fixedDeltaTime,
            float unitsPerSecond,
            bool returnToStartPropIfFailed = false,
            bool localPosition = false
        ) {
            bool teleport = (unitsPerSecond == float.PositiveInfinity) && fixedDeltaTime == 0f;

            Func<Func<Transform, Vector3>, Action<Transform, Vector3>, Func<Transform, Vector3, Vector3>, IEnumerator> moveClosure =
                (get, set, next) => updateTransformPropertyFixedUpdate(
                    controller: controller,
                    moveTransform: moveTransform,
                    target: targetPosition,
                    getProp: get,
                    setProp: set,
                    nextProp: next,
                    getDirection: (target, current) => (target - current).normalized,
                    distanceMetric: (target, current) => Vector3.SqrMagnitude(target - current),
                    fixedDeltaTime: fixedDeltaTime,
                    returnToStartPropIfFailed: returnToStartPropIfFailed,
                    epsilon: 1e-6 // Since the distance metric uses SqrMagnitude this amounts to a distance of 1 millimeter
            );

            Func<Transform, Vector3> getPosFunc;
            Action<Transform, Vector3> setPosFunc;
            Func<Transform, Vector3, Vector3> nextPosFunc;
            if (localPosition) {
                getPosFunc = (t) => t.localPosition;
                setPosFunc = (t, pos) => t.localPosition = pos;
                nextPosFunc = (t, direction) => t.localPosition + direction * unitsPerSecond * fixedDeltaTime;
            } else {
                getPosFunc = (t) => t.position;
                setPosFunc = (t, pos) => t.position = pos;
                nextPosFunc = (t, direction) => t.position + direction * unitsPerSecond * fixedDeltaTime;
            }

            if (teleport) {
                nextPosFunc = (t, direction) => targetPosition;
            }

            return moveClosure(
                getPosFunc,
                setPosFunc,
                nextPosFunc
            );
        }

        public static IEnumerator moveAB(
            MovableContinuous movable,
            ArticulatedAgentController controller,
            float fixedDeltaTime
        ) {
            return continuousUpdateAB(
                movable: movable,
                controller: controller,
                fixedDeltaTime: fixedDeltaTime
            );
        }
    
        protected static IEnumerator finallyDestroyGameObjects(
            List<GameObject> gameObjectsToDestroy,
            IEnumerator steps
        ) {
            while (steps.MoveNext()) {
                yield return steps.Current;
            }

            foreach (GameObject go in gameObjectsToDestroy) {
                GameObject.Destroy(go);
            }
        }

        public static IEnumerator rotateAroundPoint(
            PhysicsRemoteFPSAgentController controller,
            Transform updateTransform,
            Vector3 rotatePoint,
            Quaternion targetRotation,
            float fixedDeltaTime,
            float degreesPerSecond,
            bool returnToStartPropIfFailed = false
        ) {
            bool teleport = (degreesPerSecond == float.PositiveInfinity) && fixedDeltaTime == 0f;

            // To figure out how to translate/rotate the undateTransform
            // we just create two proxy game object that represent the
            // transforms of objects sitting at the updateTransform
            // position (fulcrum) and the rotatePoint (wristProxy). The
            // wristProxy transform is a child of the fulcrum transform.
            // As we rotate the fulcrum transform we thus figure out how the
            // updateTransform should be updated by looking at how the
            // wristProxy transform has changed.

            GameObject fulcrum = new GameObject();
            fulcrum.transform.position = rotatePoint;
            fulcrum.transform.rotation = updateTransform.rotation;
            targetRotation = fulcrum.transform.rotation * targetRotation;

            GameObject wristProxy = new GameObject();
            wristProxy.transform.parent = fulcrum.transform;
            wristProxy.transform.position = updateTransform.position;
            wristProxy.transform.rotation = updateTransform.rotation;

            List<GameObject> tmpObjects = new List<GameObject>();
            tmpObjects.Add(fulcrum);
            tmpObjects.Add(wristProxy);

            Func<Quaternion, Quaternion, Quaternion> directionFunc = (target, current) => target;
            Func<Quaternion, Quaternion, float> distanceFunc = (target, current) => Quaternion.Angle(current, target);

            Func<Transform, Quaternion> getRotFunc = (t) => t.rotation;
            Action<Transform, Quaternion> setRotFunc = (t, newRotation) => {
                t.rotation = newRotation;
                updateTransform.position = wristProxy.transform.position;
                updateTransform.rotation = newRotation;
            };
            Func<Transform, Quaternion, Quaternion> nextRotFunc = (t, target) => {
                return Quaternion.RotateTowards(t.rotation, target, fixedDeltaTime * degreesPerSecond);
            };

            if (teleport) {
                nextRotFunc = (t, direction) => targetRotation;
            }

            return finallyDestroyGameObjects(
                gameObjectsToDestroy: tmpObjects,
                steps: updateTransformPropertyFixedUpdate(
                    controller: controller,
                    moveTransform: fulcrum.transform,
                    target: targetRotation,
                    getProp: getRotFunc,
                    setProp: setRotFunc,
                    nextProp: nextRotFunc,
                    getDirection: directionFunc,
                    distanceMetric: distanceFunc,
                    fixedDeltaTime: fixedDeltaTime,
                    returnToStartPropIfFailed: returnToStartPropIfFailed,
                    epsilon: 1e-3
                )
            );
        }

        public static IEnumerator continuousUpdateAB(            
            MovableContinuous movable,
            BaseFPSAgentController controller,
            float fixedDeltaTime
        )
        {

            while(!movable.ShouldHalt())
            {
                movable.ContinuousUpdate(fixedDeltaTime);
                // TODO: Remove below?
                if (!Physics.autoSimulation) {
                    //Debug.Log("manual simulate from PhysicsManager");
                    PhysicsSceneManager.PhysicsSimulateTHOR(fixedDeltaTime);
                }

                yield return new WaitForFixedUpdate();
            }

            //Debug.Log("about to start continuousMoveFinish for AB");

            // yield return null;
            yield return movable.FinishContinuousMove(controller);
        }

        public static IEnumerator updateTransformPropertyFixedUpdate<T>(
            PhysicsRemoteFPSAgentController controller,
            Transform moveTransform,
            T target,
            Func<Transform, T> getProp,
            Action<Transform, T> setProp,
            Func<Transform, T, T> nextProp,
            // Main update after new property value
            // Action? update,
            // We could remove this one, but it is a speedup to not compute direction for position update calls at every addToProp call and just outside while
            Func<T, T, T> getDirection,
            Func<T, T, float> distanceMetric,
            float fixedDeltaTime,
            bool returnToStartPropIfFailed,
            double epsilon,
            T? secTarget = null
        ) where T : struct {
            T originalProperty = getProp(moveTransform);
            var previousProperty = originalProperty;

            // TODO: do not pass controller, and pass a lambda for the update function or an
            // interface 
            var arm = controller.GetComponentInChildren<ArmController>();

            // commenting out the WaitForEndOfFrame here since we shoudn't need 
            // this as we already wait for a frame to pass when we execute each action
            // yield return yieldInstruction;
            
            int transformIterations = 1;
            if (secTarget != null) {
                transformIterations = 2;
            }

            T? currentTarget = null;
            T directionToTarget;
            var currentProperty = getProp(moveTransform);
            
            float currentDistance = 0;
            bool haveGottenWithinEpsilon = currentDistance <= epsilon;

            for (int i = 0; i < transformIterations; i++) {
                // This syntax is a new method of pattern-matching that was introduced in C# 7.0, where the type check and
                // variable assignment are performed in a single step
                if (i == 0) {
                    currentTarget = target;
                } else {
                    currentTarget = (T)secTarget;
                }

                currentDistance = distanceMetric((T)currentTarget, currentProperty);
                directionToTarget = getDirection((T)currentTarget, currentProperty);

                // // view target rotation
                // if (currentTarget is Quaternion printTarget) {
                //     Debug.Log("Oh, currentTarget is " + printTarget.eulerAngles.y);
                // }

                while (!arm.ShouldHalt()) {
                // TODO: put in movable && !collisionListener.TransformChecks(controller, moveTransform)) {
                    previousProperty = getProp(moveTransform);

                    T next = nextProp(moveTransform, directionToTarget);

                    float nextDistance = distanceMetric((T)currentTarget, next);
                    
                    // allows for snapping behaviour to target when the target is close
                    // if nextDistance is too large then it will overshoot, in this case we snap to the target
                    // this can happen if the speed it set high
                    if (
                        nextDistance <= epsilon
                        || nextDistance > distanceMetric((T)currentTarget, getProp(moveTransform))
                    ) {
                        setProp(moveTransform, (T)currentTarget);
                    } else {
                        setProp(moveTransform, next);
                    }

                // this will be a NOOP for Rotate/Move/Height actions
                arm.ContinuousUpdate(fixedDeltaTime);
                //Debug.Log("2");

                // if (!Physics.autoSimulation) {
                // //Debug.Log("3.1");
                //     if (fixedDeltaTime == 0f) {
                //         Physics.SyncTransforms();
                //     } else {
                //         PhysicsSceneManager.PhysicsSimulateTHOR(fixedDeltaTime);
                //     }
                // }
                yield return new WaitForFixedUpdate();
                //Debug.Log("3.2");

                    yield return new WaitForFixedUpdate();

                    currentDistance = distanceMetric((T)currentTarget, getProp(moveTransform));

                    if (currentDistance <= epsilon) {
                        // This logic is a bit unintuitive but it ensures we run the
                        // `setProp(moveTransform, currentTarget);` line above once we get within epsilon
                        if (haveGottenWithinEpsilon) {
                            break;
                        } else {
                            haveGottenWithinEpsilon = true;
                        }
                    }
                }
            }

            //Debug.Log("4");
            T resetProp = previousProperty;
            if (returnToStartPropIfFailed) {
                resetProp = originalProperty;
            }
            //Debug.Log("about to continuousMoveFinish");
            var actionFinished = continuousMoveFinish(
                arm,
                moveTransform,
                setProp,
                resetProp
            );

            // we call this one more time in the event that the arm collided and was reset
            arm.ContinuousUpdate(fixedDeltaTime);

            yield return new WaitForFixedUpdate();

            yield return actionFinished;
        }

        // Old Action finish
        private static ActionFinished continuousMoveFinish<T>(
            MovableContinuous movable,
            Transform moveTransform,
            System.Action<Transform, T> setProp,
            T resetProp
        ) {
            bool actionSuccess = !movable.ShouldHalt();
            string errorMessage = movable.GetHaltMessage();
            if (!actionSuccess) {
                 setProp(moveTransform, resetProp);
            }

            return new ActionFinished() {
                success = actionSuccess,
                errorMessage = errorMessage
            };
        }
        // TODO: move to new way
        // private static ActionFinished continuousMoveFinish<T>(
        //     MovableContinuous movable,
        //     BaseFPSAgentController controller,
        //     Transform moveTransform,
        //     System.Action<Transform, T> setProp,
        //     T resetProp
        // ) {

        //     ActionFinished actionFinished = movable.FinishContinuousMove(controller);
            
        //     if (!actionFinished.success) {
        //          setProp(moveTransform, resetProp);
        //     }

        //     return actionFinished;
        // }
    }
}
