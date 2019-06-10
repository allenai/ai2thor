using UnityEngine;
using System.Runtime.InteropServices;


public class JavaScriptInterface : MonoBehaviour {

    [DllImport("__Internal")]
    private static extern void Init();

    [DllImport("__Internal")]
    private static extern void SendEvent(string str);

    public void SendAction(ServerAction action)
    {
        SendEvent(JsonUtility.ToJson(action));
    }

    void Start()
    {
        Init();

        Debug.Log("Calling store data");
    }
}
