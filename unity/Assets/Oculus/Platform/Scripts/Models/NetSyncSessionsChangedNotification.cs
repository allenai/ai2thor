// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class NetSyncSessionsChangedNotification
  {
    public readonly long ConnectionId;
    public readonly NetSyncSessionList Sessions;


    public NetSyncSessionsChangedNotification(IntPtr o)
    {
      ConnectionId = CAPI.ovr_NetSyncSessionsChangedNotification_GetConnectionId(o);
      Sessions = new NetSyncSessionList(CAPI.ovr_NetSyncSessionsChangedNotification_GetSessions(o));
    }
  }

}
