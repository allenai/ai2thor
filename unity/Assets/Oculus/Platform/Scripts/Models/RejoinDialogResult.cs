// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class RejoinDialogResult
  {
    public readonly bool RejoinSelected;


    public RejoinDialogResult(IntPtr o)
    {
      RejoinSelected = CAPI.ovr_RejoinDialogResult_GetRejoinSelected(o);
    }
  }

}
