//This file is deprecated.  Use the high level voip system instead:
// https://developer.oculus.com/documentation/unity/ps-voip/
#if false
namespace Oculus.Platform
{
  using UnityEngine;
  using System.Runtime.InteropServices;
  using System.Collections;

  public class VoipInput : MonoBehaviour
  {
    public delegate void OnCompressedData(byte[] compressedData);
    public OnCompressedData onCompressedData;

    protected IMicrophone micInput;
    Encoder encoder;

    public bool enableMicRecording;

    protected void Start()
    {
      encoder = new Encoder();
      if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor || UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer)
      {
        micInput = new MicrophoneInputNative();
      }
      else
      {
        micInput = new MicrophoneInput();
      }

      enableMicRecording = true;
    }

    void OnApplicationQuit()
    {
      micInput.Stop();
    }

    void Update()
    {
      if (micInput == null || encoder == null)
      {
        throw new System.Exception("VoipInput failed to init");
      }

      if (micInput != null && enableMicRecording)
      {
        float[] rawMicSamples = micInput.Update();

        if (rawMicSamples != null && rawMicSamples.Length > 5 * 1024)
        {
          Debug.Log(string.Format("Giant input mic data {0}", rawMicSamples.Length));
          return;
        }

        if (rawMicSamples != null && rawMicSamples.Length > 0)
        {
          int startIdx = 0;
          int remaining = rawMicSamples.Length;
          int splitSize = 480;

          do
          {
            int toCopy = System.Math.Min(splitSize, remaining);
            float[] splitInput = new float[toCopy];
            System.Array.Copy(rawMicSamples, startIdx, splitInput, 0, toCopy);
            startIdx += toCopy;
            remaining -= toCopy;

            byte[] compressedMic = null;
            compressedMic = encoder.Encode(splitInput);

            if (compressedMic != null && compressedMic.Length > 0)
            {
              onCompressedData(compressedMic);
            }
          } while (remaining > 0);
        }
      }
    }
  }
}
#endif
