// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class GroupPresenceOptions {

    public GroupPresenceOptions() {
      Handle = CAPI.ovr_GroupPresenceOptions_Create();
    }

    /// This the unique API Name that refers to an in-app destination
    public void SetDestinationApiName(string value) {
      CAPI.ovr_GroupPresenceOptions_SetDestinationApiName(Handle, value);
    }

    /// Set whether or not the person is shown as joinable or not to others. A user
    /// that is joinable can invite others to join them. Set this to false if other
    /// users would not be able to join this user. For example: the current session
    /// is full, or only the host can invite others and the current user is not the
    /// host.
    public void SetIsJoinable(bool value) {
      CAPI.ovr_GroupPresenceOptions_SetIsJoinable(Handle, value);
    }

    /// This is a session that represents a closer group/squad/party of users. It
    /// is expected that all users with the same lobby session id can see or hear
    /// each other. Users with the same lobby session id in their group presence
    /// will show up in the roster and will show up as "Recently Played With" for
    /// future invites if they aren't already Oculus friends. This must be set in
    /// addition to is_joinable being true for a user to use invites.
    public void SetLobbySessionId(string value) {
      CAPI.ovr_GroupPresenceOptions_SetLobbySessionId(Handle, value);
    }

    /// This is a session that represents all the users that are playing a specific
    /// instance of a map, game mode, round, etc. This can include users from
    /// multiple different lobbies that joined together and the users may or may
    /// not remain together after the match is over. Users with the same match
    /// session id in their group presence will not show up in the Roster, but will
    /// show up as "Recently Played with" for future invites.
    public void SetMatchSessionId(string value) {
      CAPI.ovr_GroupPresenceOptions_SetMatchSessionId(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(GroupPresenceOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~GroupPresenceOptions() {
      CAPI.ovr_GroupPresenceOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
