// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Challenge
  {
    public readonly ChallengeCreationType CreationType;
    public readonly string Description;
    public readonly DateTime EndDate;
    public readonly UInt64 ID;
    // May be null. Check before using.
    public readonly UserList InvitedUsersOptional;
    [Obsolete("Deprecated in favor of InvitedUsersOptional")]
    public readonly UserList InvitedUsers;
    public readonly Leaderboard Leaderboard;
    // May be null. Check before using.
    public readonly UserList ParticipantsOptional;
    [Obsolete("Deprecated in favor of ParticipantsOptional")]
    public readonly UserList Participants;
    public readonly DateTime StartDate;
    public readonly string Title;
    public readonly ChallengeVisibility Visibility;


    public Challenge(IntPtr o)
    {
      CreationType = CAPI.ovr_Challenge_GetCreationType(o);
      Description = CAPI.ovr_Challenge_GetDescription(o);
      EndDate = CAPI.ovr_Challenge_GetEndDate(o);
      ID = CAPI.ovr_Challenge_GetID(o);
      {
        var pointer = CAPI.ovr_Challenge_GetInvitedUsers(o);
        InvitedUsers = new UserList(pointer);
        if (pointer == IntPtr.Zero) {
          InvitedUsersOptional = null;
        } else {
          InvitedUsersOptional = InvitedUsers;
        }
      }
      Leaderboard = new Leaderboard(CAPI.ovr_Challenge_GetLeaderboard(o));
      {
        var pointer = CAPI.ovr_Challenge_GetParticipants(o);
        Participants = new UserList(pointer);
        if (pointer == IntPtr.Zero) {
          ParticipantsOptional = null;
        } else {
          ParticipantsOptional = Participants;
        }
      }
      StartDate = CAPI.ovr_Challenge_GetStartDate(o);
      Title = CAPI.ovr_Challenge_GetTitle(o);
      Visibility = CAPI.ovr_Challenge_GetVisibility(o);
    }
  }

  public class ChallengeList : DeserializableList<Challenge> {
    public ChallengeList(IntPtr a) {
      var count = (int)CAPI.ovr_ChallengeArray_GetSize(a);
      _Data = new List<Challenge>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Challenge(CAPI.ovr_ChallengeArray_GetElement(a, (UIntPtr)i)));
      }

      TotalCount = CAPI.ovr_ChallengeArray_GetTotalCount(a);
      _PreviousUrl = CAPI.ovr_ChallengeArray_GetPreviousUrl(a);
      _NextUrl = CAPI.ovr_ChallengeArray_GetNextUrl(a);
    }

    public readonly ulong TotalCount;
  }
}
