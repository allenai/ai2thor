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
using UnityEngine.UI;

namespace Oculus.Interaction.HandGrab.Recorder
{
    public class TimerUIControl : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI _timerLabel;

        [SerializeField]
        private int _delaySeconds = 3;

        [SerializeField]
        private int _maxSeconds = 10;

        [SerializeField]
        private Button _moreButton;

        [SerializeField]
        private Button _lessButton;

        public int DelaySeconds
        {
            get
            {
                return _delaySeconds;
            }
            set
            {
                _delaySeconds = Mathf.Clamp(value, 0, _maxSeconds);
                UpdateDisplay(value);
            }
        }

        private void OnEnable()
        {
            _moreButton.onClick.AddListener(IncreaseTime);
            _lessButton.onClick.AddListener(DecreaseTime);
        }

        private void OnDisable()
        {
            _moreButton.onClick.RemoveListener(IncreaseTime);
            _lessButton.onClick.RemoveListener(DecreaseTime);
        }

        private void Start()
        {
            UpdateDisplay(DelaySeconds);
        }

        private void IncreaseTime()
        {
            DelaySeconds++;
        }

        private void DecreaseTime()
        {
            DelaySeconds--;
        }

        private void UpdateDisplay(int seconds)
        {
            _timerLabel.text = $"{seconds}\nseconds";
            _lessButton.interactable = seconds > 0;
            _moreButton.interactable = seconds < _maxSeconds;
        }
    }
}
