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

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Test if hand joint is inside generic collider and updates its active state
    /// based on that test. We could trigger-based testing, but if the hand disappears
    /// during one frame, we will not get a trigger exit event (which means we require
    /// manual testing in Update anyway to accomodate that edge case).
    /// </summary>
    public class ColliderContainsHandJointActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand;

        [SerializeField]
        private Collider[] _entryColliders;

        [SerializeField]
        private Collider[] _exitColliders;

        [SerializeField]
        private HandJointId _jointToTest = HandJointId.HandWristRoot;

        public bool Active { get; private set; }

        private bool _active = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            Active = false;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Hand);
            Assert.IsTrue(_entryColliders != null && _entryColliders.Length > 0);
            Assert.IsTrue(_exitColliders != null && _exitColliders.Length > 0);
        }

        protected virtual void Update()
        {
            if (Hand.GetJointPose(_jointToTest, out Pose jointPose))
            {
                Active = JointPassesTests(jointPose);
            }
            else
            {
                Active = false;
            }
        }

        private bool JointPassesTests(Pose jointPose)
        {
            bool passesCollisionTest;

            if (_active)
            {
                passesCollisionTest = IsPointWithinColliders(jointPose.position,
                    _exitColliders);
            }
            else
            {
                passesCollisionTest = IsPointWithinColliders(jointPose.position,
                    _entryColliders);
            }

            _active = passesCollisionTest;
            return passesCollisionTest;
        }

        private bool IsPointWithinColliders(Vector3 point, Collider[] colliders)
        {
            foreach (var collider in colliders)
            {
                if (!Collisions.IsPointWithinCollider(point, collider))
                {
                    return false;
                }
            }
            return true;
        }

        #region Inject

        public void InjectAllColliderContainsHandJointActiveState(IHand hand, Collider[] entryColliders,
            Collider[] exitColliders, HandJointId jointToTest)
        {
            InjectHand(hand);
            InjectEntryColliders(entryColliders);
            InjectExitColliders(exitColliders);
            InjectJointToTest(jointToTest);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectEntryColliders(Collider[] entryColliders)
        {
            _entryColliders = entryColliders;
        }

        public void InjectExitColliders(Collider[] exitColliders)
        {
            _exitColliders = exitColliders;
        }

        public void InjectJointToTest(HandJointId jointToTest)
        {
            _jointToTest = jointToTest;
        }

        #endregion
    }
}
