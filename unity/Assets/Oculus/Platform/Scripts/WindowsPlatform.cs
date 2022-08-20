namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Runtime.InteropServices;

  public class WindowsPlatform
  {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UnityLogDelegate(IntPtr tag, IntPtr msg);

    void CPPLogCallback(IntPtr tag, IntPtr message)
    {
      Debug.Log(string.Format("{0}: {1}", Marshal.PtrToStringAnsi(tag), Marshal.PtrToStringAnsi(message)));
    }

    IntPtr getCallbackPointer()
    {
            //UnityLogDelegate callback_delegate = new UnityLogDelegate(CPPLogCallback);
            //IntPtr intptr_delegate = Marshal.GetFunctionPointerForDelegate(callback_delegate);
            return IntPtr.Zero;
    }

    public bool Initialize(string appId)
    {
      if(String.IsNullOrEmpty(appId))
      {
        throw new UnityException("AppID must not be null or empty");
      }

      CAPI.ovr_UnityInitWrapperWindows(appId, getCallbackPointer());
      return true;
    }

    public Request<Models.PlatformInitialize> AsyncInitialize(string appId)
    {
      if(String.IsNullOrEmpty(appId))
      {
        throw new UnityException("AppID must not be null or empty");
      }

      return new Request<Models.PlatformInitialize>(CAPI.ovr_UnityInitWrapperWindowsAsynchronous(appId, getCallbackPointer()));
    }
  }
}
