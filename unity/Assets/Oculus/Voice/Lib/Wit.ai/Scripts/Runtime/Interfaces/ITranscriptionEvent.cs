using Facebook.WitAi.Events;
using UnityEngine;

namespace Facebook.WitAi.Interfaces
{
    public interface ITranscriptionEvent
    {
        /// <summary>
        /// Message fired when a partial transcription has been received.
        /// </summary>
        WitTranscriptionEvent OnPartialTranscription { get; }

        /// <summary>
        /// Message received when a complete transcription is received.
        /// </summary>
        WitTranscriptionEvent OnFullTranscription { get; }
    }
}
