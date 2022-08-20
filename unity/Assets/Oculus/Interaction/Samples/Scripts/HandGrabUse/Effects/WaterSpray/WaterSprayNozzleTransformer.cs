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

namespace Oculus.Interaction.Demo
{
    public class WaterSprayNozzleTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField]
        private float _factor = 3f;
        [SerializeField]
        private float _snapAngle = 90;
        [SerializeField]
        [Range(0f, 1f)]
        private float _snappiness = 0.8f;
        [SerializeField]
        private int _maxSteps = 1;

        private float _relativeAngle = 0f;
        private int _stepsCount = 0;
        private IGrabbable _grabbable;

        private Pose _previousGrabPose;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void BeginTransform()
        {
            _previousGrabPose = _grabbable.GrabPoints[0];
            _relativeAngle = 0.0f;
            _stepsCount = 0;
        }

        public void UpdateTransform()
        {
            Pose grabPoint = _grabbable.GrabPoints[0];
            Transform targetTransform = _grabbable.Transform;
            Vector3 localAxis = Vector3.forward;
            Vector3 worldAxis = targetTransform.TransformDirection(localAxis);

            Vector3 initialVector = Vector3.ProjectOnPlane(_previousGrabPose.up, worldAxis).normalized;
            Vector3 targetVector = Vector3.ProjectOnPlane(grabPoint.up, worldAxis).normalized;

            float angleDelta = Vector3.SignedAngle(initialVector, targetVector, worldAxis) * _factor;

            _relativeAngle += angleDelta;

            if (Mathf.Abs(_relativeAngle) > _snapAngle * (1 - _snappiness)
                && Mathf.Abs(_stepsCount + Mathf.Sign(_relativeAngle)) <= _maxSteps)
            {
                float currentAngle = targetTransform.localEulerAngles.z;
                int turns = Mathf.FloorToInt((currentAngle + _snappiness * 0.5f) / _snapAngle);
                float sign = Mathf.Sign(_relativeAngle);
                float angle = sign > 0 ? _snapAngle * (turns + 1) : _snapAngle * turns;
                Vector3 rot = targetTransform.localEulerAngles;
                rot.z = angle;
                targetTransform.localEulerAngles = rot;

                _relativeAngle = 0;
                _stepsCount += (int)sign;
            }
            else
            {
                targetTransform.Rotate(worldAxis, angleDelta, Space.World);
            }

            _previousGrabPose = grabPoint;
        }

        public void EndTransform() { }
    }
}
