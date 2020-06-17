using System.Collections;
using UnityEngine;

public class MCSSceneManager : PhysicsSceneManager {
    protected override void Generate_UniqueID(SimObjPhysics simObjPhysics) {
        // Do not assign IDs in AI2-THOR's format to override our MCS objects!
    }
}
