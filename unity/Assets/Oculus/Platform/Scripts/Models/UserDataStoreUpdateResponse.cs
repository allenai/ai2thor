// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class UserDataStoreUpdateResponse
  {
    public readonly bool Success;


    public UserDataStoreUpdateResponse(IntPtr o)
    {
      Success = CAPI.ovr_UserDataStoreUpdateResponse_GetSuccess(o);
    }
  }

}
