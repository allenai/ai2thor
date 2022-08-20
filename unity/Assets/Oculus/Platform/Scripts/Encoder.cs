//This file is deprecated.  Use the high level voip system instead:
// https://developer3.oculus.com/documentation/platform/latest/concepts/dg-core-content/#dg-cc-voip
#if false
using UnityEngine;
using System.Collections;
using System;


namespace Oculus.Platform {

public class Encoder : IDisposable {
    IntPtr enc;

    public Encoder() {
      enc = CAPI.ovr_Voip_CreateEncoder();
    }

    public void Dispose()
    {
      if (enc != IntPtr.Zero)
      {
        CAPI.ovr_Voip_DestroyEncoder(enc);
        enc = IntPtr.Zero;
      }
    }

    public byte[] Encode(float[] samples) {
      CAPI.ovr_VoipEncoder_AddPCM(enc, samples, (uint)samples.Length);

      ulong size = (ulong)CAPI.ovr_VoipEncoder_GetCompressedDataSize(enc);
      if(size > 0) {
        byte[] compressedData = new byte[size]; //TODO 10376403 - pool this
        ulong sizeRead = (ulong)CAPI.ovr_VoipEncoder_GetCompressedData(enc, compressedData, (UIntPtr)size);

        if (sizeRead != size)
        {
          throw new Exception("Read size differed from reported size");
        }
        return compressedData;
      }
      return null;
    }
  }
}
#endif
