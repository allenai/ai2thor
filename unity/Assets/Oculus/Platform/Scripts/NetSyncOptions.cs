// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class NetSyncOptions {

    public NetSyncOptions() {
      Handle = CAPI.ovr_NetSyncOptions_Create();
    }

    /// If provided, immediately set the voip_group to this value upon connection
    public void SetVoipGroup(string value) {
      CAPI.ovr_NetSyncOptions_SetVoipGroup(Handle, value);
    }

    /// When a new remote voip user connects, default that connection to this
    /// stream type by default.
    public void SetVoipStreamDefault(NetSyncVoipStreamMode value) {
      CAPI.ovr_NetSyncOptions_SetVoipStreamDefault(Handle, value);
    }

    /// Unique identifier within the current application grouping
    public void SetZoneId(string value) {
      CAPI.ovr_NetSyncOptions_SetZoneId(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(NetSyncOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~NetSyncOptions() {
      CAPI.ovr_NetSyncOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
