// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RosterOptions {

    public RosterOptions() {
      Handle = CAPI.ovr_RosterOptions_Create();
    }

    /// Passing in these users will add them to the invitable users list. From the
    /// roster panel, the user can open the invite list, where the suggested users
    /// will be added.
    public void AddSuggestedUser(UInt64 userID) {
      CAPI.ovr_RosterOptions_AddSuggestedUser(Handle, userID);
    }

    public void ClearSuggestedUsers() {
      CAPI.ovr_RosterOptions_ClearSuggestedUsers(Handle);
    }


    /// For passing to native C
    public static explicit operator IntPtr(RosterOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~RosterOptions() {
      CAPI.ovr_RosterOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
