using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class MachineCommonSenseController : PhysicsRemoteFPSAgentController {
    public static int PHYSICS_SIMULATION_STEPS = 20;
    public int step = 0;

    public override void Initialize(ServerAction action) {
        base.Initialize(action);

        // Reset the MCS scene configuration data and player.
        this.step = 0;
        MachineCommonSenseMain main = GameObject.Find("MCS").GetComponent<MachineCommonSenseMain>();
        main.enableVerboseLog = action.logs;
        main.ChangeCurrentScene(action.sceneConfig);
    }

    public override void ProcessControlCommand(ServerAction controlCommand) {
        base.ProcessControlCommand(controlCommand);

        // Call Physics.Simulate multiple times with a small step value because a large step
        // value causes collision errors.  From the Unity Physics.Simulate documentation:
        // "Using step values greater than 0.03 is likely to produce inaccurate results."
        for (int i = 0; i < MachineCommonSenseController.PHYSICS_SIMULATION_STEPS; ++i) {
            Physics.Simulate(0.01f);
        }

        this.step++;
    }
}
