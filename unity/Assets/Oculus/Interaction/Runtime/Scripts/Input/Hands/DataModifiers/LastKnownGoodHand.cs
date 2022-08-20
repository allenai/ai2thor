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

namespace Oculus.Interaction.Input
{
    public class LastKnownGoodHand : Hand
    {
        private readonly HandDataAsset _lastState = new HandDataAsset();

        protected override void Apply(HandDataAsset data)
        {
            bool shouldUseData = data.IsHighConfidence ||
                                 data.RootPoseOrigin == PoseOrigin.FilteredTrackedPose ||
                                 data.RootPoseOrigin == PoseOrigin.SyntheticPose;
            if (data.IsDataValid && data.IsTracked && shouldUseData)
            {
                _lastState.CopyFrom(data);
            }
            else if (_lastState.IsDataValid && data.IsConnected)
            {
                // No high confidence data, use last known good.
                // Only copy pose data, not confidence/tracked flags.
                data.CopyPosesFrom(_lastState);
                data.RootPoseOrigin = PoseOrigin.SyntheticPose;
                data.IsDataValid = true;
                data.IsTracked = true;
                data.IsHighConfidence = true;
            }
            else
            {
                // This hand is not connected, or has never seen valid data.
                data.IsTracked = false;
                data.IsHighConfidence = false;
                data.RootPoseOrigin = PoseOrigin.None;
            }
        }
    }
}
