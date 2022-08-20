// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class ChallengeOptions {

    public ChallengeOptions() {
      Handle = CAPI.ovr_ChallengeOptions_Create();
    }

    public void SetDescription(string value) {
      CAPI.ovr_ChallengeOptions_SetDescription(Handle, value);
    }

    public void SetEndDate(DateTime value) {
      CAPI.ovr_ChallengeOptions_SetEndDate(Handle, value);
    }

    public void SetIncludeActiveChallenges(bool value) {
      CAPI.ovr_ChallengeOptions_SetIncludeActiveChallenges(Handle, value);
    }

    public void SetIncludeFutureChallenges(bool value) {
      CAPI.ovr_ChallengeOptions_SetIncludeFutureChallenges(Handle, value);
    }

    public void SetIncludePastChallenges(bool value) {
      CAPI.ovr_ChallengeOptions_SetIncludePastChallenges(Handle, value);
    }

    /// Optional: Only find challenges belonging to this leaderboard.
    public void SetLeaderboardName(string value) {
      CAPI.ovr_ChallengeOptions_SetLeaderboardName(Handle, value);
    }

    public void SetStartDate(DateTime value) {
      CAPI.ovr_ChallengeOptions_SetStartDate(Handle, value);
    }

    public void SetTitle(string value) {
      CAPI.ovr_ChallengeOptions_SetTitle(Handle, value);
    }

    public void SetViewerFilter(ChallengeViewerFilter value) {
      CAPI.ovr_ChallengeOptions_SetViewerFilter(Handle, value);
    }

    public void SetVisibility(ChallengeVisibility value) {
      CAPI.ovr_ChallengeOptions_SetVisibility(Handle, value);
    }


    /// For passing to native C
    public static explicit operator IntPtr(ChallengeOptions options) {
      return options != null ? options.Handle : IntPtr.Zero;
    }

    ~ChallengeOptions() {
      CAPI.ovr_ChallengeOptions_Destroy(Handle);
    }

    IntPtr Handle;
  }
}
