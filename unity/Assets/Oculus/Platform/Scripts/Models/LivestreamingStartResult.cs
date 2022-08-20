// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LivestreamingStartResult
  {
    public readonly LivestreamingStartStatus StreamingResult;


    public LivestreamingStartResult(IntPtr o)
    {
      StreamingResult = CAPI.ovr_LivestreamingStartResult_GetStreamingResult(o);
    }
  }

}
