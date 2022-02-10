using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Utility for recording runtime ServerActions as they are performed. 
/// ServerActions are recorded with timestamps then saved to a file which can then be loaded through the toolbar for playback [Tools -> ServerAction Recorder]
/// </summary>
public class MCSServerActionRecorder : MonoBehaviour
{
    private static string RECORDING_PATH = "Recordings/";
    private static List<string> RECORDABLE_ACTIONS = new List<string>
    {
        "Initialize",

        "FlyAhead",
        "FlyBack",
        "FlyLeft",
        "FlyRight",
        "FlyUp",
        "FlyDown",

        "MoveAhead",
        "MoveBack",
        "MoveLeft",
        "MoveRight",

        "LookUp",
        "LookDown",
        
        "RotateLeft",
        "RotateRight",
        "RotateLook",
        
        "Crawl",
        "Stand",
        "LieDown",

        "OpenObject",
        "CloseObject",
        "PutObject",
        "ThrowObject",
        "PushObject",
        "PullObject",
        "PickupObject",
        "DropHandObject",
        "TorqueObject",
        "RotateObject",
        
        "LaunchDroneObject",
        "Pass",

        "EndHabituation",
    };

    // Lock is placed in anticipation of possibly networking access to this object, this may not even be required
    private static object instanceLock = new object();
    private static MCSServerActionRecorder instance;

    public static MCSServerActionRecorder Instance
    {
        get
        {
            lock (instanceLock)
            {
                if (instance == null)
                {
                    // Search for existing instance.
                    instance = (MCSServerActionRecorder)FindObjectOfType(typeof(MCSServerActionRecorder));

                    // Create new instance if one doesn't already exist.
                    if (instance == null)
                    {
                        // Need to create a new GameObject to attach the singleton to.
                        var singletonObject = new GameObject();
                        instance = singletonObject.AddComponent<MCSServerActionRecorder>();
                        singletonObject.name = typeof(MCSServerActionRecorder).ToString();

                        // Make instance persistent.
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return instance;
            }
        }
    }

    private List<RecordedServerAction> recordedActions = new List<RecordedServerAction>();
    private static bool inPlayback = false;

    public static void RunRecordedActions(RecordedServerActions recordedServerActions)
    {
        inPlayback = true;
        Instance.StartCoroutine(PlaybackRecordedActions(recordedServerActions));
    }

    public void RecordAction(ServerAction action)
    {
        // Do not record actions when playing back a recorded file
        if (inPlayback)
            return;

        // Only record actions flagged as recordable
        if (RECORDABLE_ACTIONS.Contains(action.action))
        {
            // The default initialize does not store the scene config data, so lets manually add the data to record it
            if (action.action == "Initialize")
            {
                var mcs = GameObject.FindObjectOfType<MCSMain>();

                action.sceneConfig = new MCSConfigScene();
                action.sceneConfig.name = mcs.GetCurrentSceneName();
            }
            recordedActions.Add(new RecordedServerAction(Time.time, action));
        }
    }

    public void OnApplicationQuit()
    {
        // Do not record actions when playing back a recorded file
        if (inPlayback)
            return;

        // Create our data for writing to file
        var mcs = FindObjectOfType<MCSMain>();
        var recordedServerActions = new RecordedServerActions(recordedActions);
        var json = JsonUtility.ToJson(recordedServerActions);
        var fileName = "RecordedActions" + "_" + DateTime.Now.ToString("MMddyy_HHmmss") + ".json";
        WriteRecordingsToFile(json, fileName);

        Debug.Log("Recording " + fileName + " was saved!");
    }

    private static IEnumerator PlaybackRecordedActions(RecordedServerActions recordedServerActions)
    {
        // Initialize playback
        inPlayback = true;
        var currentStep = 0;

        var physicsController = FindObjectOfType<UnityStandardAssets.Characters.FirstPerson.PhysicsRemoteFPSAgentController>();

        // Playback recorded actions per timestep they were performed
        while (currentStep < recordedServerActions.serverActions.Count - 1)
        {
            if (Time.time >= recordedServerActions.serverActions[currentStep].time)
            {
                if (recordedServerActions.serverActions[currentStep].action.action == "Initialize")
                {
                    var mcs = FindObjectOfType<MCSMain>();
                    recordedServerActions.serverActions[currentStep].action.sceneConfig = MCSMain.LoadCurrentSceneFromFile(recordedServerActions.serverActions[currentStep].action.sceneConfig.name);
                }
                physicsController.ProcessControlCommand(recordedServerActions.serverActions[currentStep].action);
                currentStep += 1;
            }
            yield return null;
        }

        Debug.Log("Playback Complete!");
    }

    public static RecordedServerActions GetServerActionsFromFile(string fileName)
    {
        // Load the recording file
        var filePath = Path.Combine(Application.streamingAssetsPath, RECORDING_PATH + fileName + ".json");
        var result = File.ReadAllText(filePath);

        // Deserialize the JSON for playback
        return JsonUtility.FromJson<RecordedServerActions>(result);
    }

    public void WriteRecordingsToFile(string jsonString, string fileName)
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, RECORDING_PATH + fileName);

        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
            File.WriteAllText(filePath, jsonString);
        }
        else
        {
            File.WriteAllText(filePath, jsonString);
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}

[Serializable]
public class RecordedServerActions
{
    public List<RecordedServerAction> serverActions;

    public RecordedServerActions(List<RecordedServerAction> serverActions)
    {
        this.serverActions = serverActions;
    }
}

[Serializable]
public class RecordedServerAction
{
    public float time;
    public ServerAction action;

    public RecordedServerAction(float time, ServerAction action)
    {
        this.time = time;
        this.action = action;
    }
}
