//This file is deprecated.  Use the high level voip system instead:
// https://developer.oculus.com/documentation/unity/ps-voip/ 
//
// NOTE for android developers: The existence of UnityEngine.Microphone causes Unity to insert the 
// android.permission.RECORD_AUDIO permission into the AndroidManifest.xml generated at build time

#if OVR_PLATFORM_USE_MICROPHONE
using UnityEngine;
using System.Collections.Generic;

namespace Oculus.Platform
{

  public class MicrophoneInput : IMicrophone
  {
    AudioClip microphoneClip;
    int lastMicrophoneSample;
    int micBufferSizeSamples;

    private Dictionary<int, float[]> micSampleBuffers;

    public MicrophoneInput()
    {
      int bufferLenSeconds = 1; //minimum size unity allows
      int inputFreq = 48000; //this frequency is fixed throughout the voip system atm
      microphoneClip = Microphone.Start(null, true, bufferLenSeconds, inputFreq);
      micBufferSizeSamples = bufferLenSeconds * inputFreq;

      micSampleBuffers = new Dictionary<int, float[]>();
    }

    public void Start()
    {

    }

    public void Stop()
    {
    }

    public float[] Update()
    {
      int pos = Microphone.GetPosition(null);
      int copySize = 0;
      if (pos < lastMicrophoneSample)
      {
        int endOfBufferSize = micBufferSizeSamples - lastMicrophoneSample;
        copySize = endOfBufferSize + pos;
      }
      else
      {
        copySize = pos - lastMicrophoneSample;
      }

      if (copySize == 0) {
        return null;
      }

      float[] samples;
      if (!micSampleBuffers.TryGetValue(copySize, out samples))
      {
        samples = new float[copySize];
        micSampleBuffers[copySize] = samples;
      }

      microphoneClip.GetData(samples, lastMicrophoneSample);
      lastMicrophoneSample = pos;
      return samples;

    }
  }
}
#endif
