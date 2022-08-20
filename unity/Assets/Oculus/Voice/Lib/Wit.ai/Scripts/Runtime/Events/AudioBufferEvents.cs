using System;
using Facebook.WitAi.Data;
using UnityEngine;

namespace Facebook.WitAi.Events
{
    [Serializable]
    public class AudioBufferEvents
    {
        public delegate void OnSampleReadyEvent(RingBuffer<byte>.Marker marker, float levelMax);
        public OnSampleReadyEvent OnSampleReady;

        [Tooltip("Called when the volume level of the mic input has changed")]
        public WitMicLevelChangedEvent OnMicLevelChanged = new WitMicLevelChangedEvent();

        [Header("Data")]
        public WitByteDataEvent OnByteDataReady = new WitByteDataEvent();
        public WitByteDataEvent OnByteDataSent = new WitByteDataEvent();
    }
}
