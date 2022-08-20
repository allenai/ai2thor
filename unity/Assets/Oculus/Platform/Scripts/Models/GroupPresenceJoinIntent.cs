// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class GroupPresenceJoinIntent
  {
    public readonly string DeeplinkMessage;
    public readonly string DestinationApiName;
    public readonly string LobbySessionId;
    public readonly string MatchSessionId;


    public GroupPresenceJoinIntent(IntPtr o)
    {
      DeeplinkMessage = CAPI.ovr_GroupPresenceJoinIntent_GetDeeplinkMessage(o);
      DestinationApiName = CAPI.ovr_GroupPresenceJoinIntent_GetDestinationApiName(o);
      LobbySessionId = CAPI.ovr_GroupPresenceJoinIntent_GetLobbySessionId(o);
      MatchSessionId = CAPI.ovr_GroupPresenceJoinIntent_GetMatchSessionId(o);
    }
  }

}
