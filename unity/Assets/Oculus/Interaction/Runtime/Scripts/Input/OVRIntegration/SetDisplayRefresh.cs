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

using System.Linq;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    public class SetDisplayRefresh : MonoBehaviour
    {
        [SerializeField]
        private float _desiredDisplayFrequency = 90f;

        public void SetDesiredDisplayFrequency(float desiredDisplayFrequency)
        {
            var validFrequencies = OVRPlugin.systemDisplayFrequenciesAvailable;

            if (validFrequencies.Contains(_desiredDisplayFrequency))
            {
                Debug.Log("[Oculus.Interaction] Setting desired display frequency to " + _desiredDisplayFrequency);
                OVRPlugin.systemDisplayFrequency = _desiredDisplayFrequency;
            }
        }

        protected virtual void Awake()
        {
            SetDesiredDisplayFrequency(_desiredDisplayFrequency);
        }
    }
}
