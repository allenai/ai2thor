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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    public abstract class
        DataModifier<TData> : DataSource<TData>
        where TData : class, ICopyFrom<TData>, new()
    {
        [Header("Data Modifier")]
        [SerializeField, Interface(nameof(_modifyDataFromSource))]
        protected MonoBehaviour _iModifyDataFromSourceMono;
        private IDataSource<TData> _modifyDataFromSource;

        [SerializeField]
        [Tooltip("If this is false, then this modifier will simply pass through " +
                 "data without performing any modification. This saves on memory " +
                 "and computation")]
        private bool _applyModifier = true;

        private static TData InvalidAsset { get; } = new TData();
        private TData _thisDataAsset;
        private TData _currentDataAsset = InvalidAsset;

        protected override TData DataAsset => _currentDataAsset;

        public virtual IDataSource<TData> ModifyDataFromSource => _modifyDataFromSource == null
            ? (_modifyDataFromSource = _iModifyDataFromSourceMono as IDataSource<TData>)
            : _modifyDataFromSource;

        public override int CurrentDataVersion
        {
            get
            {
                return _applyModifier
                    ? base.CurrentDataVersion
                    : ModifyDataFromSource.CurrentDataVersion;
            }
        }

        public void ResetSources(IDataSource<TData> modifyDataFromSource, IDataSource updateAfter, UpdateModeFlags updateMode)
        {
            ResetUpdateAfter(updateAfter, updateMode);
            _modifyDataFromSource = modifyDataFromSource;
            _currentDataAsset = InvalidAsset;
        }

        protected override void UpdateData()
        {
            if (_applyModifier)
            {
                if (_thisDataAsset == null)
                {
                    _thisDataAsset = new TData();
                }

                _thisDataAsset.CopyFrom(ModifyDataFromSource.GetData());
                _currentDataAsset = _thisDataAsset;
                Apply(_currentDataAsset);
            }
            else
            {
                _currentDataAsset = ModifyDataFromSource.GetData();
            }
        }

        protected abstract void Apply(TData data);

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(ModifyDataFromSource);
        }

        #region Inject
        public void InjectAllDataModifier(UpdateModeFlags updateMode, IDataSource updateAfter, IDataSource<TData> modifyDataFromSource, bool applyModifier)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectModifyDataFromSource(modifyDataFromSource);
            InjectApplyModifier(applyModifier);
        }

        public void InjectModifyDataFromSource(IDataSource<TData> modifyDataFromSource)
        {
            _modifyDataFromSource = modifyDataFromSource;
        }

        public void InjectApplyModifier(bool applyModifier)
        {
            _applyModifier = applyModifier;
        }
        #endregion
    }
}
