// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MatchmakingBrowseResult
  {
    public readonly MatchmakingEnqueueResult EnqueueResult;
    public readonly RoomList Rooms;


    public MatchmakingBrowseResult(IntPtr o)
    {
      EnqueueResult = new MatchmakingEnqueueResult(CAPI.ovr_MatchmakingBrowseResult_GetEnqueueResult(o));
      Rooms = new RoomList(CAPI.ovr_MatchmakingBrowseResult_GetRooms(o));
    }
  }

}
