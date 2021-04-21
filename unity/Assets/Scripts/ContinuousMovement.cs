using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System;


namespace UnityStandardAssets.Characters.FirstPerson {
    public class ContinuousMovement {
        public static int unrollSimulatePhysics(IEnumerator enumerator, float fixedDeltaTime) {
            int count = 0;
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
            CollisionListener collisionListener,
            Transform moveTransform,
            Quaternion targetRotation,
            float fixedDeltaTime,
            float radiansPerSecond,
            bool returnToStartPropIfFailed = false
        ) {
            float degreesPerSecond = radiansPerSecond * 180.0f / Mathf.PI;
            return updateTransformPropertyFixedUpdate(
                controller,
                collisionListener,
                moveTransform,
                targetRotation,
                // Get
                (t) => t.rotation,
                //  Set
                (t, target) => t.rotation = target,
                // Next
                (t, target) => Quaternion.RotateTowards(t.rotation, target, fixedDeltaTime * degreesPerSecond),
                // Direction function for quaternion should just output target quaternion, since RotateTowards is used for addToProp
                (target, current) => target,
                // Distance Metric
                (target, current) => Quaternion.Angle(current, target),
                fixedDeltaTime,
                returnToStartPropIfFailed
            );
        }

        public static IEnumerator move(
            PhysicsRemoteFPSAgentController controller,
            CollisionListener collisionListener,
            Transform moveTransform,
            Vector3 targetPosition,
            float fixedDeltaTime,
            float unitsPerSecond,
            bool returnToStartPropIfFailed = false,
            bool localPosition = false
        ) {
            Func<Func<Transform, Vector3>, Action<Transform, Vector3>, Func<Transform, Vector3, Vector3>, IEnumerator> moveClosure =
                (get, set, next) => updateTransformPropertyFixedUpdate(
                    controller,
                    collisionListener,
                    moveTransform,
                    targetPosition,
                    get,
                    set,
                    next,
                    (target, current) => (target - current).normalized,
                    (target, current) => Vector3.SqrMagnitude(target - current),
                    fixedDeltaTime,
                    returnToStartPropIfFailed
            );

            if (localPosition) {
                return moveClosure(
                    (t) => t.localPosition,
                    (t, pos) => t.localPosition = pos,
                    (t, direction) => t.localPosition + direction * unitsPerSecond * fixedDeltaTime
                );
            } else {

                return moveClosure(
                    (t) => t.position,
                    (t, pos) => t.position = pos,
                    (t, direction) => t.position + direction * unitsPerSecond * fixedDeltaTime
                );
            }
        }

        public static IEnumerator updateTransformPropertyFixedUpdate<T>(
            PhysicsRemoteFPSAgentController controller,
            CollisionListener collisionListener,
            Transform moveTransform,
            T target,
            Func<Transform, T> getProp,
            Action<Transform, T> setProp,
            Func<Transform, T, T> nextProp,

            // We could remove this one, but it is a speedup to not compute direction for position update calls at every addToProp call and just outside while
            Func<T, T, T> getDirection,
            Func<T, T, float> distanceMetric,
            float fixedDeltaTime,
            bool returnToStartPropIfFailed,
            double epsilon = 1e-3
        ) {
            T originalProperty = getProp(moveTransform);
            var previousProperty = originalProperty;

            var arm = controller.GetComponentInChildren<IK_Robot_Arm_Controller>();
            var ikSolver = arm.gameObject.GetComponentInChildren<FK_IK_Solver>();

            // commenting out the WaitForEndOfFrame here since we shoudn't need 
            // this as we already wait for a frame to pass when we execute each action
            // yield return yieldInstruction;

            var currentProperty = getProp(moveTransform);
            float currentDistance = distanceMetric(target, currentProperty);

            T directionToTarget = getDirection(target, currentProperty);

            while (currentDistance > epsilon && collisionListener.StaticCollisions().Count == 0) {
                previousProperty = getProp(moveTransform);

                T next = nextProp(moveTransform, directionToTarget);
                float nextDistance = distanceMetric(target, next);

                // allows for snapping behaviour to target when the target is close
                // if nextDistance is too large then it will overshoot, in this case we snap to the target
                // this can happen if the speed it set high
                if (
                    nextDistance <= epsilon ||
                    nextDistance > distanceMetric(target, getProp(moveTransform))
                ) {
                    setProp(moveTransform, target);
                } else {
                    setProp(moveTransform, next);
                }

                // this will be a NOOP for Rotate/Move/Height actions
                ikSolver.ManipulateArm();

                if (!Physics.autoSimulation) {
                    Physics.Simulate(fixedDeltaTime);
                }

                yield return new WaitForFixedUpdate();

                currentDistance = distanceMetric(target, getProp(moveTransform));
            }

            T resetProp = previousProperty;
            if (returnToStartPropIfFailed) {
                resetProp = originalProperty;
            }
            continuousMoveFinish(
                controller,
                collisionListener,
                moveTransform,
                setProp,
                target,
                resetProp
            );

            // we call this one more time in the event that the arm collided and was reset
            ikSolver.ManipulateArm();
            if (!Physics.autoSimulation) {
                Physics.Simulate(fixedDeltaTime);
            }
        }

        private static void continuousMoveFinish<T>(
            PhysicsRemoteFPSAgentController controller,
            CollisionListener collisionListener,
            Transform moveTransform,
            System.Action<Transform, T> setProp,
            T target,
            T resetProp
        ) {
            bool actionSuccess = true;
            string debugMessage = "";
            IK_Robot_Arm_Controller arm = controller.GetComponentInChildren<IK_Robot_Arm_Controller>();

            var staticCollisions = collisionListener.StaticCollisions();

            if (staticCollisions.Count > 0) {
                var sc = staticCollisions[0];

                // decide if we want to return to original property or last known property before collision
                setProp(moveTransform, resetProp);

                // if we hit a sim object
                if (sc.isSimObj) {
                    debugMessage = "Collided with static sim object: '" + sc.simObjPhysics.name + "', could not reach target: '" + target + "'.";
                }

                // if we hit a structural object that isn't a sim object but still has static collision
                if (!sc.isSimObj) {
                    debugMessage = "Collided with static structure in scene: '" + sc.gameObject.name + "', could not reach target: '" + target + "'.";
                }

                actionSuccess = false;
            }

            controller.errorMessage = debugMessage;
            controller.actionFinished(actionSuccess, debugMessage);
        }
    }
}
