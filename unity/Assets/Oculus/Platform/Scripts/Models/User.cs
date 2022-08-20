// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class User
  {
    public readonly string DisplayName;
    public readonly UInt64 ID;
    public readonly string ImageURL;
    public readonly string InviteToken;
    public readonly string OculusID;
    public readonly string Presence;
    public readonly string PresenceDeeplinkMessage;
    public readonly string PresenceDestinationApiName;
    public readonly string PresenceLobbySessionId;
    public readonly string PresenceMatchSessionId;
    public readonly UserPresenceStatus PresenceStatus;
    public readonly string SmallImageUrl;


    public User(IntPtr o)
    {
      DisplayName = CAPI.ovr_User_GetDisplayName(o);
      ID = CAPI.ovr_User_GetID(o);
      ImageURL = CAPI.ovr_User_GetImageUrl(o);
      InviteToken = CAPI.ovr_User_GetInviteToken(o);
      OculusID = CAPI.ovr_User_GetOculusID(o);
      Presence = CAPI.ovr_User_GetPresence(o);
      PresenceDeeplinkMessage = CAPI.ovr_User_GetPresenceDeeplinkMessage(o);
      PresenceDestinationApiName = CAPI.ovr_User_GetPresenceDestinationApiName(o);
      PresenceLobbySessionId = CAPI.ovr_User_GetPresenceLobbySessionId(o);
      PresenceMatchSessionId = CAPI.ovr_User_GetPresenceMatchSessionId(o);
      PresenceStatus = CAPI.ovr_User_GetPresenceStatus(o);
      SmallImageUrl = CAPI.ovr_User_GetSmallImageUrl(o);
    }
  }

  public class UserList : DeserializableList<User> {
    public UserList(IntPtr a) {
      var count = (int)CAPI.ovr_UserArray_GetSize(a);
      _Data = new List<User>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new User(CAPI.ovr_UserArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_UserArray_GetNextUrl(a);
    }

  }
}
