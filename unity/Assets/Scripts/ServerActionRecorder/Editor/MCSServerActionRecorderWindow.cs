using UnityEditor;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class MCSServerActionRecorderWindow : EditorWindow
{
    private const string FILE_ERROR_MSG = "\nCheck recordings folder: \nStreamingAssets/Recordings";

    private string source = "";
    private bool playbackPending = false;
    private bool fileLoaded = false;
    private RecordedServerActions recordedServerActions = null;

    [MenuItem("Tools/ServerAction Recorder")]
    public static void ShowWindow()
    {
        var window = GetWindow<MCSServerActionRecorderWindow>(false, "ServerAction Recorder", true);

        window.maxSize = new Vector2(1000f, 500f);
        window.minSize = window.maxSize;
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        DrawPathWindow();
        EditorGUILayout.Space();

        // Enable Load button when there is no file loaded
        EditorGUI.BeginDisabledGroup(fileLoaded == true);
        DrawLoadButton();
        EditorGUI.EndDisabledGroup();

        // Enable Play button when there is a file loaded
        EditorGUI.BeginDisabledGroup(fileLoaded == false);
        DrawPlayButton();
        EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();
    }

    private void DrawPathWindow()
    {
        EditorGUILayout.LabelField("Recorded Actions:", EditorStyles.boldLabel);

        // Unload current file if a new filename has been entered
        var currentEntry = EditorGUILayout.TextField("File Name", source);
        if (source != currentEntry)
        {
            fileLoaded = false;
            source = currentEntry;
        }
    }

    private void DrawLoadButton()
    {
        if (GUILayout.Button("Load"))
        {
            if (source == null)
            {
                ShowNotification(new GUIContent("No recorded actions selected!"));
                fileLoaded = false;
            }
            else
            {
                try
                {
                    recordedServerActions = MCSServerActionRecorder.GetServerActionsFromFile(source);
                    if (recordedServerActions != null)
                    {
                        fileLoaded = true;
                    }
                    else
                    {
                        ShowNotification(new GUIContent("File could not be loaded!"));
                        fileLoaded = false;
                    }
                }
                catch (System.IO.FileNotFoundException fex)
                {
                    ShowNotification(new GUIContent("File could not be found!" + FILE_ERROR_MSG));
                    Debug.LogError(fex);
                }
            }
        }
    }

    private void DrawPlayButton()
    {
        EditorGUILayout.LabelField("LoadedFile: " + (fileLoaded ? source : "None"), EditorStyles.boldLabel);
        if (GUILayout.Button("Play"))
        {
            EditorApplication.ExecuteMenuItem("Edit/Play");
            playbackPending = true;
        }

        if (playbackPending && Application.isPlaying)
        {
            playbackPending = false;
            MCSServerActionRecorder.RunRecordedActions(recordedServerActions);
        }
    }
}