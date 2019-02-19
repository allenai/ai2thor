using UnityEngine;
using System.Runtime.InteropServices;


public class JavaScriptInterface : MonoBehaviour {

    [DllImport("__Internal")]
    private static extern void Hello();

    [DllImport("__Internal")]
    private static extern void HelloString(string str);

    [DllImport("__Internal")]
    private static extern void StoreData(string str);

    public void SendAction(ServerAction action)
    {
        StoreData(JsonUtility.ToJson(action));
    }

    void Start()
    {
        Hello();

        HelloString("This is a string.");

        Debug.Log("Calling store data");

        StoreData("test");
    }
}
