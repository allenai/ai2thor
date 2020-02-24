using System.Collections;
using UnityEngine;

public class MachineCommonSensePerformerManager : AgentManager {
    public override void Update() {
        base.Update();
        // Our scene is never at rest (it is always moving)!
        this.physicsSceneManager.isSceneAtRest = false;
    }
}
