// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class VoipOptions {

    public VoipOptions() {
      Handle = CAPI.ovr_VoipOptions_Create();
    }

    /// Sets the maximum average bitrate the audio codec should use. Higher
    /// bitrates will increase audio quality at the expense of increased network
    /// usage. Use a lower bitrate if you think the quality is good but the network
    /// usage is too much. Use a higher bitrate if you think the quality is bad and
    /// you can afford to have a large streaming bitrate.
    public void SetBitrateForNewConnections(VoipBitrate value) {
      CAPI.ovr_VoipOptions_SetBitrateForNewConnections(Handle, value);
    }

    /// Set the opus codec to use discontinous transmission (DTX). DTX only
    /// transmits data when a person is speaking. Setting this to true takes
    /// advantage of the fact that in a two-way converstation each individual
    /// speaks for less than half the time. Enabling DTX will conserve battery
    /// power and reduce transmission rate when a pause in the voice chat is
    /// detected.
    public void SetCreateNewConnectionUseDtx(VoipDtxState value) {
      CAPI.ovr_VoipOptions_SetCreateNewConnectionUseDtx(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(VoipOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~VoipOptions() {
      CAPI.ovr_VoipOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
