using System;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityStandardAssets.Characters.FirstPerson;


public class JavaScriptInterface : MonoBehaviour {

    private PhysicsRemoteFPSAgentController PhysicsController;
    //private DebugInputField inputField; //inputField.setControlMode no longer used in SetController

    [DllImport("__Internal")]
    private static extern void Init();

    [DllImport("__Internal")]
    private static extern void SendEvent(string str);

    [DllImport("__Internal")]
    private static extern void SendMetadata(string str);

    public void SendAction(ServerAction action)
    {
        SendEvent(JsonUtility.ToJson(action));
    }

/*
    metadata: serialized metadata, commonly an instance of MultiAgentMetadata
 */
    public void SendActionMetadata(string metadata)
    {
        SendMetadata(metadata);
    }

    void Start()
    {
        PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();
        //inputField = GameObject.Find("DebugCanvasPhysics").GetComponentInChildren<DebugInputField>();//FindObjectOfType<DebugInputField>();
        //GameObject.Find("DebugCanvas").GetComponentInChildren<AgentManager>();
        Init();

        Debug.Log("Calling store data");
    }

    public void GetRenderPath()
    {
        SendMetadata("" + GetComponentInChildren<Camera>().actualRenderingPath);
    }

    public void SetController(string controlModeEnumString) 
    {
        ControlMode controlMode = (ControlMode) Enum.Parse(typeof(ControlMode), controlModeEnumString, true);
        //inputField.setControlMode(controlMode);

        Type componentType;
        var success = PlayerControllers.controlModeToComponent.TryGetValue(controlMode, out componentType);
        var Agent = PhysicsController.gameObject;
        if (success) {
            var previousComponent = Agent.GetComponent(componentType) as MonoBehaviour;
            if (previousComponent == null) {
                previousComponent = Agent.AddComponent(componentType) as MonoBehaviour; 
            }
            previousComponent.enabled = true;
        }
    }

    public void Step(string serverAction)
    {
        ServerAction controlCommand = new ServerAction();
        JsonUtility.FromJsonOverwrite(serverAction, controlCommand);
        PhysicsController.ProcessControlCommand(controlCommand);
    }
}