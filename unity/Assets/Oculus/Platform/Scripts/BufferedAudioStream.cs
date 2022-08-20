//#define VERBOSE_LOGGING
using UnityEngine;
using System.Collections;
using System;

using Oculus.Platform;

public class BufferedAudioStream {
  const bool VerboseLogging = false;
  AudioSource audio;

  float[] audioBuffer;
  int writePos;

  const float bufferLengthSeconds = 0.25f;
  const int sampleRate = 48000;
  const int bufferSize = (int)(sampleRate * bufferLengthSeconds);
  const float playbackDelayTimeSeconds = 0.05f;

  float playbackDelayRemaining;
  float remainingBufferTime;

  public BufferedAudioStream(AudioSource audio) {
    audioBuffer = new float[bufferSize];
    this.audio = audio;

    audio.loop = true;
    audio.clip = AudioClip.Create("", bufferSize, 1, sampleRate, false);

    Stop();
  }

  public void Update () {
    
    if(remainingBufferTime > 0)
    {
#if VERBOSE_LOGGING
      Debug.Log(string.Format("current time: {0}, remainingBufferTime: {1}", Time.time, remainingBufferTime));
#endif

      if (!audio.isPlaying && remainingBufferTime > playbackDelayTimeSeconds)
      {
        playbackDelayRemaining -= Time.deltaTime;
        if (playbackDelayRemaining <= 0)
        {
#if VERBOSE_LOGGING
          Debug.Log("Starting playback");
#endif
          audio.Play();
        }
      }

      if (audio.isPlaying)
      {
        remainingBufferTime -= Time.deltaTime;
        if (remainingBufferTime < 0)
        {
          remainingBufferTime = 0;
        }
      }
    }

    if (remainingBufferTime <= 0)
    {
      if (audio.isPlaying)
      {
        Debug.Log("Buffer empty, stopping " + DateTime.Now);
        Stop();
      }
      else
      {
        if (writePos != 0)
        {
          Debug.LogError("writePos non zero while not playing, how did this happen?");
        }
      }
    }
  }

  void Stop()
  {
    audio.Stop();
    audio.time = 0;
    writePos = 0;
    playbackDelayRemaining = playbackDelayTimeSeconds;
  }

  public void AddData(float[] samples) {
    int remainingWriteLength = samples.Length;

    if(writePos > audioBuffer.Length) {
      throw new Exception();
    }

    do {
      int writeLength = remainingWriteLength;
      int remainingSpace = audioBuffer.Length - writePos;

      if(writeLength > remainingSpace) {
        writeLength = remainingSpace;
      }

      Array.Copy(samples, 0, audioBuffer, writePos, writeLength);

      remainingWriteLength -= writeLength;
      writePos += writeLength;
      if(writePos > audioBuffer.Length) {
        throw new Exception();
      }
      if(writePos == audioBuffer.Length) {
        writePos = 0;
      }
    } while(remainingWriteLength > 0);

#if VERBOSE_LOGGING
    float prev = remainingBufferTime;
#endif
    remainingBufferTime += (float)samples.Length / sampleRate;
#if VERBOSE_LOGGING
    Debug.Log(string.Format("previous remaining: {0}, new remaining: {1}, added {2} samples", prev, remainingBufferTime, samples.Length));
#endif
    audio.clip.SetData(audioBuffer, 0);
  }


}
