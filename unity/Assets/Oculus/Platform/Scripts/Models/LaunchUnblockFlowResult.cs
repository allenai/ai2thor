// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LaunchUnblockFlowResult
  {
    public readonly bool DidCancel;
    public readonly bool DidUnblock;


    public LaunchUnblockFlowResult(IntPtr o)
    {
      DidCancel = CAPI.ovr_LaunchUnblockFlowResult_GetDidCancel(o);
      DidUnblock = CAPI.ovr_LaunchUnblockFlowResult_GetDidUnblock(o);
    }
  }

}
