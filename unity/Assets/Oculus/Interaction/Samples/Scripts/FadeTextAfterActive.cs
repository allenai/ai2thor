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

using TMPro;
using UnityEngine;

namespace Oculus.Interaction.Samples
{
    public class FadeTextAfterActive : MonoBehaviour
    {
        [SerializeField] float _fadeOutTime;
        [SerializeField] TextMeshPro _text;

        float _timeLeft;

        protected virtual void OnEnable()
        {
            _timeLeft = _fadeOutTime;
            _text.fontMaterial.color = new Color(_text.color.r, _text.color.g, _text.color.b, 255);
        }

        protected virtual void Update()
        {
            if (_timeLeft <= 0)
            {
                return;
            }

            float percentDone = 1 - _timeLeft / _fadeOutTime;
            float alpha = Mathf.SmoothStep(1, 0, percentDone);
            _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, alpha);
            _timeLeft -= Time.deltaTime;
        }
    }
}
