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

namespace Oculus.Interaction.HandGrab
{
    [Serializable]
    public struct PoseMeasureParameters
    {
        [SerializeField]
        [Min(0f)]
        private float _maxDistance;

        [SerializeField]
        [Range(0f, 1f)]
        private float _positionRotationWeight;

        public float MaxDistance => _maxDistance;
        public float PositionRotationWeight => _positionRotationWeight;

        public PoseMeasureParameters(float maxDistance, float positionRotationWeight)
        {
            _maxDistance = maxDistance;
            _positionRotationWeight = positionRotationWeight;
        }

        public static readonly PoseMeasureParameters DEFAULT = new PoseMeasureParameters(0.1f, 0f);
    }
}
