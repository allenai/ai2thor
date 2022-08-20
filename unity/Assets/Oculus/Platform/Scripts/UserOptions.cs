// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class UserOptions {

    public UserOptions() {
      Handle = CAPI.ovr_UserOptions_Create();
    }

    public void SetMaxUsers(uint value) {
      CAPI.ovr_UserOptions_SetMaxUsers(Handle, value);
    }

    public void AddServiceProvider(ServiceProvider value) {
      CAPI.ovr_UserOptions_AddServiceProvider(Handle, value);
    }

    public void ClearServiceProviders() {
      CAPI.ovr_UserOptions_ClearServiceProviders(Handle);
    }

    public void SetTimeWindow(TimeWindow value) {
      CAPI.ovr_UserOptions_SetTimeWindow(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(UserOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~UserOptions() {
      CAPI.ovr_UserOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
