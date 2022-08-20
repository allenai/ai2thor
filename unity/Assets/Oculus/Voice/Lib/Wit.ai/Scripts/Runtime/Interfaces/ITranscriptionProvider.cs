/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Events;
using UnityEngine.Events;

namespace Facebook.WitAi.Interfaces
{
    public interface ITranscriptionProvider
    {
        /// <summary>
        /// Provides the last transcription value (could be a partial transcription)
        /// </summary>
        string LastTranscription { get; }

        /// <summary>
        /// Callback used to notify Wit subscribers of a partial transcription.
        /// </summary>
        WitTranscriptionEvent OnPartialTranscription { get; }

        /// <summary>
        /// Callback used to notify Wit subscribers of a full transcription
        /// </summary>
        WitTranscriptionEvent OnFullTranscription { get; }

        /// <summary>
        /// Callback used to notify Wit subscribers when the mic is active and transcription has begun
        /// </summary>
        UnityEvent OnStoppedListening { get; }

        /// <summary>
        /// Callback used to notify Wit subscribers when the mic is inactive and transcription has stopped
        /// </summary>
        UnityEvent OnStartListening { get; }

        /// <summary>
        /// Callback used to notify Wit subscribers on mic volume level changes
        /// </summary>
        WitMicLevelChangedEvent OnMicLevelChanged { get; }

        /// <summary>
        /// Tells Wit if the mic input levels from the transcription service should be used directly
        /// </summary>
        bool OverrideMicLevel { get; }

        /// <summary>
        /// Called when wit is activated
        /// </summary>
        void Activate();

        /// <summary>
        /// Called when
        /// </summary>
        void Deactivate();
    }
}
