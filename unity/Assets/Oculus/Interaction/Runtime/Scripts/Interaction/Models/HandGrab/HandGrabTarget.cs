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
    /// All the relevant data needed for a snapping position.
    /// This includes the Interactor and the surface (if any) around
    /// which the pose is valid.
    /// </summary>
    public class HandGrabTarget
    {
        public enum GrabAnchor
        {
            None,
            Wrist,
            Pinch,
            Palm
        }

        public ref Pose Pose => ref _pose;

        public HandPose HandPose => _isHandPoseValid ? _handPose : null;

        public Pose WorldGrabPose => _relativeTo != null ? _relativeTo.GlobalPose(Pose) : Pose.identity;
        public HandAlignType HandAlignment => _handAlignment;

        public GrabAnchor Anchor { get; private set; } = GrabAnchor.None;

        private bool _isHandPoseValid = false;
        private HandPose _handPose = new HandPose();
        private Pose _pose;

        private Transform _relativeTo;
        private HandAlignType _handAlignment;

        public void Set(HandGrabTarget other)
        {
            _relativeTo = other._relativeTo;
            _handAlignment = other._handAlignment;
            _pose = other._pose;
            _isHandPoseValid = other._isHandPoseValid;
            _handPose.CopyFrom(other._handPose);
            Anchor = other.Anchor;
        }

        public void Set(Transform relativeTo, HandAlignType handAlignment, GrabAnchor anchor, HandGrabResult result)
        {
            Anchor = anchor;
            _relativeTo = relativeTo;
            _handAlignment = handAlignment;
            _pose.CopyFrom(result.SnapPose);
            _isHandPoseValid = result.HasHandPose;
            if (_isHandPoseValid)
            {
                _handPose.CopyFrom(result.HandPose);
            }
        }

        public void Clear()
        {
            Anchor = GrabAnchor.None;
            _isHandPoseValid = false;
            _relativeTo = null;
            _handAlignment = HandAlignType.None;
        }
    }
}
