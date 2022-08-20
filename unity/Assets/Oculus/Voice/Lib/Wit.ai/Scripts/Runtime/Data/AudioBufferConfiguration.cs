using UnityEngine;

namespace Facebook.WitAi.Data
{
    public class AudioBufferConfiguration
    {
        [Tooltip("The length of the individual samples read from the audio source")]
        [Range(10, 500)]
        [SerializeField]
        public int sampleLengthInMs = 10;

        [Tooltip(
            "The total audio data that should be buffered for lookback purposes on sound based activations.")]
        [SerializeField]
        public float micBufferLengthInSeconds = 1;
    }
}
