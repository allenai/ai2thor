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

using System.Collections.Generic;

namespace Oculus.Interaction.PoseDetection
{
    public static class TransformFeatureProperties
    {
        public static IReadOnlyDictionary<TransformFeature, FeatureDescription> FeatureDescriptions
        {
            get;
        } = CreateFeatureDescriptions();

        private static IReadOnlyDictionary<TransformFeature, FeatureDescription> CreateFeatureDescriptions()
        {
            int startIndex = 0;
            return new Dictionary<TransformFeature, FeatureDescription>
            {
                [TransformFeature.WristUp] = CreateDesc(ref startIndex),
                [TransformFeature.WristDown] = CreateDesc(ref startIndex),
                [TransformFeature.PalmDown] = CreateDesc(ref startIndex),
                [TransformFeature.PalmUp] = CreateDesc(ref startIndex),
                [TransformFeature.PalmTowardsFace] = CreateDesc(ref startIndex),
                [TransformFeature.PalmAwayFromFace] = CreateDesc(ref startIndex),
                [TransformFeature.FingersUp] = CreateDesc(ref startIndex),
                [TransformFeature.FingersDown] = CreateDesc(ref startIndex),
                [TransformFeature.PinchClear] = CreateDesc(ref startIndex),
            };
        }

        private static FeatureDescription CreateDesc(ref int startIndex)
        {
            var desc = new FeatureDescription("", "", 0, 180,
                new[]
                {
                    new FeatureStateDescription((startIndex).ToString(), "True"),
                    // to support legacy data (which had a 3rd intermediary step), need to skip 1.
                    new FeatureStateDescription((startIndex + 2).ToString(), "False")
                });
            startIndex += 3;
            return desc;
        }
    }
}
