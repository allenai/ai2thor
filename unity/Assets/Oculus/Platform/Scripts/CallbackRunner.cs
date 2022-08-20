using System.Runtime.InteropServices;
using UnityEngine;

namespace Oculus.Platform
{
  public class CallbackRunner : MonoBehaviour
  {
    [DllImport(CAPI.DLL_NAME)]
    static extern void ovr_UnityResetTestPlatform();

    public bool IsPersistantBetweenSceneLoads = true;

    void Awake()
    {
      var existingCallbackRunner = FindObjectOfType<CallbackRunner>();
      if (existingCallbackRunner != this)
      {
        Debug.LogWarning("You only need one instance of CallbackRunner");
      }
      if (IsPersistantBetweenSceneLoads)
      {
        DontDestroyOnLoad(gameObject);
      }
    }

    void Update()
    {
      Request.RunCallbacks();
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
      ovr_UnityResetTestPlatform();
#endif
    }

    void OnApplicationQuit()
    {
      Callback.OnApplicationQuit();
    }
  }
}
