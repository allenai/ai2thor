// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// Source: https://github.com/adrenak/unimic/blob/master/Assets/UniMic/Runtime/Mic.cs

#if UNITY_EDITOR
#define EDITOR_PERMISSION_POPUP
#endif

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facebook.WitAi.Data;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Utilities;

namespace Facebook.WitAi.Lib
{
    [RequireComponent(typeof(AudioSource))]
    public class Mic : MonoBehaviour, IAudioInputSource
    {
        // ================================================

        #region MEMBERS

        // ================================================
        /// <summary>
        /// Whether the microphone is running
        /// </summary>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// Settings used to encode audio. Defaults to optimal server settings
        /// </summary>
        public AudioEncoding AudioEncoding { get; } = new AudioEncoding();

        /// <summary>
        /// Last populated audio sample
        /// </summary>
        public float[] Sample { get; private set; }

        /// <summary>
        /// Sample duration/length in milliseconds
        /// </summary>
        public int SampleDurationMS { get; private set; }

        /// <summary>
        /// Check if input is available & start if possible
        /// </summary>
        public bool IsInputAvailable => AudioClip;

        /// <summary>
        /// Safely starts mic if possible
        /// </summary>
        public void CheckForInput() => SafeStartMicrophone();

        /// <summary>
        /// The length of the sample float array
        /// </summary>
        public int SampleLength
        {
            get { return AudioEncoding.samplerate * SampleDurationMS / 1000; }
        }

        /// <summary>
        /// The AudioClip currently being streamed in the Mic
        /// </summary>
        public AudioClip AudioClip { get; private set; }


        /// <summary>
        /// List of all the available Mic devices
        /// </summary>
        public List<string> Devices
        {
            get
            {
                if (null == _devices)
                {
                    RefreshMicDevices();
                }
                return _devices;
            }
        }
        private List<string> _devices;

        /// <summary>
        /// Index of the current Mic device in m_Devices
        /// </summary>
        public int CurrentDeviceIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the name of the Mic device currently in use
        /// </summary>
        public string CurrentDeviceName
        {
            get
            {
                if (CurrentDeviceIndex < 0 || CurrentDeviceIndex >= Devices.Count)
                    return string.Empty;
                return Devices[CurrentDeviceIndex];
            }
        }

        int m_SampleCount = 0;

        #endregion

        // ================================================

        #region EVENTS

        // ================================================
        /// <summary>
        /// Invoked when the instance starts Recording.
        /// </summary>
        public event Action OnStartRecording;

        /// <summary>
        /// Invoked when an AudioClip couldn't be created to start recording.
        /// </summary>
        public event Action OnStartRecordingFailed;

        /// <summary>
        /// Invoked everytime an audio frame is collected. Includes the frame.
        /// </summary>
        public event Action<int, float[], float> OnSampleReady;

        /// <summary>
        /// Invoked when the instance stop Recording.
        /// </summary>
        public event Action OnStopRecording;

        #endregion

        // ================================================

        #region MIC

        // ================================================

        static Mic m_Instance;

        public static Mic Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = GameObject.FindObjectOfType<Mic>();
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("UniMic.Mic").AddComponent<Mic>();
                    DontDestroyOnLoad(m_Instance.gameObject);
                }

                return m_Instance;
            }
        }

        private void OnEnable()
        {
            SafeStartMicrophone();
        }

        private void OnDisable()
        {
            StopMicrophone();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                StopMicrophone();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopMicrophone();
            }
        }

        private void OnDestroy()
        {
            StopMicrophone();
        }

        // Safely start microphone
        public void SafeStartMicrophone()
        {
            // Can't start if inactive
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            // Look for devices
            if (Devices == null || Devices.Count == 0)
            {
                // Check for devices
                RefreshMicDevices();
                // None found
                if (Devices == null || Devices.Count == 0)
                {
                    return;
                }
            }

            // Ignore if already setup & recording
            string micID = CurrentDeviceName;
            if (!string.IsNullOrEmpty(micID) && AudioClip != null && string.Equals(micID, AudioClip.name) && Microphone.IsRecording(micID))
            {
                return;
            }

            // Set device
            ChangeDevice(CurrentDeviceIndex < 0 ? 0 : CurrentDeviceIndex);
        }

        /// <summary>
        /// Refresh mic device list
        /// </summary>
        public void RefreshMicDevices()
        {
            string oldDevice = CurrentDeviceName;
            _devices = new List<string>();
            UnityEngine.Profiling.Profiler.BeginSample("Microphone Devices");
            string[] micIDs = Microphone.devices;
            Debug.Log("Checking for Mic Devices");
            if (micIDs != null)
            {
                #if EDITOR_PERMISSION_POPUP
                if (Time.frameCount > 5)
                #endif
                {
                    _devices.AddRange(micIDs);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            CurrentDeviceIndex = _devices.IndexOf(oldDevice);
        }

        /// <summary>
        /// Changes to a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/></param>
        public void ChangeDevice(int index)
        {
            StopMicrophone();
            CurrentDeviceIndex = index;
            StartMicrophone();
        }

        private void StartMicrophone()
        {
            Debug.Log("[Mic] Reserved mic " + CurrentDeviceName);
            AudioClip = Microphone.Start(CurrentDeviceName, true, 1, AudioEncoding.samplerate);
            AudioClip.name = CurrentDeviceName;
        }

        private void StopMicrophone()
        {
            if (Microphone.IsRecording(CurrentDeviceName))
            {
                Debug.Log("[Mic] Released mic " + CurrentDeviceName);
                Microphone.End(CurrentDeviceName);
            }
            if (AudioClip != null)
            {
                Destroy(AudioClip);
                AudioClip = null;
            }
        }
        #endregion

        // ================================================

        #region RECORDING

        /// <summary>
        /// Starts to stream the input of the current Mic device
        /// </summary>
        public void StartRecording(int sampleLen = 10)
        {
            // Cant start unless available
            if (!IsInputAvailable)
            {
                SafeStartMicrophone();
            }
            // Still unavailable, exit
            if (!IsInputAvailable)
            {
                return;
            }

            // Stop recording if doing so
            StopRecording();

            IsRecording = true;

            SampleDurationMS = sampleLen;

            Sample = new float[AudioEncoding.samplerate / 1000 * SampleDurationMS * AudioClip.channels];

            if (AudioClip)
            {
                StartCoroutine(ReadRawAudio());

                // Make sure we seek before we start reading data
                Microphone.GetPosition(CurrentDeviceName);

                Debug.Log("[Mic] Started recording with " + CurrentDeviceName);
                if (OnStartRecording != null)
                    OnStartRecording.Invoke();
            }
            else
            {
                OnStartRecordingFailed.Invoke();
            }
        }

        /// <summary>
        /// Ends the Mic stream.
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording) return;

            IsRecording = false;

            StopCoroutine(ReadRawAudio());

            Debug.Log("[Mic] Stopped recording with " + CurrentDeviceName);
            if (OnStopRecording != null)
                OnStopRecording.Invoke();
        }

        IEnumerator ReadRawAudio()
        {
            int loops = 0;
            int readAbsPos = Microphone.GetPosition(CurrentDeviceName);
            int prevPos = readAbsPos;
            float[] temp = new float[Sample.Length];

            while (AudioClip != null && Microphone.IsRecording(CurrentDeviceName) && IsRecording)
            {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable && AudioClip)
                {
                    int currPos = Microphone.GetPosition(CurrentDeviceName);
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + temp.Length;
                    float levelMax = 0;

                    if (nextReadAbsPos < currAbsPos)
                    {
                        AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

                        for (int i = 0; i < temp.Length; i++)
                        {
                            float wavePeak = temp[i] * temp[i];
                            if (levelMax < wavePeak)
                            {
                                levelMax = wavePeak;
                            }
                        }

                        Sample = temp;
                        m_SampleCount++;
                        OnSampleReady?.Invoke(m_SampleCount, Sample, levelMax);

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }

                yield return null;
            }
        }

        #endregion
    }
}
