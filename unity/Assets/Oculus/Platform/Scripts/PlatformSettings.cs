namespace Oculus.Platform
{
  using UnityEngine;
  using System.Collections;

#if UNITY_EDITOR
  [UnityEditor.InitializeOnLoad]
#endif
  public sealed class PlatformSettings : ScriptableObject
  {
    public static string AppID
    {
      get { return Instance.ovrAppID; }
      set { Instance.ovrAppID = value; }
    }

    public static string MobileAppID
    {
      get { return Instance.ovrMobileAppID; }
      set { Instance.ovrMobileAppID = value; }
    }

    public static bool UseStandalonePlatform
    {
      get { return Instance.ovrUseStandalonePlatform; }
      set { Instance.ovrUseStandalonePlatform = value; }
    }

    [SerializeField]
    private string ovrAppID = "";

    [SerializeField]
    private string ovrMobileAppID = "";

#if UNITY_EDITOR_WIN
    [SerializeField]
    private bool ovrUseStandalonePlatform = false;
#else
    [SerializeField]
    private bool ovrUseStandalonePlatform = true;
#endif
    
    private static PlatformSettings instance;
    public static PlatformSettings Instance
    {
      get
      {
        if (instance == null)
        {
          instance = Resources.Load<PlatformSettings>("OculusPlatformSettings");

          // This can happen if the developer never input their App Id into the Unity Editor
          // and therefore never created the OculusPlatformSettings.asset file
          // Use a dummy object with defaults for the getters so we don't have a null pointer exception
          if (instance == null)
          {
            instance = ScriptableObject.CreateInstance<PlatformSettings>();

#if UNITY_EDITOR
            // Only in the editor should we save it to disk
            string properPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources");
            if (!System.IO.Directory.Exists(properPath))
            {
              UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string fullPath = System.IO.Path.Combine(
              System.IO.Path.Combine("Assets", "Resources"),
              "OculusPlatformSettings.asset"
            );
            UnityEditor.AssetDatabase.CreateAsset(instance, fullPath);
#endif
          }
        }
        return instance;
      }

      set
      {
        instance = value;
      }
    }
  }
}
