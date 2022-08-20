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
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace Oculus.Interaction.Input.Filter
{
    // Temporary structure used to pass data to and from native components
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HandData
    {
        private const int NumHandJoints = 24;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NumHandJoints * 4, ArraySubType = UnmanagedType.R4)]
        private readonly float[] jointValues;
        private readonly float _rootRotX;
        private readonly float _rootRotY;
        private readonly float _rootRotZ;
        private readonly float _rootRotW;
        private readonly float _rootPosX;
        private readonly float _rootPosY;
        private readonly float _rootPosZ;

        public HandData(IReadOnlyList<Quaternion> joints, Pose root)
        {
            Assert.AreEqual(NumHandJoints, joints.Count);
            jointValues = new float[NumHandJoints * 4];
            for (int jointIndex = 0; jointIndex < NumHandJoints; jointIndex++)
            {
                Quaternion joint = joints[jointIndex];
                int jointValueIndex = jointIndex * 4;
                jointValues[jointValueIndex + 0] = joint.x;
                jointValues[jointValueIndex + 1] = joint.y;
                jointValues[jointValueIndex + 2] = joint.z;
                jointValues[jointValueIndex + 3] = joint.w;
            }
            this._rootRotX = root.rotation.x;
            this._rootRotY = root.rotation.y;
            this._rootRotZ = root.rotation.z;
            this._rootRotW = root.rotation.w;
            this._rootPosX = root.position.x;
            this._rootPosY = root.position.y;
            this._rootPosZ = root.position.z;
        }

        public void GetData(ref Quaternion[] joints, out Pose root)
        {
            Assert.AreEqual(NumHandJoints, joints.Length);
            for (int jointIndex = 0; jointIndex < NumHandJoints; jointIndex++)
            {
                int jointValueIndex = jointIndex * 4;
                joints[jointIndex].x = jointValues[jointValueIndex + 0];
                joints[jointIndex].y = jointValues[jointValueIndex + 1];
                joints[jointIndex].z = jointValues[jointValueIndex + 2];
                joints[jointIndex].w = jointValues[jointValueIndex + 3];
            }

            root = new Pose(new Vector3(_rootPosX, _rootPosY, _rootPosZ),
                new Quaternion(_rootRotX, _rootRotY, _rootRotZ, _rootRotW));
        }
    }

    public class HandFilter : Hand
    {
        #region Oculus Library Methods and Constants
        [DllImport("InteractionSdk")]
        private static extern int isdk_DataSource_Create(int id);
        [DllImport("InteractionSdk")]
        private static extern int isdk_DataSource_Destroy(int handle);
        [DllImport("InteractionSdk")]
        private static extern int isdk_DataModifier_Create(int id, int handle);
        [DllImport("InteractionSdk")]
        private static extern int isdk_DataSource_Update(int handle);
        [DllImport("InteractionSdk")]
        private static extern int isdk_DataSource_GetData(int handle, ref HandData data);
        [DllImport("InteractionSdk")]
        private static extern int isdk_ExternalHandSource_SetData(int handle, in HandData data);
        [DllImport("InteractionSdk")]
        private static extern int isdk_DataSource_SetAttributeFloat(int handle, int attrId, float value);

        enum AttributeId
        {
            Unknown = 0,
            WristPosBeta,
            WristPosMinCutOff,
            WristRotBeta,
            WristRotMinCutOff,
            FingerRotBeta,
            FingerRotMinCutOff,
            Frequency,
            WristPosDeltaCutOff,
            WristRotDeltaCutOff,
            FingerRotDeltaCutOff,
        };

        private const int _isdkExternalHandSourceId = 2;
        private const int _isdkOneEuroHandModifierId = 1;
        private const int _isdkSuccess = 0;
        #endregion Oculus Library Methods and Constants

        #region Tuneable Values
        [Header("Settings")]
        [Tooltip("Applies a One Euro Filter when filter parameters are provided")]
        [SerializeField, Optional]
        private HandFilterParameterBlock _filterParameters = null;
        #endregion Tuneable Values

        private int _dataSourceHandle = -1;
        private int _handModifierHandle = -1;
        private const string _logPrefix = "[Oculus.Interaction]";
        private bool _hasFlaggedError = false;

        protected virtual void Awake()
        {
            _dataSourceHandle = isdk_DataSource_Create(_isdkExternalHandSourceId);
            Assert.IsTrue(_dataSourceHandle >= 0, $"{_logPrefix} Unable to allocate external hand data source!");

            _handModifierHandle = isdk_DataModifier_Create(_isdkOneEuroHandModifierId, _dataSourceHandle);
            Assert.IsTrue(_handModifierHandle >= 0, $"{_logPrefix} Unable to allocate one euro hand data modifier!");
        }

        protected virtual void OnDestroy()
        {
            int result = -1;

            //Release the filter and source
            result = isdk_DataSource_Destroy(_handModifierHandle);
            Assert.AreEqual(_isdkSuccess, result);
            result = isdk_DataSource_Destroy(_dataSourceHandle);
            Assert.AreEqual(_isdkSuccess, result);
        }

        protected override void Apply(HandDataAsset handDataAsset)
        {
            base.Apply(handDataAsset);

            if (!handDataAsset.IsTracked)
            {
                return;
            }

            if (UpdateFilterParameters() && UpdateHandData(handDataAsset))
            {
                return;
            }

            if (_hasFlaggedError)
                return;

            _hasFlaggedError = true;
            Debug.LogError("Unable to send value to filter, InteractionSDK plugin may be missing or corrupted");
        }

        protected bool UpdateFilterParameters()
        {
            if (_filterParameters == null)
                return true;

            int result = -1;

            // wrist position
            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.WristPosBeta,
                _filterParameters.wristPositionParameters.Beta);
            if (result != _isdkSuccess)
            {
                return false;
            }

            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.WristPosMinCutOff,
                _filterParameters.wristPositionParameters.MinCutoff);
            if (result != _isdkSuccess)
            {
                return false;
            }

            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.WristPosDeltaCutOff,
                _filterParameters.wristPositionParameters.DCutoff);
            if (result != _isdkSuccess)
            {
                return false;
            }


            // wrist rotation
            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.WristRotBeta,
                _filterParameters.wristRotationParameters.Beta);
            if (result != _isdkSuccess)
            {
                return false;
            }

            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.WristRotMinCutOff,
                _filterParameters.wristRotationParameters.MinCutoff);
            if (result != _isdkSuccess)
            {
                return false;
            }

            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.WristRotDeltaCutOff,
                _filterParameters.wristRotationParameters.DCutoff);
            if (result != _isdkSuccess)
            {
                return false;
            }

            // finger rotation
            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.FingerRotBeta,
                _filterParameters.fingerRotationParameters.Beta);
            if (result != _isdkSuccess)
            {
                return false;
            }

            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.FingerRotMinCutOff,
                _filterParameters.fingerRotationParameters.MinCutoff);
            if (result != _isdkSuccess)
            {
                return false;
            }

            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.FingerRotDeltaCutOff,
                _filterParameters.fingerRotationParameters.DCutoff);
            if (result != _isdkSuccess)
            {
                return false;
            }

            // frequency
            result = isdk_DataSource_SetAttributeFloat(
                _handModifierHandle, (int)AttributeId.Frequency,
                _filterParameters.frequency);
            if (result != _isdkSuccess)
            {
                return false;
            }

            return true;
        }

        protected bool UpdateHandData(HandDataAsset handDataAsset)
        {
            // null parameters implies don't filter
            if (_filterParameters == null)
                return true;

            int result = -1;

            // pipe data asset into temp struct
            HandData handData = new HandData(handDataAsset.Joints, handDataAsset.Root);

            // Send it
            result = isdk_ExternalHandSource_SetData(_dataSourceHandle, handData);
            if (result != _isdkSuccess)
            {
                return false;
            }

            // Update
            result = isdk_DataSource_Update(_handModifierHandle);
            if (result != _isdkSuccess)
            {
                return false;
            }

            // Get result
            result = isdk_DataSource_GetData(_handModifierHandle, ref handData);
            if (result != _isdkSuccess)
            {
                return false;
            }

            // Copy results into our hand data asset
            handData.GetData(ref handDataAsset.Joints, out handDataAsset.Root);

            return true;
        }
    }
}
