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

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Voice.Demo.BuiltInDemo
{
    /// <summary>
    /// Represents a countdown timer.
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        private double _time = 0; // [sec] current time of the countdown timer.
        private bool _timerExist = false;
        private bool _timerRunning = false;

        [Tooltip("The UI text element to show app messages.")]
        public Text logText;

        [Tooltip("The timer ring sound.")] public AudioClip buzzSound;

        // Update is called once per frame
        void Update()
        {
            if (_timerExist && _timerRunning)
            {
                _time -= Time.deltaTime;
                if (_time < 0)
                {
                    // Raise a ring.
                    OnElapsedTime();
                }
            }
        }

        private void Log(string msg)
        {
            Debug.Log(msg);
            logText.text = msg;
        }

        /// <summary>
        /// Buzzes and resets the timer.
        /// </summary>
        private void OnElapsedTime()
        {
            _time = 0;
            _timerRunning = false;
            _timerExist = false;
            Log("Buzz!");
            AudioSource.PlayClipAtPoint(buzzSound, Vector3.zero);
        }

        /// <summary>
        /// Deletes the timer. It corresponds to the wit intent "wit$delete_timer"
        /// </summary>
        public void DeleteTimer()
        {
            if (!_timerExist)
            {
                Log("Error: There is no timer to delete.");
                return;
            }

            _timerExist = false;
            _time = 0;
            _timerRunning = false;
            Log("Timer deleted.");
        }

        /// <summary>
        /// Creates a timer. It corresponds to the wit intent "wit$create_timer"
        /// </summary>
        /// <param name="entityValues">countdown in minutes.</param>
        public void CreateTimer(string[] entityValues)
        {
            if (_timerExist)
            {
                Log("A timer already exist.");
                return;
            }

            if (ParseTime(entityValues, out _time))
            {
                _timerExist = true;
                _timerRunning = true;
                Log($"Countdown Timer is set for {entityValues[0]} {entityValues[1]}(s).");
            }
            else
            {
                Log("Error in CreateTimer(): Could not parse wit reply.");
            }
        }

        /// <summary>
        /// Displays current timer value. It corresponds to "wit$get_timer".
        /// </summary>
        public void GetTimerIntent()
        {
            // Show the remaining time of the countdown timer.
            var msg = GetFormattedTimeFromSeconds();
            Log(msg);
        }

        /// <summary>
        /// Pauses the timer. It corresponds to the wit intent "wit$pause_timer"
        /// </summary>
        public void PauseTimer()
        {
            _timerRunning = false;
            Log("Timer paused.");
        }

        /// <summary>
        /// It corresponds to the wit intent "wit$resume_timer"
        /// </summary>
        public void ResumeTimer()
        {
            _timerRunning = true;
            Log("Timer resumed.");
        }

        /// <summary>
        /// Subtracts time from the timer. It corresponds to the wit intent "wit$subtract_time_timer".
        /// </summary>
        /// <param name="entityValues"></param>
        public void SubtractTimeTimer(string[] entityValues)
        {
            if (!_timerExist)
            {
                Log("Error: No Timer is created.");
                return;
            }

            if (ParseTime(entityValues, out var time))
            {
                var msg = $"{entityValues[0]} {entityValues[1]}(s) were subtracted from the timer.";
                _time -= time;
                if (_time < 0)
                {
                    _time = 0;
                    Log(msg);
                    return;
                }

                Log(msg);
            }
            else
            {
                Log("Error in Subtract_time_timer(): Could not parse the wit reply.");
            }
        }

        /// <summary>
        /// Adds time to the timer. It corresponds to the wit intent "wit$add_time_timer".
        /// </summary>
        /// <param name="entityValues"></param>
        public void AddTimeToTimer(string[] entityValues)
        {
            if (!_timerExist)
            {
                Log("Error: No Timer is created.");
                return;
            }

            if (ParseTime(entityValues, out var time))
            {
                _time += time;
                var msg = $"{entityValues[0]} {entityValues[1]}(s) were added to the timer.";
                Log(msg);
            }
            else
            {
                Log("Error in AddTimeToTimer(): Could not parse with reply.");
            }
        }

        /// <summary>
        /// Returns the remaining time (in sec) of the countdown timer.
        /// </summary>
        /// <returns></returns>
        public double GetRemainingTime()
        {
            return _time;
        }

        /// <summary>
        /// Returns time in the format of min:sec.
        /// </summary>
        /// <returns></returns>
        public string GetFormattedTimeFromSeconds()
        {
            if (_time >= TimeSpan.MaxValue.TotalSeconds)
            {
                _time = TimeSpan.MaxValue.TotalSeconds - 1;
                Log("Error: Hit max time");
            }
            TimeSpan span = TimeSpan.FromSeconds(_time);
            return $"{Math.Floor(span.TotalHours)}:{span.Minutes:00}:{span.Seconds:00}.{Math.Floor(span.Milliseconds/100f)}";
        }

        /// <summary>
        /// Parses entity values to get a resulting time value in seconds
        /// </summary>
        /// <param name="entityValues">The entity value results from a Response Handler</param>
        /// <param name="time">The parsed time</param>
        /// <returns>The parsed time in seconds or the current value of _time</returns>
        /// <exception cref="ArgumentException"></exception>
        private bool ParseTime(string[] entityValues, out double time)
        {
            time = _time;
            if (entityValues.Length > 0 && double.TryParse(entityValues[0], out time))
            {
                if (entityValues.Length < 2)
                {
                    throw new ArgumentException("Entities being parsed must include time value and unit.");
                }

                // If entity was not included in the result it will be empty, but the array will still be size 2
                if (!string.IsNullOrEmpty(entityValues[1]))
                {
                    switch (entityValues[1])
                    {
                        case "minute":
                            time *= 60;
                            break;
                        case "hour":
                            time *= 60 * 60;
                            break;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
