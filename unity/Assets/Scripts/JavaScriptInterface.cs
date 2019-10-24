using UnityEngine;
using System.Runtime.InteropServices;
using UnityStandardAssets.Characters.FirstPerson;


public class JavaScriptInterface : MonoBehaviour {

    private PhysicsRemoteFPSAgentController PhysicsController;

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
        Init();

        Debug.Log("Calling store data");
    }

     public void Step(string serverAction)
		{
			ServerAction controlCommand = new ServerAction();
			JsonUtility.FromJsonOverwrite(serverAction, controlCommand);
			PhysicsController.ProcessControlCommand(controlCommand);
		}
}