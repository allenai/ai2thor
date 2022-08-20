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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    public interface IDataSource
    {
        int CurrentDataVersion { get; }
        void MarkInputDataRequiresUpdate();
        event Action InputDataAvailable;
    }

    public interface IDataSource<TData> : IDataSource
    {
        TData GetData();
    }

    public abstract class DataSource<TData> : MonoBehaviour, IDataSource<TData>
        where TData : class, ICopyFrom<TData>, new()
    {
        public bool Started { get; private set; }
        protected bool _started = false;
        private bool _requiresUpdate = true;

        [Flags]
        public enum UpdateModeFlags
        {
            Manual = 0,
            UnityUpdate = 1 << 0,
            UnityFixedUpdate = 1 << 1,
            UnityLateUpdate = 1 << 2,
            AfterPreviousStep = 1 << 3
        }

        [Header("Update")]
        [SerializeField]
        private UpdateModeFlags _updateMode;
        public UpdateModeFlags UpdateMode => _updateMode;

        [SerializeField, Interface(typeof(IDataSource)), Optional]
        private MonoBehaviour _updateAfter;

        private IDataSource UpdateAfter;
        private int _currentDataVersion;

        protected bool UpdateModeAfterPrevious => (_updateMode & UpdateModeFlags.AfterPreviousStep) != 0;

        // Notifies that new data is available for query via GetData() method.
        // Do not use this event if you are reading data from a `Oculus.Interaction.Input.Hand` object,
        // instead, use the `Updated` event on that class.
        public event Action InputDataAvailable = delegate { };

        public virtual int CurrentDataVersion => _currentDataVersion;

        #region Unity Lifecycle
        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            if (_updateAfter != null)
            {
                UpdateAfter = _updateAfter as IDataSource;
                Assert.IsNotNull(UpdateAfter);
            }
            Started = true;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (Started && UpdateModeAfterPrevious && UpdateAfter != null)
                {
                    UpdateAfter.InputDataAvailable += MarkInputDataRequiresUpdate;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (UpdateAfter != null)
                {
                    UpdateAfter.InputDataAvailable -= MarkInputDataRequiresUpdate;
                }
            }
        }

        protected virtual void Update()
        {
            if ((_updateMode & UpdateModeFlags.UnityUpdate) != 0)
            {
                MarkInputDataRequiresUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            if ((_updateMode & UpdateModeFlags.UnityFixedUpdate) != 0)
            {
                MarkInputDataRequiresUpdate();
            }
        }

        protected virtual void LateUpdate()
        {
            if ((_updateMode & UpdateModeFlags.UnityLateUpdate) != 0)
            {
                MarkInputDataRequiresUpdate();
            }
        }
        #endregion

        protected void ResetUpdateAfter(IDataSource updateAfter, UpdateModeFlags updateMode)
        {
            bool wasActive = isActiveAndEnabled;
            if (isActiveAndEnabled) { OnDisable(); }

            _updateMode = updateMode;
            UpdateAfter = updateAfter;
            _requiresUpdate = true;
            _currentDataVersion += 1;

            if (wasActive) { OnEnable(); }
        }

        public TData GetData()
        {
            if (RequiresUpdate())
            {
                UpdateData();
                _requiresUpdate = false;
            }

            return DataAsset;
        }

        protected bool RequiresUpdate()
        {
            return _requiresUpdate;
        }

        /// <summary>
        /// Marks the DataAsset stored as outdated, which means it will be
        /// re-processed JIT during the next call to GetData.
        /// </summary>
        public virtual void MarkInputDataRequiresUpdate()
        {
            _requiresUpdate = true;
            _currentDataVersion += 1;
            InputDataAvailable();
        }

        protected abstract void UpdateData();

        /// <summary>
        /// Returns the current DataAsset, without performing any updates.
        /// </summary>
        /// <returns>
        /// Null if no call to GetData has been made since this data source was initialized.
        /// </returns>
        protected abstract TData DataAsset { get; }

        #region Inject
        public void InjectAllDataSource(UpdateModeFlags updateMode, IDataSource updateAfter)
        {
            InjectUpdateMode(updateMode);
            InjectUpdateAfter(updateAfter);
        }

        public void InjectUpdateMode(UpdateModeFlags updateMode)
        {
            _updateMode = updateMode;
        }

        public void InjectUpdateAfter(IDataSource updateAfter)
        {
            _updateAfter = updateAfter as MonoBehaviour;
            UpdateAfter = updateAfter;
        }
        #endregion
    }
}
