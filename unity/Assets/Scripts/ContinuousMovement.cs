using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System;


namespace UnityStandardAssets.Characters.FirstPerson {

    public class ContinuousMovement {

    public static int unrollSimulatePhysics(IEnumerator enumerator, float fixedDeltaTime) {
        var count = 0;
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
        float unitsPerSecond,
        bool waitForFixedUpdate,
        bool returnToStartPropIfFailed = false
    ) {

        return updateTransformPropertyFixedUpdate(
            controller,
            collisionListener,
            moveTransform,
            targetRotation,
            // Get
            (t) => t.rotation,
            //  Set
            (t, target) => t.rotation = target,
            // AddTo
            (t, target) => t.rotation = Quaternion.RotateTowards(t.rotation, target, fixedDeltaTime * unitsPerSecond),
            // Resets/Rollbacks if collides
            (initialRotation, lastRotation, target) => 
                returnToStartPropIfFailed? 
                    initialRotation : 
                    Quaternion.RotateTowards(lastRotation, target, -fixedDeltaTime * unitsPerSecond),
            // Direction function for quaternion should just output target quaternion, since RotateTowards is used for addToProp
            (target, current) => target,
            // Distance Metric
            (target, current) => Quaternion.Angle(current, target),
            waitForFixedUpdate,
            fixedDeltaTime
        );
    }

    public static IEnumerator move(
        PhysicsRemoteFPSAgentController controller,
        CollisionListener collisionListener,
        Transform moveTransform,
        Vector3 targetPosition,
        float fixedDeltaTime,
        float unitsPerSecond,
        bool waitForFixedUpdate,
        bool returnToStartPropIfFailed = false,
        bool localPosition = false
    ) {
        Func<Func<Transform, Vector3>, Action<Transform, Vector3>, Action<Transform, Vector3>,IEnumerator> moveClosure = 
            (get, set, add) => updateTransformPropertyFixedUpdate(
                controller,
                collisionListener,
                moveTransform,
                targetPosition,
                get,
                set,
                add,
                (initialPosition, lastPosition, direction) => 
                    returnToStartPropIfFailed? 
                        initialPosition : 
                        lastPosition - (direction * unitsPerSecond * fixedDeltaTime),
                (target, current) => (target - current).normalized,
                (target, current) => Vector3.SqrMagnitude(target - current),
                waitForFixedUpdate,
                fixedDeltaTime
        );

        if (localPosition) {
            return moveClosure(
                (t) => t.localPosition,
                (t, pos) => t.localPosition = pos,
                (t, direction) => t.localPosition += direction * unitsPerSecond * fixedDeltaTime
            );
        }
        else {

            return moveClosure(
                (t) => t.position,
                (t, pos) => t.position = pos,
                (t, direction) => t.position += direction * unitsPerSecond * fixedDeltaTime
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
        Action<Transform, T> addToProp,
        Func<T, T, T, T> resetProperty,
        // We could remove this one, but it is a speedup to not compute direction for position update calls at every addToProp call and just outside while
        Func<T, T, T> getDirection,
        Func<T, T, float> distanceMetric,
        bool waitForFixedUpdate,
        float fixedDeltaTime,
        double epsilon = 1e-3
    )
    {
        T originalProperty = getProp(moveTransform);
        var previousProperty = originalProperty;

        YieldInstruction yieldInstruction = waitForFixedUpdate ? (YieldInstruction)new WaitForFixedUpdate() : (YieldInstruction)new WaitForEndOfFrame();
        var arm = controller.GetComponentInChildren<IK_Robot_Arm_Controller>();

        yield return yieldInstruction;

        var currentProperty = getProp(moveTransform);
        float currentDistance = distanceMetric(target, currentProperty);
        float startingDistance = currentDistance;

        T directionToTarget = getDirection(target, currentProperty);

        var currentColliders = arm.currentArmCollisions();

        while ( currentDistance > epsilon && CollisionListener.StaticCollisions(currentColliders).Count == 0 && currentDistance <= startingDistance) {

            previousProperty = getProp(moveTransform);

            addToProp(moveTransform, directionToTarget);

            if (!Physics.autoSimulation) {
                Physics.Simulate(fixedDeltaTime);
            }

            yield return yieldInstruction;

            currentColliders = arm.currentArmCollisions();


            currentDistance = distanceMetric(target, getProp(moveTransform));
        }

        // // DISABLING JUMP since it can lead to clipping
        // if (currentDistance <= epsilon && !collisionListener.ShouldHalt()) {
        //    // Maybe switch to this?
        //    // addPosition(moveTransform, targetDirection * currentDistance);
        //    setProp(moveTransform, target);
        // }

        continuousMoveFinish(
            controller,
            collisionListener,
            moveTransform, 
            setProp, 
            target, 
            resetProperty(originalProperty, previousProperty, directionToTarget), 
            currentDistance > startingDistance
        );
    }

    private static void continuousMoveFinish<T>(
        PhysicsRemoteFPSAgentController controller,
        CollisionListener collisionListener,
        Transform moveTransform,
        System.Action<Transform, T> setProp,
        T target,
        T resetProp,
        bool overshoot = false
       
    ) {
        var actionSuccess = true;
        var debugMessage = "";
        //var staticCollisions = collisionListener.StaticCollisions();
        var arm = controller.GetComponentInChildren<IK_Robot_Arm_Controller>();
        // staticCollisions are not firing at the right time, so using IsArmColliding instead
        var currentColliders = arm.currentArmCollisions();

        var staticCollisions = CollisionListener.StaticCollisions(currentColliders);

        if (staticCollisions.Count > 0){
                var sc = staticCollisions[0];
                
                //decide if we want to return to original property or last known property before collision
                setProp(moveTransform, resetProp);

                //if we hit a sim object
                if(sc.isSimObj)
                {
                    debugMessage = "Collided with static sim object: '" + sc.simObjPhysics.name + "', could not reach target: '" + target + "'.";
                }

                //if we hit a structural object that isn't a sim object but still has static collision
                if(!sc.isSimObj)
                {
                    debugMessage = "Collided with static structure in scene: '" + sc.gameObject.name + "', could not reach target: '" + target + "'.";
                }

                actionSuccess = false;
        } else if (overshoot) {
            Debug.Log("stopping - target was overshot");
            debugMessage =  $" Has overshot the target ${typeof(T).ToString()}";
            actionSuccess = false;
        }
        controller.actionFinished(actionSuccess, debugMessage);
    }

        

    }

}