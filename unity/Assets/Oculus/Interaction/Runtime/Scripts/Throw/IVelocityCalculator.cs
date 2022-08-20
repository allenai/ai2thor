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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Transform information used to derive velocities.
    /// </summary>
    public struct TransformSample
    {
        public TransformSample(Vector3 position, Quaternion rotation, float time,
          int frameIndex)
        {
            Position = position;
            Rotation = rotation;
            SampleTime = time;
            FrameIndex = frameIndex;
        }

        public static TransformSample Interpolate(TransformSample start,
          TransformSample fin, float time)
        {
            float alpha = Mathf.Clamp01(Mathf.InverseLerp(start.SampleTime,
              fin.SampleTime, time));

            return new TransformSample(Vector3.Lerp(start.Position, fin.Position, alpha),
              Quaternion.Slerp(start.Rotation, fin.Rotation, alpha),
              time, (int)Mathf.Lerp((float)start.FrameIndex, (float)fin.FrameIndex, alpha));
        }

        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float SampleTime;
        public readonly int FrameIndex;
    }

    /// <summary>
    /// Information related to release velocities such as linear and
    /// angular.
    /// </summary>
    public struct ReleaseVelocityInformation
    {
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public Vector3 Origin;
        public bool IsSelectedVelocity;

        public ReleaseVelocityInformation(Vector3 linearVelocity,
            Vector3 angularVelocity,
            Vector3 origin,
            bool isSelectedVelocity = false)
        {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            Origin = origin;
            IsSelectedVelocity = isSelectedVelocity;
        }
    }

    /// <summary>
    /// Interface to velocity calculator used to make throwing
    /// possible.
    /// </summary>
    public interface IVelocityCalculator
    {
        float UpdateFrequency { get; }

        event Action<List<ReleaseVelocityInformation>> WhenThrowVelocitiesChanged;

        event Action<ReleaseVelocityInformation> WhenNewSampleAvailable;

        ReleaseVelocityInformation CalculateThrowVelocity(Transform objectThrown);

        IReadOnlyList<ReleaseVelocityInformation> LastThrowVelocities();

        void SetUpdateFrequency(float frequency);
    }
}
