// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RoomInviteNotification
  {
    public readonly UInt64 ID;
    public readonly UInt64 RoomID;
    public readonly UInt64 SenderID;
    public readonly DateTime SentTime;


    public RoomInviteNotification(IntPtr o)
    {
      ID = CAPI.ovr_RoomInviteNotification_GetID(o);
      RoomID = CAPI.ovr_RoomInviteNotification_GetRoomID(o);
      SenderID = CAPI.ovr_RoomInviteNotification_GetSenderID(o);
      SentTime = CAPI.ovr_RoomInviteNotification_GetSentTime(o);
    }
  }

  public class RoomInviteNotificationList : DeserializableList<RoomInviteNotification> {
    public RoomInviteNotificationList(IntPtr a) {
      var count = (int)CAPI.ovr_RoomInviteNotificationArray_GetSize(a);
      _Data = new List<RoomInviteNotification>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new RoomInviteNotification(CAPI.ovr_RoomInviteNotificationArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_RoomInviteNotificationArray_GetNextUrl(a);
    }

  }
}
