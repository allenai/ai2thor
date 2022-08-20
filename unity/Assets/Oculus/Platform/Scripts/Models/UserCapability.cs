// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class UserCapability
  {
    public readonly string Description;
    public readonly bool IsEnabled;
    public readonly string Name;
    public readonly string ReasonCode;


    public UserCapability(IntPtr o)
    {
      Description = CAPI.ovr_UserCapability_GetDescription(o);
      IsEnabled = CAPI.ovr_UserCapability_GetIsEnabled(o);
      Name = CAPI.ovr_UserCapability_GetName(o);
      ReasonCode = CAPI.ovr_UserCapability_GetReasonCode(o);
    }
  }

  public class UserCapabilityList : DeserializableList<UserCapability> {
    public UserCapabilityList(IntPtr a) {
      var count = (int)CAPI.ovr_UserCapabilityArray_GetSize(a);
      _Data = new List<UserCapability>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new UserCapability(CAPI.ovr_UserCapabilityArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_UserCapabilityArray_GetNextUrl(a);
    }

  }
}
