// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MatchmakingAdminSnapshotCandidate
  {
    public readonly bool CanMatch;
    public readonly double MyTotalScore;
    public readonly double TheirCurrentThreshold;
    public readonly double TheirTotalScore;
    public readonly string TraceId;


    public MatchmakingAdminSnapshotCandidate(IntPtr o)
    {
      CanMatch = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetCanMatch(o);
      MyTotalScore = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetMyTotalScore(o);
      TheirCurrentThreshold = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetTheirCurrentThreshold(o);
      TheirTotalScore = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetTheirTotalScore(o);
      TraceId = CAPI.ovr_MatchmakingAdminSnapshotCandidate_GetTraceId(o);
    }
  }

  public class MatchmakingAdminSnapshotCandidateList : DeserializableList<MatchmakingAdminSnapshotCandidate> {
    public MatchmakingAdminSnapshotCandidateList(IntPtr a) {
      var count = (int)CAPI.ovr_MatchmakingAdminSnapshotCandidateArray_GetSize(a);
      _Data = new List<MatchmakingAdminSnapshotCandidate>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new MatchmakingAdminSnapshotCandidate(CAPI.ovr_MatchmakingAdminSnapshotCandidateArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
