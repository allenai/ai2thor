// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Leaderboard
  {
    public readonly string ApiName;
    // May be null. Check before using.
    public readonly Destination DestinationOptional;
    [Obsolete("Deprecated in favor of DestinationOptional")]
    public readonly Destination Destination;
    public readonly UInt64 ID;


    public Leaderboard(IntPtr o)
    {
      ApiName = CAPI.ovr_Leaderboard_GetApiName(o);
      {
        var pointer = CAPI.ovr_Leaderboard_GetDestination(o);
        Destination = new Destination(pointer);
        if (pointer == IntPtr.Zero) {
          DestinationOptional = null;
        } else {
          DestinationOptional = Destination;
        }
      }
      ID = CAPI.ovr_Leaderboard_GetID(o);
    }
  }

  public class LeaderboardList : DeserializableList<Leaderboard> {
    public LeaderboardList(IntPtr a) {
      var count = (int)CAPI.ovr_LeaderboardArray_GetSize(a);
      _Data = new List<Leaderboard>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Leaderboard(CAPI.ovr_LeaderboardArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_LeaderboardArray_GetNextUrl(a);
    }

  }
}
