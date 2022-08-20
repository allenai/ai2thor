namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Runtime.InteropServices;

  public sealed class StandalonePlatform
  {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UnityLogDelegate(IntPtr tag, IntPtr msg);

    public Request<Models.PlatformInitialize> InitializeInEditor()
    {
#if UNITY_ANDROID
      if (String.IsNullOrEmpty(PlatformSettings.MobileAppID))
      {
        throw new UnityException("Update your App ID by selecting 'Oculus Platform' -> 'Edit Settings'");
      }
      var appID = PlatformSettings.MobileAppID;
#else
      if (String.IsNullOrEmpty(PlatformSettings.AppID))
      {
        throw new UnityException("Update your App ID by selecting 'Oculus Platform' -> 'Edit Settings'");
      }
      var appID = PlatformSettings.AppID;
#endif
      if (String.IsNullOrEmpty(StandalonePlatformSettings.OculusPlatformTestUserAccessToken))
      {
        throw new UnityException("Update your standalone credentials by selecting 'Oculus Platform' -> 'Edit Settings'");
      }
      var accessToken = StandalonePlatformSettings.OculusPlatformTestUserAccessToken;

      return AsyncInitialize(UInt64.Parse(appID), accessToken);
    }

    public Request<Models.PlatformInitialize> AsyncInitialize(ulong appID, string accessToken)
    {
      CAPI.ovr_UnityResetTestPlatform();
      CAPI.ovr_UnityInitGlobals(IntPtr.Zero);

      return new Request<Models.PlatformInitialize>(CAPI.ovr_PlatformInitializeWithAccessToken(appID, accessToken));
    }
  }
}
