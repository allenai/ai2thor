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

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Reads a HandGrabState and applies hand visual constraints to a SyntheticHand
    /// </summary>
    public class HandGrabStateVisual : MonoBehaviour
    {
        [SerializeField]
        [Interface(typeof(IHandGrabState))]
        private MonoBehaviour _handGrabState;

        private IHandGrabState HandGrabState;

        [SerializeField]
        private SyntheticHand _syntheticHand;

        private bool _areFingersFree = true;
        private bool _isWristFree = true;
        private bool _wasCompletelyFree = true;

        protected bool _started = false;

        protected virtual void Awake()
        {
            HandGrabState = _handGrabState as IHandGrabState;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(HandGrabState);
            Assert.IsNotNull(_syntheticHand);
            this.EndStart(ref _started);
        }

        private void LateUpdate()
        {
            ConstrainingForce(HandGrabState, out float fingersConstraint, out float wristConstraint);
            UpdateHandPose(HandGrabState, fingersConstraint, wristConstraint);

            bool isCompletelyFree = _areFingersFree && _isWristFree;
            if (!isCompletelyFree
                || isCompletelyFree && !_wasCompletelyFree)
            {
                _syntheticHand.MarkInputDataRequiresUpdate();
            }
            _wasCompletelyFree = isCompletelyFree;
        }

        private void ConstrainingForce(IHandGrabState grabSource, out float fingersConstraint, out float wristConstraint)
        {
            HandGrabTarget grabData = grabSource.HandGrabTarget;

            fingersConstraint = wristConstraint = 0;
            if (grabData == null)
            {
                return;
            }

            bool isGrabbing = grabSource.IsGrabbing;
            if (isGrabbing && grabData.HandAlignment != HandAlignType.None)
            {
                fingersConstraint = grabSource.FingersStrength;
                wristConstraint = grabSource.WristStrength;
            }
            else if (grabData.HandAlignment == HandAlignType.AttractOnHover)
            {
                fingersConstraint = grabSource.FingersStrength;
                wristConstraint = grabSource.WristStrength;
            }
            else if (grabData.HandAlignment == HandAlignType.AlignFingersOnHover)
            {
                fingersConstraint = grabSource.FingersStrength;
            }
        }

        private void UpdateHandPose(IHandGrabState grabSource, float fingersConstraint, float wristConstraint)
        {
            HandGrabTarget grabTarget = grabSource.HandGrabTarget;

            if (grabTarget == null)
            {
                FreeFingers();
                FreeWrist();
                return;
            }

            if (fingersConstraint > 0f
                && grabTarget.HandPose != null)
            {
                UpdateFingers(grabTarget.HandPose, grabSource.GrabbingFingers(), fingersConstraint);
                _areFingersFree = false;
            }
            else
            {
                FreeFingers();
            }

            if (wristConstraint > 0f)
            {
                Pose wristLocalPose = GetWristPose(grabTarget.WorldGrabPose, grabSource.WristToGrabPoseOffset);
                _syntheticHand.LockWristPose(wristLocalPose, wristConstraint,
                    SyntheticHand.WristLockMode.Full, true);
                _isWristFree = false;
            }
            else
            {
                FreeWrist();
            }
        }

        /// <summary>
        /// Writes the desired rotation values for each joint based on the provided HandGrabState.
        /// Apart from the rotations it also writes in the syntheticHand if it should allow rotations
        /// past that.
        /// When no snap is provided, it frees all fingers allowing unconstrained tracked motion.
        /// </summary>
        private void UpdateFingers(HandPose handPose, HandFingerFlags grabbingFingers, float strength)
        {
            Quaternion[] desiredRotations = handPose.JointRotations;
            _syntheticHand.OverrideAllJoints(desiredRotations, strength);

            for (int fingerIndex = 0; fingerIndex < Constants.NUM_FINGERS; fingerIndex++)
            {
                int fingerFlag = 1 << fingerIndex;
                JointFreedom fingerFreedom = handPose.FingersFreedom[fingerIndex];
                if (fingerFreedom == JointFreedom.Constrained
                    && ((int)grabbingFingers & fingerFlag) != 0)
                {
                    fingerFreedom = JointFreedom.Locked;
                }
                _syntheticHand.SetFingerFreedom((HandFinger)fingerIndex, fingerFreedom);
            }
        }

        private Pose GetWristPose(Pose gripPoint, Pose offset)
        {
            Pose wristOffset = offset;
            wristOffset.Invert();
            gripPoint.Premultiply(wristOffset);
            return gripPoint;
        }

        private bool FreeFingers()
        {
            if (!_areFingersFree)
            {
                _syntheticHand.FreeAllJoints();
                _areFingersFree = true;
                return true;
            }
            return false;
        }

        private bool FreeWrist()
        {
            if (!_isWristFree)
            {
                _syntheticHand.FreeWrist();
                _isWristFree = true;
                return true;
            }
            return false;
        }

        #region Inject

        public void InjectAllHandGrabInteractorVisual(IHandGrabState handGrabState, SyntheticHand syntheticHand)
        {
            InjectHandGrabState(handGrabState);
            InjectSyntheticHand(syntheticHand);
        }

        public void InjectHandGrabState(IHandGrabState handGrabState)
        {
            HandGrabState = handGrabState;
            _handGrabState = handGrabState as MonoBehaviour;
        }

        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        #endregion
    }
}
