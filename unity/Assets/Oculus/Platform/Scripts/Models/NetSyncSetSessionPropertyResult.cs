// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class NetSyncSetSessionPropertyResult
  {
    public readonly NetSyncSession Session;


    public NetSyncSetSessionPropertyResult(IntPtr o)
    {
      Session = new NetSyncSession(CAPI.ovr_NetSyncSetSessionPropertyResult_GetSession(o));
    }
  }

}
