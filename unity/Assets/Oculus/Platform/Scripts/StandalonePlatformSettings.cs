namespace Oculus.Platform
{

  // This only exists for the Unity Editor
  public sealed class StandalonePlatformSettings
  {

#if UNITY_EDITOR
    private static string _OculusPlatformTestUserPassword = "";

    private static void ClearOldStoredPassword()
    {
      // Ensure that we are not storing the old passwords anywhere on the machine
      if (UnityEditor.EditorPrefs.HasKey("OculusStandaloneUserPassword"))
      {
        UnityEditor.EditorPrefs.SetString("OculusStandaloneUserPassword", "0000");
        UnityEditor.EditorPrefs.DeleteKey("OculusStandaloneUserPassword");
      }
    }
#endif

    public static string OculusPlatformTestUserEmail
    {
      get
      {
#if UNITY_EDITOR
        return UnityEditor.EditorPrefs.GetString("OculusStandaloneUserEmail");
#else
        return string.Empty;
#endif
      }
      set
      {
#if UNITY_EDITOR
        UnityEditor.EditorPrefs.SetString("OculusStandaloneUserEmail", value);
#endif
      }
    }

    public static string OculusPlatformTestUserPassword
    {
      get
      {
#if UNITY_EDITOR
        ClearOldStoredPassword();
        return _OculusPlatformTestUserPassword;
#else
        return string.Empty;
#endif
      }
      set
      {
#if UNITY_EDITOR
        ClearOldStoredPassword();
        _OculusPlatformTestUserPassword = value;
#endif
      }
    }
    public static string OculusPlatformTestUserAccessToken
    {
      get
      {
#if UNITY_EDITOR
        return UnityEditor.EditorPrefs.GetString("OculusStandaloneUserAccessToken");
#else
        return string.Empty;
#endif
      }
      set
      {
#if UNITY_EDITOR
        UnityEditor.EditorPrefs.SetString("OculusStandaloneUserAccessToken", value);
#endif
      }
    }
  }
}
