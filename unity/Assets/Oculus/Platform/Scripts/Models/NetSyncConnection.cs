// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class NetSyncConnection
  {
    public readonly long ConnectionId;
    public readonly NetSyncDisconnectReason DisconnectReason;
    public readonly UInt64 SessionId;
    public readonly NetSyncConnectionStatus Status;
    public readonly string ZoneId;


    public NetSyncConnection(IntPtr o)
    {
      ConnectionId = CAPI.ovr_NetSyncConnection_GetConnectionId(o);
      DisconnectReason = CAPI.ovr_NetSyncConnection_GetDisconnectReason(o);
      SessionId = CAPI.ovr_NetSyncConnection_GetSessionId(o);
      Status = CAPI.ovr_NetSyncConnection_GetStatus(o);
      ZoneId = CAPI.ovr_NetSyncConnection_GetZoneId(o);
    }
  }

}
