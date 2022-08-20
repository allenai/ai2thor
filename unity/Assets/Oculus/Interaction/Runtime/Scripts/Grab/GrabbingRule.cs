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
using UnityEngine;

namespace Oculus.Interaction.GrabAPI
{
    public enum FingerRequirement
    {
        Ignored,
        Optional,
        Required
    }

    public enum FingerUnselectMode
    {
        AllReleased,
        AnyReleased
    }

    /// <summary>
    /// This struct indicates which fingers should be taken in count
    /// for performing an interaction (typically a hand-grab).
    /// All required fingers must be in use in order to start the interaction
    /// while any of the optional fingers is needed.
    /// For finishing the action it support either releasing all fingers or
    /// any of them.
    /// </summary>
    [System.Serializable]
    public struct GrabbingRule
    {
        [SerializeField]
        private FingerRequirement _thumbRequirement;
        [SerializeField]
        private FingerRequirement _indexRequirement;
        [SerializeField]
        private FingerRequirement _middleRequirement;
        [SerializeField]
        private FingerRequirement _ringRequirement;
        [SerializeField]
        private FingerRequirement _pinkyRequirement;

        [SerializeField]
        private FingerUnselectMode _unselectMode;

        public FingerUnselectMode UnselectMode => _unselectMode;

        public bool SelectsWithOptionals
        {
            get
            {
                return _thumbRequirement != FingerRequirement.Required
                    && _indexRequirement != FingerRequirement.Required
                    && _middleRequirement != FingerRequirement.Required
                    && _ringRequirement != FingerRequirement.Required
                    && _pinkyRequirement != FingerRequirement.Required;
            }
        }

        public FingerRequirement this[HandFinger fingerID]
        {
            get
            {
                switch (fingerID)
                {
                    case HandFinger.Thumb: return _thumbRequirement;
                    case HandFinger.Index: return _indexRequirement;
                    case HandFinger.Middle: return _middleRequirement;
                    case HandFinger.Ring: return _ringRequirement;
                    case HandFinger.Pinky: return _pinkyRequirement;
                }
                return FingerRequirement.Ignored;
            }
            set
            {
                switch (fingerID)
                {
                    case HandFinger.Thumb: _thumbRequirement = value; break;
                    case HandFinger.Index: _indexRequirement = value; break;
                    case HandFinger.Middle: _middleRequirement = value; break;
                    case HandFinger.Ring: _ringRequirement = value; break;
                    case HandFinger.Pinky: _pinkyRequirement = value; break;
                }
            }
        }

        public void StripIrrelevant(ref HandFingerFlags fingerFlags)
        {
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (this[finger] == FingerRequirement.Ignored)
                {
                    fingerFlags = (HandFingerFlags)((int)fingerFlags & ~(1 << i));
                }
            }
        }

        public GrabbingRule(HandFingerFlags mask, in GrabbingRule otherRule)
        {
            _thumbRequirement = (mask & HandFingerFlags.Thumb) != 0 ?
                otherRule._thumbRequirement : FingerRequirement.Ignored;

            _indexRequirement = (mask & HandFingerFlags.Index) != 0 ?
                otherRule._indexRequirement : FingerRequirement.Ignored;

            _middleRequirement = (mask & HandFingerFlags.Middle) != 0 ?
                otherRule._middleRequirement : FingerRequirement.Ignored;

            _ringRequirement = (mask & HandFingerFlags.Ring) != 0 ?
                otherRule._ringRequirement : FingerRequirement.Ignored;

            _pinkyRequirement = (mask & HandFingerFlags.Pinky) != 0 ?
                otherRule._pinkyRequirement : FingerRequirement.Ignored;

            _unselectMode = otherRule.UnselectMode;
        }

        #region Defaults

        public static GrabbingRule DefaultPalmRule { get; } = new GrabbingRule()
        {
            _thumbRequirement = FingerRequirement.Optional,
            _indexRequirement = FingerRequirement.Required,
            _middleRequirement = FingerRequirement.Required,
            _ringRequirement = FingerRequirement.Required,
            _pinkyRequirement = FingerRequirement.Optional,

            _unselectMode = FingerUnselectMode.AllReleased
        };

        public static GrabbingRule DefaultPinchRule { get; } = new GrabbingRule()
        {
            _thumbRequirement = FingerRequirement.Required,
            _indexRequirement = FingerRequirement.Optional,
            _middleRequirement = FingerRequirement.Optional,
            _ringRequirement = FingerRequirement.Ignored,
            _pinkyRequirement = FingerRequirement.Ignored,

            _unselectMode = FingerUnselectMode.AllReleased
        };

        public static GrabbingRule FullGrab { get; } = new GrabbingRule()
        {
            _thumbRequirement = FingerRequirement.Required,
            _indexRequirement = FingerRequirement.Required,
            _middleRequirement = FingerRequirement.Required,
            _ringRequirement = FingerRequirement.Required,
            _pinkyRequirement = FingerRequirement.Required,

            _unselectMode = FingerUnselectMode.AllReleased
        };

        #endregion
    }
}
