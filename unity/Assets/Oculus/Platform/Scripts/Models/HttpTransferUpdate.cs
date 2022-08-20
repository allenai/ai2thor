namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;
  using Oculus.Platform.Models;
  using UnityEngine;

  public class HttpTransferUpdate
  {
    public readonly UInt64 ID;
    public readonly byte[] Payload;
    public readonly bool IsCompleted;

    public HttpTransferUpdate(IntPtr o)
    {
      ID = CAPI.ovr_HttpTransferUpdate_GetID(o);
      IsCompleted = CAPI.ovr_HttpTransferUpdate_IsCompleted(o);

      long size = (long) CAPI.ovr_HttpTransferUpdate_GetSize(o);

      Payload = new byte[size];
      Marshal.Copy(CAPI.ovr_Packet_GetBytes(o), Payload, 0, (int) size);
    }
  }

}
