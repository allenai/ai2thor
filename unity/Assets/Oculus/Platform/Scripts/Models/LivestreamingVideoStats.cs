// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LivestreamingVideoStats
  {
    public readonly int CommentCount;
    public readonly int ReactionCount;
    public readonly string TotalViews;


    public LivestreamingVideoStats(IntPtr o)
    {
      CommentCount = CAPI.ovr_LivestreamingVideoStats_GetCommentCount(o);
      ReactionCount = CAPI.ovr_LivestreamingVideoStats_GetReactionCount(o);
      TotalViews = CAPI.ovr_LivestreamingVideoStats_GetTotalViews(o);
    }
  }

}
