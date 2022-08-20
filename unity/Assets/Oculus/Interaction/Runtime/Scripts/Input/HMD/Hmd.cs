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

namespace Oculus.Interaction.Input
{
    public class Hmd : DataModifier<HmdDataAsset>, IHmd
    {
        public ITrackingToWorldTransformer TrackingToWorldTransformer =>
          GetData().Config.TrackingToWorldTransformer;

        public event Action HmdUpdated = delegate { };

        protected override void Apply(HmdDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                HmdUpdated();
            }
        }

        public bool GetRootPose(out Pose pose)
        {
            var currentData = GetData();

            if (!currentData.IsTracked)
            {
                pose = Pose.identity;
                return false;
            }
            pose = TrackingToWorldTransformer.ToWorldPose(currentData.Root);
            return true;
        }
    }
}
