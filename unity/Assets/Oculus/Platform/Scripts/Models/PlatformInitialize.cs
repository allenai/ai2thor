// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class PlatformInitialize
  {
    public readonly PlatformInitializeResult Result;


    public PlatformInitialize(IntPtr o)
    {
      Result = CAPI.ovr_PlatformInitialize_GetResult(o);
    }
  }

}
