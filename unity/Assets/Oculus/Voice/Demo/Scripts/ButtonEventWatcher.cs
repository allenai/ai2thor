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
using UnityEngine.Events;
using UnityEngine.UI;

namespace Oculus.Voice.Demo
{
    public class ButtonEventWatcher : MonoBehaviour
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        // By default: uses space bar, oculus quest a button and oculus quest x button
        [SerializeField] private KeyCode[] _keys = new KeyCode[] { KeyCode.Space, KeyCode.JoystickButton0, KeyCode.JoystickButton2 };
#endif

        // Used for click or hold events
        public UnityEvent OnButtonDown;
        // Used for button up hold events
        public UnityEvent OnButtonUp;

#if ENABLE_LEGACY_INPUT_MANAGER
        // Update activation
        void Update()
        {
            // Iterate keys
            foreach (var key in _keys)
            {
                if (Input.GetKeyDown(key))
                {
                    OnButtonDown?.Invoke();
                }
                else if (Input.GetKeyUp(key))
                {
                    OnButtonUp?.Invoke();
                }
            }
        }
#endif
    }
}
