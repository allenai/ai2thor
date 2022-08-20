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

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Oculus.Interaction.Samples
{
    /// <summary>
    /// Performs a callback after a countdown timer has elapsed.
    /// The countdown can be enabled or disabled by an external party.
    /// </summary>
    public class CountdownTimer : MonoBehaviour
    {
        [SerializeField]
        private float _countdownTime = 1.0f;

        [SerializeField]
        private bool _countdownOn = false;

        [SerializeField]
        private UnityEvent _callback;

        [SerializeField]
        private UnityEvent<float> _progressCallback;

        private float _countdownTimer;


        public bool CountdownOn
        {
            get => _countdownOn;

            set
            {
                if (value)
                {
                    if (!_countdownOn)
                    {
                        _countdownTimer = _countdownTime;
                    }
                }

                _countdownOn = value;
            }
        }

        private void Awake()
        {
            Assert.IsTrue(_countdownTime >= 0, "Countdown Time must be positive.");
        }

        private void Update()
        {
            if (!_countdownOn || _countdownTimer < 0)
            {
                _progressCallback.Invoke(0);
                return;
            }

            _countdownTimer -= Time.deltaTime;
            if (_countdownTimer < 0f)
            {
                _countdownTimer = -1f;
                _callback.Invoke();
                _progressCallback.Invoke(1);
                return;
            }

            _progressCallback.Invoke(1 - _countdownTimer / _countdownTime);
        }
    }
}
