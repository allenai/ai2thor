// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LivestreamingApplicationStatus
  {
    public readonly bool StreamingEnabled;


    public LivestreamingApplicationStatus(IntPtr o)
    {
      StreamingEnabled = CAPI.ovr_LivestreamingApplicationStatus_GetStreamingEnabled(o);
    }
  }

}
