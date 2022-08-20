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

namespace Oculus.Interaction.Samples
{
    public class StayInView : MonoBehaviour
    {
        [SerializeField]
        private Transform _eyeCenter;

        [SerializeField]
        private float _extraDistanceForward = 0;

        [SerializeField]
        private bool _zeroOutEyeHeight = true;
        void Update()
        {
            transform.rotation = Quaternion.identity;
            transform.position = _eyeCenter.position;
            transform.Rotate(0, _eyeCenter.rotation.eulerAngles.y, 0, Space.Self);
            transform.position = _eyeCenter.position + transform.forward.normalized * _extraDistanceForward;
            if (_zeroOutEyeHeight)
                transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }
}
