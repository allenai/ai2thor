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
    public class MoveTowardsTargetProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField]
        private PoseTravelData _travellingData = PoseTravelData.FAST;

        public IMovement CreateMovement()
        {
            return new MoveTowardsTarget(_travellingData);
        }

        #region Inject
        public void InjectAllMoveTowardsTargetProvider(PoseTravelData travellingData)
        {
            InjectTravellingData(travellingData);
        }

        public void InjectTravellingData(PoseTravelData travellingData)
        {
            _travellingData = travellingData;
        }
        #endregion
    }

    public class MoveTowardsTarget : IMovement
    {
        private PoseTravelData _travellingData;

        public Pose Pose => _tween.Pose;
        public bool Stopped => _tween != null && _tween.Stopped;

        private Tween _tween;
        private Pose _source;
        private Pose _target;

        public MoveTowardsTarget(PoseTravelData travellingData)
        {
            _travellingData = travellingData;
        }

        public void MoveTo(Pose target)
        {
            _target = target;
            _tween = _travellingData.CreateTween(_source, target);
        }

        public void UpdateTarget(Pose target)
        {
            if (_target != target)
            {
                _target = target;
                _tween.UpdateTarget(_target);
            }
        }

        public void StopAndSetPose(Pose pose)
        {
            _source = pose;
            _tween?.StopAndSetPose(_source);
        }

        public void Tick()
        {
            _tween.Tick();
        }
    }
}
