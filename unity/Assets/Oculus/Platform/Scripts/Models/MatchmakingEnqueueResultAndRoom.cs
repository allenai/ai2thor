// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MatchmakingEnqueueResultAndRoom
  {
    public readonly MatchmakingEnqueueResult MatchmakingEnqueueResult;
    public readonly Room Room;


    public MatchmakingEnqueueResultAndRoom(IntPtr o)
    {
      MatchmakingEnqueueResult = new MatchmakingEnqueueResult(CAPI.ovr_MatchmakingEnqueueResultAndRoom_GetMatchmakingEnqueueResult(o));
      Room = new Room(CAPI.ovr_MatchmakingEnqueueResultAndRoom_GetRoom(o));
    }
  }

}
