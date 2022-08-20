// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LaunchBlockFlowResult
  {
    public readonly bool DidBlock;
    public readonly bool DidCancel;


    public LaunchBlockFlowResult(IntPtr o)
    {
      DidBlock = CAPI.ovr_LaunchBlockFlowResult_GetDidBlock(o);
      DidCancel = CAPI.ovr_LaunchBlockFlowResult_GetDidCancel(o);
    }
  }

}
