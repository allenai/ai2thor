namespace Oculus.Platform
{
  using UnityEngine;
  using System.Collections;
  using System;

  public class AndroidPlatform
  {
    public bool Initialize(string appId)
    {
#if UNITY_ANDROID
      if(String.IsNullOrEmpty(appId))
      {
        throw new UnityException("AppID must not be null or empty");
      }
      return CAPI.ovr_UnityInitWrapper(appId);
#else
      return false;
#endif
    }

    public Request<Models.PlatformInitialize> AsyncInitialize(string appId)
    {
#if UNITY_ANDROID
      if(String.IsNullOrEmpty(appId))
      {
        throw new UnityException("AppID must not be null or empty");
      }
      return new Request<Models.PlatformInitialize>(CAPI.ovr_UnityInitWrapperAsynchronous(appId));
#else
      return new Request<Models.PlatformInitialize>(0);
#endif
    }
  }
}
