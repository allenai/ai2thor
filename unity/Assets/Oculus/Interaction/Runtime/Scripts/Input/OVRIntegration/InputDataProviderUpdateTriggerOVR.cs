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
    [DefaultExecutionOrder(-70)]
    public class InputDataProviderUpdateTriggerOVR : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IDataSource))]
        private MonoBehaviour _dataSource;
        private IDataSource DataSource;

        [SerializeField]
        [Tooltip("Force trigger updates every update")]
        private bool _enableUpdate = true;
        [SerializeField]
        [Tooltip("Force trigger updates every fixed update")]
        private bool _enableFixedUpdate = true;

        [SerializeField, Interface(typeof(IOVRCameraRigRef)), Optional]
        [Tooltip("Provide a Camera Rig to Trigger the updates in sync with the OVR anchors update")]
        private MonoBehaviour _cameraRigRef;

        protected bool _started = false;

        public IOVRCameraRigRef CameraRigRef { get; private set; } = null;

        protected virtual void Awake()
        {
            DataSource = _dataSource as IDataSource;
            CameraRigRef = _cameraRigRef as IOVRCameraRigRef;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(DataSource);
            if (_cameraRigRef != null)
            {
                Assert.IsNotNull(CameraRigRef);
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                if (CameraRigRef != null)
                {
                    CameraRigRef.WhenInputDataDirtied += InputDataDirtied;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (CameraRigRef != null)
                {
                    CameraRigRef.WhenInputDataDirtied -= InputDataDirtied;
                }
            }
        }

        protected virtual void Update()
        {
            if (_enableUpdate)
            {
                MarkRequiresUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_enableFixedUpdate)
            {
                MarkRequiresUpdate();
            }
        }

        private void InputDataDirtied(bool isLateUpdate)
        {
            if(!isLateUpdate)
            {
                MarkRequiresUpdate();
            }
        }

        private void MarkRequiresUpdate()
        {
            DataSource.MarkInputDataRequiresUpdate();
        }


        #region Inject

        public void InjectAllInputDataProviderUpdateTriggerOVR(IDataSource dataSource, bool enableUpdate, bool enableFixedUpdate)
        {
            InjectDataSource(dataSource);
            InjectEnableUpdate(enableUpdate);
            InjectEnableFixedUpdate(enableFixedUpdate);
        }

        public void InjectDataSource(IDataSource dataSource)
        {
            _dataSource = dataSource as MonoBehaviour;
            DataSource = dataSource;
        }

        public void InjectEnableUpdate(bool enableUpdate)
        {
            _enableUpdate = enableUpdate;
        }

        public void InjectEnableFixedUpdate(bool enableFixedUpdate)
        {
            _enableFixedUpdate = enableFixedUpdate;
        }

        public void InjectOptionalCameraRigRef(IOVRCameraRigRef cameraRigRef)
        {
            _cameraRigRef = cameraRigRef as MonoBehaviour;
            CameraRigRef = cameraRigRef;
        }

        #endregion
    }
}
