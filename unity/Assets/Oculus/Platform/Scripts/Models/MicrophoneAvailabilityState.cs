// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class MicrophoneAvailabilityState
  {
    public readonly bool MicrophoneAvailable;


    public MicrophoneAvailabilityState(IntPtr o)
    {
      MicrophoneAvailable = CAPI.ovr_MicrophoneAvailabilityState_GetMicrophoneAvailable(o);
    }
  }

}
