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
    public class HandDataAsset : ICopyFrom<HandDataAsset>
    {
        public bool IsDataValid;
        public bool IsConnected;
        public bool IsTracked;
        public Pose Root;
        public PoseOrigin RootPoseOrigin;
        public Quaternion[] Joints = new Quaternion[Constants.NUM_HAND_JOINTS];
        public bool IsHighConfidence;
        public bool[] IsFingerPinching = new bool[Constants.NUM_FINGERS];
        public bool[] IsFingerHighConfidence = new bool[Constants.NUM_FINGERS];
        public float[] FingerPinchStrength = new float[Constants.NUM_FINGERS];
        public float HandScale;
        public Pose PointerPose;
        public PoseOrigin PointerPoseOrigin;
        public bool IsDominantHand;
        public HandDataSourceConfig Config = new HandDataSourceConfig();

        public bool IsDataValidAndConnected => IsDataValid && IsConnected;

        public void CopyFrom(HandDataAsset source)
        {
            IsDataValid = source.IsDataValid;
            IsConnected = source.IsConnected;
            IsTracked = source.IsTracked;
            IsHighConfidence = source.IsHighConfidence;
            IsDominantHand = source.IsDominantHand;
            Config = source.Config;
            CopyPosesFrom(source);
        }

        public void CopyPosesFrom(HandDataAsset source)
        {
            Root = source.Root;
            RootPoseOrigin = source.RootPoseOrigin;
            Array.Copy(source.Joints, Joints, Constants.NUM_HAND_JOINTS);
            Array.Copy(source.IsFingerPinching, IsFingerPinching, IsFingerPinching.Length);
            Array.Copy(source.IsFingerHighConfidence, IsFingerHighConfidence,
                IsFingerHighConfidence.Length);
            Array.Copy(source.FingerPinchStrength, FingerPinchStrength, FingerPinchStrength.Length);
            HandScale = source.HandScale;
            PointerPose = source.PointerPose;
            PointerPoseOrigin = source.PointerPoseOrigin;
            Config = source.Config;
        }
    }
}
