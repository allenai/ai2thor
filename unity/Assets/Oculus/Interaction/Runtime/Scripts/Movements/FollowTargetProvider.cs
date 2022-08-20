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

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This IPoseMovement constantly moves the pose at a fixed rate
    /// towards the target.
    /// </summary>
    public class FollowTargetProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField]
        private float _speed = 5f;

        private Transform _space;

        private void Awake()
        {
            _space = this.transform;
        }

        public IMovement CreateMovement()
        {
            return new FollowTarget(_speed, _space);
        }
    }

    public class FollowTarget : IMovement
    {
        private float _speed;
        private Transform _space;

        public Pose Pose => ToWorld(_localPose);
        public bool Stopped => false;

        private Pose _localTarget;
        private Pose _localPose;

        private float _startTime;

        private const float ROTATION_SPEED_FACTOR = 50f;

        public FollowTarget(float speed, Transform space)
        {
            _speed = speed;
            _space = space;
        }

        private Pose ToLocal(in Pose pose)
        {
            Vector3 localPos = _space.InverseTransformPoint(pose.position);
            Quaternion localRot = Quaternion.Inverse(_space.rotation) * pose.rotation;

            return new Pose(localPos, localRot);
        }

        private Pose ToWorld(in Pose pose)
        {
            Vector3 worldPos = _space.TransformPoint(pose.position);
            Quaternion worldRot = _space.rotation * pose.rotation;
            return new Pose(worldPos, worldRot);
        }

        public void MoveTo(Pose target)
        {
            _startTime = Time.time;
            _localTarget = ToLocal(target);
        }

        public void UpdateTarget(Pose target)
        {
            _localTarget = ToLocal(target);
            Tick();
        }

        public void StopAndSetPose(Pose source)
        {
            _localPose = ToLocal(source);
        }

        public void Tick()
        {

            float now = Time.time;
            float delta = (now - _startTime) * _speed;
            _startTime = now;

            _localPose.position = Vector3.MoveTowards(_localPose.position, _localTarget.position, delta);
            _localPose.rotation = Quaternion.RotateTowards(_localPose.rotation, _localTarget.rotation, delta * ROTATION_SPEED_FACTOR);
        }
    }
}
