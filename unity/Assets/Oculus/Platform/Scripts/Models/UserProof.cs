// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class UserProof
  {
    public readonly string Value;


    public UserProof(IntPtr o)
    {
      Value = CAPI.ovr_UserProof_GetNonce(o);
    }
  }

}
