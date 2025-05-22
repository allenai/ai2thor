using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JavaScriptInterface : MonoBehaviour {
    // IL2CPP throws exceptions about SendMetadata and Init not existing
    // so the body is only used for WebGL
#if UNITY_WEBGL
    private AgentManager agentManager;

    // private DebugInputField inputField; // inputField.setControlMode no longer used in SetController

    [DllImport("__Internal")]
    private static extern void Init();

    [DllImport("__Internal")]
    private static extern void SendMetadata(string str);

    [DllImport("__Internal")]
    private static extern int GetJsonBufferLength();

    [DllImport("__Internal")]
    private static extern void FreeJsonBuffer(int ptr);

    /*
        metadata: serialized metadata, commonly an instance of MultiAgentMetadata
     */
    public void SendActionMetadata(string metadata)
    {
        SendMetadata(metadata);
        this.agentManager.TransitionStateMachine(AgentState.Emit, AgentState.ActionComplete);
    }

    void Start()
    {
        this.agentManager = GameObject
            .Find("PhysicsSceneManager")
            .GetComponentInChildren<AgentManager>();
        this.agentManager.SetUpPhysicsController();

        // inputField = GameObject.Find("DebugCanvasPhysics").GetComponentInChildren<DebugInputField>();// FindObjectOfType<DebugInputField>();
        // GameObject.Find("DebugCanvas").GetComponentInChildren<AgentManager>();
        Init();

        Debug.Log("JavaScriptInterface end of start");
    }

    public void GetRenderPath()
    {
        SendMetadata("" + GetComponentInChildren<Camera>().actualRenderingPath);
    }

    public void SetController(string controlModeEnumString)
    {
        ControlMode controlMode = (ControlMode)
            Enum.Parse(typeof(ControlMode), controlModeEnumString, true);
        // inputField.setControlMode(controlMode);

        Type componentType;
        var success = PlayerControllers.controlModeToComponent.TryGetValue(
            controlMode,
            out componentType
        );
        var Agent = CurrentActiveController().gameObject;
        if (success)
        {
            var previousComponent = Agent.GetComponent(componentType) as MonoBehaviour;
            if (previousComponent == null)
            {
                previousComponent = Agent.AddComponent(componentType) as MonoBehaviour;
            }
            previousComponent.enabled = true;
        }
    }

    public void Step(string jsonAction)
    {   
        var action = new DynamicServerAction(jsonAction);
        this.agentManager.ProcessControlCommand(action);
        // this.agentManager.TransitionStateMachine(AgentState.ActionComplete, AgentState.Emit);
    }

    public void StepPointer(string ptrStr)
    {
        if (!int.TryParse(ptrStr, out int ptr))
        {
            Debug.LogError("Failed to parse pointer from string");
            return;
        }

        int length = GetJsonBufferLength(); // Read the buffer size

        if (length <= 0)
        {
            Debug.LogError("Buffer length is invalid");
            return;
        }

        byte[] buffer = new byte[length];
        Marshal.Copy((IntPtr)ptr, buffer, 0, length);

        string json = System.Text.Encoding.UTF8.GetString(buffer);
        // Debug.Log($"Pointer {ptr}");
        //  Debug.Log($"Buffer length: {length}");
        // Debug.Log("Received JSON: " + json);

        for (int i = 0; i < Math.Min(length, 20); i++)
            {
                Debug.Log($"Byte {i}: {buffer[i]}");
            }
       

        var action = new DynamicServerAction(json);
        this.agentManager.ProcessControlCommand(action);

        // try
        // {
        //     JObject obj = JsonConvert.DeserializeObject<JObject>(json);
        //     Debug.Log("Successfully parsed JSON: " + obj.ToString());
        // }
        // catch (JsonReaderException e)
        // {
        //     Debug.LogError("JSON parse error: " + e.Message);
        // }

        // Optional: Free memory
        FreeJsonBuffer(ptr);
    }

    private BaseFPSAgentController CurrentActiveController()
    {
        return this.agentManager.PrimaryAgent;
    }

    //  if (this.agentManagerState == AgentState.ActionComplete) {
    //             this.agentManagerState = AgentState.Emit;
    //         }

    //         foreach (BaseFPSAgentController agent in this.agents) {
    //             if (agent.agentState == AgentState.ActionComplete) {
    //                 agent.agentState = AgentState.Emit;
    //             }
    //         }

#endif
}
