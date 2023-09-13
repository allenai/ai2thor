using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;

public interface Arm {

    // void ContinuousUpdate();
    bool IsArmColliding();
    // bool ShouldHalt();
    GameObject GetArmTarget();

    ArmMetadata GenerateMetadata();
    

    public Dictionary<SimObjPhysics, HashSet<Collider>> heldObjects {
        get;
    }

    IEnumerator moveArmRelative(
        PhysicsRemoteFPSAgentController controller,
        Vector3 offset,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition
    );

    IEnumerator moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition
    );


    IEnumerator moveArmBase(
        PhysicsRemoteFPSAgentController controller,
        float height,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool normalizedY
    );

    IEnumerator moveArmBaseUp(
        PhysicsRemoteFPSAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed
    );

    IEnumerator rotateWrist(
        PhysicsRemoteFPSAgentController controller,
        Quaternion rotation,
        float degreesPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed
    );

    List<SimObjPhysics> WhatObjectsAreInsideMagnetSphereAsSOP(bool onlyPickupable);

    IEnumerator PickupObject(List<string> objectIds);

    IEnumerator DropObject();

}
