// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class NetSyncSession
  {
    public readonly long ConnectionId;
    public readonly bool Muted;
    public readonly UInt64 SessionId;
    public readonly UInt64 UserId;
    public readonly string VoipGroup;


    public NetSyncSession(IntPtr o)
    {
      ConnectionId = CAPI.ovr_NetSyncSession_GetConnectionId(o);
      Muted = CAPI.ovr_NetSyncSession_GetMuted(o);
      SessionId = CAPI.ovr_NetSyncSession_GetSessionId(o);
      UserId = CAPI.ovr_NetSyncSession_GetUserId(o);
      VoipGroup = CAPI.ovr_NetSyncSession_GetVoipGroup(o);
    }
  }

  public class NetSyncSessionList : DeserializableList<NetSyncSession> {
    public NetSyncSessionList(IntPtr a) {
      var count = (int)CAPI.ovr_NetSyncSessionArray_GetSize(a);
      _Data = new List<NetSyncSession>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new NetSyncSession(CAPI.ovr_NetSyncSessionArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
