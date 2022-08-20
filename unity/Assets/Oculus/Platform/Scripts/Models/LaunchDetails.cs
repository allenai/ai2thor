// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LaunchDetails
  {
    public readonly string DeeplinkMessage;
    public readonly string DestinationApiName;
    public readonly string LaunchSource;
    public readonly LaunchType LaunchType;
    public readonly UInt64 RoomID;
    public readonly string TrackingID;
    // May be null. Check before using.
    public readonly UserList UsersOptional;
    [Obsolete("Deprecated in favor of UsersOptional")]
    public readonly UserList Users;


    public LaunchDetails(IntPtr o)
    {
      DeeplinkMessage = CAPI.ovr_LaunchDetails_GetDeeplinkMessage(o);
      DestinationApiName = CAPI.ovr_LaunchDetails_GetDestinationApiName(o);
      LaunchSource = CAPI.ovr_LaunchDetails_GetLaunchSource(o);
      LaunchType = CAPI.ovr_LaunchDetails_GetLaunchType(o);
      RoomID = CAPI.ovr_LaunchDetails_GetRoomID(o);
      TrackingID = CAPI.ovr_LaunchDetails_GetTrackingID(o);
      {
        var pointer = CAPI.ovr_LaunchDetails_GetUsers(o);
        Users = new UserList(pointer);
        if (pointer == IntPtr.Zero) {
          UsersOptional = null;
        } else {
          UsersOptional = Users;
        }
      }
    }
  }

}
