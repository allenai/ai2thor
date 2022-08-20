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

using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection
{
    public class HmdOffset : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHmd))]
        private MonoBehaviour _hmd;
        private IHmd Hmd;

        [SerializeField]
        private Vector3 _offsetTranslation = Vector3.zero;
        [SerializeField]
        private Vector3 _offsetRotation = Vector3.zero;

        [SerializeField]
        private bool _disablePitchFromSource = false;
        [SerializeField]
        private bool _disableYawFromSource = false;
        [SerializeField]
        private bool _disableRollFromSource = false;

        protected virtual void Awake()
        {
            Hmd = _hmd as IHmd;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Hmd);
        }

        protected virtual void Update()
        {
            if (!Hmd.GetRootPose(out Pose centerEyePose))
            {
                return;
            }

            var centerEyePosition = centerEyePose.position;
            var centerEyeRotation = centerEyePose.rotation;

            var eulerAngles = centerEyeRotation.eulerAngles;
            var pitch = Quaternion.Euler(new Vector3(eulerAngles.x, 0.0f, 0.0f));
            var yaw = Quaternion.Euler(new Vector3(0.0f, eulerAngles.y, 0.0f));
            var roll = Quaternion.Euler(new Vector3(0.0f, 0.0f, eulerAngles.z));
            var finalSourceRotation = Quaternion.identity;

            if (!_disableYawFromSource)
            {
                finalSourceRotation *= yaw;
            }
            if (!_disablePitchFromSource)
            {
                finalSourceRotation *= pitch;
            }
            if (!_disableRollFromSource)
            {
                finalSourceRotation *= roll;
            }

            var totalRotation = finalSourceRotation * Quaternion.Euler(_offsetRotation);
            transform.position = centerEyePosition +
                totalRotation * _offsetTranslation;
            transform.rotation = totalRotation;
        }

        #region Inject

        public void InjectAllHmdOffset(IHmd hmd)
        {
            InjectHmd(hmd);
        }

        public void InjectHmd(IHmd hmd)
        {
            _hmd = hmd as MonoBehaviour;
            Hmd = hmd;
        }

        public void InjectOptionalOffsetTranslation(Vector3 val)
        {
            _offsetTranslation = val;
        }

        public void InjectOptionalOffsetRotation(Vector3 val)
        {
            _offsetRotation = val;
        }

        public void InjectOptionalDisablePitchFromSource(bool val)
        {
            _disablePitchFromSource = val;
        }

        public void InjectOptionalDisableYawFromSource(bool val)
        {
            _disableYawFromSource = val;
        }

        public void InjectOptionalDisableRollFromSource(bool val)
        {
            _disableRollFromSource = val;
        }

        #endregion
    }
}
