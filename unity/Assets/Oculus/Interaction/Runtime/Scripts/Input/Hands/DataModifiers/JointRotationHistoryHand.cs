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

using Oculus.Interaction.Throw;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Tracks the history of finger rotations and can be set to use the joint
    /// rotations from some number of frames ago.
    /// </summary>
    public class JointRotationHistoryHand : Hand
    {
        [SerializeField]
        private int _historyLength = 60;

        [SerializeField]
        private int _historyOffset = 5;

        private Quaternion[][] _jointHistory = new Quaternion[(int)HandJointId.HandMaxSkinnable][];
        private int _historyIndex = 0;
        private int _capturedDataVersion;

        protected override void Start()
        {
            base.Start();

            for (int i = 0; i < _jointHistory.Length; i++)
            {
                _jointHistory[i] = new Quaternion[_historyLength];
                for (int j = 0; j < _historyLength; j++)
                {
                    _jointHistory[i][j] = Quaternion.identity;
                }
            }
        }

        #region DataModifier Implementation
        protected override void Apply(HandDataAsset data)
        {
            if (!data.IsDataValid)
            {
                return;
            }

            if (_capturedDataVersion != ModifyDataFromSource.CurrentDataVersion)
            {
                _capturedDataVersion = ModifyDataFromSource.CurrentDataVersion;

                _historyIndex = (_historyIndex + 1) % _historyLength;
                for (int i = 0; i < _jointHistory.Length; i++)
                {
                    _jointHistory[i][_historyIndex] = data.Joints[i];
                }
            }

            _historyOffset = Mathf.Clamp(_historyOffset, 0, _historyLength);
            int index = (_historyIndex + _historyLength - _historyOffset) % _historyLength;
            for (int i = 0; i < _jointHistory.Length; i++)
            {
                data.Joints[i] = _jointHistory[i][index];
            }
        }
        #endregion

        public void SetHistoryOffset(int offset)
        {
            _historyOffset = offset;
            MarkInputDataRequiresUpdate();
        }

        #region Inject

        public void InjectAllJointHistoryHand(UpdateModeFlags updateMode, IDataSource updateAfter,
            DataModifier<HandDataAsset> modifyDataFromSource, bool applyModifier,
            Component[] aspects, int historyLength, int historyOffset)
        {
            base.InjectAllHand(updateMode, updateAfter, modifyDataFromSource, applyModifier, aspects);
            InjectHistoryLength(historyLength);
            SetHistoryOffset(historyOffset);
        }

        public void InjectHistoryLength(int historyLength)
        {
            _historyLength = historyLength;
        }

        #endregion
    }
}
