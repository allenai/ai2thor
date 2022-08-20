// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class UserReportID
  {
    public readonly bool DidCancel;
    public readonly UInt64 ID;


    public UserReportID(IntPtr o)
    {
      DidCancel = CAPI.ovr_UserReportID_GetDidCancel(o);
      ID = CAPI.ovr_UserReportID_GetID(o);
    }
  }

}
