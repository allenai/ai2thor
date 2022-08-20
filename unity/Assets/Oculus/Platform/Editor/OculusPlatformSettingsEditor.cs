#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS
#define USING_XR_SDK
#endif

namespace Oculus.Platform
{
  using System;
  using System.IO;
  using UnityEditor;
  using UnityEngine;
  using UnityEngine.Networking;

  // This classes implements a UI to edit the PlatformSettings class.
  // The UI is accessible from a the menu bar via: Oculus Platform -> Edit Settings
  [CustomEditor(typeof(PlatformSettings))]
  public class OculusPlatformSettingsEditor : Editor
  {
    private bool isUnityEditorSettingsExpanded;
    private bool isBuildSettingsExpanded;

    private UnityWebRequest getAccessTokenRequest;

    private void OnEnable()
    {
      isUnityEditorSettingsExpanded = true;
      isBuildSettingsExpanded = true;
    }

    [UnityEditor.MenuItem("Oculus/Platform/Edit Settings")]
    public static void Edit()
    {
      UnityEditor.Selection.activeObject = PlatformSettings.Instance;
    }

    public override void OnInspectorGUI()
    {
      //
      // Application IDs section
      //
      EditorGUILayout.LabelField("Application ID:");
      GUIContent riftAppIDLabel = new GUIContent("Oculus Rift [?]", "This AppID will be used when building to the Windows target.");
      GUIContent mobileAppIDLabel = new GUIContent("Oculus Go/Quest or Gear VR [?]", "This AppID will be used when building to the Android target");
      PlatformSettings.AppID = MakeTextBox(riftAppIDLabel, PlatformSettings.AppID);
      PlatformSettings.MobileAppID = MakeTextBox(mobileAppIDLabel, PlatformSettings.MobileAppID);

      if (GUILayout.Button("Create / Find your app on https://dashboard.oculus.com"))
      {
        UnityEngine.Application.OpenURL("https://dashboard.oculus.com/");
      }

#if UNITY_ANDROID
      if (String.IsNullOrEmpty(PlatformSettings.MobileAppID))
      {
        EditorGUILayout.HelpBox("Please enter a valid Oculus Go/Quest or Gear VR App ID.", MessageType.Error);
      }
      else
      {
        var msg = "Configured to connect with App ID " + PlatformSettings.MobileAppID;
        EditorGUILayout.HelpBox(msg, MessageType.Info);
      }
#else
      if (String.IsNullOrEmpty(PlatformSettings.AppID))
      {
        EditorGUILayout.HelpBox("Please enter a valid Oculus Rift App ID.", MessageType.Error);
      }
      else
      {
        var msg = "Configured to connect with App ID " + PlatformSettings.AppID;
        EditorGUILayout.HelpBox(msg, MessageType.Info);
      }
#endif
      EditorGUILayout.Separator();

      //
      // Unity Editor Settings section
      //
      isUnityEditorSettingsExpanded = EditorGUILayout.Foldout(isUnityEditorSettingsExpanded, "Unity Editor Settings");
      if (isUnityEditorSettingsExpanded)
      {
        GUIHelper.HInset(6, () =>
        {
          bool HasTestAccessToken = !String.IsNullOrEmpty(StandalonePlatformSettings.OculusPlatformTestUserAccessToken);
          if (PlatformSettings.UseStandalonePlatform)
          {
            if (!HasTestAccessToken &&
            (String.IsNullOrEmpty(StandalonePlatformSettings.OculusPlatformTestUserEmail) ||
            String.IsNullOrEmpty(StandalonePlatformSettings.OculusPlatformTestUserPassword)))
            {
              EditorGUILayout.HelpBox("Please enter a valid user credentials.", MessageType.Error);
            }
            else
            {
              var msg = "The Unity editor will use the supplied test user credentials and operate in standalone mode.  Some user data will be mocked.";
              EditorGUILayout.HelpBox(msg, MessageType.Info);
            }
          }
          else
          {
            var msg = "The Unity editor will use the user credentials from the Oculus application.";
            EditorGUILayout.HelpBox(msg, MessageType.Info);
          }

          var useStandaloneLabel = "Use Standalone Platform [?]";
          var useStandaloneHint = "If this is checked your app will use a debug platform with the User info below.  "
            + "Otherwise your app will connect to the Oculus Platform.  This setting only applies to the Unity Editor on Windows";

          PlatformSettings.UseStandalonePlatform =
            MakeToggle(new GUIContent(useStandaloneLabel, useStandaloneHint), PlatformSettings.UseStandalonePlatform);

          GUI.enabled = PlatformSettings.UseStandalonePlatform;

          if (!HasTestAccessToken)
          {
            var emailLabel = "Test User Email: ";
            var emailHint = "Test users can be configured at " +
              "https://dashboard.oculus.com/organizations/<your org ID>/testusers " +
              "however any valid Oculus account email may be used.";
            StandalonePlatformSettings.OculusPlatformTestUserEmail =
              MakeTextBox(new GUIContent(emailLabel, emailHint), StandalonePlatformSettings.OculusPlatformTestUserEmail);

            var passwdLabel = "Test User Password: ";
            var passwdHint = "Password associated with the email address.";
            StandalonePlatformSettings.OculusPlatformTestUserPassword =
              MakePasswordBox(new GUIContent(passwdLabel, passwdHint), StandalonePlatformSettings.OculusPlatformTestUserPassword);

            var isLoggingIn = (getAccessTokenRequest != null);
            var loginLabel = (!isLoggingIn) ? "Login" : "Logging in...";

            GUI.enabled = !isLoggingIn;
            if (GUILayout.Button(loginLabel))
            {
              WWWForm form = new WWWForm();
              form.AddField("email", StandalonePlatformSettings.OculusPlatformTestUserEmail);
              form.AddField("password", StandalonePlatformSettings.OculusPlatformTestUserPassword);

              // Start the WWW request to get the access token
              getAccessTokenRequest = UnityWebRequest.Post("https://graph.oculus.com/login", form);
              getAccessTokenRequest.SetRequestHeader("Authorization", "Bearer OC|1141595335965881|");
              getAccessTokenRequest.SendWebRequest();
              EditorApplication.update += GetAccessToken;
            }
            GUI.enabled = true;
          }
          else
          {
            var loggedInMsg = "Currently using the credentials associated with " + StandalonePlatformSettings.OculusPlatformTestUserEmail;
            EditorGUILayout.HelpBox(loggedInMsg, MessageType.Info);

            var logoutLabel = "Clear Credentials";

            if (GUILayout.Button(logoutLabel))
            {
              StandalonePlatformSettings.OculusPlatformTestUserAccessToken = "";
            }
          }

          GUI.enabled = true;
        });
      }
      EditorGUILayout.Separator();

      //
      // Build Settings section
      //
      isBuildSettingsExpanded = EditorGUILayout.Foldout(isBuildSettingsExpanded, "Build Settings");
      if (isBuildSettingsExpanded)
      {
        GUIHelper.HInset(6, () => {
#if !USING_XR_SDK
#if UNITY_2020_1_OR_NEWER
          EditorGUILayout.HelpBox("The Oculus XR Plugin isn't enabled from XR Plugin Management in Project Settings", MessageType.Warning);
#else
          if (!PlayerSettings.virtualRealitySupported)
          {
            EditorGUILayout.HelpBox("VR Support isn't enabled in the Player Settings", MessageType.Warning);
          }

          PlayerSettings.virtualRealitySupported = MakeToggle(new GUIContent("Virtual Reality Support"), PlayerSettings.virtualRealitySupported);
#endif
#endif

          PlayerSettings.bundleVersion = MakeTextBox(new GUIContent("Bundle Version"), PlayerSettings.bundleVersion);
#if UNITY_5_3 || UNITY_5_4 || UNITY_5_5
          PlayerSettings.bundleIdentifier = MakeTextBox(new GUIContent("Bundle Identifier"), PlayerSettings.bundleIdentifier);
#else
          BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
          PlayerSettings.SetApplicationIdentifier(
            buildTargetGroup,
            MakeTextBox(
              new GUIContent("Bundle Identifier"),
              PlayerSettings.GetApplicationIdentifier(buildTargetGroup)));
#endif
          bool canEnableARM64Support = false;
#if UNITY_2018_1_OR_NEWER
          canEnableARM64Support = true;
#endif
          if (!canEnableARM64Support)
          {
            var msg = "Update your Unity Editor to 2018.1.x or newer to enable Arm64 support";
            EditorGUILayout.HelpBox(msg, MessageType.Warning);
            if (IsArm64PluginPlatformEnabled())
            {
              DisablePluginPlatform(PluginPlatform.Android64);
            }
          }
          else
          {
            if (!IsArm64PluginPlatformEnabled())
            {
              EnablePluginPlatform(PluginPlatform.Android64);
            }
          }

          GUI.enabled = true;
        });
      }
      EditorGUILayout.Separator();
    }

    // Asyncronously fetch the access token with the given credentials
    private void GetAccessToken()
    {
      if (getAccessTokenRequest != null && getAccessTokenRequest.isDone)
      {
        // Clear the password
        StandalonePlatformSettings.OculusPlatformTestUserPassword = "";

        if (String.IsNullOrEmpty(getAccessTokenRequest.error))
        {
          var Response = JsonUtility.FromJson<OculusStandalonePlatformResponse>(getAccessTokenRequest.downloadHandler.text);
          StandalonePlatformSettings.OculusPlatformTestUserAccessToken = Response.access_token;
        }

        GUI.changed = true;
        EditorApplication.update -= GetAccessToken;
        getAccessTokenRequest.Dispose();
        getAccessTokenRequest = null;
      }
    }

    private string MakeTextBox(GUIContent label, string variable)
    {
      return GUIHelper.MakeControlWithLabel(label, () => {
        GUI.changed = false;
        var result = EditorGUILayout.TextField(variable);
        SetDirtyOnGUIChange();
        return result;
      });
    }

    private string MakePasswordBox(GUIContent label, string variable)
    {
      return GUIHelper.MakeControlWithLabel(label, () => {
        GUI.changed = false;
        var result = EditorGUILayout.PasswordField(variable);
        SetDirtyOnGUIChange();
        return result;
      });
    }

    private bool MakeToggle(GUIContent label, bool variable)
    {
      return GUIHelper.MakeControlWithLabel(label, () => {
        GUI.changed = false;
        var result = EditorGUILayout.Toggle(variable);
        SetDirtyOnGUIChange();
        return result;
      });
    }

    private void SetDirtyOnGUIChange()
    {
      if (GUI.changed)
      {
        EditorUtility.SetDirty(PlatformSettings.Instance);
        GUI.changed = false;
      }
    }

    // TODO: Merge this with core utilities plugin updater functionality. Piggybacking here to avoid an orphaned delete in the future.
    private const string PluginSubPathAndroid32 = @"/Plugins/Android32/libovrplatformloader.so";
    private const string PluginSubPathAndroid64 = @"/Plugins/Android64/libovrplatformloader.so";
    private const string PluginDisabledSuffix = @".disabled";

    public enum PluginPlatform
    {
      Android32,
      Android64
    }

    private static string GetCurrentProjectPath()
    {
      return Directory.GetParent(UnityEngine.Application.dataPath).FullName;
    }

    private static string GetPlatformRootPath()
    {
      // use the path to OculusPluginUpdaterStub as a relative path anchor point
      var so = ScriptableObject.CreateInstance(typeof(OculusPluginUpdaterStub));
      var script = MonoScript.FromScriptableObject(so);
      string assetPath = AssetDatabase.GetAssetPath(script);
      string editorDir = Directory.GetParent(assetPath).FullName;
      string platformDir = Directory.GetParent(editorDir).FullName;

      return platformDir;
    }

    private static string GetPlatformPluginPath(PluginPlatform platform)
    {
      string path = GetPlatformRootPath();
      switch (platform)
      {
        case PluginPlatform.Android32:
          path += PluginSubPathAndroid32;
          break;
        case PluginPlatform.Android64:
          path += PluginSubPathAndroid64;
          break;
        default:
          throw new ArgumentException("Attempted to enable platform support for unsupported platform: " + platform);
      }

      return path;
    }

    //[UnityEditor.MenuItem("Oculus/Platform/EnforcePluginPlatformSettings")]
    public static void EnforcePluginPlatformSettings()
    {
      EnforcePluginPlatformSettings(PluginPlatform.Android32);
      EnforcePluginPlatformSettings(PluginPlatform.Android64);
    }

    public static void EnforcePluginPlatformSettings(PluginPlatform platform)
    {
      string path = GetPlatformPluginPath(platform);

      if (!Directory.Exists(path) && !File.Exists(path))
      {
        path += PluginDisabledSuffix;
      }

      if ((Directory.Exists(path)) || (File.Exists(path)))
      {
        string basePath = GetCurrentProjectPath();
        string relPath = path.Substring(basePath.Length + 1);

        PluginImporter pi = PluginImporter.GetAtPath(relPath) as PluginImporter;
        if (pi == null)
        {
          return;
        }

        // Disable support for all platforms, then conditionally enable desired support below
        pi.SetCompatibleWithEditor(false);
        pi.SetCompatibleWithAnyPlatform(false);
        pi.SetCompatibleWithPlatform(BuildTarget.Android, false);
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
#if !UNITY_2019_2_OR_NEWER
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux, false);
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneLinuxUniversal, false);
#endif
#if UNITY_2017_3_OR_NEWER
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
#else
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneOSXUniversal, false);
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneOSXIntel, false);
        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneOSXIntel64, false);
#endif

        switch (platform)
        {
          case PluginPlatform.Android32:
            pi.SetCompatibleWithPlatform(BuildTarget.Android, true);
            pi.SetPlatformData(BuildTarget.Android, "CPU", "ARMv7");
            break;
          case PluginPlatform.Android64:
            pi.SetCompatibleWithPlatform(BuildTarget.Android, true);
            pi.SetPlatformData(BuildTarget.Android, "CPU", "ARM64");
            break;
          default:
            throw new ArgumentException("Attempted to enable platform support for unsupported platform: " + platform);
        }

        AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
      }
    }

    public static bool IsArm64PluginPlatformEnabled()
    {
      string path = GetPlatformPluginPath(PluginPlatform.Android64);
      bool pathAlreadyExists = Directory.Exists(path) || File.Exists(path);
      return pathAlreadyExists;
    }

    public static void EnablePluginPlatform(PluginPlatform platform)
    {
      string path = GetPlatformPluginPath(platform);
      string disabledPath = path + PluginDisabledSuffix;

      bool pathAlreadyExists = Directory.Exists(path) || File.Exists(path);
      bool disabledPathDoesNotExist = !Directory.Exists(disabledPath) && !File.Exists(disabledPath);

      if (pathAlreadyExists || disabledPathDoesNotExist)
      {
        return;
      }

      string basePath = GetCurrentProjectPath();
      string relPath = path.Substring(basePath.Length + 1);
      string relDisabledPath = relPath + PluginDisabledSuffix;

      AssetDatabase.MoveAsset(relDisabledPath, relPath);
      AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      AssetDatabase.SaveAssets();

      // Force reserialization of platform settings meta data
      EnforcePluginPlatformSettings(platform);
    }

    public static void DisablePluginPlatform(PluginPlatform platform)
    {
      string path = GetPlatformPluginPath(platform);
      string disabledPath = path + PluginDisabledSuffix;

      bool pathDoesNotExist = !Directory.Exists(path) && !File.Exists(path);
      bool disabledPathAlreadyExists = Directory.Exists(disabledPath) || File.Exists(disabledPath);

      if (pathDoesNotExist || disabledPathAlreadyExists)
      {
        return;
      }

      string basePath = GetCurrentProjectPath();
      string relPath = path.Substring(basePath.Length + 1);
      string relDisabledPath = relPath + PluginDisabledSuffix;

      AssetDatabase.MoveAsset(relPath, relDisabledPath);
      AssetDatabase.ImportAsset(relDisabledPath, ImportAssetOptions.ForceUpdate);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      AssetDatabase.SaveAssets();
    }
  }
}
