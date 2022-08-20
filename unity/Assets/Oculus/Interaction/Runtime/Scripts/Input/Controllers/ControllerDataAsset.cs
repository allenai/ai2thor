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
    [Serializable]
    public class ControllerDataAsset : ICopyFrom<ControllerDataAsset>
    {
        public bool IsDataValid;
        public bool IsConnected;
        public bool IsTracked;
        public ControllerButtonUsage ButtonUsageMask;
        public Pose RootPose;
        public PoseOrigin RootPoseOrigin;
        public Pose PointerPose;
        public PoseOrigin PointerPoseOrigin;
        public ControllerDataSourceConfig Config;

            public void CopyFrom(ControllerDataAsset source)
        {
            IsDataValid = source.IsDataValid;
            IsConnected = source.IsConnected;
            IsTracked = source.IsTracked;
            Config = source.Config;
            CopyPosesAndStateFrom(source);
        }

        public void CopyPosesAndStateFrom(ControllerDataAsset source)
        {
            ButtonUsageMask = source.ButtonUsageMask;
            RootPose = source.RootPose;
            RootPoseOrigin = source.RootPoseOrigin;
            PointerPose = source.PointerPose;
            PointerPoseOrigin = source.PointerPoseOrigin;
        }
    }
}
