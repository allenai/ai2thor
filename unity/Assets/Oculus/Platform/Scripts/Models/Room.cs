// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Room
  {
    public readonly UInt64 ApplicationID;
    public readonly Dictionary<string, string> DataStore;
    public readonly string Description;
    public readonly UInt64 ID;
    // May be null. Check before using.
    public readonly UserList InvitedUsersOptional;
    [Obsolete("Deprecated in favor of InvitedUsersOptional")]
    public readonly UserList InvitedUsers;
    public readonly bool IsMembershipLocked;
    public readonly RoomJoinPolicy JoinPolicy;
    public readonly RoomJoinability Joinability;
    // May be null. Check before using.
    public readonly MatchmakingEnqueuedUserList MatchedUsersOptional;
    [Obsolete("Deprecated in favor of MatchedUsersOptional")]
    public readonly MatchmakingEnqueuedUserList MatchedUsers;
    public readonly uint MaxUsers;
    public readonly string Name;
    // May be null. Check before using.
    public readonly User OwnerOptional;
    [Obsolete("Deprecated in favor of OwnerOptional")]
    public readonly User Owner;
    // May be null. Check before using.
    public readonly TeamList TeamsOptional;
    [Obsolete("Deprecated in favor of TeamsOptional")]
    public readonly TeamList Teams;
    public readonly RoomType Type;
    // May be null. Check before using.
    public readonly UserList UsersOptional;
    [Obsolete("Deprecated in favor of UsersOptional")]
    public readonly UserList Users;
    public readonly uint Version;


    public Room(IntPtr o)
    {
      ApplicationID = CAPI.ovr_Room_GetApplicationID(o);
      DataStore = CAPI.DataStoreFromNative(CAPI.ovr_Room_GetDataStore(o));
      Description = CAPI.ovr_Room_GetDescription(o);
      ID = CAPI.ovr_Room_GetID(o);
      {
        var pointer = CAPI.ovr_Room_GetInvitedUsers(o);
        InvitedUsers = new UserList(pointer);
        if (pointer == IntPtr.Zero) {
          InvitedUsersOptional = null;
        } else {
          InvitedUsersOptional = InvitedUsers;
        }
      }
      IsMembershipLocked = CAPI.ovr_Room_GetIsMembershipLocked(o);
      JoinPolicy = CAPI.ovr_Room_GetJoinPolicy(o);
      Joinability = CAPI.ovr_Room_GetJoinability(o);
      {
        var pointer = CAPI.ovr_Room_GetMatchedUsers(o);
        MatchedUsers = new MatchmakingEnqueuedUserList(pointer);
        if (pointer == IntPtr.Zero) {
          MatchedUsersOptional = null;
        } else {
          MatchedUsersOptional = MatchedUsers;
        }
      }
      MaxUsers = CAPI.ovr_Room_GetMaxUsers(o);
      Name = CAPI.ovr_Room_GetName(o);
      {
        var pointer = CAPI.ovr_Room_GetOwner(o);
        Owner = new User(pointer);
        if (pointer == IntPtr.Zero) {
          OwnerOptional = null;
        } else {
          OwnerOptional = Owner;
        }
      }
      {
        var pointer = CAPI.ovr_Room_GetTeams(o);
        Teams = new TeamList(pointer);
        if (pointer == IntPtr.Zero) {
          TeamsOptional = null;
        } else {
          TeamsOptional = Teams;
        }
      }
      Type = CAPI.ovr_Room_GetType(o);
      {
        var pointer = CAPI.ovr_Room_GetUsers(o);
        Users = new UserList(pointer);
        if (pointer == IntPtr.Zero) {
          UsersOptional = null;
        } else {
          UsersOptional = Users;
        }
      }
      Version = CAPI.ovr_Room_GetVersion(o);
    }
  }

  public class RoomList : DeserializableList<Room> {
    public RoomList(IntPtr a) {
      var count = (int)CAPI.ovr_RoomArray_GetSize(a);
      _Data = new List<Room>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Room(CAPI.ovr_RoomArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_RoomArray_GetNextUrl(a);
    }

  }
}
