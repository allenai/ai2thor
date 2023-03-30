using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;

public interface Arm {

    void manipulateArm();
    bool IsArmColliding();
    bool shouldHalt();
    GameObject GetArmTarget();

    ArmMetadata GenerateMetadata();
    

    public Dictionary<SimObjPhysics, HashSet<Collider>> heldObjects {
        get;
    }

    void moveArmRelative(
        PhysicsRemoteFPSAgentController controller,
        Vector3 offset,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition,
        bool disableRendering
    );

    void moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition,
        bool disableRendering
    );


    void moveArmBase(
        PhysicsRemoteFPSAgentController controller,
        float height,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool disableRendering,
        bool normalizedY
    );

    void moveArmBaseUp(
        PhysicsRemoteFPSAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool disableRendering
    );

    void rotateWrist(
        PhysicsRemoteFPSAgentController controller,
        Quaternion rotation,
        float degreesPerSecond,
        bool disableRendering,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed
    );

    List<SimObjPhysics> WhatObjectsAreInsideMagnetSphereAsSOP(bool onlyPickupable);

    bool PickupObject(List<string> objectIds, ref string errorMessage);

    void DropObject();

}
