using UnityEngine;
using System.Collections;
using System;

namespace Oculus.Platform
{
  public class VoipPCMSourceNative : IVoipPCMSource
  {
    ulong senderID;

    public int GetPCM(float[] dest, int length)
    {
      return (int)CAPI.ovr_Voip_GetPCMFloat(senderID, dest, (UIntPtr)length);
    }

    public void SetSenderID(ulong senderID)
    {
      this.senderID = senderID;
    }

    public int PeekSizeElements() {
      return (int)CAPI.ovr_Voip_GetPCMSize(senderID);
    }

    public void Update() { }
  }
}
