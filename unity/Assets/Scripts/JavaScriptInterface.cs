using UnityEngine;
using System.Runtime.InteropServices;


public class JavaScriptInterface : MonoBehaviour {

    [DllImport("__Internal")]
    private static extern void Init();

    [DllImport("__Internal")]
    private static extern void AddEvent(string str);

    public void SendAction(ServerAction action)
    {
        AddEvent(JsonUtility.ToJson(action));
    }

    void Start()
    {
        Init();

        int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        string[] scenes = new string[sceneCount];
        for (int i = 0; i < sceneCount; i++)
        {
            scenes[i] = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
        }

        Debug.Log("Calling store data");
    }
}
