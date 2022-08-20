// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MatchmakingAdminSnapshot
  {
    public readonly MatchmakingAdminSnapshotCandidateList Candidates;
    public readonly double MyCurrentThreshold;


    public MatchmakingAdminSnapshot(IntPtr o)
    {
      Candidates = new MatchmakingAdminSnapshotCandidateList(CAPI.ovr_MatchmakingAdminSnapshot_GetCandidates(o));
      MyCurrentThreshold = CAPI.ovr_MatchmakingAdminSnapshot_GetMyCurrentThreshold(o);
    }
  }

}
