// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class PartyUpdateNotification
  {
    public readonly PartyUpdateAction Action;
    public readonly UInt64 PartyId;
    public readonly UInt64 SenderId;
    public readonly string UpdateTimestamp;
    public readonly string UserAlias;
    public readonly UInt64 UserId;
    public readonly string UserName;


    public PartyUpdateNotification(IntPtr o)
    {
      Action = CAPI.ovr_PartyUpdateNotification_GetAction(o);
      PartyId = CAPI.ovr_PartyUpdateNotification_GetPartyId(o);
      SenderId = CAPI.ovr_PartyUpdateNotification_GetSenderId(o);
      UpdateTimestamp = CAPI.ovr_PartyUpdateNotification_GetUpdateTimestamp(o);
      UserAlias = CAPI.ovr_PartyUpdateNotification_GetUserAlias(o);
      UserId = CAPI.ovr_PartyUpdateNotification_GetUserId(o);
      UserName = CAPI.ovr_PartyUpdateNotification_GetUserName(o);
    }
  }

}
