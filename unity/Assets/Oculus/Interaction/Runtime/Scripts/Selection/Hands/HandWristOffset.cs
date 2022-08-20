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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Specifies a position and rotation offset from the wrist of the given hand
    /// </summary>
    public class HandWristOffset : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        [HideInInspector]
        private Vector3 _offset;

        [SerializeField]
        [HideInInspector]
        private Quaternion _rotation = Quaternion.identity;

        [SerializeField, Optional]
        [HideInInspector]
        private Transform _relativeTransform;

        private Pose _cachedPose = Pose.identity;

        public Vector3 Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
            }
        }

        private static readonly Quaternion LEFT_MIRROR_ROTATION = Quaternion.Euler(180f, 0f, 0f);

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        private void HandleHandUpdated()
        {
            if (Hand.GetRootPose(out Pose rootPose))
            {
                GetOffset(ref _cachedPose);
                _cachedPose.Postmultiply(rootPose);
                transform.SetPose(_cachedPose);
            }
        }

        public void GetOffset(ref Pose pose)
        {
            if (!_started)
            {
                return;
            }

            GetOffset(ref pose, Hand.Handedness, Hand.Scale);
        }

        public void GetOffset(ref Pose pose, Handedness handedness, float scale)
        {
            if (handedness == Handedness.Left)
            {
                pose.position = -_offset * scale;
                pose.rotation = _rotation * LEFT_MIRROR_ROTATION;
            }
            else
            {
                pose.position = _offset * scale;
                pose.rotation = _rotation;
            }
        }

        public void GetWorldPose(ref Pose pose)
        {
            pose.position = this.transform.position;
            pose.rotation = this.transform.rotation;
        }

        #region Inject
        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }
        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }
        public void InjectRotation(Quaternion rotation)
        {
            _rotation = rotation;
        }

        public void InjectAllHandWristOffset(IHand hand,
            Vector3 offset, Quaternion rotation)
        {
            InjectHand(hand);
            InjectOffset(offset);
            InjectRotation(rotation);
        }
        #endregion
    }
}
