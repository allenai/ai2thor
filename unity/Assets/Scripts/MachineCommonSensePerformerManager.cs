using UnityEngine;

public class MachineCommonSensePerformerManager : AgentManager {
    public static int step = 0;
    public void FinalizeEmit() {
        base.setReadyToEmit(true);
    }
    public override void setReadyToEmit(bool readyToEmit) {
        MachineCommonSensePerformerManager.step++;
    }
}
