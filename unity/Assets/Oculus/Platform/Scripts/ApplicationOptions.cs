// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class ApplicationOptions {

    public ApplicationOptions() {
      Handle = CAPI.ovr_ApplicationOptions_Create();
    }

    /// A message to be passed to a launched app, which can be retrieved with
    /// LaunchDetails.GetDeeplinkMessage()
    public void SetDeeplinkMessage(string value) {
      CAPI.ovr_ApplicationOptions_SetDeeplinkMessage(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(ApplicationOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~ApplicationOptions() {
      CAPI.ovr_ApplicationOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
