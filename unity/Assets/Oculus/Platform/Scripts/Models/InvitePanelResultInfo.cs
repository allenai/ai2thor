// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class InvitePanelResultInfo
  {
    public readonly bool InvitesSent;


    public InvitePanelResultInfo(IntPtr o)
    {
      InvitesSent = CAPI.ovr_InvitePanelResultInfo_GetInvitesSent(o);
    }
  }

}
