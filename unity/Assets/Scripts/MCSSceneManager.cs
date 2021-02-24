using System.Collections;
using UnityEngine;

public class MCSSceneManager : PhysicsSceneManager {
    public override void Generate_ObjectID(SimObjPhysics simObjPhysics) {
        // Do not assign IDs in AI2-THOR's format to override our MCS objects!
    }
}
