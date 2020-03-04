using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class MachineCommonSenseController : PhysicsRemoteFPSAgentController {
    public static int PHYSICS_SIMULATION_STEPS = 20;
    public int step = 0;

    protected int minHorizon = -90;
    protected int maxHorizon = 90;
    protected float minRotation = -360f;
    protected float maxRotation = 360f;

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

    public override void RotateLook(ServerAction response)
    {
        // Need to calculate current rotation/horizon and increment by inputs given
        float updatedRotationValue = transform.localEulerAngles.y + response.rotation.y;
        int updatedHorizonValue = (int)m_Camera.transform.localEulerAngles.x + response.horizon;

        // Check to ensure rotation value stays between -360 and 360
        while (updatedRotationValue >= maxRotation)
        {
            updatedRotationValue -= maxRotation;
        }

        while (updatedRotationValue <= minRotation)
        {
            updatedRotationValue += maxRotation;
        }

        // Limiting where to look based on realistic expectation (for instance, a person can't turn
        // their head 180 degrees)
        if (updatedHorizonValue > maxHorizon || updatedHorizonValue < minHorizon)
        {
            Debug.Log("Value of horizon needs to be between " + minHorizon + " and " + maxHorizon +
                ". Setting value to 0.");
            updatedHorizonValue = 0;
        }

        ServerAction action = new ServerAction();
        action.rotation.y = updatedRotationValue;
        action.horizon = updatedHorizonValue;
        base.RotateLook(action);
    }

    public override MetadataWrapper generateMetadataWrapper() {
        MetadataWrapper metadataWrapper = base.generateMetadataWrapper();
        metadataWrapper.lastActionStatus = this.lastActionStatus;
        return metadataWrapper;
    }
}
