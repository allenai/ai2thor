using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class MachineCommonSenseController : PhysicsRemoteFPSAgentController {
    public static float DISTANCE_HELD_OBJECT_Y = 0.15f;
    public static float DISTANCE_HELD_OBJECT_Z = 0.15f;
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

    public override void PickupObject(ServerAction action) {
        SimObjPhysics target = physicsSceneManager.UniqueIdToSimObjPhysics[action.objectId];

        MeshFilter meshFilter = target.gameObject.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null) {
            // Move the player's hand on the Y axis corresponding to the size of the target object so that the object,
            // once held, is shown at the bottom of the player's camera view.
            float handY = (meshFilter.mesh.bounds.size.y * meshFilter.transform.localScale.y);
            // Move the player's hand on the Z axis corresponding to the size of the target object so that the object,
            // once held, never collides with the player's body.
            float handZ = (meshFilter.mesh.bounds.size.z / 2.0f * meshFilter.transform.localScale.z);
            if (!GameObject.ReferenceEquals(meshFilter.gameObject, target.gameObject)) {
                handY = (handY + (meshFilter.transform.localPosition.y * meshFilter.transform.localScale.y));
                handZ = ((handZ - meshFilter.transform.localPosition.z) * target.gameObject.transform.localScale.z);
            }
            this.AgentHand.transform.localPosition = new Vector3(this.AgentHand.transform.localPosition.x,
                (handY + MachineCommonSenseController.DISTANCE_HELD_OBJECT_Y) * -1,
                (handZ + MachineCommonSenseController.DISTANCE_HELD_OBJECT_Z) * (1.0f / this.transform.localScale.z));
        } else {
            Debug.LogError("PickupObject target " + target.gameObject.name + " does not have a MeshFilter!");
        }

        base.PickupObject(action);
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

    public override void ResetAgentHandPosition(ServerAction action) {
        // Don't reset the player's hand position if the player is just moving or rotating.
        // Use this.lastAction here because this function's ServerAction argument is sometimes null.
        if (this.lastAction.StartsWith("Move") || this.lastAction.StartsWith("Rotate") ||
            this.lastAction.StartsWith("Look") || this.lastAction.StartsWith("Teleport")) {
            return;
        }
        base.ResetAgentHandPosition(action);
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
