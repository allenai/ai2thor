using System.Collections;
using UnityEngine;

public class MachineCommonSensePerformerManager : AgentManager {
    public static int step = 0;
    public void FinalizeEmit() {
        base.setReadyToEmit(true);
    }
    public override void Initialize(ServerAction action) {
        base.Initialize(action);
        MachineCommonSensePerformerManager.step = 0;
        MachineCommonSenseMain main = GameObject.Find("MCS").GetComponent<MachineCommonSenseMain>();
        main.enableVerboseLog = action.logs;
        main.ChangeCurrentScene(action.sceneConfig);
    }
    public override void setReadyToEmit(bool readyToEmit) {
        MachineCommonSensePerformerManager.step++;
    }
}
