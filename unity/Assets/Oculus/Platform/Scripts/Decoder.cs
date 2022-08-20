//This file is deprecated.  Use the high level voip system instead:
// https://developer3.oculus.com/documentation/platform/latest/concepts/dg-core-content/#dg-cc-voip
#if false
using UnityEngine;
using System.Collections;
using System;

namespace Oculus.Platform {

  public class Decoder : IDisposable {

    IntPtr dec;
    float[] decodedScratchBuffer;

    public Decoder() {
      dec = CAPI.ovr_Voip_CreateDecoder();
      decodedScratchBuffer = new float[480 * 10];
    }

    public void Dispose()
    {
      if (dec != IntPtr.Zero)
      {
        CAPI.ovr_Voip_DestroyEncoder(dec);
        dec = IntPtr.Zero;
      }
    }

    public float[] Decode(byte[] data) {
      CAPI.ovr_VoipDecoder_Decode(dec, data, (uint)data.Length);

      ulong gotSize = (ulong)CAPI.ovr_VoipDecoder_GetDecodedPCM(dec, decodedScratchBuffer, (UIntPtr)decodedScratchBuffer.Length);

      if (gotSize > 0)
      {
        float[] pcm = new float[gotSize];
        Array.Copy(decodedScratchBuffer, pcm, (int)gotSize);
        return pcm;
      }

      return null;
    }
  }
}
#endif
