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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Alters hand data piped into this modifier to lock and unlock joints (wrist position and rotation,
    /// finger joint rotations)  When switching between locked and unlocked states, additionally smooths
    /// out transitions by easing between source hand data and target hand data.
    /// </summary>
    public class SyntheticHand : Hand
    {
        [System.Flags]
        public enum WristLockMode
        {
            Position = 1 << 0,
            Rotation = 1 << 1,
            Full = (1 << 2) - 1
        }

        [SerializeField]
        private ProgressCurve _wristPositionLockCurve = new ProgressCurve();
        [SerializeField]
        private ProgressCurve _wristPositionUnlockCurve;
        [SerializeField]
        private ProgressCurve _wristRotationLockCurve;
        [SerializeField]
        private ProgressCurve _wristRotationUnlockCurve;
        [SerializeField]
        private ProgressCurve _jointLockCurve;
        [SerializeField]
        private ProgressCurve _jointUnlockCurve;

        /// <summary>
        /// Use this factor to control how much the fingers can spread when nearby a constrained pose.
        /// </summary>
        [SerializeField]
        [Tooltip("Use this factor to control how much the fingers can spread when nearby a constrained pose.")]
        private float _spreadAllowance = 5f;

        public System.Action UpdateRequired = delegate { };

        private readonly HandDataAsset _lastStates = new HandDataAsset();

        private float _wristPositionOverrideFactor;
        private float _wristRotationOverrideFactor;

        private float[] _jointsOverrideFactor = new float[FingersMetadata.HAND_JOINT_IDS.Length];

        private ProgressCurve[] _jointLockProgressCurves = new ProgressCurve[FingersMetadata.HAND_JOINT_IDS.Length];
        private ProgressCurve[] _jointUnlockProgressCurves = new ProgressCurve[FingersMetadata.HAND_JOINT_IDS.Length];

        private Pose _desiredWristPose;
        private bool _wristPositionLocked;
        private bool _wristRotationLocked;
        private Pose _constrainedWristPose;
        private Pose _lastWristPose;

        private Quaternion[] _desiredJointsRotation = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];
        private Quaternion[] _constrainedJointRotations = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];
        private Quaternion[] _lastSyntheticRotation = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];
        private JointFreedom[] _jointsFreedomLevels = new JointFreedom[FingersMetadata.HAND_JOINT_IDS.Length];

        private bool _hasConnectedData;

        protected override void Start()
        {
            base.Start();

            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                _jointLockProgressCurves[i] = new ProgressCurve(_jointLockCurve);
                _jointUnlockProgressCurves[i] = new ProgressCurve(_jointUnlockCurve);
            }
        }

        protected override void Apply(HandDataAsset data)
        {
            if (!data.IsDataValid || !data.IsTracked || !data.IsHighConfidence)
            {
                data.IsConnected = false;
                data.RootPoseOrigin = PoseOrigin.None;
                _hasConnectedData = false;
                return;
            }

            UpdateRequired.Invoke();
            _lastStates.CopyFrom(data);

            if (!_hasConnectedData)
            {
                _constrainedWristPose.CopyFrom(data.Root);
                _hasConnectedData = true;
            }

            UpdateJointsRotation(data);
            UpdateRootPose(ref data.Root);

            data.RootPoseOrigin = PoseOrigin.SyntheticPose;
        }

        /// <summary>
        /// Updates the pose of the root of the hand
        /// using the visual provided values. Sometimes this
        /// might require lerping between the tracked pose
        /// and the provided one to improve the movement of the hand
        /// without worrying about when the overwrite value was written.
        ///
        /// During this update, the modifier also ensures the unlocking
        /// animations are executed.
        /// </summary>
        /// <param name="root">The tracked root value to modify</param>
        private void UpdateRootPose(ref Pose root)
        {
            float smoothPositionFactor = _wristPositionLocked ? _wristPositionLockCurve.Progress() : _wristPositionUnlockCurve.Progress();
            Vector3 position = Vector3.Lerp(root.position, _desiredWristPose.position, _wristPositionOverrideFactor);
            root.position = Vector3.Lerp(_constrainedWristPose.position, position, smoothPositionFactor);

            float smoothRotationFactor = _wristRotationLocked ? _wristRotationLockCurve.Progress() : _wristRotationUnlockCurve.Progress();
            Quaternion rotation = Quaternion.Lerp(root.rotation, _desiredWristPose.rotation, _wristRotationOverrideFactor);
            root.rotation = Quaternion.Lerp(_constrainedWristPose.rotation, rotation, smoothRotationFactor);

            _lastWristPose.CopyFrom(root);
        }

        /// <summary>
        /// Updates the rotation of the joints in the hand
        /// using the visual provided values. Sometimes this
        /// might require lerping between the tracked pose
        /// and the provided ones to improve the movement of the fingers
        /// without worrying about when the overwrite values were written.
        ///
        /// During this update the modifier also ensures that fingers that disallow
        /// some movement (locked or constrained) have their values properly set, and
        /// when there is an unlock event the finger values are smoothly animated back to
        /// their tracked rotations.
        /// </summary>
        /// <param name="data">The entire hand data structure to read and write the joints rotations from</param>
        private void UpdateJointsRotation(HandDataAsset data)
        {
            float extraRotationAllowance = 0f;
            Quaternion[] jointRotations = data.Joints;

            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                JointFreedom freedomLevel = _jointsFreedomLevels[i];
                Quaternion desiredRotation = _desiredJointsRotation[i];
                float overrideFactor = _jointsOverrideFactor[i];
                int rawJointIndex = (int)FingersMetadata.HAND_JOINT_IDS[i];

                if (freedomLevel == JointFreedom.Free)
                {
                    //nothing to do, we move the finger freely
                }
                else if (freedomLevel == JointFreedom.Locked)
                {
                    jointRotations[rawJointIndex] = Quaternion.Slerp(
                        jointRotations[rawJointIndex],
                        desiredRotation,
                        overrideFactor);
                }
                else if (freedomLevel == JointFreedom.Constrained)
                {
                    bool jointCanSpread = false;
                    if (FingersMetadata.HAND_JOINT_CAN_SPREAD[i])
                    {
                        jointCanSpread = true;
                        extraRotationAllowance = 0f;
                    }

                    Quaternion maxRotation = desiredRotation * Quaternion.Euler(0f, 0f, -90f * extraRotationAllowance);

                    float overRotation = OverFlex(jointRotations[rawJointIndex], maxRotation);
                    extraRotationAllowance = Mathf.Max(extraRotationAllowance, overRotation);

                    if (overRotation < 0f)
                    {
                        jointRotations[rawJointIndex] = Quaternion.Slerp(
                            jointRotations[rawJointIndex],
                            maxRotation,
                            overrideFactor);
                    }
                    else if (jointCanSpread)
                    {
                        Quaternion trackedRotation = jointRotations[rawJointIndex];
                        float spreadAngle = Vector3.SignedAngle(
                            trackedRotation * Vector3.forward,
                            maxRotation * Vector3.forward,
                            trackedRotation * Vector3.up);

                        float spreadFactor = 1f - Mathf.Clamp01(overRotation * _spreadAllowance);
                        trackedRotation = trackedRotation * Quaternion.Euler(0f, spreadAngle * spreadFactor, 0f);
                        jointRotations[rawJointIndex] = trackedRotation;
                    }
                }

                float smoothFactor = _jointsFreedomLevels[i] == JointFreedom.Free ?
                    _jointUnlockProgressCurves[i].Progress()
                    : _jointLockProgressCurves[i].Progress();

                jointRotations[rawJointIndex] = Quaternion.Slerp(
                    _constrainedJointRotations[i],
                    jointRotations[rawJointIndex],
                    smoothFactor);

                _lastSyntheticRotation[i] = jointRotations[rawJointIndex];
            }
        }

        /// <summary>
        /// Stores the rotation data for all joints in the hand, to be applied during the ApplyHand event.
        /// </summary>
        /// <param name="jointRotations">The joint rotations following the FingersMetadata.HAND_JOINT_IDS format.</param>
        /// <param name="overrideFactor">How much to lerp the fingers from the tracked (raw) state to the provided one.</param>
        public void OverrideAllJoints(in Quaternion[] jointRotations, float overrideFactor)
        {
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                _desiredJointsRotation[i] = jointRotations[i];
                _jointsOverrideFactor[i] = overrideFactor;
            }
        }

        /// <summary>
        /// Stores the rotation data for all joints for the given finger, to be applied during the ApplyHand event.
        /// </summary>
        /// <param name="finger">The finger for which to lock joints.</param>
        /// <param name="rotations">The joint rotations for each joint on the finger</param>
        /// <param name="overrideFactor">How much to lerp the fingers from the tracked (raw) state to the provided one.</param>
        public void OverrideFingerRotations(HandFinger finger, Quaternion[] rotations, float overrideFactor)
        {
            int[] jointIndices = FingersMetadata.FINGER_TO_JOINT_INDEX[(int)finger];
            for (int i = 0; i < jointIndices.Length; i++)
            {
                OverrideJointRotationAtIndex(jointIndices[i], rotations[i], overrideFactor);
            }
        }

        public void OverrideJointRotation(HandJointId jointId, Quaternion rotation, float overrideFactor)
        {
            int jointIndex = FingersMetadata.HandJointIdToIndex(jointId);
            OverrideJointRotationAtIndex(jointIndex, rotation, overrideFactor);
        }

        private void OverrideJointRotationAtIndex(int jointIndex, Quaternion rotation, float overrideFactor)
        {
            _desiredJointsRotation[jointIndex] = rotation;
            _jointsOverrideFactor[jointIndex] = overrideFactor;
        }

        /// <summary>
        /// Immediately locks an individual finger (all its internal joints) at the last known value.
        /// </summary>
        /// <param name="finger">The finger for which to lock joints.</param>
        public void LockFingerAtCurrent(in HandFinger finger)
        {
            SetFingerFreedom(finger, JointFreedom.Locked);

            int fingerIndex = (int)finger;
            int[] jointIndexes = FingersMetadata.FINGER_TO_JOINT_INDEX[fingerIndex];
            for (int i = 0; i < jointIndexes.Length; ++i)
            {
                int jointIndex = jointIndexes[i];
                int rawJointIndex = (int)FingersMetadata.HAND_JOINT_IDS[jointIndex];

                _desiredJointsRotation[jointIndex] = _lastStates.Joints[rawJointIndex];
                _jointsOverrideFactor[jointIndex] = 1f;
            }
        }

        public void LockJoint(in HandJointId jointId, Quaternion rotation, float overrideFactor = 1f)
        {
            int jointIndex = FingersMetadata.HandJointIdToIndex(jointId);
            _desiredJointsRotation[jointIndex] = rotation;
            _jointsOverrideFactor[jointIndex] = 1f;
            SetJointFreedomAtIndex(jointIndex, JointFreedom.Locked);
        }

        /// <summary>
        /// To use in conjunction with OverrideAllJoints, it sets the freedom state for a provided finger.
        /// Opposite to LockFingerAtCurrent, this method uses the data provided in OverrideAllJoints instead
        /// of the last known state.
        /// </summary>
        /// <param name="freedomLevel">The freedom level for the finger</param>
        public void SetFingerFreedom(in HandFinger finger, in JointFreedom freedomLevel, bool skipAnimation = false)
        {
            int[] jointIndexes = FingersMetadata.FINGER_TO_JOINT_INDEX[(int)finger];
            for (int i = 0; i < jointIndexes.Length; ++i)
            {
                SetJointFreedomAtIndex(jointIndexes[i], freedomLevel, skipAnimation);
            }
        }

        public void SetJointFreedom(in HandJointId jointId, in JointFreedom freedomLevel, bool skipAnimation = false)
        {
            int jointIndex = FingersMetadata.HandJointIdToIndex(jointId);
            SetJointFreedomAtIndex(jointIndex, freedomLevel, skipAnimation);
        }

        public JointFreedom GetJointFreedom(in HandJointId jointId)
        {
            int jointIndex = FingersMetadata.HandJointIdToIndex(jointId);
            return _jointsFreedomLevels[jointIndex];
        }

        /// <summary>
        /// Short-hand method for setting the freedom level of all fingers in a hand to Free.
        /// Similar to calling SetFingerFreedom for each single finger in the hand
        /// with a value of FingerFreedom.Free for the freedomLevel
        /// </summary>
        public void FreeAllJoints()
        {
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                SetJointFreedomAtIndex(i, JointFreedom.Free);
            }
        }

        private void SetJointFreedomAtIndex(int jointId, in JointFreedom freedomLevel, bool skipAnimation = false)
        {
            JointFreedom currentFreedom = _jointsFreedomLevels[jointId];
            if (currentFreedom != freedomLevel)
            {
                bool locked = freedomLevel == JointFreedom.Locked
                    || freedomLevel == JointFreedom.Constrained;
                UpdateProgressCurve(ref _jointLockProgressCurves[jointId],
                    ref _jointUnlockProgressCurves[jointId],
                    locked, skipAnimation);
                _constrainedJointRotations[jointId] = _lastSyntheticRotation[jointId];
            }

            _jointsFreedomLevels[jointId] = freedomLevel;
        }

        /// <summary>
        /// Stores the desired pose to set the wrist of the hand to.
        /// This is not necessarily the final pose of the hand, as it allows
        /// lerping between the tracked and provided one during the ApplyHand phase.
        ///
        /// To ensure the hand is locked at the desired pose, pass a value of 1 in the overrideFactor
        /// </summary>
        /// <param name="wristPose">The final pose desired for the wrist</param>
        /// <param name="lockMode">Either lock the position, rotation or both (default)</param>
        /// <param name="overrideFactor">How much to lerp between the tracked and the provided pose</param>
        /// <param name="skipAnimation">Whether to skip the animation curve for this override.</param>
        public void LockWristPose(Pose wristPose, float overrideFactor = 1f, WristLockMode lockMode = WristLockMode.Full, bool worldPose = false, bool skipAnimation = false)
        {
            Pose desiredWristPose = (worldPose && TrackingToWorldTransformer != null ) ? TrackingToWorldTransformer.ToTrackingPose(wristPose) : wristPose;

            if ((lockMode & WristLockMode.Position) != 0)
            {
                LockWristPosition(desiredWristPose.position, overrideFactor, skipAnimation);
            }

            if ((lockMode & WristLockMode.Rotation) != 0)
            {
                LockWristRotation(desiredWristPose.rotation, overrideFactor, skipAnimation);
            }
        }

        public void LockWristPosition(Vector3 position, float overrideFactor = 1f, bool skipAnimation = false)
        {
            _wristPositionOverrideFactor = overrideFactor;
            _desiredWristPose.position = position;
            if (!_wristPositionLocked)
            {
                _wristPositionLocked = true;
                SyntheticWristLockChangedState(WristLockMode.Position, skipAnimation);
            }
        }

        public void LockWristRotation(Quaternion rotation, float overrideFactor = 1f, bool skipAnimation = false)
        {
            _wristRotationOverrideFactor = overrideFactor;
            _desiredWristPose.rotation = rotation;
            if (!_wristRotationLocked)
            {
                _wristRotationLocked = true;
                SyntheticWristLockChangedState(WristLockMode.Rotation, skipAnimation);
            }
        }

        /// <summary>
        /// Unlocks the hand (locked at the OverrideWristPose method) starting
        /// a timer for the smooth release animation.
        /// </summary>
        public void FreeWrist(WristLockMode lockMode = WristLockMode.Full)
        {
            if ((lockMode & WristLockMode.Position) != 0
                && _wristPositionLocked)
            {
                _wristPositionOverrideFactor = 0f;
                _wristPositionLocked = false;
                SyntheticWristLockChangedState(WristLockMode.Position);
            }
            if ((lockMode & WristLockMode.Rotation) != 0
                && _wristRotationLocked)
            {
                _wristRotationOverrideFactor = 0f;
                _wristRotationLocked = false;
                SyntheticWristLockChangedState(WristLockMode.Rotation);
            }
        }

        private void SyntheticWristLockChangedState(WristLockMode lockMode, bool skipAnimation = false)
        {
            if ((lockMode & WristLockMode.Position) != 0)
            {
                UpdateProgressCurve(ref _wristPositionLockCurve, ref _wristPositionUnlockCurve,
                    _wristPositionLocked, skipAnimation);
                _constrainedWristPose.position = _lastWristPose.position;
            }

            if ((lockMode & WristLockMode.Rotation) != 0)
            {
                UpdateProgressCurve(ref _wristRotationLockCurve, ref _wristRotationUnlockCurve,
                    _wristRotationLocked, skipAnimation);
                _constrainedWristPose.rotation = _lastWristPose.rotation;
            }
        }

        /// <summary>
        /// Indicates whether a joint's tracked rotation is past a given rotation.
        /// Works in local Unity Joint coordinates.
        /// This is useful for blocking fingers past the snapping point.
        /// </summary>
        /// <param name="desiredLocalRot">The known local rotation of the joint. </param>
        /// <param name="maxLocalRot">The desired max local rotation of the joint.</param>
        /// <returns>A negative scalar proportional to how much the rotation is over the max one, a proportional positive scalar if under.</returns>
        private static float OverFlex(in Quaternion desiredLocalRot, in Quaternion maxLocalRot)
        {
            Vector3 jointDir = desiredLocalRot * Vector3.right;
            Vector3 jointTan = desiredLocalRot * Vector3.back;
            Vector3 maxDir = maxLocalRot * Vector3.right;
            Vector3 difference = Vector3.Cross(jointDir, maxDir);
            return Vector3.Dot(jointTan, difference);
        }

        private static void UpdateProgressCurve(ref ProgressCurve lockProgress, ref ProgressCurve unlockProgress, bool locked, bool skipAnimation)
        {
            ProgressCurve progress = locked ? lockProgress : unlockProgress;
            if (skipAnimation)
            {
                progress.End();
            }
            else
            {
                progress.Start();
            }
        }

        #region Inject

        public void InjectAllSyntheticHandModifier(UpdateModeFlags updateMode, IDataSource updateAfter,
            DataModifier<HandDataAsset> modifyDataFromSource, bool applyModifier,
            Component[] aspects,
            ProgressCurve wristPositionLockCurve, ProgressCurve wristPositionUnlockCurve,
            ProgressCurve wristRotationLockCurve, ProgressCurve wristRotationUnlockCurve,
            ProgressCurve jointLockCurve, ProgressCurve jointUnlockCurve,
            float spreadAllowance)
        {
            base.InjectAllHand(updateMode, updateAfter, modifyDataFromSource, applyModifier, aspects);

            InjectWristPositionLockCurve(wristPositionLockCurve);
            InjectWristPositionUnlockCurve(wristPositionUnlockCurve);
            InjectWristRotationLockCurve(wristRotationLockCurve);
            InjectWristRotationUnlockCurve(wristRotationUnlockCurve);
            InjectJointLockCurve(jointLockCurve);
            InjectJointUnlockCurve(jointUnlockCurve);
            InjectSpreadAllowance(spreadAllowance);
        }

        public void InjectWristPositionLockCurve(ProgressCurve wristPositionLockCurve) {
            _wristPositionLockCurve = wristPositionLockCurve;
        }

        public void InjectWristPositionUnlockCurve(ProgressCurve wristPositionUnlockCurve) {
            _wristPositionUnlockCurve = wristPositionUnlockCurve;
        }

        public void InjectWristRotationLockCurve(ProgressCurve wristRotationLockCurve) {
            _wristRotationLockCurve = wristRotationLockCurve;
        }

        public void InjectWristRotationUnlockCurve(ProgressCurve wristRotationUnlockCurve) {
            _wristRotationUnlockCurve = wristRotationUnlockCurve;
        }

        public void InjectJointLockCurve(ProgressCurve jointLockCurve) {
            _jointLockCurve = jointLockCurve;
        }

        public void InjectJointUnlockCurve(ProgressCurve jointUnlockCurve) {
            _jointUnlockCurve = jointUnlockCurve;
        }

        public void InjectSpreadAllowance(float spreadAllowance) {
            _spreadAllowance = spreadAllowance;
        }

        #endregion
    }
}
