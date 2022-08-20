/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Data;

namespace Facebook.WitAi.Interfaces
{
    public interface IAudioInputSource
    {
        /// <summary>
        /// Invoked when the instance starts Recording.
        /// </summary>
        event Action OnStartRecording;

        /// <summary>
        /// Invoked when an AudioClip couldn't be created to start recording.
        /// </summary>
        event Action OnStartRecordingFailed;

        /// <summary>
        /// Invoked everytime an audio frame is collected. Includes the frame.
        /// </summary>
        event Action<int, float[], float> OnSampleReady;

        /// <summary>
        /// Invoked when the instance stop Recording.
        /// </summary>
        event Action OnStopRecording;

        void StartRecording(int sampleLen);

        void StopRecording();

        bool IsRecording { get; }

        /// <summary>
        /// Settings determining how audio is encoded by the source.
        ///
        /// NOTE: Default values for AudioEncoding are server optimized to reduce latency.
        /// </summary>
        AudioEncoding AudioEncoding { get; }

        /// <summary>
        /// Return true if input is available.
        /// </summary>
        bool IsInputAvailable { get; }

        /// <summary>
        /// Checks for input
        /// </summary>
        void CheckForInput();
    }
}
