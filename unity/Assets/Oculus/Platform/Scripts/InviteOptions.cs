// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class InviteOptions {

    public InviteOptions() {
      Handle = CAPI.ovr_InviteOptions_Create();
    }

    /// Passing in these users will add them to the invitable users list
    public void AddSuggestedUser(UInt64 userID) {
      CAPI.ovr_InviteOptions_AddSuggestedUser(Handle, userID);
    }

    public void ClearSuggestedUsers() {
      CAPI.ovr_InviteOptions_ClearSuggestedUsers(Handle);
    }


    /// For passing to native C
    public static explicit operator IntPtr(InviteOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~InviteOptions() {
      CAPI.ovr_InviteOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
