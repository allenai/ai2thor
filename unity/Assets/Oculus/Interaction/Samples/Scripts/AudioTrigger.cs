/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Assertions;
using Oculus.Interaction;

namespace Oculus.Interaction
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioTrigger : MonoBehaviour
    {
        // Private
        private AudioSource _audioSource = null;
        private List<AudioClip> _randomAudioClipPool = new List<AudioClip>();
        private AudioClip _previousAudioClip = null;

        // Serialized
        [Tooltip("Audio clip arrays with a value greater than 1 will have randomized playback.")]
        [SerializeField]
        private AudioClip[] _audioClips;
        [Tooltip("Volume set here will override the volume set on the attached sound source component.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _volume = 0.7f;
        [Tooltip("Check the 'Use Random Range' bool to and adjust the min and max slider values for randomized volume level playback.")]
        [SerializeField]
        private MinMaxPair _volumeRandomization;
        [Tooltip("Pitch set here will override the volume set on the attached sound source component.")]
        [SerializeField]
        [Range(-3f,3f)]
        [Space(10)]
        private float _pitch = 1f;
        [Tooltip("Check the 'Use Random Range' bool to and adjust the min and max slider values for randomized volume level playback.")]
        [SerializeField]
        private MinMaxPair _pitchRandomization;
        [Tooltip("True by default. Set to false for sounds to bypass the spatializer plugin. Will override settings on attached audio source.")]
        [SerializeField]
        [Space(10)]
        private bool _spatialize = true;
        [Tooltip("False by default. Set to true to enable looping on this sound. Will override settings on attached audio source.")]
        [SerializeField]
        private bool _loop = false;
        [Tooltip("100% by default. Sets likelyhood sample will actually play when called")]
        [SerializeField]
        private float _chanceToPlay = 100;
        [Tooltip("If enabled, audio will play automatically when this gameobject is enabled")]
        [SerializeField]
        private bool _playOnStart = false;
        protected virtual void Start()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            // Validate that we have audio to play
            Assert.IsTrue(_audioClips.Length > 0, "An AudioTrigger instance in the scene has no audio clips.");
            // Add all audio clips in the populated array into an audio clip list for randomization purposes
            for (int i = 0; i < _audioClips.Length; i++)
            {
                _randomAudioClipPool.Add(_audioClips[i]);
            }
            // Copy over values from the audio trigger to the audio source
            _audioSource.volume = _volume;
            _audioSource.pitch = _pitch;
            _audioSource.spatialize = _spatialize;
            _audioSource.loop = _loop;
            Random.InitState((int)Time.time);
            // Play audio on start if enabled
            if (_playOnStart)
            {
                PlayAudio();
            }
        }
        public void PlayAudio()
        {
            // Early out if our audio source is disabled
            if (!_audioSource.isActiveAndEnabled)
            {
                return;
            }
            // Check if random chance is set
            float pick = Random.Range(0.0f, 100.0f);
            if (_chanceToPlay < 100 && pick > _chanceToPlay)
            {
                return;
            }
            // Check if volume randomization is set
            if (_volumeRandomization.UseRandomRange == true)
            {
                _audioSource.volume = Random.Range(_volumeRandomization.Min, _volumeRandomization.Max);
            }
            // Check if pitch randomization is set
            if (_pitchRandomization.UseRandomRange == true)
            {
                _audioSource.pitch = Random.Range(_pitchRandomization.Min, _pitchRandomization.Max);
            }
            // If the audio trigger has one clip, play it. Otherwise play a random without repeat clip
            AudioClip clipToPlay = _audioClips.Length == 1 ? _audioClips[0] : RandomClipWithoutRepeat();
            _audioSource.clip = clipToPlay;
            // Play the audio
            _audioSource.Play();
        }

        /// <summary>
        /// Choose a random clip without repeating the last clip
        /// </summary>
        private AudioClip RandomClipWithoutRepeat()
        {
            int randomIndex = Random.Range(0, _randomAudioClipPool.Count);
            AudioClip randomClip = _randomAudioClipPool[randomIndex];
            _randomAudioClipPool.RemoveAt(randomIndex);
            if (_previousAudioClip != null) {
                _randomAudioClipPool.Add(_previousAudioClip);
            }
            _previousAudioClip = randomClip;
            return randomClip;
        }
    }
    [System.Serializable]
    public struct MinMaxPair
    {
        [SerializeField]
        private bool _useRandomRange;
        [SerializeField]
        private float _min;
        [SerializeField]
        private float _max;
        public bool UseRandomRange => _useRandomRange;
        public float Min => _min;
        public float Max => _max;
    }
}
