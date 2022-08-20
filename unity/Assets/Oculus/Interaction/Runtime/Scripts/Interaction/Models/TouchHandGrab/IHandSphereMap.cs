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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// A thin HandJoint skeleton implementation that can be used for computing
    /// world joints from local joints data.
    /// </summary>
    public struct HandSphere
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public HandJointId Joint { get; }

        public HandSphere(Vector3 position, float radius, HandJointId joint)
        {
            this.Position = position;
            this.Radius = radius;
            this.Joint = joint;
        }
    }

    /// <summary>
    /// A mapping of hand joints to spheres that can be used for collision testing
    /// </summary>
    public interface IHandSphereMap
    {
        void GetSpheres(Handedness handedness, HandJointId joint, Pose pose, float scale,
            List<HandSphere> spheres);
    }
}
