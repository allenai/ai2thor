// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MultiplayerErrorOptions {

    public MultiplayerErrorOptions() {
      Handle = CAPI.ovr_MultiplayerErrorOptions_Create();
    }

    /// Key associated with the predefined error message to be shown to users.
    public void SetErrorKey(MultiplayerErrorErrorKey value) {
      CAPI.ovr_MultiplayerErrorOptions_SetErrorKey(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(MultiplayerErrorOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~MultiplayerErrorOptions() {
      CAPI.ovr_MultiplayerErrorOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
